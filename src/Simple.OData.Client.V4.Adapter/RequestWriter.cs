using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Spatial;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.V4.Adapter
{
	public class RequestWriter : RequestWriterBase
	{
		private readonly IEdmModel _model;
		private readonly Dictionary<ODataResource, ResourceProperties> _resourceEntryMap;
		private readonly Dictionary<ODataResource, List<ODataResource>> _resourceEntries;

		public RequestWriter(ISession session, IEdmModel model, Lazy<IBatchWriter> deferredBatchWriter)
			: base(session, deferredBatchWriter)
		{
			_model = model;
			_resourceEntryMap = new Dictionary<ODataResource, ResourceProperties>();
			_resourceEntries = new Dictionary<ODataResource, List<ODataResource>>();
		}

		private void RegisterRootEntry(ODataResource root)
		{
			_resourceEntries.Add(root, new List<ODataResource>());
		}

		private void UnregisterRootEntry(ODataResource root)
		{
			if (_resourceEntries.TryGetValue(root, out var entries))
			{
				foreach (var entry in entries)
				{
					_resourceEntryMap.Remove(entry);
				}

				_resourceEntries.Remove(root);
			}
		}

		protected async override Task<Stream> WriteEntryContentAsync(string method, string collection, string commandText, IDictionary<string, object> entryData, bool resultRequired)
		{
			var message = IsBatch
				? await CreateBatchOperationMessageAsync(method, collection, entryData, commandText, resultRequired).ConfigureAwait(false)
				: new ODataRequestMessage();

			if (method == RestVerbs.Get || method == RestVerbs.Delete)
			{
				return null;
			}

			var entityType = _model.FindDeclaredType(
				_session.Metadata.GetQualifiedTypeName(collection)) as IEdmEntityType;
			var model = (method == RestVerbs.Patch || method == RestVerbs.Merge) ? new EdmDeltaModel(_model, entityType, entryData.Keys) : _model;

			using var messageWriter = new ODataMessageWriter(message, GetWriterSettings(), model);
			var contentId = _deferredBatchWriter?.Value.GetContentId(entryData, null);
			var entityCollection = _session.Metadata.NavigateToCollection(collection);
			var entryDetails = _session.Metadata.ParseEntryDetails(entityCollection.Name, entryData, contentId);

			var entryWriter = await messageWriter.CreateODataResourceWriterAsync().ConfigureAwait(false);
			var entry = CreateODataEntry(entityType.FullName(), entryDetails.Properties, null);

			RegisterRootEntry(entry);
			await WriteEntryPropertiesAsync(entryWriter, entry, entryDetails.Links).ConfigureAwait(false);
			UnregisterRootEntry(entry);

			return IsBatch ? null : await message.GetStreamAsync().ConfigureAwait(false);
		}

		private async Task WriteEntryPropertiesAsync(ODataWriter entryWriter, ODataResource entry, IDictionary<string, List<ReferenceLink>> links)
		{
			await entryWriter.WriteStartAsync(entry).ConfigureAwait(false);
			if (_resourceEntryMap.TryGetValue(entry, out var resourceEntry))
			{
				if (resourceEntry.CollectionProperties != null)
				{
					foreach (var prop in resourceEntry.CollectionProperties)
					{
						if (prop.Value != null)
						{
							await WriteNestedCollectionAsync(entryWriter, prop.Key, prop.Value).ConfigureAwait(false);
						}
					}
				}

				if (resourceEntry.StructuralProperties != null)
				{
					foreach (var prop in resourceEntry.StructuralProperties)
					{
						if (prop.Value != null)
						{
							await WriteNestedEntryAsync(entryWriter, prop.Key, prop.Value).ConfigureAwait(false);
						}
					}
				}
			}

			if (links != null)
			{
				foreach (var link in links)
				{
					if (link.Value.Any(x => x.LinkData != null))
					{
						await WriteLinkAsync(entryWriter, entry.TypeName, link.Key, link.Value)
							.ConfigureAwait(false);
					}
				}
			}

			await entryWriter.WriteEndAsync().ConfigureAwait(false);
		}

		private async Task WriteNestedCollectionAsync(ODataWriter entryWriter, string entryName, ODataCollectionValue collection)
		{
			await entryWriter.WriteStartAsync(new ODataNestedResourceInfo()
			{
				Name = entryName,
				IsCollection = true,
			}).ConfigureAwait(false);

			await entryWriter.WriteStartAsync(new ODataResourceSet())
				.ConfigureAwait(false);
			foreach (var item in collection.Items)
			{
				await WriteEntryPropertiesAsync(entryWriter, item as ODataResource, null)
					.ConfigureAwait(false);
			}

			await entryWriter.WriteEndAsync().ConfigureAwait(false);

			await entryWriter.WriteEndAsync().ConfigureAwait(false);
		}

		private async Task WriteNestedEntryAsync(ODataWriter entryWriter, string entryName, ODataResource entry)
		{
			await entryWriter.WriteStartAsync(new ODataNestedResourceInfo()
			{
				Name = entryName,
				IsCollection = false,
			}).ConfigureAwait(false);

			await WriteEntryPropertiesAsync(entryWriter, entry, null).ConfigureAwait(false);

			await entryWriter.WriteEndAsync().ConfigureAwait(false);
		}

		protected async override Task<Stream> WriteLinkContentAsync(string method, string commandText, string linkIdent)
		{
			var message = IsBatch
				? await CreateBatchOperationMessageAsync(method, null, null, commandText, false).ConfigureAwait(false)
				: new ODataRequestMessage();

			using var messageWriter = new ODataMessageWriter(message, GetWriterSettings(), _model);
			var link = new ODataEntityReferenceLink
			{
				Url = Utils.CreateAbsoluteUri(_session.Settings.BaseUri.AbsoluteUri, linkIdent)
			};
			await messageWriter.WriteEntityReferenceLinkAsync(link).ConfigureAwait(false);
			return IsBatch ? null : await message.GetStreamAsync().ConfigureAwait(false);
		}

		protected async override Task<Stream> WriteFunctionContentAsync(string method, string commandText)
		{
			if (IsBatch)
			{
				await CreateBatchOperationMessageAsync(method, null, null, commandText, true).ConfigureAwait(false);
			}

			return null;
		}

		protected async override Task<Stream> WriteActionContentAsync(
			string method,
			string commandText,
			string actionName,
			string boundTypeName,
			IDictionary<string, object> parameters)
		{
			var message = IsBatch
				? await CreateBatchOperationMessageAsync(method, null, null, commandText, true).ConfigureAwait(false)
				: new ODataRequestMessage();

			using var messageWriter = new ODataMessageWriter(message, GetWriterSettings(), _model);

			static bool typeMatch(IEdmOperationParameter parameter, IEdmType baseType) =>
				parameter == null ||
				parameter.Type.Definition == baseType ||
				parameter.Type.Definition.TypeKind == EdmTypeKind.Collection &&
					(parameter.Type.Definition as IEdmCollectionType).ElementType.Definition == baseType;

			var action = boundTypeName == null
				? _model.SchemaElements.BestMatch(
					x => x.SchemaElementKind == EdmSchemaElementKind.Action,
					x => x.Name, actionName, _session.Settings.NameMatchResolver) as IEdmAction
				: _model.SchemaElements.BestMatch(
					x => x.SchemaElementKind == EdmSchemaElementKind.Action
						 && typeMatch(
							 ((IEdmAction)x).Parameters.FirstOrDefault(p => p.Name == "bindingParameter"),
							 _model.FindDeclaredType(boundTypeName)),
					x => x.Name, actionName, _session.Settings.NameMatchResolver) as IEdmAction;
			var parameterWriter = await messageWriter.CreateODataParameterWriterAsync(action).ConfigureAwait(false);

			await parameterWriter
				.WriteStartAsync()
				.ConfigureAwait(false);

			foreach (var parameter in parameters)
			{
				var operationParameter = action.Parameters.BestMatch(x => x.Name, parameter.Key, _session.Settings.NameMatchResolver);
				if (operationParameter == null)
				{
					throw new UnresolvableObjectException(parameter.Key, $"Parameter [{parameter.Key}] not found for action [{actionName}]");
				}

				await WriteOperationParameterAsync(parameterWriter, operationParameter, parameter.Key, parameter.Value).ConfigureAwait(false);
			}

			await parameterWriter.WriteEndAsync().ConfigureAwait(false);
			return IsBatch ? null : await message.GetStreamAsync().ConfigureAwait(false);
		}

		private async Task WriteOperationParameterAsync(ODataParameterWriter parameterWriter, IEdmOperationParameter operationParameter, string paramName, object paramValue)
		{
			switch (operationParameter.Type.Definition.TypeKind)
			{
				case EdmTypeKind.Primitive:
					var value = GetPropertyValue(operationParameter.Type, paramValue, null);
					await parameterWriter.WriteValueAsync(paramName, value).ConfigureAwait(false);
					break;

				case EdmTypeKind.Enum:
					await parameterWriter.WriteValueAsync(paramName, new ODataEnumValue(paramValue.ToString())).ConfigureAwait(false);
					break;

				case EdmTypeKind.Untyped:
					await parameterWriter.WriteValueAsync(paramName, new ODataUntypedValue { RawValue = paramValue.ToString() }).ConfigureAwait(false);
					break;

				case EdmTypeKind.Entity:
					{
						var entryWriter = await parameterWriter.CreateResourceWriterAsync(paramName).ConfigureAwait(false);
						var paramValueDict = paramValue.ToDictionary(TypeCache);
						var contentId = _deferredBatchWriter?.Value.GetContentId(paramValueDict, null);

						var typeName = operationParameter.Type.Definition.FullTypeName();
						if (paramValueDict.ContainsKey("@odata.type") && paramValueDict["@odata.type"] is string)
						{
							typeName = paramValueDict["@odata.type"] as string;
							paramValueDict.Remove("@odata.type");
						}

						var entryDetails = _session.Metadata.ParseEntryDetails(typeName, paramValueDict, contentId);
						var entry = CreateODataEntry(typeName, entryDetails.Properties, null);

						RegisterRootEntry(entry);
						await WriteEntryPropertiesAsync(entryWriter, entry, entryDetails.Links).ConfigureAwait(false);
						UnregisterRootEntry(entry);
					}

					break;
				case EdmTypeKind.Complex:
					{
						var entryWriter = await parameterWriter.CreateResourceWriterAsync(paramName).ConfigureAwait(false);
						var paramValueDict = paramValue.ToDictionary(TypeCache);

						var typeName = operationParameter.Type.Definition.FullTypeName();
						if (paramValueDict.ContainsKey("@odata.type") && paramValueDict["@odata.type"] is string)
						{
							typeName = paramValueDict["@odata.type"] as string;
							paramValueDict.Remove("@odata.type");
						}

						var entry = CreateODataEntry(typeName, paramValueDict, null);

						RegisterRootEntry(entry);
						await WriteEntryPropertiesAsync(entryWriter, entry, new Dictionary<string, List<ReferenceLink>>()).ConfigureAwait(false);
						UnregisterRootEntry(entry);
					}

					break;

				case EdmTypeKind.Collection:
					var collectionType = operationParameter.Type.Definition as IEdmCollectionType;
					var elementType = collectionType.ElementType;
					if (elementType.Definition.TypeKind == EdmTypeKind.Entity)
					{
						var feedWriter = await parameterWriter.CreateResourceSetWriterAsync(paramName).ConfigureAwait(false);
						var feed = new ODataResourceSet();
						await feedWriter.WriteStartAsync(feed).ConfigureAwait(false);
						foreach (var item in (IEnumerable)paramValue)
						{
							var feedEntry = CreateODataEntry(elementType.Definition.FullTypeName(), item.ToDictionary(TypeCache), null);

							RegisterRootEntry(feedEntry);
							await feedWriter.WriteStartAsync(feedEntry).ConfigureAwait(false);
							await feedWriter.WriteEndAsync().ConfigureAwait(false);
							UnregisterRootEntry(feedEntry);
						}

						await feedWriter.WriteEndAsync().ConfigureAwait(false);
					}
					else
					{
						var collectionWriter = await parameterWriter.CreateCollectionWriterAsync(paramName).ConfigureAwait(false);
						await collectionWriter.WriteStartAsync(new ODataCollectionStart()).ConfigureAwait(false);
						foreach (var item in (IEnumerable)paramValue)
						{
							await collectionWriter.WriteItemAsync(item).ConfigureAwait(false);
						}

						await collectionWriter.WriteEndAsync().ConfigureAwait(false);
					}

					break;

				default:
					throw new NotSupportedException($"Unable to write action parameter of a type {operationParameter.Type.Definition.TypeKind}");
			}
		}

		protected async override Task<Stream> WriteStreamContentAsync(Stream stream, bool writeAsText)
		{
			var message = new ODataRequestMessage();
			using var messageWriter = new ODataMessageWriter(message, GetWriterSettings(ODataFormat.RawValue), _model);
			var value = writeAsText ? (object)Utils.StreamToString(stream) : Utils.StreamToByteArray(stream);
			await messageWriter.WriteValueAsync(value).ConfigureAwait(false);
			return await message.GetStreamAsync().ConfigureAwait(false);
		}

		protected override string FormatLinkPath(
			string entryIdent,
			string navigationPropertyName,
			string? linkIdent = null)
		{
			var linkPath = $"{entryIdent}/{navigationPropertyName}/$ref";
			if (linkIdent != null)
			{
				var link = _session.Settings.UseAbsoluteReferenceUris
					? Utils.CreateAbsoluteUri(_session.Settings.BaseUri.AbsoluteUri, linkIdent).AbsoluteUri
					: linkIdent;
				linkPath += $"?$id={link}";
			}

			return linkPath;
		}

		protected override void AssignHeaders(ODataRequest request)
		{
			// Prefer in a GET or DELETE request does not have any effect per standard
			if (request.Method != RestVerbs.Get && request.Method != RestVerbs.Delete)
			{
				request.Headers[HttpLiteral.Prefer] =
							   request.ResultRequired ? HttpLiteral.ReturnRepresentation : HttpLiteral.ReturnMinimal;
			}
		}

		private async Task<IODataRequestMessageAsync> CreateBatchOperationMessageAsync(
			string method,
			string? collection,
			IDictionary<string, object>? entryData,
			string commandText,
			bool resultRequired)
		{
			var message = (await _deferredBatchWriter.Value.CreateOperationMessageAsync(
				Utils.CreateAbsoluteUri(_session.Settings.BaseUri.AbsoluteUri, commandText),
				method,
				collection,
				entryData,
				resultRequired).ConfigureAwait(false)) as IODataRequestMessageAsync;

			return message;
		}

		private async Task WriteLinkAsync(ODataWriter entryWriter, string typeName, string linkName, IEnumerable<ReferenceLink> links)
		{
			var navigationProperty = (_model.FindDeclaredType(typeName) as IEdmEntityType).NavigationProperties()
				.BestMatch(x => x.Name, linkName, _session.Settings.NameMatchResolver);
			var isCollection = navigationProperty.Type.Definition.TypeKind == EdmTypeKind.Collection;

			var linkType = GetNavigationPropertyEntityType(navigationProperty);
			var linkTypeWithKey = linkType;
			while (linkTypeWithKey.DeclaredKey == null && linkTypeWithKey.BaseEntityType() != null)
			{
				linkTypeWithKey = linkTypeWithKey.BaseEntityType();
			}

			await entryWriter.WriteStartAsync(new ODataNestedResourceInfo()
			{
				Name = linkName,
				IsCollection = isCollection,
				Url = new Uri(ODataNamespace.Related + linkType, UriKind.Absolute),
			}).ConfigureAwait(false);

			foreach (var referenceLink in links)
			{
				var linkKey = linkTypeWithKey.DeclaredKey;
				var linkEntry = referenceLink.LinkData.ToDictionary(TypeCache);
				var contentId = GetContentId(referenceLink);
				string linkUri;
				if (contentId != null)
				{
					linkUri = "$" + contentId;
				}
				else
				{
					var formattedKey = _session.Adapter.GetCommandFormatter().ConvertKeyValuesToUriLiteral(
						linkKey.ToDictionary(x => x.Name, x => linkEntry[x.Name]), true);
					var linkedCollectionName = _session.Metadata.GetLinkedCollectionName(
						referenceLink.LinkData.GetType().Name, linkTypeWithKey.Name, out var isSingleton);
					linkUri = linkedCollectionName + (isSingleton ? string.Empty : formattedKey);
				}

				var link = new ODataEntityReferenceLink
				{
					Url = Utils.CreateAbsoluteUri(_session.Settings.BaseUri.AbsoluteUri, linkUri)
				};

				await entryWriter.WriteEntityReferenceLinkAsync(link).ConfigureAwait(false);
			}

			await entryWriter.WriteEndAsync().ConfigureAwait(false);
		}

		private static IEdmEntityType GetNavigationPropertyEntityType(IEdmNavigationProperty navigationProperty)
		{
			if (navigationProperty.Type.Definition.TypeKind == EdmTypeKind.Collection)
			{
				return (navigationProperty.Type.Definition as IEdmCollectionType)?.ElementType.Definition as IEdmEntityType;
			}
			else
			{
				return navigationProperty.Type.Definition as IEdmEntityType;
			}
		}

		private ODataMessageWriterSettings GetWriterSettings(
			ODataFormat? preferredContentType = null)
		{
			var settings = new ODataMessageWriterSettings()
			{
				ODataUri = new ODataUri()
				{
					RequestUri = _session.Settings.BaseUri,
				},
				EnableMessageStreamDisposal = IsBatch,
				Validations = (Microsoft.OData.ValidationKinds)_session.Settings.Validations
			};
			var contentType = preferredContentType ?? ODataFormat.Json;
			settings.SetContentType(contentType);
			return settings;
		}

		private ODataResource CreateODataEntry(
			string typeName,
			IDictionary<string, object> properties,
			ODataResource? root)
		{
			var entry = new ODataResource { TypeName = typeName };
			root ??= entry;

			var entryType = _model.FindDeclaredType(entry.TypeName);
			var typeProperties = typeof(IEdmEntityType).IsTypeAssignableFrom(entryType.GetType())
				? (entryType as IEdmEntityType).Properties().ToList()
				: (entryType as IEdmComplexType).Properties().ToList();

			string findMatchingPropertyName(string name)
			{
				var property = typeProperties.BestMatch(y => y.Name, name, _session.Settings.NameMatchResolver);
				return property != null ? property.Name : name;
			}

			IEdmTypeReference findMatchingPropertyType(string name)
			{
				var property = typeProperties.BestMatch(y => y.Name, name, _session.Settings.NameMatchResolver);
				return property?.Type;
			}

			bool isStructural(IEdmTypeReference type) =>
				type != null && type.TypeKind() == EdmTypeKind.Complex;
			bool isStructuralCollection(IEdmTypeReference type) =>
				type != null && type.TypeKind() == EdmTypeKind.Collection && type.AsCollection().ElementType().TypeKind() == EdmTypeKind.Complex;
			bool isPrimitive(IEdmTypeReference type) =>
				!isStructural(type) && !isStructuralCollection(type);

			var resourceEntry = new ResourceProperties(entry);
			entry.Properties = properties
				.Where(x => isPrimitive(findMatchingPropertyType(x.Key)))
				.Select(x => new ODataProperty
				{
					Name = findMatchingPropertyName(x.Key),
					Value = GetPropertyValue(typeProperties, x.Key, x.Value, root)
				}).ToList();
			resourceEntry.CollectionProperties = properties
				.Where(x => isStructuralCollection(findMatchingPropertyType(x.Key)))
				.Select(x => new KeyValuePair<string, ODataCollectionValue>(
					findMatchingPropertyName(x.Key),
					GetPropertyValue(typeProperties, x.Key, x.Value, root) as ODataCollectionValue))
				.ToDictionary();
			resourceEntry.StructuralProperties = properties
				.Where(x => isStructural(findMatchingPropertyType(x.Key)))
				.Select(x => new KeyValuePair<string, ODataResource>(
					findMatchingPropertyName(x.Key),
					GetPropertyValue(typeProperties, x.Key, x.Value, root) as ODataResource))
				.ToDictionary();
			_resourceEntryMap.Add(entry, resourceEntry);
			if (root != null && _resourceEntries.TryGetValue(root, out var entries))
			{
				entries.Add(entry);
			}

			return entry;
		}

		private object GetPropertyValue(IEnumerable<IEdmProperty> properties, string key, object value, ODataResource root)
		{
			var property = properties.BestMatch(x => x.Name, key, _session.Settings.NameMatchResolver);
			return property != null ? GetPropertyValue(property.Type, value, root) : value;
		}

		private object GetPropertyValue(IEdmTypeReference propertyType, object value, ODataResource root)
		{
			if (value == null)
			{
				return value;
			}

			switch (propertyType.TypeKind())
			{
				case EdmTypeKind.Complex:
					if (Converter.HasObjectConverter(value.GetType()))
					{
						return Converter.Convert(value, value.GetType());
					}

					return CreateODataEntry(propertyType.FullName(), value.ToDictionary(TypeCache), root);

				case EdmTypeKind.Collection:
					var collection = propertyType.AsCollection();
					return new ODataCollectionValue
					{
						TypeName = propertyType.FullName(),
						Items = ((IEnumerable)value).Cast<object>().Select(x => GetPropertyValue(collection.ElementType(), x, root)),
					};

				case EdmTypeKind.Primitive:
					var mappedTypes = _typeMap.Where(x => x.Value == ((IEdmPrimitiveType)propertyType.Definition).PrimitiveKind);
					if (mappedTypes.Any())
					{
						foreach (var mappedType in mappedTypes)
						{
							if (TryConvert(value, mappedType.Key, out var result))
							{
								return result;
							}
							else if (TypeCache.TryConvert(value, mappedType.Key, out result))
							{
								return result;
							}
						}

						throw new NotSupportedException($"Conversion is not supported from type {value.GetType()} to OData type {propertyType}");
					}

					return value;

				case EdmTypeKind.Enum:
					return new ODataEnumValue(value.ToString());

				case EdmTypeKind.Untyped:
					return new ODataUntypedValue { RawValue = value.ToString() };

				case EdmTypeKind.None:
					if (Converter.HasObjectConverter(value.GetType()))
					{
						return Converter.Convert(value, value.GetType());
					}

					throw new NotSupportedException($"Conversion is not supported from type {value.GetType()} to OData type {propertyType}");

				default:
					return value;
			}
		}

		public static bool TryConvert(object value, Type targetType, out object? result)
		{
			try
			{
				if ((targetType == typeof(Date) || targetType == typeof(Date?)) && value is DateTimeOffset dto)
				{
					result = new Date(dto.Year, dto.Month, dto.Day);
					return true;
				}
				else if ((targetType == typeof(Date) || targetType == typeof(Date?)) && value is DateTime dt)
				{
					result = new Date(dt.Year, dt.Month, dt.Day);
					return true;
				}

				result = null;
				return false;
			}
			catch (Exception)
			{
				result = null;
				return false;
			}
		}

		private static readonly Dictionary<Type, EdmPrimitiveTypeKind> _typeMap = new[]
			{
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(string), EdmPrimitiveTypeKind.String),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(bool), EdmPrimitiveTypeKind.Boolean),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(bool?), EdmPrimitiveTypeKind.Boolean),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(byte), EdmPrimitiveTypeKind.Byte),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(byte?), EdmPrimitiveTypeKind.Byte),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(decimal), EdmPrimitiveTypeKind.Decimal),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(decimal?), EdmPrimitiveTypeKind.Decimal),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(double), EdmPrimitiveTypeKind.Double),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(double?), EdmPrimitiveTypeKind.Double),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Guid), EdmPrimitiveTypeKind.Guid),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Guid?), EdmPrimitiveTypeKind.Guid),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(short), EdmPrimitiveTypeKind.Int16),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(short?), EdmPrimitiveTypeKind.Int16),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(int), EdmPrimitiveTypeKind.Int32),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(int?), EdmPrimitiveTypeKind.Int32),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(long), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(long?), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(sbyte), EdmPrimitiveTypeKind.SByte),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(sbyte?), EdmPrimitiveTypeKind.SByte),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(float), EdmPrimitiveTypeKind.Single),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(float?), EdmPrimitiveTypeKind.Single),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(byte[]), EdmPrimitiveTypeKind.Binary),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Stream), EdmPrimitiveTypeKind.Stream),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Geography), EdmPrimitiveTypeKind.Geography),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyPoint), EdmPrimitiveTypeKind.GeographyPoint),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyLineString), EdmPrimitiveTypeKind.GeographyLineString),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyPolygon), EdmPrimitiveTypeKind.GeographyPolygon),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyCollection), EdmPrimitiveTypeKind.GeographyCollection),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyMultiLineString), EdmPrimitiveTypeKind.GeographyMultiLineString),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyMultiPoint), EdmPrimitiveTypeKind.GeographyMultiPoint),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeographyMultiPolygon), EdmPrimitiveTypeKind.GeographyMultiPolygon),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Geometry), EdmPrimitiveTypeKind.Geometry),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryPoint), EdmPrimitiveTypeKind.GeometryPoint),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryLineString), EdmPrimitiveTypeKind.GeometryLineString),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryPolygon), EdmPrimitiveTypeKind.GeometryPolygon),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryCollection), EdmPrimitiveTypeKind.GeometryCollection),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryMultiLineString), EdmPrimitiveTypeKind.GeometryMultiLineString),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryMultiPoint), EdmPrimitiveTypeKind.GeometryMultiPoint),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(GeometryMultiPolygon), EdmPrimitiveTypeKind.GeometryMultiPolygon),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(DateTimeOffset), EdmPrimitiveTypeKind.DateTimeOffset),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(DateTimeOffset?), EdmPrimitiveTypeKind.DateTimeOffset),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(TimeSpan), EdmPrimitiveTypeKind.Duration),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(TimeSpan?), EdmPrimitiveTypeKind.Duration),

				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(XElement), EdmPrimitiveTypeKind.String),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(ushort), EdmPrimitiveTypeKind.Int32),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(ushort?), EdmPrimitiveTypeKind.Int32),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(uint), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(uint?), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(ulong), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(ulong?), EdmPrimitiveTypeKind.Int64),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(char[]), EdmPrimitiveTypeKind.String),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(char), EdmPrimitiveTypeKind.String),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(char?), EdmPrimitiveTypeKind.String),

				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Date), EdmPrimitiveTypeKind.Date),
				new KeyValuePair<Type, EdmPrimitiveTypeKind>(typeof(Date?), EdmPrimitiveTypeKind.Date),
			}
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}
}
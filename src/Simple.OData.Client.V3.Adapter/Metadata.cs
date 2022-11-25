using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library.Values;

namespace Simple.OData.Client.V3.Adapter
{
	public class Metadata : MetadataBase
	{
		private readonly IEdmModel _model;

		public Metadata(IEdmModel model, INameMatchResolver nameMatchResolver, bool ignoreUnmappedProperties, bool unqualifiedNameCall) : base(nameMatchResolver, ignoreUnmappedProperties, unqualifiedNameCall)
		{
			_model = model;
		}

		public override string GetEntityCollectionExactName(string collectionName)
		{
			if (TryGetEntitySet(collectionName, out var entitySet))
			{
				return entitySet.Name;
			}
			else if (TryGetEntityType(collectionName, out var entityType))
			{
				return entityType.Name;
			}

			throw new UnresolvableObjectException(collectionName, $"Entity collection [{collectionName}] not found");
		}

		public override bool EntityCollectionRequiresOptimisticConcurrencyCheck(string collectionName)
		{
			return GetEntityType(collectionName).StructuralProperties()
				.Any(x => x.ConcurrencyMode == EdmConcurrencyMode.Fixed);
		}

		public override string GetDerivedEntityTypeExactName(string collectionName, string entityTypeName)
		{
			IEdmEntityType entityType;
			if (TryGetEntitySet(collectionName, out var entitySet))
			{
				entityType = (_model.FindAllDerivedTypes(entitySet.ElementType)
					.BestMatch(x => (x as IEdmEntityType).Name, entityTypeName, NameMatchResolver) as IEdmEntityType);
				if (entityType != null)
				{
					return entityType.Name;
				}
			}
			else if (TryGetEntityType(entityTypeName, out entityType))
			{
				return entityType.Name;
			}

			throw new UnresolvableObjectException(entityTypeName, $"Entity type [{entityTypeName}] not found");
		}

		public override string GetEntityTypeExactName(string collectionName)
		{
			var entityType = GetEntityTypes().BestMatch(x => x.Name, collectionName, NameMatchResolver);
			if (entityType != null)
			{
				return entityType.Name;
			}

			throw new UnresolvableObjectException(collectionName, $"Entity type [{collectionName}] not found");
		}

		public override string GetLinkedCollectionName(string instanceTypeName, string typeName, out bool isSingleton)
		{
			isSingleton = false;

			if (TryGetEntitySet(instanceTypeName, out var entitySet))
			{
				return entitySet.Name;
			}

			if (TryGetEntitySet(typeName, out entitySet))
			{
				return entitySet.Name;
			}

			if (TryGetEntityType(instanceTypeName, out var entityType))
			{
				return entityType.Name;
			}

			if (TryGetEntityType(typeName, out entityType))
			{
				return entityType.Name;
			}

			throw new UnresolvableObjectException(typeName, $"Linked collection for type [{typeName}] not found");
		}

		public override string GetQualifiedTypeName(string typeName)
		{
			if (TryGetEntityType(typeName, out var entityType))
			{
				return string.Join(".", entityType.Namespace, entityType.Name);
			}

			if (TryGetComplexType(typeName, out var complexType))
			{
				return string.Join(".", complexType.Namespace, complexType.Name);
			}

			if (TryGetEnumType(typeName, out var enumType))
			{
				return string.Join(".", enumType.Namespace, enumType.Name);
			}

			throw new UnresolvableObjectException(typeName, $"Type [{typeName}] not found");
		}

		public override bool IsOpenType(string collectionName)
		{
			return GetEntityType(collectionName).IsOpen;
		}

		public override bool IsTypeWithId(string collectionName)
		{
			if (TryGetEntityType(collectionName, out var entityType))
			{
				return entityType.DeclaredKey != null;
			}
			else
			{
				return false;
			}
		}

		public override IEnumerable<string> GetStructuralPropertyNames(string collectionName)
		{
			return GetEntityType(collectionName).StructuralProperties().Select(x => x.Name);
		}

		public override bool HasStructuralProperty(string collectionName, string propertyName)
		{
			return GetEntityType(collectionName).StructuralProperties().Any(x => NameMatchResolver.IsMatch(x.Name, propertyName));
		}

		public override string GetStructuralPropertyExactName(string collectionName, string propertyName)
		{
			return GetStructuralProperty(collectionName, propertyName).Name;
		}

		public override string GetStructuralPropertyPath(string collectionName, params string[]? propertyNames)
		{
			if (propertyNames == null || propertyNames.Length == 0)
			{
				throw new ArgumentNullException(nameof(propertyNames));
			}

			var property = GetStructuralProperty(collectionName, propertyNames[0]);
			var exactNames = new List<string> { property.Name };

			for (var i = 1; i < propertyNames.Length; i++)
			{
				var entityType = GetComplexType(property.Type.FullName());
				property = GetStructuralProperty(entityType, propertyNames[i]);
				exactNames.Add(property.Name);

				if (property.Type.IsPrimitive())
				{
					break;
				}
			}

			return string.Join("/", exactNames.ToArray());
		}

		public override bool HasNavigationProperty(string collectionName, string propertyName)
		{
			return GetEntityType(collectionName).NavigationProperties().Any(x => NameMatchResolver.IsMatch(x.Name, propertyName));
		}

		public override string GetNavigationPropertyExactName(string collectionName, string propertyName)
		{
			return GetNavigationProperty(collectionName, propertyName).Name;
		}

		public override string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName)
		{
			var navigationProperty = GetNavigationProperty(collectionName, propertyName);
			if (!TryGetEntityType(navigationProperty.Type, out var entityType))
			{
				throw new UnresolvableObjectException(propertyName, $"No association found for [{propertyName}].");
			}

			return entityType.Name;
		}

		public override bool IsNavigationPropertyCollection(string collectionName, string propertyName)
		{
			var property = GetNavigationProperty(collectionName, propertyName);
			return property.Type.Definition.TypeKind == EdmTypeKind.Collection;
		}

		public override IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName)
		{
			var entityType = GetEntityType(collectionName);
			while (entityType.DeclaredKey == null && entityType.BaseEntityType() != null)
			{
				entityType = entityType.BaseEntityType();
			}

			if (entityType.DeclaredKey == null)
			{
				return Array.Empty<string>();
			}

			return entityType.DeclaredKey.Select(x => x.Name);
		}

		public override IEnumerable<string> GetNavigationPropertyNames(string collectionName)
		{
			return GetEntityType(collectionName).NavigationProperties().Select(x => x.Name);
		}

		/// <summary>
		/// Gets a collection of key name collections that represent the alternate keys of the given entity.
		/// As alternate keys are not supported on V3, this method will always return an empty enumeration.
		/// </summary>
		/// <param name="collectionName">The collection name of the entity</param>
		/// <returns>An empty enumeration of string enumerations representing the key names</returns>
		public override IEnumerable<IEnumerable<string>> GetAlternateKeyPropertyNames(string collectionName)
		{
			return Enumerable.Empty<IEnumerable<string>>();
		}

		public override string GetFunctionFullName(string functionName)
		{
			var function = GetFunction(functionName);
			return function.Name;
		}

		public override EntityCollection? GetFunctionReturnCollection(string functionName)
		{
			var function = GetFunction(functionName);

			if (function.ReturnType == null)
			{
				return null;
			}

			return !TryGetEntityType(function.ReturnType, out var entityType) ? null : new EntityCollection(entityType.Name);
		}

		public override string GetFunctionVerb(string functionName)
		{
			var function = GetFunction(functionName);
			var annotation = _model.GetAnnotationValue(function, ODataNamespace.Metadata, "HttpMethod");
			return annotation is EdmStringConstant constant ? constant.Value : RestVerbs.Get;
		}

		public override string GetActionFullName(string actionName)
		{
			return GetFunctionFullName(actionName);
		}

		public override EntityCollection? GetActionReturnCollection(string actionName)
		{
			return GetFunctionReturnCollection(actionName);
		}

		private IEnumerable<IEdmEntitySet> GetEntitySets()
		{
			return _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
				.SelectMany(x => (x as IEdmEntityContainer).EntitySets());
		}

		private bool TryGetEntitySet(string entitySetName, out IEdmEntitySet entitySet)
		{
			if (entitySetName.Contains("/"))
			{
				entitySetName = entitySetName.Split('/').First();
			}

			entitySet = _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
				.SelectMany(x => (x as IEdmEntityContainer).EntitySets())
				.BestMatch(x => x.Name, entitySetName, NameMatchResolver);

			return entitySet != null;
		}

		private IEnumerable<IEdmEntityType> GetEntityTypes()
		{
			return _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Entity)
				.Select(x => x as IEdmEntityType);
		}

		private IEdmEntityType GetEntityType(string collectionName)
		{
			if (TryGetEntityType(collectionName, out var entityType))
			{
				return entityType;
			}

			throw new UnresolvableObjectException(collectionName, $"Entity type [{collectionName}] not found");
		}

		private bool TryGetEntityType(string collectionName, out IEdmEntityType? entityType)
		{
			entityType = null;
			if (collectionName.Contains("/"))
			{
				var segments = GetCollectionPathSegments(collectionName);

				if (SegmentsIncludeTypeSpecification(segments))
				{
					var derivedTypeName = segments.Last();
					var derivedType = GetEntityTypes().SingleOrDefault(x => x.FullName() == derivedTypeName);
					if (derivedType != null)
					{
						entityType = derivedType;
						return true;
					}
				}
				else
				{
					var collection = NavigateToCollection(collectionName);
					entityType = GetEntityTypes().SingleOrDefault(x => x.Name == collection.Name);
					if (entityType != null)
					{
						return true;
					}
				}
			}
			else
			{
				var entitySet = GetEntitySets().BestMatch(x => x.Name, collectionName, NameMatchResolver);
				if (entitySet != null)
				{
					entityType = entitySet.ElementType;
					return true;
				}

				var derivedType = GetEntityTypes().BestMatch(x => x.Name, collectionName, NameMatchResolver);
				if (derivedType != null)
				{
					var baseType = GetEntityTypes()
						.SingleOrDefault(x => _model.FindDirectlyDerivedTypes(x).Contains(derivedType));
					if (baseType != null && GetEntitySets().Any(x => x.ElementType == baseType))
					{
						entityType = derivedType;
						return true;
					}
					// Check if we can return it anyway
					entityType = derivedType;
					return true;
				}
			}

			return false;
		}

		private static bool TryGetEntityType(IEdmTypeReference typeReference, out IEdmEntityType entityType)
		{
			entityType = typeReference.Definition.TypeKind == EdmTypeKind.Collection
				? (typeReference.Definition as IEdmCollectionType).ElementType.Definition as IEdmEntityType
				: typeReference.Definition.TypeKind == EdmTypeKind.Entity
				? typeReference.Definition as IEdmEntityType
				: null;
			return entityType != null;
		}

		private IEdmComplexType GetComplexType(string typeName)
		{
			if (TryGetComplexType(typeName, out var complexType))
			{
				return complexType;
			}

			throw new UnresolvableObjectException(typeName, $"ComplexType [{typeName}] not found");
		}

		private bool TryGetComplexType(string typeName, out IEdmComplexType complexType)
		{
			complexType = _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Complex)
				.Select(x => x as IEdmComplexType)
				.BestMatch(x => x.Name, typeName, NameMatchResolver);

			return complexType != null;
		}

		private bool TryGetEnumType(string typeName, out IEdmEnumType enumType)
		{
			enumType = _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Enum)
				.Select(x => x as IEdmEnumType)
				.BestMatch(x => x.Name, typeName, NameMatchResolver);

			return enumType != null;
		}

		private IEdmStructuralProperty GetStructuralProperty(string entitySetName, string propertyName)
		{
			var edmType = GetEntityType(entitySetName);
			return GetStructuralProperty(edmType, propertyName);
		}

		private IEdmStructuralProperty GetStructuralProperty(IEdmStructuredType edmType, string propertyName)
		{
			var property = edmType.StructuralProperties().BestMatch(x => x.Name, propertyName, NameMatchResolver);

			if (property == null)
			{
				throw new UnresolvableObjectException(propertyName, $"Structural property [{propertyName}] not found");
			}

			return property;
		}

		private IEdmNavigationProperty GetNavigationProperty(string entitySetName, string propertyName)
		{
			var property = GetEntityType(entitySetName).NavigationProperties().BestMatch(x => x.Name, propertyName, NameMatchResolver);

			if (property == null)
			{
				throw new UnresolvableObjectException(propertyName, $"Navigation property [{propertyName}] not found");
			}

			return property;
		}

		private IEdmFunctionImport GetFunction(string functionName)
		{
			var function = _model.SchemaElements
				.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
				.SelectMany(x => (x as IEdmEntityContainer).FunctionImports())
				.BestMatch(x => x.Name, functionName, NameMatchResolver);

			if (function == null)
			{
				throw new UnresolvableObjectException(functionName, $"Function [{functionName}] not found");
			}

			return function;
		}
	}
}
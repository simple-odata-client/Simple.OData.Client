using System.Net;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Simple.OData.Client.V4.Adapter
{
	public class ResponseReader(ISession session, IEdmModel model) : ResponseReaderBase(session)
	{
		private readonly IEdmModel _model = model;

		public override Task<ODataResponse> GetResponseAsync(HttpResponseMessage responseMessage)
		{
			return GetResponseAsync(new ODataResponseMessage(responseMessage));
		}

		public async Task<ODataResponse> GetResponseAsync(IODataResponseMessageAsync responseMessage)
		{
			if (responseMessage.StatusCode == (int)HttpStatusCode.NoContent)
			{
				return ODataResponse.FromStatusCode(TypeCache, responseMessage.StatusCode, responseMessage.Headers);
			}

			var readerSettings = _session.ToReaderSettings();
			using var messageReader = new ODataMessageReader(responseMessage, readerSettings, _model);
			var payloadKind = messageReader.DetectPayloadKind().ToList();
			if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Error))
			{
				return ODataResponse.FromStatusCode(TypeCache, responseMessage.StatusCode, responseMessage.Headers);
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Value))
			{
				if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Collection))
				{
					throw new NotImplementedException();
				}
				else
				{
					return ODataResponse.FromValueStream(TypeCache, await responseMessage.GetStreamAsync().ConfigureAwait(false), responseMessage is ODataBatchOperationResponseMessage);
				}
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Batch))
			{
				return await ReadResponse(messageReader.CreateODataBatchReader()).ConfigureAwait(false);
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.ResourceSet))
			{
				return ReadResponse(messageReader.CreateODataResourceSetReader(), responseMessage);
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Collection))
			{
				return ReadResponse(messageReader.CreateODataCollectionReader());
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Property))
			{
				if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Resource))
				{
					return ReadResponse(messageReader.CreateODataResourceReader(), responseMessage);
				}
				else
				{
					var property = messageReader.ReadProperty();
					return ODataResponse.FromProperty(TypeCache, property.Name, GetPropertyValue(property.Value));
				}
			}
			else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Delta))
			{
				if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Resource))
				{
					return ReadResponse(messageReader.CreateODataResourceReader(), responseMessage);
				}
				else
				{
					return ReadResponse(messageReader.CreateODataDeltaResourceSetReader(), responseMessage);
				}
			}
			else
			{
				return ReadResponse(messageReader.CreateODataResourceReader(), responseMessage);
			}
		}

		private async Task<ODataResponse> ReadResponse(ODataBatchReader odataReader)
		{
			var batch = new List<ODataResponse>();

			while (odataReader.Read())
			{
				switch (odataReader.State)
				{
					case ODataBatchReaderState.ChangesetStart:
						break;

					case ODataBatchReaderState.Operation:
						var operationMessage = odataReader.CreateOperationResponseMessage();
						if (operationMessage.StatusCode == (int)HttpStatusCode.NoContent)
						{
							batch.Add(ODataResponse.FromStatusCode(TypeCache, operationMessage.StatusCode, operationMessage.Headers));
						}
						else if (operationMessage.StatusCode >= (int)HttpStatusCode.BadRequest)
						{
							batch.Add(ODataResponse.FromStatusCode(TypeCache,
								operationMessage.StatusCode,
								operationMessage.Headers,
								await operationMessage.GetStreamAsync().ConfigureAwait(false),
								_session.Settings.WebRequestExceptionMessageSource));
						}
						else
						{
							batch.Add(await GetResponseAsync(operationMessage).ConfigureAwait(false));
						}

						break;

					case ODataBatchReaderState.ChangesetEnd:
						break;
				}
			}

			return ODataResponse.FromBatch(TypeCache, batch);
		}

		private ODataResponse ReadResponse(ODataCollectionReader odataReader)
		{
			var collection = new List<object>();

			while (odataReader.Read())
			{
				if (odataReader.State == ODataCollectionReaderState.Completed)
				{
					break;
				}

				switch (odataReader.State)
				{
					case ODataCollectionReaderState.CollectionStart:
						break;

					case ODataCollectionReaderState.Value:
						collection.Add(GetPropertyValue(odataReader.Item));
						break;

					case ODataCollectionReaderState.CollectionEnd:
						break;
				}
			}

			return ODataResponse.FromCollection(TypeCache, collection);
		}

		private ODataResponse ReadResponse(ODataReader odataReader, IODataResponseMessageAsync responseMessage)
		{
			ResponseNode? rootNode = null;
			var nodeStack = new Stack<ResponseNode>();

			while (odataReader.Read())
			{
				if (odataReader.State == ODataReaderState.Completed)
				{
					break;
				}

				switch (odataReader.State)
				{
					case ODataReaderState.ResourceSetStart:
					case ODataReaderState.DeltaResourceSetStart:
						StartFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataResourceSetBase));
						break;

					case ODataReaderState.ResourceSetEnd:
					case ODataReaderState.DeltaResourceSetEnd:
						EndFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataResourceSetBase), ref rootNode);
						break;

					case ODataReaderState.ResourceStart:
						StartEntry(nodeStack);
						break;

					case ODataReaderState.ResourceEnd:
						EndEntry(nodeStack, ref rootNode, odataReader.Item);
						break;

					case ODataReaderState.NestedResourceInfoStart:
						StartNavigationLink(nodeStack, (odataReader.Item as ODataNestedResourceInfo).Name);
						break;

					case ODataReaderState.NestedResourceInfoEnd:
						EndNavigationLink(nodeStack);
						break;
				}
			}

			return ODataResponse.FromNode(TypeCache, rootNode, responseMessage.Headers);
		}

		protected override void ConvertEntry(ResponseNode entryNode, object entry)
		{
			if (entry is not null)
			{
				var odataEntry = entry as ODataResource;
				foreach (var property in odataEntry.Properties)
				{
					entryNode.Entry.Data.Add(property.Name, GetPropertyValue(((ODataProperty)property).Value));
				}

				entryNode.Entry.SetAnnotations(CreateAnnotations(odataEntry));
			}
		}

		private static ODataFeedAnnotations CreateAnnotations(ODataResourceSetBase feed)
		{
			return new ODataFeedAnnotations()
			{
				Id = feed.Id?.AbsoluteUri,
				Count = feed.Count,
				DeltaLink = feed.DeltaLink,
				NextPageLink = feed.NextPageLink,
				InstanceAnnotations = feed.InstanceAnnotations,
			};
		}

		private ODataEntryAnnotations CreateAnnotations(ODataResource odataEntry)
		{
			string? id = null;
			Uri? readLink = null;
			Uri? editLink = null;
			string? etag = null;
			if (_session.Adapter.GetMetadata().IsTypeWithId(odataEntry.TypeName))
			{
				try
				{
					// odataEntry.Id is null for transient entities (s. http://docs.oasis-open.org/odata/odata-json-format/v4.0/errata03/os/odata-json-format-v4.0-errata03-os-complete.html#_Toc453766634)
					id = odataEntry.Id?.AbsoluteUri;
					readLink = odataEntry.ReadLink;
					editLink = odataEntry.EditLink;
					etag = odataEntry.ETag;
				}
				catch (ODataException)
				{
					// Ignored
				}
			}

			return new ODataEntryAnnotations
			{
				Id = id,
				TypeName = odataEntry.TypeName,
				ReadLink = readLink,
				EditLink = editLink,
				ETag = etag,
				MediaResource = CreateAnnotations(odataEntry.MediaResource),
				InstanceAnnotations = odataEntry.InstanceAnnotations,
			};
		}

		private static ODataMediaAnnotations? CreateAnnotations(ODataStreamReferenceValue value)
		{
			return value is null ? null : new ODataMediaAnnotations
			{
				ContentType = value.ContentType,
				ReadLink = value.ReadLink,
				EditLink = value.EditLink,
				ETag = value.ETag,
			};
		}

		private object GetPropertyValue(object value)
		{
			if (value is ODataResource resource)
			{
				return resource.Properties.ToDictionary(x => x.Name, x => GetPropertyValue(((ODataProperty)x).Value));
			}
			else if (value is ODataCollectionValue collectionValue)
			{
				return collectionValue.Items.Select(GetPropertyValue).ToList();
			}
			else if (value is ODataEnumValue enumValue)
			{
				return enumValue.Value;
			}
			else if (value is ODataUntypedValue untypedValue)
			{
				var result = untypedValue.RawValue;
				if (!string.IsNullOrEmpty(result))
				{
					// Remove extra quoting as has been read as a string
					// Don't just replace \" in case we have embedded quotes
					if (result.StartsWith("\"", StringComparison.Ordinal) && result.EndsWith("\"", StringComparison.Ordinal))
					{
						result = result.Substring(1, result.Length - 2);
					}
				}

				return result;
			}
			else if (value is ODataStreamReferenceValue referenceValue)
			{
				return CreateAnnotations(referenceValue);
			}
			else
			{
				return value;
			}
		}
	}
}

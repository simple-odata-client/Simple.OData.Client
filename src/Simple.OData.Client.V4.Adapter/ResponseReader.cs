using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OData;
using Microsoft.OData.Edm;

#pragma warning disable 1591

namespace Simple.OData.Client.V4.Adapter
{
    public class ResponseReader : ResponseReaderBase
    {
        private readonly IEdmModel _model;

        public ResponseReader(ISession session, IEdmModel model)
            : base(session)
        {
            _model = model;
        }

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
            using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, _model))
            {
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
                else
                {
                    return ReadResponse(messageReader.CreateODataResourceReader(), responseMessage);
                }
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
                            batch.Add(ODataResponse.FromStatusCode(TypeCache, operationMessage.StatusCode, operationMessage.Headers));
                        else if (operationMessage.StatusCode >= (int)HttpStatusCode.BadRequest)
                            batch.Add(ODataResponse.FromStatusCode(TypeCache, 
                                operationMessage.StatusCode,
                                operationMessage.Headers,
                                await operationMessage.GetStreamAsync().ConfigureAwait(false)));
                        else
                            batch.Add(await GetResponseAsync(operationMessage).ConfigureAwait(false));
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
                    break;

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
            ResponseNode rootNode = null;
            var nodeStack = new Stack<ResponseNode>();

            while (odataReader.Read())
            {
                if (odataReader.State == ODataReaderState.Completed)
                    break;

                switch (odataReader.State)
                {
                    case ODataReaderState.ResourceSetStart:
                        StartFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataResourceSet));
                        break;

                    case ODataReaderState.ResourceSetEnd:
                        EndFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataResourceSet), ref rootNode);
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
            if (entry != null)
            {
                var odataEntry = entry as ODataResource;
                foreach (var property in odataEntry.Properties)
                {
                    entryNode.Entry.Data.Add(property.Name, GetPropertyValue(property.Value));
                }
                entryNode.Entry.SetAnnotations(CreateAnnotations(odataEntry));
            }
        }

        private ODataFeedAnnotations CreateAnnotations(ODataResourceSet feed)
        {
            return new ODataFeedAnnotations()
            {
                Id = feed.Id == null ? null : feed.Id.AbsoluteUri,
                Count = feed.Count,
                DeltaLink = feed.DeltaLink,
                NextPageLink = feed.NextPageLink,
                InstanceAnnotations = feed.InstanceAnnotations,
            };
        }

        private ODataEntryAnnotations CreateAnnotations(ODataResource odataEntry)
        {
            string id = null;
            Uri readLink = null;
            Uri editLink = null;
            if (_session.Adapter.GetMetadata().IsTypeWithId(odataEntry.TypeName))
            {
                try
                {
                    id = odataEntry.Id.AbsoluteUri;
                    readLink = odataEntry.ReadLink;
                    editLink = odataEntry.EditLink;
                }
                catch (ODataException)
                {
                    // Ingored
                }
            }

            return new ODataEntryAnnotations
            {
                Id = id,
                TypeName = odataEntry.TypeName,
                ReadLink = readLink,
                EditLink = editLink,
                ETag = odataEntry.ETag,
                MediaResource = CreateAnnotations(odataEntry.MediaResource),
                InstanceAnnotations = odataEntry.InstanceAnnotations,
            };
        }

        private ODataMediaAnnotations CreateAnnotations(ODataStreamReferenceValue value)
        {
            return value == null ? null : new ODataMediaAnnotations
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
                return resource.Properties.ToDictionary(x => x.Name, x => GetPropertyValue(x.Value));
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
                    if (result.StartsWith("\"") && result.EndsWith("\""))
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

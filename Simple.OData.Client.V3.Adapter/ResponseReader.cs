using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Simple.OData.Client.V3.Adapter
{
    public class ResponseReader : ResponseReaderBase
    {
        private readonly IEdmModel _model;
        private bool _hasResponse = false;

        public ResponseReader(ISession session, IEdmModel model)
            : base(session)
        {
            _model = model;
        }

        public override Task<ODataResponse> GetResponseAsync(HttpResponseMessage responseMessage)
        {
            return GetResponseAsync(new ODataResponseMessage(responseMessage));
        }

#if SILVERLIGHT
        public async Task<ODataResponse> GetResponseAsync(IODataResponseMessage responseMessage)
#else

        public async Task<ODataResponse> GetResponseAsync(IODataResponseMessageAsync responseMessage)
#endif
        {
            var readerSettings = new ODataMessageReaderSettings();
            if (_session.Settings.IgnoreUnmappedProperties)
                readerSettings.UndeclaredPropertyBehaviorKinds = ODataUndeclaredPropertyBehaviorKinds.IgnoreUndeclaredValueProperty;
            readerSettings.MessageQuotas.MaxReceivedMessageSize = Int32.MaxValue;
            readerSettings.ShouldIncludeAnnotation = x => _session.Settings.IncludeAnnotationsInResults;
            using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, _model))
            {
                var payloadKind = messageReader.DetectPayloadKind();
                if (payloadKind.Any(x => x.PayloadKind != ODataPayloadKind.Property))
                    _hasResponse = true;

                if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Error))
                {
                    return ODataResponse.FromStatusCode(responseMessage.StatusCode);
                }
                else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Value))
                {
                    if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Collection))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
#if SILVERLIGHT
                        var stream = responseMessage.GetStream();
#else
                        var stream = await responseMessage.GetStreamAsync().ConfigureAwait(false);
#endif
                        return ODataResponse.FromValueStream(stream, responseMessage is ODataBatchOperationResponseMessage);
                    }
                }
                else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Batch))
                {
                    return await ReadResponse(messageReader.CreateODataBatchReader()).ConfigureAwait(false);
                }
                else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Feed))
                {
                    return ReadResponse(messageReader.CreateODataFeedReader());
                }
                else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Collection))
                {
                    return ReadResponse(messageReader.CreateODataCollectionReader());
                }
                else if (payloadKind.Any(x => x.PayloadKind == ODataPayloadKind.Property))
                {
                    var property = messageReader.ReadProperty();
                    if (property.Value != null && (property.Value.GetType() != typeof(string) || !string.IsNullOrEmpty(property.Value.ToString())))
                        _hasResponse = true;

                    if (_hasResponse)
                    {
                        return ODataResponse.FromProperty(property.Name, GetPropertyValue(property.Value));
                    }
                    else
                    {
                        return ODataResponse.EmptyFeed;
                    }
                }
                else
                {
                    return ReadResponse(messageReader.CreateODataEntryReader());
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
                            batch.Add(ODataResponse.FromStatusCode(operationMessage.StatusCode));
                        else if (operationMessage.StatusCode >= (int)HttpStatusCode.BadRequest)
                            batch.Add(ODataResponse.FromStatusCode(
                                operationMessage.StatusCode,
#if SILVERLIGHT
                                operationMessage.GetStream()));
#else
                                await operationMessage.GetStreamAsync().ConfigureAwait(false)));
#endif
                        else
                            batch.Add(await GetResponseAsync(operationMessage).ConfigureAwait(false));
                        break;

                    case ODataBatchReaderState.ChangesetEnd:
                        break;
                }
            }

            return ODataResponse.FromBatch(batch);
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

            return ODataResponse.FromCollection(collection);
        }

        private ODataResponse ReadResponse(ODataReader odataReader)
        {
            ResponseNode rootNode = null;
            var nodeStack = new Stack<ResponseNode>();

            while (odataReader.Read())
            {
                if (odataReader.State == ODataReaderState.Completed)
                    break;

                switch (odataReader.State)
                {
                    case ODataReaderState.FeedStart:
                        StartFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataFeed));
                        break;

                    case ODataReaderState.FeedEnd:
                        EndFeed(nodeStack, CreateAnnotations(odataReader.Item as ODataFeed), ref rootNode);
                        break;

                    case ODataReaderState.EntryStart:
                        StartEntry(nodeStack);
                        break;

                    case ODataReaderState.EntryEnd:
                        EndEntry(nodeStack, ref rootNode, odataReader.Item);
                        break;

                    case ODataReaderState.NavigationLinkStart:
                        StartNavigationLink(nodeStack, (odataReader.Item as ODataNavigationLink).Name);
                        break;

                    case ODataReaderState.NavigationLinkEnd:
                        EndNavigationLink(nodeStack);
                        break;
                }
            }

            return ODataResponse.FromNode(rootNode);
        }

        protected override void ConvertEntry(ResponseNode entryNode, object entry)
        {
            if (entry != null)
            {
                var odataEntry = entry as Microsoft.Data.OData.ODataEntry;
                foreach (var property in odataEntry.Properties)
                {
                    entryNode.Entry.Data.Add(property.Name, GetPropertyValue(property.Value));
                }
                entryNode.Entry.SetAnnotations(CreateAnnotations(odataEntry));
            }
        }

        private ODataFeedAnnotations CreateAnnotations(ODataFeed feed)
        {
            return new ODataFeedAnnotations()
            {
                Id = feed.Id,
                Count = feed.Count,
                DeltaLink = feed.DeltaLink,
                NextPageLink = feed.NextPageLink,
                InstanceAnnotations = feed.InstanceAnnotations,
            };
        }

        private ODataEntryAnnotations CreateAnnotations(Microsoft.Data.OData.ODataEntry odataEntry)
        {
            string id = null;
            Uri readLink = null;
            Uri editLink = null;
            if (_session.Adapter.GetMetadata().IsTypeWithId(odataEntry.TypeName))
            {
                try
                {
                    id = odataEntry.Id;
                    readLink = odataEntry.ReadLink;
                    editLink = odataEntry.EditLink;
                }
                catch (Exception)
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
                ETag = odataEntry.ETag,
                AssociationLinks = odataEntry.AssociationLinks == null
                    ? null
                    : new List<ODataEntryAnnotations.AssociationLink>(
                    odataEntry.AssociationLinks.Select(x => new ODataEntryAnnotations.AssociationLink
                    {
                        Name = x.Name,
                        Uri = x.Url,
                    })),
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
            if (value is ODataComplexValue)
            {
                return (value as ODataComplexValue).Properties.ToDictionary(
                    x => x.Name, x => GetPropertyValue(x.Value));
            }
            else if (value is ODataCollectionValue)
            {
                return (value as ODataCollectionValue).Items.Cast<object>()
                    .Select(GetPropertyValue).ToList();
            }
            else if (value is ODataStreamReferenceValue)
            {
                return CreateAnnotations(value as ODataStreamReferenceValue);
            }
            else
            {
                return value;
            }
        }
    }
}
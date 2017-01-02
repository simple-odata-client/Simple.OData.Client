using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.OData;

#pragma warning disable 1591

namespace Simple.OData.Client.V4.Adapter
{
    public class BatchWriter : BatchWriterBase
    {
        private ODataBatchWriter _batchWriter;
        private ODataRequestMessage _requestMessage;
        private ODataMessageWriter _messageWriter;

        public BatchWriter(ISession session, IDictionary<object, IDictionary<string, object>> batchEntries)
            : base(session, batchEntries)
        {
        }

        public override async Task StartBatchAsync()
        {
            _requestMessage = new ODataRequestMessage() { Url = _session.Settings.BaseUri };
            _messageWriter = new ODataMessageWriter(_requestMessage);
            _batchWriter = await _messageWriter.CreateODataBatchWriterAsync().ConfigureAwait(false);
            await _batchWriter.WriteStartBatchAsync().ConfigureAwait(false);
            this.HasOperations = true;
        }

        public override async Task<HttpRequestMessage> EndBatchAsync()
        {
            if (_pendingChangeSet)
                await _batchWriter.WriteEndChangesetAsync().ConfigureAwait(false);
            await _batchWriter.WriteEndBatchAsync().ConfigureAwait(false);
            var stream = await _requestMessage.GetStreamAsync().ConfigureAwait(false);
            return CreateMessageFromStream(stream, _requestMessage.Url, _requestMessage.GetHeader);
        }

        protected override async Task StartChangesetAsync()
        {
            if (_batchWriter == null)
                await StartBatchAsync().ConfigureAwait(false);

            await _batchWriter.WriteStartChangesetAsync().ConfigureAwait(false);
        }

        protected override Task EndChangesetAsync()
        {
            return _batchWriter.WriteEndChangesetAsync();
        }

        protected override async Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired)
        {
            if (_batchWriter == null)
                await StartBatchAsync().ConfigureAwait(false);

            return await CreateBatchOperationMessageAsync(uri, method, collection, contentId, resultRequired).ConfigureAwait(false);
        }

        private async Task<ODataBatchOperationRequestMessage> CreateBatchOperationMessageAsync(
            Uri uri, string method, string collection, string contentId, bool resultRequired)
        {
            var message = await _batchWriter.CreateOperationRequestMessageAsync(method, uri, contentId).ConfigureAwait(false);

            if (method == RestVerbs.Post || method == RestVerbs.Put || method == RestVerbs.Patch || method == RestVerbs.Merge)
                message.SetHeader(HttpLiteral.ContentId, contentId);

            if (method == RestVerbs.Post || method == RestVerbs.Put || method == RestVerbs.Patch || method == RestVerbs.Merge)
                message.SetHeader(HttpLiteral.Prefer, resultRequired ? HttpLiteral.ReturnRepresentation : HttpLiteral.ReturnMinimal);

            if (collection != null && _session.Metadata.EntityCollectionRequiresOptimisticConcurrencyCheck(collection) &&
                (method == RestVerbs.Put || method == RestVerbs.Patch || method == RestVerbs.Merge || method == RestVerbs.Delete))
            {
                message.SetHeader(HttpLiteral.IfMatch, EntityTagHeaderValue.Any.Tag);
            }

            return message;
        }
    }
}
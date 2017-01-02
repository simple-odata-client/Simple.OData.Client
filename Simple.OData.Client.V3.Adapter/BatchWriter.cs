using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Data.OData;

#pragma warning disable 1591

namespace Simple.OData.Client.V3.Adapter
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

#pragma warning disable 1998
        public override async Task StartBatchAsync()
        {
            _requestMessage = new ODataRequestMessage() { Url = _session.Settings.BaseUri };
            _messageWriter = new ODataMessageWriter(_requestMessage);
#if SILVERLIGHT
            _batchWriter = _messageWriter.CreateODataBatchWriter();
            _batchWriter.WriteStartBatch();
#else
            _batchWriter = await _messageWriter.CreateODataBatchWriterAsync().ConfigureAwait(false);
            await _batchWriter.WriteStartBatchAsync().ConfigureAwait(false);
#endif
            this.HasOperations = true;
        }
#pragma warning restore 1998

#pragma warning disable 1998
        public override async Task<HttpRequestMessage> EndBatchAsync()
        {
#if SILVERLIGHT
            if (_pendingChangeSet)
                _batchWriter.WriteEndChangeset();
            _batchWriter.WriteEndBatch();
            var stream = _requestMessage.GetStream();
#else
            if (_pendingChangeSet)
                await _batchWriter.WriteEndChangesetAsync().ConfigureAwait(false);
            await _batchWriter.WriteEndBatchAsync().ConfigureAwait(false);
            var stream = await _requestMessage.GetStreamAsync().ConfigureAwait(false);
#endif
            return CreateMessageFromStream(stream, _requestMessage.Url, _requestMessage.GetHeader);
        }
#pragma warning restore 1998

        protected override async Task StartChangesetAsync()
        {
            if (_batchWriter == null)
                await StartBatchAsync().ConfigureAwait(false);

#if SILVERLIGHT
            _batchWriter.WriteStartChangeset();
#else
            await _batchWriter.WriteStartChangesetAsync().ConfigureAwait(false);
#endif
        }

        protected override Task EndChangesetAsync()
        {
#if SILVERLIGHT
            _batchWriter.WriteEndChangeset();
            return Utils.GetTaskFromResult(0);
#else
            return _batchWriter.WriteEndChangesetAsync();
#endif
        }

        protected override async Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired)
        {
            if (_batchWriter == null)
                await StartBatchAsync().ConfigureAwait(false);

            return await CreateBatchOperationMessageAsync(uri, method, collection, contentId, resultRequired).ConfigureAwait(false);
        }

#pragma warning disable 1998
        private async Task<ODataBatchOperationRequestMessage> CreateBatchOperationMessageAsync(
            Uri uri, string method, string collection, string contentId, bool resultRequired)
        {
#if SILVERLIGHT
            var message = _batchWriter.CreateOperationRequestMessage(method, uri);
#else
            var message = await _batchWriter.CreateOperationRequestMessageAsync(method, uri).ConfigureAwait(false);
#endif

            if (method != RestVerbs.Get && method != RestVerbs.Delete)
                message.SetHeader(HttpLiteral.ContentId, contentId);

            if (method == RestVerbs.Post || method == RestVerbs.Put || method == RestVerbs.Patch || method == RestVerbs.Merge)
                message.SetHeader(HttpLiteral.Prefer, resultRequired ? HttpLiteral.ReturnContent : HttpLiteral.ReturnNoContent);

            if (collection != null && _session.Metadata.EntityCollectionRequiresOptimisticConcurrencyCheck(collection) &&
                (method == RestVerbs.Put || method == RestVerbs.Patch || method == RestVerbs.Merge || method == RestVerbs.Delete))
            {
                message.SetHeader(HttpLiteral.IfMatch, EntityTagHeaderValue.Any.Tag);
            }

            return message;
        }
#pragma warning restore 1998
    }
}

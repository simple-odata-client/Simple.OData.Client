using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public abstract class BatchWriterBase : IBatchWriter
    {
        protected readonly ISession _session;
        private int _lastOperationId;
        private readonly Dictionary<IDictionary<string, object>, string> _contentIdMap;
        protected bool _pendingChangeSet;

        protected BatchWriterBase(ISession session, IDictionary<object, IDictionary<string, object>> batchEntries)
        {
            _session = session;
            _lastOperationId = 0;
            _contentIdMap = new Dictionary<IDictionary<string, object>, string> ();
            this.BatchEntries = batchEntries;
        }

        public abstract Task StartBatchAsync();
        public abstract Task<HttpRequestMessage> EndBatchAsync();

        public async Task<ODataRequest> CreateBatchRequestAsync(
            IODataClient client, IList<Func<IODataClient, Task>> actions, IList<int> responseIndexes)
        {
            // Write batch operations into a batch content
            var lastOperationId = 0;
            foreach (var action in actions)
            {
                await action(client).ConfigureAwait(false);
                var responseIndex = -1;
                if (this.LastOperationId > lastOperationId)
                {
                    lastOperationId = LastOperationId;
                    responseIndex = lastOperationId - 1;
                }
                responseIndexes.Add(responseIndex);
            }

            if (this.HasOperations)
            {
                // Create batch request message
                var requestMessage = await EndBatchAsync().ConfigureAwait(false);
                return new ODataRequest(RestVerbs.Post, _session, ODataLiteral.Batch, requestMessage);
            }
            else
            {
                return null;
            }
        }
        
        protected abstract Task StartChangesetAsync();
        protected abstract Task EndChangesetAsync();
        protected abstract Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired);

        /// <summary>
        /// Creates operation message which allows for adding additional customer HTTP headers on per operation level
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <param name="collection"></param>
        /// <param name="contentId"></param>
        /// <param name="operationHeaders"></param>
        /// <param name="resultRequired"></param>
        /// <returns></returns>
        protected abstract Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, IDictionary<string,string> operationHeaders, bool resultRequired);

        public int LastOperationId => _lastOperationId;

        public string NextContentId()
        {
            return (++_lastOperationId).ToString();
        }

        public string GetContentId(IDictionary<string, object> entryData, object linkData)
        {
            if (!_contentIdMap.TryGetValue(entryData, out var contentId) && linkData != null)
            {
                if (this.BatchEntries.TryGetValue(linkData, out var mappedEntry))
                    _contentIdMap.TryGetValue(mappedEntry, out contentId);
            }
            return contentId;
        }

        public void MapContentId(IDictionary<string, object> entryData, string contentId)
        {
            if (entryData != null && !_contentIdMap.TryGetValue(entryData, out _))
            {
                _contentIdMap.Add(entryData, contentId);
            }
        }

        public IDictionary<object, IDictionary<string, object>> BatchEntries { get; private set; }

        public async Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, IDictionary<string, object> entryData, IDictionary<string, string> operationHeaders, bool resultRequired)
        {
            if (method != RestVerbs.Get && !_pendingChangeSet)
            {
                await StartChangesetAsync().ConfigureAwait(false);
                _pendingChangeSet = true;
            }
            else if (method == RestVerbs.Get && _pendingChangeSet)
            {
                await EndChangesetAsync().ConfigureAwait(false);
                _pendingChangeSet = false;
            }

            var contentId = NextContentId();
            if (method != RestVerbs.Get && method != RestVerbs.Delete)
            {
                MapContentId(entryData, contentId);
            }

            return await CreateOperationMessageAsync(uri, method, collection, contentId, operationHeaders, resultRequired).ConfigureAwait(false);
        }

        public bool HasOperations { get; protected set; }

        protected HttpRequestMessage CreateMessageFromStream(Stream stream, Uri requestUrl, Func<string, string> getHeaderFunc)
        {
            _pendingChangeSet = false;
            stream.Position = 0;

            var httpRequest = new HttpRequestMessage()
            {
                RequestUri = Utils.CreateAbsoluteUri(requestUrl.AbsoluteUri, ODataLiteral.Batch),
                Method = HttpMethod.Post,
                Content = new StreamContent(stream),
            };
            httpRequest.Content.Headers.Add(HttpLiteral.ContentType, getHeaderFunc(HttpLiteral.ContentType));
            return httpRequest;
        }
    }
}

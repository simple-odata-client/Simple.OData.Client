using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    internal class RequestBuilder : IRequestBuilder
    {
        private readonly ResolvedCommand _command;
        private readonly string _commandText;
        private readonly Session _session;
        private readonly Lazy<IBatchWriter> _lazyBatchWriter;

        public RequestBuilder(ResolvedCommand command, Session session, Lazy<IBatchWriter> batchWriter)
        {
            _command = command;
            _session = session;
            _lazyBatchWriter = batchWriter;
        }

        public RequestBuilder(string commandText, Session session, Lazy<IBatchWriter> batchWriter)
        {
            _commandText = commandText;
            _session = session;
            _lazyBatchWriter = batchWriter;
        }

        public async Task<ODataRequest> GetRequestAsync(bool scalarResult, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateGetRequestAsync(_commandText ?? _command.Format(), scalarResult, _command?.Details.Headers).ConfigureAwait(false);
        }

        public async Task<ODataRequest> InsertRequestAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var entryData = _command.CommandData;

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateInsertRequestAsync(_command.QualifiedEntityCollectionName, _command.Format(), entryData, resultRequired, _command?.Details.Headers).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UpdateRequestAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.Details.HasKey ? _command.KeyValues : _command.FilterAsKey;
            var entryData = _command.CommandData;
            var entryIdent = FormatEntryKey(_command);

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateUpdateRequestAsync(collectionName, entryIdent, entryKey, entryData, resultRequired, _command?.Details.Headers).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UpdateRequestAsync(Stream stream, string contentType, bool optimisticConcurrency, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreatePutRequestAsync(_commandText, stream, contentType, optimisticConcurrency, _command?.Details.Headers).ConfigureAwait(false);            
        }

        public async Task<ODataRequest> DeleteRequestAsync(CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryIdent = FormatEntryKey(_command);

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateDeleteRequestAsync(collectionName, entryIdent, _command?.Details.Headers).ConfigureAwait(false);
        }

        public async Task<ODataRequest> LinkRequestAsync(string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.Details.HasKey ? _command.KeyValues : _command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var linkedCollection = _session.Metadata.GetNavigationPropertyPartnerTypeName(collectionName, linkName);
            var linkIdent = await FormatEntryKeyAsync(linkedCollection, linkedEntryKey, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateLinkRequestAsync(collectionName, linkName, entryIdent, linkIdent, _command?.Details.Headers).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UnlinkRequestAsync(string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.Details.HasKey ? _command.KeyValues : _command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            string linkIdent = null;
            if (linkedEntryKey != null)
            {
                var linkedCollection = _session.Metadata.GetNavigationPropertyPartnerTypeName(collectionName, linkName);
                linkIdent = await FormatEntryKeyAsync(linkedCollection, linkedEntryKey, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
            }

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateUnlinkRequestAsync(collectionName, linkName, entryIdent, linkIdent, _command?.Details.Headers).ConfigureAwait(false);
        }

        private string FormatEntryKey(ResolvedCommand command)
        {
            var entryIdent = command.Details.HasKey
                ? command.Format() 
                : new FluentCommand(command).Key(command.FilterAsKey).Resolve(_session).Format();

            return entryIdent;
        }

        private async Task<string> FormatEntryKeyAsync(string collection, IDictionary<string, object> entryKey, CancellationToken cancellationToken)
        {
            var client = new BoundClient<IDictionary<string, object>>(new ODataClient(_session.Settings), _session);
            var entryIdent = await client
                .For(collection)
                .Key(entryKey)
                .GetCommandTextAsync(cancellationToken).ConfigureAwait(false);

            return entryIdent;
        }

        private void AssertHasKey(ResolvedCommand command)
        {
            if (!command.Details.HasKey && command.FilterAsKey == null)
                throw new InvalidOperationException("No entry key specified.");
        }
    }

    internal class RequestBuilder<T> : IRequestBuilder<T>
        where T : class
    {
        private readonly FluentCommand _command;
        private readonly Session _session;
        private readonly Lazy<IBatchWriter> _lazyBatchWriter;

        public RequestBuilder(FluentCommand command, Session session, Lazy<IBatchWriter> batchWriter)
        {
            _command = command;
            _session = session;
            _lazyBatchWriter = batchWriter;
        }

        public Task<IClientWithRequest<T>> FindEntriesAsync()
        {
            return FindEntriesAsync(false, CancellationToken.None);
        }

        public Task<IClientWithRequest<T>> FindEntriesAsync(CancellationToken cancellationToken)
        {
            return FindEntriesAsync(false, cancellationToken);
        }

        public Task<IClientWithRequest<T>> FindEntriesAsync(bool scalarResult)
        {
            return FindEntriesAsync(scalarResult, CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> FindEntriesAsync(bool scalarResult, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.GetRequestAsync(scalarResult, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> FindEntriesAsync(ODataFeedAnnotations annotations)
        {
            return FindEntriesAsync(annotations, CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> FindEntriesAsync(ODataFeedAnnotations annotations, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session).WithCount(), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.GetRequestAsync(false, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> FindEntryAsync()
        {
            return FindEntryAsync(CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> FindEntryAsync(CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.GetRequestAsync(false, cancellationToken), _session);
        }

        public Task<IClientWithRequest<T>> InsertEntryAsync()
        {
            return InsertEntryAsync(true, CancellationToken.None);
        }

        public Task<IClientWithRequest<T>> InsertEntryAsync(bool resultRequired)
        {
            return InsertEntryAsync(resultRequired, CancellationToken.None);
        }

        public Task<IClientWithRequest<T>> InsertEntryAsync(CancellationToken cancellationToken)
        {
            return InsertEntryAsync(true, cancellationToken);
        }

        public async Task<IClientWithRequest<T>> InsertEntryAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.InsertRequestAsync(resultRequired, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> UpdateEntryAsync()
        {
            return UpdateEntryAsync(true, CancellationToken.None);
        }

        public Task<IClientWithRequest<T>> UpdateEntryAsync(bool resultRequired)
        {
            return UpdateEntryAsync(resultRequired, CancellationToken.None);
        }

        public Task<IClientWithRequest<T>> UpdateEntryAsync(CancellationToken cancellationToken)
        {
            return UpdateEntryAsync(true, cancellationToken);
        }

        public async Task<IClientWithRequest<T>> UpdateEntryAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.UpdateRequestAsync(resultRequired, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> DeleteEntryAsync()
        {
            return DeleteEntryAsync(CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> DeleteEntryAsync(CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var requestBuilder = new RequestBuilder(_command.Resolve(_session), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.DeleteRequestAsync(cancellationToken).ConfigureAwait(false), _session);
        }
    }
}

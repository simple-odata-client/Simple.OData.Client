using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    internal class RequestBuilder : IRequestBuilder
    {
        private readonly FluentCommand _command;
        private readonly string _commandText;
        private readonly Session _session;
        private readonly Lazy<IBatchWriter> _lazyBatchWriter;

        public RequestBuilder(FluentCommand command, Session session, Lazy<IBatchWriter> batchWriter)
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

            var commandText = _commandText == null
                ? await _command.GetCommandTextAsync(cancellationToken).ConfigureAwait(false)
                : _commandText;
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateGetRequestAsync(commandText, scalarResult).ConfigureAwait(false);
        }

        public async Task<ODataRequest> InsertRequestAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var entryData = _command.CommandData;
            var commandText = await _command.GetCommandTextAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateInsertRequestAsync(_command.QualifiedEntityCollectionName, commandText, entryData, resultRequired).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UpdateRequestAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.HasKey ? _command.KeyValues : _command.FilterAsKey;
            var entryData = _command.CommandData;
            var entryIdent = await FormatEntryKeyAsync(_command, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateUpdateRequestAsync(collectionName, entryIdent, entryKey, entryData, resultRequired).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UpdateRequestAsync(Stream stream, string contentType, bool optimisticConcurrency, CancellationToken cancellationToken)
        {
            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreatePutRequestAsync(_commandText, stream, contentType, optimisticConcurrency).ConfigureAwait(false);            
        }

        public async Task<ODataRequest> DeleteRequestAsync(CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryIdent = await FormatEntryKeyAsync(_command, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateDeleteRequestAsync(collectionName, entryIdent).ConfigureAwait(false);
        }

        public async Task<ODataRequest> LinkRequestAsync(string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.HasKey ? _command.KeyValues : _command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var linkedCollection = _session.Metadata.GetNavigationPropertyPartnerTypeName(collectionName, linkName);
            var linkIdent = await FormatEntryKeyAsync(linkedCollection, linkedEntryKey, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateLinkRequestAsync(collectionName, linkName, entryIdent, linkIdent).ConfigureAwait(false);
        }

        public async Task<ODataRequest> UnlinkRequestAsync(string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            AssertHasKey(_command);

            await _session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var collectionName = _command.QualifiedEntityCollectionName;
            var entryKey = _command.HasKey ? _command.KeyValues : _command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            string linkIdent = null;
            if (linkedEntryKey != null)
            {              
                linkIdent = FormatUnlinkEntryKey(collectionName, linkedEntryKey, linkName);               
                if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
            }

            return await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateUnlinkRequestAsync(collectionName, linkName, entryIdent, linkIdent).ConfigureAwait(false);
        }

        private async Task<string> FormatEntryKeyAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            var entryIdent = command.HasKey
                ? await command.GetCommandTextAsync(cancellationToken).ConfigureAwait(false) 
                : await (new FluentCommand(command).Key(command.FilterAsKey).GetCommandTextAsync(cancellationToken)).ConfigureAwait(false);

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
        
        private string FormatUnlinkEntryKey(string collection, IDictionary<string, object> entryKey, string linkname)
        {
            var keyNames = _session.Metadata.GetDeclaredKeyPropertyNames(collection).ToList();
            var formatter = _session.Adapter.GetCommandFormatter();

            string command = _session.Metadata.GetNavigationPropertyExactName(collection, linkname);

            IDictionary<string, object> keys = entryKey.Where(v => keyNames.Contains(v.Key)).ToDictionary(v => v.Key, v => v.Value);
            command += formatter.ConvertKeyValuesToUriLiteral(keys, true);

            return command;
        }

        private void AssertHasKey(FluentCommand command)
        {
            if (!command.HasKey && command.FilterAsKey == null)
                throw new InvalidOperationException("No entry key specified.");
        }
    }

    internal class RequestBuilder<T> : IRequestBuilder<T>
        where T : class
    {
        private FluentCommand _command;
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
            var requestBuilder = new RequestBuilder(_command, _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.GetRequestAsync(scalarResult, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> FindEntriesAsync(ODataFeedAnnotations annotations)
        {
            return FindEntriesAsync(annotations, CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> FindEntriesAsync(ODataFeedAnnotations annotations, CancellationToken cancellationToken)
        {
            var requestBuilder = new RequestBuilder(_command.WithCount(), _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.GetRequestAsync(false, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> FindEntryAsync()
        {
            return FindEntryAsync(CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> FindEntryAsync(CancellationToken cancellationToken)
        {
            var requestBuilder = new RequestBuilder(_command, _session, _lazyBatchWriter);
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
            var requestBuilder = new RequestBuilder(_command, _session, _lazyBatchWriter);
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
            var requestBuilder = new RequestBuilder(_command, _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.UpdateRequestAsync(resultRequired, cancellationToken).ConfigureAwait(false), _session);
        }

        public Task<IClientWithRequest<T>> DeleteEntryAsync()
        {
            return DeleteEntryAsync(CancellationToken.None);
        }

        public async Task<IClientWithRequest<T>> DeleteEntryAsync(CancellationToken cancellationToken)
        {
            var requestBuilder = new RequestBuilder(_command, _session, _lazyBatchWriter);
            return new ClientWithRequest<T>(await requestBuilder.DeleteRequestAsync(cancellationToken).ConfigureAwait(false), _session);
        }
    }
}

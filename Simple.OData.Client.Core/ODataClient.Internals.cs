using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
    public partial class ODataClient
    {
        private async Task<IDictionary<string, object>> ExecuteInsertEntryAsync(FluentCommand command, bool resultRequired, CancellationToken cancellationToken)
        {
            var entryData = command.CommandData;
            var commandText = await command.GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateInsertRequestAsync(commandText, entryData, resultRequired);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var result = await ExecuteRequestWithResultAsync(request, cancellationToken,
                x => x.AsEntry(), () => null, () => request.EntryData);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            if (result == null && resultRequired &&
                _session.Metadata.GetDeclaredKeyPropertyNames(commandText).All(entryData.ContainsKey))
            {
                result = await this.GetEntryAsync(commandText, entryData, cancellationToken);
                if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
            }

            return result;
        }

        private async Task<IDictionary<string, object>> ExecuteUpdateEntryAsync(FluentCommand command, bool resultRequired, CancellationToken cancellationToken)
        {
            AssertHasKey(command);

            var collectionName = command.QualifiedEntityCollectionName;
            var entryKey = command.HasKey ? command.KeyValues : command.FilterAsKey;
            var entryData = command.CommandData;
            var entryIdent = await FormatEntryKeyAsync(command, cancellationToken);

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter).CreateUpdateRequestAsync(collectionName, entryIdent, entryKey, entryData, resultRequired);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var result = await ExecuteRequestWithResultAsync(request, cancellationToken,
                x => x.AsEntry(), () => null, () => request.EntryData);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            if (result == null && resultRequired)
            {
                try
                {
                    result = await GetUpdatedResult(command, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                }
                catch (Exception)
                {
                }
            }

            var entityCollection = _session.Metadata.GetEntityCollection(collectionName);
            var entryDetails = _session.Metadata.ParseEntryDetails(entityCollection.Name, entryData);

            var removedLinks = entryDetails.Links
                .SelectMany(x => x.Value.Where(y => y.LinkData == null))
                .Select(x => _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, x.LinkName))
                .ToList();

            foreach (var associationName in removedLinks)
            {
                try
                {
                    await UnlinkEntryAsync(collectionName, entryKey, associationName, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        private async Task<IDictionary<string, object>> GetUpdatedResult(FluentCommand command, CancellationToken cancellationToken)
        {
            var entryKey = command.HasKey ? command.KeyValues : command.FilterAsKey;
            var entryData = command.CommandData;

            var updatedKey = entryKey.Where(x => !entryData.ContainsKey(x.Key)).ToIDictionary();
            foreach (var item in entryData.Where(x => entryKey.ContainsKey(x.Key)))
            {
                updatedKey.Add(item);
            }
            var updatedCommand = new FluentCommand(command).Key(updatedKey);
            return await FindEntryAsync(await updatedCommand.GetCommandTextAsync(cancellationToken), cancellationToken);
        }

        private async Task<IEnumerable<IDictionary<string, object>>> ExecuteUpdateEntriesAsync(FluentCommand command, bool resultRequired, CancellationToken cancellationToken)
        {
            return await IterateEntriesAsync(
                command, resultRequired,
                async (x, y, z, w) => await UpdateEntryAsync(x, y, z, w, cancellationToken),
                cancellationToken);
        }

        private async Task ExecuteDeleteEntryAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed OData Client.");

            var collectionName = command.QualifiedEntityCollectionName;
            var entryIdent = await FormatEntryKeyAsync(command, cancellationToken);

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateDeleteRequestAsync(collectionName, entryIdent);
            if (!IsBatchRequest)
            {
                using (await _requestRunner.ExecuteRequestAsync(request, cancellationToken))
                {
                }
            }
        }

        private async Task<int> ExecuteDeleteEntriesAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            return await IterateEntriesAsync(
                command,
                async (x, y) => await DeleteEntryAsync(x, y, cancellationToken),
                cancellationToken);
        }

        private async Task ExecuteLinkEntryAsync(FluentCommand command, string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed OData Client.");

            AssertHasKey(command);

            var collectionName = command.QualifiedEntityCollectionName;
            var entryKey = command.HasKey ? command.KeyValues : command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var linkedCollection = _session.Metadata.GetNavigationPropertyPartnerName(collectionName, linkName);
            var linkIdent = await FormatEntryKeyAsync(linkedCollection, linkedEntryKey, cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateLinkRequestAsync(collectionName, linkName, entryIdent, linkIdent);

            if (!IsBatchRequest)
            {
                using (await _requestRunner.ExecuteRequestAsync(request, cancellationToken))
                {
                }
            }
        }

        private async Task ExecuteUnlinkEntryAsync(FluentCommand command, string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed OData Client.");

            AssertHasKey(command);

            var collectionName = command.QualifiedEntityCollectionName;
            var entryKey = command.HasKey ? command.KeyValues : command.FilterAsKey;

            var entryIdent = await FormatEntryKeyAsync(collectionName, entryKey, cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            string linkIdent = null;
            if (linkedEntryKey != null)
            {
                var linkedCollection = _session.Metadata.GetNavigationPropertyPartnerName(collectionName, linkName);
                linkIdent = await FormatEntryKeyAsync(linkedCollection, linkedEntryKey, cancellationToken);
                if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
            }

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateUnlinkRequestAsync(collectionName, linkName, entryIdent, linkIdent);

            if (!IsBatchRequest)
            {
                using (await _requestRunner.ExecuteRequestAsync(request, cancellationToken))
                {
                }
            }
        }

        private async Task<IEnumerable<IDictionary<string, object>>> ExecuteFunctionAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            var commandText = await command.GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateFunctionRequestAsync(commandText, command.FunctionName);

            return await ExecuteRequestWithResultAsync(request, cancellationToken,
                x => x.AsEntries(),
                () => new IDictionary<string, object>[] {});
        }

        private async Task<IEnumerable<IDictionary<string, object>>> ExecuteActionAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            var commandText = await command.GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var request = await _session.Adapter.GetRequestWriter(_lazyBatchWriter)
                .CreateActionRequestAsync(commandText, command.ActionName, command.CommandData, true);

            return await ExecuteRequestWithResultAsync(request, cancellationToken,
                x => x.AsEntries(),
                () => new IDictionary<string, object>[] {});
        }

        private async Task ExecuteBatchActionsAsync(IList<Func<IODataClient, Task>> actions, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed OData Client.");

            if (!actions.Any())
                return;

            var responseIndexes = new List<int>();

            // Write batch operations into a batch content
            foreach (var action in actions)
            {
                await action(this);
                responseIndexes.Add(_lazyBatchWriter.Value.LastOperationId - 1);
            }

            // Create batch request message
            var requestMessage = await _lazyBatchWriter.Value.EndBatchAsync();
            var request = new ODataRequest(RestVerbs.Post, _session, ODataLiteral.Batch, requestMessage);

            // Execute batch and get response
            ODataResponse batchResponse;
            using (var response = await _requestRunner.ExecuteRequestAsync(request, cancellationToken))
            {
                var responseReader = _session.Adapter.GetResponseReader();
                batchResponse = await responseReader.GetResponseAsync(response, _session.Settings.IncludeResourceTypeInEntryProperties);
            }

            // Replay batch operations to assign results
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                var actionResponse = batchResponse.Batch[responseIndexes[actionIndex]];
                if (actionResponse.StatusCode >= 400)
                {
                    var statusCode = (HttpStatusCode)actionResponse.StatusCode;
                    throw new WebRequestException(statusCode.ToString(), statusCode);
                }

                var client = new ODataClient(actionResponse);
                await actions[actionIndex](client);
            }
        }

        private async Task<T> ExecuteRequestWithResultAsync<T>(ODataRequest request, CancellationToken cancellationToken,
            Func<ODataResponse, T> createResult, Func<T> createEmptyResult, Func<T> createBatchResult = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed OData Client.");

            if (IsBatchRequest)
                return createBatchResult != null 
                    ? createBatchResult() 
                    : createEmptyResult != null
                    ? createEmptyResult() 
                    : default(T);

            try
            {
                using (var response = await _requestRunner.ExecuteRequestAsync(request, cancellationToken))
                {
                    if (response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent &&
                        (request.Method == RestVerbs.Get || request.ResultRequired))
                    {
                        var responseReader = _session.Adapter.GetResponseReader();
                        return createResult(await responseReader.GetResponseAsync(response, _settings.IncludeResourceTypeInEntryProperties));
                    }
                    else
                    {
                        return default(T);
                    }
                }
            }
            catch (WebRequestException ex)
            {
                if (_settings.IgnoreResourceNotFoundException && ex.Code == HttpStatusCode.NotFound)
                    return createEmptyResult != null ? createEmptyResult() : default(T);
                else
                    throw;
            }
        }

        private async Task<IEnumerable<IDictionary<string, object>>> IterateEntriesAsync(
            FluentCommand command, bool resultRequired,
            Func<string, IDictionary<string, object>, IDictionary<string, object>, bool, Task<IDictionary<string, object>>> funcAsync, CancellationToken cancellationToken)
        {
            var collectionName = command.QualifiedEntityCollectionName;
            var entryData = command.CommandData;
            var commandText = await command.GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<IDictionary<string, object>> result = null;
            var client = new ODataClient(_settings);
            var entries = await client.FindEntriesAsync(commandText, cancellationToken);
            if (entries != null)
            {
                var entryList = entries.ToList();
                var resultList = new List<IDictionary<string, object>>();
                foreach (var entry in entryList)
                {
                    resultList.Add(await funcAsync(collectionName, entry, entryData, resultRequired));
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                }
                result = resultList;
            }

            return result;
        }

        private async Task<int> IterateEntriesAsync(FluentCommand command,
            Func<string, IDictionary<string, object>, Task> funcAsync, CancellationToken cancellationToken)
        {
            var collectionName = command.QualifiedEntityCollectionName;
            var commandText = await command.GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var result = 0;
            var client = new ODataClient(_settings);
            var entries = await client.FindEntriesAsync(commandText, cancellationToken);
            if (entries != null)
            {
                var entryList = entries.ToList();
                foreach (var entry in entryList)
                {
                    await funcAsync(collectionName, entry);
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                    ++result;
                }
            }
            return result;
        }

        private void RemoveSystemProperties(IDictionary<string, object> entryData)
        {
            if (_settings.IncludeResourceTypeInEntryProperties && entryData.ContainsKey(FluentCommand.ResourceTypeLiteral))
            {
                entryData.Remove(FluentCommand.ResourceTypeLiteral);
            }
        }

        private void AssertHasKey(FluentCommand command)
        {
            if (!command.HasKey && command.FilterAsKey == null)
                throw new InvalidOperationException("No entry key specified.");
        }

        private async Task<string> FormatEntryKeyAsync(string collection, IDictionary<string, object> entryKey, CancellationToken cancellationToken)
        {
            var entryIdent = await GetFluentClient()
                .For(collection)
                .Key(entryKey)
                .GetCommandTextAsync(cancellationToken);

            return RemoveTypeSpecification(entryIdent);
        }

        private async Task<string> FormatEntryKeyAsync(FluentCommand command, CancellationToken cancellationToken)
        {
            var entryIdent = command.HasKey
                ? await command.GetCommandTextAsync(cancellationToken)
                : await (new FluentCommand(command).Key(command.FilterAsKey).GetCommandTextAsync(cancellationToken));

            return RemoveTypeSpecification(entryIdent);
        }

        private string RemoveTypeSpecification(string entryIdent)
        {
            var segments = entryIdent.Split('/');
            if (segments.Count() > 1 && segments.Last().Contains("."))
            {
                entryIdent = entryIdent.Substring(0, entryIdent.Length - segments.Last().Length - 1);
            }
            return entryIdent;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    partial class BoundClient<T>
    {
        public Task<IEnumerable<T>> FindEntriesAsync()
        {
            return FindEntriesAsync(CancellationToken.None);
        }

        public Task<IEnumerable<T>> FindEntriesAsync(CancellationToken cancellationToken)
        {
            return RectifyColumnSelectionAsync(
                _client.FindEntriesAsync(_command, false, null, cancellationToken), 
                _command.SelectedColumns, _command.DynamicPropertiesContainerName);
        }

        public Task<IEnumerable<T>> FindEntriesAsync(bool scalarResult)
        {
            return FindEntriesAsync(scalarResult, CancellationToken.None);
        }

        public Task<IEnumerable<T>> FindEntriesAsync(bool scalarResult, CancellationToken cancellationToken)
        {
            return RectifyColumnSelectionAsync(
                _client.FindEntriesAsync(_command, scalarResult, null, cancellationToken),
                _command.SelectedColumns, _command.DynamicPropertiesContainerName);
        }

        public Task<IEnumerable<T>> FindEntriesAsync(ODataFeedAnnotations annotations)
        {
            return FindEntriesAsync(annotations, CancellationToken.None);
        }

        public async Task<IEnumerable<T>> FindEntriesAsync(ODataFeedAnnotations annotations, CancellationToken cancellationToken)
        {
            var commandText = await _command.WithCount().GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            var result = _client.FindEntriesAsync(commandText, annotations, cancellationToken);
            return await RectifyColumnSelectionAsync(
                result, _command.SelectedColumns, _command.DynamicPropertiesContainerName);
        }

        public async Task<T> FindEntriesAsync<T>(Func<HttpResponseMessage, CancellationToken, Task<T>> responseConverterAsync, CancellationToken cancellationToken)
        {
            var commandText = await _command.WithCount().GetCommandTextAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            return await this._client.FindEntriesAsync(commandText, responseConverterAsync, cancellationToken);
        }

        public Task<IEnumerable<T>> FindEntriesAsync(Uri annotatedUri, ODataFeedAnnotations annotations)
        {
            return FindEntriesAsync(annotatedUri, annotations, CancellationToken.None);
        }

        public async Task<IEnumerable<T>> FindEntriesAsync(Uri annotatedUri, ODataFeedAnnotations annotations, CancellationToken cancellationToken)
        {
            var commandText = annotatedUri.AbsoluteUri;
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            var result = _client.FindEntriesAsync(commandText, annotations, cancellationToken);
            return await RectifyColumnSelectionAsync(
                result, _command.SelectedColumns, _command.DynamicPropertiesContainerName);
        }

        public Task<T> FindEntryAsync()
        {
            return FindEntryAsync(CancellationToken.None);
        }

        public Task<T> FindEntryAsync(CancellationToken cancellationToken)
        {
            return RectifyColumnSelectionAsync(
                _client.FindEntryAsync(_command, cancellationToken),
                _command.SelectedColumns, _command.DynamicPropertiesContainerName);
        }

        public Task<U> FindScalarAsync<U>()
        {
            return FindScalarAsync<U>(CancellationToken.None);
        }

        public async Task<U> FindScalarAsync<U>(CancellationToken cancellationToken)
        {
            return (U)Convert.ChangeType(await _client.FindScalarAsync(_command, cancellationToken), typeof(U), CultureInfo.InvariantCulture);
        }

        public Task<T> InsertEntryAsync()
        {
            return InsertEntryAsync(true, CancellationToken.None);
        }

        public Task<T> InsertEntryAsync(CancellationToken cancellationToken)
        {
            return InsertEntryAsync(true, cancellationToken);
        }

        public Task<T> InsertEntryAsync(bool resultRequired)
        {
            return InsertEntryAsync(resultRequired, CancellationToken.None);
        }

        public async Task<T> InsertEntryAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            return (await _client.InsertEntryAsync(_command, _command.CommandData, resultRequired, cancellationToken))
                .ToObject<T>(_command.DynamicPropertiesContainerName, _dynamicResults);
        }

        public Task<T> UpdateEntryAsync()
        {
            return UpdateEntryAsync(true, CancellationToken.None);
        }

        public Task<T> UpdateEntryAsync(CancellationToken cancellationToken)
        {
            return UpdateEntryAsync(true, cancellationToken);
        }

        public Task<T> UpdateEntryAsync(bool resultRequired)
        {
            return UpdateEntryAsync(resultRequired, CancellationToken.None);
        }

        public async Task<T> UpdateEntryAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            if (_command.HasFilter)
            {
                var result = await UpdateEntriesAsync(resultRequired, cancellationToken);
                return resultRequired 
                    ? result == null ? null : result.First()
                    : null;
            }
            else
            {
                var result = await _client.UpdateEntryAsync(_command, resultRequired, cancellationToken);
                return resultRequired
                    ? result == null ? null : result.ToObject<T>(_command.DynamicPropertiesContainerName, _dynamicResults)
                    : null;
            }
        }

        public Task<IEnumerable<T>> UpdateEntriesAsync()
        {
            return UpdateEntriesAsync(true, CancellationToken.None);
        }

        public Task<IEnumerable<T>> UpdateEntriesAsync(CancellationToken cancellationToken)
        {
            return UpdateEntriesAsync(true, cancellationToken);
        }

        public Task<IEnumerable<T>> UpdateEntriesAsync(bool resultRequired)
        {
            return UpdateEntriesAsync(resultRequired, CancellationToken.None);
        }

        public async Task<IEnumerable<T>> UpdateEntriesAsync(bool resultRequired, CancellationToken cancellationToken)
        {
            return (await _client.UpdateEntriesAsync(_command, _command.CommandData, resultRequired, cancellationToken))
                .Select(y => y.ToObject<T>(_command.DynamicPropertiesContainerName, _dynamicResults));
        }

        public Task DeleteEntryAsync()
        {
            return DeleteEntryAsync(CancellationToken.None);
        }

        public Task DeleteEntryAsync(CancellationToken cancellationToken)
        {
            if (_command.HasFilter)
                return DeleteEntriesAsync(cancellationToken);
            else
                return _client.DeleteEntryAsync(_command, cancellationToken);
        }

        public Task<int> DeleteEntriesAsync()
        {
            return DeleteEntriesAsync(CancellationToken.None);
        }

        public Task<int> DeleteEntriesAsync(CancellationToken cancellationToken)
        {
            return _client.DeleteEntriesAsync(_command, cancellationToken);
        }

        public Task LinkEntryAsync<U>(U linkedEntryKey)
        {
            return LinkEntryAsync(linkedEntryKey, null, CancellationToken.None);
        }

        public Task LinkEntryAsync<U>(U linkedEntryKey, CancellationToken cancellationToken)
        {
            return LinkEntryAsync(linkedEntryKey, null, cancellationToken);
        }

        public Task LinkEntryAsync<U>(U linkedEntryKey, string linkName)
        {
            return LinkEntryAsync(linkedEntryKey, linkName, CancellationToken.None);
        }

        public Task LinkEntryAsync<U>(U linkedEntryKey, string linkName, CancellationToken cancellationToken)
        {
            return _client.LinkEntryAsync(_command, linkName ?? typeof(U).Name, linkedEntryKey.ToDictionary(), cancellationToken);
        }

        public Task LinkEntryAsync<U>(Expression<Func<T, U>> expression, U linkedEntryKey)
        {
            return LinkEntryAsync(expression, linkedEntryKey, CancellationToken.None);
        }

        public Task LinkEntryAsync<U>(Expression<Func<T, U>> expression, U linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.LinkEntryAsync(_command, ColumnExpression.ExtractColumnName(expression), linkedEntryKey.ToDictionary(), cancellationToken);
        }

        public Task LinkEntryAsync(ODataExpression expression, IDictionary<string, object> linkedEntryKey)
        {
            return _client.LinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey.ToDictionary(), CancellationToken.None);
        }

        public Task LinkEntryAsync(ODataExpression expression, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.LinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey.ToDictionary(), cancellationToken);
        }

        public Task LinkEntryAsync(ODataExpression expression, ODataEntry linkedEntryKey)
        {
            return _client.LinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey.ToDictionary(), CancellationToken.None);
        }

        public Task LinkEntryAsync(ODataExpression expression, ODataEntry linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.LinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey.ToDictionary(), cancellationToken);
        }

        public Task UnlinkEntryAsync<U>()
        {
            return _client.UnlinkEntryAsync(_command, typeof(U).Name, null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync<U>(CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, typeof(U).Name, null, cancellationToken);
        }

        public Task UnlinkEntryAsync(string linkName)
        {
            return _client.UnlinkEntryAsync(_command, linkName, null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync(string linkName, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, linkName, null, cancellationToken);
        }

        public Task UnlinkEntryAsync<U>(Expression<Func<T, U>> expression)
        {
            return _client.UnlinkEntryAsync(_command, ColumnExpression.ExtractColumnName(expression), null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync<U>(Expression<Func<T, U>> expression, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, ColumnExpression.ExtractColumnName(expression), null, cancellationToken);
        }

        public Task UnlinkEntryAsync(ODataExpression expression)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync(ODataExpression expression, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), null, cancellationToken);
        }

        public Task UnlinkEntryAsync<U>(U linkedEntryKey)
        {
            return UnlinkEntryAsync(linkedEntryKey, CancellationToken.None);
        }

        public Task UnlinkEntryAsync<U>(U linkedEntryKey, CancellationToken cancellationToken)
        {
            if (linkedEntryKey.GetType() == typeof(string))
                return UnlinkEntryAsync(linkedEntryKey.ToString(), cancellationToken);
            else if (linkedEntryKey is ODataExpression && (linkedEntryKey as ODataExpression).Reference != null)
                return UnlinkEntryAsync((linkedEntryKey as ODataExpression).Reference, cancellationToken);
            else
                return UnlinkEntryAsync(linkedEntryKey, null, cancellationToken);
        }

        public Task UnlinkEntryAsync<U>(U linkedEntryKey, string linkName)
        {
            return UnlinkEntryAsync(linkedEntryKey, linkName, CancellationToken.None);
        }

        public Task UnlinkEntryAsync<U>(U linkedEntryKey, string linkName, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, linkName ?? typeof(U).Name, linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, cancellationToken);
        }

        public Task UnlinkEntryAsync<U>(Expression<Func<T, U>> expression, U linkedEntryKey)
        {
            return UnlinkEntryAsync(expression, linkedEntryKey, CancellationToken.None);
        }

        public Task UnlinkEntryAsync<U>(Expression<Func<T, U>> expression, U linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, ColumnExpression.ExtractColumnName(expression), linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, cancellationToken);
        }

        public Task UnlinkEntryAsync(ODataExpression expression, IDictionary<string, object> linkedEntryKey)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync(ODataExpression expression, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, cancellationToken);
        }

        public Task UnlinkEntryAsync(ODataExpression expression, ODataEntry linkedEntryKey)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, CancellationToken.None);
        }

        public Task UnlinkEntryAsync(ODataExpression expression, ODataEntry linkedEntryKey, CancellationToken cancellationToken)
        {
            return _client.UnlinkEntryAsync(_command, expression.AsString(_session), linkedEntryKey != null ? linkedEntryKey.ToDictionary() : null, cancellationToken);
        }
    }
}

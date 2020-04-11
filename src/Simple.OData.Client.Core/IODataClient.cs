using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    /// <summary>
    /// Provides access to OData operations.
    /// </summary>
    public interface IODataClient
    {
        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>The fluent OData client instance.</returns>
        IBoundClient<IDictionary<string, object>> For(string collectionName);
        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <param name="expression">Collection expression.</param>
        /// <returns>The fluent OData client instance.</returns>
        IBoundClient<ODataEntry> For(ODataExpression expression);
        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>The fluent OData client instance.</returns>
        IBoundClient<T> For<T>(string collectionName = null) where T : class;

        /// <summary>
        /// Returns an instance of a fluent OData client for unbound operations (functions and actions).
        /// </summary>
        /// <returns>The fluent OData client instance.</returns>
        IUnboundClient<object> Unbound();
        /// <summary>
        /// Returns an instance of a fluent OData client for unbound operations (functions and actions).
        /// </summary>
        /// <returns>The fluent OData client instance.</returns>
        IUnboundClient<T> Unbound<T>() where T : class;

        /// <summary>
        /// Gets the OData service metadata.
        /// </summary>
        /// <returns>The service metadata.</returns>
        Task<object> GetMetadataAsync();
        /// <summary>
        /// Gets the OData service metadata.
        /// </summary>
        /// <returns>The service metadata.</returns>
        Task<object> GetMetadataAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Gets the OData service metadata.
        /// </summary>
        /// <typeparam name="T">OData protocol specific metadata interface</typeparam>
        /// <returns>
        /// The service metadata.
        /// </returns>
        Task<T> GetMetadataAsync<T>();
        /// <summary>
        /// Gets the OData service metadata.
        /// </summary>
        /// <typeparam name="T">OData protocol specific metadata interface</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The service metadata.
        /// </returns>
        Task<T> GetMetadataAsync<T>(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the OData service metadata as string.
        /// </summary>
        /// <returns>The service metadata string.</returns>
        Task<string> GetMetadataDocumentAsync();
        /// <summary>
        /// Gets the OData service metadata as string.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The service metadata string.</returns>
        Task<string> GetMetadataDocumentAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the OData command text.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="expression">The command expression.</param>
        /// <returns>The command text.</returns>
        Task<string> GetCommandTextAsync(string collection, ODataExpression expression);
        /// <summary>
        /// Gets the OData command text.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="expression">The command expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command text.</returns>
        Task<string> GetCommandTextAsync(string collection, ODataExpression expression, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the OData command text.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="expression">The command expression.</param>
        /// <returns>The command text.</returns>
        Task<string> GetCommandTextAsync<T>(string collection, Expression<Func<T, bool>> expression);
        /// <summary>
        /// Gets the OData command text.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="expression">The command expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The command text.</returns>
        Task<string> GetCommandTextAsync<T>(string collection, Expression<Func<T, bool>> expression, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves entries by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <returns>Entries found.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText);
        /// <summary>
        /// Retrieves entries by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Entries found.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText, CancellationToken cancellationToken);
        /// <summary>
        /// Retrieves entries by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="scalarResult">if set to <c>true</c> the result is expected to be of a scalar type.</param>
        /// <returns>Entries found.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText, bool scalarResult);
        /// <summary>
        /// Retrieves entries by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="scalarResult">if set to <c>true</c> the result is expected to be of a scalar type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Entries found.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText, bool scalarResult, CancellationToken cancellationToken);
        /// <summary>
        /// Retrieves entries by executing OData GET request and assigns OData feed annotations.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="annotations">The OData feed annotations.</param>
        /// <returns>Entries found with entry count.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText, ODataFeedAnnotations annotations);
        /// <summary>
        /// Retrieves entries by executing OData GET request and assigns OData feed annotations.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="annotations">The OData feed annotations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Entries found with entry count.</returns>
        Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText, ODataFeedAnnotations annotations, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves an entry by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <returns>The first of the entries found.</returns>
        Task<IDictionary<string, object>> FindEntryAsync(string commandText);
        /// <summary>
        /// Retrieves an entry by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first of the entries found.</returns>
        Task<IDictionary<string, object>> FindEntryAsync(string commandText, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves an entry as a scalar type by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <returns>The result as a scalar type.</returns>
        Task<object> FindScalarAsync(string commandText);
        /// <summary>
        /// Retrieves an entry as a scalar type by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result as a scalar type.</returns>
        Task<object> FindScalarAsync(string commandText, CancellationToken cancellationToken);

        /// <summary>
        /// Looks up an entry by executing OData GET request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <returns>The entry with the specified key</returns>
        Task<IDictionary<string, object>> GetEntryAsync(string collection, params object[] entryKey);
        /// <summary>
        /// Looks up an entry by executing OData GET request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <returns>The entry with the specified key</returns>
        Task<IDictionary<string, object>> GetEntryAsync(string collection, CancellationToken cancellationToken, params object[] entryKey);
        /// <summary>
        /// Looks up an entry by executing OData GET request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <returns>The entry with the specified key</returns>
        Task<IDictionary<string, object>> GetEntryAsync(string collection, IDictionary<string, object> entryKey);
        /// <summary>
        /// Looks up an entry by executing OData GET request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entry with the specified key</returns>
        Task<IDictionary<string, object>> GetEntryAsync(string collection, IDictionary<string, object> entryKey, CancellationToken cancellationToken);

        /// <summary>
        /// Insert a new entry by executing OData POST request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryData">The entry data.</param>
        /// <returns>The newly inserted entry</returns>
        Task<IDictionary<string, object>> InsertEntryAsync(string collection, IDictionary<string, object> entryData);
        /// <summary>
        /// Insert a new entry by executing OData POST request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly inserted entry</returns>
        Task<IDictionary<string, object>> InsertEntryAsync(string collection, IDictionary<string, object> entryData, CancellationToken cancellationToken);
        /// <summary>
        /// Insert a new entry by executing OData POST request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the new entry data, otherwise returns <c>null</c>.</param>
        /// <returns>The newly inserted entry</returns>
        Task<IDictionary<string, object>> InsertEntryAsync(string collection, IDictionary<string, object> entryData, bool resultRequired);
        /// <summary>
        /// Insert a new entry by executing OData POST request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the new entry data, otherwise returns <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly inserted entry</returns>
        Task<IDictionary<string, object>> InsertEntryAsync(string collection, IDictionary<string, object> entryData, bool resultRequired, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the existing entry by executing OData PUT or PATCH request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="entryData">The entry data.</param>
        /// <returns>The updated entry data</returns>
        Task<IDictionary<string, object>> UpdateEntryAsync(string collection, IDictionary<string, object> entryKey, IDictionary<string, object> entryData);
        /// <summary>
        /// Updates the existing entry by executing OData PUT or PATCH request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated entry data</returns>
        Task<IDictionary<string, object>> UpdateEntryAsync(string collection, IDictionary<string, object> entryKey, IDictionary<string, object> entryData, CancellationToken cancellationToken);
        /// <summary>
        /// Updates the existing entry by executing OData PUT or PATCH request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the updated entry data, otherwise returns <c>null</c>.</param>
        /// <returns>The updated entry data</returns>
        Task<IDictionary<string, object>> UpdateEntryAsync(string collection, IDictionary<string, object> entryKey, IDictionary<string, object> entryData, bool resultRequired);
        /// <summary>
        /// Updates the existing entry by executing OData PUT or PATCH request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the updated entry data, otherwise returns <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated entry data</returns>
        Task<IDictionary<string, object>> UpdateEntryAsync(string collection, IDictionary<string, object> entryKey, IDictionary<string, object> entryData, bool resultRequired, CancellationToken cancellationToken);

        /// <summary>
        /// Updates entries by executing multiple OData PUT or PATCH requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="entryData">The entry data.</param>
        /// <returns>The updated entries data</returns>
        Task<IEnumerable<IDictionary<string, object>>> UpdateEntriesAsync(string collection, string commandText, IDictionary<string, object> entryData);
        /// <summary>
        /// Updates entries by executing multiple OData PUT or PATCH requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated entries data</returns>
        Task<IEnumerable<IDictionary<string, object>>> UpdateEntriesAsync(string collection, string commandText, IDictionary<string, object> entryData, CancellationToken cancellationToken);
        /// <summary>
        /// Updates entries by executing multiple OData PUT or PATCH requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the updated entry data, otherwise returns <c>null</c>.</param>
        /// <returns>The updated entries data</returns>
        Task<IEnumerable<IDictionary<string, object>>> UpdateEntriesAsync(string collection, string commandText, IDictionary<string, object> entryData, bool resultRequired);
        /// <summary>
        /// Updates entries by executing multiple OData PUT or PATCH requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="entryData">The entry data.</param>
        /// <param name="resultRequired">if set to <c>true</c> returns the updated entry data, otherwise returns <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated entries data</returns>
        Task<IEnumerable<IDictionary<string, object>>> UpdateEntriesAsync(string collection, string commandText, IDictionary<string, object> entryData, bool resultRequired, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the existing entry by executing OData DELETE request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <returns>Task instance.</returns>
        Task DeleteEntryAsync(string collection, IDictionary<string, object> entryKey);
        /// <summary>
        /// Deletes the existing entry by executing OData DELETE request.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task instance.</returns>
        Task DeleteEntryAsync(string collection, IDictionary<string, object> entryKey, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes entries by executing multiple OData DELETE requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <returns>The number of deleted entries.</returns>
        Task<int> DeleteEntriesAsync(string collection, string commandText);
        /// <summary>
        /// Deletes entries by executing multiple OData DELETE requests.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of deleted entries.</returns>
        Task<int> DeleteEntriesAsync(string collection, string commandText, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link.</param>
        /// <param name="linkedEntryKey">The linked entry key.</param>
        /// <returns>Task instance.</returns>
        Task LinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName, IDictionary<string, object> linkedEntryKey);
        /// <summary>
        /// Creates a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link.</param>
        /// <param name="linkedEntryKey">The linked entry key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task instance.</returns>
        Task LinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link to be deleted.</param>
        /// <returns>Task instance.</returns>
        Task UnlinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName);
        /// <summary>
        /// Deletes a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link to be deleted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task instance.</returns>
        Task UnlinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName, CancellationToken cancellationToken);
        /// <summary>
        /// Deletes a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link to be deleted.</param>
        /// <param name="linkedEntryKey">The linked entry key.</param>
        /// <returns>Task instance.</returns>
        Task UnlinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName, IDictionary<string, object> linkedEntryKey);
        /// <summary>
        /// Deletes a link between entries.
        /// </summary>
        /// <param name="collection">The name of the collection.</param>
        /// <param name="entryKey">The entry key.</param>
        /// <param name="linkName">Name of the link to be deleted.</param>
        /// <param name="linkedEntryKey">The linked entry key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task instance.</returns>
        Task UnlinkEntryAsync(string collection, IDictionary<string, object> entryKey, string linkName, IDictionary<string, object> linkedEntryKey, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a media entry stream by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <returns>The media stream.</returns>
        Task<Stream> GetMediaStreamAsync(string commandText);
        /// <summary>
        /// Retrieves a media entry stream by executing OData GET request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The media stream.</returns>
        Task<Stream> GetMediaStreamAsync(string commandText, CancellationToken cancellationToken);

        /// <summary>
        /// Assigns a stream to a media entry by executing OData PUT request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="stream">The media stream.</param>
        /// <param name="contentType">The media content type.</param>
        /// <param name="optimisticConcurrency">if set to <c>true</c> sets the resource ETag in the updated request header.</param>
        /// <returns>Task instance.</returns>
        Task SetMediaStreamAsync(string commandText, Stream stream, string contentType, bool optimisticConcurrency);
        /// <summary>
        /// Assigns a stream to a media entry by executing OData PUT request.
        /// </summary>
        /// <param name="commandText">The OData command text.</param>
        /// <param name="stream">The media stream.</param>
        /// <param name="contentType">The media content type.</param>
        /// <param name="optimisticConcurrency">if set to <c>true</c> sets the resource ETag in the updated request header.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task instance.</returns>
        Task SetMediaStreamAsync(string commandText, Stream stream, string contentType, bool optimisticConcurrency, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData function.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <returns>Function execution result.</returns>
        Task<IDictionary<string, object>> ExecuteFunctionAsSingleAsync(string functionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData function.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Function execution result.</returns>
        Task<IDictionary<string, object>> ExecuteFunctionAsSingleAsync(string functionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData function.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <returns>Function execution result.</returns>
        Task<IEnumerable<IDictionary<string, object>>> ExecuteFunctionAsEnumerableAsync(string functionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData function.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Function execution result.</returns>
        Task<IEnumerable<IDictionary<string, object>>> ExecuteFunctionAsEnumerableAsync(string functionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData function and returns scalar result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Function execution result.</returns>
        Task<T> ExecuteFunctionAsScalarAsync<T>(string functionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData function and returns scalar result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Function execution result.</returns>
        Task<T> ExecuteFunctionAsScalarAsync<T>(string functionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData function and returns an array.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <returns>Function execution result.</returns>
        Task<T[]> ExecuteFunctionAsArrayAsync<T>(string functionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData function and returns an array.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="parameters">The function parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Function execution result.</returns>
        Task<T[]> ExecuteFunctionAsArrayAsync<T>(string functionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <returns>Action execution result.</returns>
        Task ExecuteActionAsync(string actionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Action execution result.</returns>
        Task ExecuteActionAsync(string actionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <returns>Action execution result.</returns>
        Task<IDictionary<string, object>> ExecuteActionAsSingleAsync(string actionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Action execution result.</returns>
        Task<IDictionary<string, object>> ExecuteActionAsSingleAsync(string actionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <returns>Action execution result.</returns>
        Task<IEnumerable<IDictionary<string, object>>> ExecuteActionAsEnumerableAsync(string actionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Action execution result.</returns>
        Task<IEnumerable<IDictionary<string, object>>> ExecuteActionAsEnumerableAsync(string actionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData action and returns scalar result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>action execution result.</returns>
        Task<T> ExecuteActionAsScalarAsync<T>(string actionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData action and returns scalar result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Action execution result.</returns>
        Task<T> ExecuteActionAsScalarAsync<T>(string actionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the OData action and returns an array.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <returns>Action execution result.</returns>
        Task<T[]> ExecuteActionAsArrayAsync<T>(string actionName, IDictionary<string, object> parameters);
        /// <summary>
        /// Executes the OData action and returns an array.
        /// </summary>
        /// <typeparam name="T">The array element type.</typeparam>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="parameters">The action parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Action execution result.</returns>
        Task<T[]> ExecuteActionAsArrayAsync<T>(string actionName, IDictionary<string, object> parameters, CancellationToken cancellationToken);
        Task<ODataResponse> GetResponseAsync(ODataRequest request);
        Task<ODataResponse> GetResponseAsync(ODataRequest request, CancellationToken cancellationToken);
    }
}

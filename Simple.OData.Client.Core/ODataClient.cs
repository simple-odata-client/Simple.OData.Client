using System;
using System.Collections.Generic;

namespace Simple.OData.Client
{
    /// <summary>
    /// Provides access to OData operations.
    /// </summary>
    public partial class ODataClient : IODataClient
    {
        private readonly ODataClientSettings _settings;
        private readonly Session _session;
        private readonly RequestRunner _requestRunner;
        private readonly Lazy<IBatchWriter> _lazyBatchWriter;
        private readonly SimpleDictionary<object, IDictionary<string, object>> _batchEntries;
        private readonly ODataResponse _batchResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClient"/> class.
        /// </summary>
        /// <param name="baseUri">The OData service URL.</param>
        /// <remarks>
        /// This constructor overload is obsolete. Use <see cref="ODataClient(Uri)"/> constructor overload./>
        /// </remarks>
        public ODataClient(string baseUri)
            : this(new ODataClientSettings {BaseUri = new Uri(baseUri)})
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClient"/> class.
        /// </summary>
        /// <param name="baseUri">The OData service URL.</param>
        public ODataClient(Uri baseUri)
            : this(new ODataClientSettings { BaseUri = baseUri })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClient"/> class.
        /// </summary>
        /// <param name="settings">The OData client settings.</param>
        public ODataClient(ODataClientSettings settings)
        {
            _settings = settings;
            _session = Session.FromSettings(_settings);
            _requestRunner = new RequestRunner(_session);
        }

        internal ODataClient(ODataClientSettings settings, SimpleDictionary<object, IDictionary<string, object>> batchEntries)
            : this(settings)
        {
            if (batchEntries != null)
            {
                _batchEntries = batchEntries;
                _lazyBatchWriter = new Lazy<IBatchWriter>(() => _session.Adapter.GetBatchWriter(_batchEntries));
            }
        }

        internal ODataClient(ODataClient client, SimpleDictionary<object, IDictionary<string, object>> batchEntries)
        {
            _settings = client._settings;
            _session = client.Session;
            _requestRunner = client._requestRunner;
            if (batchEntries != null)
            {
                _batchEntries = batchEntries;
                _lazyBatchWriter = new Lazy<IBatchWriter>(() => _session.Adapter.GetBatchWriter(_batchEntries));
            }
        }

        internal ODataClient(ODataClient client, ODataResponse batchResponse)
        {
            _session = client.Session;
            _batchResponse = batchResponse;
        }

        internal Session Session { get { return _session; } }
        internal ODataResponse BatchResponse { get { return _batchResponse; } }
        internal bool IsBatchRequest { get { return _lazyBatchWriter != null; } }
        internal bool IsBatchResponse { get { return _batchResponse != null; } }
        internal SimpleDictionary<object, IDictionary<string, object>> BatchEntries { get { return _batchEntries; } }

        /// <summary>
        /// Parses the OData service metadata string.
        /// </summary>
        /// <typeparam name="T">OData protocol specific metadata interface</typeparam>
        /// <param name="metadataString">The metadata string.</param>
        /// <returns>
        /// The service metadata.
        /// </returns>
        public static T ParseMetadataString<T>(string metadataString)
        {
            var session = Session.FromMetadata(new Uri("http://localhost/" + metadataString.GetHashCode() + "$metadata"), metadataString);
            return (T)session.Adapter.Model;
        }

        /// <summary>
        /// Clears service metadata cache.
        /// </summary>
        public static void ClearMetadataCache()
        {
            lock (MetadataCache.Instances)
            {
                MetadataCache.Instances.Clear();
            }
        }

        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>
        /// The fluent OData client instance.
        /// </returns>
        public IBoundClient<IDictionary<string, object>> For(string collectionName)
        {
            return GetFluentClient().For(collectionName);
        }

        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <param name="expression">Collection expression.</param>
        /// <returns>
        /// The fluent OData client instance.
        /// </returns>
        public IBoundClient<ODataEntry> For(ODataExpression expression)
        {
            return new BoundClient<ODataEntry>(this, _session).For(expression);
        }

        /// <summary>
        /// Returns an instance of a fluent OData client for the specified collection.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>
        /// The fluent OData client instance.
        /// </returns>
        public IBoundClient<T> For<T>(string collectionName = null)
            where T : class
        {
            return new BoundClient<T>(this, _session).For(collectionName);
        }

        /// <summary>
        /// Returns an instance of a fluent OData client for unbound operations (functions and actions).
        /// </summary>
        /// <returns>The fluent OData client instance.</returns>
        public IUnboundClient<object> Unbound()
        {
            return GetUnboundClient<object>();
        }

        /// <summary>
        /// Returns an instance of a fluent OData client for unbound operations (functions and actions).
        /// </summary>
        /// <returns>The fluent OData client instance.</returns>
        public IUnboundClient<T> Unbound<T>()
            where T : class
        {
            return GetUnboundClient<T>();
        }

        private BoundClient<IDictionary<string, object>> GetFluentClient()
        {
            return new BoundClient<IDictionary<string, object>>(this, _session);
        }

        private UnboundClient<T> GetUnboundClient<T>()
            where T : class
        {
            return new UnboundClient<T>(this, _session);
        }

        /// <summary>
        /// Sets the word pluralizer used when resolving metadata objects.
        /// </summary>
        /// <param name="pluralizer">The pluralizer.</param>
        public void SetPluralizer(IPluralizer pluralizer)
        {
            _session.Pluralizer = pluralizer;
        }

        /// <summary>
        /// Allows callers to manipulate the request headers in between request executions.
        /// Useful for retrieval of x-csrf-tokens when you want to update the request header
        /// with the retrieved token on subsequent requests.
        /// </summary>
        /// <param name="headers">The list of headers to update.</param>
        public void UpdateRequestHeaders(Dictionary<string, IEnumerable<string>> headers)
        {
            _settings.BeforeRequest += (request) =>
            {
                foreach (var header in headers)
                {
                    if (request.Headers.Contains(header.Key))
                    {
                        request.Headers.Remove(header.Key);
                    }

                    request.Headers.Add(header.Key, header.Value);
                }
            };
        }
    }
}

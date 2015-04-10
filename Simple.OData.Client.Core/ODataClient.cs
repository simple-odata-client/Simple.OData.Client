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
        private bool _isDisposed = false;
        private readonly Lazy<IBatchWriter> _lazyBatchWriter;
        private readonly ODataResponse _batchResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClient"/> class.
        /// </summary>
        /// <param name="urlBase">The OData service URL.</param>
        public ODataClient(string urlBase)
            : this(new ODataClientSettings { UrlBase = urlBase })
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

        internal ODataClient(ODataClientSettings settings, bool isBatch)
            : this(settings)
        {
            if (isBatch)
            {
                _lazyBatchWriter = new Lazy<IBatchWriter>(() => _session.Adapter.GetBatchWriter());
            }
        }

        internal ODataClient(ODataResponse batchResponse)
        {
            _batchResponse = batchResponse;
        }

        internal Session Session { get { return _session; } }
        internal ODataResponse BatchResponse { get { return _batchResponse; } }
        internal bool IsBatchRequest { get { return _lazyBatchWriter != null; } }
        internal bool IsBatchResponse { get { return _batchResponse != null; } }

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
            var session = Session.FromMetadata("http://localhost/" + metadataString.GetHashCode() + "$metadata", metadataString);
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

        public void Dispose()
        {
            _isDisposed = true;
            _requestRunner.Dispose();
        }
    }
}

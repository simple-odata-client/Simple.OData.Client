using System;
using System.Net;
using System.Net.Http;

namespace Simple.OData.Client
{
    /// <summary>
    /// OData client configuration settings
    /// </summary>
    public class ODataClientSettings
    {
        private readonly INameMatchResolver _defaultNameMatchResolver = new BestMatchResolver();
        private INameMatchResolver _nameMatchResolver;

        /// <summary>
        /// Gets or sets the OData service URL.
        /// </summary>
        /// <value>
        /// The URL address.
        /// </value>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets the OData client credentials.
        /// </summary>
        /// <value>
        /// The client credentials.
        /// </value>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets the OData payload format.
        /// </summary>
        /// <value>
        /// The payload format (JSON or Atom).
        /// </value>
        public ODataPayloadFormat PayloadFormat { get; set; }

        /// <summary>
        /// Gets or sets the time period to wait before the request times out.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        public TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether entry properties should be extended with the OData annotations.
        /// </summary>
        /// <value>
        /// <c>true</c> to include OData annotations in entry properties; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeAnnotationsInResults { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether resource not found exception (404) should be ignored.
        /// </summary>
        /// <value>
        /// <c>true</c> to ignore resource not found exception; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreResourceNotFoundException { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unmapped structural or navigation properties should be ignored or cause <see cref="UnresolvableObjectException"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> to ignore unmapped properties; otherwise, <c>false</c>.
        /// </value>
        public bool IgnoreUnmappedProperties { get; set; }

        /// <summary>
        /// Gets or sets a preferred update method for OData entries. The selected method will be used wherever it's compatible with the update scenario. 
        /// If not specified, PATCH is preferred due to better performance.
        /// </summary>
        /// <value>
        /// The update method (PUT or PATCH).
        /// </value>
        public ODataUpdateMethod PreferredUpdateMethod { get; set; }

        /// <summary>
        /// Gets or sets the OData service metadata document. If not set, service metadata is downloaded prior to the first call to the OData service and stored in an in-memory cache.
        /// </summary>
        /// <value>
        /// The content of the service metadata document.
        /// </value>
        public string MetadataDocument { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="HttpClient"/> connection should be disposed and renewed between OData requests.
        /// </summary>
        /// <value>
        /// <c>true</c> to create a new <see cref="HttpClient"/> instance for each request; <c>false</c> to reuse <see cref="HttpClient"/> between requests.
        /// </value>
        public bool RenewHttpConnection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="HttpClient"/> should omit namespaces for function and action calls in generated URI.
        /// </summary>
        /// <value>
        /// <c>true</c> to omit namespaces for function and action calls in generated URI; <c>false</c> otherwise.
        /// </value>
        public bool UnqualifiedNameCall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="HttpClient"/> should omit type prefix for Enum values in generated URI.
        /// </summary>
        /// <value>
        /// <c>true</c> to omit type prefix for Enum values in generated URI; <c>false</c> otherwise.
        /// </value>
        public bool EnumPrefixFree { get; set; }

        /// <summary>
        /// Gets or sets a name resolver for OData resources, types and properties.
        /// </summary>
        /// <value>
        /// If not set, a built-in word pluralizer is used to resolve resource, type and property names.
        /// </value>
        public INameMatchResolver NameMatchResolver
        {
            get => _nameMatchResolver ?? _defaultNameMatchResolver;
            set => _nameMatchResolver = value;
        }

        /// <summary>
        /// Gets or sets the HttpMessageHandler factory used by HttpClient.
        /// If not set, ODataClient creates an instance of HttpClientHandler.
        /// </summary>
        /// <value>
        /// The action on <see cref="HttpMessageHandler"/>.
        /// </value>
        public Func<HttpMessageHandler> OnCreateMessageHandler { get; set; }

        /// <summary>
        /// Gets or sets the action on HttpClientHandler.
        /// </summary>
        /// <value>
        /// The action on <see cref="HttpClientHandler"/>.
        /// </value>
        public Action<HttpClientHandler> OnApplyClientHandler { get; set; }

        /// <summary>
        /// Gets or sets the action executed before the OData request.
        /// </summary>
        /// <value>
        /// The action on <see cref="HttpRequestMessage"/>.
        /// </value>
        public Action<HttpRequestMessage> BeforeRequest { get; set; }

        /// <summary>
        /// Gets or sets the action executed after the OData request.
        /// </summary>
        /// <value>
        /// The action on <see cref="HttpResponseMessage"/>.
        /// </value>
        public Action<HttpResponseMessage> AfterResponse { get; set; }

        /// <summary>
        /// Gets or sets the method that will be executed to write trace messages.
        /// </summary>
        /// <value>
        /// The trace message handler.
        /// </value>
        public Action<string, object[]> OnTrace { get; set; }

        /// <summary>
        /// Gets or sets the filter of information that is written to trace messages.
        /// </summary>
        /// <value>
        /// The <see cref="ODataTrace"/> filter value.
        /// </value>
        public ODataTrace TraceFilter { get; set; }

        /// <summary>
        /// Gets or sets the adapter factory to create custom OData adapters
        /// </summary>
        public Core.Adapter.IAdapterFactory AdapterFactory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClientSettings"/> class.
        /// </summary>
        public ODataClientSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClientSettings"/> class.
        /// </summary>
        /// <param name="baseUri">The URL address.</param>
        /// <param name="credentials">The client credentials.</param>
        public ODataClientSettings(string baseUri, ICredentials credentials = null)
        {
            this.BaseUri = new Uri(baseUri);
            this.Credentials = credentials;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataClientSettings"/> class.
        /// </summary>
        /// <param name="baseUri">The URL address.</param>
        /// <param name="credentials">The client credentials.</param>
        public ODataClientSettings(Uri baseUri, ICredentials credentials = null)
        {
            this.BaseUri = baseUri;
            this.Credentials = credentials;
        }

        internal ODataClientSettings(ISession session)
        {
            this.BaseUri = session.Settings.BaseUri;
            this.Credentials = session.Settings.Credentials;
            this.PayloadFormat = session.Settings.PayloadFormat;
            this.RequestTimeout = session.Settings.RequestTimeout;
            this.IncludeAnnotationsInResults = session.Settings.IncludeAnnotationsInResults;
            this.IgnoreResourceNotFoundException = session.Settings.IgnoreResourceNotFoundException;
            this.IgnoreUnmappedProperties = session.Settings.IgnoreUnmappedProperties;
            this.PreferredUpdateMethod = session.Settings.PreferredUpdateMethod;
            this.MetadataDocument = session.Settings.MetadataDocument;
            this.RenewHttpConnection = session.Settings.RenewHttpConnection;
            this.UnqualifiedNameCall = session.Settings.UnqualifiedNameCall;
            this.EnumPrefixFree = session.Settings.EnumPrefixFree;
            this.NameMatchResolver = session.Settings.NameMatchResolver;
            this.OnCreateMessageHandler = session.Settings.OnCreateMessageHandler;
            this.OnApplyClientHandler = session.Settings.OnApplyClientHandler;
            this.BeforeRequest = session.Settings.BeforeRequest;
            this.AfterResponse = session.Settings.AfterResponse;
            this.OnTrace = session.Settings.OnTrace;
            this.TraceFilter = session.Settings.TraceFilter;
        }
    }
}
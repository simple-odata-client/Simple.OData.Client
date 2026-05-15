using System.Net;

namespace Simple.OData.Client;

/// <summary>
/// OData client configuration settings
/// </summary>
public class ODataClientSettings
{

	#region Private Members

	private readonly INameMatchResolver _defaultNameMatchResolver = new BestMatchResolver();
	private INameMatchResolver? _nameMatchResolver;
	private readonly IODataAdapterFactory _defaultAdapterFactory = new ODataAdapterFactory();
	private IODataAdapterFactory? _adapterFactory;
	private Uri? _baseOrRelativeUri;

	#endregion

	#region Public Properties

	/// <summary>
	/// Gets or sets the adapter factory that is used by the session
	/// </summary>
	public IODataAdapterFactory AdapterFactory
	{
		get => _adapterFactory ?? _defaultAdapterFactory;
		set => _adapterFactory = value;
	}

	/// <summary>
	/// Gets or sets the action executed after the OData request.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpResponseMessage"/>.
	/// </value>
	public Action<HttpResponseMessage>? AfterResponse { get; set; }

	/// <summary>
	/// Gets or sets the action executed after the OData request.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpResponseMessage"/>.
	/// </value>
	public Func<HttpResponseMessage, Task>? AfterResponseAsync { get; set; }

	/// <summary>
	/// Gets or sets the OData service URL.
	/// </summary>
	/// <value>
	/// The URL address.
	/// </value>
	public Uri? BaseUri
	{
		get
		{
			if (HttpClient is not null && HttpClient.BaseAddress is not null)
			{
				if (_baseOrRelativeUri is not null)
				{
					return new Uri(HttpClient.BaseAddress, _baseOrRelativeUri);
				}
				else
				{
					return HttpClient.BaseAddress;
				}
			}
			else
			{
				return _baseOrRelativeUri;
			}
		}
		set
		{
			if (value is not null && value.IsAbsoluteUri && HttpClient is not null && HttpClient.BaseAddress is not null)
			{
				throw new InvalidOperationException("Unable to set BaseUri when BaseAddress is specified on HttpClient.");
			}

			_baseOrRelativeUri = value;
		}
	}

	/// <summary>
	/// Gets or sets the BatchPayloadUriOption to use when building a batch request payload.
	/// Only available for OData V4.
	/// </summary>
	/// <value>
	/// <c>AbsoluteUri</c> (Default) to use absolute URIs for the batch payload.
	/// </value>
	/// <value>
	/// <c>AbsoluteUriUsingHostHeader</c> to use absolute URIs from the Host header for the batch payload.
	/// </value>
	/// <value>
	/// <c>RelativeUri</c> to use relative URIs for the batch payload.
	/// </value>
	public BatchPayloadUriOption BatchPayloadUriOption { get; set; } = BatchPayloadUriOption.AbsoluteUri;

	/// <summary>
	/// Gets or sets the action executed before the OData request.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpRequestMessage"/>.
	/// </value>
	public Action<HttpRequestMessage>? BeforeRequest { get; set; }

	/// <summary>
	/// Gets or sets the action executed before the OData request.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpRequestMessage"/>.
	/// </value>
	public Func<HttpRequestMessage, Task>? BeforeRequestAsync { get; set; }

	/// <summary>
	/// Gets or sets the OData client credentials.
	/// </summary>
	/// <value>
	/// The client credentials.
	/// </value>
	public ICredentials? Credentials { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="System.Net.Http.HttpClient"/> should omit type prefix for Enum values in generated URI.
	/// </summary>
	/// <value>
	/// <c>true</c> to omit type prefix for Enum values in generated URI; <c>false</c> otherwise.
	/// </value>
	public bool EnumPrefixFree { get; set; }

	/// <summary>
	/// Gets or sets external instance of HttpClient to be used when issuing OData requests.
	/// </summary>
	/// <value>
	/// The instance of <see cref="System.Net.Http.HttpClient"/>.
	/// </value>
	public HttpClient? HttpClient { get; private set; }

	/// <summary>
	/// Gets or sets a value indicating whether entry properties should be extended with the OData annotations.
	/// </summary>
	/// <value>
	/// <c>true</c> to include OData annotations in entry properties; otherwise, <c>false</c>.
	/// </value>
	public bool IncludeAnnotationsInResults { get; set; }

	/// <summary>
	/// Indicates whether navigation properties should be ignored when updating entities.
	/// </summary>
	/// <value>
	/// <c>false</c> (Default) to maintain current behavior and issue additional requests to remove 
	/// </value>
	public bool IgnoreNavigationPropertiesOnUpdate { get; set; }

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
	/// Gets or sets the number of simultaneous requests the ODataClient can send.
	/// </summary>
	/// <value>
	/// 0 (Default) to allow unlimited requests at the same time. 
	/// </value>
	/// <remarks>
	/// Set this to a value > 0 when you are seeing 429 responses to your requests.
	public int MaxConcurrentRequests { get; set; }

	/// <summary>
	/// Gets or sets the OData service metadata document. If not set, service metadata is downloaded prior to the first call to the OData service and stored in an in-memory cache.
	/// </summary>
	/// <value>
	/// The content of the service metadata document.
	/// </value>
	public string? MetadataDocument { get; set; }

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
	/// Gets or sets the action on HttpClientHandler.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpClientHandler"/>.
	/// </value>
	public Action<HttpClientHandler>? OnApplyClientHandler { get; set; }

	/// <summary>
	/// Gets or sets the HttpMessageHandler factory used by HttpClient.
	/// If not set, ODataClient creates an instance of HttpClientHandler.
	/// </summary>
	/// <value>
	/// The action on <see cref="HttpMessageHandler"/>.
	/// </value>
	public Func<HttpMessageHandler>? OnCreateMessageHandler { get; set; }

	/// <summary>
	/// Gets or sets the method that will be executed to write trace messages.
	/// </summary>
	/// <value>
	/// The trace message handler.
	/// </value>
	public Action<string, object[]>? OnTrace { get; set; }

	/// <summary>
	/// Gets or sets the OData payload format.
	/// </summary>
	/// <value>
	/// The payload format (JSON or Atom).
	/// </value>
	public ODataPayloadFormat PayloadFormat { get; set; }

	/// <summary>
	/// Gets or sets a preferred update method for OData entries. The selected method will be used wherever it's compatible with the update scenario.
	/// If not specified, PATCH is preferred due to better performance.
	/// </summary>
	/// <value>
	/// The update method (PUT or PATCH).
	/// </value>
	public ODataUpdateMethod PreferredUpdateMethod { get; set; }

	/// <summary>
	/// Gets or sets a bag of extra properties used to opt into provider-specific behaviors
	/// (e.g. <see cref="ExtraProperties.STRINGIZE_DATETIME_VALUES"/> for SAP B1 services).
	/// </summary>
	public Dictionary<string, object>? Properties { get; set; }

	/// <summary>
	/// Gets or sets the value that indicates either to read untyped properties as strings.
	/// </summary>
	/// <value>
	/// <c>true</c> (Default) to read untyped values as strings; <c>false</c> otherwise.
	/// </value>
	public bool ReadUntypedAsString { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="System.Net.Http.HttpClient"/> connection should be disposed and renewed between OData requests.
	/// </summary>
	/// <value>
	/// <c>true</c> to create a new <see cref="System.Net.Http.HttpClient"/> instance for each request; <c>false</c> to reuse <see cref="System.Net.Http.HttpClient"/> between requests.
	/// </value>
	public bool RenewHttpConnection { get; set; }

	/// <summary>
	/// Gets or sets the handler that executes <see cref="HttpRequestMessage"/> and returns <see cref="HttpResponseMessage"/>.
	/// Can be used to mock OData request execution without sending messages to the server.
	/// </summary>
	/// <value>
	/// The <see cref="HttpRequestMessage"/> executor.
	/// </value>
	public Func<HttpRequestMessage, Task<HttpResponseMessage>>? RequestExecutor { get; set; }

	/// <summary>
	/// Gets or sets the time period to wait before the request times out.
	/// </summary>
	/// <value>
	/// The timeout.
	/// </value>
	public TimeSpan RequestTimeout { get; set; }
	
	/// <summary>
	/// Gets or sets the filter of information that is written to trace messages.
	/// </summary>
	/// <value>
	/// The <see cref="ODataTrace"/> filter value.
	/// </value>
	public ODataTrace TraceFilter { get; set; }

	/// <summary>
	/// Gets the <see cref="ITypeCache"/> associated with the uri, used to register converters and dynamic types.
	/// </summary>
	public ITypeCache TypeCache
	{
		get
		{
			if (BaseUri is null)
			{
				throw new InvalidOperationException("Assign BaseUri before accessing TypeCache");
			}

			return TypeCaches.TypeCache(BaseUri.AbsoluteUri, NameMatchResolver);
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="System.Net.Http.HttpClient"/> should omit namespaces for function and action calls in generated URI.
	/// </summary>
	/// <value>
	/// <c>true</c> to omit namespaces for function and action calls in generated URI; <c>false</c> otherwise.
	/// </value>
	public bool UnqualifiedNameCall { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether reference uris should be written as absolute uris instead of relative uris.
	/// </summary>
	/// <value>
	/// <c>true</c> to extend reference links to absolute uris; otherwise, <c>false</c>.
	/// </value>
	/// <summary>
	public bool UseAbsoluteReferenceUris { get; set; }

	/// <summary>
	/// Gets or sets validations to perform. Default value is <see cref="T:Microsoft.OData.ValidationKinds.All" />,
	/// </summary>
	public ValidationKinds Validations { get; set; } = ValidationKinds.All;

	/// <summary>
	/// Gets or sets the source of the message of web request exceptions.
	/// </summary>
	/// <value>
	/// <c>ReasonPhrase</c> (Default) to output the reason phrase of the HTTP request message.
	/// </value>
	/// <value>
	/// <c>ResponseContent</c> (Default) to output the content of the HTTP request message.
	/// </value>
	/// /// <value>
	/// <c>Both</c> (Default) to output both the reason phrase and the content of the HTTP request message.
	/// </value>
	public WebRequestExceptionMessageSource WebRequestExceptionMessageSource { get; set; }

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientSettings" /> class.
	/// </summary>
	public ODataClientSettings()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientSettings" /> class.
	/// </summary>
	/// <param name="baseUri">The URL address.</param>
	/// <param name="credentials">The client credentials.</param>
	public ODataClientSettings(Uri baseUri, ICredentials? credentials = null)
		: this()
	{
		BaseUri = baseUri;
		Credentials = credentials;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientSettings" /> class.
	/// </summary>
	/// <param name="baseUri">The URL address.</param>
	/// <param name="credentials">The client credentials.</param>
	[Obsolete("Use of string-typed baseUri is deprecated, please use Uri-typed baseUri instead.")]
	public ODataClientSettings(string baseUri, ICredentials? credentials = null)
		: this(new Uri(baseUri), credentials)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientSettings" /> class.
	/// </summary>
	/// <param name="httpClient">The instance of <see cref="System.Net.Http.HttpClient"/>.</param>
	/// <param name="relativeUri">The URL address.</param>
	/// <param name="credentials">The client credentials.</param>
	public ODataClientSettings(HttpClient httpClient, Uri? relativeUri = null, ICredentials? credentials = null)
		: this(relativeUri, credentials)
	{
		HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

		if (httpClient.BaseAddress is not null && !httpClient.BaseAddress.IsAbsoluteUri)
		{
			throw new ArgumentException("HttpClient BaseAddress must be an absolute URI", nameof(httpClient));
		}

		if (relativeUri is not null && relativeUri.IsAbsoluteUri)
		{
			throw new ArgumentException("Must be a relative URI", nameof(relativeUri));
		}

		if (httpClient.BaseAddress is null && relativeUri is not null)
		{
			throw new ArgumentException("Must not specify relative URI when HttpClient has no BaseAddress", nameof(relativeUri));
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ODataClientSettings" /> class using an existing <see cref="ISession" />.
	/// </summary>
	/// <param name="session">The <see cref="ISession" /> instance to clone the settings from.</param>
	internal ODataClientSettings(ISession session)
	{
		AdapterFactory = session.Settings.AdapterFactory;
		AfterResponse = session.Settings.AfterResponse;
		AfterResponseAsync = session.Settings.AfterResponseAsync;
		BaseUri = session.Settings.BaseUri;
		BatchPayloadUriOption = session.Settings.BatchPayloadUriOption;
		BeforeRequest = session.Settings.BeforeRequest;
		BeforeRequestAsync = session.Settings.BeforeRequestAsync;
		Credentials = session.Settings.Credentials;
		EnumPrefixFree = session.Settings.EnumPrefixFree;
		HttpClient = session.Settings.HttpClient;
		IgnoreNavigationPropertiesOnUpdate = session.Settings.IgnoreNavigationPropertiesOnUpdate;
		IgnoreResourceNotFoundException = session.Settings.IgnoreResourceNotFoundException;
		IgnoreUnmappedProperties = session.Settings.IgnoreUnmappedProperties;
		IncludeAnnotationsInResults = session.Settings.IncludeAnnotationsInResults;
		MaxConcurrentRequests = session.Settings.MaxConcurrentRequests;
		MetadataDocument = session.Settings.MetadataDocument;
		NameMatchResolver = session.Settings.NameMatchResolver;
		OnApplyClientHandler = session.Settings.OnApplyClientHandler;
		OnCreateMessageHandler = session.Settings.OnCreateMessageHandler;
		OnTrace = session.Settings.OnTrace;
		PayloadFormat = session.Settings.PayloadFormat;
		PreferredUpdateMethod = session.Settings.PreferredUpdateMethod;
		Properties = session.Settings.Properties is null ? null : new Dictionary<string, object>(session.Settings.Properties);
		ReadUntypedAsString = session.Settings.ReadUntypedAsString;
		RenewHttpConnection = session.Settings.RenewHttpConnection;
		RequestExecutor = session.Settings.RequestExecutor;
		RequestTimeout = session.Settings.RequestTimeout;
		TraceFilter = session.Settings.TraceFilter;
		UnqualifiedNameCall = session.Settings.UnqualifiedNameCall;
		UseAbsoluteReferenceUris = session.Settings.UseAbsoluteReferenceUris;
		WebRequestExceptionMessageSource = session.Settings.WebRequestExceptionMessageSource;
	}

	#endregion

	/// <summary>
	/// Keys for the <see cref="ODataClientSettings.Properties"/> bag, used to opt into
	/// provider-specific behaviors.
	/// </summary>
	public static class ExtraProperties
	{
		/// <summary>
		/// When set to a truthy value, the client renders DateTime literals as single-quoted
		/// strings instead of the standard OData datetime syntax (required by some SAP B1
		/// services that do not accept proper datetimes).
		/// </summary>
		public const string STRINGIZE_DATETIME_VALUES = "StringizeDatetimeValues";
	}
}

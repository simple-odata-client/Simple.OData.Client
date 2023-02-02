using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Metadata;

namespace Simple.OData.Client;

internal class Session : ISession
{
	private IODataAdapter? _adapter;
	private HttpConnection? _httpConnection;

	private Session(Uri baseUri, string metadataString) : this(new ODataClientSettings
	{
		BaseUri = baseUri,
		MetadataDocument = metadataString
	})
	{
	}

	private Session(ODataClientSettings settings)
	{
		if (settings.BaseUri == null || string.IsNullOrEmpty(settings.BaseUri.AbsoluteUri))
		{
			throw new InvalidOperationException("Unable to create client session with no URI specified.");
		}

		Settings = settings;

		if (!string.IsNullOrEmpty(Settings.MetadataDocument))
		{
			// Create as early as possible as most unit tests require this and also makes it simpler when assigning a static document
			MetadataCache = InitializeStaticMetadata(Settings.MetadataDocument);
		}

		_metadataCacheFactory = settings.MetadataCacheFactory ?? new EdmMetadataCacheFactory();
	}

	public IODataAdapter Adapter
	{
		get
		{
			if (_adapter == null)
			{
				lock (this)
				{
					if (_adapter == null)
					{
						_adapter = MetadataCache.GetODataAdapter(this);
					}
				}
			}

			return _adapter;
		}
	}

	public IMetadata Metadata => Adapter.GetMetadata();

	public IEdmMetadataCache? MetadataCache { get; private set; }

	public ODataClientSettings Settings { get; }

	public ITypeCache TypeCache => TypeCaches.TypeCache(Settings.BaseUri.AbsoluteUri, Settings.NameMatchResolver);

	public void Dispose()
	{
		lock (this)
		{
			if (_httpConnection != null)
			{
				_httpConnection.Dispose();
				_httpConnection = null;
			}
		}
	}

	private readonly SemaphoreSlim _initializeSemaphore = new(1);
	private readonly IEdmMetadataCacheFactory _metadataCacheFactory;
	public async Task Initialize(CancellationToken cancellationToken)
	{
		// Just allow one schema request at a time, unlikely to be much contention but avoids multiple requests for same endpoint.
		await _initializeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

		try
		{
			if (MetadataCache == null)
			{
				MetadataCache = await InitializeMetadataCache(cancellationToken)
					.ConfigureAwait(false);
			}

			if (_adapter == null)
			{
				_adapter = MetadataCache.GetODataAdapter(this);
			}
		}
		finally
		{
			_initializeSemaphore.Release();
		}
	}

	public void Trace(string message, params object[] messageParams)
	{
		Settings.OnTrace?.Invoke(message, messageParams);
	}

	public void ClearMetadataCache()
	{
		var metadataCache = MetadataCache;
		if (metadataCache != null)
		{
			_metadataCacheFactory.Clear(metadataCache.Key);
			MetadataCache = null;
			_adapter = null;
		}
	}

	public async Task<IODataAdapter> ResolveAdapterAsync(CancellationToken cancellationToken)
	{
		await Initialize(cancellationToken).ConfigureAwait(false);

		if (Settings.PayloadFormat == ODataPayloadFormat.Unspecified)
		{
			Settings.PayloadFormat = Adapter.DefaultPayloadFormat;
		}

		return Adapter;
	}

	public HttpConnection GetHttpConnection()
	{
		if (_httpConnection == null)
		{
			lock (this)
			{
				if (_httpConnection == null)
				{
					_httpConnection = new HttpConnection(Settings);
				}
			}
		}

		return _httpConnection;
	}

	internal static Session FromSettings(ODataClientSettings settings)
	{
		return new Session(settings);
	}

	internal static Session FromMetadata(Uri baseUri, string metadataString)
	{
		return new Session(baseUri, metadataString);
	}

	private async Task<HttpResponseMessage> SendMetadataRequestAsync(CancellationToken cancellationToken)
	{
		var request = new ODataRequest(RestVerbs.Get, this, ODataLiteral.Metadata);
		return await new RequestRunner(this).ExecuteRequestAsync(request, cancellationToken).ConfigureAwait(false);
	}

	private IEdmMetadataCache InitializeStaticMetadata(string metadata)
	{
		return _metadataCacheFactory.GetOrAdd(
			Settings.BaseUri.AbsoluteUri,
			uri => CreateMdc(uri, metadata));
	}

	private async Task<IEdmMetadataCache> InitializeMetadataCache(CancellationToken cancellationToken)
	{
		return await _metadataCacheFactory.GetOrAddAsync(
			Settings.BaseUri.AbsoluteUri,
			async uri =>
			{
				var metadata = await ResolveMetadataAsync(cancellationToken).ConfigureAwait(false);
				return CreateMdc(uri, metadata);
			})
			.ConfigureAwait(false);
	}

	private async Task<string> ResolveMetadataAsync(CancellationToken cancellationToken)
	{
		if (!string.IsNullOrEmpty(Settings.MetadataDocument))
		{
			return Settings.MetadataDocument;
		}

		var response = await SendMetadataRequestAsync(cancellationToken).ConfigureAwait(false);
		var metadataDocument = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		return metadataDocument;
	}

	private IEdmMetadataCache CreateMdc(string key, string metadata)
	{
		return new EdmMetadataCache(key, metadata, TypeCaches.TypeCache(key, Settings.NameMatchResolver));
	}
}

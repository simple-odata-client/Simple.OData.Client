using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    class Session : ISession
    {
        private IODataAdapter _adapter;
        private HttpConnection _httpConnection;

        public ODataClientSettings Settings { get; }
        public MetadataCache MetadataCache { get; private set; }

        private Session(Uri baseUri, string metadataString)
        {
            this.Settings = new ODataClientSettings();
            this.Settings.BaseUri = baseUri;
            this.MetadataCache = MetadataCache.GetOrAdd(baseUri.AbsoluteUri, uri => new MetadataCache(uri, metadataString));
        }

        private Session(ODataClientSettings settings)
        {
            if (settings.BaseUri == null || string.IsNullOrEmpty(settings.BaseUri.AbsoluteUri))
                throw new InvalidOperationException("Unable to create client session with no URI specified");

            this.Settings = settings;
        }

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

        public void Trace(string message, params object[] messageParams)
        {
            if (this.Settings.OnTrace != null)
            {
                this.Settings.OnTrace(message, messageParams);
            }
        }

        public void ClearMetadataCache()
        {
            var metadataCache = this.MetadataCache;
            if (metadataCache != null)
            {
                MetadataCache.Clear(metadataCache.Key);
                this.MetadataCache = null;
            }
        }

        public async Task<IODataAdapter> ResolveAdapterAsync(CancellationToken cancellationToken)
        {
            if (this.MetadataCache == null)
            {
                string metadataDocument = Settings.MetadataDocument;
                if (string.IsNullOrEmpty(metadataDocument))
                {
                    metadataDocument = await ResolveMetadataAsync(cancellationToken).ConfigureAwait(false);
                }
                this.MetadataCache =
                    MetadataCache.GetOrAdd(
                        this.Settings.BaseUri.AbsoluteUri,
                        uri =>
                        {
                            var cache = new MetadataCache(uri, metadataDocument);
                            return cache;
                        });
            }

            if (this.Settings.PayloadFormat == ODataPayloadFormat.Unspecified)
                this.Settings.PayloadFormat = this.Adapter.DefaultPayloadFormat;

            return this.Adapter;
        }

        private async Task<string> ResolveMetadataAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.Settings.MetadataDocument))
            {
                var response = await SendMetadataRequestAsync(cancellationToken).ConfigureAwait(false);
                var metadataDocument = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return metadataDocument;
            }
            else
            {
                return this.Settings.MetadataDocument;
            }
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
                            _adapter = this.MetadataCache.GetODataAdapter(this);
                    }
                }
                return _adapter;
            }
        }

        public IMetadata Metadata
        {
            get { return this.Adapter.GetMetadata(); }
        }

        public HttpConnection GetHttpConnection()
        {
            if (_httpConnection == null)
            {
                lock (this)
                {
                    if (_httpConnection == null)
                        _httpConnection = new HttpConnection(this.Settings);
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
    }
}

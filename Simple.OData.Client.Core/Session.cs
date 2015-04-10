using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    class Session : ISession
    {
        private readonly AdapterFactory _adapterFactory;
        private Func<IODataAdapter> _createAdapter;
        private IODataAdapter _adapter;
        private readonly SimpleDictionary<object, IDictionary<string, object>> _entryMap = new SimpleDictionary<object, IDictionary<string, object>>(); 

        public ODataClientSettings Settings { get; private set; }
        public MetadataCache MetadataCache { get; private set; }
        public IPluralizer Pluralizer { get; internal set; }

        private Session(string urlBase, string metadataString)
        {
            _adapterFactory = new AdapterFactory(this);
            _createAdapter = () => _adapterFactory.ParseMetadata(metadataString);

            this.Settings = new ODataClientSettings();
            this.Settings.UrlBase = urlBase;
            this.MetadataCache = MetadataCache.Instances.GetOrAdd(urlBase, new MetadataCache());
            this.MetadataCache.SetMetadataString(metadataString);
            this.Pluralizer = new SimplePluralizer();
        }

        private Session(string urlBase, ICredentials credentials, ODataPayloadFormat payloadFormat)
        {
            _adapterFactory = new AdapterFactory(this);
            _createAdapter = () => _adapterFactory.ParseMetadata(this.MetadataCache.MetadataAsString);

            this.Settings = new ODataClientSettings();
            this.Settings.UrlBase = urlBase;
            this.Settings.Credentials = credentials;
            this.Settings.PayloadFormat = payloadFormat;
            this.MetadataCache = MetadataCache.Instances.GetOrAdd(urlBase, new MetadataCache());
            this.Pluralizer = new SimplePluralizer();
        }

        private Session(ODataClientSettings settings)
        {
            _adapterFactory = new AdapterFactory(this);
            _createAdapter = () => _adapterFactory.ParseMetadata(this.MetadataCache.MetadataAsString);

            this.Settings = settings;
            this.MetadataCache = MetadataCache.Instances.GetOrAdd(this.Settings.UrlBase, new MetadataCache());
            this.Pluralizer = new SimplePluralizer();
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
            MetadataCache.Instances.Remove(MetadataCache.Instances.Single(x => x.Value == this.MetadataCache).Key);
        }

        public async Task<IODataAdapter> ResolveAdapterAsync(CancellationToken cancellationToken)
        {
            if (!this.MetadataCache.IsResolved())
            {
                var response = await _adapterFactory.SendMetadataRequestAsync(cancellationToken);
                this.MetadataCache.SetMetadataString(await _adapterFactory.GetMetadataAsStringAsync(response));

                var adapter = await _adapterFactory.CreateAdapterAsync(response);
                _createAdapter = () => adapter;
            }

            if (this.Settings.PayloadFormat == ODataPayloadFormat.Unspecified)
                this.Settings.PayloadFormat = this.Adapter.DefaultPayloadFormat;

            return this.Adapter;
        }

        public IODataAdapter Adapter
        {
            get
            {
                if (_adapter == null)
                    _adapter = _createAdapter();
                return _adapter;
            }
        }

        public IMetadata Metadata
        {
            get { return this.Adapter.GetMetadata(); }
        }

        internal SimpleDictionary<object, IDictionary<string, object>> EntryMap { get { return _entryMap; } } 

        internal static Session FromSettings(ODataClientSettings settings)
        {
            return new Session(settings);
        }

        public static Session FromMetadata(string urlBase, string metadataString)
        {
            return new Session(urlBase, metadataString);
        }
    }
}

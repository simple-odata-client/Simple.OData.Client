using System;
using System.Collections.Generic;

namespace Simple.OData.Client
{
    class MetadataCache
    {
        static readonly object metadataLock = new object();
        static readonly IDictionary<string, MetadataCache> _instances = new Dictionary<string, MetadataCache>();

        public static void Clear()
        {
            lock (metadataLock)
            {
                _instances.Clear();
            }
        }

        public static void Clear(string key)
        {
            lock (metadataLock)
            {
                _instances.Remove(key);
            }
        }

        public static MetadataCache GetOrAdd(string key, Func<string, MetadataCache> valueFactory)
        {
            lock (metadataLock)
            {
                MetadataCache found;
                if (!_instances.TryGetValue(key, out found))
                {
                    _instances[key] = found = valueFactory(key);
                }

                return found;
            }
        }

        private readonly string _key;
        private Func<ISession, IODataAdapter> _adapterFactory;

        private readonly string _metadataDocument;
        
        public MetadataCache(string key, string metadataDocument)
        {
            if (String.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (String.IsNullOrWhiteSpace(metadataDocument))
                throw new ArgumentNullException("metadataDocument");

            _key = key;
            _metadataDocument = metadataDocument;
            _adapterFactory = new AdapterFactory().CreateAdapter(metadataDocument);
        }

        public string Key
        {
            get
            {
                return _key;
            }
        }

        public string MetadataDocument
        {
            get
            {
                return _metadataDocument;
            }
        }

        public IODataAdapter GetODataAdapter(ISession session)
        {
            return _adapterFactory(session);
        }
    }
}

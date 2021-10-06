using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
    class EdmMetadataCache
    {
        static readonly ConcurrentDictionary<string, EdmMetadataCache> _instances = new ConcurrentDictionary<string, EdmMetadataCache>();
        static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public static void Clear()
        {
            _instances.Clear();
            // NOTE: Is this necessary, if so should we wipe the ITypeCache constructors?
            DictionaryExtensions.ClearCache();
        }

        public static void Clear(string key)
        {
            _instances.TryRemove(key, out var md);
        }

        public static EdmMetadataCache GetOrAdd(string key, Func<string, EdmMetadataCache> valueFactory)
        {
            return _instances.GetOrAdd(key, valueFactory);
        }

        public static async Task<EdmMetadataCache> GetOrAddAsync(string key, Func<string, Task<EdmMetadataCache>> valueFactory)
        {
            // Cheaper to check first before we do the remote call
            if (_instances.TryGetValue(key, out var found))
            {
                return found;
            }

            // Just allow one schema request at a time, unlikely to be much contention but avoids multiple requests for same endpoint.
            await semaphore.WaitAsync().ConfigureAwait(false);

            if (_instances.TryGetValue(key, out found))
            {
                return found;
            }

            try
            {
                // Can't easily lock, could introduce a semaphoreSlim but not sure if it's worth it.
                found = await valueFactory(key).ConfigureAwait(false);

                return _instances.GetOrAdd(key, found);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private readonly ITypeCache typeCache;

        public EdmMetadataCache(string key, string metadataDocument, ITypeCache typeCache)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(metadataDocument))
                throw new ArgumentNullException(nameof(metadataDocument));
            this.typeCache = typeCache;

            Key = key;
            MetadataDocument = metadataDocument;
        }

        public string Key { get; }

        public string MetadataDocument { get; }

        public IODataAdapter GetODataAdapter(ISession session)
        {
            return session.Settings.AdapterFactory.CreateAdapterLoader(MetadataDocument, typeCache)(session);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.Metadata;

internal class EdmMetadataCacheFactory : IEdmMetadataCacheFactory
{
	private static readonly ConcurrentDictionary<string, IEdmMetadataCache> _instances = new();
	private static readonly SemaphoreSlim semaphore = new(1);

	public static void Clear()
	{
		_instances.Clear();
		// NOTE: Is this necessary, if so should we wipe the ITypeCache constructors?
		DictionaryExtensions.ClearCache();
	}

	public void Clear(string key)
	{
		_instances.TryRemove(key, out _);
	}

	public IEdmMetadataCache GetOrAdd(string key, Func<string, IEdmMetadataCache> valueFactory)
	{
		return _instances.GetOrAdd(key, valueFactory);
	}

	public async Task<IEdmMetadataCache> GetOrAddAsync(string key, Func<string, Task<IEdmMetadataCache>> valueFactory)
	{
		// Cheaper to check first before we do the remote call
		if (_instances.TryGetValue(key, out var found))
		{
			return found;
		}

		// Just allow one schema request at a time, unlikely to be much contention but avoids multiple requests for same endpoint.
		await semaphore
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			if (_instances.TryGetValue(key, out found))
			{
				return found;
			}

			found = await valueFactory(key)
				.ConfigureAwait(false);

			return _instances.GetOrAdd(key, found);
		}
		finally
		{
			semaphore.Release();
		}
	}
}
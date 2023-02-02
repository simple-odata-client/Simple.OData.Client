using System;
using System.Threading.Tasks;

namespace Simple.OData.Client.Metadata;

public interface IEdmMetadataCacheFactory
{
	void Clear(string key);
	IEdmMetadataCache GetOrAdd(string key, Func<string, IEdmMetadataCache> valueFactory);
	Task<IEdmMetadataCache> GetOrAddAsync(string key, Func<string, Task<IEdmMetadataCache>> valueFactory);
}
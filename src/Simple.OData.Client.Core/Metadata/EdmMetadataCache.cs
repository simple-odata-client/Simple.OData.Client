using System;

namespace Simple.OData.Client.Metadata;

internal class EdmMetadataCache: IEdmMetadataCache
{
	private readonly ITypeCache typeCache;

	public EdmMetadataCache(string key, string metadataDocument, ITypeCache typeCache)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentNullException(nameof(key));
		}

		if (string.IsNullOrWhiteSpace(metadataDocument))
		{
			throw new ArgumentNullException(nameof(metadataDocument));
		}
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
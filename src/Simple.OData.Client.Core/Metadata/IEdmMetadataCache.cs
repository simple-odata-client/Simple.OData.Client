namespace Simple.OData.Client.Metadata;

public interface IEdmMetadataCache
{
	string Key { get; }
	string MetadataDocument { get; }
	IODataAdapter GetODataAdapter(ISession session);
}
using Microsoft.OData;

namespace Simple.OData.Client.V4.Adapter;

public class ResourceProperties(ODataResource resource)
{
	public ODataResource Resource { get; } = resource;
	public string TypeName { get; set; }
	public IEnumerable<ODataProperty> PrimitiveProperties => (IEnumerable<ODataProperty>)Resource.Properties;
	public IDictionary<string, ODataCollectionValue> CollectionProperties { get; set; }
	public IDictionary<string, ODataResource> StructuralProperties { get; set; }
}

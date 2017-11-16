using System.Collections.Generic;
using System.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.V4.Adapter
{
    public class ResourceProperties
    {
        public ODataResource Resource { get; }
        public string TypeName { get; set; }
        public IEnumerable<ODataProperty> PrimitiveProperties => this.Resource.Properties;
        public IDictionary<string, ODataCollectionValue> CollectionProperties { get; set; }
        public IDictionary<string, ODataResource> StructuralProperties { get; set; }

        public ResourceProperties(ODataResource resource)
        {
            this.Resource = resource;
        }
    }
}
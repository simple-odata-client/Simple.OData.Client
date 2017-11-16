using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class ODataSchemaTests
    {
        private const string _serviceUrl = "http://services.odata.org/{0}/OData/OData.svc/";

        [Fact]
        public async Task ReadMetadataV3()
        {
            var client = new ODataClient(string.Format(_serviceUrl, "V3"));
            var metadata = await client.GetMetadataAsync<Microsoft.Data.Edm.IEdmModel>();
            Assert.Equal(12, metadata.SchemaElements.Count());
        }

        [Fact]
        public async Task ReadMetadataV3AsDynamic()
        {
            var client = new ODataClient(string.Format(_serviceUrl, "V3"));
            dynamic metadata = await client.GetMetadataAsync();
            Assert.Equal(12, (metadata.SchemaElements as IEnumerable<dynamic>).Count());
        }

        [Fact]
        public async Task ReadMetadataV4()
        {
            var client = new ODataClient(string.Format(_serviceUrl, "V4"));
            var metadata = await client.GetMetadataAsync<Microsoft.OData.Edm.IEdmModel>();
            Assert.Equal(12, metadata.SchemaElements.Count());
        }

        [Fact]
        public async Task ReadMetadataV4AsDynamic()
        {
            var client = new ODataClient(string.Format(_serviceUrl, "V4"));
            dynamic metadata = await client.GetMetadataAsync();
            Assert.Equal(12, (metadata.SchemaElements as IEnumerable<dynamic>).Count());
        }
    }
}
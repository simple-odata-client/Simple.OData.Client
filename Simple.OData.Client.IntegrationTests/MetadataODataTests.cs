using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class MetadataODataTestsV4Json : MetadataODataTests
    {
        public MetadataODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class MetadataODataTests : ODataTestBase
    {
        protected MetadataODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task FilterWithMetadataDocument()
        {
            var metadataDocument = await _client.GetMetadataDocumentAsync();
            ODataClient.ClearMetadataCache();
            var settings = new ODataClientSettings()
            {
                BaseUri = _serviceUri,
                PayloadFormat = _payloadFormat,
                MetadataDocument = metadataDocument,
            };
            var client = new ODataClient(settings);
            var products = await client
                .For("Products")
                .Filter("Name eq 'Milk'")
                .FindEntriesAsync();
            Assert.Equal("Milk", products.Single()["Name"]);
        }
    }
}

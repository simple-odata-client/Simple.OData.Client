using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class MetadataODataTestsV2Atom : MetadataODataTests
    {
        public MetadataODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
    }

    public class MetadataODataTestsV2Json : MetadataODataTests
    {
        public MetadataODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
    }

    public class MetadataODataTestsV3Atom : MetadataODataTests
    {
        public MetadataODataTestsV3Atom() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Atom, 3) { }
    }

    public class MetadataODataTestsV3Json : MetadataODataTests
    {
        public MetadataODataTestsV3Json() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3) { }
    }

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

        [Fact]
        public async Task ParallelBootstrapping()
        {
            ODataClient.ClearMetadataCache();
            int metadataCallsCount = 0;
            var settings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                BeforeRequest = _ => metadataCallsCount++
            };
            var client = new ODataClient(settings);

            await Task.WhenAll(
                // Dispatch 10 calls in parallel
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync(),
                client.GetMetadataAsStringAsync()
            );

            Assert.Equal(1, metadataCallsCount);
        }
    }
}
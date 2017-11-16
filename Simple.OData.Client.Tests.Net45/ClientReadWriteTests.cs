using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Edm;
using Xunit;

namespace Simple.OData.Client.Tests
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class ClientReadWriteTests : TestBase
    {
        [Fact]
        public async Task InsertEntryWithResult()
        {
            var product = await _client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 18m } }, true);

            Assert.Equal("Test1", product["ProductName"]);
        }

        [Fact]
        public async Task InsertEntryNoResult()
        {
            var product = await _client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 18m } }, false);

            Assert.Null(product);
        }

        [Fact]
        public async Task InsertEntrySubcollection()
        {
            var ship = await _client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);

            Assert.Equal("Test1", ship["ShipName"]);
        }

        [Fact]
        public async Task InsertUsingModifiedSchema()
        {
            await AssertThrowsAsync<Microsoft.Data.OData.ODataException>(async () => 
                await _client.InsertEntryAsync("Customers", new Entry() { { "CompanyName", null } }));

            var metadataDocument = await _client.GetMetadataDocumentAsync();
            metadataDocument = metadataDocument.Replace(@"Name=""CompanyName"" Type=""Edm.String"" Nullable=""false""", @"Name=""CompanyName"" Type=""Edm.String"" Nullable=""true""");
            ODataClient.ClearMetadataCache();
            var settings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                MetadataDocument = metadataDocument,
            };
            var client = new ODataClient(settings);
            var model = await client.GetMetadataAsync<IEdmModel>();
            var type = model.FindDeclaredType("NorthwindModel.Customer");
            var property = (type as IEdmEntityType).DeclaredProperties.Single(x => x.Name == "CompanyName");
            Assert.Equal(true, property.Type.IsNullable);

            await AssertThrowsAsync<WebRequestException>(async () => 
                await client.InsertEntryAsync("Customers", new Entry() { { "CompanyName", null } }));

            ODataClient.ClearMetadataCache();
        }

        [Fact]
        public async Task UpdateEntryWithResult()
        {
            var key = new Entry() { { "ProductID", 1 } };
            var product = await _client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, true);

            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public async Task UpdateEntryNoResult()
        {
            var key = new Entry() { { "ProductID", 1 } };
            var product = await _client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, false);
            Assert.Null(product);

            product = await _client.GetEntryAsync("Products", key);
            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public async Task UpdateEntrySubcollection()
        {
            var ship = await _client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);
            var key = new Entry() { { "TransportID", ship["TransportID"] } };
            await _client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } });

            ship = await _client.GetEntryAsync("Transport", key);
            Assert.Equal("Test2", ship["ShipName"]);
        }

        [Fact]
        public async Task UpdateEntrySubcollectionWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IncludeAnnotationsInResults = true,
            };
            var client = new ODataClient(clientSettings);
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);
            var key = new Entry() { { "TransportID", ship["TransportID"] } };
            await client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } });

            ship = await client.GetEntryAsync("Transport", key);
            Assert.Equal("Test2", ship["ShipName"]);
        }

        [Fact]
        public async Task DeleteEntry()
        {
            var product = await _client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test3" }, { "UnitPrice", 18m } }, true);
            product = await _client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.NotNull(product);

            await _client.DeleteEntryAsync("Products", product);

            product = await _client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteEntrySubCollection()
        {
            var ship = await _client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true);
            ship = await _client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.NotNull(ship);

            await _client.DeleteEntryAsync("Transport", ship);

            ship = await _client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.Null(ship);
        }

        [Fact]
        public async Task DeleteEntrySubCollectionWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IncludeAnnotationsInResults = true,
            };
            var client = new ODataClient(clientSettings);
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true);
            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.NotNull(ship);

            await client.DeleteEntryAsync("Transport", ship);

            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.Null(ship);
        }

        [Fact]
        public async Task LinkEntry()
        {
            var category = await _client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test4" } }, true);
            var product = await _client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test5" } }, true);

            await _client.LinkEntryAsync("Products", product, "Category", category);

            product = await _client.FindEntryAsync("Products?$filter=ProductName eq 'Test5'");
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);
        }

        [Fact]
        public async Task UnlinkEntry()
        {
            var category = await _client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test6" } }, true);
            var product = await _client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test7" }, { "CategoryID", category["CategoryID"] } }, true);
            product = await _client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'");
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);

            await _client.UnlinkEntryAsync("Products", product, "Category");

            product = await _client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'");
            Assert.Null(product["CategoryID"]);
        }
    }
}

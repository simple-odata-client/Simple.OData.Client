using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.BasicApi
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class ClientReadWriteTests : TestBase
    {
        [Fact]
        public async Task InsertEntryWithResult()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 18m } }, true);

            Assert.Equal("Test1", product["ProductName"]);
        }

        [Fact]
        public async Task InsertEntryNoResult()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 18m } }, false);

            Assert.Null(product);
        }

        [Fact]
        public async Task InsertEntrySubcollection()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);

            Assert.Equal("Test1", ship["ShipName"]);
        }

        [Fact]
        public async Task UpdateEntryWithResult()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var key = new Entry() { { "ProductID", 1 } };
            var product = await client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, true);

            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public async Task UpdateEntryNoResult()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var key = new Entry() { { "ProductID", 1 } };
            var product = await client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, false);
            Assert.Null(product);

            product = await client.GetEntryAsync("Products", key);
            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public async Task UpdateEntrySubcollection()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);
            var key = new Entry() { { "TransportID", ship["TransportID"] } };
            await client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } });

            ship = await client.GetEntryAsync("Transport", key);
            Assert.Equal("Test2", ship["ShipName"]);
        }

        [Fact]
        public async Task UpdateEntrySubcollectionWithAnnotations()
        {
            var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true);
            var key = new Entry() { { "TransportID", ship["TransportID"] } };
            await client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } });

            ship = await client.GetEntryAsync("Transport", key);
            Assert.Equal("Test2", ship["ShipName"]);
        }

        [Fact]
        public async Task DeleteEntry()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test3" }, { "UnitPrice", 18m } }, true);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.NotNull(product);

            await client.DeleteEntryAsync("Products", product);

            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteEntrySubCollection()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true);
            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.NotNull(ship);

            await client.DeleteEntryAsync("Transport", ship);

            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.Null(ship);
        }

        [Fact]
        public async Task DeleteEntrySubCollectionWithAnnotations()
        {
            var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
            var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true);
            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.NotNull(ship);

            await client.DeleteEntryAsync("Transport", ship);

            ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]);
            Assert.Null(ship);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var category = await client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test4" } }, true);
            var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test5" } }, true);

            await client.LinkEntryAsync("Products", product, "Category", category);

            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test5'");
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnlinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var category = await client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test6" } }, true);
            var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test7" }, { "CategoryID", category["CategoryID"] } }, true);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'");
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);

            await client.UnlinkEntryAsync("Products", product, "Category");

            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'");
            Assert.Null(product["CategoryID"]);
        }
    }
}

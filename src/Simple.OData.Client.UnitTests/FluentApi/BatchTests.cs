using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class BatchTests : TestBase
    {
        [Fact]
        public async Task Success()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 10m } }, false);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 20m } }, false);
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test1'");
            Assert.NotNull(product);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test2'");
            Assert.NotNull(product);
        }

        [Fact]
        public async Task EmptyBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            await batch.ExecuteAsync();
        }

        [Fact]
        public async Task ReadOnlyBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;
            var batch = new ODataBatch(settings);
            batch += async c => product = await c.FindEntryAsync("Products");
            await batch.ExecuteAsync();

            Assert.NotNull(product);
        }

        [Fact]
        public async Task NestedBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch1 = new ODataBatch(settings);
            batch1 += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 10m } }, false);
            batch1 += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 20m } }, false);

            var batch2 = new ODataBatch(settings);
            batch2 += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test3" }, { "UnitPrice", 30m } }, false);
            batch2 += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test4" }, { "UnitPrice", 40m } }, false);
            await batch2.ExecuteAsync();
            
            await batch1.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test1'");
            Assert.NotNull(product);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test2'");
            Assert.NotNull(product);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.NotNull(product);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test4'");
            Assert.NotNull(product);
        }

        [Fact]
        public async Task SuccessWithResults()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;

            var batch = new ODataBatch(settings);
            batch += async x => { product1 = await x.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 10m }}); };
            batch += async x => { product2 = await x.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 20m }}); }; 
            await batch.ExecuteAsync();

            Assert.NotNull(product1["ProductID"]);
            Assert.NotNull(product2["ProductID"]);

            var client = new ODataClient(settings);
            product1 = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test1'");
            Assert.NotNull(product1);
            product2 = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test2'");
            Assert.NotNull(product2);
        }

        [Fact]
        public async Task PartialFailures()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 10m } }, false);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 10m }, { "SupplierID", 0xFFFF } }, false);

            try
            {
                await batch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                Assert.NotNull(exception.Response);
            }
        }

        [Fact]
        public async Task AllFailures()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "UnitPrice", 10m } }, false);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "UnitPrice", 20m } }, false);

            try
            {
                await batch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                Assert.NotNull(exception.Response);
            }
        }

        [Fact]
        public async Task MultipleUpdateEntrySingleBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;

            var batch = new ODataBatch(settings);
            batch += async c => product = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test11" }, { "UnitPrice", 21m } });
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += c => c.UpdateEntryAsync("Products", product, new Entry() { { "UnitPrice", 22m } });
            batch += async x => product1 = await x.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            batch += c => c.UpdateEntryAsync("Products", product, new Entry() { { "UnitPrice", 23m } });
            batch += async x => product2 = await x.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1["UnitPrice"]);
            Assert.Equal(23m, product2["UnitPrice"]);

            var client = new ODataClient(settings);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            Assert.Equal(23m, product["UnitPrice"]);
        }

        [Fact]
        public async Task MultipleUpdatesEntriesSingleBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;

            var batch = new ODataBatch(settings);
            batch += async c => product = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test11" }, { "UnitPrice", 121m } });
            batch += async c => product = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test12" }, { "UnitPrice", 121m } });
            batch += async c => product = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test13" }, { "UnitPrice", 121m } });
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += c => c.For("Products").Filter("UnitPrice eq 121").Set(new Entry() { { "UnitPrice", 122m } }).UpdateEntriesAsync();
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            product = await client.FindEntryAsync("Products?$filter=UnitPrice eq 121");
            Assert.Null(product);
            var products = await client.FindEntriesAsync("Products?$filter=UnitPrice eq 122");
            Assert.Equal(3, products.Count());
        }

        [Fact]
        public async Task UpdateDeleteSingleBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;

            var batch = new ODataBatch(settings);
            batch += async x => product = await x.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test11" }, { "UnitPrice", 21m } });
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += c => c.UpdateEntryAsync("Products", product, new Entry() { { "UnitPrice", 22m } });
            batch += async c => product1 = await c.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            batch += c => c.DeleteEntryAsync("Products", product);
            batch += async c => product2 = await c.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1["UnitPrice"]);
            Assert.Null(product2);

            var client = new ODataClient(settings);
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test11'");
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertUpdateDeleteSeparateBatches()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test12" }, { "UnitPrice", 21m } }, false);
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test12'");
            Assert.Equal(21m, product["UnitPrice"]);
            var key = new Entry() { { "ProductID", product["ProductID"] } };

            batch = new ODataBatch(settings);
            batch += c => c.UpdateEntryAsync("Products", key, new Entry() { { "UnitPrice", 22m } });
            await batch.ExecuteAsync();
            
            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test12'");
            Assert.Equal(22m, product["UnitPrice"]);

            batch = new ODataBatch(settings);
            batch += c => c.DeleteEntryAsync("Products", key);
            await batch.ExecuteAsync();

            product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test12'");
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertSingleEntityWithSingleAssociationSingleBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> category = null;
            var batch = new ODataBatch(settings);
            batch += async x => category = await x.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test13" } });
            batch += c => c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test14" }, { "UnitPrice", 21m }, { "Category", category } }, false);
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client
                .For("Products")
                .Expand("Category")
                .Filter("ProductName eq 'Test14'")
                .FindEntryAsync();
            Assert.Equal("Test13", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task InsertSingleEntityWithMultipleAssociationsSingleBatch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;
            var batch = new ODataBatch(settings);
            batch += async c => product1 = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test15" }, { "UnitPrice", 21m } });
            batch += async c => product2 = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test16" }, { "UnitPrice", 22m } });
            batch += c => c.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test17" }, { "Products", new[] { product1, product2 } } }, false);
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var category = await client
                .For("Categories")
                .Expand("Products")
                .Filter("CategoryName eq 'Test17'")
                .FindEntryAsync();
            Assert.Equal(2, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task DeleteEntryNonExisting()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.DeleteEntryAsync("Products", new Entry { { "ProductID", 0xFFFF } });
            await AssertThrowsAsync<WebRequestException>(async () => await batch.ExecuteAsync());
        }

        [Fact]
        public async Task DeleteEntriesExisting()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += async c => await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test19" }, { "UnitPrice", 21m } });
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += c => c.DeleteEntriesAsync("Products", "Products?$filter=ProductName eq 'Test19'");
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client
                .For("Products")
                .Filter("ProductName eq 'Test19'")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteEntriesNonExisting()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += c => c.DeleteEntriesAsync("Products", "Products?$filter=ProductName eq 'Test99'");
            await batch.ExecuteAsync();
        }

        [Fact]
        public async Task DeleteEntriesMultiple()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            batch += async c => await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test21" }, { "UnitPrice", 111m } });
            batch += async c => await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test22" }, { "UnitPrice", 111m } });
            batch += async c => await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test23" }, { "UnitPrice", 111m } });
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += c => c.DeleteEntriesAsync("Products", "Products?$filter=UnitPrice eq 111");
            await batch.ExecuteAsync();

            var client = new ODataClient(settings);
            var product = await client
                .For("Products")
                .Filter("UnitPrice eq 111")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteEntriesNonExistingThenInsert()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;
            var batch = new ODataBatch(settings);
            batch += c => c.DeleteEntriesAsync("Products", "Products?$filter=ProductName eq 'Test99'");
            batch += async c => product = await c.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test15" }, { "UnitPrice", 21m } });
            batch += c => c.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test17" }, { "Products", new[] { product } } }, false);
            await batch.ExecuteAsync();
        }

        [Fact]
        public async Task UpdateWithResultRequired()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            IDictionary<string, object> product = null;
            IDictionary<string, object> product1 = null;

            var batch = new ODataBatch(settings);
            batch += async x => product = await x.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test11" }, { "UnitPrice", 21m } }, true);
            await batch.ExecuteAsync();

            batch = new ODataBatch(settings);
            batch += async c => product1 = await c.UpdateEntryAsync("Products", product, new Entry() { { "UnitPrice", 22m } }, true);
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1["UnitPrice"]);
        }

        [Fact]
        public async Task Function()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            int result = 0;
            batch += async c => result = await c.Unbound().Function("ParseInt").Set(new Entry() { { "number", "1" } }).ExecuteAsScalarAsync<int>();
            await batch.ExecuteAsync();

            Assert.Equal(1, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var category = await client
                .For("Categories")
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await client
                .For("Products")
                .Set(new { ProductName = "Test5" })
                .InsertEntryAsync();

            var batch = new ODataBatch(settings);
            batch += async c => await c
                .For("Products")
                .Key(product)
                .LinkEntryAsync("Category", category);
            await batch.ExecuteAsync();

            product = await client
                .For("Products")
                .Filter("ProductName eq 'Test5'")
                .FindEntryAsync();
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);
        }

        [Fact]
        public async Task Count()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var batch = new ODataBatch(settings);
            int count = 0;
            batch += async c => count = await c
                .For("Products")
                .Count()
                .FindScalarAsync<int>();
            await batch.ExecuteAsync();

            Assert.Equal(ExpectedCountOfProducts, count);
        }
    }
}

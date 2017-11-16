using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class BatchTypedTests : TestBase
    {
        [Fact]
        public async Task Success()
        {
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Set(new Product() { ProductName = "Test1", UnitPrice = 10m })
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(new Product() { ProductName = "Test2", UnitPrice = 20m })
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();
            Assert.NotNull(product);
            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test2")
                .FindEntryAsync();
            Assert.NotNull(product);
        }

        [Fact]
        public async Task SuccessWithResults()
        {
            Product product1 = null;
            Product product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product1 = await c
                .For<Product>()
                .Set(new Product() { ProductName = "Test1", UnitPrice = 10m })
                .InsertEntryAsync();
            batch += async c => product2 = await c
                .For<Product>()
                .Set(new Product() { ProductName = "Test2", UnitPrice = 20m })
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            Assert.NotEqual(0, product1.ProductID);
            Assert.NotEqual(0, product2.ProductID);

            product1 = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();
            Assert.NotNull(product1);
            product2 = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test2")
                .FindEntryAsync();
            Assert.NotNull(product2);
        }

        [Fact]
        public async Task PartialFailures()
        {
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 10m })
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(new { ProductName = "Test2", UnitPrice = 20m, SupplierID = 0xFFFF })
                .InsertEntryAsync(false);

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
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Set(new { UnitPrice = 10m })
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(new { UnitPrice = 20m })
                .InsertEntryAsync(false);

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
        public async Task MultipleUpdatesSingleBatch()
        {
            Product product = null;
            Product product1 = null;
            Product product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product = await c
                .For<Product>()
                .Set(new { ProductName = "Test11", UnitPrice = 21m })
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { UnitPrice = 22m })
                .UpdateEntryAsync(false);
            batch += async c => product1 = await c
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            batch += c => c
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { UnitPrice = 23m })
                .UpdateEntryAsync(false);
            batch += async c => product2 = await c
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1.UnitPrice);
            Assert.Equal(23m, product2.UnitPrice);

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            Assert.Equal(23m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateDeleteSingleBatch()
        {
            Product product = null;
            Product product1 = null;
            Product product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product = await c
                .For<Product>()
                .Set(new { ProductName = "Test11", UnitPrice = 21m })
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .Set(new { UnitPrice = 22m })
                .UpdateEntryAsync(false);
            batch += async c => product1 = await c
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            batch += c => c
                .For<Product>()
                .Key(product.ProductID)
                .DeleteEntryAsync();
            batch += async c => product2 = await c
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1.UnitPrice);
            Assert.Null(product2);

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test11")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertUpdateDeleteSeparateBatches()
        {
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Set(new { ProductName = "Test12", UnitPrice = 21m })
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Equal(21m, product.UnitPrice);
            var productID = product.ProductID;

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Key(productID)
                .Set(new { UnitPrice = 22m })
                .UpdateEntryAsync(false);
            await batch.ExecuteAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Equal(22m, product.UnitPrice);

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Key(productID)
                .DeleteEntryAsync();
            await batch.ExecuteAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertSingleEntityWithSingleAssociationSingleBatch()
        {
            var category = new Category() { CategoryName = "Test13" };
            var batch = new ODataBatch(_serviceUri);
            batch += async c => await c
                .For<Category>()
                .Set(category)
                .InsertEntryAsync();
            batch += c => c
                .For<Product>()
                .Set(new { ProductName = "Test14", UnitPrice = 21m, Category = category })
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            var product = await _client
                .For<Product>()
                .Expand(x => x.Category)
                .Filter(x => x.ProductName == "Test14")
                .FindEntryAsync();
            Assert.Equal("Test13", product.Category.CategoryName);
        }

        [Fact]
        public async Task InsertSingleEntityWithMultipleAssociationsSingleBatch()
        {
            var product1 = new Product() {ProductName = "Test15", UnitPrice = 21m};
            var product2 = new Product() {ProductName = "Test16", UnitPrice = 22m};

            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For<Product>()
                .Set(product1)
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(product2)
                .InsertEntryAsync(false);
            batch += async c => await c
                .For<Category>()
                .Set(new { CategoryName = "Test17", Products = new[] { product1, product2 } })
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var category = await _client
                .For<Category>()
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Test17")
                .FindEntryAsync();
            Assert.Equal(2, category.Products.Count());
        }
    }
}

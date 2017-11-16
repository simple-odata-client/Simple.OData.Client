using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class BatchDynamicTests : TestBase
    {
        [Fact]
        public async Task Success()
        {
            var x = ODataDynamic.Expression;
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 10m)
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(x.ProductName = "Test2", x.UnitPrice = 20m)
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .FindEntryAsync();
            Assert.NotNull(product);
            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test2")
                .FindEntryAsync();
            Assert.NotNull(product);
        }

        [Fact]
        public async Task SuccessWithResults()
        {
            var x = ODataDynamic.Expression;
            dynamic product1 = null;
            dynamic product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product1 = await c
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 10m)
                .InsertEntryAsync();
            batch += async c => product2 = await c
                .For(x.Products)
                .Set(x.ProductName = "Test2", x.UnitPrice = 20m)
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            Assert.NotEqual(0, product1.ProductID);
            Assert.NotEqual(0, product2.ProductID);

            product1 = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .FindEntryAsync();
            Assert.NotNull(product1);
            product2 = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test2")
                .FindEntryAsync();
            Assert.NotNull(product2);
        }

        [Fact]
        public async Task PartialFailures()
        {
            var x = ODataDynamic.Expression;
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 10m)
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(x.ProductName = "Test2", x.UnitPrice = 20m, x.SupplierID = 0xFFFF)
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
            var x = ODataDynamic.Expression;
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Set(x.UnitPrice = 10m)
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(x.UnitPrice = 20m)
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
            var x = ODataDynamic.Expression;
            dynamic product = null;
            dynamic product1 = null;
            dynamic product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product = await c
                .For(x.Products)
                .Set(x.ProductName = "Test11", x.UnitPrice = 21m)
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Key(product.ProductID)
                .Set(x.UnitPrice = 22m)
                .UpdateEntryAsync(false);
            batch += async c => product1 = await c
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            batch += c => c
                .For(x.Products)
                .Key(product.ProductID)
                .Set(x.UnitPrice = 23m)
                .UpdateEntryAsync(false);
            batch += async c => product2 = await c
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1.UnitPrice);
            Assert.Equal(23m, product2.UnitPrice);

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            Assert.Equal(23m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateDeleteSingleBatch()
        {
            var x = ODataDynamic.Expression;
            dynamic product = null;
            dynamic product1 = null;
            dynamic product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async c => product = await c
                .For(x.Products)
                .Set(x.ProductName = "Test11", x.UnitPrice = 21m)
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .Set(new { UnitPrice = 22m })
                .UpdateEntryAsync(false);
            batch += async c => product1 = await c
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            batch += c => c
                .For(x.Products)
                .Key(product.ProductID)
                .DeleteEntryAsync();
            batch += async c => product2 = await c
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            await batch.ExecuteAsync();

            Assert.Equal(22m, product1.UnitPrice);
            Assert.Null(product2);

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test11")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertUpdateDeleteSeparateBatches()
        {
            var x = ODataDynamic.Expression;
            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Set(new { ProductName = "Test12", UnitPrice = 21m })
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Equal(21m, product.UnitPrice);
            var productID = product.ProductID;

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Key(productID)
                .Set(x.UnitPrice = 22m)
                .UpdateEntryAsync(false);
            await batch.ExecuteAsync();

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Equal(22m, product.UnitPrice);

            batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Key(productID)
                .DeleteEntryAsync();
            await batch.ExecuteAsync();

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test12")
                .FindEntryAsync();
            Assert.Null(product);
        }

        [Fact]
        public async Task InsertSingleEntityWithSingleAssociationSingleBatch()
        {
            var x = ODataDynamic.Expression;
            var category = new Category() { CategoryName = "Test13" };
            var batch = new ODataBatch(_serviceUri);
            batch += async c => await c
                .For(x.Categories)
                .Set(category)
                .InsertEntryAsync();
            batch += c => c
                .For(x.Products)
                .Set(x.ProductName = "Test14", x.UnitPrice = 21m, x.Category = category)
                .InsertEntryAsync();
            await batch.ExecuteAsync();

            var product = await _client
                .For(x.Products)
                .Expand(x.Category)
                .Filter(x.ProductName == "Test14")
                .FindEntryAsync();
            Assert.Equal("Test13", product.Category.CategoryName);
        }

        [Fact]
        public async Task InsertSingleEntityWithMultipleAssociationsSingleBatch()
        {
            var x = ODataDynamic.Expression;
            dynamic product1 = new { ProductName = "Test15", UnitPrice = 21m };
            dynamic product2 = new { ProductName = "Test16", UnitPrice = 22m };

            var batch = new ODataBatch(_serviceUri);
            batch += c => c
                .For(x.Products)
                .Set(product1)
                .InsertEntryAsync(false);
            batch += c => c
                .For<Product>()
                .Set(product2)
                .InsertEntryAsync(false);
            batch += async c => await c
                .For(x.Categories)
                .Set(x.CategoryName = "Test17", x.Products = new[] { product1, product2 })
                .InsertEntryAsync(false);
            await batch.ExecuteAsync();

            var category = await _client
                .For(x.Categories)
                .Expand(x.Products)
                .Filter(x.CategoryName == "Test17")
                .FindEntryAsync();
            Assert.Equal(2, (category.Products as IEnumerable<dynamic>).Count());
        }
    }
}

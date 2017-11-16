using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class LinkTypedTests : TestBase
    {
        [Fact]
        public async Task LinkEntry()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test5" })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product)
                .LinkEntryAsync(category);

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.NotNull(product.CategoryID);
            Assert.Equal(category.CategoryID, product.CategoryID);
        }

        [Fact]
        public async Task UnlinkEntry()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test5", CategoryID = category.CategoryID })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product)
                .UnlinkEntryAsync<Category>();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.Null(product.CategoryID);
        }
    }
}

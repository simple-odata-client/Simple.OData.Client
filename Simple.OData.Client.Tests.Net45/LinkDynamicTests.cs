using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class LinkDynamicTests : TestBase
    {
        [Fact]
        public async Task LinkEntry()
        {
            var x = ODataDynamic.Expression;
            var category = await _client
                .For(x.Categories)
                .Set(x.CategoryName = "Test4")
                .InsertEntryAsync();
            var product = await _client
                .For(x.Products)
                .Set(x.ProductName = "Test5")
                .InsertEntryAsync();

            await _client
                .For(x.Products)
                .Key(product)
                .LinkEntryAsync(x.Category, category);

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.NotNull(product.CategoryID);
            Assert.Equal(category.CategoryID, product.CategoryID);
        }

        [Fact]
        public async Task UnlinkEntry()
        {
            var x = ODataDynamic.Expression;
            var category = await _client
                .For(x.Categories)
                .Set(x.CategoryName = "Test4")
                .InsertEntryAsync();
            var product = await _client
                .For(x.Products)
                .Set(x.ProductName = "Test5", x.CategoryID = category.CategoryID)
                .InsertEntryAsync();

            await _client
                .For(x.Products)
                .Key(product)
                .UnlinkEntryAsync(x.Category);

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.Null(product.CategoryID);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class LinkTests : TestBase
    {
        [Fact]
        public async Task LinkEntry()
        {
            var category = await _client
                .For("Categories")
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test5" })
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .LinkEntryAsync("Category", category);

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test5'")
                .FindEntryAsync();
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);
        }

        [Fact]
        public async Task UnlinkEntry()
        {
            var category = await _client
                .For("Categories")
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test5", CategoryID = category["CategoryID"] })
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .UnlinkEntryAsync("Category");

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test5'")
                .FindEntryAsync();
            Assert.Null(product["CategoryID"]);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class ClientAdapterTests : TestBase
    {
        private readonly ClientAdapter _adapter;

        public ClientAdapterTests()
        {
            _adapter = new ClientAdapter(_serviceUri);
        }

        [Fact]
        public async Task FindEntries()
        {
            var products = await _adapter.FindEntriesAsync("Products");
            Assert.True(products.Count() > 0);
        }

        [Fact]
        public async Task InsertEntry()
        {
            var product = await _adapter.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 18m } }, false);

            Assert.Null(product);
        }

        [Fact]
        public async Task UpdateEntry()
        {
            var key = new Entry() { { "ProductID", 1 } };
            await _adapter.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } });

            var product = await _adapter.GetEntryAsync("Products", key);
            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public async Task DeleteEntry()
        {
            var product = await _adapter.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test3" }, { "UnitPrice", 18m } }, true);
            product = await _adapter.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.NotNull(product);

            await _adapter.DeleteEntryAsync("Products", product);

            product = await _adapter.FindEntryAsync("Products?$filter=ProductName eq 'Test3'");
            Assert.Null(product);
        }
    }
}
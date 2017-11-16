using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class DeleteTests : TestBase
    {
        [Fact]
        public async Task DeleteByKey()
        {
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product["ProductID"])
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByKeyClearMetadataCache()
        {
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            (_client as ODataClient).Session.ClearMetadataCache();
            await _client
                .For("Products")
                .Key(product["ProductID"])
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByFilter()
        {
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .DeleteEntriesAsync();

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByFilterFromCommand()
        {
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            var commandText = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .GetCommandTextAsync();

            await _client
                .DeleteEntriesAsync("Products", commandText);

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByObjectAsKey()
        {
            var product = await _client
                .For("Products")
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("ProductName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteDerived()
        {
            var ship = await _client
                .For("Transport")
                .As("Ship")
                .Set(new { ShipName = "Test1" })
                .InsertEntryAsync();

            await _client
                .For("Transport")
                .As("Ship")
                .Key(ship["TransportID"])
                .DeleteEntryAsync();

            ship = await _client
                .For("Transport")
                .As("Ship")
                .Filter("ShipName eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(ship);
        }
    }
}

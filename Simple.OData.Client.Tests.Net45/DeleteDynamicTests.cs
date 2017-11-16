using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class DeleteDynamicTests : TestBase
    {
        [Fact]
        public async Task DeleteByKey()
        {
            var x = ODataDynamic.Expression;
            var product = await _client
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 18m)
                .InsertEntryAsync();

            await _client
                .For(x.Products)
                .Key(product.ProductID)
                .DeleteEntryAsync();

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByFilter()
        {
            var x = ODataDynamic.Expression;
            var product = await _client
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 18m)
                .InsertEntryAsync();

            await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .DeleteEntryAsync();

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByObjectAsKey()
        {
            var x = ODataDynamic.Expression;
            var product = await _client
                .For(x.Products)
                .Set(x.ProductName = "Test1", x.UnitPrice = 18m)
                .InsertEntryAsync();

            await _client
                .For(x.Products)
                .Key(product)
                .DeleteEntryAsync();

            product = await _client
                .For(x.Products)
                .Filter(x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteDerived()
        {
            var x = ODataDynamic.Expression;
            var ship = await _client
                .For(x.Transport)
                .As(x.Ship)
                .Set(x.ShipName = "Test1")
                .InsertEntryAsync();

            await _client
                .For(x.Transport)
                .As(x.Ship)
                .Key(ship.TransportID)
                .DeleteEntryAsync();

            ship = await _client
                .For(x.Transport)
                .As(x.Ship)
                .Filter(x.ShipName == "Test1")
                .FindEntryAsync();

            Assert.Null(ship);
        }
    }
}

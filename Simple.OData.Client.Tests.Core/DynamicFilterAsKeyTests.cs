using System;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class DynamicFilterAsKeyV4Tests : DynamicFilterAsKeyTests
    {
        public override string MetadataFile { get { return "Northwind4.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }
    }

    public abstract class DynamicFilterAsKeyTests : TestBase
    {
        [Fact]
        public async Task FindAllByFilterAsKeyEqual()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(x.ProductID == 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyNotEqual()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(x.ProductID != 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductID%20ne%201", commandText);
        }

        [Fact]
        public void FindAllByFilterTwoClauses()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Product)
                .Filter(x.ProductID != 1)
                .Filter(x.ProductID != 2);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductID%20ne%201%20and%20ProductID%20ne%202", commandText);
        }

        [Fact]
        public void FindAllByFilterTwoClausesWithOr()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Product)
                .Filter(x.ProductID != 1 || x.ProductID != 2)
                .Filter(x.ProductID != 3);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=%28ProductID%20ne%201%20or%20ProductID%20ne%202%29%20and%20ProductID%20ne%203", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsNotKeyEqual()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(!(x.ProductID == 1));
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal(string.Format("Products?$filter=not%20{0}ProductID%20eq%201{1}", 
                Uri.EscapeDataString("("), Uri.EscapeDataString(")")), commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualLong()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(x.ProductID == 1L);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal(string.Format("Products(1{0})", FormatSettings.LongNumberSuffix), commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualAndExtraClause()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(x.ProductID == 1 && x.ProductName == "abc");
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal(string.Format("Products?$filter=ProductID%20eq%201%20and%20ProductName%20eq%20{0}abc{0}", 
                Uri.EscapeDataString("'")), commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualDuplicateClause()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Products)
                .Filter(x.ProductID == 1 && x.ProductID == 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsCompleteCompoundKey()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.OrderDetails)
                .Filter(x.OrderID == 1 && x.ProductID == 2);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Order_Details(OrderID=1,ProductID=2)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsInCompleteCompoundKey()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.OrderDetails)
                .Filter(x.OrderID == 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Order_Details?$filter=OrderID%20eq%201", commandText);
        }

        [Fact]
        public async Task FindAllEmployeeSuperiors()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Employees)
                .Filter(x.EmployeeID == 1)
                .NavigateTo(x.Superior);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public async Task FindAllCustomerOrders()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Customers)
                .Filter(x.CustomerID == "ALFKI")
                .NavigateTo(x.Orders);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Customers(%27ALFKI%27)/Orders", commandText);
        }

        [Fact]
        public async Task FindAllEmployeeSubordinates()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Employees)
                .Filter(x.EmployeeID == 2)
                .NavigateTo(x.Subordinates);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(2)/Subordinates", commandText);
        }

        [Fact]
        public async Task FindAllOrderOrderDetails()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Orders)
                .Filter(x.OrderID == 10952)
                .NavigateTo(x.OrderDetails);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Orders(10952)/Order_Details", commandText);
        }

        [Fact]
        public async Task FindEmployeeSuperior()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Employees)
                .Filter(x.EmployeeID == 1)
                .NavigateTo(x.Superior);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public async Task FindAllFromBaseTableByFilterAsKeyEqual()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Transport)
                .Filter(x.TransportID == 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Transport(1)", commandText);
        }

        [Fact]
        public async Task FindAllFromDerivedTableByFilterAsKeyEqual()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Transport)
                .As(x.Ship)
                .Filter(x.TransportID == 1);
            string commandText = await command.GetCommandTextAsync();
            Assert.Equal("Transport(1)/NorthwindModel.Ships", commandText);
        }

        [Fact]
        public void FindAllByTypedFilterAndQueryOptions()
        {
            var x = ODataDynamic.Expression;
            var command = _client
                .For(x.Product)
                .Filter(x.ProductName == "abc")
                .QueryOptions(x.IntOption == 42 && x.StringOption == "xyz");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductName%20eq%20%27abc%27&IntOption=42&StringOption='xyz'", commandText);
        }
    }
}

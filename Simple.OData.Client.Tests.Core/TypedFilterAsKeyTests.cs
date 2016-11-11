using System;
using System.Collections.Generic;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class TypedFilterAsKeyV4Tests : TypedFilterAsKeyTests
    {
        public override string MetadataFile { get { return "Northwind4.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }
    }

    public abstract class TypedFilterAsKeyTests : TestBase
    {
        [Fact]
        public void FindAllByTypedFilterAsKeyEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public void FindAllByFilterAsKeyNotEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductID%20ne%201", commandText);
        }

        [Fact]
        public void FindAllByFilterTwoClauses()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1)
                .Filter(x => x.ProductID != 2);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductID%20ne%201%20and%20ProductID%20ne%202", commandText);
        }

        [Fact]
        public void FindAllByFilterTwoClausesWithOr()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1 || x.ProductID != 2)
                .Filter(x => x.ProductID != 3);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=%28ProductID%20ne%201%20or%20ProductID%20ne%202%29%20and%20ProductID%20ne%203", commandText);
        }

        [Fact]
        public void FindAllByFilterAsNotKeyEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => !(x.ProductID == 1));
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Products?$filter=not{0}ProductID%20eq%201{1}", 
                Uri.EscapeDataString("("), Uri.EscapeDataString(")")), commandText);
        }

        [Fact]
        public void FindAllByFilterAsKeyEqualLong()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1L);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Products(1{0})", FormatSettings.LongNumberSuffix), commandText);
        }

        [Fact]
        public void FindAllByFilterAsKeyEqualAndExtraClause()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1 && x.ProductName == "abc");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Products?$filter=ProductID%20eq%201%20and%20ProductName%20eq%20{0}abc{0}", 
                Uri.EscapeDataString("'")), commandText);
        }

        [Fact]
        public void FindAllByFilterAsKeyEqualDuplicateClause()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1 && x.ProductID == 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public void FindAllByFilterAsCompleteCompoundKey()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.OrderID == 1 && x.ProductID == 2);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Order_Details(OrderID=1,ProductID=2)", commandText);
        }

        [Fact]
        public void FindAllByFilterAsInCompleteCompoundKey()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.OrderID == 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Order_Details?$filter=OrderID%20eq%201", commandText);
        }

        [Fact]
        public void FindAllByFilterWithDateTimeOffset()
        {
            var created = new DateTimeOffset(2010, 12, 1, 12, 11, 10, TimeSpan.FromHours(0));
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > created);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Orders?$filter=ShippedDateTimeOffset%20gt%20{0}", 
                FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)), commandText);
        }

        [Fact]
        public void FindAllByFilterWithDateTimeOffsetCastFromDateTime()
        {
            var created = new DateTime(2010, 12, 1, 12, 11, 10, DateTimeKind.Utc);
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > (DateTimeOffset)created);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Orders?$filter=ShippedDateTimeOffset%20gt%20{0}",
                FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)), commandText);
        }

        [Fact]
        public void FindAllByFilterWithDateTimeOffsetCastFromDateTimeOffset()
        {
            var created = new DateTimeOffset(2010, 12, 1, 12, 11, 10, TimeSpan.FromHours(0));
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > (DateTimeOffset)created);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal(string.Format("Orders?$filter=ShippedDateTimeOffset%20gt%20{0}",
                FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)), commandText);
        }

        [Fact]
        public void FindAllEmployeeSuperiors()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 1)
                .NavigateTo("Superior");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public void FindAllCustomerOrders()
        {
            var command = _client
                .For<Customer>()
                .Filter(x => x.CustomerID == "ALFKI")
                .NavigateTo<Order>();
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Customers(%27ALFKI%27)/Orders", commandText);
        }

        [Fact]
        public void FindAllEmployeeSubordinates()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 2)
                .NavigateTo("Subordinates");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Employees(2)/Subordinates", commandText);
        }

        [Fact]
        public void FindAllOrderOrderDetails()
        {
            var command = _client
                .For<Order>()
                .Filter(x => x.OrderID == 10952)
                .NavigateTo<OrderDetail>();
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Orders(10952)/Order_Details", commandText);
        }

        [Fact]
        public void FindEmployeeSuperior()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 1)
                .NavigateTo("Superior");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public void FindAllFromBaseTableByFilterAsKeyEqual()
        {
            var command = _client
                .For<Transport>()
                .Filter(x => x.TransportID == 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Transport(1)", commandText);
        }

        [Fact]
        public void FindAllFromDerivedTableByFilterAsKeyEqual()
        {
            var command = _client
                .For<Transport>()
                .As<Ship>()
                .Filter(x => x.TransportID == 1);
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Transport(1)/NorthwindModel.Ships", commandText);
        }

        [Fact]
        public void FindAllByTypedFilterAndTypedQueryOptions()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductName == "abc")
                .QueryOptions<QueryOptions>(y => y.IntOption == 42 && y.StringOption == "xyz");
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductName%20eq%20%27abc%27&IntOption=42&StringOption='xyz'", commandText);
        }

        [Fact]
        public void FindAllByTypedFilterAndUntypedQueryOptions()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductName == "abc")
                .QueryOptions(new Dictionary<string, object>() { { "IntOption", 42}, { "StringOption", "xyz"} });
            string commandText = command.GetCommandTextAsync().Result;
            Assert.Equal("Products?$filter=ProductName%20eq%20%27abc%27&IntOption=42&StringOption='xyz'", commandText);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.OData;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class TypedFilterAsKeyV3Tests : TypedFilterAsKeyTests
    {
        public override string MetadataFile => "Northwind3.xml";
        public override IFormatSettings FormatSettings => new ODataV3Format();

        [Fact]
        public async Task FindAllByFilterWithContainsThrowsExceptions()
        {
            var list = new List<int> {1};
            var command = _client
                .For<Product>()
                .Filter(x => list.Contains(x.ProductID));
            await Assert.ThrowsAsync<ODataException>(() => command.GetCommandTextAsync());
        }
    }

    public class TypedFilterAsKeyV4Tests : TypedFilterAsKeyTests
    {
        public override string MetadataFile => "Northwind4.xml";
        public override IFormatSettings FormatSettings => new ODataV4Format();

        [Fact]
        public async Task FunctionWithCollectionAsParameter()
        {
            var command = _client
                .Unbound()
                .Function("PassThroughIntCollection")
                .Set(new Dictionary<string, object>() { { "numbers", new[] { 1, 2, 3 } } });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("PassThroughIntCollection(numbers=@p1)?@p1=[1,2,3]", commandText);
        }

        [Fact]
        public async Task FindAllByFilterWithContains()
        {
            var ids = new List<int> {1, 2, 3};
            var command = _client
                .For<Product>()
                .Filter(x => ids.Contains(x.ProductID));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductID%20in%20%281%2C2%2C3%29", commandText);
        }
    }

    public abstract class TypedFilterAsKeyTests : CoreTestBase
    {
        [Fact]
        public async Task FindAllByTypedFilterAsKeyEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyNotEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductID%20ne%201", commandText);
        }

        [Fact]
        public async Task FindAllByFilterTwoClauses()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1)
                .Filter(x => x.ProductID != 2);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductID%20ne%201%20and%20ProductID%20ne%202", commandText);
        }

        [Fact]
        public async Task FindAllByFilterTwoClausesWithOr()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID != 1 || x.ProductID != 2)
                .Filter(x => x.ProductID != 3);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=%28ProductID%20ne%201%20or%20ProductID%20ne%202%29%20and%20ProductID%20ne%203", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsNotKeyEqual()
        {
            var command = _client
                .For<Product>()
                .Filter(x => !(x.ProductID == 1));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"Products?$filter=not%20{Uri.EscapeDataString("(")}ProductID%20eq%201{Uri.EscapeDataString(")")}", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualLong()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1L);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"Products(1{FormatSettings.LongNumberSuffix})", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualAndExtraClause()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1 && x.ProductName == "abc");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(string.Format("Products?$filter=ProductID%20eq%201%20and%20ProductName%20eq%20{0}abc{0}", 
                Uri.EscapeDataString("'")), commandText);
        }
       
        [Fact]
        public async Task FindAllByFilterAsFirstPartOfCompoundKeyEqualAndExtraClause()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.OrderID == 1 && x.Quantity == 1 );
            var commandText = await command.GetCommandTextAsync();

            var expected = "Order_Details?$filter=OrderID%20eq%201%20and%20Quantity%20eq%201";
            Assert.Equal(expected, commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsSecondPartOfCompoundKeyEqualAndExtraClause()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.ProductID == 1 && x.Quantity == 1);
            var commandText = await command.GetCommandTextAsync();

            var expected = "Order_Details?$filter=ProductID%20eq%201%20and%20Quantity%20eq%201";
            Assert.Equal(expected, commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsKeyEqualDuplicateClause()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductID == 1 && x.ProductID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products(1)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsCompleteCompoundKey()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.OrderID == 1 && x.ProductID == 2);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Order_Details(OrderID=1,ProductID=2)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAsInCompleteCompoundKey()
        {
            var command = _client
                .For<OrderDetail>()
                .Filter(x => x.OrderID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Order_Details?$filter=OrderID%20eq%201", commandText);
        }

        [Fact]
        public async Task FindAllByFilterWithDateTimeOffset()
        {
            var created = new DateTimeOffset(2010, 12, 1, 12, 11, 10, TimeSpan.FromHours(0));
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > created);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"Orders?$filter=ShippedDateTimeOffset%20gt%20{FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)}", commandText);
        }

        [Fact]
        public async Task FindAllByFilterWithDateTimeOffsetCastFromDateTime()
        {
            var created = new DateTime(2010, 12, 1, 12, 11, 10, DateTimeKind.Utc);
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > (DateTimeOffset)created);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"Orders?$filter=ShippedDateTimeOffset%20gt%20{FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)}", commandText);
        }

        [Fact]
        public async Task FindAllByFilterWithDateTimeOffsetCastFromDateTimeOffset()
        {
            var created = new DateTimeOffset(2010, 12, 1, 12, 11, 10, TimeSpan.FromHours(0));
            var command = _client
                .For<Order>()
                .Filter(x => x.ShippedDateTimeOffset > (DateTimeOffset)created);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"Orders?$filter=ShippedDateTimeOffset%20gt%20{FormatSettings.GetDateTimeOffsetFormat("2010-12-01T12:11:10Z", true)}", commandText);
        }

        [Fact]
        public async Task FindAllEmployeeSuperiors()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 1)
                .NavigateTo("Superior");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public async Task FindAllCustomerOrders()
        {
            var command = _client
                .For<Customer>()
                .Filter(x => x.CustomerID == "ALFKI")
                .NavigateTo<Order>();
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Customers(%27ALFKI%27)/Orders", commandText);
        }

        [Fact]
        public async Task FindAllEmployeeSubordinates()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 2)
                .NavigateTo("Subordinates");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(2)/Subordinates", commandText);
        }

        [Fact]
        public async Task FindAllOrderOrderDetails()
        {
            var command = _client
                .For<Order>()
                .Filter(x => x.OrderID == 10952)
                .NavigateTo<OrderDetail>();
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Orders(10952)/Order_Details", commandText);
        }

        [Fact]
        public async Task FindEmployeeSuperior()
        {
            var command = _client
                .For<Employee>()
                .Filter(x => x.EmployeeID == 1)
                .NavigateTo("Superior");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Employees(1)/Superior", commandText);
        }

        [Fact]
        public async Task FindAllFromBaseTableByFilterAsKeyEqual()
        {
            var command = _client
                .For<Transport>()
                .Filter(x => x.TransportID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Transport(1)", commandText);
        }

        [Fact]
        public async Task FindAllFromDerivedTableByFilterAsKeyEqual()
        {
            var command = _client
                .For<Transport>()
                .As<Ship>()
                .Filter(x => x.TransportID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Transport(1)/NorthwindModel.Ships", commandText);
        }

        [Fact]
        public async Task FindAllByTypedFilterAndTypedQueryOptions()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductName == "abc")
                .QueryOptions<QueryOptions>(y => y.IntOption == 42 && y.StringOption == "xyz");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductName%20eq%20%27abc%27&IntOption=42&StringOption='xyz'", commandText);
        }

        [Fact]
        public async Task FindAllByTypedFilterAndUntypedQueryOptions()
        {
            var command = _client
                .For<Product>()
                .Filter(x => x.ProductName == "abc")
                .QueryOptions(new Dictionary<string, object>() { { "IntOption", 42}, { "StringOption", "xyz"} });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Products?$filter=ProductName%20eq%20%27abc%27&IntOption=42&StringOption='xyz'", commandText);
        }

        [Fact(Skip = "Revise URL escape method")]
        public async Task FindByStringKeyWithSpaceAndPunctuation()
        {
            var command = _client
                .For<Product>()
                .Key("CRONUS USA, Inc.");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("'CRONUS%20USA%2C%20Inc.'", commandText);
        }

        [Fact]
        public async Task FindByGuidFilterEqual()
        {
            var key = new Guid("D8F3F70F-C185-49AB-9A92-0C86C344AB1B");
            var command = _client
                .For<TypeWithGuidKey>()
                .Filter(x => x.Key == key);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"TypeWithGuidKey({Uri.EscapeDataString(FormatSettings.GetGuidFormat(key.ToString()))})", commandText);
        }

        [Fact]
        public async Task FindByGuidKey()
        {
            var key = new Guid("BEC6C966-8016-46D0-A3D1-99D69DF69D74");
            var command = _client
                .For<TypeWithGuidKey>()
                .Key(key);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal($"TypeWithGuidKey({Uri.EscapeDataString(FormatSettings.GetGuidFormat(key.ToString()))})", commandText);
        }

        [Fact]
        public async Task FindAllEntityLowerCaseNoPrefix()
        {
            var command = _client
                .For("project1")
                .Key("abc");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("project1(%27abc%27)", commandText);
        }

        [Fact(Skip = "Entity set names with multiple segments are not supported")]
        public async Task FindAllEntityLowerCaseWithPrefix()
        {
            var client = CreateClient(this.MetadataFile, ODataNameMatchResolver.Strict);
            var command = client
                .For("project2")
                .Key("abc");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("project2(%27abc%27)", commandText);
        }

        [Fact]
        public async Task FindAllByFilterAndKey()
        {
            var command = _client
                .For<Category>()
                .Key(1)
                .Filter(x => x.CategoryName == "Beverages");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal("Categories(1)?$filter=CategoryName%20eq%20%27Beverages%27", commandText);
        }
    }
}
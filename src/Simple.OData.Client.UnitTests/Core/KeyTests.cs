using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class KeyTests : CoreTestBase
    {
        public override string MetadataFile { get { return "Northwind4WithAlternateKeys.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }

        [Theory]
        [InlineData("Northwind3.xml", "Categories(1)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(1)")]
        public async Task PrimaryKey_SingleProperty_NoNames(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Key(1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }
        
        [Theory]
        [InlineData("Northwind3.xml", "Categories(1)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(1)")]
        public async Task PrimaryKey_SingleProperty_Named(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Key(new { CategoryID = 1 });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Order_Details(OrderID=1,ProductID=2)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Order_Details(OrderID=1,ProductID=2)")]
        public async Task PrimaryKey_MultipleProperty_NoNames(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<OrderDetail>()
                .Key(1, 2);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Order_Details(OrderID=1,ProductID=2)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Order_Details(OrderID=1,ProductID=2)")]
        public async Task PrimaryKey_MultipleProperty_Named(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<OrderDetail>()
                .Key(new { ProductID = 2, OrderID = 1 });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Order_Details(OrderID=1,ProductID=2)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Order_Details(OrderID=1,ProductID=2)")]
        public async Task PrimaryKey_MultipleProperty_FromEntity(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<OrderDetail>()
                .Key(new OrderDetail { ProductID = 2, OrderID = 1, Quantity = 5 });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Categories(1)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(1)")]
        public async Task PrimaryKey_SingleProperty_FromEntity(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Key(new Category { CategoryID = 1 });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Categories(1)")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(1)")]
        public async Task PrimaryKey_SingleProperty_FromFilter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Filter(x => x.CategoryID == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Categories?$filter=CategoryID%20eq%201%20and%20CategoryName%20eq%20%27Test%27")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories?$filter=CategoryID%20eq%201%20and%20CategoryName%20eq%20%27Test%27")]
        public async Task PrimaryKey_SingleProperty_NoMatchinhFilter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Filter(x => x.CategoryID == 1 && x.CategoryName == "Test");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("KeyDemo.xml", "Activities?$filter=Ticket%2FId%20eq%201")]
        public async Task PrimaryKey_SingleProperty_NavigationKeyFilter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Activity>()
                .Filter(x => x.Ticket.Id == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("KeyDemo.xml", "Activities?$filter=Option%2FId%20eq%201")]
        public async Task PrimaryKey_SingleProperty_ComplexKeyFilter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Activity>()
                .Filter(x => x.Option.Id == 1);
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Categories")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(CategoryName=%27Beverages%27)")]
        public async Task AlternateKey_SingleProperty(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Key(new { CategoryName = "Beverages" });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Categories?$filter=CategoryName%20eq%20%27Beverages%27")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Categories(CategoryName=%27Beverages%27)")]
        public async Task AlternateKey_SingleProperty_As_Filter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Category>()
                .Filter(x => x.CategoryName == "Beverages");
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Employees")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Employees(HomePhone=%27123%27,Title=%27Manager%27)")]
        public async Task AlternateKey_MultipleProperty(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Employee>()
                .Key(new { HomePhone = "123", Title = "Manager" });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Employees?$filter=HomePhone%20eq%20%27123%27%20and%20Title%20eq%20%27Manager%27")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Employees(HomePhone=%27123%27,Title=%27Manager%27)")]
        public async Task AlternateKey_MultipleProperty_FromFilter(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Employee>()
                .Filter(x =>  x.HomePhone == "123" && x.Title == "Manager" );
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Orders")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Orders(CustomerID=%27ALFKI%27)")]
        public async Task AlternateKey_MultipleKeys_Key1(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Order>()
                .Key(new { CustomerID = "ALFKI" });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind3.xml", "Orders")]
        [InlineData("Northwind4WithAlternateKeys.xml", "Orders(ShipName=%27TEST%27)")]
        public async Task AlternateKey_MultipleKeys_Key2(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Order>()
                .Key(new { ShipName = "TEST" });
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }
    }
}

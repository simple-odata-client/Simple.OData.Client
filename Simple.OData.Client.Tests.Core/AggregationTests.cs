using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class AggregationTests : TestBase
    {
        public override string MetadataFile { get { return "Northwind.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV3Format(); } }

        [Theory]
        [InlineData("Northwind.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%29")]
        [InlineData("Northwind4.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%29")]
        public async Task FilterAny(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Employee>()
                .Filter(x => x.Subordinates.Any(y => y.EmployeeID == 1));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%20and%20x1%2FFirstName%20eq%20%27abc%27%29")]
        [InlineData("Northwind4.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%20and%20x1%2FFirstName%20eq%20%27abc%27%29")]
        public async Task FilterMultipleAny(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Employee>()
                .Filter(x => x.Subordinates.Any(y => y.EmployeeID == 1 && y.FirstName == "abc"));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%20and%20substringof%28%27abc%27%2Cx1%2FFirstName%29%29")]
        [InlineData("Northwind4.xml", "Employees?$filter=Subordinates%2Fany%28x1%3Ax1%2FEmployeeID%20eq%201%20and%20contains%28x1%2FFirstName%2C%27abc%27%29%29")]
        public async Task FilterMultipleAnyContains(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Employee>()
                .Filter(x => x.Subordinates.Any(y => y.EmployeeID == 1 && y.FirstName.Contains("abc")));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind.xml", "Products?$filter=Category%2FProducts%2Fany%28x1%3Ax1%2FProductID%20eq%201%29")]
        [InlineData("Northwind4.xml", "Products?$filter=Category%2FProducts%2Fany%28x1%3Ax1%2FProductID%20eq%201%29")]
        public async Task FilterNestedAny(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Product>()
                .Filter(x => x.Category.Products.Any(y => y.ProductID == 1));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }

        [Theory]
        [InlineData("Northwind.xml", "Products?$filter=Category%2FProducts%2Fany%28x1%3Ax1%2FCategory%2FProducts%2Fany%28x2%3Ax2%2FProductID%20eq%201%29%29")]
        [InlineData("Northwind4.xml", "Products?$filter=Category%2FProducts%2Fany%28x1%3Ax1%2FCategory%2FProducts%2Fany%28x2%3Ax2%2FProductID%20eq%201%29%29")]
        public async Task FilterMultipleNestedAny(string metadataFile, string expectedCommand)
        {
            ODataExpression.ArgumentCounter = 0;
            var client = CreateClient(metadataFile);
            var command = client
                .For<Product>()
                .Filter(x => x.Category.Products.Any(y => y.Category.Products.Any(z => z.ProductID == 1)));
            var commandText = await command.GetCommandTextAsync();
            Assert.Equal(expectedCommand, commandText);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class UpdateTypedTests : TestBase
    {
        [Fact]
        public async Task UpdateByKey()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { UnitPrice = 123m })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Equal(123m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateByFilter()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .Set(new { UnitPrice = 123m })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Equal(123m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateMultipleWithResult()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            product = (await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .Set(new { UnitPrice = 123m })
                .UpdateEntriesAsync()).Single();

            Assert.Equal(123m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateMultipleNoResult()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            product = (await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .Set(new { UnitPrice = 123m })
                .UpdateEntriesAsync(false)).Single();
            Assert.Null(product);

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Equal(123m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateByObjectAsKey()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product)
                .Set(new { UnitPrice = 456m })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Equal(456m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateObjectValue()
        {
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test1", UnitPrice = 18m })
                .InsertEntryAsync();

            product.UnitPrice = 456m;
            await _client
                .For<Product>()
                .Key(product)
                .Set(product)
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .FindEntryAsync();

            Assert.Equal(456m, product.UnitPrice);
        }

        [Fact]
        public async Task UpdateDate()
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);

            var employee = await _client
                .For<Employee>()
                .Set(new { FirstName = "Test1", LastName = "Test1", HireDate = today })
                .InsertEntryAsync();

            await _client
                .For<Employee>()
                .Key(employee.EmployeeID)
                .Set(new { HireDate = tomorrow })
                .UpdateEntryAsync();

            employee = await _client
                .For<Employee>()
                .Key(employee.EmployeeID)
                .FindEntryAsync();

            Assert.Equal(tomorrow, employee.HireDate);
        }

        [Fact]
        public async Task AddSingleAssociation()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test1" })
                .InsertEntryAsync();
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test2", UnitPrice = 18m })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { Category = category })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductID == product.ProductID)
                .FindEntryAsync();
            Assert.Equal(category.CategoryID, product.CategoryID);
            category = await _client
                .For<Category>()
                .Filter(x => x.CategoryID == category.CategoryID)
                .Expand(x => x.Products)
                .FindEntryAsync();
            Assert.Equal(1, category.Products.Count());
        }

        [Fact]
        public async Task UpdateSingleAssociation()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test1" })
                .InsertEntryAsync();
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test2", UnitPrice = 18m, CategoryID = 1 })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { Category = category })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductID == product.ProductID)
                .FindEntryAsync();
            Assert.Equal(category.CategoryID, product.CategoryID);
            category = await _client
                .For<Category>()
                .Filter(x => x.CategoryID == category.CategoryID)
                .Expand(x => x.Products)
                .FindEntryAsync();
            Assert.Equal(1, category.Products.Count());
        }

        [Fact]
        public async Task RemoveSingleAssociation()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test6" })
                .InsertEntryAsync();
            var product = await _client
                .For<Product>()
                .Set(new { ProductName = "Test7", UnitPrice = 18m, Category = category })
                .InsertEntryAsync();

            await _client
                .For<Product>()
                .Key(product.ProductID)
                .Set(new { Category = (int?)null })
                .UpdateEntryAsync();

            product = await _client
                .For<Product>()
                .Filter(x => x.ProductID == product.ProductID)
                .FindEntryAsync();
            Assert.Null(product.CategoryID);
        }

        [Fact]
        public async Task UpdateMultipleAssociations()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test3" })
                .InsertEntryAsync();
            var product1 = await _client
                .For<Product>()
                .Set(new { ProductName = "Test4", UnitPrice = 18m, CategoryID = 1 })
                .InsertEntryAsync();
            var product2 = await _client
                .For<Product>()
                .Set(new { ProductName = "Test5", UnitPrice = 18m, CategoryID = 1 })
                .InsertEntryAsync();

            await _client
                .For<Category>()
                .Key(category.CategoryID)
                .Set(new { Products = new[] { product1, product2 } })
                .UpdateEntryAsync();

            category = await _client
                .For<Category>()
                .Filter(x => x.CategoryID == category.CategoryID)
                .Expand(x => x.Products)
                .FindEntryAsync();
            Assert.Equal(2, category.Products.Count());
        }

        [Fact]
        public async Task UpdateProducttWithoutExpandingCategory()
        {
            var category = await _client
                .For<Category>()
                .Set(new { CategoryName = "Test5" })
                .InsertEntryAsync();
            await _client
                .For<Product>()
                .Set(new { ProductName = "Test6", UnitPrice = 18m, CategoryID = category.CategoryID })
                .InsertEntryAsync();

            var product = await _client
                .For<ProductWithNoCategoryLink>("Products")
                .Filter(x => x.ProductName == "Test6")
                .FindEntryAsync();

            product.ProductName = "Test7";

            await _client
                .For<ProductWithNoCategoryLink>("Products")
                .Key(product.ProductID)
                .Set(product)
                .UpdateEntryAsync();

            product = await _client
                .For<ProductWithNoCategoryLink>("Products")
                .Key(product.ProductID)
                .FindEntryAsync();
            Assert.Equal("Test7", product.ProductName);
            Assert.NotNull(product.CategoryID);
        }

        [Fact]
        public async Task UpdateDerived()
        {
            var ship = await _client
                .For<Transport>()
                .As<Ship>()
                .Set(new Ship { ShipName = "Test1" })
                .InsertEntryAsync();

            ship = await _client
                .For<Transport>()
                .As<Ship>()
                .Key(ship.TransportID)
                .Set(new { ShipName = "Test2" })
                .UpdateEntryAsync();

            Assert.Equal("Test2", ship.ShipName);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class FindTypedTests : TestBase
    {
        [Fact]
        public async Task SingleCondition()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact]
        public async Task SingleConditionWithLocalVariable()
        {
            var productName = "Chai";
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == productName)
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        private string productName = "Chai";

        [Fact]
        public async Task SingleConditionWithMemberrVariable()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == this.productName)
                .FindEntryAsync();
            var sameProduct = await _client
                .For<Product>()
                .Filter(x => x.ProductName != product.ProductName)
                .FindEntryAsync();
            Assert.NotEqual(product.ProductName, sameProduct.ProductName);
        }

        [Fact]
        public async Task CombinedConditions()
        {
            var product = await _client
                .For<Employee>()
                .Filter(x => x.FirstName == "Nancy" && x.HireDate < DateTime.Now)
                .FindEntryAsync();
            Assert.Equal("Davolio", product.LastName);
        }

        [Fact]
        public async Task CombineAll()
        {
            var product = (await _client
                .For<Product>()
                .OrderBy(x => x.ProductName)
                .ThenByDescending(x => x.UnitPrice)
                .Skip(2)
                .Top(1)
                .Expand(x => x.Category)
                .Select(x => x.Category)
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", product.Category.CategoryName);
        }

        [Fact]
        public async Task CombineAllReverse()
        {
            var product = (await _client
                .For<Product>()
                .Select(x => x.Category)
                .Expand(x => x.Category)
                .Top(1)
                .Skip(2)
                .OrderBy(x => x.ProductName)
                .ThenByDescending(x => x.UnitPrice)
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", product.Category.CategoryName);
        }

        [Fact]
        public async Task MappedColumn()
        {
            await _client
                .For<Product>()
                .Set(new Product { ProductName = "Test1", UnitPrice = 18m, MappedEnglishName = "EnglishTest" })
                .InsertEntryAsync(false);

            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Test1")
                .Select(x => new { x.ProductID, x.ProductName, x.MappedEnglishName})
                .FindEntryAsync();
            Assert.Equal("EnglishTest", product.MappedEnglishName);
        }

        [Fact]
        public async Task UnmappedColumn()
        {
            await AssertThrowsAsync<UnresolvableObjectException>(async () => await _client
                .For<ProductWithUnmappedProperty>("Products")
                .Set(new ProductWithUnmappedProperty { ProductName = "Test1" })
                .InsertEntryAsync());
        }

        [Fact]
        public async Task IgnoredUnmappedColumn()
        {
            var settings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IgnoreUnmappedProperties = true,
            };
            var client = new ODataClient(settings);

            var product = await client
                .For<ProductWithUnmappedProperty>("Products")
                .Set(new ProductWithUnmappedProperty { ProductName = "Test1" })
                .InsertEntryAsync();

            await client
                .For<ProductWithUnmappedProperty>("Products")
                .Key(product.ProductID)
                .Set(new ProductWithUnmappedProperty { ProductName = "Test2" })
                .UpdateEntryAsync(false);

            product = await client
                .For<ProductWithUnmappedProperty>("Products")
                .Key(product.ProductID)
                .FindEntryAsync();
            Assert.Equal("Test2", product.ProductName);
        }

        [Fact]
        public async Task Subclass()
        {
            var product = await _client
                .For<ExtendedProduct>("Products")
                .Filter(x => x.ProductName == "Chai")
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact]
        public async Task StringContains()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Contains("ai"))
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single().ProductName);
        }

        [Fact]
        public async Task StringContainsWithLocalVariable()
        {
            var text = "ai";
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Contains(text))
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single().ProductName);
        }

        [Fact]
        public async Task StringContainsWithArrayVariable()
        {
            var text = new [] {"ai"};
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Contains(text[0]))
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single().ProductName);
        }

        [Fact]
        public async Task StringNotContains()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => !x.ProductName.Contains("ai"))
                .FindEntriesAsync();
            Assert.NotEqual("Chai", products.First().ProductName);
        }

        [Fact]
        public async Task StringStartsWith()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.StartsWith("Ch"))
                .FindEntriesAsync();
            Assert.Equal("Chai", products.First().ProductName);
        }

        [Fact]
        public async Task LengthOfStringEqual()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Length == 4)
                .FindEntriesAsync();
            Assert.Equal("Chai", products.First().ProductName);
        }

        [Fact]
        public async Task SubstringWithPositionAndLengthEqual()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Substring(1, 2) == "ha")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.First().ProductName);
        }

        [Fact]
        public async Task SubstringWithPositionAndLengthEqualWithLocalVariable()
        {
            var text = "ha";
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName.Substring(1, 2) == text)
                .FindEntriesAsync();
            Assert.Equal("Chai", products.First().ProductName);
        }

        [Fact]
        public async Task TopOne()
        {
            var products = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .Top(1)
                .FindEntriesAsync();
            Assert.Equal(1, products.Count());
        }

        [Fact]
        public async Task Count()
        {
            var count = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .Count()
                .FindScalarAsync<int>();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Get()
        {
            var category = await _client
                .For<Category>()
                .Key(1)
                .FindEntryAsync();
            Assert.Equal(1, category.CategoryID);
        }

        [Fact]
        public async Task GetNonExisting()
        {
            await AssertThrowsAsync<WebRequestException>(async () => await _client
                .For<Category>()
                .Key(-1)
                .FindEntryAsync());
        }

        [Fact]
        public async Task SelectSingle()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .Select(x => x.ProductName)
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact]
        public async Task SelectMultiple()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .Select(x => new { x.ProductID, x.ProductName })
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact(Skip = "NewExpression property names are not supported")]
        public async Task SelectMultipleRename()
        {
            var settings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IgnoreUnmappedProperties = true,
            };
            var client = new ODataClient(settings);

            var product = await client
                .For<ProductWithUnmappedProperty>("Products")
                .Filter(x => x.ProductName == "Chai")
                .Select(x => new { x.ProductID, UnmappedName = x.ProductName })
                .FindEntryAsync();
            Assert.Equal("Chai", product.UnmappedName);
            Assert.Null(product.ProductName);
        }

        [Fact]
        public async Task ExpandOne()
        {
            var product = (await _client
                .For<Product>()
                .OrderBy(x => x.ProductID)
                .Expand(x => x.Category)
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", product.Category.CategoryName);
        }

        [Fact]
        public async Task ExpandManyAsArray()
        {
            var category = await _client
                .For<Category>()
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Beverages")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count());
        }

        [Fact]
        public async Task ExpandManyAsList()
        {
            var category = await _client
                .For<CategoryWithList>("Categories")
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Beverages")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count());
        }

        [Fact]
        public async Task ExpandManyAsIList()
        {
            var category = await _client
                .For<CategoryWithIList>("Categories")
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Beverages")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count());
        }

        [Fact]
        public async Task ExpandManyAsHashSet()
        {
            var category = await _client
                .For<CategoryWithHashSet>("Categories")
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Beverages")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count());
        }

        [Fact]
        public async Task  ExpandManyAsICollection()
        {
            var category = await _client
                .For<CategoryWithICollection>("Categories")
                .Expand(x => x.Products)
                .Filter(x => x.CategoryName == "Beverages")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count());
        }

        [Fact]
        public async Task ExpandSecondLevel()
        {
            var product = (await _client
                .For<Product>()
                .OrderBy(x => x.ProductID)
                .Expand(x => x.Category.Products)
                .FindEntriesAsync()).Last();
            Assert.Equal(ExpectedCountOfCondimentsProducts, product.Category.Products.Length);
        }

        [Fact]
        public async Task ExpandMultipleLevelsWithCollection()
        {
            var product = (await _client
                .For<Product>()
                .OrderBy(x => x.ProductID)
                .Expand(x => x.Category.Products.Select(y => y.Category))
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", product.Category.Products.First().Category.CategoryName);
        }

        [Fact]
        public async Task ExpandWithSelect()
        {
            var product = (await _client
                .For<Product>()
                .OrderBy(x => x.ProductID)
                .Expand(x => x.Category)
                .Select(x => new { x.ProductName, x.Category.CategoryName })
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", product.Category.CategoryName);
        }

        [Fact]
        public async Task OrderBySingle()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .OrderBy(x => x.ProductName)
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact]
        public async Task OrderByMultiple()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.ProductName == "Chai")
                .OrderBy(x => new { x.ProductID, x.ProductName })
                .FindEntryAsync();
            Assert.Equal("Chai", product.ProductName);
        }

        [Fact]
        public async Task OrderByExpanded()
        {
            var product = (await _client
                .For<Product>()
                .Expand(x => x.Category)
                .OrderBy(x => new { x.Category.CategoryName })
                .FindEntriesAsync()).Last();
            Assert.Equal("Seafood", product.Category.CategoryName);
        }

        [Fact]
        public async Task NavigateToSingle()
        {
            var category = await _client
                .For<Product>()
                .Key(new { ProductID = 2 })
                .NavigateTo<Category>()
                .FindEntryAsync();
            Assert.Equal("Beverages", category.CategoryName);
        }

        [Fact]
        public async Task NavigateToSingleByExpression()
        {
            var category = await _client
                .For<Product>()
                .Key(new { ProductID = 2 })
                .NavigateTo(x => x.Category)
                .FindEntryAsync();
            Assert.Equal("Beverages", category.CategoryName);
        }

        [Fact]
        public async Task NavigateToMultiple()
        {
            var products = await _client
                .For<Category>()
                .Key(2)
                .NavigateTo<Product>()
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfCondimentsProducts, products.Count());
        }

        [Fact]
        public async Task NavigateToRecursive()
        {
            var employee = await _client
                .For<Employee>()
                .Key(14)
                .NavigateTo<Employee>("Superior")
                .NavigateTo<Employee>("Superior")
                .NavigateTo<Employee>("Subordinates")
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee.FirstName);
        }

        [Fact]
        public async Task NavigateToRecursiveByExpression()
        {
            var employee = await _client
                .For<Employee>()
                .Key(14)
                .NavigateTo(x => x.Superior)
                .NavigateTo(x => x.Superior)
                .NavigateTo(x => x.Subordinates)
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee.FirstName);
        }

        [Fact]
        public async Task NavigateToRecursiveSingleClause()
        {
            var employee = await _client
                .For<Employee>()
                .Key(14)
                .NavigateTo(x => x.Superior.Superior.Subordinates)
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee.FirstName);
        }

        [Fact]
        public async Task BaseClassEntries()
        {
            var transport = await _client
                .For<Transport>()
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
        }

        [Fact]
        public async Task BaseClassEntriesWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IncludeAnnotationsInResults = true,
            };
            var client = new ODataClient(clientSettings);
            var transport = await client
                .For<Transport>()
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
        }

        [Fact]
        public async Task AllDerivedClassEntries()
        {
            var transport = await _client
                .For<Transport>()
                .As<Ship>()
                .FindEntriesAsync();
            Assert.Equal("Titanic", transport.Single().ShipName);
        }

        [Fact]
        public async Task AllDerivedClassEntriesWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IncludeAnnotationsInResults = true,
            };
            var client = new ODataClient(clientSettings);
            var transport = await client
                .For<Transport>()
                .As<Ship>()
                .FindEntriesAsync();
            Assert.Equal("Titanic", transport.Single().ShipName);
        }

        [Fact]
        public async Task DerivedClassEntry()
        {
            var transport = await _client
                .For<Transport>()
                .As<Ship>()
                .Filter(x => x.ShipName == "Titanic")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport.ShipName);
        }

        [Fact]
        public async Task BaseClassEntryByKey()
        {
            var transport = await _client
                .For<Transport>()
                .Key(1)
                .FindEntryAsync();
            Assert.Equal(1, transport.TransportID);
        }

        [Fact]
        public async Task DerivedClassEntryByKey()
        {
            var transport = await _client
                .For<Transport>()
                .As<Ship>()
                .Key(1)
                .FindEntryAsync();
            Assert.Equal("Titanic", transport.ShipName);
        }

        [Fact]
        public async Task DerivedClassEntryBaseAndDerivedFields()
        {
            var transport = await _client
                .For<Transport>()
                .As<Ship>()
                .Filter(x => x.TransportID == 1 && x.ShipName == "Titanic")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport.ShipName);
        }

        [Fact]
        public async Task IsOfDerivedClassEntry()
        {
            var transport = await _client
                .For<Transport>()
                .Filter(x => x is Ship)
                .As<Ship>()
                .FindEntryAsync();
            Assert.Equal("Titanic", transport.ShipName);
        }

        [Fact]
        public async Task IsOfAssociation()
        {
            var employee = await _client
                .For<Employee>()
                .Filter(x => x.Superior is Employee)
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastToPrimitiveType()
        {
            var product = await _client
                .For<Product>()
                .Filter(x => x.CategoryID == (int)1L)
                .FindEntryAsync();
            Assert.NotNull(product);
        }

        [Fact]
        public async Task CastInstanceToEntityType()
        {
            var employee = await _client
                .For<Employee>()
                .Filter(x => x as Employee != null)
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastPropertyToEntityType()
        {
            var employee = await _client
                .For<Employee>()
                .Filter(x => x.Superior as Employee != null)
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task FilterAny()
        {
            var products = await _client
                .For<Order>()
                .Filter(x => x.OrderDetails.Any(y => y.Quantity > 50))
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, products.Count());
        }

        [Fact]
        public async Task FilterAll()
        {
            var products = await _client
                .For<Order>()
                .Filter(x => x.OrderDetails.All(y => y.Quantity > 50))
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, products.Count());
        }

        class OrderDetails : OrderDetail { }
        class Order_Details : OrderDetail { }

        [Fact]
        public async Task NameMatchResolver()
        {
            var client = CreateClientWithNameResolver(ODataNameMatchResolver.Strict);
            await AssertThrowsAsync<UnresolvableObjectException>(async () =>
                await client.For<OrderDetails>().FindEntriesAsync());
            var orderDetails1 = await client.For<Order_Details>().FindEntriesAsync();
            Assert.NotEmpty(orderDetails1);
            var orderDetails2 = await _client.For<OrderDetails>().FindEntriesAsync();
            Assert.NotEmpty(orderDetails2);
        }

        public class ODataOrgProduct
        {
            public string Name { get; set; }
            public decimal Price { get; set; }
        }
    }
}

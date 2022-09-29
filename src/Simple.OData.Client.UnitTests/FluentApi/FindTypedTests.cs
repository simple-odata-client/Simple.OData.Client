using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class FindTypedTests : TestBase
{
	[Fact]
	public async Task SingleCondition()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task SingleConditionWithLocalVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task CombinedConditions()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Filter(x => x.FirstName == "Nancy" && x.HireDate < DateTime.Now)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Davolio", employee.LastName);
	}

	[Fact]
	public async Task CombinedConditionsWithMultipleFilters()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Filter(x => x.FirstName == "Nancy")
			.Filter(x => x.HireDate < DateTime.Now)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Davolio", employee.LastName);
	}

	[Fact]
	public async Task CombineAll()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.OrderBy(x => x.ProductName)
			.ThenByDescending(x => x.UnitPrice)
			.Skip(2)
			.Top(1)
			.Expand(x => x.Category)
			.Select(x => x.Category)
			.FindEntriesAsync().ConfigureAwait(false)).Single();
		Assert.Equal("Seafood", product.Category.CategoryName);
	}

	[Fact]
	public async Task CombineAllReverse()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.Select(x => x.Category)
			.Expand(x => x.Category)
			.Top(1)
			.Skip(2)
			.OrderBy(x => x.ProductName)
			.ThenByDescending(x => x.UnitPrice)
			.FindEntriesAsync().ConfigureAwait(false)).Single();
		Assert.Equal("Seafood", product.Category.CategoryName);
	}

	[Fact]
	public async Task MappedColumn()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		await client
			.For<Product>()
			.Set(new Product { ProductName = "Test1", UnitPrice = 18m, MappedEnglishName = "EnglishTest" })
			.InsertEntryAsync(false).ConfigureAwait(false);

		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test1")
			.Select(x => new { x.ProductID, x.ProductName, x.MappedEnglishName })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("EnglishTest", product.MappedEnglishName);
	}

	[Fact]
	public async Task UnmappedColumn()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		await AssertThrowsAsync<UnresolvableObjectException>(async () => await client
			.For<ProductWithUnmappedProperty>("Products")
			.Set(new ProductWithUnmappedProperty { ProductName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task IgnoredUnmappedColumn()
	{
		var client = new ODataClient(CreateDefaultSettings().WithIgnoredUnmappedProperties().WithHttpMock());
		var product = await client
			.For<ProductWithUnmappedProperty>("Products")
			.Set(new ProductWithUnmappedProperty { ProductName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For<ProductWithUnmappedProperty>("Products")
			.Key(product.ProductID)
			.Set(new ProductWithUnmappedProperty { ProductName = "Test2" })
			.UpdateEntryAsync(false).ConfigureAwait(false);

		product = await client
			.For<ProductWithUnmappedProperty>("Products")
			.Key(product.ProductID)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Test2", product.ProductName);
	}

	[Fact]
	public async Task RemappedColumn()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		await client
			.For<Product>()
			.Set(new ProductWithRemappedColumn { ProductName = "Test1", UnitPrice = 18m, MappedEnglishName = "EnglishTest" })
			.InsertEntryAsync(false).ConfigureAwait(false);

		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test1")
			.Select(x => new { x.ProductID, x.ProductName, x.MappedEnglishName })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("EnglishTest", product.MappedEnglishName);
	}

	[Fact]
	public async Task Subclass()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<ExtendedProduct>("Products")
			.Filter(x => x.ProductName == "Chai")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task StringContains()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Contains("ai"))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.Single().ProductName);
	}

	[Fact]
	public async Task StringContainsWithLocalVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var text = "ai";
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Contains(text))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.Single().ProductName);
	}

	[Fact]
	public async Task StringContainsWithArrayVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var text = new[] { "ai" };
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Contains(text[0]))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.Single().ProductName);
	}

	[Fact]
	public async Task StringNotContains()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => !x.ProductName.Contains("ai"))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.NotEqual("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task StringStartsWith()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.StartsWith("Ch"))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task LengthOfStringEqual()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Length == 4)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task SubstringWithPositionAndLengthEqual()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Substring(1, 2) == "ha")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task SubstringWithPositionAndLengthEqualWithLocalVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var text = "ha";
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName.Substring(1, 2) == text)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task TopOne()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Single(products);
	}

	[Fact]
	public async Task Count()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var count = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.Count()
			.FindScalarAsync<int>().ConfigureAwait(false);
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task Get()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<Category>()
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(1, category.CategoryID);
	}

	[Fact]
	public async Task GetNonExisting()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		await AssertThrowsAsync<WebRequestException>(async () => await client
			.For<Category>()
			.Key(-1)
			.FindEntryAsync().ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task SelectSingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.Select(x => x.ProductName)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task SelectMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.Select(x => new { x.ProductID, x.ProductName })
			.FindEntryAsync().ConfigureAwait(false);
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
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		//var client = new ODataClient(settings);

		var product = await client
			.For<ProductWithUnmappedProperty>("Products")
			.Filter(x => x.ProductName == "Chai")
			.Select(x => new { x.ProductID, UnmappedName = x.ProductName })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.UnmappedName);
		Assert.Null(product.ProductName);
	}

	[Fact]
	public async Task ExpandOne()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.OrderBy(x => x.ProductID)
			.Expand(x => x.Category)
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Condiments", product.Category.CategoryName);
	}

	[Fact]
	public async Task ExpandManyAsArray()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<Category>()
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Beverages")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Length);
	}

	[Fact]
	public async Task ExpandManyAsList()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<CategoryWithList>("Categories")
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Beverages")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count);
	}

	[Fact]
	public async Task ExpandManyAsIList()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<CategoryWithIList>("Categories")
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Beverages")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count);
	}

	[Fact]
	public async Task ExpandManyAsHashSet()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<CategoryWithHashSet>("Categories")
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Beverages")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count);
	}

	[Fact]
	public async Task ExpandManyAsICollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<CategoryWithICollection>("Categories")
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Beverages")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfBeveragesProducts, category.Products.Count);
	}

	[Fact]
	public async Task ExpandSecondLevel()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.OrderBy(x => x.ProductID)
			.Expand(x => x.Category.Products)
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal(ExpectedCountOfCondimentsProducts, product.Category.Products.Length);
	}

	[Fact]
	public async Task ExpandMultipleLevelsWithCollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.OrderBy(x => x.ProductID)
			.Expand(x => x.Category.Products.Select(y => y.Category))
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Condiments", product.Category.Products.First().Category.CategoryName);
	}

	[Fact]
	public async Task ExpandWithSelect()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.OrderBy(x => x.ProductID)
			.Expand(x => x.Category)
			.Select(x => new { x.ProductName, x.Category.CategoryName })
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Condiments", product.Category.CategoryName);
	}

	[Fact]
	public async Task OrderBySingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.OrderBy(x => x.ProductName)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task OrderByMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Chai")
			.OrderBy(x => new { x.ProductID, x.ProductName })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task OrderByExpanded()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = (await client
			.For<Product>()
			.Expand(x => x.Category)
			.OrderBy(x => new { x.Category.CategoryName })
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Seafood", product.Category.CategoryName);
	}

	[Fact]
	public async Task NavigateToSingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<Product>()
			.Key(new { ProductID = 2 })
			.NavigateTo<Category>()
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Beverages", category.CategoryName);
	}

	[Fact]
	public async Task NavigateToSingleByExpression()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For<Product>()
			.Key(new { ProductID = 2 })
			.NavigateTo(x => x.Category)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Beverages", category.CategoryName);
	}

	[Fact]
	public async Task NavigateToMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Category>()
			.Key(2)
			.NavigateTo<Product>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfCondimentsProducts, products.Count());
	}

	[Fact]
	public async Task NavigateToRecursive()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Key(14)
			.NavigateTo<Employee>("Superior")
			.NavigateTo<Employee>("Superior")
			.NavigateTo<Employee>("Subordinates")
			.Key(3)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Janet", employee.FirstName);
	}

	[Fact]
	public async Task NavigateToRecursiveByExpression()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Key(14)
			.NavigateTo(x => x.Superior)
			.NavigateTo(x => x.Superior)
			.NavigateTo(x => x.Subordinates)
			.Key(3)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Janet", employee.FirstName);
	}

	[Fact]
	public async Task NavigateToRecursiveSingleClause()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Key(14)
			.NavigateTo(x => x.Superior.Superior.Subordinates)
			.Key(3)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Janet", employee.FirstName);
	}

	[Fact]
	public async Task BaseClassEntries()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var query = await client
			.For<Transport>()
			.FindEntriesAsync().ConfigureAwait(false);
		var transport = query.ToList();
		Assert.Equal(2, transport.Count);
	}

	[Fact]
	public async Task BaseClassEntriesWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var query = await client
			.For<Transport>()
			.FindEntriesAsync().ConfigureAwait(false);
		var transport = query.ToList();
		Assert.Equal(2, transport.Count);
	}

	[Fact]
	public async Task AllDerivedClassEntries()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.As<Ship>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.Single().ShipName);
	}

	[Fact]
	public async Task AllDerivedClassEntriesWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.As<Ship>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.Single().ShipName);
	}

	[Fact]
	public async Task DerivedClassEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.As<Ship>()
			.Filter(x => x.ShipName == "Titanic")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task BaseClassEntryByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(1, transport.TransportID);
	}

	[Fact]
	public async Task DerivedClassEntryByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.As<Ship>()
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task DerivedClassEntryBaseAndDerivedFields()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.As<Ship>()
			.Filter(x => x.TransportID == 1 && x.ShipName == "Titanic")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task IsOfDerivedClassEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var transport = await client
			.For<Transport>()
			.Filter(x => x is Ship)
			.As<Ship>()
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task IsOfAssociation()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Filter(x => x.Superior is Employee)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task CastToPrimitiveType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For<Product>()
			.Filter(x => x.CategoryID == (int)1L)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product);
	}

	[Fact]
	public async Task CastInstanceToEntityType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Filter(x => x != null)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task CastPropertyToEntityType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var employee = await client
			.For<Employee>()
			.Filter(x => x.Superior != null)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task FilterAny()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Order>()
			.Filter(x => x.OrderDetails.Any(y => y.Quantity > 50))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, products.Count());
	}

	[Fact]
	public async Task FilterAll()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client
			.For<Order>()
			.Filter(x => x.OrderDetails.All(y => y.Quantity > 50))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, products.Count());
	}

	private class Order_Details : OrderDetail { }

	private class OrderDetails
	{
		public int Order_ID { get; set; }
		public int product_id { get; set; }
		public int quantity { get; set; }
	}

	private class orderDetails
	{
		public int orderID { get; set; }
		public int productID { get; set; }
		public int quantity { get; set; }
	}

	[Fact]
	public async Task NameMatchResolverStrict()
	{
		var client = new ODataClient(CreateDefaultSettings()
			.WithNameResolver(ODataNameMatchResolver.Strict)
			.WithHttpMock());
		var orderDetails1 = await client
			.For<Order_Details>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.NotEmpty(orderDetails1);
		await AssertThrowsAsync<UnresolvableObjectException>(async () =>
			await client.For<OrderDetails>()
				.FindEntriesAsync().ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task NameMatchResolverNotStrict()
	{
		var client = new ODataClient(CreateDefaultSettings()
			.WithNameResolver(ODataNameMatchResolver.NotStrict)
			.WithHttpMock());
		var orderDetails1 = await client
			.For<Order_Details>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.NotEmpty(orderDetails1);
		Assert.True(orderDetails1.First().OrderID > 0);
		var orderDetails2 = await client
			.For<OrderDetails>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.NotEmpty(orderDetails2);
		Assert.True(orderDetails2.First().Order_ID > 0);
	}

	[Fact]
	public async Task NameMatchResolverCaseInsensitive()
	{
		var client = new ODataClient(CreateDefaultSettings()
			.WithNameResolver(ODataNameMatchResolver.AlpahumericCaseInsensitive)
			.WithHttpMock());
		var orderDetails = await client
			.For<orderDetails>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.NotEmpty(orderDetails);
		Assert.True(orderDetails.First().orderID > 0);
	}
}

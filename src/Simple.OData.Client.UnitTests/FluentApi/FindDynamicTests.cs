using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class FindDynamicTests : TestBase
{
	public FindDynamicTests()
		: base(true)
	{
	}

	[Fact]
	public async Task SingleCondition()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task SingleConditionWithVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var productName = "Chai";
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == productName)
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task CombinedConditions()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Employees)
			.Filter(x.FirstName == "Nancy" && x.HireDate < DateTime.Now)
			.FindEntryAsync();
		Assert.Equal("Davolio", product.LastName);
	}

	[Fact]
	public async Task CombinedConditionsWithMultipleFilters()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Employees)
			.Filter(x.FirstName == "Nancy")
			.Filter(x.HireDate < DateTime.Now)
			.FindEntryAsync();
		Assert.Equal("Davolio", product.LastName);
	}

	[Fact]
	public async Task CombineAll()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.OrderBy(x.ProductName)
			.ThenByDescending(x.UnitPrice)
			.Skip(2)
			.Top(1)
			.Expand(x.Category)
			.Select(x.Category)
			.FindEntriesAsync();
		Assert.Equal("Seafood", products.Single().Category.CategoryName);
	}

	[Fact]
	public async Task CombineAllReverse()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Select(x.Category)
			.Expand(x.Category)
			.Top(1)
			.Skip(2)
			.OrderBy(x.ProductName)
			.ThenByDescending(x.UnitPrice)
			.FindEntriesAsync();
		Assert.Equal("Seafood", products.Single().Category.CategoryName);
	}

	[Fact]
	public async Task StringContains()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.Contains("ai"))
			.FindEntriesAsync();
		Assert.Equal("Chai", products.Single().ProductName);
	}

	[Fact]
	public async Task StringContainsWithLocalVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var text = "ai";
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.Contains(text))
			.FindEntriesAsync();
		Assert.Equal("Chai", products.Single().ProductName);
	}

	[Fact]
	public async Task StringNotContains()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(!x.ProductName.Contains("ai"))
			.FindEntriesAsync();
		Assert.NotEqual("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task StringStartsWith()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.StartsWith("Ch"))
			.FindEntriesAsync();
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task LengthOfStringEqual()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.Length == 4)
			.FindEntriesAsync();
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task SubstringWithPositionAndLengthEqual()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.Substring(1, 2) == "ha")
			.FindEntriesAsync();
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task SubstringWithPositionAndLengthEqualWithLocalVariable()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var text = "ha";
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName.Substring(1, 2) == text)
			.FindEntriesAsync();
		Assert.Equal("Chai", products.First().ProductName);
	}

	[Fact]
	public async Task TopOne()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.Top(1)
			.FindEntriesAsync();
		Assert.Single(products);
	}

	[Fact]
	public async Task Count()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var count = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.Count()
			.FindScalarAsync<int>();
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task Get()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var category = await client
			.For(x.Categories)
			.Key(1)
			.FindEntryAsync();
		Assert.Equal(1, category.CategoryID);
	}

	[Fact]
	public async Task GetNonExisting()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		await AssertThrowsAsync<WebRequestException>(async () => await client
			.For(x.Categories)
			.Key(-1)
			.FindEntryAsync()).ConfigureAwait(false);
	}

	[Fact]
	public async Task SelectSingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.Select(x.ProductName)
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task SelectMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.Select(x.ProductID, x.ProductName)
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task ExpandOne()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = (await client
			.For(x.Products)
			.OrderBy(x.ProductID)
			.Expand(x.Category)
			.FindEntriesAsync() as IEnumerable<dynamic>).Last();
		Assert.Equal("Condiments", (product.Category.CategoryName));
	}

	[Fact]
	public async Task ExpandMany()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var category = await client
			.For(x.Category)
			.Expand(x.Products)
			.Filter(x.CategoryName == "Beverages")
			.FindEntryAsync();
		Assert.Equal(ExpectedCountOfBeveragesProducts, (category.Products as IEnumerable<dynamic>).Count());
	}

	[Fact]
	public async Task ExpandSecondLevel()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = (await client
			.For(x.Product)
			.OrderBy(x.ProductID)
			.Expand(x.Category.Products)
			.FindEntriesAsync() as IEnumerable<dynamic>).Last();
		Assert.Equal(ExpectedCountOfCondimentsProducts, (product.Category.Products as IEnumerable<dynamic>).Count());
	}

	[Fact]
	public async Task ExpandMultipleLevelsWithCollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = (await client
			.For(x.Product)
			.OrderBy(x.ProductID)
			.Expand(x.Category.Products.Category)
			.FindEntriesAsync() as IEnumerable<dynamic>).Last();
		Assert.Equal("Condiments", (product.Category.Products as IEnumerable<dynamic>).First().Category.CategoryName);
	}

	[Fact]
	public async Task ExpandWithSelect()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = (await client
			.For(x.Product)
			.OrderBy(x.ProductID)
			.Expand(x.Category.Products)
			.Select(x.ProductName, x.Category.CategoryName)
			.FindEntriesAsync() as IEnumerable<dynamic>).Last();
		Assert.Equal("Condiments", product.Category.CategoryName);
	}

	[Fact]
	public async Task OrderBySingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.OrderBy(x.ProductName)
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task OrderByMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.OrderBy(x.ProductID, x.ProductName)
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task OrderByExpanded()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = (await client
			.For(x.Products)
			.Expand(x.Category)
			.OrderBy(x.Category.CategoryName)
			.FindEntriesAsync() as IEnumerable<dynamic>).Last();
		Assert.Equal("Seafood", product.Category.CategoryName);
	}

	[Fact]
	public async Task NavigateToSingle()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var category = await client
			.For(x.Products)
			.Key(2)
			.NavigateTo(x.Category)
			.FindEntryAsync();
		Assert.Equal("Beverages", category.CategoryName);
	}

	[Fact]
	public async Task NavigateToMultiple()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> products = await client
			.For(x.Categories)
			.Key(2)
			.NavigateTo(x.Products)
			.FindEntriesAsync();
		Assert.Equal(ExpectedCountOfCondimentsProducts, products.Count());
	}

	[Fact]
	public async Task NavigateToRecursive()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var employee = await client
			.For(x.Employees)
			.Key(14)
			.NavigateTo(x.Superior)
			.NavigateTo(x.Superior)
			.NavigateTo(x.Subordinates)
			.Key(3)
			.FindEntryAsync();
		Assert.Equal("Janet", employee.FirstName);
	}

	[Fact]
	public async Task NavigateToRecursiveSingleClause()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var employee = await client
			.For(x.Employees)
			.Key(14)
			.NavigateTo(x.Superior.Superior.Subordinates)
			.Key(3)
			.FindEntryAsync();
		Assert.Equal("Janet", employee.FirstName);
	}

	[Fact]
	public async Task BaseClassEntries()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> transport = await client
			.For(x.Transport)
			.FindEntriesAsync();
		Assert.Equal(2, transport.Count());
		Assert.DoesNotContain(transport, y => y.AsDictionary().ContainsKey(FluentCommand.AnnotationsLiteral));
	}

	[Fact]
	public async Task BaseClassEntriesWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> transport = await client
			.For(x.Transport)
			.FindEntriesAsync();
		Assert.Equal(2, transport.Count());
		Assert.True(transport.All(y => y.AsDictionary().ContainsKey(FluentCommand.AnnotationsLiteral)));
	}

	[Fact]
	public async Task AllDerivedClassEntries()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		IEnumerable<dynamic> transport = await client
			.For(x.Transport)
			.As(x.Ships)
			.FindEntriesAsync();
		Assert.Equal("Titanic", transport.Single().ShipName);
	}

	[Fact]
	public async Task AllDerivedClassEntriesWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.As(x.Ships)
			.FindEntriesAsync();
		Assert.Equal("Titanic", (transport as IEnumerable<dynamic>).Single().ShipName);
		Assert.Equal("NorthwindModel.Ship", ((transport as IEnumerable<dynamic>).Single()[FluentCommand.AnnotationsLiteral] as ODataEntryAnnotations).TypeName);
	}

	[Fact]
	public async Task DerivedClassEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.As(x.Ships)
			.Filter(x.ShipName == "Titanic")
			.FindEntryAsync();
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task BaseClassEntryByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.Key(1)
			.FindEntryAsync();
		Assert.Equal(1, transport.TransportID);
	}

	[Fact]
	public async Task DerivedClassEntryByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.As(x.Ships)
			.Key(1)
			.FindEntryAsync();
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task DerivedClassEntryBaseAndDerivedFields()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.As(x.Ships)
			.Filter(x.TransportID == 1 && x.ShipName == "Titanic")
			.FindEntryAsync();
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task IsOfDerivedClassEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var transport = await client
			.For(x.Transport)
			.Filter(x.Is(typeof(Ship)))
			.FindEntryAsync();
		Assert.Equal("Titanic", transport.ShipName);
	}

	[Fact]
	public async Task IsOfAssociation()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var employee = await client
			.For(x.Employee)
			.Filter(x.Superior.Is(typeof(Employee)))
			.FindEntryAsync();
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task CastToPrimitiveType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var product = await client
			.For(x.Product)
			.Filter(x.CategoryID == (int)1L)
			.FindEntryAsync();
		Assert.NotNull(product);
	}

	[Fact]
	public async Task CastInstanceToEntityType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var employee = await client
			.For(x.Employee)
			.Filter(x.As(typeof(Employee)) != null)
			.FindEntryAsync();
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task CastPropertyToEntityType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var employee = await client
			.For(x.Employee)
			.Filter(x.Superior.As(typeof(Employee)) != null)
			.FindEntryAsync();
		Assert.NotNull(employee);
	}

	[Fact]
	public async Task FilterAny()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var products = await client
			.For(x.Orders)
			.Filter(x.OrderDetails.Any(x.Quantity > 50))
			.FindEntriesAsync();
		Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, (products as IEnumerable<dynamic>).Count());
	}

	[Fact]
	public async Task FilterAll()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var products = await client
			.For(x.Orders)
			.Filter(x.OrderDetails.All(x.Quantity > 50))
			.FindEntriesAsync();
		Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, (products as IEnumerable<dynamic>).Count());
	}

	[Fact]
	public async Task ImplicitCastToType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		Product product = await client
			.For(x.Products)
			.Filter(x.ProductName == "Chai")
			.FindEntryAsync();
		Assert.Equal("Chai", product.ProductName);
	}

	[Fact]
	public async Task ImplicitCastToTypeNullResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		Product product = await client
			.For(x.Products)
			.Filter(x.ProductName == "XYZ")
			.FindEntryAsync();
		Assert.Null(product);
	}
}

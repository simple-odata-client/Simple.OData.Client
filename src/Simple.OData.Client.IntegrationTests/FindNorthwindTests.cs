using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests;

public class Product
{
	public int ProductID { get; set; }
	public string ProductName { get; set; }
	public int? CategoryID { get; set; }

	public Category Category { get; set; }
}

public class Category
{
	public int CategoryID { get; set; }
	public string CategoryName { get; set; }

	public Product[] Products { get; set; }
}

public class FindNorthwindTestsV2Atom : FindNorthwindTests
{
	public FindNorthwindTestsV2Atom() : base(NorthwindV2ReadOnlyUri, ODataPayloadFormat.Atom) { }
}

public class FindNorthwindTestsV2Json : FindNorthwindTests
{
	public FindNorthwindTestsV2Json() : base(NorthwindV2ReadOnlyUri, ODataPayloadFormat.Json) { }
}

public class FindNorthwindTestsV3Atom : FindNorthwindTests
{
	public FindNorthwindTestsV3Atom() : base(NorthwindV3ReadOnlyUri, ODataPayloadFormat.Atom) { }
}

public class FindNorthwindTestsV3Json : FindNorthwindTests
{
	public FindNorthwindTestsV3Json() : base(NorthwindV3ReadOnlyUri, ODataPayloadFormat.Json) { }
}

public class FindNorthwindTestsV4Json : FindNorthwindTests
{
	public FindNorthwindTestsV4Json() : base(NorthwindV4ReadOnlyUri, ODataPayloadFormat.Json) { }
}

public abstract class FindNorthwindTests : TestBase
{
	protected FindNorthwindTests(string serviceUri, ODataPayloadFormat payloadFormat) : base(serviceUri, payloadFormat) { }

	protected async override Task DeleteTestData()
	{
		var products = await _client.For("Products").Select("ProductID", "ProductName").FindEntriesAsync().ConfigureAwait(false);
		foreach (var product in products)
		{
			if (product["ProductName"].ToString().StartsWith("Test"))
			{
				await _client.DeleteEntryAsync("Products", product).ConfigureAwait(false);
			}
		}

		var categories = await _client.For("Categories").Select("CategoryID", "CategoryName").FindEntriesAsync().ConfigureAwait(false);
		foreach (var category in categories)
		{
			if (category["CategoryName"].ToString().StartsWith("Test"))
			{
				await _client.DeleteEntryAsync("Categories", category).ConfigureAwait(false);
			}
		}

		var employees = await _client.For("Employees").Select("EmployeeID", "LastName").FindEntriesAsync().ConfigureAwait(false);
		foreach (var employee in employees)
		{
			if (employee["LastName"].ToString().StartsWith("Test"))
			{
				await _client.DeleteEntryAsync("Employees", employee).ConfigureAwait(false);
			}
		}
	}

	[Fact]
	public async Task Filter()
	{
		var products = await _client
			.For("Products")
			.Filter("ProductName eq 'Chai'")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Chai", products.Single()["ProductName"]);
	}

	[Fact]
	public async Task FilterStringExpression()
	{
		var x = ODataDynamic.Expression;
		var products = await _client
			.For(x.Products)
			.Filter(x.ProductName.Contains("ai"))
			.FindEntriesAsync();
		Assert.Equal("Chai", (products as IEnumerable<dynamic>).Single().ProductName);
	}

	[Fact]
	public async Task Get()
	{
		var category = await _client
			.For("Categories")
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(1, category["CategoryID"]);
	}

	[Fact]
	public async Task SkipOneTopOne()
	{
		var products = await _client
			.For("Products")
			.Skip(1)
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Single(products);
	}

	[Fact]
	public async Task OrderBy()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ProductName")
			.FindEntriesAsync().ConfigureAwait(false)).First();
		Assert.Equal("Alice Mutton", product["ProductName"]);
	}

	[Fact]
	public async Task SelectMultiple()
	{
		var product = await _client
			.For("Products")
			.Select("ProductID", "ProductName")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Contains("ProductName", product.Keys);
		Assert.Contains("ProductID", product.Keys);
	}

	[Fact]
	public async Task ExpandOne()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ProductID")
			.Expand("Category")
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Confections", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
	}

	[Fact]
	public async Task ExpandMany()
	{
		var category = await _client
			.For("Categories")
			.Expand("Products")
			.Filter("CategoryName eq 'Beverages'")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(12, (category["Products"] as IEnumerable<object>).Count());
	}

	[Fact]
	public async Task ExpandSecondLevel()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ProductID")
			.Expand("Category/Products")
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal(13, ((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>).Count());
	}

	[Fact]
	public async Task ExpandProductsOrderByCategoryName()
	{
		var product = (await _client
			.For<Product>()
			.Expand(x => x.Category)
			.OrderBy(x => x.Category.CategoryName)
			.Select(x => x.Category.CategoryName)
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal("Condiments", product.Category.CategoryName);
	}

	[Fact]
	public async Task ExpandCategoryOrderByProductName()
	{
		if (_serviceUri.AbsoluteUri == ODataV4ReadWriteUri)
		{
			var category = (await _client
				.For<Category>()
				.Expand(x => x.Products)
				.OrderBy(x => x.Products.Select(y => y.ProductName))
				.FindEntriesAsync().ConfigureAwait(false)).Last();
			Assert.Equal("Röd Kaviar", category.Products.Last().ProductName);
		}
	}

	[Fact]
	public async Task Count()
	{
		var count = await _client
			.For("Products")
			.Count()
			.FindScalarAsync<int>().ConfigureAwait(false);
		Assert.Equal(77, count);
	}

	[Fact]
	public async Task TotalCount()
	{
		var annotations = new ODataFeedAnnotations();
		var products = await _client
			.For("Products")
			.FindEntriesAsync(annotations).ConfigureAwait(false);
		Assert.Equal(77, annotations.Count);
		Assert.Equal(20, products.Count());
	}

	[Fact]
	public async Task CombineAll()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ProductName")
			.Skip(2)
			.Top(1)
			.Expand("Category")
			.Select("Category")
			.FindEntriesAsync().ConfigureAwait(false)).Single();
		Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
	}

	[Fact]
	public async Task NavigateToSingle()
	{
		var category = await _client
			.For("Products")
			.Key(new Entry() { { "ProductID", 2 } })
			.NavigateTo("Category")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Beverages", category["CategoryName"]);
	}

	[Fact]
	public async Task NavigateToMultiple()
	{
		var products = await _client
			.For("Categories")
			.Key(2)
			.NavigateTo("Products")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(12, products.Count());
	}

	[Fact]
	public async Task NavigateToRecursive()
	{
		var employee = await _client
			.For("Employees")
			.Key(6)
			.NavigateTo("Employee1")
			.NavigateTo("Employee1")
			.NavigateTo("Employees1")
			.Key(5)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Steven", employee["FirstName"]);
	}

	[Fact]
	public async Task NavigateToRecursiveSingleClause()
	{
		var employee = await _client
			.For("Employees")
			.Key(6)
			.NavigateTo("Employee1/Employee1/Employees1")
			.Key(5)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Steven", employee["FirstName"]);
	}
}

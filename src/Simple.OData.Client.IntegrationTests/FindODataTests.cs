using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests;

public class FindODataTestsV2Atom : FindODataTests
{
	public FindODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
}

public class FindODataTestsV2Json : FindODataTests
{
	public FindODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
}

public class FindODataTestsV3Atom : FindODataTests
{
	public FindODataTestsV3Atom() : base(ODataV3ReadOnlyUri, ODataPayloadFormat.Atom, 3) { }
}

public class FindODataTestsV3Json : FindODataTests
{
	public FindODataTestsV3Json() : base(ODataV3ReadOnlyUri, ODataPayloadFormat.Json, 3) { }
}

public class FindODataTestsV4Json : FindODataTests
{
	public FindODataTestsV4Json() : base(ODataV4ReadOnlyUri, ODataPayloadFormat.Json, 4) { }
}

public abstract class FindODataTests : ODataTestBase
{
	protected FindODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
		: base(serviceUri, payloadFormat, version)
	{
	}

	[Fact]
	public async Task Filter()
	{
		var products = await _client
			.For("Products")
			.Filter("Name eq 'Milk'")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal("Milk", products.Single()["Name"]);
	}

	[Fact]
	public async Task FilterStringExpression()
	{
		var x = ODataDynamic.Expression;
		var products = await _client
			.For(x.Products)
			.Filter(x.Name.Contains("lk"))
			.FindEntriesAsync();
		Assert.Equal("Milk", (products as IEnumerable<dynamic>).Single()["Name"]);
	}

	[Fact]
	public async Task Get()
	{
		var category = await _client
			.For("Categories")
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(1, category["ID"]);
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
			.OrderBy("Name")
			.FindEntriesAsync().ConfigureAwait(false)).First();
		Assert.Equal("Bread", product["Name"]);
	}

	[Fact]
	public async Task OrderByNestedComplex()
	{
		var supplier = (await _client
			.For("Suppliers")
			.OrderBy("Address/City")
			.FindEntriesAsync().ConfigureAwait(false)).First();
		Assert.Equal("Tokyo Traders", supplier["Name"]);
		supplier = (await _client
			.For("Suppliers")
			.OrderByDescending("Address/City")
			.FindEntriesAsync().ConfigureAwait(false)).First();
		Assert.Equal("Exotic Liquids", supplier["Name"]);
	}

	[Fact]
	public async Task SelectMultiple()
	{
		var product = await _client
			.For("Products")
			.Select("ID", "Name")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Contains("Name", product.Keys);
		Assert.Contains("ID", product.Keys);
	}

	[Fact]
	public async Task ExpandOne()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ID")
			.Expand(ProductCategoryName)
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal(ExpectedCategory, ProductCategoryFunc(product)["Name"]);
	}

	[Fact]
	public async Task ExpandMany()
	{
		var category = await _client
			.For("Categories")
			.Expand("Products")
			.Filter("Name eq 'Beverages'")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(ExpectedExpandMany, (category["Products"] as IEnumerable<object>).Count());
	}

	[Fact]
	public async Task ExpandSecondLevel()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("ID")
			.Expand(ProductCategoryName + "/Products")
			.FindEntriesAsync().ConfigureAwait(false)).Last();
		Assert.Equal(ExpectedExpandSecondLevel, (ProductCategoryFunc(product)["Products"] as IEnumerable<object>).Count());
	}

	[Fact]
	public async Task Count()
	{
		var count = await _client
			.For("Products")
			.Count()
			.FindScalarAsync<int>().ConfigureAwait(false);
		Assert.Equal(ExpectedCount, count);
	}

	[Fact]
	public async Task TotalCount()
	{
		var annotations = new ODataFeedAnnotations();
		var products = await _client
			.For("Products")
			.FindEntriesAsync(annotations).ConfigureAwait(false);
		Assert.Equal(ExpectedTotalCount, annotations.Count);
		Assert.Equal(ExpectedTotalCount, products.Count());
	}

	[Fact]
	public async Task CombineAll()
	{
		var product = (await _client
			.For("Products")
			.OrderBy("Name")
			.Skip(2)
			.Top(1)
			.Expand(ProductCategoryName)
			.Select(ProductCategoryName)
			.FindEntriesAsync().ConfigureAwait(false)).Single();
		Assert.Equal(ExpectedCategory, ProductCategoryFunc(product)["Name"]);
	}

	[Fact]
	public async Task NavigateToSingle()
	{
		var category = await _client
			.For("Products")
			.Key(new Dictionary<string, object>() { { "ID", 2 } })
			.NavigateTo(ProductCategoryName)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Beverages", category["Name"]);
	}

	[Fact]
	public async Task NavigateToMultiple()
	{
		var products = await _client
			.For("Categories")
			.Key(2)
			.NavigateTo("Products")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(2, products.Count());
	}

	[Fact]
	public async Task GetMediaStream()
	{
		if (_version == 2) // No media support in OData V2
		{
			return;
		}

		var ad = await _client
			.For("Advertisements")
			.FindEntryAsync().ConfigureAwait(false);
		var id = ad["ID"];
		var stream = await _client
			.For("Advertisements")
			.Key(id)
			.Media()
			.GetStreamAsync().ConfigureAwait(false);
		var text = Utils.StreamToString(stream);
		Assert.StartsWith("Test stream data", text);
	}

	[Fact]
	public async Task GetNamedMediaStream()
	{
		if (_version == 2) // No media support in OData V2
		{
			return;
		}

		var stream = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo("PersonDetail")
			.Media("Photo")
			.GetStreamAsync().ConfigureAwait(false);
		var text = Utils.StreamToString(stream);
		Assert.StartsWith("Test named stream data", text);
	}

	private class PersonDetail
	{
		public string Photo { get; set; }
	}

	[Fact]
	public async Task GetTypedNamedMediaStream()
	{
		if (_version == 2) // No media support in OData V2
		{
			return;
		}

		var text = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo<PersonDetail>()
			.Media(x => x.Photo)
			.GetStreamAsStringAsync().ConfigureAwait(false);
		Assert.StartsWith("Test named stream data", text);
	}
}

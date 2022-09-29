using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class LinkTypedTests : TestBase
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LinkEntry(bool useAbsoluteReferenceUris)
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
		var client = new ODataClient(settings);
		var category = await client
			.For<Category>()
			.Set(new { CategoryName = "Test4" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product = await client
			.For<Product>()
			.Set(new { ProductName = "Test5" })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For<Product>()
			.Key(product)
			.LinkEntryAsync(category).ConfigureAwait(false);

		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test5")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product.CategoryID);
		Assert.Equal(category.CategoryID, product.CategoryID);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnlinkEntry(bool useAbsoluteReferenceUris)
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
		var client = new ODataClient(settings);
		var category = await client
			.For<Category>()
			.Set(new { CategoryName = "Test4" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product = await client
			.For<Product>()
			.Set(new { ProductName = "Test5", CategoryID = category.CategoryID })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For<Product>()
			.Key(product)
			.UnlinkEntryAsync<Category>().ConfigureAwait(false);

		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test5")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Null(product.CategoryID);
	}
}

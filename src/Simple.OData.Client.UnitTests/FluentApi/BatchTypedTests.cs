using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class BatchTypedTests : TestBase
{
	[Fact]
	public async Task Success()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Set(new Product() { ProductName = "Test1", UnitPrice = 10m })
			.InsertEntryAsync(false);
		batch += c => c
			.For<Product>()
			.Set(new Product() { ProductName = "Test2", UnitPrice = 20m })
			.InsertEntryAsync(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		var client = new ODataClient(settings);
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test1")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product);
		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test2")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product);
	}

	[Fact]
	public async Task SuccessWithResults()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		Product? product1 = null;
		Product? product2 = null;

		var batch = new ODataBatch(settings);
		batch += async c => product1 = await c
			.For<Product>()
			.Set(new Product() { ProductName = "Test1", UnitPrice = 10m })
			.InsertEntryAsync().ConfigureAwait(false);
		batch += async c => product2 = await c
			.For<Product>()
			.Set(new Product() { ProductName = "Test2", UnitPrice = 20m })
			.InsertEntryAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.NotEqual(0, product1.ProductID);
		Assert.NotEqual(0, product2.ProductID);

		var client = new ODataClient(settings);
		product1 = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test1")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product1);
		product2 = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test2")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(product2);
	}

	[Fact]
	public async Task PartialFailures()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Set(new { ProductName = "Test1", UnitPrice = 10m })
			.InsertEntryAsync(false);
		batch += c => c
			.For<Product>()
			.Set(new { ProductName = "Test2", UnitPrice = 20m, SupplierID = 0xFFFF })
			.InsertEntryAsync(false);

		try
		{
			await batch.ExecuteAsync().ConfigureAwait(false);
		}
		catch (WebRequestException exception)
		{
			Assert.NotNull(exception.Response);
		}
	}

	[Fact]
	public async Task AllFailures()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Set(new { UnitPrice = 10m })
			.InsertEntryAsync(false);
		batch += c => c
			.For<Product>()
			.Set(new { UnitPrice = 20m })
			.InsertEntryAsync(false);

		try
		{
			await batch.ExecuteAsync().ConfigureAwait(false);
		}
		catch (WebRequestException exception)
		{
			Assert.NotNull(exception.Response);
		}
	}

	[Fact]
	public async Task MultipleUpdatesSingleBatch()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		Product? product = null;
		Product? product1 = null;
		Product? product2 = null;

		var batch = new ODataBatch(settings);
		batch += async c => product = await c
			.For<Product>()
			.Set(new { ProductName = "Test11", UnitPrice = 21m })
			.InsertEntryAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Key(product.ProductID)
			.Set(new { UnitPrice = 22m })
			.UpdateEntryAsync(false);
		batch += async c => product1 = await c
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		batch += c => c
			.For<Product>()
			.Key(product.ProductID)
			.Set(new { UnitPrice = 23m })
			.UpdateEntryAsync(false);
		batch += async c => product2 = await c
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(22m, product1.UnitPrice);
		Assert.Equal(23m, product2.UnitPrice);

		var client = new ODataClient(settings);
		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(23m, product.UnitPrice);
	}

	[Fact]
	public async Task UpdateDeleteSingleBatch()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		Product? product = null;
		Product? product1 = null;
		Product? product2 = null;

		var batch = new ODataBatch(settings);
		batch += async c => product = await c
			.For<Product>()
			.Set(new { ProductName = "Test11", UnitPrice = 21m })
			.InsertEntryAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.Set(new { UnitPrice = 22m })
			.UpdateEntryAsync(false);
		batch += async c => product1 = await c
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		batch += c => c
			.For<Product>()
			.Key(product.ProductID)
			.DeleteEntryAsync();
		batch += async c => product2 = await c
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(22m, product1.UnitPrice);
		Assert.Null(product2);

		var client = new ODataClient(settings);
		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test11")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Null(product);
	}

	[Fact]
	public async Task InsertUpdateDeleteSeparateBatches()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Set(new { ProductName = "Test12", UnitPrice = 21m })
			.InsertEntryAsync(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		var client = new ODataClient(settings);
		var product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test12")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(21m, product.UnitPrice);
		var productID = product.ProductID;

		batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Key(productID)
			.Set(new { UnitPrice = 22m })
			.UpdateEntryAsync(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test12")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(22m, product.UnitPrice);

		batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Key(productID)
			.DeleteEntryAsync();
		await batch.ExecuteAsync().ConfigureAwait(false);

		product = await client
			.For<Product>()
			.Filter(x => x.ProductName == "Test12")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Null(product);
	}

	[Fact]
	public async Task InsertSingleEntityWithSingleAssociationSingleBatch()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var category = new Category() { CategoryName = "Test13" };
		var batch = new ODataBatch(settings);
		batch += async c => await c
			.For<Category>()
			.Set(category)
			.InsertEntryAsync().ConfigureAwait(false);
		batch += c => c
			.For<Product>()
			.Set(new { ProductName = "Test14", UnitPrice = 21m, Category = category })
			.InsertEntryAsync();
		await batch.ExecuteAsync().ConfigureAwait(false);

		var client = new ODataClient(settings);
		var product = await client
			.For<Product>()
			.Expand(x => x.Category)
			.Filter(x => x.ProductName == "Test14")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Test13", product.Category.CategoryName);
	}

	[Fact]
	public async Task InsertSingleEntityWithMultipleAssociationsSingleBatch()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var product1 = new Product() { ProductName = "Test15", UnitPrice = 21m };
		var product2 = new Product() { ProductName = "Test16", UnitPrice = 22m };

		var batch = new ODataBatch(settings);
		batch += c => c
			.For<Product>()
			.Set(product1)
			.InsertEntryAsync(false);
		batch += c => c
			.For<Product>()
			.Set(product2)
			.InsertEntryAsync(false);
		batch += async c => await c
			.For<Category>()
			.Set(new { CategoryName = "Test17", Products = new[] { product1, product2 } })
			.InsertEntryAsync(false).ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		var client = new ODataClient(settings);
		var category = await client
			.For<Category>()
			.Expand(x => x.Products)
			.Filter(x => x.CategoryName == "Test17")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(2, category.Products.Length);
	}
}

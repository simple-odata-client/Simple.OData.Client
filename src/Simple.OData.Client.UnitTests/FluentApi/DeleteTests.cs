using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class DeleteTests : TestBase
{
	[Fact]
	public async Task DeleteByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product["ProductID"])
			.DeleteEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact(Skip = "Cannot be mocked")]
	public async Task DeleteByKeyClearMetadataCache()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		client.Session.ClearMetadataCache();
		await client
			.For("Products")
			.Key(product["ProductID"])
			.DeleteEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task DeleteByFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.DeleteEntriesAsync().ConfigureAwait(false);

		var product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task DeleteByFilterFromCommand()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		var commandText = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.GetCommandTextAsync().ConfigureAwait(false);

		await client
			.DeleteEntriesAsync("Products", commandText).ConfigureAwait(false);

		var product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task DeleteByObjectAsKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product)
			.DeleteEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task DeleteDerived()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var ship = await client
			.For("Transport")
			.As("Ship")
			.Set(new { ShipName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Transport")
			.As("Ship")
			.Key(ship["TransportID"])
			.DeleteEntryAsync().ConfigureAwait(false);

		ship = await client
			.For("Transport")
			.As("Ship")
			.Filter("ShipName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(ship);
	}
}

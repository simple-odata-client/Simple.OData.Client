using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.BasicApi;

using Entry = System.Collections.Generic.Dictionary<string, object>;

public class ClientReadWriteTests : TestBase
{
	[Fact]
	public async Task InsertEntryWithResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 18m } }, true).ConfigureAwait(false);

		Assert.Equal("Test1", product["ProductName"]);
	}

	[Fact]
	public async Task InsertEntryNoResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test2" }, { "UnitPrice", 18m } }, false).ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task InsertEntrySubcollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true).ConfigureAwait(false);

		Assert.Equal("Test1", ship["ShipName"]);
	}

	[Fact]
	public async Task UpdateEntryWithResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var key = new Entry() { { "ProductID", 1 } };
		var product = await client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, true).ConfigureAwait(false);

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateEntryNoResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var key = new Entry() { { "ProductID", 1 } };
		var product = await client.UpdateEntryAsync("Products", key, new Entry() { { "ProductName", "Chai" }, { "UnitPrice", 123m } }, false).ConfigureAwait(false);
		Assert.Null(product);

		product = await client.GetEntryAsync("Products", key).ConfigureAwait(false);
		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateEntrySubcollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true).ConfigureAwait(false);
		var key = new Entry() { { "TransportID", ship["TransportID"] } };
		await client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } }).ConfigureAwait(false);

		ship = await client.GetEntryAsync("Transport", key).ConfigureAwait(false);
		Assert.Equal("Test2", ship["ShipName"]);
	}

	[Fact]
	public async Task UpdateEntrySubcollectionWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test1" } }, true).ConfigureAwait(false);
		var key = new Entry() { { "TransportID", ship["TransportID"] } };
		await client.UpdateEntryAsync("Transport/Ships", key, new Entry() { { "ShipName", "Test2" } }).ConfigureAwait(false);

		ship = await client.GetEntryAsync("Transport", key).ConfigureAwait(false);
		Assert.Equal("Test2", ship["ShipName"]);
	}

	[Fact]
	public async Task DeleteEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test3" }, { "UnitPrice", 18m } }, true).ConfigureAwait(false);
		var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'").ConfigureAwait(false);
		Assert.NotNull(product);

		await client.DeleteEntryAsync("Products", product).ConfigureAwait(false);

		product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test3'").ConfigureAwait(false);
		Assert.Null(product);
	}

	[Fact]
	public async Task DeleteEntrySubCollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true).ConfigureAwait(false);
		ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]).ConfigureAwait(false);
		Assert.NotNull(ship);

		await client.DeleteEntryAsync("Transport", ship).ConfigureAwait(false);

		ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]).ConfigureAwait(false);
		Assert.Null(ship);
	}

	[Fact]
	public async Task DeleteEntrySubCollectionWithAnnotations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
		var ship = await client.InsertEntryAsync("Transport/Ships", new Entry() { { "ShipName", "Test3" } }, true).ConfigureAwait(false);
		ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]).ConfigureAwait(false);
		Assert.NotNull(ship);

		await client.DeleteEntryAsync("Transport", ship).ConfigureAwait(false);

		ship = await client.FindEntryAsync("Transport?$filter=TransportID eq " + ship["TransportID"]).ConfigureAwait(false);
		Assert.Null(ship);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LinkEntry(bool useAbsoluteReferenceUris)
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
		var client = new ODataClient(settings);
		var category = await client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test4" } }, true).ConfigureAwait(false);
		var product = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test5" } }, true).ConfigureAwait(false);

		await client.LinkEntryAsync("Products", product, "Category", category).ConfigureAwait(false);

		product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test5'").ConfigureAwait(false);
		Assert.NotNull(product["CategoryID"]);
		Assert.Equal(category["CategoryID"], product["CategoryID"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnlinkEntry(bool useAbsoluteReferenceUris)
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
		var client = new ODataClient(settings);
		var category = await client.InsertEntryAsync("Categories", new Entry() { { "CategoryName", "Test6" } }, true).ConfigureAwait(false);
		_ = await client.InsertEntryAsync("Products", new Entry() { { "ProductName", "Test7" }, { "CategoryID", category["CategoryID"] } }, true).ConfigureAwait(false);
		var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'").ConfigureAwait(false);
		Assert.NotNull(product["CategoryID"]);
		Assert.Equal(category["CategoryID"], product["CategoryID"]);

		await client.UnlinkEntryAsync("Products", product, "Category").ConfigureAwait(false);

		product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Test7'").ConfigureAwait(false);
		Assert.Null(product["CategoryID"]);
	}
}

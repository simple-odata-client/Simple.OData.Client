using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.BasicApi;

using Entry = System.Collections.Generic.Dictionary<string, object>;

public class ClientReadOnlyTests : TestBase
{
	public ClientReadOnlyTests()
		: base(true)
	{
	}

	[Fact]
	public async Task FindEntries()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client.FindEntriesAsync("Products").ConfigureAwait(false);
		Assert.True(products.Any());
	}

	[Fact]
	public async Task FindEntriesNonExisting()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client.FindEntriesAsync("Products?$filter=ProductID eq -1").ConfigureAwait(false);
		Assert.True(!products.Any());
	}

	[Fact]
	public async Task FindEntriesSelect()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var products = await client.FindEntriesAsync("Products?$select=ProductName").ConfigureAwait(false);
		Assert.Equal(1, products.First().Count);
		Assert.Equal("ProductName", products.First().First().Key);
	}

	[Fact]
	public async Task FindEntriesFilterAny()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var orders = await client.FindEntriesAsync("Orders?$filter=Order_Details/any(d:d/Quantity gt 50)").ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, orders.Count());
	}

	[Fact]
	public async Task FindEntriesFilterAll()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var orders = await client.FindEntriesAsync("Orders?$filter=Order_Details/all(d:d/Quantity gt 50)").ConfigureAwait(false);
		Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, orders.Count());
	}

	[Fact]
	public async Task FindEntry()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client.FindEntryAsync("Products?$filter=ProductName eq 'Chai'").ConfigureAwait(false);
		Assert.Equal("Chai", product["ProductName"]);
	}

	[Fact]
	public async Task GetEntryExisting()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client.GetEntryAsync("Products", new Entry() { { "ProductID", 1 } }).ConfigureAwait(false);
		Assert.Equal("Chai", product["ProductName"]);
	}

	[Fact]
	public async Task GetEntryExistingCompoundKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var orderDetail = await client.GetEntryAsync("Order_Details", new Entry() { { "OrderID", 10248 }, { "ProductID", 11 } }).ConfigureAwait(false);
		Assert.Equal(11, orderDetail["ProductID"]);
	}

	[Fact]
	public async Task GetEntryNonExisting()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		await AssertThrowsAsync<WebRequestException>(async () => await client.GetEntryAsync("Products", new Entry() { { "ProductID", -1 } }).ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task GetEntryNonExistingIgnoreException()
	{
		var client = new ODataClient(CreateDefaultSettings().WithIgnoredResourceNotFoundException().WithHttpMock());
		var product = await client.GetEntryAsync("Products", new Entry() { { "ProductID", -1 } }).ConfigureAwait(false);

		Assert.Null(product);
	}

	[Fact]
	public async Task ExecuteScalarFunctionWithStringParameter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client.ExecuteFunctionAsScalarAsync<int>("ParseInt", new Entry() { { "number", "1" } }).ConfigureAwait(false);
		Assert.Equal(1, result);
	}

	[Fact]
	public async Task ExecuteScalarFunctionWithLongParameter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client.ExecuteFunctionAsScalarAsync<long>("PassThroughLong", new Entry() { { "number", 1L } }).ConfigureAwait(false);
		Assert.Equal(1L, result);
	}

	[Fact]
	public async Task ExecuteScalarFunctionWithDateTimeParameter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var dateTime = new DateTime(2013, 1, 1, 12, 13, 14, 789, DateTimeKind.Utc);
		var result = await client.ExecuteFunctionAsScalarAsync<DateTime>("PassThroughDateTime", new Entry() { { "dateTime", dateTime } }).ConfigureAwait(false);
		Assert.Equal(dateTime.ToUniversalTime(), result);
	}

	[Fact]
	public async Task ExecuteScalarFunctionWithGuidParameter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var guid = new Guid("7d8f1758-00d4-4c53-a2e3-8fca73ebb92c");
		var result = await client.ExecuteFunctionAsScalarAsync<Guid>("PassThroughGuid", new Entry() { { "guid", guid } }).ConfigureAwait(false);
		Assert.Equal(guid, result);
	}

	[Fact]
	public async Task FindEntryExistingDynamicFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var filter = await ((Task<string>)client.GetCommandTextAsync("Products", x.ProductName == "Chai")).ConfigureAwait(false);
		var product = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Chai", product["ProductName"]);
	}

	[Fact]
	public async Task FindBaseClassEntryDynamicFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var filter = await ((Task<string>)client.GetCommandTextAsync("Transport", x.TransportID == 1)).ConfigureAwait(false);
		var ship = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Titanic", ship["ShipName"]);
	}

	[Fact]
	public async Task FindDerivedClassEntryDynamicFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var x = ODataDynamic.Expression;
		var filter = await ((Task<string>)client.GetCommandTextAsync("Transport/Ships", x.ShipName == "Titanic")).ConfigureAwait(false);
		var ship = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Titanic", ship["ShipName"]);
	}

	[Fact]
	public async Task FindEntryExistingTypedFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var filter = await client.GetCommandTextAsync<Product>("Products", x => x.ProductName == "Chai").ConfigureAwait(false);
		var product = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Chai", product["ProductName"]);
	}

	[Fact]
	public async Task FindBaseClassEntryTypedFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var filter = await client.GetCommandTextAsync<Transport>("Transport", x => x.TransportID == 1).ConfigureAwait(false);
		var ship = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Titanic", ship["ShipName"]);
	}

	[Fact]
	public async Task FindDerivedClassEntryTypedFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var filter = await client.GetCommandTextAsync<Ship>("Transport/Ships", x => x.ShipName == "Titanic").ConfigureAwait(false);
		var ship = await client.FindEntryAsync(filter).ConfigureAwait(false);
		Assert.Equal("Titanic", ship["ShipName"]);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

using Entry = System.Collections.Generic.Dictionary<string, object>;

public class FunctionTests : TestBase
{
	public FunctionTests()
		: base(true)
	{
	}

	[Fact]
	public async Task FunctionWithStringToInt()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound()
			.Function("ParseInt")
			.Set(new Entry() { { "number", "1" } })
			.ExecuteAsScalarAsync<int>().ConfigureAwait(false);

		Assert.Equal(1, result);
	}

	[Fact]
	public async Task FunctionWithString()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound()
			.Function("ReturnString")
			.Set(new Entry() { { "text", "abc" } })
			.ExecuteAsScalarAsync<string>().ConfigureAwait(false);

		Assert.Equal("abc", result);
	}

	[Fact]
	public async Task FunctionWithStringAsCollection()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound<string>()
			.Function("ReturnString")
			.Set(new Entry() { { "text", "abc" } })
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		Assert.Equal("abc", result);
	}

	[Fact]
	public async Task FunctionWithIntCollectionSingleElement()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound()
			.Function("ReturnIntCollection")
			.Set(new Entry() { { "count", 1 } })
			.ExecuteAsArrayAsync<int>().ConfigureAwait(false);

		Assert.Equal(new[] { 1 }, result);
	}

	[Fact]
	public async Task FunctionWithIntCollectionMultipleElements()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound()
			.Function("ReturnIntCollection")
			.Set(new Entry() { { "count", 3 } })
			.ExecuteAsArrayAsync<int>().ConfigureAwait(false);

		Assert.Equal(new[] { 1, 2, 3 }, result);
	}

	[Fact]
	public async Task FunctionWithLong()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = await client
			.Unbound()
			.Function("PassThroughLong")
			.Set(new Entry() { { "number", 1L } })
			.ExecuteAsScalarAsync<long>().ConfigureAwait(false);

		Assert.Equal(1L, result);
	}

	[Fact]
	public async Task FunctionWithDateTime()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var dateTime = new DateTime(2013, 1, 1, 12, 13, 14);
		var result = await client
			.Unbound()
			.Function("PassThroughDateTime")
			.Set(new Entry() { { "dateTime", dateTime } })
			.ExecuteAsScalarAsync<DateTime>().ConfigureAwait(false);

		Assert.Equal(dateTime, result);
	}

	[Fact]
	public async Task FunctionWithLocalDateTime()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var dateTime = DateTime.Parse("2018-05-20T20:30:40.6779345+02:00");
		var result = await client
			.Unbound()
			.Function("PassThroughDateTime")
			.Set(new Entry() { { "dateTime", dateTime } })
			.ExecuteAsScalarAsync<DateTime>().ConfigureAwait(false);

		Assert.Equal(dateTime, result);
	}

	[Fact]
	public async Task FunctionWithGuid()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var guid = new Guid("8DA69EAD-C2DC-4E1E-A588-BA9EB6AA7294");
		var result = await client
			.Unbound()
			.Function("PassThroughGuid")
			.Set(new Entry() { { "guid", guid } })
			.ExecuteAsScalarAsync<Guid>().ConfigureAwait(false);

		Assert.Equal(guid, result);
	}

	[Fact]
	public async Task FunctionWithComplexType()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var address = new Address { City = "Oslo", Country = "Norway", Region = "Oslo", PostalCode = "1234" };
		var result = await client
			.Unbound<IDictionary<string, object>>()
			.Action("PassThroughAddress")
			.Set(new Entry() { { "address", address } })
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		result = result["PassThroughAddress"] as IDictionary<string, object>;
		Assert.Equal("Oslo", result["City"]);
		Assert.Equal("Norway", result["Country"]);
	}

	[Fact]
	public async Task FunctionWithComplexTypeCollectionSingleElement()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = (await client
			.Unbound<IDictionary<string, object>>()
			.Function("ReturnAddressCollection")
			.Set(new Entry() { { "count", 1 } })
			.ExecuteAsSingleAsync().ConfigureAwait(false));

		Assert.Equal("Oslo", result["City"]);
		Assert.Equal("Norway", result["Country"]);
	}

	[Fact]
	public async Task FunctionWithComplexTypeCollectionMultipleElements()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = (await client
			.Unbound<IDictionary<string, object>>()
			.Function("ReturnAddressCollection")
			.Set(new Entry() { { "count", 3 } })
			.ExecuteAsEnumerableAsync().ConfigureAwait(false)).ToArray();

		Assert.Equal("Oslo", result[0]["City"]);
		Assert.Equal("Norway", result[0]["Country"]);
		Assert.Equal("Oslo", result[1]["City"]);
		Assert.Equal("Oslo", result[2]["City"]);
	}

	[Fact]
	public async Task FunctionWithComplexTypeCollectionEmpty()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var result = (await client
			.Unbound<IDictionary<string, object>>()
			.Function("ReturnAddressCollection")
			.Set(new Entry() { { "count", 0 } })
			.ExecuteAsEnumerableAsync().ConfigureAwait(false)).ToArray();

		Assert.Empty(result);
	}
}

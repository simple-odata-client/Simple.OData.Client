using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests.FluentApi;

public class WithHeaderTests : TestBase
{
	[Fact]
	public async Task WithHeader()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());

		var request = await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.WithHeader("header2", "header2Value")
			.BuildRequestFor()
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal("header1Value", request.GetRequest().RequestMessage.Headers.GetValues("header1").SingleOrDefault());
		Assert.Equal("header2Value", request.GetRequest().RequestMessage.Headers.GetValues("header2").SingleOrDefault());
	}

	[Fact]
	public async Task WithHeaders()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());

		var request = await client
			.For("Products")
			.WithHeaders(new Dictionary<string, string>{
					{ "header1", "header1Value" },
					{ "header2", "header2Value" }})
			.BuildRequestFor()
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal("header1Value", request.GetRequest().RequestMessage.Headers.GetValues("header1").SingleOrDefault());
		Assert.Equal("header2Value", request.GetRequest().RequestMessage.Headers.GetValues("header2").SingleOrDefault());
	}

	private (ODataClient client, IDictionary<string, IEnumerable<string>> headers) CreateClient()
	{
		var headers = new Dictionary<string, IEnumerable<string>>();
		return (new ODataClient(CreateDefaultSettings().WithHttpMock().WithRequestInterceptor(r => r.Headers.ToList().ForEach(x => headers.Add(x.Key, x.Value)))), headers);
	}

	private static void AssertHeader(IDictionary<string, IEnumerable<string>> headers, string name, string value)
	{
		Assert.True(headers.TryGetValue(name, out var values) && values.Single() == value);
	}

	private static void AssertHeader(ODataRequest request, string name, string value)
	{
		Assert.True(request.RequestMessage.Headers.TryGetValues(name, out var values) && values.Single() == value);
	}

	[Fact]
	public async Task BuildRequestFor()
	{
		var (client, headers) = CreateClient();

		var requestClient = await client
			.For("Categories")
			.WithHeader("header1", "header1Value")
			.Key(1)
			.BuildRequestFor()
			.FindEntryAsync().ConfigureAwait(false);

		var request = requestClient.GetRequest();

		AssertHeader(request, "header1", "header1Value");


		await requestClient.RunAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
		AssertHeader(request, "header1", "header1Value");

		//Clear first run captured headers
		headers.Clear();
		//Run twice to assert no duplicate headers added
		await requestClient.RunAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
		AssertHeader(request, "header1", "header1Value");
	}

	[Fact]
	public async Task FindEntry()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Categories")
			.WithHeader("header1", "header1Value")
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task GetStream()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Categories")
			.WithHeader("header1", "header1Value")
			.Key(1)
			.Media()
			.GetStreamAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task SetStream()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Categories")
			.WithHeader("header1", "header1Value")
			.Key(1)
			.Media()
			.SetStreamAsync("stream_data", false).ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task FindEntries()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Filter("ProductName eq 'Chai'")
			.FindEntriesAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task FindEntriesWithAnnotations()
	{
		var annotations = new ODataFeedAnnotations();
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Filter("ProductName eq 'Chai'")
			.FindEntriesAsync(annotations).ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task FindScalar()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Count()
			.FindScalarAsync<int>().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task Function()
	{
		var (client, headers) = CreateClient();

		await client
			.Unbound()
			.WithHeader("header1", "header1Value")
			.Function("ReturnString")
			.Set(new Entry() { { "text", "abc" } })
			.ExecuteAsScalarAsync<string>().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task InsertEntry()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Set(new Entry() { { "ProductName", "Test1" }, { "UnitPrice", 18m } })
			.InsertEntryAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task UpdateEntry()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Key(1171)
			.Set(new { UnitPrice = 123m })
			.UpdateEntryAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task DeleteEntry()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Key(1109)
			.DeleteEntryAsync().ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task LinkEntry()
	{
		var (client, headers) = CreateClient();

		var category = new Entry { { "CategoryID", 1003 } };

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Key(1004)
			.LinkEntryAsync("Category", category).ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}

	[Fact]
	public async Task UnlinkEntry()
	{
		var (client, headers) = CreateClient();

		await client
			.For("Products")
			.WithHeader("header1", "header1Value")
			.Key(1008)
			.UnlinkEntryAsync("Category").ConfigureAwait(false);

		AssertHeader(headers, "header1", "header1Value");
	}
}

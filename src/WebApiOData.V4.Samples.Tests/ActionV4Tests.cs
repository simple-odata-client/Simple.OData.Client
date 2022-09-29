using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Simple.OData.Client;
using Simple.OData.Client.Tests;
using WebApiOData.V4.Samples.Models;
using Xunit;
#if NET461 && !MOCK_HTTP
using Microsoft.Owin.Testing;
using WebApiOData.V4.Samples.Startups;
#endif

namespace WebApiOData.V4.Samples.Tests;

public class ActionV4Tests
{
#if NET461 && !MOCK_HTTP
        private readonly TestServer _server;

        public ActionV4Tests()
        {
            _server = TestServer.Create<ActionStartup>();
        }

        private void Dispose()
        {
            _server.Dispose();
        }
#endif

	private static ODataClientSettings CreateDefaultSettings()
	{
		return new ODataClientSettings()
		{
			BaseUri = new Uri("http://localhost/actions"),
			MetadataDocument = GetMetadataDocument(),
			PayloadFormat = ODataPayloadFormat.Json,
#if NET461 && !MOCK_HTTP
                OnCreateMessageHandler = () => _server.Handler,
#endif
			OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
		};
	}

	private static string GetMetadataDocument()
	{
#if MOCK_HTTP
		return MetadataResolver.GetMetadataDocument("Metadata.xml");
#else
            return null;
#endif

	}

	[Fact]
	public async Task Check_out_a_movie()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var isCheckedOut = false;
		Movie? result = null;
		try
		{
			result = await client
				.For<Movie>()
				.Key(1)
				.Action("CheckOut")
				.ExecuteAsSingleAsync().ConfigureAwait(false);
		}
		catch (WebRequestException)
		{
			isCheckedOut = true;
		}

		if (isCheckedOut)
		{
			await client
				.For<Movie>()
				.Key(1)
				.Action("Return")
				.ExecuteAsSingleAsync().ConfigureAwait(false);

			result = await client
				.For<Movie>()
				.Key(1)
				.Action("CheckOut")
				.ExecuteAsSingleAsync().ConfigureAwait(false);
		}

		Assert.Equal(1, result.ID);
	}

	[Fact]
	public async Task Return_a_movie()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var result = await client
			.For<Movie>()
			.Key(1)
			.Action("Return")
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		Assert.Equal(1, result.ID);
	}

	[Fact]
	public async Task Check_out_several()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var result = await client
			.For<Movie>()
			.Action("CheckOutMany")
			.Set(new Dictionary<string, object>() { { "MovieIDs", new[] { 1, 2, 3 } } })
			.ExecuteAsEnumerableAsync().ConfigureAwait(false);

		Assert.True(result.Count() > 1);
	}

	[Fact]
	public async Task CreateMovie()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var guid = new Guid("EC9DB412-AF7F-4157-B9F5-BFAB8A942B16");
		var result = await client
			.Unbound<Movie>()
			.Action("CreateMovie")
			.Set(new { Title = guid.ToString() })
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		Assert.True(result.ID > 0);
	}

	[Fact]
	public async Task CreateMovie_batch()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var guid = new Guid("E7857D7F-5B85-406A-A5C7-0DBA1D411576");
		Movie? result = null;
		var batch = new ODataBatch(client);
		batch += async c => result = await c
			.Unbound<Movie>()
			.Action("CreateMovie")
			.Set(new { Title = guid.ToString() })
			.ExecuteAsSingleAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.True(result.ID > 0);
	}
}

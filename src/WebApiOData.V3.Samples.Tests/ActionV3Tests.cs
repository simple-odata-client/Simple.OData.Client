using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if NET461 && !MOCK_HTTP
using Microsoft.Owin.Testing;
#endif
using Xunit;
using Simple.OData.Client;
using Simple.OData.Client.Tests;
using WebApiOData.V3.Samples.Models;

namespace WebApiOData.V3.Samples.Tests;

public class ActionV3Tests : IDisposable
{
#if NET461 && !MOCK_HTTP
        private readonly TestServer _server;

        public ActionV3Tests()
        {
            _server = TestServer.Create<Startup>();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
#else
	public void Dispose()
	{
		GC.SuppressFinalize(this);
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
		var guid = new Guid("6B968CA9-4822-49EE-90BD-0439AAA48E9A");
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
		var guid = new Guid("2C44053F-6790-4221-934E-BA214DFEB643");
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

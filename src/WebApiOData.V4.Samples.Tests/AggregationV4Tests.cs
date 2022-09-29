using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Simple.OData.Client;
using Simple.OData.Client.Tests;
using Simple.OData.Client.V4.Adapter.Extensions;
using WebApiOData.V4.Samples.Models;
using Xunit;

#if NET461 && !MOCK_HTTP
using Microsoft.Owin.Testing;
using WebApiOData.V4.Samples.Startups;
#endif

namespace WebApiOData.V4.Samples.Tests;

public class AggregationV4Tests : IDisposable
{
#if NET461 && !MOCK_HTTP
        private readonly TestServer _server;

        public FunctionV4Tests()
        {
            _server = TestServer.Create<FunctionStartup>();
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
			BaseUri = new Uri("http://localhost/functions"),
			MetadataDocument = GetMetadataDocument(),
			PayloadFormat = ODataPayloadFormat.Json,
#if NET461 && !MOCK_HTTP
                OnCreateMessageHandler = () => _server.Handler,
#endif
			OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
			IgnoreUnmappedProperties = true
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
	public async Task Get_movie_count_by_year_typed()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var result = await client
			.WithExtensions()
			.For<Movie>()
			.Apply(b => b.GroupBy((x, a) => new MovieCountByYear
			{
				Year = x.Year,
				Count = a.Count()
			}))
			.OrderByDescending(x => x.Count)
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(3, result.Count());
		Assert.Equal(new[] { 1990, 1989, 1995 }, result.Select(x => x.Year).ToArray());
		Assert.Equal(new[] { 10, 9, 1 }, result.Select(x => x.Count).ToArray());
	}

	[Fact]
	public async Task Get_movie_count_by_year_untyped()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var result = await client
			.WithExtensions()
			.For<Movie>()
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.Year,
				Count = a.Count()
			}))
			.OrderByDescending(x => x.Count)
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(3, result.Count());
		Assert.Equal(new[] { 1990, 1989, 1995 }, result.Select(x => x.Year).ToArray());
		Assert.Equal(new[] { 10, 9, 1 }, result.Select(x => x.Count).ToArray());
	}

	[Fact]
	public async Task Get_movie_count_by_year_dynamic()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var client = new ODataClient(settings);
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		IEnumerable<dynamic> result = await client
			.WithExtensions()
			.For(x.Movie)
			.Apply(b.GroupBy(new
			{
				x.Year,
				Count = a.Count()
			}))
			.OrderByDescending(x.Count)
			.FindEntriesAsync();

		Assert.Equal(3, result.Count());
		Assert.Equal(new[] { 1990, 1989, 1995 }, new[] { (int)result.ElementAt(0).Year, (int)result.ElementAt(1).Year, (int)result.ElementAt(2).Year });
		Assert.Equal(new[] { 10, 9, 1 }, new[] { (int)result.ElementAt(0).Count, (int)result.ElementAt(1).Count, (int)result.ElementAt(2).Count });
	}
}

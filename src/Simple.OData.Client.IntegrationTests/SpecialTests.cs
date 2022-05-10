using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.Edm;
using Xunit;

namespace Simple.OData.Client.Tests;

using Entry = System.Collections.Generic.Dictionary<string, object>;

public class SpecialTests : ODataTestBase
{
	public SpecialTests()
		: base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3)
	{
	}

	[Fact]
	public async Task InterceptRequest()
	{
		var settings = new ODataClientSettings(_serviceUri)
		{
			PayloadFormat = _payloadFormat,
			BeforeRequest = x => x.Method = new HttpMethod("PUT")
		};
		var client = new ODataClient(settings);
		await AssertThrowsAsync<WebRequestException>(async () => await client.FindEntriesAsync("Products").ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task InterceptRequestAsync()
	{
		var settings = new ODataClientSettings
		{
			BaseUri = _serviceUri,
			BeforeRequestAsync = x =>
			{
				x.Method = new HttpMethod("PUT");
				var tcs = new TaskCompletionSource<HttpRequestMessage>();
				var task = tcs.Task;
				tcs.SetResult(x);

				return task;
			}
		};
		var client = new ODataClient(settings);
		await AssertThrowsAsync<WebRequestException>(async () => await client.FindEntriesAsync("Products").ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task InterceptResponse()
	{
		var settings = new ODataClientSettings(_serviceUri)
		{
			PayloadFormat = _payloadFormat,
			AfterResponse = x => throw new InvalidOperationException()
		};
		var client = new ODataClient(settings);
		await AssertThrowsAsync<InvalidOperationException>(async () => await client.FindEntriesAsync("Products").ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task InterceptResponseAsync()
	{
		var settings = new ODataClientSettings
		{
			BaseUri = _serviceUri,
			AfterResponseAsync = x => { throw new InvalidOperationException(); },
		};
		var client = new ODataClient(settings);
		await AssertThrowsAsync<InvalidOperationException>(async () => await client.FindEntriesAsync("Products").ConfigureAwait(false)).ConfigureAwait(false);
	}

	[Fact]
	public async Task InsertUsingModifiedSchema()
	{
		await AssertThrowsAsync<Microsoft.Data.OData.ODataException>(async () =>
			await _client.InsertEntryAsync("Products", new Entry() { { "Price", null } }).ConfigureAwait(false)).ConfigureAwait(false);

		var metadataDocument = await _client.GetMetadataDocumentAsync().ConfigureAwait(false);
		metadataDocument = metadataDocument.Replace(@"Name=""Price"" Type=""Edm.Double"" Nullable=""false""", @"Name=""Price"" Type=""Edm.Double"" Nullable=""true""");
		ODataClient.ClearMetadataCache();
		var settings = new ODataClientSettings
		{
			BaseUri = _serviceUri,
			MetadataDocument = metadataDocument,
		};
		var client = new ODataClient(settings);
		var model = await client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false);
		var type = model.FindDeclaredType("ODataDemo.Product");
		var property = (type as IEdmEntityType).DeclaredProperties.Single(x => x.Name == "Price");
		Assert.True(property.Type.IsNullable);

		await AssertThrowsAsync<WebRequestException>(async () =>
			await client.InsertEntryAsync("Products",
				new Entry()
				{
						{ "Name", "Test"},
						{ "Description", "Test" },
						{ "Price", null }
				}).ConfigureAwait(false)).ConfigureAwait(false);

		ODataClient.ClearMetadataCache();
	}

	[Fact]
	public async Task FindEntryParallelThreads()
	{
		var products = (await _client.FindEntriesAsync("Products").ConfigureAwait(false)).ToArray();

		var summary = new ExecutionSummary();
		var tasks = new List<Task>();
		foreach (var product in products)
		{
			var task = RunClient(_client, Convert.ToInt32(product["ID"]), summary);
			tasks.Add(task);
		}

		Task.WaitAll(tasks.ToArray());

		Assert.Equal(products.Length, summary.ExecutionCount);
		Assert.Equal(0, summary.ExceptionCount);
		Assert.Equal(0, summary.NonEqualCount);
	}

	[Fact]
	public async Task FindEntryParallelThreadsRenewConnection()
	{
		var client = new ODataClient(new ODataClientSettings() { BaseUri = _serviceUri, RenewHttpConnection = true });
		var products = (await client.FindEntriesAsync("Products").ConfigureAwait(false)).ToArray();

		var summary = new ExecutionSummary();
		var tasks = new List<Task>();
		foreach (var product in products)
		{
			var task = RunClient(client, Convert.ToInt32(product["ID"]), summary);
			tasks.Add(task);
		}

		Task.WaitAll(tasks.ToArray());

		Assert.Equal(products.Length, summary.ExecutionCount);
		Assert.Equal(0, summary.ExceptionCount);
		Assert.Equal(0, summary.NonEqualCount);
	}

	[Fact]
	public async Task InsertUpdateDeleteSeparateBatchesRenewHttpConnection()
	{
		var settings = new ODataClientSettings(_serviceUri)
		{
			PayloadFormat = _payloadFormat,
		};
		var batch = new ODataBatch(settings);
		batch += c => c.InsertEntryAsync("Products",
			new Entry()
			{
					{"ID", 1001},
					{ "Name", "Test1" },
					{ "Price", 21m },
					{ "ReleaseDate", DateTime.Now },
					{ "Rating", 1 },
			}, false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		var client = new ODataClient(new ODataClientSettings { BaseUri = _serviceUri, RenewHttpConnection = true });
		var product = await client.FindEntryAsync("Products?$filter=Name eq 'Test1'").ConfigureAwait(false);
		Assert.Equal(21m, Convert.ToDecimal(product["Price"]));
		var key = new Entry() { { "ID", product["ID"] } };

		batch = new ODataBatch(settings);
		batch += c => c.UpdateEntryAsync("Products", key, new Entry() { { "Price", 22m } });
		await batch.ExecuteAsync().ConfigureAwait(false);

		product = await client.FindEntryAsync("Products?$filter=Name eq 'Test1'").ConfigureAwait(false);
		Assert.Equal(22m, Convert.ToDecimal(product["Price"]));

		batch = new ODataBatch(settings);
		batch += c => c.DeleteEntryAsync("Products", key);
		await batch.ExecuteAsync().ConfigureAwait(false);

		product = await client.FindEntryAsync("Products?$filter=Name eq 'Test1'").ConfigureAwait(false);
		Assert.Null(product);
	}
	[Fact]
	public async Task MetadataErrorIsNotCached()
	{
		const string uriString = "ftp://localhost/";
		var baseUri = new Uri(uriString);
		try
		{
			var settings = new ODataClientSettings { BaseUri = baseUri };
			var client = new ODataClient(settings);
			await client.GetMetadataAsync().ConfigureAwait(false);
		}
		catch (NotSupportedException)
		{
			//only HTTP and HTTPS supported
		}

		var wasCached = true;
		var cached = EdmMetadataCache.GetOrAdd(uriString, x =>
		{
			wasCached = false;
			return null;
		});
		Assert.False(wasCached);
		Assert.Null(cached);
	}

	[Fact]
	public async Task MetadataIsCached()
	{
		var settings = new ODataClientSettings { BaseUri = _serviceUri };
		var client = new ODataClient(settings);

		await client.GetMetadataAsync().ConfigureAwait(false);
		EdmMetadataCache.GetOrAdd(_serviceUri.ToString(), x => throw new Exception("metadata was not cached."));

		settings.BeforeRequest = x => throw new Exception("metadata cache was not used.");
		await client.GetMetadataAsync().ConfigureAwait(false);

		settings = new ODataClientSettings { BaseUri = _serviceUri, BeforeRequest = x => throw new Exception("not reusing settings will defeat metadata cache.") };
		client = new ODataClient(settings);
		await client.GetMetadataAsync().ConfigureAwait(false);
	}

	private class ExecutionSummary
	{
		public int ExecutionCount { get; set; }
		public int NonEqualCount { get; set; }
		public int ExceptionCount { get; set; }
	}

	private async static Task RunClient(IODataClient client, int productID, ExecutionSummary result)
	{
		try
		{
			var product = await client.FindEntryAsync($"Products({productID})").ConfigureAwait(false);
			if (productID != Convert.ToInt32(product["ID"]))
			{
				lock (result)
				{
					result.NonEqualCount++;
				}
			}
		}
		catch (Exception)
		{
			lock (result)
			{
				result.ExceptionCount++;
			}
		}
		finally
		{
			lock (result)
			{
				result.ExecutionCount++;
			}
		}
	}
}

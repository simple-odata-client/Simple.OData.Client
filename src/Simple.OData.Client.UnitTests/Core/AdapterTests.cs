using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Data.OData;
using Simple.OData.Client.V3.Adapter;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class AdapterTests : TestBase
{
	private class CustomBatchWriter : BatchWriter
	{
		private readonly ISession session;

		public CustomBatchWriter(ISession session, IDictionary<object, IDictionary<string, object>> batchEntries) : base(session, batchEntries)
		{
			this.session = session;
		}

		protected async override Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired)
		{
			var result = await base.CreateOperationMessageAsync(AppendToken(uri), method, collection, contentId, resultRequired).ConfigureAwait(false);
			if (result is IODataRequestMessage request)
			{
				session.Trace("{0} batch request id {1}: {2}", request.Method, contentId, request.Url.AbsoluteUri);
			}

			return result;
		}

		private static Uri AppendToken(Uri uri, string token = "123456")
		{
			var uriBuilder = new UriBuilder(uri);
			var queryParameters = HttpUtility.ParseQueryString(uriBuilder.Query);
			queryParameters["token"] = token;
			uriBuilder.Query = queryParameters.ToString();
			return uriBuilder.Uri;
		}
	}

	private class CustomAdapter : ODataAdapter
	{
		private readonly ISession session;

		public CustomAdapter(ISession session, IODataModelAdapter modelAdapter) : base(session, modelAdapter)
		{
			this.session = session;
		}

		public override IBatchWriter GetBatchWriter(IDictionary<object, IDictionary<string, object>> batchEntries)
		{
			return new CustomBatchWriter(session, batchEntries);
		}
	}

	private class CustomAdapterFactory : ODataAdapterFactory
	{
		public override Func<ISession, IODataAdapter> CreateAdapterLoader(string metadataString, ITypeCache typeCache)
		{
			var modelAdapter = CreateModelAdapter(metadataString, typeCache);
			return session => new CustomAdapter(session, modelAdapter);
		}
	}

	[Fact]
	public void CustomAdapterIsProduced()
	{
		var settings = CreateDefaultSettings();
		settings.AdapterFactory = new CustomAdapterFactory();
		var client = new ODataClient(settings);
		Assert.IsType<CustomAdapter>(client.Session.Adapter);
	}

	[Fact]
	public async void CustomBatchWriterAppendsToken()
	{
		var settings = CreateDefaultSettings().WithHttpMock();
		var trace = new List<string>();
		settings.OnTrace = (str, args) => trace.Add(string.Format(str, args));
		settings.AdapterFactory = new CustomAdapterFactory();

		var batch = new ODataBatch(settings);
		batch += c => c.FindEntriesAsync("Products");
		await batch.ExecuteAsync().ConfigureAwait(false);

		var batchTrace = new Regex("^(.*)batch request id(.*)token=123456$");
		var matches = trace.Where(x => batchTrace.IsMatch(x));
		Assert.Single(matches);
	}
}

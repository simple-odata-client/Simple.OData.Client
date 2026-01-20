using System.Net.Http;
using FluentAssertions;
using Simple.OData.Client;

namespace Simple.OData.Tests.Client;

public abstract class TestBase : IDisposable
{
	private static readonly HttpClient metadataHttpClient = new();

	protected const string ODataV2ReadWriteUri = "https://services.odata.org/V2/%28S%28readwrite%29%29/OData/OData.svc/";
	protected const string ODataV3ReadOnlyUri = "https://services.odata.org/V3/OData/OData.svc/";
	protected const string ODataV3ReadWriteUri = "https://services.odata.org/V3/%28S%28readwrite%29%29/OData/OData.svc/";
	protected const string ODataV4ReadOnlyUri = "https://services.odata.org/V4/OData/OData.svc/";
	protected const string ODataV4ReadWriteUri = "https://services.odata.org/V4/OData/%28S%28readwrite%29%29/OData.svc/";

	protected const string NorthwindV2ReadOnlyUri = "https://services.odata.org/V2/Northwind/Northwind.svc/";
	protected const string NorthwindV3ReadOnlyUri = "https://services.odata.org/V3/Northwind/Northwind.svc/";
	protected const string NorthwindV4ReadOnlyUri = "https://services.odata.org/V4/Northwind/Northwind.svc/";

	protected const string TripPinV4ReadWriteUri = "https://services.odata.org/V4/TripPinServiceRW/";
	protected const string TripPinV4RESTierUri = "https://services.odata.org/TripPinRESTierService/";

	protected readonly Uri _serviceUri;
	protected readonly ODataPayloadFormat _payloadFormat;
	protected readonly IODataClient _client;

	protected TestBase(string serviceUri, ODataPayloadFormat payloadFormat)
	{
		// services.odata.org only works with TLS 1.2
		System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

		if (serviceUri.Contains("%28readwrite%29") || serviceUri == TripPinV4ReadWriteUri)
		{
			serviceUri = GetReadWriteUri(serviceUri).GetAwaiter().GetResult();
		}

		_serviceUri = new Uri(serviceUri);
		_payloadFormat = payloadFormat;
		_client = CreateClientWithDefaultSettings();
	}

	private async static Task<string> GetReadWriteUri(string serviceUri)
	{
		var response = await metadataHttpClient.GetAsync(serviceUri);
		var uri = response.RequestMessage.RequestUri.AbsoluteUri;
		if (serviceUri == ODataV2ReadWriteUri)
		{
			var i1 = uri.IndexOf(".org/V");
			var i2 = uri.IndexOf("/OData/") == -1 ? uri.IndexOf("/odata/") : uri.IndexOf("/OData/");
#if NET7_0_OR_GREATER
			uri = string.Concat(uri.AsSpan()[..(i1 + 5)], uri.AsSpan(i1 + 8, i2 - i1 - 7), uri.AsSpan(i1 + 5, 2), uri.AsSpan()[i2..]);
#else
            uri = uri.Substring(0, i1 + 5) + uri.Substring(i1 + 8, i2 - i1 - 7) + uri.Substring(i1 + 5, 2) + uri.Substring(i2);
#endif
		}

		return uri;
	}

	protected ODataClientSettings CreateDefaultSettings(Action<ODataClientSettings> configure = null)
	{
		var settings = new ODataClientSettings(_serviceUri)
		{
			PayloadFormat = _payloadFormat,
			IgnoreResourceNotFoundException = true,
			OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
		};

		configure?.Invoke(settings);

		return settings;
	}

	protected ODataClientSettings CreateDefaultSettingsWithNameResolver(INameMatchResolver nameMatchResolver)
	{
		return new ODataClientSettings(_serviceUri)
		{
			PayloadFormat = _payloadFormat,
			IgnoreResourceNotFoundException = true,
			OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
			NameMatchResolver = nameMatchResolver,
		};
	}

	protected IODataClient CreateClientWithDefaultSettings()
	{
		return new ODataClient(CreateDefaultSettings());
	}

	protected IODataClient CreateClientWithNameResolver(INameMatchResolver nameMatchResolver)
	{
		return new ODataClient(CreateDefaultSettingsWithNameResolver(nameMatchResolver));
	}

	public void Dispose()
	{
		if (_client is not null)
		{
			DeleteTestData().Wait();
		}

		GC.SuppressFinalize(this);
	}

	protected abstract Task DeleteTestData();

	public async static Task AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
	{
		try
		{
			await testCode();
			throw new Exception($"Expected exception: {typeof(T)}");
		}
		catch (T)
		{
		}
		catch (AggregateException exception)
		{
			exception.InnerExceptions.Single().Should().BeOfType<T>();
		}
	}
}

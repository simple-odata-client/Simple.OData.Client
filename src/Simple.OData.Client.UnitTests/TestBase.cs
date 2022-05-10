using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
#if NET461 && !MOCK_HTTP
using Simple.OData.NorthwindModel;
#endif

namespace Simple.OData.Client.Tests;

public class TestBase : IDisposable
{
	protected Uri _serviceUri;
#if NET461 && !MOCK_HTTP
        protected TestService _service;
#endif
	protected IODataClient _client;
	protected readonly bool _readOnlyTests;

	protected TestBase(bool readOnlyTests = false)
	{
		_readOnlyTests = readOnlyTests;
#if NET461 && !MOCK_HTTP
            _service = new TestService(typeof(NorthwindService));
            _serviceUri = _service.ServiceUri;
#else
		_serviceUri = new Uri("http://localhost/");
#endif
		_client = new ODataClient(CreateDefaultSettings());
	}

	/* Original Northwind database
	 * protected const int ExpectedCountOfProducts = 77;
	 * protected const int ExpectedCountOfBeveragesProducts = 12;
	 * protected const int ExpectedCountOfCondimentsProducts = 12;
	 * protected const int ExpectedCountOfOrdersHavingAnyDetail = 160;
	 * protected const int ExpectedCountOfOrdersHavingAllDetails = 11;
	 * protected const int ExpectedCountOfProductsWithOrdersHavingAnyDetail = 160;
	 * protected const int ExpectedCountOfProductsWithOrdersHavingAllDetails = 11;
	*/

	protected const int ExpectedCountOfProducts = 22;
	protected const int ExpectedCountOfBeveragesProducts = 2;
	protected const int ExpectedCountOfCondimentsProducts = 7;
	protected const int ExpectedCountOfOrdersHavingAnyDetail = 5;
	protected const int ExpectedCountOfOrdersHavingAllDetails = 6;
	protected const int ExpectedCountOfProductsWithOrdersHavingAnyDetail = 5;
	protected const int ExpectedCountOfProductsWithOrdersHavingAllDetails = 6;

	protected ODataClientSettings CreateDefaultSettings()
	{
		return new ODataClientSettings
		{
			BaseUri = _serviceUri,
			MetadataDocument = GetMetadataDocument(),
			OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
		};
	}

	public void Dispose()
	{
#if NET461 && !MOCK_HTTP
            if (_client != null && !_readOnlyTests)
            {
                DeleteTestData().Wait();
            }

            if (_service != null)
            {
                _service.Dispose();
                _service = null;
            }
#endif
		GC.SuppressFinalize(this);
	}

	private static string GetMetadataDocument()
	{
#if MOCK_HTTP
		return MetadataResolver.GetMetadataDocument("Northwind.xml");
#else
            return null;
#endif
	}

	public async static Task AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
	{
		try
		{
			await testCode().ConfigureAwait(false);
			throw new Exception($"Expected exception: {typeof(T)}");
		}
		catch (T)
		{
		}
		catch (AggregateException exception)
		{
			var innerException = exception.InnerExceptions.Single();
			Assert.IsType<T>(innerException);
		}
	}
}

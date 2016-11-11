using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected const string ODataV4ReadOnlyUri = "http://services.odata.org/V4/OData/OData.svc/";
        protected const string ODataV4ReadWriteUri = "http://services.odata.org/V4/OData/%28S%28readwrite%29%29/OData.svc/";

        protected const string NorthwindV4ReadOnlyUri = "http://services.odata.org/V4/Northwind/Northwind.svc/";

        protected const string TripPinV4ReadWriteUri = "http://services.odata.org/V4/TripPinServiceRW/";

        protected readonly Uri _serviceUri;
        protected readonly ODataPayloadFormat _payloadFormat;
        protected readonly IODataClient _client;

        protected TestBase(string serviceUri, ODataPayloadFormat payloadFormat)
        {
            if (serviceUri.Contains("%28readwrite%29") || serviceUri == TripPinV4ReadWriteUri)
            {
                serviceUri = GetReadWriteUri(serviceUri).Result;
            }

            _serviceUri = new Uri(serviceUri);
            _payloadFormat = payloadFormat;
            _client = CreateClientWithDefaultSettings();
        }

        private async Task<string> GetReadWriteUri(string serviceUri)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(serviceUri);
            var uri = response.RequestMessage.RequestUri.AbsoluteUri;
            return uri;
        }

        protected IODataClient CreateClientWithDefaultSettings()
        {
            return new ODataClient(new ODataClientSettings(_serviceUri)
            {
                PayloadFormat = _payloadFormat,
                IgnoreResourceNotFoundException = true,
                OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
            });
        }

        public void Dispose()
        {
            if (_client != null)
            {
                DeleteTestData().Wait();
            }
        }

        protected abstract Task DeleteTestData();

        public async static Task AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
        {
            try
            {
                await testCode();
                throw new Exception(string.Format("Expected exception: {0}", typeof (T)));
            }
            catch (T)
            {
            }
            catch (AggregateException exception)
            {
                Assert.IsType<T>(exception.InnerExceptions.Single());
            }
        }
    }
}

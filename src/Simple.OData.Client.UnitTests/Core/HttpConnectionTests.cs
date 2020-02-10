using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class HttpConnectionTests
    {
        [Fact]
        public void Should_Use_Settings_Same_HttpClient()
        {
            var httpClient = new HttpClient();

            var settings1 = new ODataClientSettings(httpClient)
            {
                BaseUri = new Uri("http://localhost")
            };

            var settings2 = new ODataClientSettings(httpClient)
            {
                BaseUri = new Uri("http://localhost")
            };

            var httpConnection1 = new HttpConnection(settings1);
            var httpConnection2 = new HttpConnection(settings2);

            Assert.Equal(httpConnection1.HttpClient, httpClient);
            Assert.Equal(httpConnection1.HttpClient, httpConnection2.HttpClient);
        }

        [Fact]
        public void Should_Use_Settings_Different_HttpClient()
        {
            var httpClient1 = new HttpClient();
            var httpClient2 = new HttpClient();

            var settings1 = new ODataClientSettings(httpClient1)
            {
                BaseUri = new Uri("http://localhost")
            };

            var settings2 = new ODataClientSettings(httpClient2)
            {
                BaseUri = new Uri("http://localhost")
            };

            var httpConnection1 = new HttpConnection(settings1);
            var httpConnection2 = new HttpConnection(settings2);

            Assert.Equal(httpConnection1.HttpClient, httpClient1);
            Assert.Equal(httpConnection2.HttpClient, httpClient2);
        }

        [Fact]
        public void Should_Use_Same_Cached_HttpClient()
        {
            var settings1 = new ODataClientSettings()
            {
                BaseUri = new Uri("http://localhost")
            };

            var settings2 = new ODataClientSettings()
            {
                BaseUri = new Uri("http://localhost")
            };

            var httpConnection1 = new HttpConnection(settings1);
            var httpConnection2 = new HttpConnection(settings2);

            Assert.Equal(httpConnection1.HttpClient, httpConnection2.HttpClient);
        }

        [Fact]
        public void Should_Use_Different_Cached_HttpClient()
        {
            var settings1 = new ODataClientSettings()
            {
                BaseUri = new Uri("http://localhost1")
            };

            var settings2 = new ODataClientSettings()
            {
                BaseUri = new Uri("http://localhost2")
            };

            var httpConnection1 = new HttpConnection(settings1);
            var httpConnection2 = new HttpConnection(settings2);

            Assert.NotEqual(httpConnection1.HttpClient, httpConnection2.HttpClient);
        }
    }
}

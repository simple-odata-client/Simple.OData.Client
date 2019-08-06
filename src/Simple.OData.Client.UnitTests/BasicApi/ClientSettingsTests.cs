using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Simple.OData.Client;
using Xunit;

namespace Simple.OData.Client.Tests.BasicApi
{
    public class ClientSettingsTests : TestBase
    {
        [Fact]
        public async Task UpdateRequestHeadersForXCsrfTokenRequests()
        {
            // Make sure the default doesn't contain any headers
            var concreteClient = _client as ODataClient;
            Assert.Null(concreteClient.Session.Settings.BeforeRequest);

            // Add some headers - note this will simply set up the request action
            // to lazily add them to the request.
            concreteClient.UpdateRequestHeaders(new Dictionary<string, IEnumerable<string>>
            {
                {"x-csrf-token", new List<string> {"fetch"}}
            });
            Assert.NotNull(concreteClient.Session.Settings.BeforeRequest);

            // Make sure we can still execute a request
            await concreteClient.GetMetadataDocumentAsync();
        }

        [Fact]
        public async Task TestOnGetHttpClient()
        {
            var client = new DisposableHttpClient();
            var firstClient = client;
            Func<HttpClient> getFactory = () =>
            {
                if (client.IsDisposed)
                    client = new DisposableHttpClient();

                return client;
            };

            var settings = new ODataClientSettings { OnGetHttpClient = getFactory };
            var settings2 = new ODataClientSettings { OnGetHttpClient = getFactory };

            Assert.True(ReferenceEquals(settings.HttpClient, settings2.HttpClient));

            var session = Session.FromSettings(settings);
            var connection = session.GetHttpConnection();

            Assert.True(ReferenceEquals(settings.HttpClient, connection.HttpClient));

            client.Dispose();

            var session2 = Session.FromSettings(settings);
            var connection2 = session2.GetHttpConnection();

            Assert.True(!ReferenceEquals(settings.HttpClient, firstClient));
            Assert.True(ReferenceEquals(connection2.HttpClient, client));
            Assert.True(!ReferenceEquals(connection2.HttpClient, firstClient));
        }

        class DisposableHttpClient : HttpClient
        {
            public DisposableHttpClient()
            {
                BaseAddress = new Uri("http://localhost");
            }

            public bool IsDisposed { get; private set; }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                IsDisposed = true;
            }
        }
    }
}

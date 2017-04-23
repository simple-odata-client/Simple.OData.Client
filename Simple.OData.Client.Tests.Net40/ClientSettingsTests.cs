using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
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
            Assert.DoesNotThrow(async () => await concreteClient.GetMetadataDocumentAsync());
        }
    }
}

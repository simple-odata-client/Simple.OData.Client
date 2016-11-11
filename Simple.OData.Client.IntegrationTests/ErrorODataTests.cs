using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class ErrorODataTestsV4Json : ErrorODataTests
    {
        public ErrorODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class ErrorODataTests : ODataTestBase
    {
        protected ErrorODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task ErrorContent()
        {
            try
            {
                await _client
                    .For("Products")
                    .Filter("NonExistingProperty eq 1")
                    .FindEntryAsync();

                Assert.False(true, "Expected exception");
            }
            catch (WebRequestException ex)
            {
                Assert.NotNull(ex.Response);
            }
            catch (Exception)
            {
                Assert.False(true, "Expected WebRequestException");
            }
        }
    }
}

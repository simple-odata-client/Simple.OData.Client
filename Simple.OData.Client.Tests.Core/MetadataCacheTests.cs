using System;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class MetadataCacheTests
    {
        [Fact]
        public async Task MetadataErrorIsNotCached()
        {
            var baseUri = new Uri("ftp://localhost/");
            var settings = new ODataClientSettings { BaseUri = baseUri };

            var client = new ODataClient(settings);
            try
            {
                await client.GetMetadataAsync();
            }
            catch (ArgumentException)
            {
                //only HTTP and HTTPS supported
            }
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                if (ex.InnerExceptions.Count != 1)
                    throw;
                var arg = ex.InnerException as ArgumentException;
                if (arg == null) throw;
                //only HTTP and HTTPS supported
            }

            bool wasCached = true;
            var cached = MetadataCache.GetOrAdd("ftp://localhost/", x =>
            {
                wasCached = false;
                return null;
            });
            Assert.False(wasCached);
            Assert.Null(cached);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class FindNuGetTests
    {
        private int metadataCalls;

        [Fact]
        public async Task FindEntryNuGetV1()
        {
            var client = new ODataClient("http://nuget.org/api/v1");
            var package = await client.FindEntryAsync("Packages?$filter=Title eq 'EntityFramework'");
            Assert.NotNull(package["Id"]);
            Assert.NotNull(package["Authors"]);
        }

        [Fact]
        public async Task FindEntryNuGetV2()
        {
            var settings = new ODataClientSettings("http://nuget.org/api/v2")
            {
                OnTrace = (format, args) =>
                {
                    if (args.Length > 1)
                    {
                        var request = args[1] as string;
                        if (request != null && request.EndsWith("$metadata"))
                        {
                            Interlocked.Increment(ref metadataCalls);
                        }
                    }
                    var msg = string.Format(format, args);
                    Console.WriteLine($"[{DateTimeOffset.Now:O}][OData] " + msg);
                }
            };
            var client = new ODataClient(settings);
            var package = await client.FindEntryAsync("Packages?$filter=Title eq 'EntityFramework'");
            Assert.NotNull(package["Id"]);
        }

        [Fact(Skip = "Investigate reason for such test and its logic")]
        public async Task FindEntryNuGetV2MultiThread()
        {
            MetadataCache.Clear();

            var tasks = new List<Task>();
            metadataCalls = 0;

            for (var i = 0; i < 10; i++)
            {
                tasks.Add(FindEntryNuGetV2());
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, metadataCalls);
        }

        [Fact(Skip = "Investigate reason for such test and its logic")]
        public async Task FindEntryNuGetV2MultiThreadWithDelays()
        {
            int taskCount = 100;
            Random random = new Random();
            MetadataCache.Clear();

            var tasks = new List<Task>();
            metadataCalls = 0;
            
            for (var i = 0; i < taskCount; i++)
            {
                if (random.NextDouble() < 0.25)
                {
                    await Task.Delay(random.Next(1, 250));
                }

                tasks.Add(FindEntryNuGetV2());
            }

            await Task.WhenAll(tasks);

            Assert.Equal(1, metadataCalls);
        }

        [Fact]
        public async Task FindEntryNuGetV2_FieldWithAnnotation()
        {
            var client = new ODataClient("http://nuget.org/api/v2");
            var package = await client.FindEntryAsync("Packages?$filter=Title eq 'EntityFramework'");
            Assert.NotNull(package["Authors"]);
        }
    }
}

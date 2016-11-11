using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class MediaODataTestsV4Json : MediaODataTests
    {
        public MediaODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class MediaODataTests : ODataTestBase
    {
        class PersonDetail
        {
            public string Photo { get; set; }
        }

        protected MediaODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task GetMediaStream()
        {
            var ad = await _client
                .For("Advertisements")
                .FindEntryAsync();
            var id = ad["ID"];
            var stream = await _client
                .For("Advertisements")
                .Key(id)
                .Media()
                .GetStreamAsync();
            var text = Utils.StreamToString(stream);
            Assert.True(text.Contains("stream data"));
        }

        [Fact]
        public async Task GetNamedMediaStream()
        {
            var stream = await _client
                .For("Persons")
                .Key(1)
                .NavigateTo("PersonDetail")
                .Media("Photo")
                .GetStreamAsync();
            var text = Utils.StreamToString(stream);
            Assert.True(text.Contains("named stream data"));
        }

        [Fact]
        public async Task GetTypedNamedMediaStream()
        {
            var text = await _client
                .For("Persons")
                .Key(1)
                .NavigateTo<PersonDetail>()
                .Media(x => x.Photo)
                .GetStreamAsStringAsync();
            Assert.True(text.Contains("named stream data"));
        }

        [Fact]
        public async Task FindEntryWithEntityMedia()
        {
            var ad = await _client
                .For("Advertisements")
                .FindEntryAsync();
            var id = ad["ID"];

            ad = await _client
                .For("Advertisements")
                .WithMedia("Media")
                .Key(id)
                .FindEntryAsync();
            Assert.NotNull(ad["Media"]);
            var text = Utils.StreamToString(ad["Media"] as Stream);
            Assert.True(text.Contains("stream data"));
        }

        [Fact]
        public async Task FindEntryWithNamedMedia()
        {
            var person = await _client
                .For("Persons")
                .Key(1)
                .NavigateTo("PersonDetail")
                .WithMedia("Photo")
                .FindEntryAsync();
            Assert.NotNull(person["Photo"]);
            var text = Utils.StreamToString(person["Photo"] as Stream);
            Assert.True(text.Contains("named stream data"));
        }

        [Fact]
        public async Task SetMediaStream()
        {
            var ad = await _client
                .For("Advertisements")
                .FindEntryAsync();
            var id = ad["ID"];
            var stream = Utils.StringToStream("Updated stream data");
            await _client
                .For("Advertisements")
                .Key(id)
                .Media()
                .SetStreamAsync(stream, "text/plain", false);
            stream = await _client
                .For("Advertisements")
                .Key(id)
                .Media()
                .GetStreamAsync();
            var text = Utils.StreamToString(stream);
            Assert.Equal("Updated stream data", text);
        }

        [Fact]
        public async Task SetNamedMediaStream()
        {
            var stream = Utils.StringToStream("Updated named stream data");
            await _client
                .For("Persons")
                .Key(1)
                .NavigateTo("PersonDetail")
                .Media("Photo")
                .SetStreamAsync(stream, "text/plain", false);
            stream = await _client
                .For("Persons")
                .Key(1)
                .NavigateTo("PersonDetail")
                .Media("Photo")
                .GetStreamAsync();
            var text = Utils.StreamToString(stream);
            Assert.Equal("Updated named stream data", text);
        }
    }
}
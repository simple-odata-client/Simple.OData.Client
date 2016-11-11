using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Moq;
using Xunit;
using Simple.OData.Client.V4.Adapter;

namespace Simple.OData.Client.Tests
{
    public class ResponseReaderV4Tests : TestBase
    {
        public override string MetadataFile { get { return "DynamicsCRM.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }

        [Fact]
        public async Task GetExpandedLinkAnnotationOnlyLink()
        {
            var response = SetUpResourceMock("AccountTasksOnlyLink.json");
            var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>());
            var result = (await responseReader.GetResponseAsync(response)).Feed;
            Assert.NotNull(result.Entries.First().LinkAnnotations);
        }

        [Fact]
        public async Task GetExpandedLinkAnnotationDataAndLink()
        {
            var response = SetUpResourceMock("AccountTasksAndLink.json");
            var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>());
            var result = (await responseReader.GetResponseAsync(response)).Feed;
            Assert.NotNull(result.Entries.First().LinkAnnotations);
        }

        private IODataResponseMessageAsync SetUpResourceMock(string resourceName)
        {
            var document = GetResourceAsString(resourceName);
            var mock = new Mock<IODataResponseMessageAsync>();
            mock.Setup(x => x.GetStreamAsync()).ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(document)));
            mock.Setup(x => x.GetStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(document)));
            mock.Setup(x => x.GetHeader("Content-Type")).Returns(() => "application/json; type=feed; charset=utf-8");
            return mock.Object;
        }
    }
}

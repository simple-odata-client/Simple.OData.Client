using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Moq;
using Simple.OData.Client.V4.Adapter;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class ResponseReaderV4Tests : CoreTestBase
{
	public override string MetadataFile => "DynamicsCRM.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	[Fact]
	public async Task GetExpandedLinkAnnotationOnlyLink()
	{
		var response = SetUpResourceMock("AccountTasksOnlyLink.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed;
		Assert.NotNull(result.Entries.First().LinkAnnotations);
	}

	[Fact]
	public async Task GetExpandedLinkAnnotationDataAndLink()
	{
		var response = SetUpResourceMock("AccountTasksAndLink.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed;
		Assert.NotNull(result.Entries.First().LinkAnnotations);
	}

	[Fact]
	public async Task ExampleActionReturnsComplexType()
	{
		var response = SetUpResourceMock("ExampleActionComplexType.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed;
		var entry = result.Entries.First();
		Assert.NotNull(entry);
		Assert.Equal("MyPropertyValue", entry.Data["SomeProperty"]);
	}

	private static new IODataResponseMessageAsync SetUpResourceMock(string resourceName)
	{
		var document = GetResourceAsString(resourceName);
		var mock = new Mock<IODataResponseMessageAsync>();
		mock.Setup(x => x.GetStreamAsync()).ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(document)));
		mock.Setup(x => x.GetStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(document)));
		mock.Setup(x => x.GetHeader("Content-Type")).Returns(() => "application/json; type=feed; charset=utf-8");
		return mock.Object;
	}
}

using System.Text;
using FluentAssertions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Moq;
using Simple.OData.Client;
using Simple.OData.Client.V4.Adapter;
using Xunit;

namespace Simple.OData.Tests.Client.Core;

public class ResponseReaderV4Tests : CoreTestBase
{
	public override string MetadataFile => "DynamicsCRM.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	[Fact]
	public async Task GetExpandedLinkAnnotationOnlyLink()
	{
		var response = SetUpResourceMock("AccountTasksOnlyLink.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>());
		var result = (await responseReader.GetResponseAsync(response)).Feed;
		result.Entries.First().LinkAnnotations.Should().NotBeNull();
	}

	[Fact]
	public async Task GetExpandedLinkAnnotationDataAndLink()
	{
		var response = SetUpResourceMock("AccountTasksAndLink.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>());
		var result = (await responseReader.GetResponseAsync(response)).Feed;
		result.Entries.First().LinkAnnotations.Should().NotBeNull();
	}

	[Fact]
	public async Task ExampleActionReturnsComplexType()
	{
		var response = SetUpResourceMock("ExampleActionComplexType.json");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>());
		var result = (await responseReader.GetResponseAsync(response)).Feed;
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

	[Fact]
	public async Task GetRelativeNextPageLink()
	{
		var settings = new ODataClientSettings {
			BaseUri = new Uri("http://localhost"),
			MetadataDocument = GetResourceAsString("Northwind4.xml"),
			PayloadFormat = ODataPayloadFormat.Json
		};
		settings = settings.WithHttpMock();
		var client = new ODataClient(settings);

		var annotations = new ODataFeedAnnotations();

		var response = await client
			.For("Products")
			.FindEntriesAsync(annotations);

		Assert.Equal(10, response.Count());
		Assert.NotNull(annotations.NextPageLink);

		var response2 = await client
			.For("Products")
			.FindEntriesAsync(annotations);

		Assert.Equal(2, response2.Count());
		Assert.Null(annotations.NextPageLink);
	}
}

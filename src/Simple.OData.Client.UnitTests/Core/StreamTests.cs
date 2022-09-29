using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class StreamTests : CoreTestBase
{
	public override string MetadataFile => "TripPin.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	private class Photo
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public byte[] Media { get; set; }
	}

	[Fact]
	public async Task GetMediaStream()
	{
		var command = _client
			.For<Photo>()
			.Key(1)
			.QueryOptions(new Dictionary<string, object>() { { "IntOption", 42 }, { "StringOption", "xyz" } })
			.Media();
		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Photos(1)/$value?IntOption=42&StringOption='xyz'", commandText);
	}
}

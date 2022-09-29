using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests;

// Not supported by OData V2
//public class MediaODataTestsV2Atom : MediaODataTests
//{
//    public MediaODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
//}

//public class MediaODataTestsV2Json : MediaODataTests
//{
//    public MediaODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
//}

public class MediaODataTestsV3Atom : MediaODataTests
{
	public MediaODataTestsV3Atom() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Atom, 3) { }
}

public class MediaODataTestsV3Json : MediaODataTests
{
	public MediaODataTestsV3Json() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3) { }
}

public class MediaODataTestsV4Json : MediaODataTests
{
	public MediaODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
}

public abstract class MediaODataTests : ODataTestBase
{
	private class PersonDetail
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
			.FindEntryAsync().ConfigureAwait(false);
		var id = ad["ID"];
		var stream = await _client
			.For("Advertisements")
			.Key(id)
			.Media()
			.GetStreamAsync().ConfigureAwait(false);
		var text = Utils.StreamToString(stream);
		Assert.Contains("stream data", text);
	}

	[Fact]
	public async Task GetNamedMediaStream()
	{
		var stream = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo("PersonDetail")
			.Media("Photo")
			.GetStreamAsync().ConfigureAwait(false);
		var text = Utils.StreamToString(stream);
		Assert.Contains("named stream data", text);
	}

	[Fact]
	public async Task GetTypedNamedMediaStream()
	{
		var text = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo<PersonDetail>()
			.Media(x => x.Photo)
			.GetStreamAsStringAsync().ConfigureAwait(false);
		Assert.Contains("named stream data", text);
	}

	[Fact]
	public async Task FindEntryWithEntityMedia()
	{
		var ad = await _client
			.For("Advertisements")
			.FindEntryAsync().ConfigureAwait(false);
		var id = ad["ID"];

		ad = await _client
			.For("Advertisements")
			.WithMedia("Media")
			.Key(id)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(ad["Media"]);
		var text = Utils.StreamToString(ad["Media"] as Stream);
		Assert.Contains("stream data", text);
	}

	[Fact]
	public async Task FindEntryWithNamedMedia()
	{
		var person = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo("PersonDetail")
			.WithMedia("Photo")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(person["Photo"]);
		var text = Utils.StreamToString(person["Photo"] as Stream);
		Assert.Contains("named stream data", text);
	}

	[Fact]
	public async Task SetMediaStream()
	{
		var ad = await _client
			.For("Advertisements")
			.FindEntryAsync().ConfigureAwait(false);
		var id = ad["ID"];
		var stream = Utils.StringToStream("Updated stream data");
		await _client
			.For("Advertisements")
			.Key(id)
			.Media()
			.SetStreamAsync(stream, "text/plain", false).ConfigureAwait(false);
		stream = await _client
			.For("Advertisements")
			.Key(id)
			.Media()
			.GetStreamAsync().ConfigureAwait(false);
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
			.SetStreamAsync(stream, "text/plain", false).ConfigureAwait(false);
		stream = await _client
			.For("Persons")
			.Key(1)
			.NavigateTo("PersonDetail")
			.Media("Photo")
			.GetStreamAsync().ConfigureAwait(false);
		var text = Utils.StreamToString(stream);
		Assert.Equal("Updated named stream data", text);
	}
}

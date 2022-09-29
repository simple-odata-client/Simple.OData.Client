using System;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests;

public class ErrorODataTestsV2Atom : ErrorODataTests
{
	public ErrorODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
}

public class ErrorODataTestsV2Json : ErrorODataTests
{
	public ErrorODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
}

public class ErrorODataTestsV3Atom : ErrorODataTests
{
	public ErrorODataTestsV3Atom() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Atom, 3) { }
}

public class ErrorODataTestsV3Json : ErrorODataTests
{
	public ErrorODataTestsV3Json() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3) { }
}

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
				.FindEntryAsync().ConfigureAwait(false);

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

	[Fact]
	public async Task ErrorMessage_ReasonPhrase()
	{
		try
		{
			var client = new ODataClient(CreateDefaultSettings(x =>
				x.WebRequestExceptionMessageSource = WebRequestExceptionMessageSource.ReasonPhrase));

			await client
				.For("Products")
				.Filter("NonExistingProperty eq 1")
				.FindEntryAsync().ConfigureAwait(false);

			Assert.False(true, "Expected exception");
		}
		catch (WebRequestException ex)
		{
			Assert.NotNull(ex.Message);
			Assert.Equal(ex.ReasonPhrase, ex.Message);
		}
	}

	[Fact]
	public async Task ErrorMessage_ResponseContent()
	{
		try
		{
			var client = new ODataClient(CreateDefaultSettings(x =>
				x.WebRequestExceptionMessageSource = WebRequestExceptionMessageSource.ResponseContent));

			await client
				.For("Products")
				.Filter("NonExistingProperty eq 1")
				.FindEntryAsync().ConfigureAwait(false);

			Assert.False(true, "Expected exception");
		}
		catch (WebRequestException ex)
		{
			Assert.NotNull(ex.Message);
			Assert.Equal(ex.Response, ex.Message);
		}
	}

	[Fact]
	public async Task ErrorMessage_PhraseAndContent()
	{
		try
		{
			var client = new ODataClient(CreateDefaultSettings(x =>
				x.WebRequestExceptionMessageSource = WebRequestExceptionMessageSource.Both));

			await client
				.For("Products")
				.Filter("NonExistingProperty eq 1")
				.FindEntryAsync().ConfigureAwait(false);

			Assert.False(true, "Expected exception");
		}
		catch (WebRequestException ex)
		{
			Assert.NotNull(ex.Message);
			Assert.True(ex.Message.Contains(ex.ReasonPhrase) && ex.Message.Contains(ex.Response));
		}
	}
}

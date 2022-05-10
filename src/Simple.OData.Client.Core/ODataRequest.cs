using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Simple.OData.Client;

public class ODataRequest
{
	private readonly string _uri;
	private HttpRequestMessage _requestMessage;
	private readonly ODataPayloadFormat _payloadFormat;
	private readonly Stream? _contentStream;
	private readonly string? _contentType;

	public HttpRequestMessage RequestMessage
	{
		get => GetOrCreateRequestMessage();
		private set => _requestMessage = value;
	}

	public string[] Accept
	{
		get
		{
			var isMetadataRequest = RequestMessage.RequestUri.LocalPath.EndsWith(ODataLiteral.Metadata, StringComparison.Ordinal);
			if (!isMetadataRequest && (ReturnsScalarResult || !ResultRequired))
			{
				return null;
			}

			if (isMetadataRequest)
			{
				return new[] { "application/xml" };
			}
			else
			{
				return _payloadFormat switch
				{
					ODataPayloadFormat.Json => new[] { "application/json", "application/xml", "application/text" },
					_ => new[] { "application/atom+xml", "application/xml", "application/text" },
				};
			}
		}
	}

	public string CommandText { get; private set; }
	public string Method { get; private set; }
	public IDictionary<string, object>? EntryData { get; private set; }
	public bool IsLink { get; set; }
	public ODataPayloadFormat UsePayloadFormat { get; set; }
	public bool ReturnsScalarResult { get; set; }
	public bool ResultRequired { get; set; }
	public bool CheckOptimisticConcurrency { get; set; }
	public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

	internal ODataRequest(
		string method,
		ISession session,
		string commandText,
		IDictionary<string, string>? headers = null)
	{
		CommandText = commandText;
		Method = method;

		var uri = new Uri(commandText, UriKind.RelativeOrAbsolute);
		_uri = uri.IsAbsoluteUri
			? uri.AbsoluteUri
			: Utils.CreateAbsoluteUri(session.Settings.BaseUri.AbsoluteUri, commandText).AbsoluteUri;
		_payloadFormat = session.Settings.PayloadFormat;

		if (headers != null)
		{
			Headers = headers;
		}
	}

	internal ODataRequest(string method, ISession session, string commandText, HttpRequestMessage requestMessage)
		: this(method, session, commandText)
	{
		RequestMessage = requestMessage;
	}

	internal ODataRequest(
		string method,
		ISession session,
		string commandText,
		IDictionary<string, object>? entryData,
		Stream contentStream,
		string? mediaType = null,
		IDictionary<string, string>? headers = null)
		: this(method, session, commandText, headers)
	{
		EntryData = entryData;
		_contentStream = contentStream;
		_contentType = mediaType;
	}

	private HttpContent? GetContent()
	{
		if (_contentStream == null)
		{
			return null;
		}

		if (_contentStream.CanSeek)
		{
			_contentStream.Seek(0, SeekOrigin.Begin);
		}

		var content = new StreamContent(_contentStream);
		content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType());
		content.Headers.ContentLength = _contentStream.Length;
		return content;
	}

	private string GetContentType()
	{
		if (!string.IsNullOrEmpty(_contentType))
		{
			return _contentType;
		}
		else
		{
			var payloadFormat = UsePayloadFormat != ODataPayloadFormat.Unspecified
				? UsePayloadFormat
				: _payloadFormat;

			return payloadFormat switch
			{
				ODataPayloadFormat.Json => "application/json",
				_ => IsLink ? "application/xml" : "application/atom+xml",
			};
		}
	}

	private HttpRequestMessage GetOrCreateRequestMessage()
	{
		if (_requestMessage != null)
		{
			return _requestMessage;
		}

		_requestMessage = new HttpRequestMessage(new HttpMethod(Method), _uri)
		{
			Content = _contentStream != null ? GetContent() : null
		};

		if (Headers != null)
		{
			foreach (var header in Headers)
			{
				_requestMessage.Headers.Add(header.Key, header.Value);
			}
		}

		return _requestMessage;
	}
}

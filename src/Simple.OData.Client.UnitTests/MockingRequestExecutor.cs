using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests;

[DataContract]
public class SerializableHttpRequestMessage
{
	[DataMember]
	public string Method { get; set; }
	[DataMember]
	public Uri RequestUri { get; set; }
	[DataMember]
	public Dictionary<string, List<string>> RequestHeaders;
	[DataMember]
	public Dictionary<string, List<string>> ContentHeaders;
	[DataMember]
	public string Content { get; set; }

	public SerializableHttpRequestMessage()
	{
	}

	public SerializableHttpRequestMessage(HttpRequestMessage request)
	{
		Method = request.Method.ToString();
		RequestUri = request.RequestUri;
		RequestHeaders = request.Headers.Select(
			x => new KeyValuePair<string, List<string>>(x.Key, new List<string>(x.Value))).ToList()
			.ToDictionary(x => x.Key, x => x.Value);
		if (request.Content != null)
		{
			ContentHeaders = request.Content.Headers.Select(
				x => new KeyValuePair<string, List<string>>(x.Key, new List<string>(x.Value))).ToList()
				.ToDictionary(x => x.Key, x => x.Value);
			Content = request.Content.ReadAsStringAsync().Result;
		}
	}
}

[DataContract]
public class SerializableHttpResponseMessage
{
	[DataMember]
	public HttpStatusCode StatusCode { get; set; }
	[DataMember]
	public Uri RequestUri { get; set; }
	[DataMember]
	public Dictionary<string, List<string>> ResponseHeaders;
	[DataMember]
	public Dictionary<string, List<string>> ContentHeaders;
	[DataMember]
	public string? Content { get; set; }

	public SerializableHttpResponseMessage()
	{

	}

	public SerializableHttpResponseMessage(HttpResponseMessage response)
	{
		StatusCode = response.StatusCode;
		RequestUri = response.RequestMessage.RequestUri;
		ResponseHeaders = response.Headers.Select(
				x => new KeyValuePair<string, List<string>>(x.Key, new List<string>(x.Value))).ToList()
			.ToDictionary(x => x.Key, x => x.Value);
		if (response.Content != null)
		{
			ContentHeaders = response.Content.Headers.Select(
					x => new KeyValuePair<string, List<string>>(x.Key, new List<string>(x.Value))).ToList()
				.ToDictionary(x => x.Key, x => x.Value);
			Content = response.Content.ReadAsStringAsync().Result;
		}
	}
}

public class MockingRequestExecutor
{
	private readonly ODataClientSettings _settings;
	private readonly string? _mockDataPathBase;
	private readonly string[]? _mockResponses;
	private readonly bool _validate;
	private readonly bool _recording;
	private int _fileCounter;
	private static readonly Regex _regexBatch = new(@"batch_([0-9AFa-f]){8}-([0-9AFa-f]){4}-([0-9AFa-f]){4}-([0-9AFa-f]){4}-([0-9AFa-f]){12}");
	private static readonly Regex _regexChangeset = new(@"changeset_([0-9AFa-f]){8}-([0-9AFa-f]){4}-([0-9AFa-f]){4}-([0-9AFa-f]){4}-([0-9AFa-f]){12}");
	private static readonly Regex _regexBaseUrl = new(@"http:\/\/((\w|_|-|\.|)+\/){3}");

	public MockingRequestExecutor(ODataClientSettings settings, string mockDataPathBase, bool validate, bool recording)
	{
		_settings = settings;
		_mockDataPathBase = mockDataPathBase;
		_mockResponses = null;
		_validate = validate;
		_recording = recording;
	}

	public MockingRequestExecutor(ODataClientSettings settings, IEnumerable<string> mockResponses, bool validate, bool recording)
	{
		_settings = settings;
		_mockDataPathBase = null;
		_mockResponses = mockResponses.ToArray();
		_validate = validate;
		_recording = recording;
	}

	public async Task<HttpResponseMessage> ExecuteRequestAsync(HttpRequestMessage request)
	{
		if (_recording)
		{
			if (!IsMetadataRequest(request))
			{
				SaveRequest(request);
			}

			var httpConnection = new HttpConnection(_settings);
			var response = await httpConnection.HttpClient.SendAsync(request).ConfigureAwait(false);

			if (!IsMetadataRequest(request))
			{
				SaveResponse(response);
			}

			return response;
		}
		else
		{
			if (_validate)
			{
				await ValidateRequestAsync(request).ConfigureAwait(false);
			}

			if (_mockResponses == null)
			{
				return GetResponseFromResponseMessage(request);
			}
			else
			{
				return GetResponseFromJson(request);
			}
		}
	}

	private static bool IsMetadataRequest(HttpRequestMessage request)
	{
		return request.RequestUri.LocalPath.EndsWith(ODataLiteral.Metadata);
	}

	private string GenerateMockDataPath()
	{
		if (!string.IsNullOrEmpty(_mockDataPathBase))
		{
			return string.Format($"{_mockDataPathBase}.{++_fileCounter}.txt");
		}
		else
		{
			return _mockResponses[_fileCounter++];
		}
	}

	private void SaveRequest(HttpRequestMessage request)
	{
		using var stream = new FileStream(GenerateMockDataPath(), FileMode.Create);
		var ser = new DataContractJsonSerializer(typeof(SerializableHttpRequestMessage));
		ser.WriteObject(stream, new SerializableHttpRequestMessage(request));
	}

	private void SaveResponse(HttpResponseMessage response)
	{
		using var stream = new FileStream(GenerateMockDataPath(), FileMode.Create);
		var ser = new DataContractJsonSerializer(typeof(SerializableHttpResponseMessage));
		ser.WriteObject(stream, new SerializableHttpResponseMessage(response));
	}

	private async Task ValidateRequestAsync(HttpRequestMessage request)
	{
		using var stream = new FileStream(GenerateMockDataPath(), FileMode.Open);
		var ser = new DataContractJsonSerializer(typeof(SerializableHttpRequestMessage));
		var savedRequest = ser.ReadObject(stream) as SerializableHttpRequestMessage;
		Assert.Equal(savedRequest.Method, request.Method.ToString());
		Assert.Equal(savedRequest.RequestUri.AbsolutePath.Split('/').Last(), request.RequestUri.AbsolutePath.Split('/').Last());
		var expectedHeaders = new Dictionary<string, IEnumerable<string>>();
		foreach (var header in savedRequest.RequestHeaders)
		{
			expectedHeaders.Add(header.Key, header.Value);
		}

		var actualHeaders = new Dictionary<string, IEnumerable<string>>();
		foreach (var header in request.Headers)
		{
			actualHeaders.Add(header.Key, header.Value);
		}

		ValidateHeaders(expectedHeaders, actualHeaders);
		if (request.Content != null)
		{
			expectedHeaders = new Dictionary<string, IEnumerable<string>>();
			foreach (var header in savedRequest.ContentHeaders)
			{
				expectedHeaders.Add(header.Key, header.Value);
			}

			actualHeaders = new Dictionary<string, IEnumerable<string>>();
			foreach (var header in request.Content.Headers)
			{
				actualHeaders.Add(header.Key, header.Value);
			}

			ValidateHeaders(expectedHeaders, actualHeaders);
			var expectedContent = savedRequest.Content;
			expectedContent = AdjustContent(expectedContent);
			var actualContent = AdjustContent(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
			Assert.Equal(expectedContent, actualContent);
		}
	}

	private HttpResponseMessage GetResponseFromResponseMessage(HttpRequestMessage request)
	{
		using var stream = new FileStream(GenerateMockDataPath(), FileMode.Open);
		var ser = new DataContractJsonSerializer(typeof(SerializableHttpResponseMessage));
		var savedResponse = ser.ReadObject(stream) as SerializableHttpResponseMessage;
		var response = new HttpResponseMessage
		{
			StatusCode = savedResponse.StatusCode,
			Content = savedResponse.Content == null
				? null
				: new StreamContent(Utils.StringToStream(savedResponse.Content)),
			RequestMessage = request,
			Version = new Version(1, 1),
		};
		foreach (var header in savedResponse.ResponseHeaders)
		{
			if (response.Headers.Contains(header.Key))
			{
				response.Headers.Remove(header.Key);
			}

			response.Headers.Add(header.Key, header.Value);
		}

		if (savedResponse.Content != null)
		{
			foreach (var header in savedResponse.ContentHeaders)
			{
				if (response.Content.Headers.Contains(header.Key))
				{
					response.Content.Headers.Remove(header.Key);
				}

				response.Content.Headers.Add(header.Key, header.Value);
			}
		}

		return response;
	}

	private HttpResponseMessage GetResponseFromJson(HttpRequestMessage request)
	{
		var response = new HttpResponseMessage
		{
			StatusCode = HttpStatusCode.OK,
			Content = new StreamContent(Utils.StringToStream(File.ReadAllText(GenerateMockDataPath()))),
			RequestMessage = request,
			Version = new Version(1, 1),
		};
		response.Headers.Add("DataServiceVersion", "1.0;");
		response.Content.Headers.Add("Content-Type", "application/json; type=feed; charset=utf-8");
		return response;
	}

	private static void ValidateHeaders(
		IDictionary<string, IEnumerable<string>> expectedHeaders,
		IDictionary<string, IEnumerable<string>> actualHeaders)
	{
		Assert.Equal(expectedHeaders.Count, actualHeaders.Count);
		foreach (var header in expectedHeaders)
		{
			Assert.Contains(header.Key, actualHeaders.Keys);
			if (header.Key != "Content-Length")
			{
				var expectedValue = AdjustBatchIds(header.Value.FirstOrDefault());
				var actualValue = AdjustBatchIds(actualHeaders[header.Key].FirstOrDefault());
				Assert.Equal(expectedValue, actualValue);
			}
		}
	}

	private static string AdjustContent(string content)
	{
		return
			AdjustBatchIds(
			AdjustNewLines(
			AdjustBaseUrl(
			RemoveElements(content, new[] { "updated" }))));

	}

	private static string RemoveElements(string content, IEnumerable<string> elementNames)
	{
		foreach (var elementName in elementNames)
		{
			while (true)
			{
				var startPos = content.IndexOf($"<{elementName}>");
				var endPos = content.IndexOf($"</{elementName}>");
				if (startPos >= 0 && endPos > startPos)
				{
					content = content[..startPos] + content[(endPos + elementName.Length + 3)..];
				}
				else
				{
					break;
				}
			}
		}

		return content;
	}

	private static string AdjustNewLines(string content)
	{
		return content.Replace("\r\n", "\n");
	}

	private static string AdjustBaseUrl(string content)
	{
		return _regexBaseUrl.Replace(content, "http://localhost/");
	}

	private static string AdjustBatchIds(string content)
	{
		var result = _regexBatch.Replace(content, Guid.Empty.ToString());
		result = _regexChangeset.Replace(result, Guid.Empty.ToString());
		return result;
	}
}

public static partial class ODataClientSettingsExtensionMethods
{
	private const string MockDataDir = @"../../../../MockData";

	public static ODataClientSettings WithNameResolver(this ODataClientSettings settings, INameMatchResolver resolver)
	{
		settings.NameMatchResolver = resolver;
		return settings;
	}

	public static ODataClientSettings WithAnnotations(this ODataClientSettings settings)
	{
		settings.IncludeAnnotationsInResults = true;
		return settings;
	}

	public static ODataClientSettings WithIgnoredUnmappedProperties(this ODataClientSettings settings)
	{
		settings.IgnoreUnmappedProperties = true;
		return settings;
	}

	public static ODataClientSettings WithIgnoredResourceNotFoundException(this ODataClientSettings settings)
	{
		settings.IgnoreResourceNotFoundException = true;
		return settings;
	}

	public static ODataClientSettings WithRequestInterceptor(this ODataClientSettings settings, Action<HttpRequestMessage> action)
	{
		settings.BeforeRequest = action;
		return settings;
	}

	public static ODataClientSettings WithResponseInterceptor(this ODataClientSettings settings, Action<HttpResponseMessage> action)
	{
		settings.AfterResponse = action;
		return settings;
	}

	public static ODataClientSettings WithHttpMock(this ODataClientSettings settings)
	{
		var methodName = GetTestMethodFullName();
		var mockDataPathBase = GetMockDataPathBase(methodName);
#if MOCK_HTTP
		var recording = false;
#else
            var recording = true;
#endif
		var requestExecutor = new MockingRequestExecutor(settings, mockDataPathBase, true, recording);
		settings.RequestExecutor = requestExecutor.ExecuteRequestAsync;
		return settings;
	}

	public static ODataClientSettings WithHttpResponses(this ODataClientSettings settings, IEnumerable<string> responses)
	{
		var requestExecutor = new MockingRequestExecutor(settings, responses, false, false);
		settings.RequestExecutor = requestExecutor.ExecuteRequestAsync;
		return settings;
	}

	private static string GetMockDataPathBase(string testMethodName)
	{
		return Path.Combine(MockDataDir, testMethodName);
	}

	private static string GetTestMethodFullName()
	{
		var stackTrace = new System.Diagnostics.StackTrace();
		for (var frameNumber = 2; ; frameNumber++)
		{
			var stackFrame = stackTrace.GetFrame(frameNumber);
			if (stackFrame == null)
			{
				throw new InvalidOperationException("Attempt to retrieve a frame beyond the call stack.");
			}

			var method = stackFrame.GetMethod();
			var methodName = new string(method.Name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
			if (method.IsPublic && !method.IsGenericMethod)
			{
				return string.Format($"{method.DeclaringType.Name}.{methodName}");
			}
		}
	}
}

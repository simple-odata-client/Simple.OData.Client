using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Simple.OData.Client;

public abstract class BatchWriterBase : IBatchWriter
{
	protected readonly ISession _session;
	private readonly Dictionary<IDictionary<string, object>, string> _contentIdMap;
	protected bool _pendingChangeSet;

	protected BatchWriterBase(ISession session, IDictionary<object, IDictionary<string, object>> batchEntries)
	{
		_session = session;
		LastOperationId = 0;
		_contentIdMap = new Dictionary<IDictionary<string, object>, string>();
		BatchEntries = batchEntries;
	}

	public abstract Task StartBatchAsync();
	public abstract Task<HttpRequestMessage> EndBatchAsync();

	public async Task<ODataRequest> CreateBatchRequestAsync(
		IODataClient client, IList<Func<IODataClient, Task>> actions, IList<int> responseIndexes,
		IDictionary<string, string>? headers = null)
	{
		// Write batch operations into a batch content
		var lastOperationId = 0;
		foreach (var action in actions)
		{
			await action(client)
				.ConfigureAwait(false);
			var responseIndex = -1;
			if (LastOperationId > lastOperationId)
			{
				lastOperationId = LastOperationId;
				responseIndex = lastOperationId - 1;
			}

			responseIndexes.Add(responseIndex);
		}

		if (HasOperations)
		{
			// Create batch request message
			var requestMessage = await EndBatchAsync()
				.ConfigureAwait(false);

			foreach (var header in headers)
			{
				requestMessage.Headers.Add(header.Key, header.Value);
			}

			return new ODataRequest(RestVerbs.Post, _session, ODataLiteral.Batch, requestMessage);
		}
		else
		{
			return null;
		}
	}

	protected abstract Task StartChangesetAsync();
	protected abstract Task EndChangesetAsync();
	protected abstract Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired);

	public int LastOperationId { get; private set; }

	public string NextContentId()
	{
		return (++LastOperationId).ToString(CultureInfo.InvariantCulture);
	}

	public string GetContentId(IDictionary<string, object> entryData, object linkData)
	{
		if (!_contentIdMap.TryGetValue(entryData, out var contentId) && linkData != null)
		{
			if (BatchEntries.TryGetValue(linkData, out var mappedEntry))
			{
				_contentIdMap.TryGetValue(mappedEntry, out contentId);
			}
		}

		return contentId;
	}

	public void MapContentId(IDictionary<string, object> entryData, string contentId)
	{
		if (entryData != null && !_contentIdMap.TryGetValue(entryData, out _))
		{
			_contentIdMap.Add(entryData, contentId);
		}
	}

	public IDictionary<object, IDictionary<string, object>> BatchEntries { get; private set; }

	public async Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, IDictionary<string, object> entryData, bool resultRequired)
	{
		if (method != RestVerbs.Get && !_pendingChangeSet)
		{
			await StartChangesetAsync()
				.ConfigureAwait(false);
			_pendingChangeSet = true;
		}
		else if (method == RestVerbs.Get && _pendingChangeSet)
		{
			await EndChangesetAsync()
				.ConfigureAwait(false);
			_pendingChangeSet = false;
		}

		var contentId = NextContentId();
		if (method != RestVerbs.Get && method != RestVerbs.Delete)
		{
			MapContentId(entryData, contentId);
		}

		return await CreateOperationMessageAsync(uri, method, collection, contentId, resultRequired)
			.ConfigureAwait(false);
	}

	public bool HasOperations { get; protected set; }

	protected HttpRequestMessage CreateMessageFromStream(Stream stream, Uri requestUrl, Func<string, string> getHeaderFunc)
	{
		_pendingChangeSet = false;
		stream.Position = 0;

		var httpRequest = new HttpRequestMessage()
		{
			RequestUri = Utils.CreateAbsoluteUri(requestUrl.AbsoluteUri, ODataLiteral.Batch),
			Method = HttpMethod.Post,
			Content = new StreamContent(stream),
		};
		httpRequest.Content.Headers.Add(HttpLiteral.ContentType, getHeaderFunc(HttpLiteral.ContentType));
		return httpRequest;
	}
}

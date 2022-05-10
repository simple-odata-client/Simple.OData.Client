using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

public class ClientWithResponse<T> : IClientWithResponse<T>
	where T : class
{
	private readonly ISession _session;
	private readonly ODataRequest _request;

	public HttpResponseMessage ResponseMessage { get; private set; }

	public ClientWithResponse(ISession session, ODataRequest request, HttpResponseMessage responseMessage)
	{
		_session = session;
		_request = request;
		ResponseMessage = responseMessage;
	}

	private ITypeCache TypeCache => _session.TypeCache;

	public void Dispose()
	{
		ResponseMessage?.Dispose();
	}

	public Task<Stream> GetResponseStreamAsync()
	{
		return GetResponseStreamAsync(CancellationToken.None);
	}

	public async Task<Stream> GetResponseStreamAsync(CancellationToken cancellationToken)
	{
		if (ResponseMessage.IsSuccessStatusCode && ResponseMessage.StatusCode != HttpStatusCode.NoContent &&
			(_request.Method == RestVerbs.Get || _request.ResultRequired))
		{
			var stream = new MemoryStream();
			await ResponseMessage.Content.CopyToAsync(stream)
				.ConfigureAwait(false);
			if (stream.CanSeek)
			{
				stream.Seek(0L, SeekOrigin.Begin);
			}

			return stream;
		}
		else
		{
			return Stream.Null;
		}
	}

	public Task<IEnumerable<T>> ReadAsCollectionAsync()
	{
		return ReadAsCollectionAsync(CancellationToken.None);
	}

	public Task<IEnumerable<T>> ReadAsCollectionAsync(CancellationToken cancellationToken)
	{
		return ReadAsCollectionAsync(null, CancellationToken.None);
	}

	public Task<IEnumerable<T>> ReadAsCollectionAsync(ODataFeedAnnotations annotations)
	{
		return ReadAsCollectionAsync(annotations, CancellationToken.None);
	}

	public async Task<IEnumerable<T>> ReadAsCollectionAsync(ODataFeedAnnotations annotations, CancellationToken cancellationToken)
	{
		if (ResponseMessage.IsSuccessStatusCode && ResponseMessage.StatusCode != HttpStatusCode.NoContent &&
			(_request.Method == RestVerbs.Get || _request.ResultRequired))
		{
			var responseReader = _session.Adapter.GetResponseReader();
			var response = await responseReader
				.GetResponseAsync(ResponseMessage)
				.ConfigureAwait(false);
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (annotations != null && response.Feed != null)
			{
				annotations.CopyFrom(response.Feed.Annotations);
			}

			var result = response.AsEntries(_session.Settings.IncludeAnnotationsInResults);
			return result.Select(x => x.ToObject<T>(TypeCache));
		}
		else
		{
			return Array.Empty<T>();
		}
	}

	public Task<T> ReadAsSingleAsync()
	{
		return ReadAsSingleAsync(CancellationToken.None);
	}

	public async Task<T> ReadAsSingleAsync(CancellationToken cancellationToken)
	{
		if (ResponseMessage.IsSuccessStatusCode && ResponseMessage.StatusCode != HttpStatusCode.NoContent &&
			(_request.Method == RestVerbs.Get || _request.ResultRequired))
		{
			var responseReader = _session.Adapter.GetResponseReader();
			var response = await responseReader
				.GetResponseAsync(ResponseMessage)
				.ConfigureAwait(false);
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

			var result = response.AsEntries(_session.Settings.IncludeAnnotationsInResults);
			return result?.FirstOrDefault().ToObject<T>(TypeCache);
		}
		else
		{
			return default(T);
		}
	}

	public Task<U> ReadAsScalarAsync<U>()
	{
		return ReadAsScalarAsync<U>(CancellationToken.None);
	}

	public async Task<U> ReadAsScalarAsync<U>(CancellationToken cancellationToken)
	{
		if (ResponseMessage.IsSuccessStatusCode && ResponseMessage.StatusCode != HttpStatusCode.NoContent &&
			(_request.Method == RestVerbs.Get || _request.ResultRequired))
		{
			var responseReader = _session.Adapter.GetResponseReader();
			var response = await responseReader
				.GetResponseAsync(ResponseMessage)
				.ConfigureAwait(false);
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

			var result = response.AsEntries(_session.Settings.IncludeAnnotationsInResults);

			static object extractScalar(IDictionary<string, object?> x) => (x == null) || !x.Any() ? null : x.Values.First();
			return result == null ? default(U) : _session.TypeCache.Convert<U>(extractScalar(result.FirstOrDefault()));
		}
		else
		{
			return default(U);
		}
	}
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client;

internal class RequestRunner
{
	private readonly ISession _session;

	public RequestRunner(ISession session)
	{
		_session = session;
	}

	public async Task<HttpResponseMessage> ExecuteRequestAsync(ODataRequest request, CancellationToken cancellationToken)
	{
		HttpConnection? httpConnection = null;
		try
		{
			await PreExecuteAsync(request).ConfigureAwait(false);

			_session.Trace("{0} request: {1}", request.Method, request.RequestMessage.RequestUri.AbsoluteUri);
			if (request.RequestMessage.Content != null && (_session.Settings.TraceFilter & ODataTrace.RequestContent) != 0)
			{
				var content = await request.RequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
				_session.Trace("Request content:{0}{1}", Environment.NewLine, content);
			}

			HttpResponseMessage response;
			if (_session.Settings.RequestExecutor != null)
			{
				response = await _session.Settings.RequestExecutor(request.RequestMessage).ConfigureAwait(false);
			}
			else
			{
				httpConnection = _session.Settings.RenewHttpConnection
					? new HttpConnection(_session.Settings)
					: _session.GetHttpConnection();

				response = await httpConnection.HttpClient.SendAsync(request.RequestMessage, cancellationToken).ConfigureAwait(false);
				if (cancellationToken.IsCancellationRequested)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}

			_session.Trace("Request completed: {0}", response.StatusCode);
			if (response.Content != null && (_session.Settings.TraceFilter & ODataTrace.ResponseContent) != 0)
			{
				var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				_session.Trace("Response content:{0}{1}", Environment.NewLine, content);
			}

			await PostExecute(response).ConfigureAwait(false);
			return response;
		}
		catch (WebException ex)
		{
			throw WebRequestException.CreateFromWebException(ex);
		}
		catch (AggregateException ex)
		{
			if (ex.InnerException is WebException)
			{
				throw WebRequestException.CreateFromWebException(ex.InnerException as WebException);
			}
			else
			{
				throw;
			}
		}
		finally
		{
			if (httpConnection != null && _session.Settings.RenewHttpConnection)
			{
				httpConnection.Dispose();
			}
		}
	}

	private async Task PreExecuteAsync(ODataRequest request)
	{
		if (request.Accept != null)
		{
			foreach (var accept in request.Accept)
			{
				request.RequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
			}
		}

		if (request.CheckOptimisticConcurrency &&
			(request.Method == RestVerbs.Put ||
			 request.Method == RestVerbs.Patch ||
			 request.Method == RestVerbs.Merge ||
			 request.Method == RestVerbs.Delete)
			&& (request.RequestMessage.Headers.IfMatch?.Count ?? 0) == 0)
		{
			request.RequestMessage.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
		}

		foreach (var header in request.Headers)
		{
			if (request.RequestMessage.Headers.TryGetValues(header.Key, out var values) && !values.Contains(header.Value))
			{
				request.RequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
		}

		_session.Settings.BeforeRequest?.Invoke(request.RequestMessage);
		if (_session.Settings.BeforeRequestAsync != null)
		{
			await _session.Settings.BeforeRequestAsync(request.RequestMessage).ConfigureAwait(false);
		}
	}

	private async Task PostExecute(HttpResponseMessage responseMessage)
	{
		_session.Settings.AfterResponse?.Invoke(responseMessage);
		if (_session.Settings.AfterResponseAsync != null)
		{
			await _session.Settings.AfterResponseAsync(responseMessage).ConfigureAwait(false);
		}

		if (!responseMessage.IsSuccessStatusCode)
		{
			throw await WebRequestException.CreateFromResponseMessageAsync(responseMessage, _session.Settings.WebRequestExceptionMessageSource).ConfigureAwait(false);
		}
	}
}

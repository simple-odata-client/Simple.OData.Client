#if DEBUG
#undef TRACE_REQUEST_CONTENT
#undef TRACE_RESPONSE_CONTENT
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    class RequestRunner : IDisposable
    {
        private readonly ISession _session;
        private HttpClient _httpClient;
        private Object _httpClientSyncObject = new Object();
        private bool _isDisposed = false;

        public RequestRunner(ISession session)
        {
            _session = session;
        }

        public async Task<HttpResponseMessage> ExecuteRequestAsync(ODataRequest request, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Cannot access a disposed Request Runner.");
            try
            {
                var httpClient = CreateOrGetHttpClient(request);
                {
                    PreExecute(request);

                    _session.Trace("{0} request: {1}", request.Method, request.RequestMessage.RequestUri.AbsoluteUri);
#if TRACE_REQUEST_CONTENT
                    if (request.RequestMessage.Content != null)
                    {
                        var content = await request.RequestMessage.Content.ReadAsStringAsync();
                        _session.Trace("Request content:{0}{1}", Environment.NewLine, content);
                    }
#endif

                    var response = await httpClient.SendAsync(request.RequestMessage, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

                    _session.Trace("Request completed: {0}", response.StatusCode);
#if TRACE_RESPONSE_CONTENT
                    if (response.Content != null)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _session.Trace("Response content:{0}{1}", Environment.NewLine, content);
                    }
#endif

                    PostExecute(response);
                    return response;
                }
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
        }

        private HttpMessageHandler CreateMessageHandler(ODataRequest request)
        {
            if (_session.Settings.OnCreateMessageHandler != null)
            {
                return _session.Settings.OnCreateMessageHandler();
            }
            else
            {
                var clientHandler = new HttpClientHandler();

                // Perform this test to prevent failure to access Credentials/PreAuthenticate properties on SL5
                if (request.Credentials != null)
                {
                    clientHandler.Credentials = request.Credentials;
                    if (clientHandler.SupportsPreAuthenticate())
                        clientHandler.PreAuthenticate = true;
                }

                if (_session.Settings.OnApplyClientHandler != null)
                {
                    _session.Settings.OnApplyClientHandler(clientHandler);
                }

                return clientHandler;
            }
        }

        private HttpClient CreateOrGetHttpClient(ODataRequest request)
        {
            lock (_httpClientSyncObject)
            {
                if (_httpClient == null)
                {
                    var messageHandler = CreateMessageHandler(request);
                    if (_session.Settings.RequestTimeout >= TimeSpan.FromMilliseconds(1))
                    {
                        _httpClient = new HttpClient(messageHandler)
                        {
                            Timeout = _session.Settings.RequestTimeout,
                        };
                    }
                    else
                    {
                        _httpClient = new HttpClient(messageHandler);
                    }
                }
                return _httpClient;
            }
        }

        private void PreExecute(ODataRequest request)
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
                 request.Method == RestVerbs.Delete))
            {
                request.RequestMessage.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
            }

            foreach (var header in request.Headers)
            {
                request.RequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (_session.Settings.BeforeRequest != null)
                _session.Settings.BeforeRequest(request.RequestMessage);
        }

        private void PostExecute(HttpResponseMessage responseMessage)
        {
            if (_session.Settings.AfterResponse != null)
                _session.Settings.AfterResponse(responseMessage);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new WebRequestException(responseMessage.ReasonPhrase, responseMessage.StatusCode);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            lock (_httpClientSyncObject)
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
            }
        }
    }
}

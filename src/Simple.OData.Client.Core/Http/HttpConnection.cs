using System;
using System.Net.Http;

namespace Simple.OData.Client
{
    public class HttpConnection : IDisposable
    {
        private HttpMessageHandler _messageHandler;
        private HttpClient _httpClient;
        private bool _dispose;

        public HttpClient HttpClient { get { return _httpClient; } }

        public HttpConnection(ODataClientSettings settings)
        {
            _messageHandler = CreateMessageHandler(settings);
            _httpClient = CreateHttpClient(settings, _messageHandler);
            _dispose = settings.OnGetHttpClient == null;
        }

        public void Dispose()
        {
            if (_messageHandler != null)
            {
                if (_dispose)
                    _messageHandler.Dispose();
                _messageHandler = null;
            }

            if (_httpClient != null)
            {
                if (_dispose)
                    _httpClient.Dispose();
                _httpClient = null;
            }
        }

        private static HttpClient CreateHttpClient(ODataClientSettings settings, HttpMessageHandler messageHandler)
        {
            if (settings.HttpClient != null)
                return settings.HttpClient;
            if (settings.RequestTimeout >= TimeSpan.FromMilliseconds(1))
                return new HttpClient(messageHandler) { Timeout = settings.RequestTimeout };
            return new HttpClient(messageHandler);
        }

        private static HttpMessageHandler CreateMessageHandler(ODataClientSettings settings)
        {
            if (settings.OnCreateMessageHandler != null)
            {
                return settings.OnCreateMessageHandler();
            }
            else
            {
                var clientHandler = new HttpClientHandler();

                if (settings.Credentials != null)
                {
                    clientHandler.Credentials = settings.Credentials;
                    clientHandler.PreAuthenticate = true;
                }

                settings.OnApplyClientHandler?.Invoke(clientHandler);

                return clientHandler;
            }
        }
    }
}

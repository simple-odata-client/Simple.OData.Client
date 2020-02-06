using System;
using System.Net.Http;

namespace Simple.OData.Client
{
    public class HttpConnection
    {
        private HttpMessageHandler _messageHandler;
        private HttpClient _httpClient;

        public HttpClient HttpClient { get {  return _httpClient; } }

        public HttpConnection(ODataClientSettings settings)
        {
            _messageHandler = CreateMessageHandler(settings);
            _httpClient = CreateHttpClient(settings, _messageHandler);
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
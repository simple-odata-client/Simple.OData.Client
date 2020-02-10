using System;
using System.Net.Http;
using System.Collections.Concurrent;

namespace Simple.OData.Client
{
    public class HttpConnection
    {
        private static readonly ConcurrentDictionary<string, HttpClient> httpClientCache
            = new ConcurrentDictionary<string, HttpClient>();

        public HttpClient HttpClient { get; }

        public HttpConnection(ODataClientSettings settings)
        {
            HttpClient = CreateHttpClient(settings);
        }

        private static HttpClient CreateHttpClient(ODataClientSettings settings)
        {
            if (settings.HttpClient != null)
                return settings.HttpClient;

            var baseAddress = settings.BaseUri.ToString();

            if (!httpClientCache.TryGetValue(baseAddress, out var client))
            {
                client = new HttpClient(CreateMessageHandler(settings))
                {
                    BaseAddress = settings.BaseUri
                };

                if (!httpClientCache.TryAdd(baseAddress, client)
                    && httpClientCache.TryGetValue(baseAddress, out client))
                {
                    client.Dispose();
                }
            }

            return client;
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
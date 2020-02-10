using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Extensions
{
    public class HttpExtensionTests
    {
        private class MockTimeoutHttpHandler : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> createResponse;

            public MockTimeoutHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse)
            {
                this.createResponse = createResponse;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Properties.TryGetValue("ExecutionTime", out var tmp)
                    && tmp is TimeSpan executionTime && executionTime > TimeSpan.Zero)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    while (sw.Elapsed < executionTime)
                    {
                        Thread.Sleep(100);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                return Task.FromResult(createResponse(request));
            }
        }

        private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest, Func<HttpRequestMessage, HttpResponseMessage> createResponse, TimeSpan executionTime, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var handler = new MockTimeoutHttpHandler(createResponse))
            using (var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") })
            using (var requestMessage = createRequest())
            {
                requestMessage.Properties.Add("ExecutionTime", executionTime);
                return await client.SendWithTimeoutAsync(requestMessage, cancellationToken, timeout);
            }
        }

        private async Task<string> SendAsync(string requestContent, Func<string, string> createResponse, TimeSpan executionTime, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var responseMessage = await SendAsync(
                 () => new HttpRequestMessage
                 {
                     Content = new StringContent(requestContent)
                 },
                 (request) => new HttpResponseMessage
                 {
                     Content = new StringContent(createResponse(requestContent))
                 },
                 executionTime,
                 timeout,
                 cancellationToken
                ))
            {
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        [Theory]
        [InlineData(100, 1000)]
        [InlineData(100, 0)]
        [InlineData(100, Timeout.Infinite)]
        public async Task Should_Complete_Succesfully(int executionTimeMs, int timeoutMs)
        {
            var expectedResponse = "Out";
            var response = await SendAsync("In", (r) => expectedResponse, TimeSpan.FromMilliseconds(executionTimeMs), TimeSpan.FromMilliseconds(timeoutMs), CancellationToken.None);

            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public async Task Should_Timeout()
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
               SendAsync("In", (r) => "Out", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), CancellationToken.None));
        }

        [Fact]
        public async Task Should_Cancel()
        {
            using (var cts = new CancellationTokenSource(100))
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                   SendAsync("In", (r) => "Out", TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan, cts.Token));
            }
        }
    }
}

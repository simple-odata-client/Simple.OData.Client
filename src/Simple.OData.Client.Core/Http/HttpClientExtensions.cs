using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    internal static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> SendWithTimeoutAsync(this HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
                timeout = httpClient.Timeout;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                if (timeout != Timeout.InfiniteTimeSpan
                    && timeout != TimeSpan.Zero
                    && timeout != TimeSpan.MaxValue)
                {
                    cts.CancelAfter(timeout);
                }
                
                try
                {
                    return httpClient.SendAsync(request, cts.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
        }
    }
}

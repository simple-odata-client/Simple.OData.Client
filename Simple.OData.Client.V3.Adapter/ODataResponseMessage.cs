using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.OData;

namespace Simple.OData.Client.V3.Adapter
{
    class ODataResponseMessage : IODataResponseMessageAsync
    {
        private readonly HttpResponseMessage _response;

        public ODataResponseMessage(HttpResponseMessage response)
        {
            _response = response;
        }

        public string GetHeader(string headerName)
        {
            if (headerName == HttpLiteral.ContentType || headerName == HttpLiteral.ContentLength)
            {
                if (_response.Content.Headers.Contains(headerName))
                    return _response.Content.Headers.GetValues(headerName).FirstOrDefault();
                else
                    return null;
            }
            else
            {
                if (_response.Headers.Contains(headerName))
                    return _response.Headers.GetValues(headerName).FirstOrDefault();
                else
                    return null;
            }
        }

        public Stream GetStream()
        {
            return GetStreamAsync().Result;
        }

        public Task<Stream> GetStreamAsync()
        {
            var responseContent = _response.Content as StreamContent;
            if (responseContent != null)
            {
                return responseContent.ReadAsStreamAsync();
            }
            else
            {
                var completionSource = new TaskCompletionSource<Stream>();
                completionSource.SetResult(Stream.Null);
                return completionSource.Task;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get { return _response.Headers
                .Select(h => new KeyValuePair<string, string>(h.Key, h.Value.FirstOrDefault())); }
        }

        public void SetHeader(string headerName, string headerValue)
        {
            throw new NotSupportedException();
        }

        public int StatusCode
        {
            get { return (int)_response.StatusCode; }
            set { throw new NotSupportedException(); }
        }
    }
}
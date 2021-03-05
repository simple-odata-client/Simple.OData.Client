using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public class ODataRequest
    {
        private readonly string _uri;
        private HttpRequestMessage _requestMessage;
        private readonly ODataPayloadFormat _payloadFormat;
        private readonly Stream _contentStream;
        private readonly string _contentType;

        public HttpRequestMessage RequestMessage
        {
            get => GetOrCreateRequestMessage();
            private set => _requestMessage = value;
        }

        public string[] Accept
        {
            get
            {
                bool isMetadataRequest = this.RequestMessage.RequestUri.LocalPath.EndsWith(ODataLiteral.Metadata);
                if (!isMetadataRequest && (this.ReturnsScalarResult || !this.ResultRequired))
                    return null;

                if (isMetadataRequest)
                {
                    return new[] { "application/xml" };
                }
                else
                {
                    switch (this._payloadFormat)
                    {
                        default:
                        case ODataPayloadFormat.Atom:
                            return new[] { "application/atom+xml", "application/xml", "application/text" };

                        case ODataPayloadFormat.Json:
                            return new[] { "application/json", "application/xml", "application/text" };
                    }
                }
            }
        }

        public string CommandText { get; private set; }
        public string Method { get; private set; }
        public IDictionary<string, object> EntryData { get; private set; }
        public bool IsLink { get; set; }
        public ODataPayloadFormat UsePayloadFormat { get; set; }
        public bool ReturnsScalarResult { get; set; }
        public bool ResultRequired { get; set; }
        public bool CheckOptimisticConcurrency { get; set; }
        public readonly IDictionary<string, string> Headers = new Dictionary<string, string>();

        internal ODataRequest(string method, ISession session, string commandText, IDictionary<string, string> headers = null)
        {
            this.CommandText = commandText;
            this.Method = method;

            var uri = new Uri(commandText, UriKind.RelativeOrAbsolute);
            _uri = uri.IsAbsoluteUri
                ? uri.AbsoluteUri
                : Utils.CreateAbsoluteUri(session.Settings.BaseUri.AbsoluteUri, commandText).AbsoluteUri;
            _payloadFormat = session.Settings.PayloadFormat;
            
            if (headers != null)
                Headers = headers;
        }

        internal ODataRequest(string method, ISession session, string commandText, HttpRequestMessage requestMessage)
            : this(method, session, commandText)
        {
            this.RequestMessage = requestMessage;
        }

        internal ODataRequest(string method, ISession session, string commandText, IDictionary<string, object> entryData, Stream contentStream, string mediaType = null, IDictionary<string, string> headers = null)
            : this(method, session, commandText, headers)
        {
            EntryData = entryData;
            _contentStream = contentStream;
            _contentType = mediaType;
        }

        private HttpContent GetContent()
        {
            if (_contentStream == null)
                return null;

            if (_contentStream.CanSeek)
                _contentStream.Seek(0, SeekOrigin.Begin);
            var content = new StreamContent(_contentStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(this.GetContentType());
            content.Headers.ContentLength = _contentStream.Length;
            return content;
        }

        private string GetContentType()
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                return _contentType;
            }
            else
            {
                var payloadFormat = this.UsePayloadFormat != ODataPayloadFormat.Unspecified
                    ? this.UsePayloadFormat
                    : _payloadFormat;

                switch (payloadFormat)
                {
                    default:
                    case ODataPayloadFormat.Atom:
                        return this.IsLink ? "application/xml" : "application/atom+xml";

                    case ODataPayloadFormat.Json:
                        return "application/json";
                }
            }
        }

        private HttpRequestMessage GetOrCreateRequestMessage()
        {
            if (_requestMessage != null)
                return _requestMessage;

            _requestMessage = new HttpRequestMessage(new HttpMethod(this.Method), this._uri)
            {
                Content = this._contentStream != null ? this.GetContent() : null
            };

            if (Headers != null)
            {
                foreach (var header in Headers)
                    _requestMessage.Headers.Add(header.Key, header.Value);
            }

            return _requestMessage;
        }
    }
}

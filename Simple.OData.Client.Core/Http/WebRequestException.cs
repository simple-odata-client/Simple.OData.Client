using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    /// <summary>
    /// The exception that is thrown when the service failed to process the Web request
    /// </summary>
    public class WebRequestException : Exception
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;
        private readonly Uri _requestUri;

        /// <summary>
        /// Creates from the instance of HttpResponseMessage.
        /// </summary>
        /// <param name="response">The instance of <see cref="HttpResponseMessage"/>.</param>
        /// <returns>The instance of <see cref="WebRequestException"/>.</returns>
        public static async Task<WebRequestException> CreateFromResponseMessageAsync(HttpResponseMessage response)
        {
            var requestUri = response.RequestMessage != null ? response.RequestMessage.RequestUri : null;
            return new WebRequestException(response.ReasonPhrase, response.StatusCode, requestUri,
                response.Content != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : null, null);
        }

        /// <summary>
        /// Creates from the instance of WebException.
        /// </summary>
        /// <param name="ex">The instance of <see cref="WebException"/>.</param>
        /// <returns>The instance of <see cref="WebRequestException"/>.</returns>
        public static WebRequestException CreateFromWebException(WebException ex)
        {
            var response = ex.Response as HttpWebResponse;
            return response == null ?
                new WebRequestException(ex) :
                new WebRequestException(ex.Message, response.StatusCode, response.ResponseUri, Utils.StreamToString(response.GetResponseStream()), ex);
        }

        /// <summary>
        /// Creates from the instance of HttpResponseMessage.
        /// </summary>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <returns>The instance of <see cref="WebRequestException"/>.</returns>
        public static WebRequestException CreateFromStatusCode(HttpStatusCode statusCode, string responseContent = null)
        {
            return new WebRequestException(statusCode.ToString(), statusCode, null, responseContent, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="requestUri">The original request URI.</param>
        /// <param name="responseContent">The response content.</param>
        /// <param name="inner">The inner exception.</param>
        private WebRequestException(string message, HttpStatusCode statusCode, Uri requestUri, string responseContent, Exception inner)
            : base(message, inner)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
            _requestUri = requestUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestException"/> class.
        /// </summary>
        /// <param name="inner">The inner exception.</param>
        private WebRequestException(WebException inner)
            : base("Unexpected WebException encountered", inner)
        {
        }

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <value>
        /// The <see cref="HttpStatusCode"/>.
        /// </value>
        public HttpStatusCode Code
        {
            get { return _statusCode; }
        }

        /// <summary>
        /// Gets the HTTP response text.
        /// </summary>
        /// <value>
        /// The response text.
        /// </value>
        public string Response
        {
            get { return _responseContent; }
        }

        /// <summary>
        /// Gets the HTTP Uri
        /// </summary>
        /// <value>
        /// The original request URI, or the resulting URI if a redirect took place.
        /// </value>
        public Uri RequestUri
        {
            get { return _requestUri; }
        }
    }
}

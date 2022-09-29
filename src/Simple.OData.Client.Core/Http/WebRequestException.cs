using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Simple.OData.Client;

/// <summary>
/// The exception that is thrown when the service failed to process the Web request
/// </summary>
public class WebRequestException : Exception
{

	/// <summary>
	/// Creates from the instance of HttpResponseMessage.
	/// </summary>
	/// <param name="response">The instance of <see cref="HttpResponseMessage"/>.</param>
	/// <param name="exceptionMessageSource">The source used to build exception message, see <see cref="WebRequestExceptionMessageSource"/></param>
	/// <returns>The instance of <see cref="WebRequestException"/>.</returns>
	public async static Task<WebRequestException> CreateFromResponseMessageAsync(HttpResponseMessage response, WebRequestExceptionMessageSource exceptionMessageSource)
	{
		var requestUri = response.RequestMessage?.RequestUri;
		return new WebRequestException(response.ReasonPhrase, response.StatusCode, requestUri,
			response.Content != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : null, exceptionMessageSource, null);
	}

	/// <summary>
	/// Creates from the instance of WebException.
	/// </summary>
	/// <param name="ex">The instance of <see cref="WebException"/>.</param>
	/// <param name="exceptionMessageSource">The source used to build exception message, see <see cref="WebRequestExceptionMessageSource"/></param>
	/// <returns>The instance of <see cref="WebRequestException"/>.</returns>
	public static WebRequestException CreateFromWebException(WebException ex, WebRequestExceptionMessageSource exceptionMessageSource = WebRequestExceptionMessageSource.ReasonPhrase)
	{
		return ex.Response is not HttpWebResponse response ?
			new WebRequestException(ex) :
			new WebRequestException(ex.Message, response.StatusCode, response.ResponseUri, Utils.StreamToString(response.GetResponseStream()), exceptionMessageSource, ex);
	}

	/// <summary>
	/// Creates from the instance of HttpResponseMessage.
	/// </summary>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="exceptionMessageSource">The source used to build exception message, see <see cref="WebRequestExceptionMessageSource"/></param>
	/// <param name="responseContent"></param>
	/// <returns>The instance of <see cref="WebRequestException"/>.</returns>
	public static WebRequestException CreateFromStatusCode(
		HttpStatusCode statusCode,
		WebRequestExceptionMessageSource exceptionMessageSource,
		string? responseContent = null)
	{
		return new WebRequestException(statusCode.ToString(), statusCode, null, responseContent, exceptionMessageSource, null);
	}

	private static string? BuildMessage(
		HttpStatusCode statusCode,
		string reasonPhrase,
		string? responseContent,
		WebRequestExceptionMessageSource exceptionMessageSource)
	{
		reasonPhrase ??= statusCode.ToString();
		if (exceptionMessageSource == WebRequestExceptionMessageSource.ReasonPhrase)
		{
			return reasonPhrase;
		}
		else if (exceptionMessageSource == WebRequestExceptionMessageSource.ResponseContent)
		{
			return responseContent;
		}
		else
		{
			return $"Request failed with reason {((int)statusCode)} {reasonPhrase}. Response content was {responseContent}.";
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WebRequestException"/> class.
	/// </summary>
	/// <param name="reasonPhrase">The message that describes the error.</param>
	/// <param name="statusCode">The HTTP status code.</param>
	/// <param name="requestUri">The original request URI.</param>
	/// <param name="responseContent">The response content.</param>
	/// <param name="exceptionMessageSource">The source used to build exception message, see <see cref="WebRequestExceptionMessageSource"/></param>
	/// <param name="inner">The inner exception.</param>
	private WebRequestException(
		string reasonPhrase,
		HttpStatusCode statusCode,
		Uri? requestUri,
		string? responseContent,
		WebRequestExceptionMessageSource exceptionMessageSource,
		Exception inner)
		: base(BuildMessage(statusCode, reasonPhrase, responseContent, exceptionMessageSource), inner)
	{
		ReasonPhrase = reasonPhrase;
		Code = statusCode;
		Response = responseContent;
		RequestUri = requestUri;
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
	public HttpStatusCode Code { get; private set; }

	/// <summary>
	/// Gets the HTTP response text.
	/// </summary>
	/// <value>
	/// The response text.
	/// </value>
	public string? Response { get; private set; }

	/// <summary>
	/// Gets the HTTP Uri
	/// </summary>
	/// <value>
	/// The original request URI, or the resulting URI if a redirect took place.
	/// </value>
	public Uri? RequestUri { get; private set; }

	/// <summary>
	/// Gets the reason phrase associated with the Http status code.
	/// </summary>
	public string ReasonPhrase { get; private set; }
}

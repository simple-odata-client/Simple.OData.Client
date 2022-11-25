using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Devbridge.BasicAuthentication;

/// <summary>
/// This module performs basic authentication.
/// For details on basic authentication see RFC 2617.
/// Based on the work by Mike Volodarsky (www.iis.net/learn/develop/runtime-extensibility/developing-a-module-using-net)
///
/// The basic operational flow is:
///
/// On AuthenticateRequest:
///     extract the basic authentication credentials
///     verify the credentials
///     if successful, create and send authentication cookie
///
/// On SendResponseHeaders:
///     if there is no authentication cookie in request, clear response, add unauthorized status code (401) and
///     add the basic authentication challenge to trigger basic authentication.
/// </summary>
public class BasicAuthenticationModule : IHttpModule
{
	/// <summary>
	/// HTTP1.1 Authorization header
	/// </summary> 
	public const string HttpAuthorizationHeader = "Authorization";

	/// <summary>
	/// HTTP1.1 Basic Challenge Scheme Name
	/// </summary>
	public const string HttpBasicSchemeName = "Basic"; // 

	/// <summary>
	/// HTTP1.1 Credential username and password separator
	/// </summary>
	public const char HttpCredentialSeparator = ':';

	/// <summary>
	/// HTTP1.1 Not authorized response status code
	/// </summary>
	public const int HttpNotAuthorizedStatusCode = 401;

	/// <summary>
	/// HTTP1.1 Basic Challenge Scheme Name
	/// </summary>
	public const string HttpWwwAuthenticateHeader = "WWW-Authenticate";

	/// <summary>
	/// The name of cookie that is sent to client
	/// </summary>
	public const string AuthenticationCookieName = "BasicAuthentication";

	/// <summary>
	/// HTTP.1.1 Basic Challenge Realm
	/// </summary>
	public const string Realm = "ODataIntegration";

	private IDictionary<string, string> activeUsers;

	public void AuthenticateUser(object source, EventArgs e)
	{
		var context = ((HttpApplication)source).Context;

		var authorizationHeader = context.Request.Headers[HttpAuthorizationHeader];

		// Extract the basic authentication credentials from the request
		string? userName = null;
		string? password = null;
		if (!ExtractBasicCredentials(authorizationHeader, ref userName, ref password))
		{
			return;
		}

		// Validate the user credentials
		if (!ValidateCredentials(userName, password))
		{
			return;
		}

		// check whether cookie is set and send it to client if needed
		var authCookie = context.Request.Cookies.Get(AuthenticationCookieName);
		if (authCookie == null)
		{
			authCookie = new HttpCookie(AuthenticationCookieName, "1") { Expires = DateTime.Now.AddHours(1) };
			context.Response.Cookies.Add(authCookie);
		}
	}

	public void IssueAuthenticationChallenge(object source, EventArgs e)
	{
		var context = ((HttpApplication)source).Context;
		if (!context.Request.Path.Contains("/secure"))
		{
			return;
		}

		var authorizationHeader = context.Request.Headers[HttpAuthorizationHeader];

		string? userName = null;
		string? password = null;
		if (ExtractBasicCredentials(authorizationHeader, ref userName, ref password) &&
			ValidateCredentials(userName, password))
		{
			return;
		}

		// if authentication cookie is not set issue a basic challenge
		var authCookie = context.Request.Cookies.Get(AuthenticationCookieName);
		if (authCookie == null)
		{
			//make sure that user is not authenticated yet
			if (!context.Response.Cookies.AllKeys.Contains(AuthenticationCookieName))
			{
				context.Response.Clear();
				context.Response.StatusCode = HttpNotAuthorizedStatusCode;
				context.Response.AddHeader(HttpWwwAuthenticateHeader, "Basic realm =\"" + Realm + "\"");
			}
		}
	}

	protected virtual bool ValidateCredentials(string userName, string password)
	{
		if (activeUsers.ContainsKey(userName) && activeUsers[userName] == password)
		{
			return true;
		}

		return false;
	}

	protected virtual bool ExtractBasicCredentials(string authorizationHeader, ref string? username, ref string? password)
	{
		if (string.IsNullOrEmpty(authorizationHeader))
		{
			return false;
		}

		var verifiedAuthorizationHeader = authorizationHeader.Trim();
		if (verifiedAuthorizationHeader.IndexOf(HttpBasicSchemeName, StringComparison.InvariantCultureIgnoreCase) != 0)
		{
			return false;
		}

		// get the credential payload
		verifiedAuthorizationHeader = verifiedAuthorizationHeader.Substring(HttpBasicSchemeName.Length, verifiedAuthorizationHeader.Length - HttpBasicSchemeName.Length).Trim();
		// decode the base 64 encoded credential payload
		var credentialBase64DecodedArray = Convert.FromBase64String(verifiedAuthorizationHeader);
		var decodedAuthorizationHeader = Encoding.UTF8.GetString(credentialBase64DecodedArray, 0, credentialBase64DecodedArray.Length);

		// get the username, password, and realm
		var separatorPosition = decodedAuthorizationHeader.IndexOf(HttpCredentialSeparator);

		if (separatorPosition <= 0)
		{
			return false;
		}

		username = decodedAuthorizationHeader.Substring(0, separatorPosition).Trim();
		password = decodedAuthorizationHeader.Substring(separatorPosition + 1, (decodedAuthorizationHeader.Length - separatorPosition - 1)).Trim();

		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
		{
			return false;
		}

		return true;
	}

	public void Init(HttpApplication context)
	{
		var config = System.Configuration.ConfigurationManager.GetSection("basicAuth");
		var basicAuth = (Configuration.BasicAuthenticationConfigurationSection)config;
		activeUsers = new Dictionary<string, string>();

		for (var i = 0; i < basicAuth.Credentials.Count; i++)
		{
			var credential = basicAuth.Credentials[i];
			activeUsers.Add(credential.UserName, credential.Password);
		}

		// Subscribe to the authenticate event to perform the authentication.
		context.AuthenticateRequest += AuthenticateUser;

		// Subscribe to the EndRequest event to issue the authentication challenge if necessary.
		context.EndRequest += IssueAuthenticationChallenge;
	}

	public void Dispose()
	{
		// Do nothing here
	}
}

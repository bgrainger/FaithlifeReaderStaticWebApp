using System.Net.Http.Headers;
using Faithlife.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions;

public static class OAuthSignIn
{
	[FunctionName("OAuthSignIn")]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
		ILogger log)
	{
		log.LogInformation("OAuthSignIn HTTP trigger function processing a request.");

		var oauthToken = req.Query["oauth_token"];
		var oauthVerifier = req.Query["oauth_verifier"];
		var redirectUri = req.Query["redirect"];
		log.LogDebug("token={0}, verifier={1}", oauthToken, oauthVerifier);
		if (oauthToken.Count != 1 || oauthVerifier.Count != 1 || redirectUri.Count != 1)
			return new BadRequestResult();

		var accessCredentialsMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(Utility.OAuthBaseUri, "accesstoken"));
		accessCredentialsMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateAuthorizationHeaderValue(Utility.ConsumerToken, Utility.ConsumerSecret, oauthToken, SignIn.GetSecret(oauthToken), oauthVerifier));

		using var httpClient = new HttpClient();
		var accessCredentials = await httpClient.GetFormValuesAsync(accessCredentialsMessage);
		var accessToken = accessCredentials["oauth_token"];
		var accessSecret = accessCredentials["oauth_token_secret"];
		log.LogDebug("token={0}, secret={1}", accessToken, accessSecret);
		if (accessToken is null || accessSecret is null)
			return new BadRequestResult();

		var options = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true };

		var dataToEncrypt = $"{accessToken}/{accessSecret}";
		var encryptedAuth = Encryption.Encrypt(dataToEncrypt);
		req.HttpContext.Response.Cookies.Append("faithlife-reader-auth", Convert.ToBase64String(encryptedAuth), options);
		req.HttpContext.Response.Headers["Cache-Control"] = "no-store, max-age=0";

		return new RedirectResult(redirectUri);
	}
}

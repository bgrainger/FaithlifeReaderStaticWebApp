using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Faithlife.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions
{
	public static class OAuthSignIn
	{
		[FunctionName("OAuthSignIn")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("OAuthSignIn HTTP trigger function processing a request.");

			string oauthToken = req.Query["oauth_token"];
			var oauthVerifier = req.Query["oauth_verifier"];
			var redirectUri = req.Query["redirect"];
			log.LogDebug("token={0}, verifier={1}", oauthToken, oauthVerifier);

			var accessCredentialsMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(Utility.OAuthBaseUri, "accesstoken"));
			accessCredentialsMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateAuthorizationHeaderValue(Utility.ConsumerToken, Utility.ConsumerSecret, oauthToken, SignIn.GetSecret(oauthToken), oauthVerifier));

			using var httpClient = new HttpClient();
			var accessCredentials = await httpClient.GetFormValuesAsync(accessCredentialsMessage);
			oauthToken = accessCredentials["oauth_token"];
			var oauthSecret = accessCredentials["oauth_token_secret"];
			log.LogDebug("token={0}, secret={1}", oauthToken, oauthSecret);

			var options = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true };

			var dataToEncrypt = $"{oauthToken}/{oauthSecret}";
			var encryptedAuth = Encryption.Encrypt(dataToEncrypt);
			req.HttpContext.Response.Cookies.Append("faithlife-reader-auth", Convert.ToBase64String(encryptedAuth), options);
			req.HttpContext.Response.Headers["Cache-Control"] = "no-store, max-age=0";

			return new RedirectResult(redirectUri);
		}
	}
}

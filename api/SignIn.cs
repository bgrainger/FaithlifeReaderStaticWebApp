using System;
using System.Collections.Generic;
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
	public static class SignIn
	{
		[FunctionName("SignIn")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("SignIn HTTP trigger function processing a request.");

			var redirectUri = new Uri(req.Query["redirect"]);
			var callbackUri = new Uri(redirectUri, $"/api/OAuthSignIn?redirect={Uri.EscapeDataString(redirectUri.AbsoluteUri)}");
			log.LogDebug("Callback URI is {0}", callbackUri.AbsoluteUri);
			var temporaryTokenMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(Utility.OAuthBaseUri, "temporarytoken?allowSession=true"));
			temporaryTokenMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateAuthorizationHeaderValue(Utility.ConsumerToken, Utility.ConsumerSecret, callbackUri.AbsoluteUri));

			using var httpClient = new HttpClient();
			var temporaryToken = await Utility.GetFormValuesAsync(httpClient, temporaryTokenMessage);
			var oauthToken = temporaryToken["oauth_token"];
			var oauthSecret = temporaryToken["oauth_token_secret"];
			var callbackConfirmed = temporaryToken["oauth_callback_confirmed"];
			log.LogDebug("token={0}, secret={1}", oauthToken, oauthSecret);

			lock (s_cache)
				s_cache[oauthToken] = oauthSecret;

			var authUri = new Uri(Utility.OAuthBaseUri, $"authorize?brand=faithlife&oauth_token={Uri.EscapeDataString(oauthToken)}");
			log.LogInformation("Redirecting to {0}", authUri.AbsoluteUri);
			return new RedirectResult(authUri.AbsoluteUri);
		}

		public static string GetSecret(string token)
		{
			lock (s_cache)
				return s_cache[token];
		}

		static readonly Dictionary<string, string> s_cache = new();
	}
}

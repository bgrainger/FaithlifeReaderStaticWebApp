using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Faithlife.OAuth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions
{
	public class SignIn
	{
		[Function("SignIn")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("SignIn");
			log.LogInformation("SignIn HTTP trigger function processing a request.");

			var queryParameters = HttpUtility.ParseQueryString(req.Url.Query);
			var redirectUrl = queryParameters["redirect"];
			if (redirectUrl is null)
				return req.CreateResponse(HttpStatusCode.BadRequest);
			var redirectUri = new Uri(redirectUrl);
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

			var response = req.CreateResponse(HttpStatusCode.Redirect);
			response.Headers.Add("Location", authUri.AbsoluteUri);
			return response;
		}

		public static string GetSecret(string token)
		{
			lock (s_cache)
				return s_cache[token];
		}

		static readonly Dictionary<string, string> s_cache = new();
	}
}

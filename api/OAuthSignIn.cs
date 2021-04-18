using System;
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
	public class OAuthSignIn
	{
		[Function("OAuthSignIn")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("OAuthSignIn");
			log.LogInformation("OAuthSignIn HTTP trigger function processing a request.");

			var queryParameters = HttpUtility.ParseQueryString(req.Url.Query);
			var oauthToken = queryParameters["oauth_token"];
			var oauthVerifier = queryParameters["oauth_verifier"];
			var redirectUri = queryParameters["redirect"];
			log.LogDebug("token={0}, verifier={1}", oauthToken, oauthVerifier);

			var accessCredentialsMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(Utility.OAuthBaseUri, "accesstoken"));
			var auth = OAuthUtility.CreateAuthorizationHeaderValue(Utility.ConsumerToken, Utility.ConsumerSecret, oauthToken, SignIn.GetSecret(oauthToken), oauthVerifier);
			accessCredentialsMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(auth);

			using var httpClient = new HttpClient();
			var accessCredentials = await Utility.GetFormValuesAsync(httpClient, accessCredentialsMessage);
			oauthToken = accessCredentials["oauth_token"];
			var oauthSecret = accessCredentials["oauth_token_secret"];
			log.LogDebug("token={0}, secret={1}", oauthToken, oauthSecret);

			var dataToEncrypt = $"{oauthToken}/{oauthSecret}";
			var encryptedAuth = Encryption.Encrypt(dataToEncrypt);

			var response = req.CreateResponse(HttpStatusCode.Redirect);
			response.Headers.Add("Location", redirectUri);
			response.Cookies.Append("faithlife-reader-auth", Convert.ToBase64String(encryptedAuth));
			return response;
		}
	}
}

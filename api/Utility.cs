using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Faithlife.OAuth;
using Microsoft.Azure.Cosmos;

namespace FaithlifeReader.Functions
{
	internal static class Utility
	{
		public static string ConsumerToken => s_consumerToken ??= Environment.GetEnvironmentVariable("ConsumerToken")!;

		public static string ConsumerSecret => s_consumerSecret ??= Environment.GetEnvironmentVariable("ConsumerSecret")!;

		public static CosmosClient CosmosClient
		{
			get
			{
				if (s_cosmosClient is null)
				{
					s_cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosConnectionString"),
						new CosmosClientOptions()
						{
							SerializerOptions = new CosmosSerializationOptions()
							{
								PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
							}
						});
				}
				return s_cosmosClient;
			}
		}
		
		public static Uri AccountsBaseUri { get; } = new Uri("https://accountsapi.logos.com/v1/");

		public static Uri OAuthBaseUri { get; } = new Uri("https://auth.faithlife.com/v1/");

		public static ReadOnlySpan<byte> SecretKey => (s_secretKey ??= Convert.FromBase64String(Environment.GetEnvironmentVariable("SecretKey")!)).AsSpan();

		public static async Task<NameValueCollection> GetFormValuesAsync(this HttpClient httpClient, HttpRequestMessage request)
		{
			var response = await httpClient.SendAsync(request);
			var content = await response.Content.ReadAsStringAsync();
			return HttpUtility.ParseQueryString(content);
		}

		public static void AddAuthorizationHeader(this HttpRequestMessage request, string accessToken, string accessSecret) =>
			request.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateHmacSha1AuthorizationHeaderValue(request.RequestUri, request.Method.ToString().ToUpperInvariant(), Utility.ConsumerToken, Utility.ConsumerSecret, accessToken, accessSecret));

		public static string Truncate(string value, int length) =>
			value.Length <= length ? value :
			value.LastIndexOf(' ', length) == -1 ? (value.Substring(0, length) + "\u2026") :
			(value[..value.LastIndexOf(' ', length)] + "\u2026");

		static string? s_consumerToken;
		static string? s_consumerSecret;
		static CosmosClient? s_cosmosClient;
		static byte[]? s_secretKey;
	}
}

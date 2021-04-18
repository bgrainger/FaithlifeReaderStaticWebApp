using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FaithlifeReader.Functions
{
	internal static class Utility
	{
		public static string ConsumerToken => s_consumerToken ??= Environment.GetEnvironmentVariable("ConsumerToken")!;

		public static string ConsumerSecret => s_consumerSecret ??= Environment.GetEnvironmentVariable("ConsumerSecret")!;

		public static Uri AccountsBaseUri { get; } = new Uri("https://accountsapi.logos.com/v1/");

		public static Uri OAuthBaseUri { get; } = new Uri("https://auth.faithlife.com/v1/");

		public static ReadOnlySpan<byte> SecretKey => (s_secretKey ??= Convert.FromBase64String(Environment.GetEnvironmentVariable("SecretKey")!)).AsSpan();

		public static IReadOnlyDictionary<string, string> ParseFormValues(string value) =>
			value.Split('&')
				.Select(x => x.Split('=', 2))
				.ToDictionary(x => Uri.UnescapeDataString(x[0]), x => Uri.UnescapeDataString(x[1]));

		public static async Task<IReadOnlyDictionary<string, string>> GetFormValuesAsync(this HttpClient httpClient, HttpRequestMessage request)
		{
			var response = await httpClient.SendAsync(request);
			var content = await response.Content.ReadAsStringAsync();
			return ParseFormValues(content);
		}

		public static string Truncate(string value, int length) => value.Length <= length ? value : (value[..value.LastIndexOf(' ', length)] + "\u2026");

		static string? s_consumerToken;
		static string? s_consumerSecret;
		static byte[]? s_secretKey;
	}
}

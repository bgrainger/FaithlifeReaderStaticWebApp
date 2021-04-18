using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Faithlife.OAuth;
using FaithlifeReader.Functions.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions
{
	public static class FeedItems
	{
		[FunctionName("FeedItems")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("FeedItems HTTP trigger function processing a request.");

			// get user's credentials from the cookie
			var (accessToken, accessSecret) = GetCredentials(req);
			if (accessToken is null)
				return new ForbidResult();

			// get user's ID from AccountServices
			using var httpClient = new HttpClient();
			var getCurrentUserRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(Utility.AccountsBaseUri, "users/me"));
			getCurrentUserRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateHmacSha1AuthorizationHeaderValue(getCurrentUserRequest.RequestUri, "GET", Utility.ConsumerToken, Utility.ConsumerSecret, accessToken, accessSecret));
			var getCurrentUserResponse = await httpClient.SendAsync(getCurrentUserRequest);
			if (!getCurrentUserResponse.IsSuccessStatusCode)
				return new ForbidResult();
			var currentUser = await getCurrentUserResponse.Content.ReadAsAsync<UserDto>();
			if (currentUser.Id <= 0)
				return new ForbidResult();
			log.LogInformation("Current User ID is {0}", currentUser.Id);

			var container = Utility.CosmosClient.GetContainer("reader", "user_data");
			var id = $"UserData:{currentUser.Id}";
			var lastReadDate = req.Query["lastReadDate"].FirstOrDefault();
			if (lastReadDate is null)
			{
				using (var responseMessage = await container.ReadItemStreamAsync(id, new PartitionKey(currentUser.Id.ToString())))
				{
					if (responseMessage.StatusCode != HttpStatusCode.NotFound)
					{
						var userData = await JsonSerializer.DeserializeAsync<UserDataDto>(responseMessage.Content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
						lastReadDate = userData?.LastReadDate;
						log.LogInformation("Loaded lastReadDate={0} from CosmosDB", lastReadDate);
					}
				}
			}
			lastReadDate ??= DateTime.UtcNow.AddDays(-1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

			string? lastPageDate = null;
			var items = new List<UserFeedItem>();

			var found = false;
			while (!found)
			{
				log.LogInformation("Requesting a page of items starting at {0}", lastPageDate);

				var uri = "https://api.faithlife.com/community/v1/newsfeed?sortBy=createdDate&repliesCount=0&count=100";
				if (lastPageDate is not null)
					uri += "&startDateTime=" + Uri.EscapeDataString(lastPageDate);

				var newsfeedRequest = new HttpRequestMessage(HttpMethod.Get, uri);
				newsfeedRequest.Headers.Add("X-CommunityServices-Version", "7");
				newsfeedRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateHmacSha1AuthorizationHeaderValue(newsfeedRequest.RequestUri, "GET", Utility.ConsumerToken, Utility.ConsumerSecret, accessToken, accessSecret));
				var response = await httpClient.SendAsync(newsfeedRequest);
				var newsFeed = await response.Content.ReadAsAsync<NewsFeedDto>();
				foreach (var item in newsFeed!.Items)
				{
					var handler = item.Kind switch
					{
						"calendarItem" => (IFeedItemHandler)new CalendarFeedItemHandler(item),
						"discussionTopic" => new DiscussionTopicFeedItemHandler(item),
						"comment" => new CommentFeedItemHandler(item),
						"groupBulletin" => new GroupBulletinFeedItemHandler(item),
						"newsletter" => new NewsletterFeedItemHandler(item),
						"note" => new NoteFeedItemHandler(item),
						"review" => new ReviewFeedItemHandler(item),
						_ => throw new NotSupportedException($"Can't read details of kind '{item.Kind}' at '{uri}'"),
					};

					lastPageDate = item.PageDate;
					if (string.CompareOrdinal(lastPageDate, lastReadDate) <= 0)
					{
						found = true;
						break;
					}

					items.Add(new UserFeedItem(item.PageDate, handler.GetRelativeDate(), handler.Url, handler.Title, handler.Details));
				}
			}

			items.Reverse();
			return new OkObjectResult(new List<UserFeedItem>(items.Take(10)));
		}

		private static (string AccessToken, string AccessSecret) GetCredentials(HttpRequest request)
		{
			var encodedCookie = request.Cookies["faithlife-reader-auth"];
			if (encodedCookie is null)
				return default;

			byte[] encryptedCookie;
			try
			{
				encryptedCookie =Convert.FromBase64String(encodedCookie);
			}
			catch (FormatException)
			{
				return default;
			}

			var decryptedCookie = Encryption.Decrypt(encryptedCookie);
			if (decryptedCookie is null)
				return default;

			var components = decryptedCookie.Split('/');
			return (components[0], components[1]);
		}
	}
}

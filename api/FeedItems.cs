using System.Net;
using System.Text.Json;
using System.Web;
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
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("FeedItems HTTP trigger function processing a request.");

			// get user's credentials from the cookie
			var (accessToken, accessSecret) = GetCredentials(req, log);
			if (accessToken is null)
				return new StatusCodeResult(403);

			// get user's ID from AccountServices
			using var httpClient = new HttpClient();
			var getCurrentUserRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(Utility.AccountsBaseUri, "users/me"));
			getCurrentUserRequest.AddAuthorizationHeader(accessToken, accessSecret);
			var getCurrentUserResponse = await httpClient.SendAsync(getCurrentUserRequest);
			if (!getCurrentUserResponse.IsSuccessStatusCode)
				return new StatusCodeResult(403);
			var currentUser = await getCurrentUserResponse.Content.ReadAsAsync<UserDto>();
			if (currentUser.Id <= 0)
				return new StatusCodeResult(403);
			log.LogInformation("Current User ID is {0}", currentUser.Id);

			// dispatch the request
			return req.Method switch
			{
				"GET" => await GetNewsFeedAsync(),
				"POST" => await PostNewsFeedAsync(),
				_ => new StatusCodeResult(405),
			};

			async Task<IActionResult> GetNewsFeedAsync()
			{
				// get the last read date for this user from CosmosDB
				var container = Utility.CosmosClient.GetContainer("reader", "user_data");
				var id = $"UserData:{currentUser.Id}";
				var lastReadDate = req.Query["lastReadDate"].FirstOrDefault();
				if (lastReadDate is null)
				{
					using var responseMessage = await container.ReadItemStreamAsync(id, new PartitionKey(currentUser.Id.ToString()));
					if (responseMessage.StatusCode != HttpStatusCode.NotFound)
					{
						var userData = await JsonSerializer.DeserializeAsync<UserDataDto>(responseMessage.Content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
						lastReadDate = userData?.LastReadDate;
						log.LogInformation("Loaded lastReadDate={0} from CosmosDB", lastReadDate);
					}
				}
				lastReadDate ??= DateTime.UtcNow.AddDays(-1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

				string? lastPageDate = null;
				var items = new List<UserFeedItem>();

				// keep retrieving items from CommunityServices until we reach the last read date
				var found = false;
				while (!found)
				{
					log.LogInformation("Requesting a page of items starting at {0}", lastPageDate);

					var uri = "https://api.faithlife.com/community/v1/newsfeed?sortBy=createdDate&repliesCount=0&count=100";
					if (lastPageDate is not null)
						uri += "&startDateTime=" + Uri.EscapeDataString(lastPageDate);

					// make the authenticated request
					var newsfeedRequest = new HttpRequestMessage(HttpMethod.Get, uri);
					newsfeedRequest.Headers.Add("X-CommunityServices-Version", "7");
					newsfeedRequest.AddAuthorizationHeader(accessToken, accessSecret);
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

						items.Add(new(item.PageDate, handler.GetRelativeDate(), handler.Url, handler.Title, handler.Details));
					}
				}

				// return only the last 10 items
				req.HttpContext.Response.Headers["Cache-Control"] = "no-store, max-age=0";
				return new OkObjectResult(new List<UserFeedItem>(items.Take(^10..).Reverse()));
			}

			async Task<IActionResult> PostNewsFeedAsync()
			{
				using var streamReader = new StreamReader(req.Body);
				var body = await streamReader.ReadToEndAsync();
				var lastReadDate = HttpUtility.ParseQueryString(body)["lastReadDate"];

				if (string.IsNullOrEmpty(lastReadDate))
					return new BadRequestObjectResult("Missing 'lastReadDate'");

				log.LogInformation("Storing lastReadDate={0} in CosmosDB", lastReadDate);

				var userData = new UserDataDto
				{
					Id = $"UserData:{currentUser.Id}",
					UserId = currentUser.Id.ToString(),
					LastReadDate = lastReadDate,
				};

				var container = Utility.CosmosClient.GetContainer("reader", "user_data");
				await container.UpsertItemAsync(userData, new PartitionKey(userData.UserId), new ItemRequestOptions
				{
					EnableContentResponseOnWrite = false,
				});

				return new NoContentResult();
			}
		}

		private static (string AccessToken, string AccessSecret) GetCredentials(HttpRequest request, ILogger log)
		{
			var encodedCookie = request.Cookies["faithlife-reader-auth"];
			if (encodedCookie is null)
			{
				log.LogInformation("faithlife-reader-auth cookie is missing");
				return default;
			}

			byte[] encryptedCookie;
			try
			{
				encryptedCookie = Convert.FromBase64String(Uri.UnescapeDataString(encodedCookie));
			}
			catch (FormatException ex)
			{
				log.LogInformation("Couldn't Base64-decode auth cookie: {0}", ex.Message);
				return default;
			}

			var decryptedCookie = Encryption.Decrypt(encryptedCookie);
			if (decryptedCookie is null)
			{
				log.LogInformation("Couldn't decrypt & verify auth cookie");
				return default;
			}

			var components = decryptedCookie.Split('/');
			log.LogInformation("Decrypted access-token={0} from auth cookie", components[0]);
			return (components[0], components[1]);
		}
	}
}

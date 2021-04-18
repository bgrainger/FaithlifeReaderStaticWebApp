using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Faithlife.OAuth;
using FaithlifeReader.Functions.Dtos;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions
{
	public class FeedItems
	{
		private readonly CosmosClient m_cosmosClient;

		public FeedItems(CosmosClient cosmosClient)
		{
			m_cosmosClient = cosmosClient;
		}

		[Function("FeedItems")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
			FunctionContext executionContext)
		{
			var log = executionContext.GetLogger("FeedItems");
			log.LogInformation("FeedItems HTTP trigger function processing a request.");

			// get user's credentials from the cookie
			var (accessToken, accessSecret) = GetCredentials(req, log);
			if (accessToken is null)
				return req.CreateResponse(HttpStatusCode.Forbidden);

			// get user's ID from AccountServices
			using var httpClient = new HttpClient();
			var getCurrentUserRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(Utility.AccountsBaseUri, "users/me"));
			getCurrentUserRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateHmacSha1AuthorizationHeaderValue(getCurrentUserRequest.RequestUri, "GET", Utility.ConsumerToken, Utility.ConsumerSecret, accessToken, accessSecret));
			var getCurrentUserResponse = await httpClient.SendAsync(getCurrentUserRequest);
			if (!getCurrentUserResponse.IsSuccessStatusCode)
				return req.CreateResponse(HttpStatusCode.Forbidden);
			var currentUser = await getCurrentUserResponse.Content.ReadFromJsonAsync<UserDto>();
			if (currentUser?.Id is not > 0)
				return req.CreateResponse(HttpStatusCode.Forbidden);
			log.LogInformation("Current User ID is {0}", currentUser.Id);

			// dispatch the request
			return req.Method switch
			{
				"GET" => await GetNewsFeedAsync(),
				"POST" => await PostNewsFeedAsync(),
				_ => req.CreateResponse(HttpStatusCode.MethodNotAllowed),
			};

			async Task<HttpResponseData> GetNewsFeedAsync()
			{
				// get the last read date for this user from CosmosDB
				var container = m_cosmosClient.GetContainer("reader", "user_data");
				var id = $"UserData:{currentUser.Id}";
				var queryParameters = HttpUtility.ParseQueryString(req.Url.Query);
				var lastReadDate = queryParameters["lastReadDate"];
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
					newsfeedRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(OAuthUtility.CreateHmacSha1AuthorizationHeaderValue(newsfeedRequest.RequestUri, "GET", Utility.ConsumerToken, Utility.ConsumerSecret, accessToken, accessSecret));
					var newsfeedResponse = await httpClient.SendAsync(newsfeedRequest);
					var newsFeed = await newsfeedResponse.Content.ReadFromJsonAsync<NewsFeedDto>();
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

				// return only the last 10 items
				items.Reverse();

				var response = req.CreateResponse(HttpStatusCode.OK);
				response.Headers.Add("Content-Type", "application/json");
				await JsonSerializer.SerializeAsync(response.Body, new List<UserFeedItem>(items.Take(10)), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
				return response;
			}

			async Task<HttpResponseData> PostNewsFeedAsync()
			{
				using var streamReader = new StreamReader(req.Body);
				var body = await streamReader.ReadToEndAsync();
				var bodyValues = Utility.ParseFormValues(body);
				var lastReadDate = bodyValues.GetValueOrDefault("lastReadDate");

				if (string.IsNullOrEmpty(lastReadDate))
					return req.CreateResponse(HttpStatusCode.BadRequest);

				log.LogInformation("Storing lastReadDate={0} in CosmosDB", lastReadDate);

				var userData = new UserDataDto
				{
					Id = $"UserData:{currentUser.Id}",
					UserId = currentUser.Id.ToString(),
					LastReadDate = lastReadDate,
				};

				var container = m_cosmosClient.GetContainer("reader", "user_data");
				try
				{
					await container.CreateItemAsync(userData, new PartitionKey(userData.UserId));
				}
				catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
				{
					await container.ReplaceItemAsync(userData, userData.Id, new PartitionKey(userData.UserId));
				}

				return req.CreateResponse(HttpStatusCode.NoContent);
			}
		}

		private static (string AccessToken, string AccessSecret) GetCredentials(HttpRequestData request, ILogger log)
		{
			// request.Cookies collection is empty (bug?) so parse them manually
			var cookies = new CookieContainer();
			foreach (var cookieHeader in request.Headers.Where(x => x.Key == "Cookie").SelectMany(x => x.Value))
				cookies.SetCookies(request.Url, cookieHeader);

			var encodedCookie = cookies.GetCookies(request.Url).FirstOrDefault(x => x.Name == "faithlife-reader-auth");
			if (encodedCookie is null)
			{
				log.LogInformation("faithlife-reader-auth cookie is missing");
				return default;
			}

			byte[] encryptedCookie;
			try
			{
				encryptedCookie = Convert.FromBase64String(Uri.UnescapeDataString(encodedCookie.Value));
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

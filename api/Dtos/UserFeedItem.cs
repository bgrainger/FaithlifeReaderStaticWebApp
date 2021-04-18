using System;

namespace FaithlifeReader.Functions.Dtos
{
	public sealed class UserFeedItem
	{
		public UserFeedItem(string date, string relativeDate, Uri url, string title, string details)
		{
			Date = date;
			RelativeDate = relativeDate;
			Url = url;
			Title = title;
			Details = details;
		}

		public string Date { get; }
		public string RelativeDate { get; }
		public Uri Url { get; }
		public string Title { get; }
		public string Details { get; }
	}
}

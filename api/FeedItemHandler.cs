using FaithlifeReader.Functions.Dtos;
using static FaithlifeReader.Functions.Utility;

namespace FaithlifeReader.Functions
{
	internal interface IFeedItemHandler
	{
		ItemDto Item { get; }
		Uri Url { get; }
		string Title { get; }
		string Details { get; }

		string GetRelativeDate()
		{
			var date = DateTime.Parse(Item.PageDate).ToUniversalTime();
			var ago = DateTime.UtcNow - date;
			return ago switch
			{
				{ Days: > 1 } => $"{ago.Days} days ago",
				{ Days: 1 } => $"{ago.Days} day ago",
				{ Hours: > 1 } => $"{ago.Hours} hours ago",
				{ Hours: 1 } => $"{ago.Hours} hour ago",
				{ Minutes: > 1 } => $"{ago.Minutes} minutes ago",
				_ => "just now",
			};
		}
	}

	internal readonly record struct CalendarFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/{CalendarItem.Destination.Id}/calendar/view/{CalendarItem.Id}");
		public string Title => $"{CalendarItem.Source.Name} posted an event to {CalendarItem.Destination.Name}";
		public string Details => Truncate(CalendarItem.Title, 200) + (string.IsNullOrEmpty(CalendarItem.Start) ? "" : (" at " + CalendarItem.Start));
		private CalendarItemDto CalendarItem => Item.CalendarItem!;
	}

	internal readonly record struct CommentFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/posts/{Comment.Id}");
		public string Title
		{
			get
			{
				if (Comment.Source.Id != Comment.Destination.Id)
					return $"{Comment.Source.Name} posted to {Comment.Destination.Name}";

				var destination = Comment.MinUserPrivacy switch
				{
					"groupAll" => "co-members of groups",
					"groupManagers" => "group admins",
					"everyone" => "everyone",
					_ => "My Faithlife",
				};
				return $"{Comment.Source.Name} posted to {destination}";
			}
		}

		public string Details
		{
			get
			{
				if (!string.IsNullOrEmpty(Comment.Url) && (Comment.Text == "" || Comment.Text == Comment.Url))
					return Comment.Url;
				return Truncate(Comment.Text, 200);
			}
		}

		private CommentDto Comment => Item.Comment!;
	}

	internal readonly record struct DiscussionTopicFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/{DiscussionTopic.OriginalPost.Destination.Id}/topics/{DiscussionTopic.Id}/latest");
		public string Title => $"{DiscussionTopic.OriginalPost.Source.Name} started a discussion in {DiscussionTopic.OriginalPost.Destination.Name}";
		public string Details => Truncate(DiscussionTopic.Topic, 200);
		private DiscussionTopicDto DiscussionTopic => Item.DiscussionTopic!;
	}

	internal readonly record struct GroupBulletinFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/{Newsletter.Destination.Id}/bulletins/{Newsletter.Id}");
		public string Title => $"{Newsletter.Source.Name} published a bulletin in {Newsletter.Destination.Name}";
		public string Details => Truncate(Newsletter.Title, 120) + (string.IsNullOrEmpty(Newsletter.Subtitle) ? "" : (" / " + Truncate(Newsletter.Subtitle, 60)));
		private NewsletterDto Newsletter => Item.Newsletter!;
	}

	internal readonly record struct NewsletterFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/{Newsletter.Destination.Id}/newsletters/{Newsletter.Id}");
		public string Title => $"{Newsletter.Source.Name} posted a newsletter to {Newsletter.Destination.Name}";
		public string Details => Truncate(Newsletter.Title, 120) + (string.IsNullOrEmpty(Newsletter.Subtitle) ? "" : (" / " + Truncate(Newsletter.Subtitle, 60)));
		private NewsletterDto Newsletter => Item.Newsletter!;
	}

	internal readonly record struct NoteFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/notes/{Note.Id}");
		public string Title => $"{Note.Source.Name} took a note";
		public string Details => Truncate(Note.Text, 200);
		private NoteDto Note => Item.Note!;
	}

	internal readonly record struct ReviewFeedItemHandler(ItemDto Item) : IFeedItemHandler
	{
		public Uri Url => new($"https://beta.faithlife.com/reviews/{Review.Id}");
		public string Title => $"{Review.Source.Name} wrote a review";
		public string Details => $"{Review.Rating} stars on {Review.PageUrl}";
		private ReviewDto Review => Item.Review!;
	}
}

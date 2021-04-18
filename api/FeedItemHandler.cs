using System;
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

	internal readonly struct CalendarFeedItemHandler : IFeedItemHandler
	{
		public CalendarFeedItemHandler(ItemDto item)
		{
			Item = item;
			CalendarItem = item.CalendarItem!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/{CalendarItem.Destination.Id}/calendar/view/{CalendarItem.Id}");
		public string Title => $"{CalendarItem.Source.Name} posted an event to {CalendarItem.Destination.Name}";
		public string Details => Truncate(CalendarItem.Title, 200) + (string.IsNullOrEmpty(CalendarItem.Start) ? "" : (" at " + CalendarItem.Start));
		private CalendarItemDto CalendarItem { get; }
	}

	internal readonly struct CommentFeedItemHandler : IFeedItemHandler
	{
		public CommentFeedItemHandler(ItemDto item)
		{
			Item = item;
			Comment = item.Comment!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/posts/{Comment.Id}");
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

		private CommentDto Comment { get; }
	}

	internal readonly struct DiscussionTopicFeedItemHandler : IFeedItemHandler
	{
		public DiscussionTopicFeedItemHandler(ItemDto item)
		{
			Item = item;
			DiscussionTopic = item.DiscussionTopic!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/{DiscussionTopic.OriginalPost.Destination.Id}/topics/{DiscussionTopic.Id}/latest");
		public string Title => $"{DiscussionTopic.OriginalPost.Source.Name} started a discussion in {DiscussionTopic.OriginalPost.Destination.Name}";
		public string Details => Truncate(DiscussionTopic.Topic, 200);

		private DiscussionTopicDto DiscussionTopic { get; }
	}

	internal readonly struct GroupBulletinFeedItemHandler : IFeedItemHandler
	{
		public GroupBulletinFeedItemHandler(ItemDto item)
		{
			Item = item;
			Newsletter = item.Newsletter!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/{Newsletter.Destination.Id}/bulletins/{Newsletter.Id}");
		public string Title => $"{Newsletter.Source.Name} published a bulletin in {Newsletter.Destination.Name}";
		public string Details => Truncate(Newsletter.Title, 120) + (string.IsNullOrEmpty(Newsletter.Subtitle) ? "" : (" / " + Truncate(Newsletter.Subtitle, 60)));

		private NewsletterDto Newsletter { get; }
	}

	internal readonly struct NewsletterFeedItemHandler : IFeedItemHandler
	{
		public NewsletterFeedItemHandler(ItemDto item)
		{
			Item = item;
			Newsletter = item.Newsletter!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/{Newsletter.Destination.Id}/newsletters/{Newsletter.Id}");
		public string Title => $"{Newsletter.Source.Name} posted a newsletter to {Newsletter.Destination.Name}";
		public string Details => Truncate(Newsletter.Title, 120) + (string.IsNullOrEmpty(Newsletter.Subtitle) ? "" : (" / " + Truncate(Newsletter.Subtitle, 60)));
		private NewsletterDto Newsletter { get; }
	}

	internal readonly struct NoteFeedItemHandler : IFeedItemHandler
	{
		public NoteFeedItemHandler(ItemDto item)
		{
			Item = item;
			Note = item.Note!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/notes/{Note.Id}");
		public string Title => $"{Note.Source.Name} took a note";
		public string Details => Truncate(Note.Text, 200);
		private NoteDto Note { get; }
	}

	internal readonly struct ReviewFeedItemHandler : IFeedItemHandler
	{
		public ReviewFeedItemHandler(ItemDto item)
		{
			Item = item;
			Review = item.Review!;
		}

		public ItemDto Item { get; }
		public Uri Url => new Uri($"https://beta.faithlife.com/reviews/{Review.Id}");
		public string Title => $"{Review.Source.Name} wrote a review";
		public string Details => $"{Review.Rating} stars on {Review.PageUrl}";
		private ReviewDto Review { get; }
	}
}

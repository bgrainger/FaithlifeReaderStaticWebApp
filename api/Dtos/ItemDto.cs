using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class ItemDto
	{
		[AllowNull]
		public string Kind { get; set; }
		[AllowNull]
		public string PageDate { get; set; }
		public CalendarItemDto? CalendarItem { get; set; }
		public CommentDto? Comment { get; set; }
		public DiscussionTopicDto? DiscussionTopic { get; set; }
		public NewsletterDto? Newsletter { get; set; }
		public NoteDto? Note { get; set; }
		public ReviewDto? Review { get; set; }
	}
}

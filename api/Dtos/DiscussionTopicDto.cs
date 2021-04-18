using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class DiscussionTopicDto
	{
		public int Id { get; set; }
		[AllowNull]
		public string Topic { get; set; }
		[AllowNull]
		public DiscussionTopicOriginalPostDto OriginalPost { get; set; }
	}
}

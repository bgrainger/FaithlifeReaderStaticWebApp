using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class CommentDto : ItemDetailsDto
	{
		[AllowNull]
		public AccountDto Destination { get; set; }
		[AllowNull]
		public string Text { get; set; }
		public string? Url { get; set; }
		[AllowNull]
		public string MinUserPrivacy { get; set; }
	}
}

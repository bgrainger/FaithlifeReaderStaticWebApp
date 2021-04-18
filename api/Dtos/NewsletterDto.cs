using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class NewsletterDto : ItemDetailsDto
	{
		[AllowNull]
		public AccountDto Destination { get; set; }
		[AllowNull]
		public string Title { get; set; }
		[AllowNull]
		public string Subtitle { get; set; }
	}
}

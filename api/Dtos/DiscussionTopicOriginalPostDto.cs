using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class DiscussionTopicOriginalPostDto : ItemDetailsDto
	{
		[AllowNull]
		public AccountDto Destination { get; set; }

		[AllowNull]
		public List<DiscussionTopicPostDto> Contents { get; set; }
	}
}

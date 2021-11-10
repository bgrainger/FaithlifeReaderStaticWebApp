namespace FaithlifeReader.Functions.Dtos;

public class DiscussionTopicOriginalPostDto : ItemDetailsDto
{
	[AllowNull]
	public AccountDto Destination { get; set; }

	[AllowNull]
	public List<DiscussionTopicPostDto> Contents { get; set; }
}

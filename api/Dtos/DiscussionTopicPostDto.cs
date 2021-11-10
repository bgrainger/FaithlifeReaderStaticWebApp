namespace FaithlifeReader.Functions.Dtos;

public class DiscussionTopicPostDto
{
	public int Index { get; set; }
	[AllowNull]
	public string Kind { get; set; }
	public string? Text { get; set; }
}

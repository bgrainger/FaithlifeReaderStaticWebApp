namespace FaithlifeReader.Functions.Dtos;

public class ReviewDto : ItemDetailsDto
{
	public int Rating { get; set; }
	[AllowNull]
	public string PageUrl { get; set; }
}

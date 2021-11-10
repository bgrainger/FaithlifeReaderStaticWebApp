namespace FaithlifeReader.Functions.Dtos;

public class CalendarItemDto : ItemDetailsDto
{
	[AllowNull]
	public AccountDto Destination { get; set; }
	public bool AllDay { get; set; }
	public string? End { get; set; }
	[AllowNull]
	public string Location { get; set; }
	public string? Start { get; set; }
	[AllowNull]
	public string Title { get; set; }
}

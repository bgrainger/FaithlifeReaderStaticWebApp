namespace FaithlifeReader.Functions.Dtos;

public class NoteDto : ItemDetailsDto
{
	[AllowNull]
	public AccountDto Destination { get; set; }
	[AllowNull]
	public string Text { get; set; }
}

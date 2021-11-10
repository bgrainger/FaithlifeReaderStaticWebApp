namespace FaithlifeReader.Functions.Dtos;

internal sealed class UserDataDto
{
	[AllowNull]
	public string Id { get; set; }
	[AllowNull]
	public string UserId { get; set; }
	[AllowNull]
	public string LastReadDate { get; set; }
}

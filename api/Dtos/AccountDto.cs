namespace FaithlifeReader.Functions.Dtos;

public class AccountDto
{
	public int Id { get; set; }
	[AllowNull]
	public string Name { get; set; }
	[AllowNull]
	public string Token { get; set; }
	public string? AvatarUrl { get; set; }
	[AllowNull]
	public string Kind { get; set; }
	[AllowNull]
	public string AccountKind { get; set; }
}

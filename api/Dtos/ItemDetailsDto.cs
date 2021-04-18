using System.Diagnostics.CodeAnalysis;

namespace FaithlifeReader.Functions.Dtos
{
	public class ItemDetailsDto
	{
		public int Id { get; set; }
		[AllowNull]
		public AccountDto Source { get; set; }
	}
}

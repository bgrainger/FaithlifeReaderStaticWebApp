using System.Text.Json.Serialization;

namespace FaithlifeReader.Functions.Dtos
{
	internal sealed class UserDto
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }
	}
}

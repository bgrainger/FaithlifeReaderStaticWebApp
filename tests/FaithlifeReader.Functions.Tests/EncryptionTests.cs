using System;
using System.Linq;
using Xunit;

namespace FaithlifeReader.Functions.Tests;

public class EncryptionTests
{
	public EncryptionTests()
	{
		Utility.SetSecretKey(Enumerable.Range(0, 64).Select(x => (byte) x).ToArray());
	}

	[Fact]
	public void Encrypt()
	{
		var encrypted = Encryption.Encrypt("the data");
		Assert.Equal(64, encrypted.Length);
	}

	[Theory]
	[InlineData("")]
	[InlineData("a long input string")]
	[InlineData("a very very very long input string with lots of characters")]
	public void RoundTrip(string input)
	{
		var encrypted = Encryption.Encrypt(input);
		var base64 = Convert.ToBase64String(encrypted);
		var decrypted = Encryption.Decrypt(encrypted);
		Assert.Equal(input, decrypted);
	}

	[Theory]
	[InlineData("3IXVEBe3KJjeYubk5JkPqN0oDElpV1JEMrup8kuH7mCNU2yPb8q8wJp86FEZCmQLj07fd8knDz2iydrPbBfRcg==")]
	[InlineData("qFHL7De2YCUFayfCHIfBkEXUNJjrtLnibF4qJg/Nz8L9DvRqKxvJ3K6WKbXzQn/p0Vv9oSuWdbGGT3owVYXq/Q==")]
	public void Decrypt(string encrypted)
	{
		var decrypted = Encryption.Decrypt(Convert.FromBase64String(encrypted));
		Assert.Equal("the data", decrypted);
	}
}
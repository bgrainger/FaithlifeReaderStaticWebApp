using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FaithlifeReader.Functions
{
	internal static class Encryption
	{
		// Implements AES_CBC_HMAC_SHA2 encryption from Section 5.2.2.1 of https://www.rfc-editor.org/rfc/rfc7518.txt
		public static byte[] Encrypt(string data)
		{
			// split secret into two keys (Step 1)
			var secretKey = Utility.SecretKey;
			var encryptionKey = secretKey[..32].ToArray();
			var macKey = secretKey[^32..].ToArray();

			// encrypt the data (Steps 2-3)
			using var aes = CreateAes(encryptionKey);
			using var encryptor = aes.CreateEncryptor();
			var dataBytes = Encoding.UTF8.GetBytes(data);
			var encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

			// compute MAC (Steps 4-5)
			var mac = ComputeMac(macKey, aes.IV, encryptedData);

			// pack outputs into one array
			var output = new byte[aes.IV.Length + encryptedData.Length + mac.Length];
			aes.IV.CopyTo(output, 0);
			encryptedData.AsSpan().CopyTo(output.AsSpan().Slice(aes.IV.Length));
			mac.AsSpan().CopyTo(output.AsSpan().Slice(aes.IV.Length + encryptedData.Length));
			return output;
		}

		// Implements AES_CBC_HMAC_SHA2 decryption from Section 5.2.2.2 of https://www.rfc-editor.org/rfc/rfc7518.txt
		public static string? Decrypt(byte[] input)
		{
			// unpack outputs from 'Encrypt'
			var iv = input[0..16].ToArray();
			var encryptedData = input[16..^32].ToArray();
			var mac = input[^32..].ToArray();

			// split secret into two keys (Step 1)
			var secretKey = Utility.SecretKey;
			var encryptionKey = secretKey[..32].ToArray();
			var macKey = secretKey[^32..].ToArray();

			// check MAC authenticity (Step 2)
			var computedMac = ComputeMac(macKey, iv, encryptedData);
			if (!computedMac.SequenceEqual(mac))
				return null;

			// decrypt (Steps 3-4)
			using var aes = CreateAes(encryptionKey, iv);
			using var decryptor = aes.CreateDecryptor();
			var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
			return Encoding.UTF8.GetString(decryptedData);
		}

		private static byte[] ComputeMac(ReadOnlySpan<byte> macKey, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> encryptedData)
		{
			Span<byte> inputData = stackalloc byte[iv.Length + encryptedData.Length + 8];
			iv.CopyTo(inputData);
			encryptedData.CopyTo(inputData.Slice(iv.Length));
			return HMACSHA256.HashData(macKey, inputData);
		}

		private static Aes CreateAes(byte[] key, byte[]? iv = null)
		{
			var aes = Aes.Create();
			aes.Key = key;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			if (iv is null)
				aes.GenerateIV();
			else
				aes.IV = iv;
			return aes;
		}
	}
}

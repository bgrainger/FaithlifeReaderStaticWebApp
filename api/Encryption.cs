using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FaithlifeReader.Functions
{
	internal static class Encryption
	{
		public static byte[] Encrypt(string data)
		{
			var secretKey = Utility.SecretKey;
			var encryptionKey = secretKey[..32].ToArray();
			var macKey = secretKey[^32..].ToArray();

			using var aes = Aes.Create();
			aes.GenerateIV();
			aes.Key = encryptionKey;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.Zeros;
			using var encryptor = aes.CreateEncryptor();
			var dataBytes = Encoding.UTF8.GetBytes(data);
			var encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

			var mac = ComputeMac(macKey, aes.IV, encryptedData);

			var output = new byte[aes.IV.Length + encryptedData.Length + mac.Length];
			aes.IV.CopyTo(output, 0);
			encryptedData.AsSpan().CopyTo(output.AsSpan().Slice(aes.IV.Length));
			mac.AsSpan().CopyTo(output.AsSpan().Slice(aes.IV.Length + encryptedData.Length));
			return output;
		}

		public static string? Decrypt(byte[] input)
		{
			var iv = input[0..16].ToArray();
			var encryptedData = input[16..^32].ToArray();
			var mac = input[^32..].ToArray();

			var secretKey = Utility.SecretKey;
			var encryptionKey = secretKey[..32].ToArray();
			var macKey = secretKey[^32..].ToArray();

			var computedMac = ComputeMac(macKey, iv, encryptedData);

			if (!computedMac.SequenceEqual(mac))
				return null;

			using var aes = Aes.Create();
			aes.IV = iv;
			aes.Key = encryptionKey;
			aes.Mode = CipherMode.ECB;
			aes.Padding = PaddingMode.Zeros;
			using var decryptor = aes.CreateDecryptor();
			var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
			return Encoding.UTF8.GetString(decryptedData).TrimEnd('\0');
		}

		private static byte[] ComputeMac(byte[] macKey, byte[] iv, byte[] encryptedData)
		{
			using var hmac = new HMACSHA256(macKey);
			var inputData = new List<byte>();
			inputData.AddRange(iv);
			inputData.AddRange(encryptedData);
			inputData.AddRange(BitConverter.GetBytes((long) iv.Length * 8));
			inputData.AddRange(BitConverter.GetBytes(0L));
			hmac.TransformFinalBlock(inputData.ToArray(), 0, inputData.Count);
			return hmac.Hash!;
		}
	}
}

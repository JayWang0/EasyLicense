using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasyLicense.Lib
{
	public class CryptoHelper
	{
		private const int BufferSize = 1024;
		private readonly ICryptoTransform decryptor;
		private readonly ICryptoTransform encryptor;

		public CryptoHelper(string algorithmName, string key)
		{
			var provider = SymmetricAlgorithm.Create(algorithmName);
			provider.Key = Encoding.UTF8.GetBytes(key);
			provider.IV = new byte[] {0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF};

			encryptor = provider.CreateEncryptor();
			decryptor = provider.CreateDecryptor();
		}

		public CryptoHelper(string key)
			: this("TripleDES", key)
		{
		}

		public static string Decrypt(string encryptedText, string key)
		{
			var helper = new CryptoHelper(key);
			return helper.Decrypt(encryptedText);
		}

		public static string Encrypt(string clearText, string key)
		{
			var helper = new CryptoHelper(key);
			return helper.Encrypt(clearText);
		}

		public string Decrypt(string encryptedText)
		{
			var encryptedBuffer = Convert.FromBase64String(encryptedText);
			Stream encryptedStream = new MemoryStream(encryptedBuffer);

			var clearStream = new MemoryStream();
			var cryptoStream =
				new CryptoStream(encryptedStream, decryptor, CryptoStreamMode.Read);

			var bytesRead = 0;
			var buffer = new byte[BufferSize];

			do
			{
				bytesRead = cryptoStream.Read(buffer, 0, BufferSize);
				clearStream.Write(buffer, 0, bytesRead);
			} while (bytesRead > 0);

			buffer = clearStream.GetBuffer();
			var clearText =
				Encoding.UTF8.GetString(buffer, 0, (int) clearStream.Length);

			return clearText;
		}

		public string Encrypt(string clearText)
		{
			var clearBuffer = Encoding.UTF8.GetBytes(clearText);
			var clearStream = new MemoryStream(clearBuffer);

			var encryptedStream = new MemoryStream();

			var cryptoStream =
				new CryptoStream(encryptedStream, encryptor, CryptoStreamMode.Write);

			var bytesRead = 0;
			var buffer = new byte[BufferSize];
			do
			{
				bytesRead = clearStream.Read(buffer, 0, BufferSize);
				cryptoStream.Write(buffer, 0, bytesRead);
			} while (bytesRead > 0);

			cryptoStream.FlushFinalBlock();

			buffer = encryptedStream.ToArray();
			var encryptedText = Convert.ToBase64String(buffer);
			return encryptedText;
		}
	}
}
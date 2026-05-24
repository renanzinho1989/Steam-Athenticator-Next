using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SteamAuthenticator.Next.Services.Vault;

internal static class VaultCrypto
{
	private const string CurrentFormatPrefix = "v2:";

	private const int KeySizeBytes = 32;

	private const int ModernIterations = 210000;

	private const int LegacyIterations = 50000;

	private const int SaltSize = 16;

	private const int ModernNonceSize = 12;

	private const int TagSize = 16;

	public static string GetRandomSalt()
	{
		byte[] array = new byte[16];
		RandomNumberGenerator.Fill(array);
		return Convert.ToBase64String(array);
	}

	public static string GetInitializationVector()
	{
		byte[] array = new byte[12];
		RandomNumberGenerator.Fill(array);
		return Convert.ToBase64String(array);
	}

	public static string Encrypt(string password, string salt, string iv, string plainText)
	{
		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentException("A senha do cofre nao pode ficar vazia.", "password");
		}
		if (string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(iv))
		{
			throw new ArgumentException("O material de criptografia do cofre ficou invalido.");
		}
		byte[] key = DeriveKey(password, salt, 210000, HashAlgorithmName.SHA256);
		byte[] array = Convert.FromBase64String(iv);
		if (array.Length != 12)
		{
			throw new ArgumentException("O vetor de inicializacao do cofre ficou invalido.", "iv");
		}
		byte[] bytes = Encoding.UTF8.GetBytes(plainText);
		byte[] array2 = new byte[bytes.Length];
		byte[] array3 = new byte[16];
		using AesGcm aesGcm = new AesGcm(key, 16);
		aesGcm.Encrypt(array, bytes, array2, array3);
		byte[] array4 = new byte[16 + array2.Length];
		Buffer.BlockCopy(array3, 0, array4, 0, 16);
		Buffer.BlockCopy(array2, 0, array4, 16, array2.Length);
		return "v2:" + Convert.ToBase64String(array4);
	}

	public static string? Decrypt(string password, string? salt, string? iv, string encryptedData)
	{
		if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(iv))
		{
			return null;
		}
		if (encryptedData.StartsWith("v2:", StringComparison.Ordinal))
		{
			int length = "v2:".Length;
			return DecryptModern(password, salt, iv, encryptedData.Substring(length, encryptedData.Length - length));
		}
		return DecryptLegacy(password, salt, iv, encryptedData);
	}

	private static string? DecryptModern(string password, string salt, string iv, string encryptedData)
	{
		byte[] key = DeriveKey(password, salt, 210000, HashAlgorithmName.SHA256);
		byte[] array = Convert.FromBase64String(iv);
		byte[] array2 = Convert.FromBase64String(encryptedData);
		if (array.Length != 12 || array2.Length < 16)
		{
			return null;
		}
		byte[] subArray = array2[..16];
		byte[] subArray2 = array2[16..];
		byte[] array3 = new byte[subArray2.Length];
		try
		{
			using AesGcm aesGcm = new AesGcm(key, 16);
			aesGcm.Decrypt(array, subArray2, subArray, array3);
			return Encoding.UTF8.GetString(array3);
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	private static string? DecryptLegacy(string password, string salt, string iv, string encryptedData)
	{
		byte[] key = DeriveKey(password, salt, 50000, HashAlgorithmName.SHA1);
		byte[] buffer = Convert.FromBase64String(encryptedData);
		try
		{
			using Aes aes = Aes.Create();
			aes.Key = key;
			aes.IV = Convert.FromBase64String(iv);
			aes.Padding = PaddingMode.PKCS7;
			aes.Mode = CipherMode.CBC;
			using MemoryStream stream = new MemoryStream(buffer);
			using CryptoStream stream2 = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
			using StreamReader streamReader = new StreamReader(stream2);
			return streamReader.ReadToEnd();
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	private static byte[] DeriveKey(string password, string salt, int iterations, HashAlgorithmName hashAlgorithm)
	{
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), iterations, hashAlgorithm);
		return rfc2898DeriveBytes.GetBytes(32);
	}
}

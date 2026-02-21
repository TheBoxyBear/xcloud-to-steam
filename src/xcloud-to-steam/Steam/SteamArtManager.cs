using System.Security.Cryptography;
using System.Text;

namespace xCloudToSteam.Steam;

public static class SteamArtManager
{
	private const string DbKeyPath = ".grid";
	private static readonly byte[] s_Salt = [.. "Iav9OHUQhHMwG/mD/m+3UZHVWSxre6jB"u8];

	public static string DbKey
	{
		get
		{
			if (!string.IsNullOrEmpty(field))
				return field;

			if (!File.Exists(DbKeyPath))
				return string.Empty;

			try
			{
				using Aes aes = Aes.Create();
				using FileStream fs = File.OpenRead(DbKeyPath);

				byte[] iv = new byte[aes.IV.Length];
				fs.ReadExactly(iv);

				aes.Key = DeriveEncryptionKey();
				aes.IV = iv;

				using ICryptoTransform decryptor = aes.CreateDecryptor();
				using CryptoStream cs = new(fs, decryptor, CryptoStreamMode.Read);
				using StreamReader sr = new(cs, Encoding.UTF8);

				field = sr.ReadToEnd();
				return field;
			}
			catch
			{
				return string.Empty;
			}
		}
		set
		{
			byte[] keyBytes = DeriveEncryptionKey();

			using Aes aes = Aes.Create();
			aes.Key = keyBytes;
			aes.GenerateIV();

			using FileStream fs = File.Create(DbKeyPath);
			fs.Write(aes.IV, 0, aes.IV.Length);

			using ICryptoTransform encryptor = aes.CreateEncryptor();
			using CryptoStream cs = new(fs, encryptor, CryptoStreamMode.Write);

			byte[] plainBytes = Encoding.UTF8.GetBytes(value);
			cs.Write(plainBytes, 0, plainBytes.Length);
			cs.FlushFinalBlock();

			field = value;
		}
	}

	private static byte[] DeriveEncryptionKey()
	{
		string machineKey = $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion}";
		return Rfc2898DeriveBytes.Pbkdf2(machineKey, s_Salt, 100000, HashAlgorithmName.SHA256, 32);
	}
}

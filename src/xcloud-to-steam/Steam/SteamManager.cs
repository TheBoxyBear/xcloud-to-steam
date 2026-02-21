using System.Runtime.InteropServices;

using ValveKeyValue;

using xCloudToSteam.Steam.Model;

using static xCloudToSteam.Steam.SteamKVHelper;

namespace xCloudToSteam.Steam;

public enum ImageType : byte
{
	Cover,
	Banner,
	Hero,
	Logo,
	Icon
}

public static class SteamManager
{
	public static string SteamPath { get; }

	static SteamManager()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			SteamPath = @"C:\Program Files (x86)\Steam";
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			SteamPath = "~/Library/Application Support/Steam";
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			SteamPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.local/share/Steam";
		else
			throw new PlatformNotSupportedException();
	}

	public static IEnumerable<SteamUser> GetUsers()
	{
		string path = Path.Combine(SteamPath, "config", "loginusers.vdf");

		KVDocument doc;

		using (FileStream fs = File.OpenRead(path))
			doc = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(fs);

		if (doc.Name != "users")
			throw new InvalidDataException("Unexpected root key, expected 'users'");

		foreach (KVObject entry in doc)
		{
			if (entry.Value.ValueType != KVValueType.Collection)
				continue;

			if (!ulong.TryParse(entry.Name, out ulong steamId))
				throw new InvalidDataException($"Expected steam id {entry.Name} to be uint64");

			yield return new SteamUser
			{
				// See https://developer.valvesoftware.com/wiki/SteamID
				AccountId    = (uint)(steamId & uint.MaxValue),
				AccountName  = GetString(entry["AccountName"]),
				PersonaName  = GetString(entry["PersonalName"]),
				MostRecent   = GetInt(entry["MostRecent"])! == 1
			};
		}
	}

	public static string GetGridImagePath(SteamUserSession session, SteamId appId, ImageType type)
	{
		string fileNameSuffix = type switch
		{
			ImageType.Banner => string.Empty,
			ImageType.Cover  => "p",
			ImageType.Hero   => "_hero",
			ImageType.Logo   => "_logo",
			ImageType.Icon   => "_icon",
		};

		return Path.Combine(session.GridDir, appId + fileNameSuffix);
	}
}

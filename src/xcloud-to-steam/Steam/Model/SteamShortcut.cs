using System.Runtime.CompilerServices;
using System.Text;

using ValveKeyValue;

using static xCloudToSteam.Steam.SteamKVHelper;

namespace xCloudToSteam.Steam.Model;

public record class SteamShortcut
{
	private bool
		m_appNameLowercase,
		m_exeLowercase;

	public SteamId AppId { get; set; }
	public string AppName { get; set; } = string.Empty;
	public string Exe { get; set; } = string.Empty;
	public string StartDir { get; set; } = string.Empty;
	public string Icon { get; set; } = string.Empty;
	public string ShortcutPath { get; set; } = string.Empty;
	public string LaunchOptions { get; set; } = string.Empty;
	public bool IsHidden { get; set; }
	public bool AllowDesktopConfig { get; set; }
	public bool AllowOverlay { get; set; }
	public bool OpenVR { get; set; }
	public bool Devkit { get; set; }
	public string DevkitGameID { get; set; } = string.Empty;
	public SteamId DevkitOverrideAppID { get; set; }
	public DateTime LastPlayTime { get; set; }
	public string FlatpakAppID { get; set; } = string.Empty;
	public string SortAs { get; set; } = string.Empty;
	public List<string> Tags { get; set; } = [];

	public static SteamShortcut Create(string appName, string exe)
	{
		SteamShortcut shortcut = new()
		{
			AppName = appName,
			Exe = exe
		};

		shortcut.AppId = GenerateAppID(shortcut);
		return shortcut;
	}

	public static List<SteamShortcut> Read(SteamUserSession session)
	{
		if (!File.Exists(session.ShortcutsPath))
			return [];

		KVDocument doc;

		using (FileStream fs = File.OpenRead(session.ShortcutsPath))
			doc = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(fs);

		if (doc.Name != "shortcuts")
			throw new InvalidDataException("Unexpected root key, expected 'shortcuts'");

		List<SteamShortcut> shortcuts = [];

		foreach (KVObject? entry in doc)
		{
			if (entry is null)
				break;

			shortcuts.Add(Deserialize(entry));
		}

		return shortcuts;
	}

	public static async Task Write(SteamUserSession session, params IList<SteamShortcut> shortcuts)
	{
		KVObject[] entries = new KVObject[shortcuts.Count];

		for (int i = 0; i < shortcuts.Count; i++)
			entries[i] = Serialize(shortcuts[i], i);

		KVSerializer serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);
		KVObject obj = new("shortcuts", entries);

		string tempPath = Path.GetTempFileName();

		using (FileStream fs = File.OpenWrite(tempPath))
			serializer.Serialize(fs, obj);

		File.Move(tempPath, session.ShortcutsPath, overwrite: true);
		File.Delete(tempPath);
	}

	private static SteamShortcut Deserialize(KVObject dict)
	{
		SteamShortcut shortcut = new()
		{
			AppName				= GetString(dict, "AppName"),
			Exe					= GetString(dict, "Exe"),
			AppId               = GetId(dict, "appid"),
			StartDir            = GetString(dict, "StartDir"),
			Icon                = GetString(dict, "icon"),
			ShortcutPath        = GetString(dict, "ShortcutPath"),
			LaunchOptions       = GetString(dict, "LaunchOptions"),
			IsHidden            = GetBool(dict, "IsHidden"),
			AllowDesktopConfig  = GetBool(dict, "AllowDesktopConfig"),
			AllowOverlay        = GetBool(dict, "AllowOverlay"),
			OpenVR              = GetBool(dict, "OpenVR"),
			Devkit              = GetBool(dict, "Devkit"),
			DevkitGameID        = GetString(dict, "DevkitGameID"),
			DevkitOverrideAppID = GetId(dict, "DevkitOverrideAppID"),
			LastPlayTime        = GetDateTime(dict, "LastPlayTime"),
			FlatpakAppID        = GetString(dict, "FlatpakAppID"),
			SortAs              = GetString(dict, "sortas")
		};

		// Steam Rom Manager uses `appname` and `exe` instead of `AppName` and `Exe`. Both are supported by Steam, but reading the wrong key will lose the app name
		// Issue reported to Steam Rom Manager: https://github.com/SteamGridDB/steam-rom-manager/issues/793
		if (shortcut.AppName == string.Empty)
		{
			shortcut.m_appNameLowercase = true;
			shortcut.AppName = GetString(dict, "appname");
		}
		if (shortcut.Exe == string.Empty)
		{
			shortcut.m_exeLowercase = true;
			shortcut.Exe = GetString(dict, "exe");
		}

		KVValue? tagsValue = dict["tags"];

		if (tagsValue is null)
			throw new InvalidDataException("Missing entry `tags`");

		if (tagsValue?.ValueType != KVValueType.Collection)
			throw new InvalidDataException("Expected tags to be an array");

		foreach (KVObject tag in (IEnumerable<KVObject>)tagsValue)
		{
			if (tag.Value.ValueType != KVValueType.String)
				throw new Exception("Expected tags to be an array of strings");

			shortcut.Tags.Add((string)tag.Value);
		}

		return shortcut;
	}

	private static KVObject Serialize(SteamShortcut shortcut, int index)
	{
		KVObject[] tags = new KVObject[shortcut.Tags.Count];

		for (int i = 0; i < shortcut.Tags.Count; i++)
			tags[i] = new(i.ToString(), shortcut.Tags[i]);

		KVObject[] dict =
		[
			new("AppName", shortcut.AppName),
			new("appid", (int)shortcut.AppId),
			new("Exe", shortcut.Exe),
			new("StartDir", shortcut.StartDir),
			new("Icon", shortcut.Icon),
			new("ShortcutPath", shortcut.ShortcutPath),
			new("LaunchOptions", shortcut.LaunchOptions),
			new("IsHidden", shortcut.IsHidden),
			new("AllowDesktopConfig", shortcut.AllowDesktopConfig),
			new("AllowOverlay", shortcut.AllowOverlay),
			new("OpenVR", shortcut.OpenVR),
			new("Devkit", shortcut.Devkit),
			new("DevkitGameID", shortcut.DevkitGameID),
			new("DevkitOverrideAppID", (int)shortcut.DevkitOverrideAppID),
			new("LastPlayTime", (int)new DateTimeOffset(shortcut.LastPlayTime).ToUnixTimeSeconds()),
			new("FlatpakAppID", shortcut.FlatpakAppID),
			new("sortas", shortcut.SortAs),
			new("tags", tags)
		];

		return new(index.ToString(), dict);
	}

	// Based on https://developer.valvesoftware.com/w/index.php?title=Steam_Library_Shortcuts
	private static uint ComputeCRC32(byte[] bytes)
	{
		const uint Polynomial = 0xEDB88320;

		Span<uint> table = stackalloc uint[256];

		for (uint i = 0; i < 256; i++)
		{
			uint temp = i;

			for (int j = 0; j < 8; j++)
				temp = (temp & 1) == 1 ? (Polynomial ^ (temp >> 1)) : (temp >> 1);

			table[(int)i] = temp;
		}

		uint crc = 0xFFFFFFFF;

		foreach (byte b in bytes)
		{
			byte index = (byte)((crc & 0xFF) ^ b);
			crc = (crc >> 8) ^ table[index];
		}

		return ~crc;
	}

	public static SteamId GenerateAppID(SteamShortcut shortcut)
	{
		string combined = shortcut.AppName + shortcut.Exe + '\0';

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		byte[] data = Encoding.GetEncoding("Windows-1252").GetBytes(combined);

		uint
			crc = ComputeCRC32(data),
			result = crc | 0x80000000;

		return (SteamId)Unsafe.As<uint, int>(ref result);
	}
}

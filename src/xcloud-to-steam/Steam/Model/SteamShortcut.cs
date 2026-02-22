using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

using ValveKeyValue;

using static xCloudToSteam.Steam.SteamKVHelper;

namespace xCloudToSteam.Steam.Model;

public record class SteamShortcut
{
	private static readonly List<string> s_defaultKeys =
	[
		"AppName",
		"Exe",
		"appid",
		"StartDir",
		"icon",
		"ShortcutPath",
		"LaunchOptions",
		"IsHidden",
		"AllowDesktopConfig",
		"AllowOverlay",
		"OpenVR",
		"Devkit",
		"DevkitGameID",
		"DevkitOverrideAppID",
		"LastPlayTime",
		"FlatpakAppID",
		"SortAs",
		"tags"
	];

	private List<string> Keys { get; init; } = [];

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
			Exe     = exe,
			Keys = s_defaultKeys
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
		SteamShortcut shortcut = new();

		foreach (KVObject entry in dict)
		{
			string key = entry.Name;

			switch (key.ToLower())
			{
				case "appname":
					shortcut.AppName = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "exe":
					shortcut.Exe = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "appid":
					shortcut.AppId = GetId(entry.Value) ?? ThrowInvalidDataException<SteamId>(key);
					break;
				case "startdir":
					shortcut.StartDir = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "icon":
					shortcut.Icon = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "shortcutpath":
					shortcut.ShortcutPath = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "launchoptions":
					shortcut.LaunchOptions = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "ishidden":
					shortcut.IsHidden = GetBool(entry.Value) ?? ThrowInvalidDataException<bool>(key);
					break;
				case "allowdesktopconfig":
					shortcut.AllowDesktopConfig = GetBool(entry.Value) ?? ThrowInvalidDataException<bool>(key);
					break;
				case "allowoverlay":
					shortcut.AllowOverlay = GetBool(entry.Value) ?? ThrowInvalidDataException<bool>(key);
					break;
				case "openvr":
					shortcut.OpenVR = GetBool(entry.Value) ?? ThrowInvalidDataException<bool>(key);
					break;
				case "devkit":
					shortcut.Devkit = GetBool(entry.Value) ?? ThrowInvalidDataException<bool>(key);
					break;
				case "devkitgameid":
					shortcut.DevkitGameID = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "devkitoverrideappid":
					shortcut.DevkitOverrideAppID = GetId(entry.Value) ?? ThrowInvalidDataException<SteamId>(key);
					break;
				case "lastplaytime":
					shortcut.LastPlayTime = GetDateTime(entry.Value) ?? ThrowInvalidDataException<DateTime>(key);
					break;
				case "flatpakappid":
					shortcut.FlatpakAppID = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "sortas":
					shortcut.SortAs = GetString(entry.Value) ?? ThrowInvalidDataException<string>(key);
					break;
				case "tags":
					if (entry.Value.ValueType != KVValueType.Collection)
						throw new InvalidDataException("Expected tags to be an array");

					foreach (KVObject tag in entry)
					{
						if (tag.Value.ValueType != KVValueType.String)
							ThrowInvalidDataException<string[]>(key);

						shortcut.Tags.Add((string)tag.Value);
					}
					break;
				default:
					continue;
			}

			shortcut.Keys.Add(key);
		}

		return shortcut;

		[DoesNotReturn]
		static T ThrowInvalidDataException<T>(string key)
			=> throw new InvalidDataException($"Expected {key} to be of type {typeof(T).Name}.");
	}

	private static KVObject Serialize(SteamShortcut shortcut, int index)
	{
		KVObject[] dict = new KVObject[shortcut.Keys.Count];

		for (int i = 0; i < shortcut.Keys.Count; i++)
		{
			string key = shortcut.Keys[i];

			switch (key.ToLower())
			{
				case "appname":
					dict[i] = new(key, shortcut.AppName);
					break;
				case "exe":
					dict[i] = new(key, shortcut.Exe);
					break;
				case "appid":
					dict[i] = new(key, (int)shortcut.AppId);
					break;
				case "startdir":
					dict[i] = new(key, shortcut.StartDir);
					break;
				case "icon":
					dict[i] = new(key, shortcut.Icon);
					break;
				case "shortcutpath":
					dict[i] = new(key, shortcut.ShortcutPath);
					break;
				case "launchoptions":
					dict[i] = new(key, shortcut.LaunchOptions);
					break;
				case "ishidden":
					dict[i] = new(key, shortcut.IsHidden);
					break;
				case "allowdesktopconfig":
					dict[i] = new(key, shortcut.AllowDesktopConfig);
					break;
				case "allowoverlay":
					dict[i] = new(key, shortcut.AllowOverlay);
					break;
				case "openvr":
					dict[i] = new(key, shortcut.OpenVR);
					break;
				case "devkit":
					dict[i] = new(key, shortcut.Devkit);
					break;
				case "devkitgameid":
					dict[i] = new(key, shortcut.DevkitGameID);
					break;
				case "devkitoverrideappid":
					dict[i] = new(key, (int)shortcut.DevkitOverrideAppID);
					break;
				case "lastplaytime":
					dict[i] = new(key, (int)new DateTimeOffset(shortcut.LastPlayTime).ToUnixTimeSeconds());
					break;
				case "flatpakappid":
					dict[i] = new(key, shortcut.FlatpakAppID);
					break;
				case "sortas":
					dict[i] = new(key, shortcut.SortAs);
					break;
				case "tags":
					KVObject[] tags = new KVObject[shortcut.Tags.Count];

					for (int tagIndex = 0; tagIndex < shortcut.Tags.Count; tagIndex++)
						tags[tagIndex] = new(tagIndex.ToString(), shortcut.Tags[tagIndex]);

					dict[i] = new(key, tags);
					break;
			}
		}

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

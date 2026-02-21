using ValveKeyValue;

using xCloudToSteam.Steam.Model;

namespace xCloudToSteam.Steam;

public static class SteamKVHelper
{
	public static string GetString(KVObject dict, string key)
	{
		KVValue value = dict[key];

		if (value is null)
			return string.Empty;

		return GetString(value, key);
	}

	public static string GetString(KVValue value, string descriptor)
	{
		if (value.ValueType is not KVValueType.String)
			throw new InvalidDataException($"Expected {descriptor} to be a string");

		return (string)value;
	}

	public static int GetInt(KVObject dict, string key)
	{
		KVValue? value = dict[key];

		if (value is null)
			return 0;

		if (value.ValueType is not KVValueType.Int32)
			throw new InvalidDataException($"Expected {key} to be an int");

		return (int)value;
	}

	public static bool GetBool(KVObject dict, string key)
		=> GetInt(dict, key) switch
		{
			0 => false,
			1 => true,
			_ => throw new InvalidDataException($"Expected {key} to be 0 or 1 for bool value"),
		};

	public static SteamId GetId(KVObject dict, string key)
		=> (SteamId)GetInt(dict, key);

	public static DateTime GetDateTime(KVObject dict, string key)
		=> DateTimeOffset.FromUnixTimeSeconds(GetInt(dict, key)).DateTime;
}

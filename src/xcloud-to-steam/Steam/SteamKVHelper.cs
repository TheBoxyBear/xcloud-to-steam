using ValveKeyValue;

using xCloudToSteam.Steam.Model;

namespace xCloudToSteam.Steam;

public static class SteamKVHelper
{
	public static string? GetString(KVValue value)
		=> value.ValueType is KVValueType.String ? (string)value : null;

	public static int? GetInt(KVValue value)
		=> value.ValueType is KVValueType.Int32 ? (int)value : null;

	public static bool? GetBool(KVValue value)
		=> GetInt(value) switch
		{
			0 => false,
			1 => true,
			_ => null
		};

	public static SteamId? GetId(KVValue value)
		=> (SteamId?)GetInt(value);

	public static DateTime? GetDateTime(KVValue value)
	{
		int? intValue = GetInt(value);
		return intValue is null ? null : DateTimeOffset.FromUnixTimeSeconds(intValue.Value).DateTime;
	}
}

namespace xCloudToSteam.Core.Config;

public class ShortcutConfig
{
	public Dictionary<string, ShortcutConfigProfile> Windows { get; init; }
	public Dictionary<string, ShortcutConfigProfile> MacOS { get; init; }
	public Dictionary<string, ShortcutConfigProfile> Linux { get; init; }

	public Dictionary<string , ShortcutConfigProfile> GetProfilesForCurrentOS()
	{
		if (OperatingSystem.IsWindows())
			return Windows;
		else if (OperatingSystem.IsMacOS())
			return MacOS;
		else if (OperatingSystem.IsLinux())
			return Linux;
		else
			throw new PlatformNotSupportedException();
	}
}

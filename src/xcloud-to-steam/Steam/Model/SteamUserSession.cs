namespace xCloudToSteam.Steam.Model;

public class SteamUserSession
{
	public SteamUser User { get; }

	public string ConfigDir { get; }

	public string ShortcutsPath { get; }

	public string GridDir { get; }

	public SteamUserSession(SteamUser user)
	{
		User          = user;
		ConfigDir     = Path.Combine(SteamManager.SteamPath, "userdata", user.AccountId.ToString(), "config");
		ShortcutsPath = Path.Combine(ConfigDir, "shortcuts.vdf");
		GridDir       = Path.Combine(ConfigDir, "grid");
	}
}

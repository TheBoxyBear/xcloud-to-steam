namespace xCloudToSteam.Core.Config;

public class ShortcutConfigProfile
{
	public required string AppName { get; init; }
	public required string Exe { get; init; }
	public required string WorkingDir { get; init; }
	public required string Args { get; init; }
}

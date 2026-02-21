namespace xCloudToSteam.Steam.Model;

public class SteamUser
{
	public required uint AccountId { get; init; }
	public required string AccountName { get; init; }
	public required string PersonaName { get; init; }
	public required bool MostRecent { get; init; }
}

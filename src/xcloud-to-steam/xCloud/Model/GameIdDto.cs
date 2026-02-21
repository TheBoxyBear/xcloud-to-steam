using System.Text.Json.Serialization;

namespace xCloudToSteam.xCloud.Model;

public readonly struct GameIdDto
{
	[JsonPropertyName("id")]
	public string Id { get; init; }
}

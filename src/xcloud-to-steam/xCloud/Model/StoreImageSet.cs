using System.Text.Json.Serialization;

using xCloudToSteam.xCloud.Converters;

namespace xCloudToSteam.xCloud.Model;

public class StoreImageSet
{
	[JsonPropertyName("boxArt")]
	[JsonConverter(typeof(UrlConverter))]
	public string BoxArt { get; init; }

	[JsonPropertyName("poster")]
	[JsonConverter(typeof(UrlConverter))]
	public string Poster { get; init; }

	[JsonPropertyName("superHeroArt")]
	[JsonConverter(typeof(UrlConverter))]
	public string SuperHeroArt { get; init; }
}

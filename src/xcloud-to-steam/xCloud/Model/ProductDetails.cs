using System.Text.Json.Serialization;

using xCloudToSteam.xCloud.Converters;

namespace xCloudToSteam.xCloud.Model;

public class ProductDetails
{
	public required string ProductTitle { get; init; }

	public string? PublisherName { get; init; }

	public string? XCloudTitleId { get; init; }

	public required string StoreId { get; init; }

	[JsonPropertyName("Image_Tile")]
	[JsonConverter(typeof(UrlConverter))]
	public string? ImageTile { get; init; }

	[JsonPropertyName("Image_Poster")]
	[JsonConverter(typeof(UrlConverter))]
	public string? ImagePoster { get; init; }
}

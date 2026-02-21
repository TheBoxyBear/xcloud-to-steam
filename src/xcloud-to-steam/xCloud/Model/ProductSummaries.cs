using System.Text.Json.Serialization;

namespace xCloudToSteam.xCloud.Model;

public class ProductSummaries
{
	[JsonPropertyName("images")]
	public StoreImageSet Images { get; init; }
}

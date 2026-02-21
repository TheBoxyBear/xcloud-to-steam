using System.Text.Json.Serialization;

namespace xCloudToSteam.xCloud.Model;

public class StoreDetails
{
	[JsonPropertyName("productSummaries")]
	public ProductSummaries[] ProductSummaries { get; init; }
}

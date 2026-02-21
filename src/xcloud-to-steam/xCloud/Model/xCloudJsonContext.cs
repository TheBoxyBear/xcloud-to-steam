using System.Text.Json.Serialization;

namespace xCloudToSteam.xCloud.Model;

[JsonSerializable(typeof(GameIdDto))]
[JsonSerializable(typeof(GameIdDto[]))]
[JsonSerializable(typeof(ProductDetails))]
[JsonSerializable(typeof(ProductRootDto<IEnumerable<string>>), TypeInfoPropertyName = "ProductDetailsRequestDto")]
[JsonSerializable(typeof(ProductRootDto<Dictionary<string, ProductDetails>>), TypeInfoPropertyName = "ProductDetailsDict")]
[JsonSerializable(typeof(StoreImageSet))]
[JsonSerializable(typeof(StoreDetails))]
[JsonSerializable(typeof(ProductSummaries))]
public partial class xCloudJsonContext : JsonSerializerContext { }

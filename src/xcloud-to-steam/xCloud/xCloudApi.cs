using RestSharp;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using xCloudToSteam.xCloud.Model;

namespace xCloudToSteam.xCloud;

public static class xCloudApi
{
	private static readonly RestClient
		s_catalogClient = new("https://catalog.gamepass.com/"),
		s_storeClient   = new("https://emerald.xboxservices.com/");

	public static async IAsyncEnumerable<string> GetCatalog(CancellationToken cancellationToken = default)
	{
		RestRequest request = new("sigls/v3", Method.Get);

		request
			.AddQueryParameter("id", "1bf84c2b-0643-4591-893f-d9edb703f692")
			.AddQueryParameter("subscriptionContext", "none")
			.AddQueryParameter("platformContext", "Cloud:XGPUWEB")
			.AddQueryParameter("hydration", "RemoteLowJade0")
			.AddQueryParameter("market", "CA")
			.AddQueryParameter("language", "en-CA")
			.AddHeader("ms-cv", GenerateCorrelationVector())
			.AddHeader("calling-app-name", "xCloud To Steam")
			.AddHeader("calling-app-version", "0.1");

		RestResponse response = await s_catalogClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

		if (response.StatusCode != HttpStatusCode.OK)
			throw new Exception($"Failed to fetch catalog: {response.StatusCode} - {response.Content}");

		if (response.Content is null)
			throw new Exception("Failed to fetch catalog: Response content is null");

		using MemoryStream stream = new(Encoding.UTF8.GetBytes(response.Content));

		// Goldeneye, Rare Replay
		List<string> missingIds = ["9N6639H7VGH4", "BWXKD3FFMNP3"];

		await foreach (GameIdDto dto in JsonSerializer.DeserializeAsyncEnumerable(stream, xCloudJsonContext.Default.GameIdDto, cancellationToken).Skip(1))
		{
			int missingIdIndex = missingIds.IndexOf(dto.Id);

			if (missingIdIndex != -1)
				missingIds.RemoveAt(missingIdIndex);

			// Skip Just Dance as it's unplayable over cloud
			if (dto.Id == "9P0LHV4DV2BG")
				continue;

			yield return dto.Id;
		}

		foreach (string missingId in missingIds)
			yield return missingId;
	}

	public static async IAsyncEnumerable<ProductDetails> GetDetails(IEnumerable<string> ids, CancellationToken cancellationToken = default)
	{
		ProductRootDto<IEnumerable<string>> dto = new()
		{
			Products = ids
		};

		RestRequest request = new("v3/products", Method.Post);

		request
			.AddQueryParameter("hydration", "RemoteLowJade0")
			.AddQueryParameter("market", "CA")
			.AddQueryParameter("language", "en-CA")
			.AddHeader("ms-cv", GenerateCorrelationVector())
			.AddHeader("calling-app-name", "xCloud To Steam")
			.AddHeader("calling-app-version", "0.1");

		request.AddStringBody(JsonSerializer.Serialize(dto, xCloudJsonContext.Default.ProductDetailsRequestDto), DataFormat.Json);

		RestResponse response = await s_catalogClient.ExecuteAsync(request, cancellationToken);

		if (response.StatusCode != HttpStatusCode.OK)
			throw new Exception($"Failed to fetch product details: {response.StatusCode} - {response.Content}");

		if (response.Content is null)
			throw new Exception("Failed to fetch game details: Response content is null");

		foreach (ProductDetails details in JsonSerializer.Deserialize(response.Content, xCloudJsonContext.Default.ProductDetailsDict).Products.Values)
			yield return details;
	}

	public static async Task<StoreDetails> GetStoreDetails(string storeId, CancellationToken cancellationToken = default)
	{
		RestRequest request = new($"xboxcomfd/products/{storeId}", Method.Get);

		request
			.AddQueryParameter("locale", "en-CA")
			.AddHeader("ms-cv", GenerateCorrelationVector())
			.AddHeader("calling-app-name", "xCloud To Steam")
			.AddHeader("calling-app-version", "0.1");

		RestResponse response = await s_storeClient.ExecuteAsync(request, cancellationToken);

		return JsonSerializer.Deserialize(response.Content, xCloudJsonContext.Default.StoreDetails);
	}

	private static string GenerateCorrelationVector()
	{
		// Generate a random 16-byte base value
		Span<byte> bytes = stackalloc byte[16];
		RandomNumberGenerator.Fill(bytes);

		// Convert to base64 and remove padding/special characters
		string base64 = Convert.ToBase64String(bytes)
			.Replace("+", "")
			.Replace("/", "")
			.Replace("=", "");

		// Return in Correlation Vector format: base.counter
		return $"{base64}.0";
	}
}

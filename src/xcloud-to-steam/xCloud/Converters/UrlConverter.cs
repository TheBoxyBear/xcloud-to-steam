using System.Text.Json;
using System.Text.Json.Serialization;

namespace xCloudToSteam.xCloud.Converters;

public class UrlConverter : JsonConverter<string>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType == JsonTokenType.String)
			return reader.GetString();

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			using JsonDocument doc = JsonDocument.ParseValue(ref reader);

			if (doc.RootElement.TryGetProperty("URL", out JsonElement urlElement))
				return urlElement.GetString();

			if (doc.RootElement.TryGetProperty("url", out urlElement))
				return urlElement.GetString();
		}

		throw new JsonException($"Unable to convert {reader.TokenType} to ImageDto");
	}

	public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}

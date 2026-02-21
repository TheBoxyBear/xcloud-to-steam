using System.Text.Json.Serialization;

using xCloudToSteam.Core.Config;

namespace xCloudToSteam.Core;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ShortcutConfig))]
[JsonSerializable(typeof(ShortcutConfigProfile))]
[JsonSerializable(typeof(Dictionary<string, ShortcutConfigProfile>))]
public partial class AppJsonContext : JsonSerializerContext { }

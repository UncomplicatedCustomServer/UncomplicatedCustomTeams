using LabApi.Features;
using System.Text.Json.Serialization;
using UncomplicatedCustomTeams.Manager;

namespace UncomplicatedCustomTeams.Messages
{
    internal class ShareLogMessage(string message)
    {
        [JsonPropertyName("labapi_version")]
        public string LabAPIVersion { get; set; } = LabApiProperties.CompiledVersion;

        [JsonPropertyName("plugin_version")]
        public string PluginVersion { get; set; } = Plugin.Singleton.Version.ToString(4);

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = VersionManager.HashFile(Plugin.Singleton.FilePath);

        [JsonPropertyName("message")]
        public string Message { get; set; } = message;
    }
}

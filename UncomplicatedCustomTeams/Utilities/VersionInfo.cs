using System.Text.Json.Serialization;

namespace UncomplicatedCustomRoles.Manager.NET
{
#nullable enable

    [method: JsonConstructor]
#nullable enable

    internal class VersionInfo(string name, string source, string? sourceLink, string? customName, bool preRelease, bool forceDebug, string message, bool recall, string? recallTarget, string? recallReason, bool? recallImportant, string hash)
    {
        [JsonPropertyName("name")]
        public string Name { get; } = name;

        [JsonPropertyName("source")]
        public string Source { get; } = source;

        [JsonPropertyName("source_link")]
        public string? SourceLink { get; } = sourceLink;

        [JsonPropertyName("custom_name")]
        public string? CustomName { get; } = customName;

        [JsonPropertyName("pre_release")]
        public bool PreRelease { get; } = preRelease;

        [JsonPropertyName("force_debug")]
        public bool ForceDebug { get; } = forceDebug;

        [JsonPropertyName("message")]
        public string Message { get; } = message;

        [JsonPropertyName("recall")]
        public bool Recall { get; } = recall;

        [JsonPropertyName("recall_target")]
        public string? RecallTarget { get; } = recallTarget;

        [JsonPropertyName("recall_reason")]
        public string? RecallReason { get; } = recallReason;

        [JsonPropertyName("recall_important")]
        public bool? RecallImportant { get; } = recallImportant;

        [JsonPropertyName("hash")]
        public string Hash { get; } = hash;
    }
}
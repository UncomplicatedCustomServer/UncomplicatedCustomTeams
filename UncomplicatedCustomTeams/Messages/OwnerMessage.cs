using LabApi.Features.Wrappers;
using System.Text.Json.Serialization;

namespace UncomplicatedCustomTeams.Messages
{
    internal class OwnerMessage(Player player, string discordId)
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = player.UserId;

        [JsonPropertyName("discord_id")]
        public string DiscordId { get; set; } = discordId;
    }
}

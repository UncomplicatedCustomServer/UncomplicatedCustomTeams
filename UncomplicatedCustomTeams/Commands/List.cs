using CommandSystem;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;
using UnityEngine;


namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class List : IUCTCommand
    {
        public string Name { get; } = "list";
        
        public string Description { get; } = "Displays all registered custom teams.";
        
        public string RequiredPermission { get; } = "uct.list";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (Team.List.Count == 0)
            {
                response = "There are no registered custom teams.";
                return false;
            }

            StringBuilder sb = new();
            sb.AppendLine("== Registered Custom Teams ==");
            foreach (var team in Team.List)
            {
                bool IsAudioSystemInUse = team.SoundPath != null && team.SoundPath != "/path/to/your/ogg/file" && team.SoundVolume > 0;
                bool IsUsingCustomSpawnPosition = team.SpawnConditions.SpawnPosition != Vector3.zero;
                bool IsUsingCassieSystem = team.CassieMessage != null && team.CassieMessage.Length > 0;
                bool IsUsingCassieTranslation = team.CassieTranslation != null && team.CassieTranslation.Length > 0;

                sb.AppendLine($"- <b>{team.Name}</b> (ID: {team.Id})");
                sb.AppendLine($"  Min Players: {team.MinPlayers}");
                sb.AppendLine($"  Spawn Chance: {team.SpawnChance}%");
                sb.AppendLine($"  Is Using Cassie System: {IsUsingCassieSystem}");
                sb.AppendLine($"  Is Using Cassie Translation: {IsUsingCassieTranslation}");
                sb.AppendLine($"  Is Noisy: {team.IsNoisy}");
                sb.AppendLine($"  Spawn Wave: {team.SpawnConditions.SpawnWave}");
                sb.AppendLine($"  Used Item: {team.SpawnConditions.UsedItem}");
                sb.AppendLine($"  Target SCP: {team.SpawnConditions.TargetScp}");
                sb.AppendLine($"  Spawn Delay: {team.SpawnConditions.SpawnDelay}");
                sb.AppendLine($"  Is Using Custom Spawn Position: {IsUsingCustomSpawnPosition}");
                sb.AppendLine($"  Roles: {team.Roles.Count}");
                sb.AppendLine($"  Is Audio System In Use: {IsAudioSystemInUse}");
                sb.AppendLine($"  Audio Volume: {team.SoundVolume}");
                sb.AppendLine($"  Team Alive To Win: {string.Join(", ", team.TeamAliveToWin)}");
                sb.AppendLine();
            }

            response = sb.ToString();
            return true;
        }
    }
}
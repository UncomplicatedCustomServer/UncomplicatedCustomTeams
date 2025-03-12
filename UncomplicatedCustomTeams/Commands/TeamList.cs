using CommandSystem;
using System.Collections.Generic;
using System.Text;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;


namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class TeamList : IUCTCommand
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
                sb.AppendLine($"- <b>{team.Name}</b> (ID: {team.Id})");
                sb.AppendLine($"  Min Players: {team.MinPlayers} | Spawn Chance: {team.SpawnChance}%");
                sb.AppendLine($"  Spawn Wave: {team.SpawnWave} | Roles: {team.Roles.Count}");
                sb.AppendLine();
            }

            response = sb.ToString();
            return true;
        }
    }
}
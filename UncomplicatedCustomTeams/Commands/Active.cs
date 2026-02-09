using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Active : IUCTCommand
    {
        public string Name { get; } = "active";
        public string Description { get; } = "Displays all currently active custom teams with alive players.";
        public string RequiredPermission { get; } = "uct.active";
        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            var aliveTeams = SummonedTeam.List.Where(t => t.Members.Any(m => m.Player.IsAlive)).ToList();

            if (aliveTeams.Count == 0)
            {
                response = "No custom teams have alive players.";
                return false;
            }

            StringBuilder sb = new();
            sb.AppendLine("== Active Custom Teams ==");

            foreach (var team in aliveTeams)
            {
                sb.AppendLine($"- <b>{team.Definition.Name}</b> (ID: {team.Definition.Id})");
                sb.AppendLine($"  Players Alive: {team.Members.Count(m => m.Player.IsAlive)} / {team.Members.Count}");

                var spawnTime = DateTimeOffset.FromUnixTimeMilliseconds(team.SpawnTime);
                sb.AppendLine($"  Spawn Time: {spawnTime.ToLocalTime():HH:mm:ss}");

                TimeSpan elapsed = DateTimeOffset.UtcNow - spawnTime;
                sb.AppendLine($"  Time Since Spawn: {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s");

                var roleNames = team.Members.Select(m => m.CustomRole.Name).Distinct();
                sb.AppendLine($"  Roles: {string.Join(", ", roleNames)}");
                sb.AppendLine();
            }

            response = sb.ToString();
            return true;
        }
    }
}
using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UncomplicatedCustomTeams.API.Features;
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
            var aliveTeams = SummonedTeam.List.Where(t => t.HasAlivePlayers()).ToList();

            if (aliveTeams.Count == 0)
            {
                response = "No custom teams have alive players.";
                return false;
            }

            StringBuilder sb = new();
            sb.AppendLine("== Active Custom Teams ==");

            foreach (var team in aliveTeams)
            {
                sb.AppendLine($"- <b>{team.Team.Name}</b> (ID: {team.Team.Id})");
                sb.AppendLine($"  Players Alive: {team.Players.Count(p => p.Player.IsAlive)} / {team.Players.Count}");
                sb.AppendLine($"  Spawn Time: {DateTimeOffset.FromUnixTimeMilliseconds(team.Time).ToLocalTime():HH:mm:ss}");
                TimeSpan elapsed = DateTimeOffset.Now - DateTimeOffset.FromUnixTimeMilliseconds(team.Time);
                sb.AppendLine($"  Time Since Spawn: {elapsed.Minutes:D2}m {elapsed.Seconds:D2}s");

                sb.AppendLine($"  Roles: {string.Join(", ", team.Players.Select(p => p.CustomRole.Name))}");
                sb.AppendLine();
            }


            response = sb.ToString();
            return true;
        }
    }
}

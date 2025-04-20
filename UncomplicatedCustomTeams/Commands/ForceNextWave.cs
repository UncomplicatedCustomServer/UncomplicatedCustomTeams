using CommandSystem;
using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Interfaces;
using Respawning;
using Exiled.API.Enums;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class ForceNextWave // Broken
    {
        public string Name { get; } = "fnw";

        public string Description { get; } = "Force the next wave to be a custom Team AKA \"fnw\".";

        public string RequiredPermission { get; } = "uct.fnw";

        public bool Executor(List<string> arguments, ICommandSender _, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Usage: uct fnw <TeamId>";
                return false;
            }

            if (!uint.TryParse(arguments[0], out var id))
            {
                response = "Invalid team ID!";
                return false;
            }

            Team team = Team.List.FirstOrDefault(t => t.Id == id);

            if (team is null)
            {
                response = $"Team {id} is not registered!";
                return false;
            }

            if (team.SpawnConditions.SpawnWave != "NtfWave" && team.SpawnConditions.SpawnWave != "ChaosWave")
            {
                response = $"This team cannot be forced because its SpawnWave is not 'NtfWave' or 'ChaosWave'.";
                return false;
            }

            Plugin.NextTeam = new SummonedTeam(team);
            Handler handler = new()
            {
                ForcedNextWave = true
            };

            response = $"Successfully forced the team {team.Name} to be the next respawn wave!";
            return true;
        }
    }
}
using CommandSystem;
using LabApi.Features.Wrappers;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Interfaces;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Spawn : IUCTCommand
    {
        public string Name { get; } = "spawn";
        public string Description { get; } = "Force spawn a custom team.";
        public string RequiredPermission { get; } = "uct.spawn";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsRoundStarted)
            {
                response = "Round is not started yet!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: uct spawn <TeamId>";
                return false;
            }

            if (!uint.TryParse(arguments.ElementAt(0), out uint teamId))
            {
                response = "Invalid TeamId! It must be a positive integer.";
                return false;
            }

            Team team = Team.List.FirstOrDefault(t => t.Id == teamId);

            if (team is null)
            {
                response = $"Team with ID {teamId} is not registered!";
                return false;
            }

            var summonedTeam = TeamSpawner.SpawnSpecificTeam(team);

            if (summonedTeam == null)
            {
                response = $"Failed to spawn team {team.Name}. Check logs (maybe not enough spectators?).";
                return false;
            }

            response = $"Successfully spawned team '{team.Name}' with {summonedTeam.Members.Count} players!";
            return true;
        }
    }
}
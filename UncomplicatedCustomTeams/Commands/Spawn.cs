using CommandSystem;
using Exiled.API.Features;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    internal class Spawn : IUCTCommand
    {
        public string Name { get; } = "spawn";

        public string Description { get; } = "Force spawn a custom team.";

        public string RequiredPermission { get; } = "uct.spawn";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "Round is not started yet!";
                return false;
            }

            if (arguments.Count < 1 || arguments.Count > 2)
            {
                response = "Usage: uct spawn <TeamId> <PlayerCount>";
                return false;
            }

            if (!uint.TryParse(arguments[0], out uint teamId))
            {
                response = "Invalid TeamId! It must be a positive integer.";
                return false;
            }
            Team team = Team.List.FirstOrDefault(t => t.Id == teamId);

            if (team is null)
            {
                response = $"Team {uint.Parse(arguments[0])} is not registered!";
                return false;
            }
            else
            {
                Bucket.SpawnBucket = [];

                var spectators = Player.List.Where(p => !p.IsAlive && p.Role.Type is PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled).ToList();

                int playersToSpawnCount = spectators.Count;

                if (arguments.Count == 2)
                {
                    if (!int.TryParse(arguments[1], out int requestedCount) || requestedCount <= 0)
                    {
                        response = "Invalid player count! It must be a positive number.";
                        return false;
                    }
                    playersToSpawnCount = requestedCount;
                }

                var playersToSpawn = spectators.Take(playersToSpawnCount);

                SummonedTeam Summoned = SummonedTeam.Summon(team, playersToSpawn);
                if (Summoned == null)
                {
                    response = $"Failed to spawn team {team.Name}.";
                    return false;
                }

                Summoned.SpawnAll();

                response = $"Successfully spawned the team {team.Name} with {Summoned.Players.Count} players!";

                Timing.CallDelayed(1.5f, () =>
                {
                    Bucket.SpawnBucket = [];
                });
                return true;
            }
        }
    }
}

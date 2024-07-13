using CommandSystem;
using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;

namespace UncomplicatedCustomTeams.Commands
{
    internal class ForceNextWave
    {
        public string Name { get; } = "force_next_wave";

        public string Description { get; } = "Force the next wave to be a custom Team";

        public string RequiredPermission { get; } = "uct.force_next_wave";

        public bool Executor(List<string> arguments, ICommandSender _, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Usage: uct force_next_wave <TeamId>";
                return false;
            }

            Team Team = Team.List.Where(team => team.Id == uint.Parse(arguments[0])).FirstOrDefault();

            if (Team is null)
            {
                response = $"Team {uint.Parse(arguments[0])} is not registered!";
                return false;
            }
            else
            {
                Bucket.SpawnBucket = new();
                foreach (Player Player in Player.List.Where(p => !p.IsAlive && p.Role.Type is PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                    Bucket.SpawnBucket.Add(Player.Id);

                Plugin.NextTeam = SummonedTeam.Summon(Team, Player.List.Where(p => !p.IsAlive && p.Role.Type is PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled));

                response = $"Successfully forced the team {Team.Name} to be the next respawn wave!";

                return true;
            }
        }
    }
}

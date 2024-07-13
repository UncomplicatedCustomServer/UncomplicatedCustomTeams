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

        public string Description { get; } = "Force spawn a custom team";

        public string RequiredPermission { get; } = "uct.spawn";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Usage: uct spawn <TeamId>";
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

                SummonedTeam Summoned = SummonedTeam.Summon(Team, Player.List.Where(p => !p.IsAlive && p.Role.Type is PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled));
                Summoned.SpawnAll();

                response = $"Successfully spawned the team {Team.Name}!";

                Timing.CallDelayed(1.5f, () => {
                    Bucket.SpawnBucket = new();
                    Plugin.NextTeam?.CheckPlayers(); 
                });

                return true;
            }
        }
    }
}

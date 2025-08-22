using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    [Obsolete("This command is broken and will be removed in a future version. Use UCR spawn command.")]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Role : IUCTCommand
    {
        public string Name => "role";
        public string Description => "Forcefully respawns a player as a custom role from a custom team.";
        public string RequiredPermission => "uct.role";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(RequiredPermission))
            {
                response = "You do not have permission to use this command.";
                return false;
            }

            if (arguments.Count < 3)
            {
                response = "Usage: role <playerId> <teamId> <roleId>";
                return false;
            }

            if (!int.TryParse(arguments[0], out int playerId) ||
                !uint.TryParse(arguments[1], out uint teamId) ||
                !int.TryParse(arguments[2], out int roleId))
            {
                response = "Invalid arguments! Expected integers.";
                return false;
            }

            Player player = Player.Get(playerId);
            if (player == null)
            {
                response = $"Player with ID {playerId} not found!";
                return false;
            }

            Team team = Team.List.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                response = $"Team with ID {teamId} not found!";
                return false;
            }

            IUCTCustomRole role = team.TeamRoles.FirstOrDefault(r => r.Id == roleId);
            if (role == null)
            {
                response = $"Role with ID {roleId} not found in team {team.Name}.";
                return false;
            }
            int originalMax = role.MaxPlayers;
            role.MaxPlayers = 1;

            var summonedTeam = new SummonedTeam(team);
            var summonedRole = new SummonedCustomRole(summonedTeam, player, role);
            summonedTeam.Players.Add(summonedRole);
            summonedRole.AddRole();
            role.MaxPlayers = originalMax;

            response = $"Successfully respawned {player.Nickname} as role ID {role.Id} in team {team.Name}.";
            return true;
        }

    }
}

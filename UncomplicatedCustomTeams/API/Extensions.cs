using LabApi.Features.Wrappers;
using PlayerRoles;
using System.Linq;
using UncomplicatedCustomTeams.API.Features.Runtime;

namespace UncomplicatedCustomTeams.API
{
    public static class Extensions
    {
        /// <summary>
        /// Gets the <see cref="SummonedTeam"/> the player belongs to, or null if none.
        /// </summary>
        public static SummonedTeam GetCustomTeam(this Player player)
        {
            return SummonedTeam.List.FirstOrDefault(t => t.Members.Any(m => m.Player == player));
        }

        /// <summary>
        /// Checks if two players are allies based on Custom Team definitions.
        /// </summary>
        public static bool IsCustomTeamAlly(this Player player, Player target)
        {
            var teamA = player.GetCustomTeam();
            var teamB = target.GetCustomTeam();

            if (teamA != null && teamB != null && teamA == teamB)
                return true;

            if (teamA != null && teamB == null)
            {
                return teamA.Definition.WinCondition.AlliedTeams.Contains(target.Role.GetTeam());
            }

            if (teamA == null && teamB != null)
            {
                return teamB.Definition.WinCondition.AlliedTeams.Contains(player.Role.GetTeam());
            }

            return false;
        }
    }
}
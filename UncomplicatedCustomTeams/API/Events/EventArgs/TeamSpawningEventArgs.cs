using LabApi.Features.Wrappers;
using System.Collections.Generic;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Features.Definitions;

namespace UncomplicatedCustomTeams.API.Events.EventArgs
{
    /// <summary>
    /// Arguments for the <see cref="UCTEvents.TeamSpawning"/> event.
    /// Fired BEFORE a custom team is actually spawned.
    /// </summary>
    public class TeamSpawningEventArgs(Team team, Dictionary<Player, IUCTCustomRole> playersToSpawn) : System.EventArgs
    {
        /// <summary>
        /// The configuration definition of the team that is about to spawn.
        /// </summary>
        public Team Team { get; } = team;

        /// <summary>
        /// The dictionary of players selected to join this team.
        /// You can add or remove players from this dictionary to change who gets spawned.
        /// </summary>
        public Dictionary<Player, IUCTCustomRole> PlayersToSpawn { get; set; } = playersToSpawn;

        /// <summary>
        /// If set to false, the team spawn will be completely cancelled.
        /// </summary>
        public bool IsAllowed { get; set; } = true;
    }
}
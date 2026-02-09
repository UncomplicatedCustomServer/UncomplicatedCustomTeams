using UncomplicatedCustomTeams.API.Features.Runtime;

namespace UncomplicatedCustomTeams.API.Events.EventArgs
{
    /// <summary>
    /// Arguments for the <see cref="UCTEvents.TeamSpawned"/> event.
    /// Fired AFTER a custom team has been successfully spawned and roles assigned.
    /// </summary>
    public class TeamSpawnedEventArgs(SummonedTeam summonedTeam) : System.EventArgs
    {
        /// <summary>
        /// The active instance of the spawned team containing members and runtime data.
        /// </summary>
        public SummonedTeam SummonedTeam { get; } = summonedTeam;
    }
}
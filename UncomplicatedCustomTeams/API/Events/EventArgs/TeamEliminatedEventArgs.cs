using System;
using UncomplicatedCustomTeams.API.Features.Runtime;

namespace UncomplicatedCustomTeams.API.Events.EventArgs
{
    /// <summary>
    /// Arguments for the <see cref="UCTEvents.TeamEliminated"/> event.
    /// Fired when the last member of a custom team dies or is removed.
    /// </summary>
    public class TeamEliminatedEventArgs : System.EventArgs
    {
        /// <summary>
        /// The active instance of the team that was eliminated.
        /// </summary>
        public SummonedTeam SummonedTeam { get; }

        /// <summary>
        /// The amount of time this team survived since it was spawned.
        /// </summary>
        public TimeSpan TimeSurvived { get; }

        public TeamEliminatedEventArgs(SummonedTeam summonedTeam)
        {
            SummonedTeam = summonedTeam;
            long spawnTime = summonedTeam.SpawnTime;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            TimeSurvived = TimeSpan.FromMilliseconds(now - spawnTime);
        }
    }
}
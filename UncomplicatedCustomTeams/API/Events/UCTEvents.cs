using LabApi.Events;
using System;
using UncomplicatedCustomTeams.API.Events.EventArgs;

namespace UncomplicatedCustomTeams.API.Events
{
    /// <summary>
    /// Provides access to all UncomplicatedCustomTeams events.
    /// </summary>
    public static class UCTEvents
    {
        /// <summary>
        /// Fired BEFORE a team spawns. Can be cancelled to prevent the spawn.
        /// Allows modifying the list of players to be spawned.
        /// </summary>
        public static event LabEventHandler<TeamSpawningEventArgs> TeamSpawning;

        /// <summary>
        /// Fired AFTER a team has successfully spawned and roles have been assigned.
        /// </summary>
        public static event LabEventHandler<TeamSpawnedEventArgs> TeamSpawned;

        /// <summary>
        /// Fired when the LAST member of a custom team dies.
        /// </summary>
        public static event LabEventHandler<TeamEliminatedEventArgs> TeamEliminated;

        /// <summary>
        /// Fired after config files have been reloaded using the 'uct reload' command.
        /// External plugins should subscribe to this event to re-register their code-defined teams.
        /// </summary>
        public static event Action DefinitionsLoaded;

        internal static void InvokeTeamSpawning(TeamSpawningEventArgs args) => TeamSpawning?.Invoke(args);
        internal static void InvokeTeamSpawned(TeamSpawnedEventArgs args) => TeamSpawned?.Invoke(args);
        internal static void InvokeTeamEliminated(TeamEliminatedEventArgs args) => TeamEliminated?.Invoke(args);
        internal static void InvokeDefinitionsLoaded() => DefinitionsLoaded?.Invoke();
    }
}
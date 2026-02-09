using LabApi.Features.Wrappers;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Events.EventArgs;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.API.Features.Services
{
    /// <summary>
    /// Service responsible for handling spawn logic, RNG calculations, and creating team instances.
    /// </summary>
    public static class TeamSpawner
    {
        private static readonly System.Random _random = new();

        /// <summary>
        /// Attempts to spawn ANY team that matches the specified <see cref="WaveType"/>.
        /// This handles the full process: Evaluation, RNG, Player Selection, and Instantiation.
        /// </summary>
        /// <param name="wave">The wave type to check.</param>
        public static void TrySpawnWave(WaveType wave)
        {
            var candidates = Team.List.Where(t => t.SpawnConditions.SpawnWave == wave).ToList();

            if (candidates.Count == 0) return;

            LogManager.Debug($"Found {candidates.Count} candidate teams for wave {wave}.");

            foreach (var team in candidates)
            {
                if (CanSpawn(team))
                {
                    var players = GetSpectatorsForTeam(team);
                    if (players.Count == 0) continue;

                    var ev = new TeamSpawningEventArgs(team, players);
                    UCTEvents.InvokeTeamSpawning(ev);

                    if (!ev.IsAllowed || ev.PlayersToSpawn.Count == 0) continue;

                    SummonedTeam.Create(team, ev.PlayersToSpawn);

                    if (!team.AllowConcurrentSpawns) break;
                }
            }
        }

        private static bool CanSpawn(Team team)
        {
            if (team.MaxSpawns != -1 && team.CurrentSpawnCount >= team.MaxSpawns)
            {
                LogManager.Debug($"Team {team.Name} reached max spawns.");
                return false;
            }

            if (team.SpawnConditions.RequiredAliveRoles.Count > 0)
            {
                bool anyAlive = Player.List.Any(p => p.IsAlive && team.SpawnConditions.RequiredAliveRoles.Contains(p.Role));
                if (!anyAlive) return false;
            }

            int roll = _random.Next(0, 100);
            bool success = roll < team.SpawnChance;
            LogManager.Debug($"Team {team.Name} spawn roll: {roll} < {team.SpawnChance} = {success}");
            return success;
        }

        /// <summary>
        /// Forces a specific <see cref="Team"/> to spawn immediately, utilizing available spectators.
        /// </summary>
        /// <param name="team">The team definition to spawn.</param>
        /// <returns>The created <see cref="SummonedTeam"/> instance, or null if failed.</returns>
        public static SummonedTeam SpawnSpecificTeam(Team team)
        {
            var players = GetSpectatorsForTeam(team);
            if (players.Count == 0)
            {
                LogManager.Debug($"Cannot spawn team {team.Name}. No spectators available.");
                return null;
            }

            var ev = new TeamSpawningEventArgs(team, players);
            UCTEvents.InvokeTeamSpawning(ev);

            if (!ev.IsAllowed || ev.PlayersToSpawn.Count == 0)
            {
                return null;
            }

            return SummonedTeam.Create(team, ev.PlayersToSpawn);
        }

        /// <summary>
        /// Evaluates spawn chances for all teams matching the <paramref name="wave"/> type.
        /// Returns a list of teams that "won" the RNG roll and are eligible to spawn.
        /// </summary>
        /// <param name="wave">The wave context.</param>
        /// <returns>A list of team definitions that should spawn.</returns>
        public static List<Team> EvaluateSpawn(WaveType wave)
        {
            List<Team> winningTeams = [];

            var eligibleTeams = Team.List.Where(t => t.SpawnConditions.SpawnWave == wave).ToList();

            if (!eligibleTeams.Any())
            {
                return winningTeams;
            }

            LogManager.Debug($"Found {eligibleTeams.Count} eligible Custom Team(s) for WaveType '{wave}'. Evaluating chances.");

            foreach (var team in eligibleTeams)
            {
                if (team.MaxSpawns != -1 && team.CurrentSpawnCount >= team.MaxSpawns)
                {
                    LogManager.Debug($"Team '{team.Name}' reached max spawns ({team.CurrentSpawnCount}/{team.MaxSpawns}). Skipping.");
                    continue;
                }

                int roll = _random.Next(0, 100);
                if (roll < team.SpawnChance)
                {
                    LogManager.Debug($"Team '{team.Name}' succeeded its spawn roll! (Rolled: {roll} < {team.SpawnChance}). Adding to spawn list.");
                    winningTeams.Add(team);

                    if (!team.AllowConcurrentSpawns)
                    {
                        LogManager.Debug($"Team '{team.Name}' has AllowConcurrentSpawns set to false. Stopping further evaluations for this wave.");
                        break;
                    }
                }
                else
                {
                    LogManager.Debug($"Team '{team.Name}' failed its spawn roll. (Rolled: {roll} >= {team.SpawnChance}).");
                }
            }

            if (!winningTeams.Any())
                LogManager.Debug($"No custom team succeeded their spawn roll for wave '{wave}'.");

            return winningTeams;
        }

        private static List<Player> GetSpectatorsForTeam(Team team)
        {
            if (team == null)
                return [];

            List<Player> allPlayers = [.. Player.List];
            int totalPlayers = allPlayers.Count;

            LogManager.Debug($"Total players: {totalPlayers}, MinPlayers required: {team.MinPlayers}");
            if (totalPlayers < team.MinPlayers)
            {
                LogManager.Debug($"Not enough players on spectator to spawn team {team.Name}.");
                return [];
            }

            List<Player> spectators = [.. allPlayers.Where(p => !p.IsAlive && p.Role == RoleTypeId.Spectator && !p.IsOverwatchEnabled)];

            var sortedRoles = team.TeamRoles
                .Where(role => role.Priority != RolePriority.None)
                .OrderBy(role => role.Priority)
                .ToList();

            List<Player> selectedPlayers = [];
            int index = 0;

            foreach (var teamRole in sortedRoles)
            {
                int max = teamRole.MaxPlayers;
                for (int i = 0; i < max && index < spectators.Count; i++, index++)
                {
                    selectedPlayers.Add(spectators[index]);
                }
            }

            if (selectedPlayers.Count == 0)
            {
                LogManager.Debug($"No spectators available to spawn for team {team.Name}.");
            }
            else
            {
                LogManager.Info($"Team {team.Name} will spawn with {selectedPlayers.Count} players.");
            }

            return selectedPlayers;
        }
    }
}


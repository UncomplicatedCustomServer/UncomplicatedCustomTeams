using Exiled.API.Features;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class RoundStarted
    {
        public void OnRoundStarted() // Written from scratch
        {
            LogManager.Debug("Round started, checking for RoundStarted spawns...");
            var team = Team.EvaluateSpawn(WaveType.RoundStarted);
            if (team == null)
            {
                LogManager.Debug("No valid team found for RoundStarted.");
                return;
            }

            team.SpawnCount = 0;
            var requiredRoles = team.SpawnConditions.RoleAliveOnRoundStart;
            if (requiredRoles != null && requiredRoles.Count > 0)
            {
                var aliveRoles = Player.List.Where(p => p.IsAlive).Select(p => p.Role.Type).ToHashSet();
                if (!requiredRoles.Any(aliveRoles.Contains))
                {
                    LogManager.Debug($"Skipping spawn for team {team.Name} — required roles not alive. Required: [{string.Join(", ", requiredRoles)}]");
                    return;
                }
            }

            LogManager.Debug($"EvaluateSpawn found team: {team.Name}");
            Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                var affectedRoles = team.SpawnConditions.RolesAffectedOnRoundStart;
                if (affectedRoles == null || !affectedRoles.Any())
                {
                    LogManager.Error($"Team {team.Name} is set to spawn at round start but does not have any roles defined in 'RolesAffectedOnRoundStart'. Skipping spawn.");
                    return;
                }

                List<Player> candidatePlayers = Player.List
                    .Where(p => p.IsAlive && affectedRoles.Contains(p.Role.Type))
                    .ToList();

                if (!candidatePlayers.Any())
                {
                    LogManager.Debug($"No players found with roles eligible for conversion to {team.Name}. Eligible roles: [{string.Join(", ", affectedRoles)}]. Skipping spawn.");
                    return;
                }

                Plugin.NextTeam = SummonedTeam.Summon(team, candidatePlayers);

                if (Plugin.NextTeam == null)
                {
                    LogManager.Debug($"Summoning team {team.Name} failed.");
                    return;
                }

                LogManager.Debug($"Spawning team {Plugin.NextTeam.Team.Name} by converting {Plugin.NextTeam.Players.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }
    }
}
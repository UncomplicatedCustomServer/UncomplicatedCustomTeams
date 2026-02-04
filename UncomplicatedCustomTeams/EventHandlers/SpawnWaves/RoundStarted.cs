using Exiled.API.Features;
using MEC;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class RoundStarted
    {
        public void OnRoundStarted()
        {
            LogManager.Debug("Round started, checking for RoundStarted spawns...");

            List<Team> teamsToSpawn = Team.EvaluateSpawn(WaveType.RoundStarted);

            if (!teamsToSpawn.Any())
            {
                LogManager.Debug("No valid team found for RoundStarted.");
                return;
            }

            LogManager.Debug($"EvaluateSpawns found {teamsToSpawn.Count} team(s) to spawn at round start.");

            foreach (var team in teamsToSpawn)
            {
                team.SpawnCount = 0;

                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    var affectedRoles = team.SpawnConditions.RolesAffectedOnRoundStart;
                    List<Player> candidatePlayers;

                    if (affectedRoles == null || !affectedRoles.Any())
                    {
                        LogManager.Debug($"'RolesAffectedOnRoundStart' is not defined for team {team.Name}. Considering all spectators as candidates.");
                        candidatePlayers = Player.List.Where(p => p.Role.Type == RoleTypeId.Spectator).ToList();
                    }
                    else
                    {
                        LogManager.Debug($"Filtering candidate players by roles for team {team.Name}. Roles: [{string.Join(", ", affectedRoles)}]");
                        candidatePlayers = Player.List
                            .Where(p => p.IsAlive && affectedRoles.Contains(p.Role.Type))
                            .ToList();
                    }

                    if (!candidatePlayers.Any())
                    {
                        LogManager.Debug($"No players found with roles eligible for conversion to {team.Name}. Skipping spawn.");
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

                    LogManager.Debug($"All players for team '{team.Name}' have been assigned roles.");
                });
                Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
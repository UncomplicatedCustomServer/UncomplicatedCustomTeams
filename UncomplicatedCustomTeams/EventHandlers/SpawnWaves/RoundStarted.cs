using LabApi.Features.Wrappers;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Events.EventArgs;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class RoundStarted
    {
        public void OnRoundStarted()
        {
            LogManager.Debug("Checking RoundStarted spawns...");

            var candidates = Team.List.Where(t => t.SpawnConditions.SpawnWave == WaveType.RoundStarted).ToList();

            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.RoundStarted);

            if (!candidates.Any()) return;

            foreach (var team in candidates)
            {
                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    var affectedRoles = team.SpawnConditions.RolesAffectedOnRoundStart;
                    if (affectedRoles == null || !affectedRoles.Any()) return;

                    var candidatePlayers = Player.List
                        .Where(p => p.IsAlive && affectedRoles.Contains(p.Role))
                        .ToList();

                    if (!candidatePlayers.Any()) return;

                    var ev = new TeamSpawningEventArgs(team, candidatePlayers);
                    UCTEvents.InvokeTeamSpawning(ev);

                    if (!ev.IsAllowed || ev.PlayersToSpawn.Count == 0) return;

                    var spawnedTeam = SummonedTeam.Create(team, ev.PlayersToSpawn);

                    LogManager.Debug($"Team with 'RoundStarted' Spawn Wave spawned successfully: {team.Name} ({spawnedTeam.Members.Count} players)");
                });
                Plugin.Singleton.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
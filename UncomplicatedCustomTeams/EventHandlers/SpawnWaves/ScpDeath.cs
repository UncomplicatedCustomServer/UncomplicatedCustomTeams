using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class ScpDeath
    {
        public void OnScpDying(PlayerDyingEventArgs ev)
        {
            if (ev.Player == null || !ev.Player.IsSCP) return;

            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.ScpDeath);

            if (!teamsToSpawn.Any()) return;

            foreach (var team in teamsToSpawn)
            {
                var spawnData = team.SpawnConditions;
                bool conditionMet = false;

                if (Enum.TryParse(spawnData.GetTargetScp(), true, out RoleTypeId targetRole) && targetRole != RoleTypeId.None)
                {
                    if (targetRole == ev.Player.Role) conditionMet = true;
                }
                else if (spawnData.GetTargetScp().Equals("SCPs", StringComparison.OrdinalIgnoreCase))
                {
                    if (ev.Player.Role == RoleTypeId.Scp0492 && !spawnData.IsScp0492CountedAsScp())
                        conditionMet = false;
                    else
                        conditionMet = true;
                }

                if (!conditionMet) continue;

                CoroutineHandle handle = Timing.CallDelayed(spawnData.SpawnDelay, () =>
                {
                    var spawnedTeam = TeamSpawner.SpawnSpecificTeam(team);
                    if (spawnedTeam != null)
                    {
                        LogManager.Debug($"Team with 'ScpDeath' Spawn Wave spawned successfully: {team.Name}");
                    }
                });
                Plugin.Singleton.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
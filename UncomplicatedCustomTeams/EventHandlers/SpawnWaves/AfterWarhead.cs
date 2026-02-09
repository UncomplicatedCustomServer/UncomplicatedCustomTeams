using LabApi.Events.Arguments.WarheadEvents;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class AfterWarhead
    {
        public void OnDetonated(WarheadDetonatedEventArgs _)
        {
            LogManager.Debug("Warhead detonated, checking for spawns...");

            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.AfterWarhead);

            if (!teamsToSpawn.Any()) return;

            foreach (Team team in teamsToSpawn)
            {
                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    var spawnedTeam = TeamSpawner.SpawnSpecificTeam(team);
                    if (spawnedTeam != null)
                    {
                        LogManager.Debug($"Team with 'AfterWarhead' Spawn Wave spawned successfully: {team.Name}");
                    }
                });
                Plugin.Singleton.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
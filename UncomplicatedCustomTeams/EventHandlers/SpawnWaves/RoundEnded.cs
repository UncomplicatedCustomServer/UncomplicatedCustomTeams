using LabApi.Events.Arguments.ServerEvents;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class RoundEnded
    {
        public void OnRoundEnded(RoundEndedEventArgs _)
        {
            LogManager.Debug("Round ended, checking for spawns...");

            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.RoundEnded);

            if (!teamsToSpawn.Any()) return;

            foreach (Team team in teamsToSpawn)
            {
                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    var spawnedTeam = TeamSpawner.SpawnSpecificTeam(team);
                    if (spawnedTeam != null)
                    {
                        LogManager.Debug($"Team with 'RoundEnded' Spawn Wave spawned successfully: {team.Name}");
                    }
                });
                Plugin.Singleton.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}

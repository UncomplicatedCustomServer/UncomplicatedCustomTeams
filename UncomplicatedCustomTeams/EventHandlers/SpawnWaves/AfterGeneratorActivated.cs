using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class AfterGeneratorActivated
    {
        public void OnGeneratorActivating(GeneratorActivatingEventArgs _)
        {
            int engagedCount = Generator.List.Count(g => g.Engaged);
            LogManager.Debug($"Generator engaged. Total: {engagedCount}. Checking spawns...");

            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.AfterGeneratorActivated);

            if (!teamsToSpawn.Any()) return;

            foreach (Team team in teamsToSpawn)
            {
                if (engagedCount < team.SpawnConditions.GetRequiredGenerators()) continue;

                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    var spawnedTeam = TeamSpawner.SpawnSpecificTeam(team);
                    if (spawnedTeam != null)
                    {
                        LogManager.Debug($"Team with 'AfterGeneratorActivated' Spawn Wave spawned successfully: {team.Name}");
                    }
                });
                Plugin.Singleton.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
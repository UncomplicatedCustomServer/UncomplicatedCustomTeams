using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Integrations;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class UsedItem
    {
        public void OnItemUsed(PlayerUsedItemEventArgs ev)
        {
            List<Team> teamsToSpawn = TeamSpawner.EvaluateSpawn(WaveType.UsedItem);

            if (!teamsToSpawn.Any()) return;

            foreach (var teamToSpawn in teamsToSpawn)
            {
                var spawnData = teamToSpawn.SpawnConditions;
                bool itemMatches = false;
                int? requiredCustomId = spawnData.GetCustomItemId();

                if (requiredCustomId.HasValue)
                {
                    if (UCI.IsCustomItem(ev.UsableItem, requiredCustomId.Value))
                        itemMatches = true;
                }
                else
                {
                    if (ev.UsableItem.Type == spawnData.GetUsedItemType())
                        itemMatches = true;
                }

                if (!itemMatches) continue;

                LogManager.Debug($"Item '{ev.UsableItem.Type}' matches team '{teamToSpawn.Name}' and passed RNG.");

                Timing.CallDelayed(teamToSpawn.SpawnConditions.SpawnDelay, () =>
                {
                    var spawnedTeam = TeamSpawner.SpawnSpecificTeam(teamToSpawn);
                    if (spawnedTeam != null)
                    {
                        LogManager.Debug($"Team with 'UsedItem' Spawn Wave spawned successfully: {teamToSpawn.Name}");
                    }
                });
            }
        }
    }
}
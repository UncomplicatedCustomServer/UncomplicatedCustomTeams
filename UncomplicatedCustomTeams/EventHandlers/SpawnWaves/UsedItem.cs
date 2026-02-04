using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class UsedItem
    {
        public void OnItemUsed(UsedItemEventArgs ev)
        {
            var allItemTeams = Team.List.Where(t => t.SpawnConditions.SpawnWave == WaveType.UsedItem).ToList();
            if (!allItemTeams.Any())
                return;

            List<Team> teamsToSpawn = new();

            foreach (var team in allItemTeams)
            {
                var spawnData = team.SpawnConditions;
                ItemType requiredItemType = spawnData.GetUsedItemType();
                int? requiredCustomId = spawnData.GetCustomItemId();

                bool itemMatches = false;
                if (requiredCustomId.HasValue)
                {
                    if (CustomItem.TryGet(ev.Item, out var customItem) && customItem.Id == requiredCustomId.Value)
                        itemMatches = true;
                }
                else
                {
                    if (ev.Item.Type == requiredItemType)
                        itemMatches = true;
                }

                if (itemMatches)
                {
                    LogManager.Debug($"Item '{ev.Item.Type}' matches requirements for team '{team.Name}'. Rolling spawn chance...");

                    if (new System.Random().Next(0, 100) < team.SpawnChance)
                    {
                        LogManager.Debug($"Team '{team.Name}' succeeded its roll. Adding to spawn list.");
                        teamsToSpawn.Add(team);

                        if (!team.AllowConcurrentSpawns)
                        {
                            LogManager.Debug($"Team '{team.Name}' has AllowConcurrentSpawns=false, stopping further checks.");
                            break;
                        }
                    }
                    else
                    {
                        LogManager.Debug($"Team '{team.Name}' failed its spawn roll.");
                    }
                }
            }

            if (!teamsToSpawn.Any())
            {
                LogManager.Debug("No team succeeded their spawn roll for this item usage.");
                return;
            }

            foreach (var teamToSpawn in teamsToSpawn)
            {
                CoroutineHandle handle = Timing.CallDelayed(teamToSpawn.SpawnConditions.SpawnDelay, () =>
                {
                    Bucket.SpawnBucket = [];
                    foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                        Bucket.SpawnBucket.Add(player.Id);

                    if (Bucket.SpawnBucket.Count == 0) return;

                    Plugin.NextTeam = SummonedTeam.Summon(teamToSpawn, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                    if (Plugin.NextTeam == null) return;

                    LogManager.Debug($"Spawned UsedItem team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                    foreach (var summonedRole in Plugin.NextTeam.Players)
                    {
                        LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                        summonedRole.AddRole();
                    }

                    LogManager.Debug($"All players for team '{teamToSpawn.Name}' have been assigned roles.");
                });
                Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
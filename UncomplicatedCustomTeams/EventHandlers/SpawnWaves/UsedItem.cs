using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class UsedItem
    {
        public bool IsCustomItem(Item item, int? customItemId)
        {
            if (customItemId == null) return false;
            int itemId = GetCustomItemId(item);

            return itemId == customItemId;
        }

        public int GetCustomItemId(Item item)
        {
            return CustomItem.TryGet(item, out var customItem) ? unchecked((int)customItem.Id) : 0;
        }

        public void OnItemUsed(UsedItemEventArgs ev)
        {
            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(WaveType.UsedItem);

            if (team == null)
            {
                LogManager.Debug("No team with 'UsedItem' spawn condition found.");
                return;
            }

            Team.SpawnData spawnData = team.SpawnConditions;

            ItemType usedItemType = spawnData.GetUsedItemType();
            int? usedCustomItemId = spawnData.GetCustomItemId();

            bool isStandardItem = ev.Item.Type == usedItemType;
            bool isCustomItem = usedCustomItemId.HasValue && IsCustomItem(ev.Item, usedCustomItemId.Value);

            if (!isStandardItem && !isCustomItem)
            {
                LogManager.Debug($"Item {ev.Item.Type} (Custom ID: {GetCustomItemId(ev.Item)}) does not match required item ({spawnData.UsedItem}). Ignoring.");
                return;
            }

            LogManager.Debug($"Player used {ev.Item.Type} (Custom ID: {GetCustomItemId(ev.Item)}), checking for team spawn...");

            UncomplicatedCustomTeams.API.Features.Team selectedTeam = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(spawnData.SpawnWave);

            if (selectedTeam == null) return;

            LogManager.Debug($"EvaluateSpawn found team: {selectedTeam.Name}");

            Timing.CallDelayed(spawnData.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = new();
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                {
                    Bucket.SpawnBucket.Add(player.Id);
                }

                if (Bucket.SpawnBucket.Count == 0) return;

                Plugin.NextTeam = SummonedTeam.Summon(selectedTeam, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned {spawnData.SpawnWave} team for {Bucket.SpawnBucket.Count} players.");

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

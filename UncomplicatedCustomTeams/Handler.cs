using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using System;
using System.Linq;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using static UncomplicatedCustomTeams.API.Features.Team;

namespace UncomplicatedCustomTeams
{
    internal class Handler
    {
        internal Task TeamCleaner;

        internal bool TeamCleanerEnabled = false;

        internal bool ForcedNextWave = false;

        internal bool CustomTeamSpawnedThisWave = false;

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            CustomTeamSpawnedThisWave = false;
            Bucket.SpawnBucket = new();
            foreach (Player Player in ev.Players)
                Bucket.SpawnBucket.Add(Player.Id);

            var allPlayers = ev.Players.ToList();

            if (allPlayers.Count == 0)
            {
                LogManager.Debug("No players available for respawn.");
                return;
            }

            Plugin.NextTeam?.RefreshPlayers(allPlayers);

            if (ForcedNextWave && Plugin.NextTeam is not null)
            {
                ForcedNextWave = false;
                Plugin.NextTeam.RefreshPlayers(allPlayers);
                CustomTeamSpawnedThisWave = true;

                LimitPlayersToCustomTeam(ev);

                LogManager.Debug($"Forced wave executed for {Plugin.NextTeam.Team.Name} with ID {Plugin.NextTeam.Team.Id}");
                return;
            }

            LogManager.Debug($"Respawning team event, let's propose our team\nTeams: {API.Features.Team.List.Count}");
            LogManager.Debug($"Next team for respawn is {ev.NextKnownTeam}");

            string faction = ev.NextKnownTeam switch
            {
                PlayerRoles.Faction.FoundationStaff => "NtfWave",
                PlayerRoles.Faction.FoundationEnemy => "ChaosWave",
                _ => null
            };

            if (faction is null)
            {
                Plugin.NextTeam = null;
                return;
            }

            var team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(faction);

            if (team == null)
            {
                LogManager.Debug("No valid team found in EvaluateSpawn, aborting team selection.");
                Plugin.NextTeam = null;
                return;
            }

            if (team.SpawnConditions?.SpawnWave == faction)
            {
                Plugin.CachedSpawnList = SummonedTeam.CanSpawnTeam(team);

                Plugin.NextTeam = SummonedTeam.Summon(team, Plugin.CachedSpawnList);

                if (Plugin.NextTeam is not null)
                {
                    var allowedIds = Plugin.CachedSpawnList.Select(p => p.Id).ToList();
                    ev.Players.RemoveAll(p => !allowedIds.Contains(p.Id));

                    CustomTeamSpawnedThisWave = true;
                }
            }
            else
            {
                Plugin.NextTeam = null;
                LogManager.Debug("No valid custom team found for this wave.");
            }

            LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
        }

        private void LimitPlayersToCustomTeam(RespawningTeamEventArgs ev)
        {
            if (Plugin.NextTeam is null)
                return;

            var all = ev.Players.ToList();
            int totalMax = Plugin.NextTeam.Team.TeamRoles.Sum(r => r.MaxPlayers);

            if (totalMax <= 0 || totalMax >= all.Count)
                return;

            var sorted = Plugin.NextTeam.Players
                .OrderBy(r => r.CustomRole.Priority)
                .ThenBy(_ => UnityEngine.Random.value)
                .Take(totalMax)
                .Select(r => r.Player.Id)
                .ToHashSet();

            ev.Players.RemoveAll(p => !sorted.Contains(p.Id));
        }

        public void GetThisChaosOutOfHere(AnnouncingChaosEntranceEventArgs ev)
        {
            if (CustomTeamSpawnedThisWave)
            {
                ev.IsAllowed = false;
            }
        }

        public void GetThisNtfOutOfHere(AnnouncingNtfEntranceEventArgs ev)
        {
            if (CustomTeamSpawnedThisWave)
            {
                ev.IsAllowed = false;
            }
        }

        public void OnRoundStarted()
        {
            LogManager.Debug("Round started, checking for RoundStarted spawns...");

            var team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("RoundStarted");
            if (team == null)
            {
                LogManager.Debug("No valid team found for RoundStarted.");
                return;
            }
            team.SpawnCount = 0;

            var requiredRoles = team.SpawnConditions.RoleAliveOnRoundStart;
            if (requiredRoles != null && requiredRoles.Count > 0)
            {
                var aliveRoles = Player.List.Where(p => p.IsAlive).Select(p => p.Role.Type).ToList();
                bool hasRequiredAlive = requiredRoles.Any(r => aliveRoles.Contains(r));

                LogManager.Debug($"Team {team.Name} requires alive roles: [{string.Join(", ", requiredRoles)}]; currently alive: [{string.Join(", ", aliveRoles)}] => {(hasRequiredAlive ? "OK" : "SKIPPED")}");

                if (!hasRequiredAlive)
                {
                    LogManager.Debug("Skipping spawn — required roles not alive.");
                    return;
                }
            }

            LogManager.Debug($"EvaluateSpawn found team: {team.Name}");

            Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = new();
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                {
                    Bucket.SpawnBucket.Add(player.Id);
                }

                if (Bucket.SpawnBucket.Count == 0) return;

                Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned RoundStarted team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }

        public void OnDetonated()
        {
            LogManager.Debug("Warhead detonated, checking for AfterWarhead spawns...");

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("AfterWarhead");

            if (team == null) return;

            LogManager.Debug($"EvaluateSpawn found team: {team.Name}");

            Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = new();
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                    Bucket.SpawnBucket.Add(player.Id);

                if (Bucket.SpawnBucket.Count == 0) return;
                Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned AfterWarhead team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }

        public void OnPlayerDying(DyingEventArgs ev)
        {
            if (ev.Player == null || !ev.Player.IsScp)
                return;

            LogManager.Debug($"{ev.Player.Role.Type} is dying, checking for ScpDeath spawn condition...");

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("ScpDeath");
            if (team == null)
            {
                LogManager.Debug("No valid team found with ScpDeath condition.");
                return;
            }

            var spawnData = team.SpawnConditions;
            if (Enum.TryParse(spawnData.TargetScp, true, out RoleTypeId targetRole) && targetRole != RoleTypeId.None)
            {
                if (targetRole != ev.Player.Role.Type)
                    return;
            }
            else if (spawnData.TargetScp.Equals("SCPs", StringComparison.OrdinalIgnoreCase))
            {
                if (ev.Player.Role.Team != PlayerRoles.Team.SCPs)
                    return;
            }
            else
            {
                LogManager.Debug($"Invalid TargetScp value: {spawnData.TargetScp}. Only valid RoleTypeIds or 'SCPs' as Team are allowed.");
                return;
            }

            LogManager.Debug($"ScpDeath spawn condition met. Team to be spawned: {team.Name}");

            Timing.CallDelayed(spawnData.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = new();
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                    Bucket.SpawnBucket.Add(player.Id);

                if (Bucket.SpawnBucket.Count == 0) return;

                Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned ScpDeath team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }

        public void OnDecontaminating(DecontaminatingEventArgs ev)
        {
            LogManager.Debug("Decontamination in progress, checking for AfterDecontamination spawns...");

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("AfterDecontamination");

            if (team == null) return;

            LogManager.Debug($"EvaluateSpawn found team: {team.Name}");

            Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = new();
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                    Bucket.SpawnBucket.Add(player.Id);

                if (Bucket.SpawnBucket.Count == 0) return;
                Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned AfterDecontamination team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }

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
            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.List.FirstOrDefault(t => t.SpawnConditions.SpawnWave == "UsedItem");

            if (team == null)
            {
                LogManager.Debug("No team with 'UsedItem' spawn condition found.");
                return;
            }

            SpawnData spawnData = team.SpawnConditions;

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
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
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

        public void OnVerified(VerifiedEventArgs ev)
        {
            if (ev.Player == null)
                return;

            if (!Round.IsStarted)
                return;

            if (!Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player.Nickname} is verified, adding to spawn bucket.");
                Bucket.SpawnBucket.Add(ev.Player.Id);
            }
            SummonedTeam.CanSpawnTeam(null);
        }

        public void OnDestroying(DestroyingEventArgs ev)
        {
            if (ev.Player == null)
                return;

            if (!Round.IsStarted)
                return;

            if (Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player.Nickname} is being destroyed, removing from spawn bucket.");
                Bucket.SpawnBucket.Remove(ev.Player.Id);
            }
            SummonedTeam.CanSpawnTeam(null);
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (Plugin.NextTeam is not null && Bucket.SpawnBucket.Contains(ev.Player.Id) && Plugin.NextTeam.Team != null)
            {
                Bucket.SpawnBucket.Remove(ev.Player.Id);

                ev.IsAllowed = false;

                if (Plugin.CachedSpawnList.Contains(ev.Player))
                {
                    LogManager.Debug($"Spawning custom team role for {ev.Player.Nickname} ({ev.Player.Id})");
                    Timing.CallDelayed(0.1f, () => { Plugin.NextTeam.TrySpawnPlayer(ev.Player, ev.NewRole); });
                }
                else
                {
                    LogManager.Debug($"Skipping respawn for {ev.Player.Nickname} ({ev.Player.Id}), not selected for spawn.");
                }

                if (!TeamCleanerEnabled)
                {
                    TeamCleanerEnabled = true;
                    TeamCleaner = Task.Run(async () =>
                    {
                        await Task.Delay(2500);
                        Plugin.NextTeam = null;
                        CustomTeamSpawnedThisWave = false;
                        TeamCleanerEnabled = false;
                    });
                }
            }

            Timing.CallDelayed(0.2f, () =>
            {
                SummonedTeam.CheckRoundEndCondition();

                foreach (var team in SummonedTeam.List)
                {
                    team.CheckPlayers();
                }
            });
        }
    }
}
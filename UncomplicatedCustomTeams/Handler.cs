using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using System.Collections.Generic;
using Exiled.Events.EventArgs.Map;
using System.Linq;
using PlayerRoles;
using static UncomplicatedCustomTeams.API.Features.Team;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using System;

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

            Plugin.NextTeam?.RefreshPlayers(ev.Players);
            if (ForcedNextWave && Plugin.NextTeam is not null)
            {
                ForcedNextWave = false;
                Plugin.NextTeam.RefreshPlayers(ev.Players);
                CustomTeamSpawnedThisWave = true;
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
                Plugin.NextTeam = null; // no team
                return;
            }

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(faction);
            if (team == null)
            {
                LogManager.Debug("No valid team found in EvaluateSpawn, aborting team selection.");
                Plugin.NextTeam = null;
            }
            else if (team.SpawnConditions?.SpawnWave == faction)
            {
                Plugin.NextTeam = SummonedTeam.Summon(team, ev.Players);
                CustomTeamSpawnedThisWave = Plugin.NextTeam != null;
                LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
            }
            else
            {
                Plugin.NextTeam = null;
                LogManager.Debug("No valid custom team found for this wave.");
            }
            LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
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

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("RoundStarted");

            if (team == null)
            {
                LogManager.Debug("No valid team found for RoundStarted.");
                return;
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
                LogManager.Debug($"Player {ev.Player} is changing role, let's do something to it! v2");
                Bucket.SpawnBucket.Remove(ev.Player.Id);

                List<Player> selectedPlayers = SummonedTeam.CanSpawnTeam(Plugin.NextTeam.Team);

                if (selectedPlayers.Contains(ev.Player))
                {
                    Timing.CallDelayed(0.1f, () => { Plugin.NextTeam.TrySpawnPlayer(ev.Player, ev.NewRole); });
                    ev.IsAllowed = false;
                }
                else
                {
                    LogManager.Debug($"Skipping respawn for {ev.Player.Nickname}, not selected for spawn.");
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
            Timing.CallDelayed(0.2f, () => SummonedTeam.CheckRoundEndCondition());
        }
    }
}
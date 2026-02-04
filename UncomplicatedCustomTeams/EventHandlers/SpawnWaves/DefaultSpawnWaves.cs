using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class DefaultSpawnWaves
    {
        public static bool ForcedNextWave = false;
        public static bool IgnoreSpawnChance = true;
        public static bool ForceAnyCustomTeam = false;
        public static bool CustomTeamSpawnedThisWave = false;

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            CustomTeamSpawnedThisWave = false;
            Bucket.SpawnBucket = [];
            foreach (Player player in ev.Players)
                Bucket.SpawnBucket.Add(player.Id);

            var allPlayers = ev.Players.ToList();

            if (allPlayers.Count == 0)
            {
                LogManager.Debug("No players available for respawn.");
                return;
            }

            WaveType faction = ev.NextKnownTeam switch
            {
                PlayerRoles.Faction.FoundationStaff => WaveType.NtfWave,
                PlayerRoles.Faction.FoundationEnemy => WaveType.ChaosWave,
                _ => WaveType.None
            };

            if (ForceAnyCustomTeam && faction != WaveType.None)
            {
                ForceAnyCustomTeam = false;
                var availableTeams = Team.List.Where(t => t.SpawnConditions.SpawnWave == faction).ToList();

                if (availableTeams.Count > 0)
                {
                    var randomTeam = availableTeams[UnityEngine.Random.Range(0, availableTeams.Count)];
                    LogManager.Info($"Randomly selected custom team '{randomTeam.Name}' for forced spawn.");

                    Plugin.NextTeam = new SummonedTeam(randomTeam);
                    ForcedNextWave = true;
                    IgnoreSpawnChance = true;
                }
                else
                {
                    LogManager.Warn($"No Custom Teams found for wave {faction}. Reverting to vanilla.");
                }
            }

            Plugin.NextTeam?.RefreshPlayers(allPlayers);

            if (ForcedNextWave && Plugin.NextTeam is not null)
            {
                bool shouldSpawn = true;

                if (Plugin.NextTeam.Team.SpawnConditions.SpawnWave != faction)
                {
                    LogManager.Warn($"Forced team '{Plugin.NextTeam.Team.Name}' wave mismatch ({Plugin.NextTeam.Team.SpawnConditions.SpawnWave} vs {faction}). Aborting force.");
                    shouldSpawn = false;
                }
                else if (!IgnoreSpawnChance)
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    if (roll >= Plugin.NextTeam.Team.SpawnChance)
                    {
                        LogManager.Info($"Forced wave for '{Plugin.NextTeam.Team.Name}' failed RNG roll. Reverting to standard check.");
                        shouldSpawn = false;
                    }
                }

                if (shouldSpawn)
                {
                    ForcedNextWave = false;
                    CustomTeamSpawnedThisWave = true;
                    LimitPlayersToCustomTeam(ev);
                    LogManager.Debug($"Forced wave executed for {Plugin.NextTeam.Team.Name}");
                    return;
                }
                else
                {
                    ForcedNextWave = false;
                    Plugin.NextTeam = null;
                }
            }

            if (faction is WaveType.None)
            {
                Plugin.NextTeam = null;
                return;
            }

            List<Team> teamsToSpawn = Team.EvaluateSpawn(faction);

            if (!teamsToSpawn.Any())
            {
                Plugin.NextTeam = null;
                return;
            }

            var team = teamsToSpawn.First();

            if (team != null)
            {
                Plugin.CachedSpawnList = SummonedTeam.CanSpawnTeam(team);
                Plugin.NextTeam = SummonedTeam.Summon(team, Plugin.CachedSpawnList);

                if (Plugin.NextTeam is not null)
                {
                    CustomTeamSpawnedThisWave = true;
                    LimitPlayersToCustomTeam(ev);
                }
            }
            else
            {
                Plugin.NextTeam = null;
            }
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
    }
}
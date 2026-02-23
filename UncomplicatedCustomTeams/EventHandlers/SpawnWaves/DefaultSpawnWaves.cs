using LabApi.Events.Arguments.ServerEvents;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Events.EventArgs;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class DefaultSpawnWaves
    {
        public static bool ForceAnyCustomTeam = false;
        public static bool ForcedNextWave = false;
        public static Team NextWaveDefinition = null;
        public static bool IgnoreSpawnChance = false;
        public static bool CustomTeamSpawnedThisWave = false;

        public void OnRespawningTeam(WaveRespawningEventArgs ev)
        {
            CustomTeamSpawnedThisWave = false;
            if (ev.SpawningPlayers.Count() == 0) return;

            WaveType faction = ev.Wave.Faction switch
            {
                PlayerRoles.Faction.FoundationStaff => WaveType.NtfWave,
                PlayerRoles.Faction.FoundationEnemy => WaveType.ChaosWave,
                _ => WaveType.None
            };

            if (faction == WaveType.None) return;

            Team selectedTeam = null;

            if (ForcedNextWave && NextWaveDefinition != null)
            {
                if (NextWaveDefinition.SpawnConditions.SpawnWave == faction || NextWaveDefinition.SpawnConditions.SpawnOnBothWaves)
                {
                    if (IgnoreSpawnChance || new System.Random().Next(0, 100) < NextWaveDefinition.SpawnChance)
                    {
                        selectedTeam = NextWaveDefinition;
                    }
                    else
                    {
                        LogManager.Debug($"Forced team {NextWaveDefinition.Name} failed spawn roll (Chance: {NextWaveDefinition.SpawnChance}%).");
                    }
                }
                else
                {
                    LogManager.Warn($"Forced team {NextWaveDefinition.Name} (Wave: {NextWaveDefinition.SpawnConditions.SpawnWave}) does not match current respawn faction: {faction}.");
                }

                ForcedNextWave = false;
                NextWaveDefinition = null;
                IgnoreSpawnChance = false;
            }
            else if (ForceAnyCustomTeam)
            {
                ForceAnyCustomTeam = false;
                var available = Team.List.Where(t => t.SpawnConditions.SpawnWave == faction || t.SpawnConditions.SpawnOnBothWaves).ToList();
                if (available.Any())
                    selectedTeam = available[UnityEngine.Random.Range(0, available.Count)];
            }
            else
            {
                var candidates = Team.List.Where(t => t.SpawnConditions.SpawnWave == faction || t.SpawnConditions.SpawnOnBothWaves).ToList();
                foreach (var team in candidates)
                {
                    if (team.MaxSpawns != -1 && team.CurrentSpawnCount >= team.MaxSpawns) continue;

                    uint currentSpawnChance = team.SpawnChance;

                    if (team.SpawnConditions.SpawnOnBothWaves)
                    {
                        if (faction == WaveType.NtfWave && team.SpawnConditions.SpawnChanceNtf >= 0)
                        {
                            currentSpawnChance = (uint)team.SpawnConditions.SpawnChanceNtf;
                        }
                        else if (faction == WaveType.ChaosWave && team.SpawnConditions.SpawnChanceChaos >= 0)
                        {
                            currentSpawnChance = (uint)team.SpawnConditions.SpawnChanceChaos;
                        }
                    }

                    if (new System.Random().Next(0, 100) < currentSpawnChance)
                    {
                        selectedTeam = team;
                        if (!team.AllowConcurrentSpawns) break;
                    }
                }
            }

            if (selectedTeam == null) return;

            var playersToSpawn = ev.SpawningPlayers.ToList();

            var spawnEv = new TeamSpawningEventArgs(selectedTeam, playersToSpawn);
            UCTEvents.InvokeTeamSpawning(spawnEv);

            if (!spawnEv.IsAllowed || spawnEv.PlayersToSpawn.Count == 0) return;

            var summonedTeam = SummonedTeam.Create(selectedTeam, spawnEv.PlayersToSpawn);
            LogManager.Info($"Replaced {faction} with custom team: {selectedTeam.Name}");

            CustomTeamSpawnedThisWave = true;

            foreach (var member in summonedTeam.Members)
            {
                if (ev.Roles.ContainsKey(member.Player))
                {
                    ev.Roles.Remove(member.Player);
                }
            }
        }
    }
}
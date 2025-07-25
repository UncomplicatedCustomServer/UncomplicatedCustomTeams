using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    public class DefaultSpawnWaves
    {
        public static bool ForcedNextWave = false;

        public static bool CustomTeamSpawnedThisWave = false;

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            CustomTeamSpawnedThisWave = false;
            Bucket.SpawnBucket = new();
            foreach (Player player in ev.Players)
                Bucket.SpawnBucket.Add(player.Id);

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

            WaveType faction = ev.NextKnownTeam switch
            {
                PlayerRoles.Faction.FoundationStaff => WaveType.NtfWave,
                PlayerRoles.Faction.FoundationEnemy => WaveType.ChaosWave,
                _ => WaveType.None
            };

            if (faction is WaveType.None)
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
    }
}

using Exiled.API.Features;
using MEC;
using PlayerRoles;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class TeamDependent
    {
        public void CheckForTeamElimination(SummonedTeam teamOfPlayer)
        {
            if (teamOfPlayer == null) return;

            Timing.CallDelayed(0.1f, () =>
            {
                if (teamOfPlayer.IsTeamEliminated())
                {
                    LogManager.Debug($"Team '{teamOfPlayer.Team.Name}' (ID: {teamOfPlayer.Team.Id}) has been eliminated. Checking for dependent teams (AfterTeamDeath)...");
                    TrySpawnDependentTeams(teamOfPlayer.Team.Id, isDeathTrigger: true);
                }
            });
        }

        public void OnTeamSpawned(SummonedTeam spawnedTeam)
        {
            LogManager.Debug($"Team '{spawnedTeam.Team.Name}' (ID: {spawnedTeam.Team.Id}) has spawned. Checking for dependent teams (AfterTeamSpawn)...");
            TrySpawnDependentTeams(spawnedTeam.Team.Id, isDeathTrigger: false);
        }

        private void TrySpawnDependentTeams(uint triggerTeamId, bool isDeathTrigger)
        {
            string triggerType = isDeathTrigger ? "AfterTeamDeath" : "AfterTeamSpawn";

            var teamsToSpawn = Team.List.Where(t =>
                t.SpawnConditions.SpawnWave == WaveType.TeamDependent &&
                (isDeathTrigger ? t.SpawnConditions.AfterTeamDeath == triggerTeamId : t.SpawnConditions.AfterTeamSpawn == triggerTeamId)
            ).ToList();

            if (!teamsToSpawn.Any())
            {
                LogManager.Debug($"No dependent teams found for trigger ID {triggerTeamId} on event {triggerType}.");
                return;
            }

            foreach (var team in teamsToSpawn)
            {
                LogManager.Debug($"Found dependent team '{team.Name}' for trigger ID {triggerTeamId}. Attempting to spawn...");
                SpawnTeam(team);
            }
        }

        private void SpawnTeam(Team team)
        {
            CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                Bucket.SpawnBucket = [];
                foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                    Bucket.SpawnBucket.Add(player.Id);

                if (Bucket.SpawnBucket.Count == 0)
                {
                    LogManager.Warn($"Could not spawn dependent team '{team.Name}' because no players were available in spectator.");
                    return;
                }

                Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned dependent team: {Plugin.NextTeam.Team.Name} for {Plugin.NextTeam.Players.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug($"All players for dependent team '{Plugin.NextTeam.Team.Name}' have been assigned roles.");
            });
            Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
        }
    }
}

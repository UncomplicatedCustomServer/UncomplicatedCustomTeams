using MEC;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Events.EventArgs;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class TeamDependent
    {
        public void OnTeamEliminated(TeamEliminatedEventArgs ev)
        {
            var team = ev.SummonedTeam;
            if (team == null) return;

            Timing.CallDelayed(0.1f, () =>
            {
                LogManager.Debug($"Team '{team.Definition.Name}' eliminated. Checking AfterTeamDeath dependencies...");
                TrySpawnDependentTeams(team.Definition.Id, isDeathTrigger: true);
            });
        }

        public void OnTeamSpawned(TeamSpawnedEventArgs ev)
        {
            var team = ev.SummonedTeam;
            if (team == null) return;

            LogManager.Debug($"Team '{team.Definition.Name}' spawned. Checking AfterTeamSpawn dependencies...");
            TrySpawnDependentTeams(team.Definition.Id, isDeathTrigger: false);
        }

        private void TrySpawnDependentTeams(uint triggerTeamId, bool isDeathTrigger)
        {
            string triggerType = isDeathTrigger ? "AfterTeamDeath" : "AfterTeamSpawn";

            var teamsToSpawn = Team.List.Where(t =>
                t.SpawnConditions.SpawnWave == WaveType.TeamDependent &&
                (isDeathTrigger ? t.SpawnConditions.AfterTeamDeath == triggerTeamId : t.SpawnConditions.AfterTeamSpawn == triggerTeamId)
            ).ToList();

            if (!teamsToSpawn.Any()) return;

            foreach (var team in teamsToSpawn)
            {
                SpawnTeam(team);
            }
        }

        private void SpawnTeam(Team team)
        {
            Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
            {
                var spawned = TeamSpawner.SpawnSpecificTeam(team);

                if (spawned != null)
                {
                    LogManager.Debug($"Dependent Team Spawned: {team.Name}");
                }
                else
                {
                    LogManager.Warn($"Dependent Team '{team.Name}' failed to spawn.");
                }
            });
        }
    }
}
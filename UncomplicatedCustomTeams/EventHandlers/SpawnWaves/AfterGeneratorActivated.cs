using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using MEC;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class AfterGeneratorActivated
    {
        private static List<Generator> EngagedGenerators { get; set; } = [];
        public void OnGeneratorActivating(GeneratorActivatingEventArgs _)
        {
            EngagedGenerators = [.. Generator.List.Where(g => g.IsEngaged)];
            int engagedCount = EngagedGenerators.Count;

            LogManager.Debug($"Generator engaged. Current engaged count: {engagedCount}. Checking for spawns...");
            List<Team> potentialTeams = Team.EvaluateSpawn(WaveType.AfterGeneratorActivated);

            if (!potentialTeams.Any())
            {
                return;
            }

            List<Team> teamsToSpawn = [.. potentialTeams.Where(team => engagedCount >= team.SpawnConditions.RequiredEngagedGenerators)];

            if (!teamsToSpawn.Any())
            {
                LogManager.Debug("Found potential teams, but none met the RequiredEngagedGenerators condition.");
                return;
            }

            LogManager.Debug($"Found {teamsToSpawn.Count} team(s) that meet all conditions.");

            foreach (Team team in teamsToSpawn)
            {
                CoroutineHandle handle = Timing.CallDelayed(team.SpawnConditions.SpawnDelay, () =>
                {
                    Bucket.SpawnBucket = [];
                    foreach (Player player in Player.List.Where(p => !p.IsAlive && p.Role.Type == PlayerRoles.RoleTypeId.Spectator && !p.IsOverwatchEnabled))
                        Bucket.SpawnBucket.Add(player.Id);

                    if (Bucket.SpawnBucket.Count == 0) return;

                    Plugin.NextTeam = SummonedTeam.Summon(team, Player.List.Where(p => Bucket.SpawnBucket.Contains(p.Id)));

                    if (Plugin.NextTeam == null) return;

                    LogManager.Debug($"Spawning team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                    foreach (var summonedRole in Plugin.NextTeam.Players)
                    {
                        summonedRole.AddRole();
                    }

                    LogManager.Debug($"All players for team '{team.Name}' have been assigned roles.");
                });

                Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}

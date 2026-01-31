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
    internal class AfterDecontamination
    {
        public void OnDecontaminating(DecontaminatingEventArgs ev)
        {
            LogManager.Debug("Decontamination in progress, checking for AfterDecontamination spawns...");

            List<Team> teamsToSpawn = Team.EvaluateSpawn(WaveType.AfterDecontamination);

            if (!teamsToSpawn.Any())
            {
                return;
            }

            LogManager.Debug($"EvaluateSpawns found {teamsToSpawn.Count} team(s) to spawn.");

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

                    LogManager.Debug($"Spawned AfterDecontamination team: {Plugin.NextTeam.Team.Name} for {Bucket.SpawnBucket.Count} players.");

                    foreach (var summonedRole in Plugin.NextTeam.Players)
                    {
                        LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                        summonedRole.AddRole();
                    }

                    LogManager.Debug($"All players for team '{team.Name}' have been assigned roles.");
                });
                Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
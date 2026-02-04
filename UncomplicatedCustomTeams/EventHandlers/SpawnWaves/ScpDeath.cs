using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using Team = UncomplicatedCustomTeams.API.Features.Team;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class ScpDeath
    {
        public void OnScpDying(DyingEventArgs ev)
        {
            if (ev.Player == null || !ev.Player.IsScp)
                return;

            LogManager.Debug($"{ev.Player.Role.Type} is dying, checking for ScpDeath spawn condition...");

            List<Team> potentialTeams = Team.EvaluateSpawn(WaveType.ScpDeath);

            if (!potentialTeams.Any())
            {
                LogManager.Debug("No valid team found with ScpDeath condition.");
                return;
            }

            foreach (var team in potentialTeams)
            {
                var spawnData = team.SpawnConditions;
                bool conditionMet = false;

                if (Enum.TryParse(spawnData.TargetScp, true, out RoleTypeId targetRole) && targetRole != RoleTypeId.None)
                {
                    if (targetRole == ev.Player.Role.Type)
                        conditionMet = true;
                }
                else if (spawnData.TargetScp.Equals("SCPs", StringComparison.OrdinalIgnoreCase))
                {
                    if (ev.Player.Role.Team == PlayerRoles.Team.SCPs)
                    {
                        if (!spawnData.IsScp0492CountedAsScp && ev.Player.Role.Type == RoleTypeId.Scp0492)
                        {
                            LogManager.Debug($"Scp049-2 death is ignored for team '{team.Name}'.");
                        }
                        else
                        {
                            conditionMet = true;
                        }
                    }
                }

                if (!conditionMet)
                    continue;

                LogManager.Debug($"ScpDeath spawn condition met. Team to be spawned: {team.Name}");

                CoroutineHandle handle = Timing.CallDelayed(spawnData.SpawnDelay, () =>
                {
                    Bucket.SpawnBucket = [];
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

                    LogManager.Debug($"All players for team '{team.Name}' have been assigned roles.");
                });
                Plugin.Instance.Handler.ActiveSpawnDelays.Add(handle);
            }
        }
    }
}
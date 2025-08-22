using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using System;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.EventHandlers.SpawnWaves
{
    internal class ScpDeath
    {
        public void OnScpDying(DyingEventArgs ev)
        {
            if (ev.Player == null || !ev.Player.IsScp)
                return;

            LogManager.Debug($"{ev.Player.Role.Type} is dying, checking for ScpDeath spawn condition...");

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(WaveType.ScpDeath);
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
    }
}

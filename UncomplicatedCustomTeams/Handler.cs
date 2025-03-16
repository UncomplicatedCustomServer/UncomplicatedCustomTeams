using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using MEC;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using Exiled.Events.EventArgs.Warhead;
using System.Collections.Generic;
using Respawning.Announcements;
using Exiled.Events.EventArgs.Map;
using System.Linq;
using UncomplicatedCustomRoles.API.Features;
using PlayerRoles;

namespace UncomplicatedCustomTeams
{
    internal class Handler
    {
        internal Task TeamCleaner;

        internal bool TeamCleanerEnabled = false;

        internal bool ForcedNextWave = false;

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            Bucket.SpawnBucket = new();
            foreach (Player Player in ev.Players)
                Bucket.SpawnBucket.Add(Player.Id);

            Plugin.NextTeam?.RefreshPlayers(ev.Players);

            if (ForcedNextWave)
            {
                LogManager.Debug("Can't spawn this wave because the wave has been forced!");
                ForcedNextWave = false;
                return;
            }

            LogManager.Debug($"Respawning team event, let's propose our team\nTeams: {API.Features.Team.List.Count}");

            LogManager.Debug($"Next team for respawn is {ev.NextKnownTeam}");

            string faction = ev.NextKnownTeam switch
            {
                PlayerRoles.Faction.FoundationStaff => "NtfWave",
                PlayerRoles.Faction.FoundationEnemy => "ChaosWave",
                _ => "None"
            };

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn(faction) ?? new UncomplicatedCustomTeams.API.Features.Team();

            if (team is null)
                Plugin.NextTeam = null; // No next team
            else
            {
                Plugin.NextTeam = SummonedTeam.Summon(team, ev.Players);
            }

            LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
        }

        public void GetThisChaosOutOfHere(AnnouncingChaosEntranceEventArgs ev)
        {
            if (SummonedTeam.List.Any())
            {
                ev.IsAllowed = false;
            }
        }

        public void GetThisNtfOutOfHere(AnnouncingNtfEntranceEventArgs ev)
        {
            if (SummonedTeam.List.Any())
            {
                ev.IsAllowed = false;
            }
        }

        public void OnDetonated()
        {
            LogManager.Debug("Warhead detonated, checking for AfterWarhead spawns...");

            UncomplicatedCustomTeams.API.Features.Team team = UncomplicatedCustomTeams.API.Features.Team.EvaluateSpawn("AfterWarhead");

            if (team == null) return;

            LogManager.Debug($"EvaluateSpawn found team: {team.Name}");

            Timing.CallDelayed(team.spawnConditions.Offset, () =>
            {
                var spectators = Player.List.Where(p => p.Role == RoleTypeId.Spectator).ToList();

                if (spectators.Count == 0) return;

                Plugin.NextTeam = SummonedTeam.Summon(team, spectators);

                if (Plugin.NextTeam == null) return;

                LogManager.Debug($"Spawned AfterWarhead team: {Plugin.NextTeam.Team.Name} for {spectators.Count} players.");

                foreach (var summonedRole in Plugin.NextTeam.Players)
                {
                    LogManager.Debug($"Assigning role to {summonedRole.Player.Nickname} ({summonedRole.Player.Id})...");
                    summonedRole.AddRole();
                }

                LogManager.Debug("All players have been assigned roles.");
            });
        }



        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (Plugin.NextTeam is not null && Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player} is changing role, let's do something to it! v2");
                Bucket.SpawnBucket.Remove(ev.Player.Id);
                Timing.CallDelayed(0.1f, () => { Plugin.NextTeam.TrySpawnPlayer(ev.Player, ev.NewRole); });
                ev.IsAllowed = false;

                if (!TeamCleanerEnabled)
                {
                    Cassie.MessageTranslated(Plugin.NextTeam.Team.CassieMessage, Plugin.NextTeam.Team.CassieTranslation);

                    TeamCleanerEnabled = true;
                    TeamCleaner = Task.Run(async () =>
                    {
                        await Task.Delay(2500);
                        Plugin.NextTeam = null;
                        TeamCleanerEnabled = false;
                    });
                }
            }
            Timing.CallDelayed(0.2f, () => SummonedTeam.CheckRoundEndCondition());
        }
    }
}
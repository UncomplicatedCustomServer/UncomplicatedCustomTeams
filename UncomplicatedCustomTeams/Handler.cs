using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine.UIElements;

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

            // Evaluate the team
            Team SpawnTeam = Team.EvaluateSpawn((SpawnableFaction)ev.NextKnownTeam);

            if (SpawnTeam is null)
                Plugin.NextTeam = null; // No next team
            else
            {
                Plugin.NextTeam = SummonedTeam.Summon(SpawnTeam, ev.Players);
                UncomplicatedCustomRoles.API.Features.Spawn.DisableSpawnWave();
            }

            LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
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
        }
    }
}
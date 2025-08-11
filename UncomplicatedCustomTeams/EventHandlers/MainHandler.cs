using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using MEC;
using System.Collections.Generic;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.EventHandlers.SpawnWaves;
using UncomplicatedCustomTeams.Utilities;
using MapHandler = Exiled.Events.Handlers.Map;
using PlayerHandler = Exiled.Events.Handlers.Player;
using ServerHandler = Exiled.Events.Handlers.Server;
using WarheadHandler = Exiled.Events.Handlers.Warhead;

namespace UncomplicatedCustomTeams
{
    internal class MainHandler
    {
        internal Task TeamCleaner;

        internal bool TeamCleanerEnabled = false;

        public AfterDecontamination afterDecontamination;
        public AfterWarhead afterWarhead;
        public DefaultSpawnWaves DefaultSpawnWaves;
        public RoundStarted RoundStarted;
        public ScpDeath ScpDeath;
        public UsedItem UsedItem;

        public MainHandler()
        {
            afterDecontamination = new AfterDecontamination();
            afterWarhead = new AfterWarhead();
            DefaultSpawnWaves = new DefaultSpawnWaves();
            RoundStarted = new RoundStarted();
            ScpDeath = new ScpDeath();
            UsedItem = new UsedItem();
        }

        public void SubscribeToSpawnWaves()
        {
            MapHandler.Decontaminating += afterDecontamination.OnDecontaminating;
            WarheadHandler.Detonated += afterWarhead.OnDetonated;
            ServerHandler.RespawningTeam += DefaultSpawnWaves.OnRespawningTeam;
            ServerHandler.RoundStarted += RoundStarted.OnRoundStarted;
            PlayerHandler.Dying += ScpDeath.OnScpDying;
            PlayerHandler.UsedItem += UsedItem.OnItemUsed;
        }

        public void UnsubscribeToSpawnWaves()
        {
            MapHandler.Decontaminating -= afterDecontamination.OnDecontaminating;
            WarheadHandler.Detonated -= afterWarhead.OnDetonated;
            ServerHandler.RespawningTeam -= DefaultSpawnWaves.OnRespawningTeam;
            ServerHandler.RoundStarted -= RoundStarted.OnRoundStarted;
            PlayerHandler.Dying -= ScpDeath.OnScpDying;
            PlayerHandler.UsedItem -= UsedItem.OnItemUsed;
        }

        public void GetThisChaosOutOfHere(AnnouncingChaosEntranceEventArgs ev) // Don't take this seriously
        {
            if (DefaultSpawnWaves.CustomTeamSpawnedThisWave)
            {
                ev.IsAllowed = false;
            }
        }

        public void GetThisNtfOutOfHere(AnnouncingNtfEntranceEventArgs ev) // Don't take this seriously
        {
            if (DefaultSpawnWaves.CustomTeamSpawnedThisWave)
            {
                ev.IsAllowed = false;
            }
        }

        public void OnVerified(VerifiedEventArgs ev)
        {
            if (ev.Player == null)
                return;

            if (!Round.IsStarted)
                return;

            if (!Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player.Nickname} is verified, adding to spawn bucket.");
                Bucket.SpawnBucket.Add(ev.Player.Id);
            }
            SummonedTeam.CanSpawnTeam(null);
        }

        public void OnDestroying(DestroyingEventArgs ev)
        {
            if (ev.Player == null)
                return;

            if (!Round.IsStarted)
                return;

            if (Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player.Nickname} is being destroyed, removing from spawn bucket.");
                Bucket.SpawnBucket.Remove(ev.Player.Id);
            }
            SummonedTeam.CanSpawnTeam(null);
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (Plugin.NextTeam is not null && Bucket.SpawnBucket.Contains(ev.Player.Id) && Plugin.NextTeam.Team != null)
            {
                Bucket.SpawnBucket.Remove(ev.Player.Id);

                ev.IsAllowed = false;

                if (Plugin.CachedSpawnList.Contains(ev.Player))
                {
                    LogManager.Debug($"Spawning custom team role for {ev.Player.Nickname} ({ev.Player.Id})");
                    Timing.CallDelayed(0.1f, () => { Plugin.NextTeam.TrySpawnPlayer(ev.Player, ev.NewRole); });
                }
                else
                {
                    LogManager.Debug($"Skipping respawn for {ev.Player.Nickname} ({ev.Player.Id}), not selected for spawn.");
                }

                if (!TeamCleanerEnabled)
                {
                    TeamCleanerEnabled = true;
                    TeamCleaner = Task.Run(async () =>
                    {
                        await Task.Delay(2500);
                        Plugin.NextTeam = null;
                        DefaultSpawnWaves.CustomTeamSpawnedThisWave = false;
                        TeamCleanerEnabled = false;
                    });
                }
            }

            Timing.CallDelayed(0.2f, () =>
            {
                SummonedTeam.CheckRoundEndCondition();

                List<SummonedTeam> teamsToRemove = [];

                foreach (var team in SummonedTeam.List)
                {
                    if (team.IsTeamEliminated())
                    {
                        LogManager.Debug($"Team {team.Team.Name} has been eliminated. Scheduling for removal.");
                        teamsToRemove.Add(team);
                    }
                }

                foreach (var team in teamsToRemove)
                {
                    team.Destroy();
                }
            });
        }
    }
}
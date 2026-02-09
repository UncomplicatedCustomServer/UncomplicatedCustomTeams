using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Events.EventArgs;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.EventHandlers.SpawnWaves;
using UncomplicatedCustomTeams.Utilities;
using PlayerHandler = LabApi.Events.Handlers.PlayerEvents;
using ServerHandler = LabApi.Events.Handlers.ServerEvents;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;
using WarheadHandler = LabApi.Events.Handlers.WarheadEvents;

namespace UncomplicatedCustomTeams
{
    internal class MainHandler
    {
        public List<CoroutineHandle> ActiveSpawnDelays { get; } = [];

        public AfterDecontamination afterDecontamination = new();
        public AfterWarhead afterWarhead = new();
        public DefaultSpawnWaves DefaultSpawnWaves = new();
        public RoundStarted RoundStarted = new();
        public ScpDeath ScpDeath = new();
        public UsedItem UsedItem = new();
        public TeamDependent TeamDependent = new();
        public AfterGeneratorActivated AfterGeneratorActivated = new();

        public void SubscribeToSpawnWaves()
        {
            ServerHandler.LczDecontaminationStarting += afterDecontamination.OnDecontaminating;
            WarheadHandler.Detonated += afterWarhead.OnDetonated;
            ServerHandler.WaveRespawning += DefaultSpawnWaves.OnRespawningTeam;
            ServerHandler.RoundStarted += RoundStarted.OnRoundStarted;
            PlayerHandler.Dying += ScpDeath.OnScpDying;
            PlayerHandler.UsedItem += UsedItem.OnItemUsed;
            ServerHandler.GeneratorActivating += AfterGeneratorActivated.OnGeneratorActivating;

            UCTEvents.TeamSpawned += TeamDependent.OnTeamSpawned;
            UCTEvents.TeamEliminated += TeamDependent.OnTeamEliminated;
        }

        public void UnsubscribeToSpawnWaves()
        {
            ServerHandler.LczDecontaminationStarting -= afterDecontamination.OnDecontaminating;
            WarheadHandler.Detonated -= afterWarhead.OnDetonated;
            ServerHandler.WaveRespawning -= DefaultSpawnWaves.OnRespawningTeam;
            ServerHandler.RoundStarted -= RoundStarted.OnRoundStarted;
            PlayerHandler.Dying -= ScpDeath.OnScpDying;
            PlayerHandler.UsedItem -= UsedItem.OnItemUsed;
            ServerHandler.GeneratorActivating -= AfterGeneratorActivated.OnGeneratorActivating;

            UCTEvents.TeamSpawned -= TeamDependent.OnTeamSpawned;
            UCTEvents.TeamEliminated -= TeamDependent.OnTeamEliminated;
        }

        public void OnRestartingRound()
        {
            LogManager.Debug("Round restarting. Cleaning up...");
            foreach (var handle in ActiveSpawnDelays) Timing.KillCoroutines(handle);
            ActiveSpawnDelays.Clear();

            foreach (var team in Team.List) team.CurrentSpawnCount = 0;
            SummonedTeam.List.Clear();
        }

        public void OnEndingRound(RoundEndingEventArgs ev)
        {
            var activeCustomTeams = SummonedTeam.List.Where(st => st.Members.Any(m => m.Player.IsAlive)).ToList();

            if (activeCustomTeams.Count == 0) return;

            bool shouldRoundEnd = false;
            RoundSummary.LeadingTeam winner = RoundSummary.LeadingTeam.Draw;

            foreach (var summonedTeam in activeCustomTeams)
            {
                var rules = summonedTeam.Definition.WinCondition;

                if (rules.PreventRoundEndIfAlive)
                {
                    ev.IsAllowed = false;
                }

                var enemies = Player.List.Where(p =>
                    p.IsAlive &&
                    !summonedTeam.Members.Any(m => m.Player == p) &&
                    !rules.AlliedTeams.Contains(p.Role.GetTeam())
                );

                if (!enemies.Any())
                {
                    shouldRoundEnd = true;
                    winner = rules.WinningTeam;
                    break;
                }
            }

            if (shouldRoundEnd)
            {
                ev.IsAllowed = true;
                ev.LeadingTeam = winner;
            }
        }

        public void OnDying(PlayerDyingEventArgs ev)
        {
            if (ev.Player == null) return;

            var team = SummonedTeam.List.FirstOrDefault(t => t.Members.Any(m => m.Player == ev.Player));
            if (team == null) return;

            var member = team.Members.First(m => m.Player == ev.Player);

            if (!member.CustomRole.DropInventoryOnDeath)
            {
                LogManager.Debug($"Clearing inventory for {ev.Player.Nickname} ({member.CustomRole.Name})");
                ev.Player.ClearInventory(true, true);
            }

            Timing.CallDelayed(0.1f, () =>
            {
                if (team.IsEliminated) return;

                if (team.Members.All(m => !m.Player.IsAlive))
                {
                    team.IsEliminated = true;
                    UCTEvents.InvokeTeamEliminated(new TeamEliminatedEventArgs(team));
                }
            });
        }
    }
}
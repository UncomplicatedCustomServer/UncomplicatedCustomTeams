using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams
{
    internal class Handler
    {
        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            Bucket.SpawnBucket = new();

            LogManager.Debug($"Respawning team event, let's propose our team\nTeams: {API.Features.Team.List.Count}");

            foreach (Player Player in ev.Players)
                Bucket.SpawnBucket.Add(Player.Id);

            LogManager.Debug($"Next team for respawn is {ev.NextKnownTeam}");

            // Evaluate the team
            Team Team = Team.EvaluateSpawn(ev.NextKnownTeam);

            if (Team is null)
                Plugin.NextTeam = null; // No next team
            else
            {
                Plugin.NextTeam = SummonedTeam.Summon(Team, ev.Players);
                UncomplicatedCustomRoles.API.Features.SpawnBehaviour.DisableSpawnWave();
            }

            LogManager.Debug($"Next team selected: {Plugin.NextTeam?.Team?.Name}");
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (Plugin.NextTeam is not null && Bucket.SpawnBucket.Contains(ev.Player.Id))
            {
                LogManager.Debug($"Player {ev.Player} is changing role, let's do something to it!");
                Bucket.SpawnBucket.Remove(ev.Player.Id);
                
                Plugin.NextTeam.TrySpawnPlayer(ev.Player);
            }
        }
    }
}
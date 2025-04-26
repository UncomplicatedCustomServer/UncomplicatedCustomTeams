using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.Utilities;
using Utils.NonAllocLINQ;

namespace UncomplicatedCustomTeams.API.Features
{
    public class SummonedTeam
    {
        /// <summary>
        /// Gets a list of every spawned <see cref="Team"/> as <see cref="SummonedTeam"/>
        /// </summary>
        public static List<SummonedTeam> List { get; } = new();

        public string Id { get; }

        public List<SummonedCustomRole> Players { get; } = new();

        public Team Team { get; }

        public long Time { get; }

        /// <summary>
        /// Creates a new summoned team and adds it to the list.
        /// </summary>
        public SummonedTeam(Team team)
        {
            Team = team;
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Id = Guid.NewGuid().ToString();

            List.Add(this);
        }

        /// <summary>
        /// Spawns all players assigned to this team.
        /// </summary>
        public void SpawnAll()
        {
            foreach (SummonedCustomRole Role in Players)
            {
                if (Role.Player.IsAlive)
                {
                    Players.Remove(Role);
                    continue;
                }

                RoleTypeId SpawnType = RoleTypeId.ChaosConscript;

                if (Team.SpawnConditions?.SpawnWave == "NtfWave")
                    SpawnType = RoleTypeId.NtfPrivate;

                Role.AddRole(SpawnType);
            }
        }

        /// <summary>
        /// Checks if any players assigned to this team are still alive.
        /// </summary>
        public void CheckPlayers()
        {
            foreach (SummonedCustomRole Role in Players)
                if (Role.Player.IsAlive)
                    Players.Remove(Role);
        }

        /// <summary>
        /// Checks if the given team is a custom team.
        /// </summary>
        public static bool IsCustomTeam(PlayerRoles.Team team)
        {
            return Team.List.Any(t => t.Name == team.ToString());
        }

        /// <summary>
        /// Checks if the round should end based on the alive teams and winning conditions.
        /// </summary>
        public static void CheckRoundEndCondition()
        {
            var aliveTeams = Player.List.Where(p => p.IsAlive)
                .Select(p => p.Role.Team)
                .Where(t => !IsCustomTeam(t))
                .Distinct()
                .ToList();

            var winningTeams = Team.GetWinningTeams();
            bool hasWinningTeamAlive = aliveTeams.Any(team => winningTeams.Contains(team));
            bool onlyWinningTeamsRemain = aliveTeams.All(team => winningTeams.Contains(team));
            bool hasAliveCustomTeam = SummonedTeam.List.Any(team => team.HasAlivePlayers());

            if (hasWinningTeamAlive && onlyWinningTeamsRemain && hasAliveCustomTeam)
            {
                if (!Round.IsLocked)
                {
                    Round.EndRound();
                }
            }
        }

        /// <summary>
        /// Checks if this summoned team has any alive players.
        /// </summary>
        public bool HasAlivePlayers()
        {
            return Players.Any(role => role.Player.IsAlive);
        }

        /// <summary>
        /// Destroys this summoned team and removes it from the list.
        /// </summary>
        public void Destroy()
        {
            foreach (SummonedCustomRole role in Players) { role.Destroy(); }
            Players.Clear();
            List.Remove(this);
        }

        /// <summary>
        /// Clamps a value between the given minimum and maximum.
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public void ForceSpawnPlayer(Player player, RoleTypeId fallbackRole = RoleTypeId.ClassD)
        {
            LogManager.Debug($"Force spawning player {player.Nickname} for team {Team.Name}...");

            SummonedCustomRole summonedRole = SummonedPlayersGet(player);

            if (summonedRole != null)
            {
                LogManager.Debug($"Found custom role for {player.Nickname}: {summonedRole.CustomRole.Name}");
                summonedRole.AddRole();
            }
            else
            {
                LogManager.Debug($"No assigned custom role found for {player.Nickname}, using fallback role: {fallbackRole}");
                player.Role.Set(fallbackRole, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            }
        }

        /// <summary>
        /// Summons a new team and assigns players to available roles.
        /// </summary>
        public static SummonedTeam Summon(Team team, IEnumerable<Player> players)
        {
            if (team == null)
            {
                return null;
            }
            SummonedTeam SummonedTeam = new(team);

            foreach (Player Player in players)
            {
                foreach (CustomRole role in team.TeamRoles.OrderBy(r => r.Priority))
                {
                    if (SummonedTeam.SummonedPlayersCount(role) < role.MaxPlayers)
                    {
                        SummonedTeam.Players.Add(new(SummonedTeam, Player, role));
                        LogManager.Debug($"{Player.Nickname} -> {role.Name} (Priority: {role.Priority})");
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(team.CassieTranslation))
            {
                Cassie.MessageTranslated(team.CassieMessage, team.CassieTranslation, isNoisy: team.IsNoisy, isSubtitles: true);
            }
            if (!string.IsNullOrEmpty(team.SoundPath))
            {
                AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Global_Audio_{team.Id}", onIntialCreation: (p) =>
                {
                    Speaker speaker = p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
                });
                float volume = Clamp(team.SoundVolume, 1f, 100f);
                audioPlayer.AddClip($"sound_{team.Id}", volume);
            }
            return SummonedTeam;
        }

        /// <summary>
        /// Checks if a team can spawn based on the number of spectators and team role limits.
        /// </summary>
        public static List<Player> CanSpawnTeam(Team team)
        {
            if (team == null)
                return new List<Player>();

            List<Player> allPlayers = Player.List.ToList();
            int totalPlayers = allPlayers.Count;

            LogManager.Debug($"Total players: {totalPlayers}, MinPlayers required: {team.MinPlayers}");
            if (totalPlayers < team.MinPlayers)
            {
                LogManager.Debug($"Not enough players on the server to spawn team {team.Name}.");
                return new List<Player>();
            }

            List<Player> spectators = allPlayers
                .Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled)
                .ToList();

            int maxPlayersForTeam = team.TeamRoles.Sum(role => role.MaxPlayers);
            LogManager.Debug($"Spectators available: {spectators.Count}, MaxPlayers for team {team.Name}: {maxPlayersForTeam}");
            if (spectators.Count >= maxPlayersForTeam)
            {
                LogManager.Info($"Team {team.Name} is eligible for spawn. Spectators available: {spectators.Count}, required: {maxPlayersForTeam}");
            }
            else
            {
                LogManager.Debug($"Not enough spectators to spawn team {team.Name}. Required: {maxPlayersForTeam}, available: {spectators.Count}");
                return new List<Player>();
            }
            LogManager.Debug($"Spawning all {spectators.Count} spectators for team {team.Name}.");
            return spectators;
        }

        /// <summary>
        /// Refreshes the players list, ensuring that the maximum allowed players per role is respected.
        /// </summary>
        public void RefreshPlayers(IEnumerable<Player> players)
        {
            foreach (Player Player in players)
            {
                foreach (CustomRole Role in Team.TeamRoles.OrderBy(r => r.Priority))
                {
                    if (SummonedPlayersCount(Role) < Role.MaxPlayers)
                    {
                        Players.Add(new(this, Player, Role));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Counts the number of players assigned to a specific custom role.
        /// </summary>
        public int SummonedPlayersCount(CustomRole role)
        {
            return Players.Where(cr => cr.CustomRole == role).Count();
        }

        /// <summary>
        /// Gets the list of summoned players for a specific custom role.
        /// </summary>
        public IEnumerable<SummonedCustomRole> SummonedPlayersGet(CustomRole role) => Players.Where(cr => cr.CustomRole == role);

        /// <summary>
        /// Gets the summoned player role for a specific player.
        /// </summary>
        public SummonedCustomRole SummonedPlayersGet(Player player) => Players.Where(cr => cr.Player.Id == player.Id).FirstOrDefault();

        /// <summary>
        /// Tries to get a summoned player role for a specific player.
        /// </summary>
        public bool SummonedPlayersTryGet(Player player, out SummonedCustomRole role)
        {
            role = SummonedPlayersGet(player);
            return role != null;
        }

        /// <summary>
        /// Attempts to spawn a player with the given role.
        /// </summary>
        public void TrySpawnPlayer(Player player, RoleTypeId role) => SummonedPlayersGet(player)?.AddRole(role);

        /// <summary>
        /// Finds a summoned team by its ID.
        /// </summary>
        public static SummonedTeam Get(string Id) => List.Where(st => st.Id == Id).FirstOrDefault();

        /// <summary>
        /// Tries to find a summoned team by its ID.
        /// </summary>
        public static bool TryGet(string Id, out SummonedTeam team)
        {
            team = Get(Id);
            return team != null;
        }
    }
}

using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Storage;
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
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                SummonedCustomRole Role = Players[i];

                if (Role.Player == null)
                {
                    Players.RemoveAt(i);
                    continue;
                }

                if (Role.Player.IsAlive)
                {
                    Players.RemoveAt(i);
                    continue;
                }

                RoleTypeId SpawnType = RoleTypeId.ChaosConscript;
                if (Team.SpawnConditions?.SpawnWave == WaveType.NtfWave)
                    SpawnType = RoleTypeId.NtfPrivate;

                Role.AddRole(SpawnType);
            }
        }

        /// <summary>
        /// Checks if all players in this spawn wave have been eliminated.
        /// </summary>
        public bool IsTeamEliminated()
        {
            return Players.All(p => !p.Player.IsAlive);
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

            if (team.MaxSpawns >= 0 && team.SpawnCount >= team.MaxSpawns)
            {
                LogManager.Warn($"Team {team.Name} has reached its maximum number of spawns ({team.MaxSpawns}). Skipping spawn.");
                return null;
            }

            var rr = team.SpawnConditions.RequiredAliveRoles;
            if (rr != null && rr.Count > 0)
            {
                var aliveRoles = Player.List.Where(p => p.IsAlive).Select(p => p.Role.Type).ToHashSet();

                if (!rr.Any(aliveRoles.Contains))
                {
                    LogManager.Warn($"Skipping spawn for team {team.Name}, none of the required roles are alive. Required: [{string.Join(", ", rr)}]");
                    return null;
                }
            }

            SummonedTeam SummonedTeam = new(team);

            int totalAllowed = team.TeamRoles.Sum(r => r.MaxPlayers);
            int assigned = 0;

            foreach (Player player in players)
            {
                if (assigned >= totalAllowed)
                    break;

                foreach (IUCTCustomRole role in team.TeamRoles.OrderBy(r => r.Priority))
                {
                    if (role.Priority == RolePriority.None)
                        continue;

                    if (SummonedTeam.SummonedPlayersCount(role) < role.MaxPlayers)
                    {
                        SummonedTeam.Players.Add(new(SummonedTeam, player, role));
                        assigned++;
                        LogManager.Debug($"{player.Nickname} -> {role.Name} (Priority: {role.Priority})");
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(team.CassieTranslation))
            {
                if (team.IsCassieAnnouncementEnabled)
                    Cassie.MessageTranslated(team.CassieMessage, team.CassieTranslation, isNoisy: team.IsNoisy, isSubtitles: true);
            }
            bool hasCustomSound = team.SoundPaths != null && team.SoundPaths.Any(s => !string.IsNullOrEmpty(s.Path) && s.Path != "/path/to/your/ogg/file");
            if (hasCustomSound)
            {
                Timing.RunCoroutine(PlaySoundSequence(team));
            }

            team.SpawnCount++;
            return SummonedTeam;
        }


        /// <summary>
        /// A coroutine that plays a sequence of sounds with specified delays.
        /// </summary>
        private static IEnumerator<float> PlaySoundSequence(Team team)
        {
            AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"Global_Audio_{team.Id}", onIntialCreation: (p) =>
            {
                p.AddSpeaker("Main", isSpatial: false, maxDistance: 5000f);
            });
            float volume = Clamp(team.SoundVolume, 1f, 100f);

            for (int i = 0; i < team.SoundPaths.Count; i++)
            {
                var sound = team.SoundPaths[i];

                if (string.IsNullOrEmpty(sound.Path) || sound.Path == "/path/to/your/ogg/file")
                    continue;

                if (sound.Delay > 0f)
                {
                    yield return Timing.WaitForSeconds(sound.Delay);
                }

                string clipId = $"sound_{team.Id}_{i}";
                audioPlayer.AddClip(clipId, volume);
            }
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
                LogManager.Debug($"Not enough players on spectator to spawn team {team.Name}.");
                return new List<Player>();
            }

            List<Player> spectators = allPlayers
                .Where(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled)
                .ToList();

            var sortedRoles = team.TeamRoles
                .Where(role => role.Priority != RolePriority.None)
                .OrderBy(role => role.Priority)
                .ToList();

            List<Player> selectedPlayers = new();
            int index = 0;

            foreach (var teamRole in sortedRoles)
            {
                int max = teamRole.MaxPlayers;
                for (int i = 0; i < max && index < spectators.Count; i++, index++)
                {
                    selectedPlayers.Add(spectators[index]);
                }
            }

            if (selectedPlayers.Count == 0)
            {
                LogManager.Debug($"No spectators available to spawn for team {team.Name}.");
            }
            else
            {
                LogManager.Info($"Team {team.Name} will spawn with {selectedPlayers.Count} players.");
            }

            return selectedPlayers;
        }


        /// <summary>
        /// Refreshes the players list, ensuring that the maximum allowed players per role is respected.
        /// </summary>
        public void RefreshPlayers(IEnumerable<Player> players)
        {
            Plugin.CachedSpawnList.Clear();
            Bucket.SpawnBucket.Clear();

            foreach (Player player in players)
            {
                foreach (IUCTCustomRole role in Team.TeamRoles.OrderBy(r => r.Priority))
                {
                    if (role.Priority == RolePriority.None)
                        continue;

                    if (SummonedPlayersCount(role) < role.MaxPlayers)
                    {
                        Players.Add(new(this, player, role));
                        Plugin.CachedSpawnList.Add(player);
                        Bucket.SpawnBucket.Add(player.Id);
                        break;
                    }
                }
            }
            LogManager.Debug($"layers selected for spawn: {Plugin.CachedSpawnList.Count}");
        }

        /// <summary>
        /// Counts the number of players assigned to a specific custom role.
        /// </summary>
        public int SummonedPlayersCount(IUCTCustomRole role)
        {
            return Players.Where(cr => cr.CustomRole == role).Count();
        }

        /// <summary>
        /// Gets the list of summoned players for a specific custom role.
        /// </summary>
        public IEnumerable<SummonedCustomRole> SummonedPlayersGet(IUCTCustomRole role) => Players.Where(cr => cr.CustomRole == role);

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

﻿using Exiled.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public SummonedTeam(Team team)
        {
            Team = team;
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Id = Guid.NewGuid().ToString();

            List.Add(this);
        }

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

                if (Team.SpawnWave  == Exiled.API.Enums.SpawnableFaction.NtfWave)
                    SpawnType = RoleTypeId.NtfPrivate;

                Role.AddRole(SpawnType);
            }
        }

        public void CheckPlayers()
        {
            foreach (SummonedCustomRole Role in Players)
                if (Role.Player.IsAlive)
                    Players.Remove(Role);
        }

        public void Destroy()
        {
            foreach (SummonedCustomRole role in Players) { role.Destroy(); }
            Players.Clear();
            List.Remove(this);
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static SummonedTeam Summon(Team team, IEnumerable<Player> players)
        {
            SummonedTeam SummonedTeam = new(team);

            foreach (Player Player in players)
            {
                foreach (CustomRole Role in team.Roles)
                {
                    if (SummonedTeam.SummonedPlayersCount(Role) < Role.MaxPlayers)
                    {
                        SummonedTeam.Players.Add(new(SummonedTeam, Player, Role));
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(team.CassieTranslation))
            {
                Cassie.Message(team.CassieTranslation, isSubtitles: true, isNoisy: team.IsNoisy, isHeld: false);
            }
            else if (!string.IsNullOrEmpty(team.CassieMessage))
            {
                Cassie.Message(team.CassieMessage, isSubtitles: true, isNoisy: team.IsNoisy, isHeld: false);
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

        public void RefreshPlayers(IEnumerable<Player> players)
        {
            foreach (Player Player in players)
            {
                foreach (CustomRole Role in Team.Roles)
                {
                    if (SummonedPlayersCount(Role) < Role.MaxPlayers)
                    {
                        Players.Add(new(this, Player, Role));
                        break;
                    }
                }
            }
        }

        public int SummonedPlayersCount(CustomRole role)
        {
            return Players.Where(cr => cr.CustomRole == role).Count();
        }

        public IEnumerable<SummonedCustomRole> SummonedPlayersGet(CustomRole role) => Players.Where(cr => cr.CustomRole == role);

        public SummonedCustomRole SummonedPlayersGet(Player player) => Players.Where(cr => cr.Player.Id == player.Id).FirstOrDefault();

        public bool SummonedPlayersTryGet(Player player, out SummonedCustomRole role)
        {
            role = SummonedPlayersGet(player);
            return role != null;
        }

        public void TrySpawnPlayer(Player player, RoleTypeId role) => SummonedPlayersGet(player)?.AddRole(role);

        public static SummonedTeam Get(string Id) => List.Where(st => st.Id == Id).FirstOrDefault();

        public static bool TryGet(string Id, out SummonedTeam team)
        {
            team = Get(Id);
            return team != null;
        }
    }
}

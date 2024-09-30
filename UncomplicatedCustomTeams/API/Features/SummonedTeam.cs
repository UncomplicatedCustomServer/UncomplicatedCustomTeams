using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

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

        public InternalTeam Team { get; }

        public long Time { get; }

        internal List<Tuple<Player, InternalCustomRole>> SpawnRoles { get; } = new();

        public SummonedTeam(InternalTeam team)
        {
            Team = team;
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Id = Guid.NewGuid().ToString();

            List.Add(this);
        }

        public void SpawnAll()
        {
            foreach (Tuple<Player, InternalCustomRole> spawn in SpawnRoles)
            {
                if (spawn.Item1.IsAlive)
                {
                    SpawnRoles.Remove(spawn);
                    continue;
                }

                RoleTypeId SpawnType = RoleTypeId.ChaosConscript;

                if (Team.SpawnWave is Respawning.SpawnableTeamType.NineTailedFox)
                    SpawnType = RoleTypeId.NtfPrivate;

                Spawn(spawn.Item1, spawn.Item2, SpawnType);
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

        public static SummonedTeam Summon(InternalTeam team, IEnumerable<Player> players)
        {
            SummonedTeam SummonedTeam = new(team);

            foreach (Player player in players)
            {
                foreach (InternalCustomRole role in team.Roles)
                {
                    if (SummonedTeam.SummonedPlayersCount(role) < role.MaxPlayers)
                    {
                        SummonedTeam.SpawnRoles.Add(new(player, role));
                        break;
                    }
                }
            }

            return SummonedTeam;
        }

        public void RefreshPlayers(IEnumerable<Player> players)
        {
            SpawnRoles.Clear();
            foreach (Player player in players)
            {
                foreach (InternalCustomRole role in Team.Roles)
                {
                    if (SummonedPlayersCount(role) < role.MaxPlayers)
                    {
                        SpawnRoles.Add(new(player, role));
                        break;
                    }
                }
            }
        }

        public int SummonedPlayersCount(InternalCustomRole role)
        {
            return Players.Where(cr => cr.Role.Id == role.Id).Count();
        }

        public IEnumerable<SummonedCustomRole> SummonedPlayersGet(CustomRole role) => Players.Where(cr => cr.Role.Id == role.Id);

        public SummonedCustomRole SummonedPlayersGet(Player player) => Players.Where(cr => cr.Player.Id == player.Id).FirstOrDefault();

        public bool SummonedPlayersTryGet(Player player, out SummonedCustomRole role)
        {
            role = SummonedPlayersGet(player);
            return role != null;
        }

        public void TrySpawnPlayer(Player player, RoleTypeId role) => Spawn(player, SpawnRoles.FirstOrDefault(s => s.Item1.Id == player.Id).Item2, role);

        public static SummonedTeam Get(string Id) => List.Where(st => st.Id == Id).FirstOrDefault();

        public static bool TryGet(string Id, out SummonedTeam team)
        {
            team = Get(Id);
            return team != null;
        }

        public void Spawn(Player player, InternalCustomRole customRole, RoleTypeId proposed)
        {
            LogManager.Debug($"Changing role to player {player.Nickname} ({player.Id}) to {customRole.Name} ({customRole.Id}) from team {Team.Name}");

            player.Role.Set(customRole.Role, Exiled.API.Enums.SpawnReason.Respawn, RoleSpawnFlags.None);

            if (Team.SpawnPosition == Vector3.zero || Team.SpawnPosition == Vector3.one)
                player.Position = proposed.GetRandomSpawnLocation().Position;
            else
                player.Position = Team.SpawnPosition;

#pragma warning disable CS0618 // Il tipo o il membro è obsoleto
            player.SetCustomRoleAttributes(customRole);
#pragma warning restore CS0618 // Il tipo o il membro è obsoleto

            SpawnRoles.RemoveAll(s => s.Item1.Id == player.Id);
        }
    }
}

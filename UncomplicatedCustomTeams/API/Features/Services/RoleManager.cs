using System;
using System.Collections.Generic;
using UncomplicatedCustomRoles.API.Enums;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Features.Behaviour;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.API.Features.Services
{
    /// <summary>
    /// Handles the bridge between UCT (Internal) and UCR (Public) registries.
    /// Ensures load-order independence and reload safety.
    /// </summary>
    public static class RoleManager
    {
        private static readonly HashSet<int> UCTRegisteredRoles = [];

        /// <summary>
        /// Ensures that the role is properly registered in UCR.
        /// </summary>
        public static void EnsureIsRegistered(UncomplicatedCustomRole role)
        {
            if (role == null) return;

            if (CustomRole.TryGet(role.Id, out _))
            {
                if (UCTRegisteredRoles.Contains(role.Id))
                {
                    LogManager.Error($"Role with ID {role.Id} ({role.Name}) has already been registered by UCT!");
                    throw new InvalidOperationException($"Role duplication! ID {role.Id} has already been used by UCT.");
                }
                else
                {
                    LogManager.Info($"Found an external UCR role with ID {role.Id}. Skipping internal registration, importing...");
                    return;
                }
            }

            role.SpawnSettings ??= GetDefaultSpawnBehaviour();

            if (CustomRole.Register(role) == LoadStatusType.Success)
            {
                UCTRegisteredRoles.Add(role.Id);
                LogManager.Debug($"Registered Custom Role '{role.Name}' (ID: {role.Id}).");
            }
            else
            {
                LogManager.Warn($"Failed to register Custom Role '{role.Name}' (ID: {role.Id}).");
            }
        }

        /// <summary>
        /// Registers all roles from a specific team to UCR immediately.
        /// </summary>
        public static void RegisterTeamRoles(Team team)
        {
            foreach (var role in team.Roles)
            {
                role.SpawnSettings ??= GetDefaultSpawnBehaviour();
                EnsureIsRegistered(role);
            }
        }

        private static SpawnBehaviour GetDefaultSpawnBehaviour()
        {
            return new SpawnBehaviour
            {
                Spawn = SpawnType.KeepCurrentPositionSpawn,
                SpawnRooms = ["Unknown"],
                SpawnRoles = [PlayerRoles.RoleTypeId.None],
                CanReplaceRoles = [PlayerRoles.RoleTypeId.None],
                MaxPlayers = 10,
                MinPlayers = 1,
                SpawnChance = 0f,
                SpawnZones = [],
                SpawnPoints = [],
                RequiredPermission = string.Empty
            };
        }
    }
}
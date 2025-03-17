using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

namespace UncomplicatedCustomTeams.API.Features
{
    public class SummonedCustomRole
    {
        /// <summary>
        /// The <see cref="Exiled.API.Features.Player"/> instance
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// The CustomRole instance for the given player
        /// </summary>
        public CustomRole CustomRole { get; }

        public SummonedTeam Team { get; }

        /// <summary>
        /// Indicate wether the custom role has been assigned or not
        /// </summary>
        public bool IsRoleSet { get; private set; } = false;

        public SummonedCustomRole(SummonedTeam team, Player player, CustomRole role)
        {
            Team = team;
            Player = player;
            CustomRole = role;
        }

        public void Destroy()
        {
            if (Player.IsAlive)
                Player.TryRemoveCustomRole();
        }

#pragma warning disable CS0618 // A class member was marked with the Obsolete attribute -> the [Obsolete()] attribute is only there to avoid users to use this in a wrong way!
        public void AddRole(RoleTypeId? proposed = null)
        {
            RoleTypeId finalRole = proposed ?? RoleTypeId.ChaosConscript;
            LogManager.Debug($"Changing role to {finalRole} for player {Player.Nickname} ({Player.Id})");

            Player.Role.Set(CustomRole.Role, Exiled.API.Enums.SpawnReason.Respawn, RoleSpawnFlags.None);

            if (Player.Role != CustomRole.Role)
            {
                LogManager.Debug($"Role assignment failed! Falling back to {finalRole}.");
                Player.Role.Set(finalRole, Exiled.API.Enums.SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            }

            Player.Position = Team.Team.SpawnConditions.SpawnPosition == Vector3.zero ?
                finalRole.GetRandomSpawnLocation().Position : Team.Team.SpawnConditions.SpawnPosition;

            Player.SetCustomRoleAttributes(CustomRole);
            IsRoleSet = true;
        }
    }
}

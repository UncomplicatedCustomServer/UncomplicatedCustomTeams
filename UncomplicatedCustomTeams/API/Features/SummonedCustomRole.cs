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
        public void AddRole(RoleTypeId proposed)
        {
            LogManager.Debug($"Changing role to player {Player.Nickname} ({Player.Id}) to {CustomRole.Name} ({CustomRole.Id}) from team {Team.Team.Name}");

            Player.Role.Set(CustomRole.Role, Exiled.API.Enums.SpawnReason.Respawn, RoleSpawnFlags.None);

            if (Team.Team.SpawnPosition == Vector3.zero || Team.Team.SpawnPosition == Vector3.one)
                Player.Position = proposed.GetRandomSpawnLocation().Position;
            else
                Player.Position = Team.Team.SpawnPosition;

            Player.SetCustomRoleAttributes(CustomRole);
            IsRoleSet = true;
        }
    }
}

using LabApi.Features.Wrappers;
using System.Collections.Generic;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.API.Enums;

namespace UncomplicatedCustomTeams.API.Features.Definitions
{
    public class UncomplicatedCustomRole : CustomRole, IUCTCustomRole
    {
        /// <summary>
        /// The maximum number of players that can have this role in this wave
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// The priority of assigning this role in the wave (First -> Fifth).
        /// The lower the value, the higher the priority.
        /// </summary>
        public RolePriority Priority { get; set; } = RolePriority.First;

        /// <summary>
        /// A list of required group or permission needed to spawn as this role.
        /// Evaluates group first, then falls back to permission.
        /// </summary>
        public List<string> PermissionsRequired { get; set; } = [];

        /// <summary>
        /// Whether the items should be dropped on ground upon death for this role.
        /// </summary>
        public bool DropInventoryOnDeath { get; set; } = true;

        /// <summary>
        /// Whether Godmode is enabled for this role.
        /// </summary>
        public bool IsGodmodeEnabled { get; set; }

        /// <summary>
        /// Whether bypass is enabled for this role.
        /// </summary>
        public bool IsBypassEnabled { get; set; }

        /// <summary>
        /// Whether noclip is enabled for this role.
        /// </summary>
        public bool IsNoclipEnabled { get; set; }

        public void Spawn(Player player)
        {
            if (CustomRole.TryGet(this.Id, out var existingUCRRole))
            {
                player.SetCustomRole(existingUCRRole);
            }
            else
            {
                player.SetCustomRole(this);
            }
        }
    }
}

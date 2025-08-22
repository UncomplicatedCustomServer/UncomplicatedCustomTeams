using Exiled.API.Features;
using System.ComponentModel;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.API.Enums;

namespace UncomplicatedCustomTeams.API.Features
{
    public class UncomplicatedCustomRole : UncomplicatedCustomRoles.API.Features.CustomRole, IUCTCustomRole
    {
        /// <summary>
        /// The maximum number of players that can have this role in this wave
        /// </summary>
        [Description("The maximum number of players that can have this role in this wave")]
        public int MaxPlayers { get; set; }

        /// <summary>
        /// The priority of assigning this role in the wave (First -> Fifth).
        /// The lower the value, the higher the priority.
        /// </summary>
        [Description("Priority of assigning custom role in Team (First -> Fifth). The lower the value, the higher the priority.")]
        public RolePriority Priority { get; set; } = RolePriority.First;

        /// <summary>
        /// Whether the items should be dropped on ground upon death for this role.
        /// </summary>
        [Description("Whether the items should be dropped on ground upon death for this role.")]
        public bool DropInventoryOnDeath { get; set; } = true;

        /// <summary>
        /// Whether Godmode is enabled for this role.
        /// </summary>
        [Description("Whether Godmode is enabled for this role.")]
        public bool IsGodmodeEnabled { get; set; }

        /// <summary>
        /// Whether bypassing obstacles is enabled for this role.
        /// </summary>
        [Description("Whether bypassing obstacles is enabled for this role.")]
        public bool IsBypassEnabled { get; set; }

        /// <summary>
        /// Whether noclip is enabled for this role.
        /// </summary>
        [Description("Whether noclip is enabled for this role.")]
        public bool IsNoclipEnabled { get; set; }

        public void Spawn(Player player)
        {
            player.SetCustomRole(this);
        }
    }
}

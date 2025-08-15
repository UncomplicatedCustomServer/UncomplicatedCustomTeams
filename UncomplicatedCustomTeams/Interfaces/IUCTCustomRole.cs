using Exiled.API.Features;
using PlayerRoles;
using System.ComponentModel;
using UncomplicatedCustomTeams.API.Enums;
using YamlDotNet.Serialization;

namespace UncomplicatedCustomTeams.API.Features
{
    public interface IUCTCustomRole
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
        public RolePriority Priority { get; set; }

        /// <summary>
        /// Whether the items should be dropped on ground upon death for this role.
        /// </summary>
        [Description("Whether the items should be dropped on ground upon death for this role.")]
        public bool DropInventoryOnDeath { get; set; }

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

        public int Id { get; set; }

        public string Name { get; }

        [YamlIgnore]
        public RoleTypeId Role { get; }

        public void Spawn(Player player);
    }
}

using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using UncomplicatedCustomTeams.API.Enums;
using YamlDotNet.Serialization;

namespace UncomplicatedCustomTeams.API.Features
{
    public class ExiledCustomRole : IUCTCustomRole
    {
        [YamlIgnore]
        private Exiled.CustomRoles.API.Features.CustomRole CustomRole => Exiled.CustomRoles.API.Features.CustomRole.Get((uint)Id);
        public int MaxPlayers { get; set; }
        public RolePriority Priority { get; set; } = RolePriority.None;
        public bool DropInventoryOnDeath { get; set; } = true;
        public bool IsGodmodeEnabled { get; set; }
        public bool IsBypassEnabled { get; set; }
        public bool IsNoclipEnabled { get; set; }
        public int Id { get; set; }

        [YamlIgnore]
        public string Name
        {
            get
            {
                var role = CustomRole;
                return role != null ? role.Name : "Unknown Role";
            }
        }

        [YamlIgnore]
        public RoleTypeId Role
        {
            get
            {
                var role = CustomRole;
                return role != null ? role.Role : RoleTypeId.None;
            }
        }

        public void Spawn(Player player)
        {
            if (Plugin.Instance.Config.UseExiledCustomRoles)
                CustomRole.AddRole(player);
        }
    }
}

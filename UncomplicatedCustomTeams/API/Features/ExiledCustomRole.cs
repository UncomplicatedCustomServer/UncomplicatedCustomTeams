using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Enums;
using YamlDotNet.Serialization;

namespace UncomplicatedCustomTeams.API.Features
{
    public class ExiledCustomRole : CustomRole
    {
        [YamlIgnore]
        private Exiled.CustomRoles.API.Features.CustomRole CustomRole => Exiled.CustomRoles.API.Features.CustomRole.Get((uint)Id);
        public int MaxPlayers { get; set; }
        public RolePriority Priority { get; set; } = RolePriority.First;
        public int Id { get; set; }

        [YamlIgnore]
        public string Name => CustomRole.Name;

        [YamlIgnore]
        public RoleTypeId Role => CustomRole.Role;

        public void Spawn(Player player)
        {
            CustomRole.AddRole(player);
        }
    }
}

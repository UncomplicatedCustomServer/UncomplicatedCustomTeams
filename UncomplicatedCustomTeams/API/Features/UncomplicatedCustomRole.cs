using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.API.Features;
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
        /// The priority of assigning this role in the wave (First -> Fourth).
        /// The lower the value, the higher the priority.
        /// </summary>
        [Description("Priority of assigning custom role in Team (First -> Fourth). The lower the value, the higher the priority.")]
        public RolePriority Priority { get; set; } = RolePriority.First;

        public void Spawn(Player player)
        {
            player.SetCustomRole(this);
        }
    }
}

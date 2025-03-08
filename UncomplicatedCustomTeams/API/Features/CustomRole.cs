using System.ComponentModel;

namespace UncomplicatedCustomTeams.API.Features
{
    public class CustomRole : UncomplicatedCustomRoles.API.Features.CustomRole
    {
        /// <summary>
        /// The maximum number of players that can have this role in this wave
        /// </summary>
        [Description("The maximum number of players that can have this role in this wave")]
        public int MaxPlayers { get; set; }
    }
}

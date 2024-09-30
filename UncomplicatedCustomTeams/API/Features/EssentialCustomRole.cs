using System.ComponentModel;

namespace UncomplicatedCustomTeams.API.Features
{
    public class EssentialCustomRole
    {
        /// <summary>
        /// The Id of the targeted custom role
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The maximum number of players that can have this role in this wave
        /// </summary>
        [Description("The maximum number of players that can have this role in this wave")]
        public int MaxPlayers { get; set; }
    }
}
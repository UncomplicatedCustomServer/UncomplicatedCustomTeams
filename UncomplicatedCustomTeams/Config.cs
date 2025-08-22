using Exiled.API.Interfaces;
using System.ComponentModel;

namespace UncomplicatedCustomTeams
{
    internal class Config : IConfig
    {
        [Description("Is the plugin enabled?")]
        public bool IsEnabled { get; set; } = true;

        [Description("Do enable the developer (debug) mode?")]
        public bool Debug { get; set; } = false;
        public bool UseExiledCustomRoles { get; set; } = false;

        [Description("How long to wait for Exiled CustomRoles to be registered before checking for registry?")]
        public float ExiledCustomRoleCheckDelay { get; set; } = 10f;
    }
}

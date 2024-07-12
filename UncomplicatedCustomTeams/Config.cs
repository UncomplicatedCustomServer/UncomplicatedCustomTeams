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
    }
}

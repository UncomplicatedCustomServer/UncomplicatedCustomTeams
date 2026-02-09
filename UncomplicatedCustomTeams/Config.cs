using System.ComponentModel;

namespace UncomplicatedCustomTeams
{
    internal class Config
    {
        [Description("Do enable the developer (debug) mode?")]
        public bool Debug { get; set; } = false;
        [Description("Enable or disable credit tags functionality.")]
        public bool EnableCreditTags { get; set; } = true;
        [Description("Enable or disable the auto-updater.")]
        public bool EnableAutoUpdater { get; set; } = true;
    }
}

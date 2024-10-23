using HarmonyLib;

namespace UncomplicatedCustomTeams.Patches
{
    [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.ReloadServerName))]
    internal class ServerNamePatch
    {
        private static void Postfix() => ServerConsole._serverName += $"<color=#000000><size=1>UDI {Plugin.Instance.Version.ToString(3)}</size></color>";
    }
}

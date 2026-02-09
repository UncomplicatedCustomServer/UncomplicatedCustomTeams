using HarmonyLib;
using Respawning.Announcements;
using Respawning.Waves;
using System.Collections.Generic;

namespace UncomplicatedCustomTeams.Patches
{
    [HarmonyPatch(typeof(WaveAnnouncementBase), nameof(WaveAnnouncementBase.PlayAnnouncement))]
    public static class PreventWaveAnnouncementPatch
    {
        public static bool Prefix(WaveAnnouncementBase __instance, List<ReferenceHub> spawnedPlayers, IAnnouncedWave wave)
        {
            if (EventHandlers.SpawnWaves.DefaultSpawnWaves.CustomTeamSpawnedThisWave)
            {
                return false;
            }
            return true;
        }
    }
}

using System.Collections.Generic;
using UncomplicatedCustomTeams.API.Enums;

namespace UncomplicatedCustomTeams.API.Features.Definitions
{
    /// <summary>
    /// Holds the conditions and settings for when and where a team should spawn.
    /// Uses a modular 'Settings' dictionary to avoid configuration clutter.
    /// </summary>
    public class SpawnData
    {
        /// <summary>
        /// The trigger event type.
        /// </summary>
        public WaveType SpawnWave { get; set; } = WaveType.NtfWave;

        /// <summary>
        /// Allow this team to spawn during mini-waves.
        /// </summary>
        public bool AllowMiniWaves { get; set; } = false;

        /// <summary>
        /// Determines whether the team is eligible to spawn during both MTF and Chaos spawn waves. 
        /// Note: This feature applies ONLY to NtfWave and ChaosWave types.
        /// </summary>
        public bool SpawnOnBothWaves { get; set; } = false;

        /// <summary>
        /// The spawn chance (0-100) specifically for the Nine-Tailed Fox wave. 
        /// Overrides the team's default SpawnChance if SpawnOnBothWaves is true.
        /// </summary>
        public int SpawnChanceNtf { get; set; } = -1;

        /// <summary>
        /// The spawn chance (0-100) specifically for the Chaos Insurgency wave.
        /// Overrides the team's default SpawnChance if SpawnOnBothWaves is true.
        /// </summary>
        public int SpawnChanceChaos { get; set; } = -1;

        /// <summary>
        /// Delay in seconds before spawning after the condition is met.
        /// </summary>
        public float SpawnDelay { get; set; } = 0f;

        /// <summary>
        /// Dynamic dictionary for wave-specific settings.
        /// Refer to the WIKI for valid keys per WaveType.
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = [];
    }
}
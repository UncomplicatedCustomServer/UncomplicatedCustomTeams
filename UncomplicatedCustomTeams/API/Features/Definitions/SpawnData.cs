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
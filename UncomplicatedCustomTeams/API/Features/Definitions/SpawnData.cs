using PlayerRoles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UncomplicatedCustomTeams.API.Enums;
using UnityEngine;

namespace UncomplicatedCustomTeams.API.Features.Definitions
{
    /// <summary>
    /// Holds the conditions and settings for when and where a team should spawn.
    /// </summary>
    public class SpawnData
    {
        /// <summary>
        /// The trigger event typ.
        /// </summary>
        public WaveType SpawnWave { get; set; } = WaveType.NtfWave;

        /// <summary>
        /// The vector3 position for the spawn.
        /// </summary>
        public Vector3 SpawnPosition { get; set; } = Vector3.zero;
        public Vector3 SpawnRotation { get; set; } = Vector3.zero;
        public uint AfterTeamSpawn { get; set; } = 0;
        public uint AfterTeamDeath { get; set; } = 0;
        public int RequiredEngagedGenerators { get; set; } = 1;

        private ItemType _usedItem = ItemType.None;
        private int? _customItemId = null;
        public string UsedItem
        {
            get => _customItemId.HasValue ? _customItemId.Value.ToString() : _usedItem.ToString();
            set
            {
                if (int.TryParse(value, out int customItemId)) { _customItemId = customItemId; _usedItem = ItemType.None; }
                else if (Enum.TryParse(value, true, out ItemType item)) { _usedItem = item; _customItemId = null; }
                else { _usedItem = ItemType.None; _customItemId = null; }
            }
        }

        public ItemType GetUsedItemType() => _usedItem;
        public int? GetCustomItemId() => _customItemId;
        public string TargetScp { get; set; } = "None";
        public bool IsScp0492CountedAsScp { get; set; } = false;
        public List<RoleTypeId> RequiredAliveRoles { get; set; } = [];

        [Description("Roles converted on RoundStart.")]
        public List<RoleTypeId> RolesAffectedOnRoundStart { get; set; } = [];
        public float SpawnDelay { get; set; } = 0f;

        /// <summary>
        /// Helper method to determine if the current SpawnWave type requires a manual Vector3 SpawnPosition.
        /// </summary>
        public bool RequiresSpawnPosition()
        {
            return SpawnWave == WaveType.AfterDecontamination ||
                   SpawnWave == WaveType.AfterWarhead ||
                   SpawnWave == WaveType.RoundStarted ||
                   SpawnWave == WaveType.ScpDeath ||
                   SpawnWave == WaveType.UsedItem ||
                   SpawnWave == WaveType.TeamDependent ||
                   SpawnWave == WaveType.AfterGeneratorActivated;
        }
    }

    public class SoundPathEntry
    {
        [Description("The path to the .ogg sound file.")]
        public string Path { get; set; } = "/path/to/your/ogg/file";
        public float Delay { get; set; } = 0f;
    }

    public class RoundEndRule
    {
        public bool PreventRoundEndIfAlive { get; set; } = true;
        public List<PlayerRoles.Team> AlliedTeams { get; set; } = [];
        public RoundSummary.LeadingTeam WinningTeam { get; set; } = RoundSummary.LeadingTeam.Draw;
    }
}
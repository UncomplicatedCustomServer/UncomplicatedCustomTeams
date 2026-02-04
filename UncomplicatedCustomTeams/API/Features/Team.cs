using Exiled.API.Enums;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;
using YamlDotNet.Serialization;

namespace UncomplicatedCustomTeams.API.Features
{
    public class Team
    {
        private static readonly System.Random _random = new();
        /// <summary>
        /// Gets a complete list of every custom <see cref="Team"/> registered
        /// </summary>
        public static List<Team> List { get; } = [];

        /// <summary>
        /// Register a new custom <see cref="Team"/>
        /// </summary>
        /// <param name="team"></param>
        public static void Register(Team team)
        {
            List.Add(team);
        }

        /// <summary>
        /// Unregister a custom <see cref="Team"/>
        /// </summary>
        /// <param name="team"></param>
        public static void Unregister(Team team) => List.Remove(team);

        /// <summary>
        /// The Id of the custom <see cref="Team"/>
        /// </summary>
        public uint Id { get; set; } = 1;

        /// <summary>
        /// The name of the custom <see cref="Team"/>
        /// </summary>
        public string Name { get; set; } = "GOC";

        /// <summary>
        /// The minimum number of players that are required to be on the server to make this custom <see cref="Team"/> spawn
        /// </summary>
        public int MinPlayers { get; set; } = 1;

        /// <summary>
        /// The maximum number of times this team can be spawned in a single round. Set to -1 for unlimited.
        /// </summary>
        [Description("The maximum number of times this team can be spawned in a single round. Set to -1 for unlimited.")]
        public int MaxSpawns { get; set; } = -1;

        /// <summary>
        /// Tracks how many times this team has been spawned in the current round.
        /// </summary>
        [YamlIgnore]
        public int SpawnCount { get; internal set; } = 0;

        /// <summary>
        /// The chance of spawning of this custom <see cref="Team"/>.
        /// 0 is 0% and 100 is 100%!
        /// </summary>
        public uint SpawnChance { get; set; } = 100;

        /// <summary>
        /// If set to true, this team's successful spawn roll will not prevent other teams
        /// with the same SpawnWave from being evaluated.
        /// </summary>
        [Description("Set to true to allow this team to spawn alongside other teams during the same Spawn Wave. If false (default), it will be the only one.")]
        public bool AllowConcurrentSpawns { get; set; } = false;

        /// <summary>
        /// Defines the spawn conditions for a custom team.
        /// </summary>
        public SpawnData SpawnConditions { get; set; } = new();

        /// <summary>
        /// Is Cassie announcement enabled?
        /// </summary>
        public bool IsCassieAnnouncementEnabled { get; set; } = true;

        /// <summary>
        /// The cassie message that will be sent to every player
        /// </summary>
        public string CassieMessage { get; set; } = "team arrived";

        /// <summary>
        /// The translation of the cassie message
        /// </summary>
        public string CassieTranslation { get; set; } = "Team arrived!";

        /// <summary>
        /// Determines whether the Cassie message should be noisy.
        /// </summary>
        public bool IsNoisy { get; set; } = true;

        public RoundEndRule WinCondition { get; set; } = new RoundEndRule();

        /// <summary>
        /// A list of sounds to be played sequentially when the team spawns.
        /// Requires AudioPlayerAPI.
        /// </summary>
        [Description("A list of sounds to be played sequentially. Requires AudioPlayerAPI. Download it here: https://github.com/Killers0992/AudioPlayerApi")]
        public List<SoundPathEntry> SoundPaths { get; set; } = [new()];

        /// <summary>
        /// Volume of the sound, should be between 1 and 5.
        /// </summary>
        public float SoundVolume { get; set; } = 1f;

        /// <summary>
        /// The list of every role that will be a part of this wave
        /// </summary>
        [YamlIgnore]
        public List<IUCTCustomRole> TeamRoles => [.. Roles.OfType<IUCTCustomRole>(), .. EcrRoles];

        /// <summary>
        /// The list of every UCR role that will be a part of this wave
        /// </summary>
        public List<UncomplicatedCustomRole> Roles { get; set; } =
        [
            new()
            {
                Id = 1,
                Team = PlayerRoles.Team.ClassD,
                SpawnSettings = null,
                CanEscape = false,
                RoleAfterEscape = null,
                MaxPlayers = 1,
                Priority = RolePriority.First,
                DropInventoryOnDeath = true,
                IsGodmodeEnabled = false,
                IsBypassEnabled = false,
                IsNoclipEnabled = false,
                CustomFlags = null,
            },
            new()
            {
                Id = 2,
                Team = PlayerRoles.Team.ClassD,
                SpawnSettings = null,
                CanEscape = false,
                RoleAfterEscape = null,
                CustomFlags = null,
                Priority = RolePriority.Second,
                DropInventoryOnDeath = true,
                IsGodmodeEnabled = false,
                IsBypassEnabled = false,
                IsNoclipEnabled = false,
                MaxPlayers = 1
            }
        ];


        /// <summary>
        /// The list of every ECR role that will be a part of this wave
        /// </summary>
        public List<ExiledCustomRole> EcrRoles { get; set; } =
        [
            new()
            {
                Id = 1,
                Priority = RolePriority.None,
                MaxPlayers = 1,
                DropInventoryOnDeath = true
            }
        ];

        public static List<Team> EvaluateSpawn(WaveType wave)
        {
            List<Team> winningTeams = [];

            var eligibleTeams = List.Where(t => t.SpawnConditions.SpawnWave == wave).ToList();

            if (!eligibleTeams.Any())
            {
                return winningTeams;
            }

            LogManager.Debug($"Found {eligibleTeams.Count} eligible Custom Team(s) for WaveType '{wave}'. Evaluating chances.");

            foreach (var team in eligibleTeams)
            {
                int roll = _random.Next(0, 100);
                if (roll < team.SpawnChance)
                {
                    LogManager.Debug($"Team '{team.Name}' succeeded its spawn roll! (Rolled: {roll}, Needed < {team.SpawnChance}). Adding to spawn list.");

                    winningTeams.Add(team);

                    if (!team.AllowConcurrentSpawns)
                    {
                        LogManager.Debug($"Team '{team.Name}' has AllowConcurrentSpawns set to false. Stopping further evaluations for this wave.");
                        break;
                    }
                }
                else
                {
                    LogManager.Debug($"Team '{team.Name}' failed its spawn roll. (Rolled: {roll}, Needed >= {team.SpawnChance}).");
                }
            }

            if (!winningTeams.Any())
                LogManager.Debug("No custom team succeeded their spawn roll for this wave.");

            return winningTeams;
        }

        public class SpawnData
        {
            public WaveType SpawnWave { get; set; } = WaveType.NtfWave;
            public Vector3 SpawnPosition { get; set; } = Vector3.zero;
            public Vector3 SpawnRotation { get; set; } = Vector3.zero; // yaml has skill issue with Quaternion

            [Description("Spawn this team AFTER the custom team with the specified ID has spawned. SpawnWave must be set to 'TeamDependent'.")]
            public uint AfterTeamSpawn { get; set; } = 0;

            [Description("Spawn this team AFTER the custom team with the specified ID has been completely eliminated. SpawnWave must be set to 'TeamDependent'.")]
            public uint AfterTeamDeath { get; set; } = 0;

            [Description("How many generators must be engaged for this team to spawn. Only works if SpawnWave is set to 'AfterGeneratorActivated'. Value should be between 1 and 3.")]
            public int RequiredEngagedGenerators { get; set; } = 1;

            private ItemType _usedItem = ItemType.None;
            private int? _customItemId = null;

            [Description("Specify the Game Base Utem or EXILED Custom Item ID that triggers this team spawn. Only works if SpawnWave is set to 'UsedItem'.")]
            public string UsedItem
            {
                get
                {
                    if (_customItemId.HasValue)
                        return _customItemId.Value.ToString();
                    return _usedItem.ToString();
                }
                set
                {
                    if (int.TryParse(value, out int customItemId))
                    {
                        _customItemId = customItemId;
                        _usedItem = ItemType.None;
                    }
                    else if (Enum.GetNames(typeof(ItemType)).Any(name => name.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    {
                        _usedItem = (ItemType)Enum.Parse(typeof(ItemType), value, true);
                        _customItemId = null;
                    }
                    else
                    {
                        _usedItem = ItemType.None;
                        _customItemId = null;
                    }
                }
            }

            public ItemType GetUsedItemType() => _usedItem;
            public int? GetCustomItemId() => _customItemId;

            [Description("Specify the SCP role (e.g., Scp106) or use the SCPs team (SCPs) whose death triggers this team spawn. Only SCPs is allowed when using a team. This setting only applies when SpawnWave is set to 'ScpDeath'.")]
            public string TargetScp { get; set; } = "None";
            public bool IsScp0492CountedAsScp { get; set; } = false;

            [Description("List of roles where at least one of which must be alive for this team to spawn. Ignored if empty.")]
            public List<RoleTypeId> RequiredAliveRoles { get; set; } = [];

            [Description("Defines which starting roles can be converted into this team. At the start of the round, the plugin will randomly select players from these roles to respawn as this team. This option only works if 'SpawnWave' is set to 'RoundStarted'.")]
            public List<RoleTypeId> RolesAffectedOnRoundStart { get; set; } = [];

            [Description("Setting a SpawnDelay greater than 0 will not work when using NtfWave or ChaosWave!")]
            public float SpawnDelay { get; set; } = 0f;

            /// <summary>
            /// Whether this spawn type requires a defined spawn position.
            /// </summary>
            /// <returns><c>true</c> if the spawn position is required; otherwise, <c>false</c>.</returns>
            public bool RequiresSpawnPosition()
            {
                return SpawnWave == WaveType.AfterDecontamination || SpawnWave == WaveType.AfterWarhead || SpawnWave == WaveType.RoundStarted || SpawnWave == WaveType.ScpDeath || SpawnWave == WaveType.UsedItem || SpawnWave == WaveType.TeamDependent || SpawnWave == WaveType.AfterGeneratorActivated;
            }
        }
        /// <summary>
        /// Represents a single sound entry with its path and a delay before it's played.
        /// </summary>
        public class SoundPathEntry
        {
            /// <summary>
            /// The path to the sound file.
            /// </summary>
            [Description("The path to the .ogg sound file.")]
            public string Path { get; set; } = "/path/to/your/ogg/file";

            /// <summary>
            /// The delay in seconds before this sound is played.
            /// </summary>
            [Description("Delay in seconds before this sound starts playing.")]
            public float Delay { get; set; } = 0f;
        }

        public class RoundEndRule
        {
            [Description("If true, the round will not end while this team is alive, effectively blocking standard round-end conditions. NOTE: This does NOT prevent the round from ending if this team wins by eliminating all enemies. If they win, the round ends immediately.")]
            public bool PreventRoundEndIfAlive { get; set; } = true;
            [Description("A list of vanilla teams that are considered allies. The Custom Team does NOT need to eliminate players from these teams to trigger the win condition.")]
            public List<PlayerRoles.Team> AlliedTeams { get; set; } = [];

            [Description("The specific faction to declare as the winner when the win condition is met.")]
            public LeadingTeam WinningTeam { get; set; } = LeadingTeam.Draw;
        }
    }
}

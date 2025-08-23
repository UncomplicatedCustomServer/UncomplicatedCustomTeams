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
        /// <summary>
        /// Gets a complete list of every custom <see cref="Team"/> registered
        /// </summary>
        public static List<Team> List { get; } = new();

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

        /// <summary>
        /// A list of sounds to be played sequentially when the team spawns.
        /// Requires AudioPlayerAPI. Download it here: https://github.com/Killers0992/AudioPlayerApi
        /// </summary>
        [Description("A list of sounds to be played sequentially. Requires AudioPlayerAPI.")]
        public List<SoundPathEntry> SoundPaths { get; set; } = [new()];

        /// <summary>
        /// Volume of the sound, should be between 1 and 100.
        /// </summary>
        public float SoundVolume { get; set; } = 1f;

        /// <summary>
        /// A list of PlayerRoles.Team whose presence on the map guarantees victory with custom team.
        /// </summary>
        [Description("Here, you can define which teams will win against your custom team.")]
        public List<PlayerRoles.Team> TeamAliveToWin { get; set; } = [];

        /// <summary>
        /// Retrieves a list of actual PlayerRoles.Team enums based on the teams in TeamAliveToWin.
        /// </summary>
        public static List<PlayerRoles.Team> GetWinningTeams()
        {
            return [.. Team.List
                .SelectMany(team => team.TeamAliveToWin)
                .Distinct()];
        }

        /// <summary>
        /// The list of every role that will be a part of this wave
        /// </summary>
        [YamlIgnore]
        public List<IUCTCustomRole> TeamRoles => Roles.OfType<IUCTCustomRole>().Concat(EcrRoles).ToList();

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
        public List<ExiledCustomRole> EcrRoles { get; set; } = new()
        {
            new()
            {
                Id = 1,
                Priority = RolePriority.None,
                MaxPlayers = 1,
                DropInventoryOnDeath = true
            }
        };

        public static Team EvaluateSpawn(WaveType wave)
        {
            List<Team> Teams = [];
            foreach (Team Team in List.Where(t => t.SpawnConditions.SpawnWave == wave))
            {
                for (int a = 0; a < Team.SpawnChance; a++)
                    Teams.Add(Team);
            }
            LogManager.Debug($"Evaluated team count, found {Teams.Count}/100 elements [{List.Count(t => t.SpawnConditions.SpawnWave == wave)}]!\n If the number is less than 100 THERE'S A PROBLEM!");

            if (Teams.Count == 0)
            {
                LogManager.Debug("No valid team found, returning...");
                return null;
            }
            int Chance = new System.Random().Next(0, 99);
            return Teams.Count > Chance ? Teams[Chance] : null;
        }

        public class SpawnData
        {
            public WaveType SpawnWave { get; set; } = WaveType.NtfWave;
            public Vector3 SpawnPosition { get; set; } = Vector3.zero;
            public Vector3 SpawnRotation { get; set; } = Vector3.zero; // yaml has skill issue with Quaternion

            private ItemType _usedItem = ItemType.None;
            private int? _customItemId = null;

            [Description("Specify the item or custom item ID that triggers this team spawn. Only works if SpawnWave is set to 'UsedItem'.")]
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
                return SpawnWave == WaveType.AfterDecontamination || SpawnWave == WaveType.AfterWarhead || SpawnWave == WaveType.RoundStarted || SpawnWave == WaveType.ScpDeath || SpawnWave == WaveType.UsedItem;
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
    }
}

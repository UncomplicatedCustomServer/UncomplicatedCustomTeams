using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using static RoundSummary;

namespace UncomplicatedCustomTeams.API.Features.Definitions
{
    /// <summary>
    /// Represents the configuration definition of a Custom Team.
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Gets a complete list of every custom <see cref="Team"/> registered in the plugin.
        /// </summary>
        public static List<Team> List { get; } = [];

        /// <summary>
        /// Registers a new custom <see cref="Team"/> to the system.
        /// </summary>
        /// <param name="team">The team definition to register.</param>
        public static void Register(Team team) => List.Add(team);

        /// <summary>
        /// Unregisters a custom <see cref="Team"/> from the system.
        /// </summary>
        /// <param name="team">The team definition to remove.</param>
        public static void Unregister(Team team) => List.Remove(team);

        /// <summary>
        /// The unique Identifier of the custom team. Must be unique across all teams.
        /// </summary>
        public uint Id { get; set; } = 1;

        /// <summary>
        /// The display name of the custom team.
        /// </summary>
        public string Name { get; set; } = "GOC";

        /// <summary>
        /// The minimum number of players required to spawn this team.
        /// </summary>
        public int MinPlayers { get; set; } = 1;

        /// <summary>
        /// The maximum number of times this team can be spawned in a single round. Set to -1 for unlimited.
        /// </summary>
        public int MaxSpawns { get; set; } = -1;

        [YamlIgnore]
        public int CurrentSpawnCount { get; internal set; } = 0;

        /// <summary>
        /// The percentage chance (0-100) for this team to spawn when its wave condition is met.
        /// </summary>
        public uint SpawnChance { get; set; } = 100;

        /// <summary>
        /// If set to true, this team can spawn even if another team has already been selected for the same wave.
        /// </summary>
        public bool AllowConcurrentSpawns { get; set; } = false;

        /// <summary>
        /// Defines the conditions (When, Where, How) for the team spawn.
        /// </summary>
        public SpawnData SpawnConditions { get; set; } = new();

        public bool IsCassieAnnouncementEnabled { get; set; } = true;
        public string CassieMessage { get; set; } = "team arrived";
        public string CassieTranslation { get; set; } = "Team arrived!";
        public bool IsNoisy { get; set; } = true;
        public float GlitchScale { get; set; } = 1f;

        /// <summary>
        /// Defines the win conditions and alliances for this team.
        /// </summary>
        public RoundEndRule WinCondition { get; set; } = new();

        public List<SoundPathEntry> SoundPaths { get; set; } = [new()];
        public float SoundVolume { get; set; } = 1f;

        [YamlIgnore]
        public List<IUCTCustomRole> TeamRoles => [.. Roles.OfType<IUCTCustomRole>()];

        /// <summary>
        /// The list of roles that constitute this team.
        /// </summary>
        public List<UncomplicatedCustomRole> Roles { get; set; } = [];

        public class SoundPathEntry
        {
            /// <summary>
            /// The path to the sound file.
            /// </summary>
            public string Path { get; set; } = "/path/to/your/ogg/file";

            /// <summary>
            /// The delay in seconds before this sound is played.
            /// </summary>
            public float Delay { get; set; } = 0f;
        }

        public class RoundEndRule
        {
            public bool PreventRoundEndIfAlive { get; set; } = true;
            public List<PlayerRoles.Team> AlliedTeams { get; set; } = [];
            public LeadingTeam WinningTeam { get; set; } = LeadingTeam.Draw;
        }
    }
}

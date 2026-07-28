using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Integrations;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.API.Features.Runtime
{
    /// <summary>
    /// Represents a live, active instance of a custom team in the current round.
    /// Contains the list of players and runtime data.
    /// </summary>
    public class SummonedTeam
    {
        /// <summary>
        /// Gets a list of all currently active custom teams.
        /// </summary>
        public static List<SummonedTeam> List { get; } = [];

        public static event Action<SummonedTeam> OnTeamSummoned;

        private readonly Dictionary<string, object> _metadata = [];

        /// <summary>
        /// Unique Runtime ID (GUID) for this specific team instance.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The configuration definition (<see cref="Team"/>) that this instance was created from.
        /// </summary>
        public Team Definition { get; }

        /// <summary>
        /// Gets a value indicating whether this team has been completely eliminated.
        /// </summary>
        public bool IsEliminated { get; internal set; } = false;

        /// <summary>
        /// The list of players currently assigned to this team.
        /// </summary>
        public List<SummonedCustomRole> Members { get; } = [];

        /// <summary>
        /// The Unix timestamp when this team was spawned.
        /// </summary>
        public long SpawnTime { get; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private SummonedTeam(Team definition)
        {
            Definition = definition;
            List.Add(this);
        }

        /// <summary>
        /// Factory method that creates a new <see cref="SummonedTeam"/>, assigns roles to players, and handles spawning logic.
        /// </summary>
        /// <param name="definition">The team configuration to use.</param>
        /// <param name="players">The list of players to add to this team.</param>
        /// <returns>The created <see cref="SummonedTeam"/> instance.</returns>
        public static SummonedTeam Create(Team definition, Dictionary<Player, IUCTCustomRole> playerRoles)
        {
            var instance = new SummonedTeam(definition);

            instance.AssignRoles(playerRoles);
            instance.SpawnMembers();
            AudioPlayer.PlayTeamAnnouncement(definition);

            definition.CurrentSpawnCount++;

            OnTeamSummoned?.Invoke(instance);
            UCTEvents.InvokeTeamSpawned(new(instance));

            LogManager.Info($"Team {definition.Name} spawned with {instance.Members.Count} players.");

            return instance;
        }

        private void AssignRoles(Dictionary<Player, IUCTCustomRole> playerRoles)
        {
            Plugin.CachedSpawnList.Clear();
            Bucket.SpawnBucket.Clear();

            foreach (var kvp in playerRoles)
            {
                Player player = kvp.Key;
                IUCTCustomRole role = kvp.Value;

                var member = new SummonedCustomRole(this, player, role);
                Members.Add(member);

                Plugin.CachedSpawnList.Add(player);
                Bucket.SpawnBucket.Add(player.PlayerId);

                LogManager.Debug($"{player.Nickname} assigned to {role.Name} (Priority: {role.Priority}) based on TeamSpawner evaluation.");
            }
        }

        private void SpawnMembers()
        {
            foreach (var member in Members)
            {
                member.AddRole();
            }
        }
        /// <summary>
        /// Sends a broadcast or hint message to all alive members of this team.
        /// </summary>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="message">The content of the message.</param>
        /// <param name="IsHint">If true, uses Hints; otherwise uses Broadcast.</param>
        public void BroadcastToMembers(ushort duration, string message, bool IsHint = false)
        {
            foreach (var member in Members)
            {
                if (member.Player != null && member.Player.IsAlive)
                {
                    if (IsHint)
                        member.Player.SendHint(message, duration);
                    else
                        member.Player.SendBroadcast(message, duration);
                }
            }
        }

        public int CountMembersWithRole(IUCTCustomRole role) => Members.Count(m => m.CustomRole == role);

        /// <summary>
        /// Destroys this team instance and cleans up references.
        /// </summary>
        public void Destroy()
        {
            foreach (var member in Members) member.Destroy();
            Members.Clear();
            List.Remove(this);
        }

        /// <summary>
        /// Sets a custom metadata value attached to this specific team instance.
        /// </summary>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <param name="key">The key to identify the data.</param>
        /// <param name="value">The value to store.</param>
        public void SetData<T>(string key, T value)
        {
            if (_metadata.ContainsKey(key))
                _metadata[key] = value;
            else
                _metadata.Add(key, value);
        }

        /// <summary>
        /// Retrieves a custom metadata value attached to this team instance.
        /// </summary>
        /// <typeparam name="T">The expected type of the data.</typeparam>
        /// <param name="key">The key of the data.</param>
        /// <returns>The value if found, otherwise default(T).</returns>
        public T GetData<T>(string key)
        {
            if (_metadata.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                    return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Checks if the team contains specific metadata key.
        /// </summary>
        public bool HasData(string key) => _metadata.ContainsKey(key);

        /// <summary>
        /// Removes a metadata entry by key.
        /// </summary>
        public void RemoveData(string key) => _metadata.Remove(key);

        /// <summary>
        /// Finds an active <see cref="SummonedTeam"/> by its Runtime ID.
        /// </summary>
        public static SummonedTeam Get(string id) => List.FirstOrDefault(t => t.Id == id);

        /// <summary>
        /// Checks if the specified player is a member of any active custom team.
        /// </summary>
        public static bool IsPlayerInCustomTeam(Player player) => List.Any(t => t.Members.Any(m => m.Player == player));
    }
}

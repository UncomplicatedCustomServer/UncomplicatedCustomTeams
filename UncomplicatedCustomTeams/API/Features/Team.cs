﻿using PlayerRoles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

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
        [Description("The Id of the custom Team")]
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
        /// The chance of spawning of this custom <see cref="Team"/>.
        /// 0 is 0% and 100 is 100%!
        /// </summary>
        public uint SpawnChance { get; set; } = 100;

        /// <summary>
        /// Defines the spawn conditions for a custom team.
        /// </summary>
        public SpawnData SpawnConditions { get; set; } = new();

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
        /// The path to the sound file provided by the user in the configuration.
        /// </summary>
        [Description("Requires AudioPlayerAPI. Download it here: https://github.com/Killers0992/AudioPlayerApi")]
        public string SoundPath { get; set; } = "/path/to/your/ogg/file";

        /// <summary>
        /// Volume of the sound, should be between 1 and 100.
        /// </summary>
        public float SoundVolume { get; set; } = 1f;

        /// <summary>
        /// A list of PlayerRoles.Team whose presence on the map guarantees victory with custom team.
        /// </summary>
        [Description("Here, you can define which teams will win against your custom team.")]
        public List<PlayerRoles.Team> TeamAliveToWin { get; set; } = new();

        /// <summary>
        /// Retrieves a list of actual PlayerRoles.Team enums based on the teams in TeamAliveToWin.
        /// </summary>
        public static List<PlayerRoles.Team> GetWinningTeams()
        {
            return Team.List
                .SelectMany(team => team.TeamAliveToWin)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// The list of every role that will be a part of this wave
        /// </summary>
        public List<CustomRole> Roles { get; set; } = new()
        {
            new()
            {
                Id = 1,
                Team = PlayerRoles.Team.ClassD,
                SpawnSettings = null,
                CanEscape = false,
                RoleAfterEscape = null,
                MaxPlayers = 1,
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
                MaxPlayers = 1
            }
        };

        public static Team EvaluateSpawn(string wave)
        {
            List<Team> Teams = new();
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
            public string SpawnWave { get; set; } = "NtfWave";
            public Vector3 SpawnPosition { get; set; } = Vector3.zero;

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
                    else if (Enum.TryParse(value, true, out ItemType itemType))
                    {
                        _usedItem = itemType;
                        _customItemId = null;
                    }
                }
            }

            public ItemType GetUsedItemType() => _usedItem;
            public int? GetCustomItemId() => _customItemId;

            [Description("Specify the SCP role whose death triggers this team spawn. Only works if SpawnWave is set to 'ScpDeath'.")]
            public string TargetScp { get; set; } = "None";

            [Description("Setting a SpawnDelay greater than 0 will not work when using NtfWave or ChaosWave!")]
            public float SpawnDelay { get; set; } = 0f;

            public bool RequiresSpawnType()
            {
                return SpawnWave is "AfterWarhead" or "AfterDecontamination" or "UsedItem" or "RoundStarted" or "ScpDeath";
            }

        }

    }
}

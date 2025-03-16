﻿using Exiled.API.Enums;
using PlayerRoles;
using Respawning;
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

        public SpawnConditions spawnConditions { get; set; } = new();

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
        /// A list of team names whose presence on the map guarantees victory.
        /// </summary>
        [Description("Here, you can define which teams will win against your custom team. Make sure to also specify the team for your custom roles.")]
        public List<PlayerRoles.Team> TeamAliveToWin { get; set; } = new();

        /// <summary>
        /// Retrieves a list of actual Team objects based on the names in TeamAliveToWin.
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
                MaxPlayers = 1,
                CustomFlags = null,
            },
            new()
            {
                Id = 2,
                Team = PlayerRoles.Team.ClassD,
                SpawnSettings = null,
                CustomFlags = null,
                MaxPlayers = 500
            }
        };

        public static Team EvaluateSpawn(string wave)
        {
            List<Team> Teams = new();

            foreach (Team Team in List.Where(t => t.spawnConditions.SpawnWave == wave))
                for (int a = 0; a < Team.SpawnChance; a++)
                    Teams.Add(Team);

            LogManager.Debug($"Evaluated team count, found {Teams.Count}/100 elements [{List.Count(t => t.spawnConditions.SpawnWave == wave)}]!\nIf the number is less than 100 THERE's A PROBLEM!");

            if (Teams.Count == 0)
                return null;

            System.Random random = new System.Random();
            int Chance = random.Next(0, Teams.Count);
            return Teams[Chance];
        }
    }
    public class SpawnConditions
    {
        public string SpawnWave { get; set; } = "NtfWave";
        public Vector3 SpawnPosition { get; set; } = Vector3.zero;
        public float Offset { get; set; } = 0f;
    }
}

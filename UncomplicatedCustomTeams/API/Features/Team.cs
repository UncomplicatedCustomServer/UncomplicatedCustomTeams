﻿using Exiled.API.Extensions;
using Exiled.API.Features;
using Respawning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.API.Features
{
    public class Team
    {
        /// <summary>
        /// Gets a complete list of every custom <see cref="Team"/> registered
        /// </summary>
        public static List<Team> List { get; } = new();

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
        /// The wave that will be replaced by this custom wave
        /// </summary>
        public SpawnableTeamType SpawnWave { get; set; } = SpawnableTeamType.NineTailedFox;

        /// <summary>
        /// The list of every role that will be a part of this wave
        /// </summary>
        public List<CustomRole> Roles { get; set; } = new()
        {
            new()
            {
                SpawnSettings = null,
                MaxPlayers = 1,
            },
            new()
            {
                Id = 2,
                SpawnSettings = null,
                MaxPlayers = 500
            }
        };

        public static Team EvaluateSpawn(SpawnableTeamType wave)
        {
            List<Team> Teams = new();

            foreach (Team Team in List.Where(t => t.SpawnWave == wave))
                for (int a = 0; a < Team.SpawnChance; a++)
                    Teams.Add(Team);

            LogManager.Debug($"Evaluated team count, found {Teams.Count}/100 elements [{List.Where(t => t.SpawnWave == wave).Count()}]!\nIf the number is less than 100 THERE's A PROBLEM!");

            int Chance = new Random().Next(0, 99);
            if (Teams.Count > Chance)
                return Teams[Chance];

            return null;
        }
    }
}
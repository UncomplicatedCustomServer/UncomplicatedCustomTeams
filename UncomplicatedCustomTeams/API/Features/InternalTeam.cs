using Respawning;
using System.Collections.Generic;
using System.ComponentModel;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomRoles.API.Interfaces;
using UnityEngine;

namespace UncomplicatedCustomTeams.API.Features
{
    public class InternalTeam
    {       
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
        /// The SpawnPosition of the wave.<br></br>
        /// If Vector3.zero or Vector3.one then it will be retrived from the RoleTypeId
        /// </summary>
        public Vector3 SpawnPosition { get; set; } = Vector3.zero;

        /// <summary>
        /// The cassie message that will be sent when the team spawn - empty to disable
        /// </summary>
        public string CassieMessage { get; set; } = "team arrived";

        /// <summary>
        /// The translation of the cassie message
        /// </summary>
        public string CassieTranslation { get; set; } = "Team arrived!";

        /// <summary>
        /// The list of every role that will be a part of this wave
        /// </summary>
        public List<InternalCustomRole> Roles { get; set; }


        public static implicit operator InternalTeam(Team team)
        {
            InternalTeam newTeam = new()
            {
                Id = team.Id,
                Name = team.Name,
                MinPlayers = team.MinPlayers,
                SpawnChance = team.SpawnChance,
                SpawnWave = team.SpawnWave,
                SpawnPosition = team.SpawnPosition,
                CassieMessage = team.CassieMessage,
                CassieTranslation = team.CassieTranslation,
                Roles = new()
            };

            foreach (EssentialCustomRole role in team.Roles)
            {
                ICustomRole customRole = CustomRole.Get(role.Id);
                newTeam.Roles.Add(new()
                {
                    MaxPlayers = role.MaxPlayers,
                    Id = customRole.Id,
                    Name = customRole.Name,
                    OverrideRoleName = customRole.OverrideRoleName,
                    Nickname = customRole.Nickname,
                    CustomInfo = customRole.CustomInfo,
                    BadgeName = customRole.BadgeName,
                    BadgeColor = customRole.BadgeColor,
                    Role = customRole.Role,
                    Team = customRole.Team,
                    RoleAppearance = customRole.RoleAppearance,
                    IsFriendOf = customRole.IsFriendOf,
                    Health = customRole.Health,
                    Ahp = customRole.Ahp,
                    Effects = customRole.Effects,
                    Stamina = customRole.Stamina,
                    MaxScp330Candies = customRole.MaxScp330Candies,
                    CanEscape = customRole.CanEscape,
                    RoleAfterEscape = customRole.RoleAfterEscape,
                    Scale = customRole.Scale,
                    SpawnBroadcast = customRole.SpawnBroadcast,
                    SpawnBroadcastDuration = customRole.SpawnBroadcastDuration,
                    SpawnHint = customRole.SpawnHint,
                    SpawnHintDuration = customRole.SpawnHintDuration,
                    CustomInventoryLimits = customRole.CustomInventoryLimits,
                    Inventory = customRole.Inventory,
                    CustomItemsInventory = customRole.CustomItemsInventory,
                    Ammo = customRole.Ammo,
                    DamageMultiplier = customRole.DamageMultiplier,
                    SpawnSettings = customRole.SpawnSettings,
                    CustomFlags = customRole.CustomFlags,
                    IgnoreSpawnSystem = customRole.IgnoreSpawnSystem,
                });
            }

            return newTeam;
        }
    }
}

using LabApi.Features.Wrappers;
using PlayerRoles;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

namespace UncomplicatedCustomTeams.API.Features.Runtime
{
    public class SummonedCustomRole
    {
        /// <summary>
        /// The <see cref="LabApi.Features.Wrappers.Player"/> instance
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// The CustomRole instance for the given player
        /// </summary>
        public IUCTCustomRole CustomRole { get; }

        public SummonedTeam Team { get; }

        /// <summary>
        /// Indicate wether the custom role has been assigned or not
        /// </summary>
        public bool IsRoleSet { get; private set; } = false;


        public SummonedCustomRole(SummonedTeam team, Player player, IUCTCustomRole role)
        {
            Player = player;
            Team = team;

            if (role is UncomplicatedCustomRole uctRole && UncomplicatedCustomRoles.API.Features.CustomRole.TryGet(uctRole.Id, out var existingUcrRole))
            {
                if (existingUcrRole is IUCTCustomRole uctImportedRole)
                {
                    CustomRole = uctImportedRole;
                }
                else
                {
                    CustomRole = role;
                }
            }
            else
            {
                CustomRole = role;
            }
        }

        public void Destroy()
        {
            if (Player.IsAlive)
                Player.TryRemoveCustomRole();
        }

        private void ApplyRoleSettings()
        {
            if (CustomRole.IsGodmodeEnabled)
            {
                Player.IsGodModeEnabled = true;
                LogManager.Debug($"{CustomRole.Name} is about to receive GodMode. Enabling...");
            }

            if (CustomRole.IsBypassEnabled)
            {
                Player.IsBypassEnabled = true;
                LogManager.Debug($"{CustomRole.Name} is about to receive Bypass. Enabling...");
            }

            if (CustomRole.IsNoclipEnabled)
            {
                Player.IsNoclipEnabled = true;
                LogManager.Debug($"{CustomRole.Name} is about to receive Noclip. Enabling...");
            }
        }
        public void AddRole(RoleTypeId? proposed = null)
        {
            if (CustomRole is UncomplicatedCustomRole uctRole)
            {
                RoleManager.EnsureIsRegistered(uctRole);
            }

            RoleTypeId finalRole = proposed ?? RoleTypeId.ChaosConscript;

            Player.SetRole(CustomRole.Role, RoleChangeReason.Respawn, RoleSpawnFlags.None);

            if (Player.Role != CustomRole.Role)
            {
                LogManager.Debug($"Role assignment failed! Falling back to {finalRole}.");
                Player.SetRole(finalRole, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);
            }
            Vector3 spawnPos;
            if (Team.Definition.SpawnConditions.GetSpawnPosition() != Vector3.zero)
            {
                spawnPos = Team.Definition.SpawnConditions.GetSpawnPosition();
                LogManager.Debug($"Using custom Vector3 spawn position: {spawnPos}");
            }
            else
            {
                switch (Team.Definition.SpawnConditions.SpawnWave)
                {
                    case Enums.WaveType.NtfWave:
                        spawnPos = RoleTypeId.NtfCaptain.GetRandomSpawnLocation();
                        LogManager.Debug($"Using NTF spawn position for role: {CustomRole.Role}");
                        break;

                    case Enums.WaveType.ChaosWave:
                        spawnPos = RoleTypeId.ChaosConscript.GetRandomSpawnLocation();
                        LogManager.Debug($"Using Chaos spawn position for role: {CustomRole.Role}");
                        break;

                    default:
                        spawnPos = finalRole.GetRandomSpawnLocation();
                        LogManager.Debug($"Using fallback spawn for role: {CustomRole.Role}");
                        break;
                }
            }
            CustomRole.Spawn(Player);

            Vector3 spawnAngle = Team.Definition.SpawnConditions.GetSpawnRotation();
            Quaternion spawnRot = Quaternion.Euler(spawnAngle);
            Player.Position = spawnPos;
            Player.Rotation = spawnRot;
            ApplyRoleSettings();
            IsRoleSet = true;
        }
    }
}

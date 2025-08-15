using Exiled.API.Extensions;
using Exiled.API.Features;
using PlayerRoles;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

namespace UncomplicatedCustomTeams.API.Features
{
    public class SummonedCustomRole(SummonedTeam team, Player player, IUCTCustomRole role)
    {
        /// <summary>
        /// The <see cref="Exiled.API.Features.Player"/> instance
        /// </summary>
        public Player Player { get; } = player;

        /// <summary>
        /// The CustomRole instance for the given player
        /// </summary>
        public IUCTCustomRole CustomRole { get; } = role;

        public SummonedTeam Team { get; } = team;

        /// <summary>
        /// Indicate wether the custom role has been assigned or not
        /// </summary>
        public bool IsRoleSet { get; private set; } = false;

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
            else
            {
                Player.IsGodModeEnabled = false;
            }

            if (CustomRole.IsBypassEnabled)
            {
                Player.IsBypassModeEnabled = true;
                LogManager.Debug($"{CustomRole.Name} is about to receive Bypass. Enabling...");
            }
            else
            {
                Player.IsBypassModeEnabled = false;
            }

            if (CustomRole.IsNoclipEnabled)
            {
                Player.IsNoclipPermitted = true;
                LogManager.Debug($"{CustomRole.Name} is about to receive Noclip. Enabling...");
            }
            else
            {
                Player.IsNoclipPermitted = false;
            }
        }
        public void AddRole(RoleTypeId? proposed = null)
        {
            RoleTypeId finalRole = proposed ?? RoleTypeId.ChaosConscript;
            LogManager.Debug($"Changing role to {finalRole} for player {Player.Nickname} ({Player.Id})");

            Player.Role.Set(CustomRole.Role, Exiled.API.Enums.SpawnReason.Respawn, RoleSpawnFlags.None);

            if (Player.Role != CustomRole.Role)
            {
                LogManager.Debug($"Role assignment failed! Falling back to {finalRole}.");
                Player.Role.Set(finalRole, Exiled.API.Enums.SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            }
            Vector3 spawnPos;
            if (Team.Team.SpawnConditions.SpawnPosition != Vector3.zero)
            {
                spawnPos = Team.Team.SpawnConditions.SpawnPosition;
                LogManager.Debug($"Using custom Vector3 spawn position: {spawnPos}");
            }
            else
            {
                switch (Team.Team.SpawnConditions.SpawnWave)
                {
                    case Enums.WaveType.NtfWave:
                        spawnPos = RoleTypeId.NtfCaptain.GetRandomSpawnLocation().Position;
                        LogManager.Debug($"Using NTF spawn position for role: {CustomRole.Role}");
                        break;

                    case Enums.WaveType.ChaosWave:
                        spawnPos = RoleTypeId.ChaosConscript.GetRandomSpawnLocation().Position;
                        LogManager.Debug($"Using Chaos spawn position for role: {CustomRole.Role}");
                        break;

                    default:
                        spawnPos = finalRole.GetRandomSpawnLocation().Position;
                        LogManager.Debug($"Using fallback spawn for role: {CustomRole.Role}");
                        break;
                }
            }
            CustomRole.Spawn(Player);

            Vector3 spawnAngle = Team.Team.SpawnConditions.SpawnRotation;
            Quaternion spawnRot = Quaternion.Euler(spawnAngle);
            Player.Position = spawnPos;
            Player.Rotation = spawnRot;
            ApplyRoleSettings();
            IsRoleSet = true;
        }
    }
}

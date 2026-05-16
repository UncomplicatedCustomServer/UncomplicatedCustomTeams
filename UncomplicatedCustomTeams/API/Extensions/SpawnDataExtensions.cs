using PlayerRoles;
using System;
using System.Collections.Generic;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UnityEngine;

namespace UncomplicatedCustomTeams.API
{
    public static class SpawnDataExtensions
    {
        /// <summary>
        /// Helper to safely retrieve a value from the Settings dictionary.
        /// </summary>
        public static T GetSetting<T>(this SpawnData data, string key, T defaultValue = default)
        {
            if (data.Settings == null || !data.Settings.TryGetValue(key, out object value))
                return defaultValue;

            try
            {
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value.ToString(), true);
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static Vector3 GetSpawnPosition(this SpawnData data)
        {
            float x = data.GetSetting<float>("pos_x", 0);
            float y = data.GetSetting<float>("pos_y", 0);
            float z = data.GetSetting<float>("pos_z", 0);
            return new Vector3(x, y, z);
        }

        public static Vector3 GetSpawnRotation(this SpawnData data)
        {
            float x = data.GetSetting<float>("rot_x", 0);
            float y = data.GetSetting<float>("rot_y", 0);
            float z = data.GetSetting<float>("rot_z", 0);
            return new Vector3(x, y, z);
        }

        public static int GetRequiredGenerators(this SpawnData data)
        {
            return data.GetSetting<int>("required_engaged", 1);
        }

        public static uint GetAfterTeamSpawn(this SpawnData data)
        {
            return data.GetSetting<uint>("after_team_spawn", 0);
        }

        public static uint GetAfterTeamDeath(this SpawnData data)
        {
            return data.GetSetting<uint>("after_team_death", 0);
        }

        public static ItemType GetUsedItem(this SpawnData data)
        {
            return data.GetSetting<ItemType>("used_item", ItemType.None);
        }

        public static int? GetCustomItemId(this SpawnData data)
        {
            if (data.Settings.TryGetValue("custom_item_id", out object val))
            {
                if (int.TryParse(val.ToString(), out int result))
                    return result;
            }
            return null;
        }

        public static string GetTargetScp(this SpawnData data)
        {
            return data.GetSetting<string>("target_scp", "None");
        }

        public static bool IsScp0492CountedAsScp(this SpawnData data)
        {
            return data.GetSetting<bool>("count_zombies", false);
        }

        public static List<RoleTypeId> GetRequiredAliveRoles(this SpawnData data)
        {
            return GetRoleList(data, "required_roles");
        }

        public static List<RoleTypeId> GetRolesAffectedOnRoundStart(this SpawnData data)
        {
            return GetRoleList(data, "affected_roles");
        }

        private static List<RoleTypeId> GetRoleList(SpawnData data, string key)
        {
            var list = new List<RoleTypeId>();

            if (data.Settings.TryGetValue(key, out object val) && val is List<object> objList)
            {
                foreach (var item in objList)
                {
                    if (Enum.TryParse(item.ToString(), true, out RoleTypeId role))
                    {
                        list.Add(role);
                    }
                }
            }
            return list;
        }
    }
}
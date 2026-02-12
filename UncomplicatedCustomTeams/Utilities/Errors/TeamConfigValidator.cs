using LabApi.Loader.Features.Yaml;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UncomplicatedCustomTeams.API;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UnityEngine;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.Utilities.Errors
{
    public static class TeamConfigValidator
    {
        public static bool ValidateFile(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);

                if (!Regex.IsMatch(fileContent, @"(?m)^\s*teams\s*:"))
                {
                    LogValidationError(filePath, "'teams:' section not found!", "Ensure your YAML has a 'teams:' section defined at the top level.");
                    return false;
                }

                var deserialized = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, List<Team>>>(fileContent);
                if (deserialized == null || !deserialized.TryGetValue("teams", out var teamList) || teamList == null)
                {
                    LogValidationError(filePath, "No 'teams' key found or list is empty!", "Make sure 'teams:' is correctly defined.");
                    return false;
                }

                foreach (var team in teamList)
                {
                    if (!ValidateSingleTeam(team, filePath))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                string suggestion = ErrorManager.GetSuggestionFromMessage(ex.Message);
                ErrorManager.Add(filePath, ex.Message, suggestion: suggestion);
                LogManager.Error($"Exception processing {Path.GetFileName(filePath)}: {ex.Message}\n Suggestion: {suggestion}");
                return false;
            }
        }

        private static bool ValidateSingleTeam(Team team, string filePath)
        {
            try
            {
                _ = checked((int)team.Id);
            }
            catch (OverflowException)
            {
                LogValidationError(filePath, $"Team ID {team.Id} is too large for an int!", "Use a smaller number for the team ID.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(team.Name))
            {
                LogValidationError(filePath, $"Missing team name for ID {team.Id}!", "Make sure each team has a 'name' field.");
                return false;
            }

            if (!ValidateSpawnConditions(team, filePath)) return false;

            if (!ValidateRoles(team, filePath)) return false;

            return true;
        }

        public static bool ValidateAndSanitizeTeam(Team team, string file)
        {
            bool hasCustomSound = team.SoundPaths != null && team.SoundPaths.Any(s => !string.IsNullOrEmpty(s.Path) && !s.Path.Contains("/path/to/your"));
            if (hasCustomSound && team.IsCassieAnnouncementEnabled)
            {
                LogManager.Warn($"Team \"{team.Name}\" (ID: {team.Id}) has both Cassie and SoundPath defined. Both will play simultaneously.");
            }

            if ((team.SpawnConditions.SpawnWave == WaveType.NtfWave || team.SpawnConditions.SpawnWave == WaveType.ChaosWave)
                && team.SpawnConditions.SpawnDelay > 0)
            {
                string warning = $"SpawnWave '{team.SpawnConditions.SpawnWave}' won't work with SpawnDelay.";
                string suggestion = "Set 'SpawnDelay' to 0 if you are using NtfWave or ChaosWave.";

                ErrorManager.Add(file, warning, suggestion: suggestion);
                LogManager.Warn($"{warning} -> Ignoring delay for team '{team.Name}'.");

                team.SpawnConditions.SpawnDelay = 0f;
            }

            if (TeamExtensions.IsCustomPositionWave(team.SpawnConditions.SpawnWave) && team.SpawnConditions.GetSpawnPosition() == Vector3.zero)
            {
                LogValidationError(file,
                    $"SpawnWave '{team.SpawnConditions.SpawnWave}' requires a SpawnPosition, but none was set (0,0,0).",
                    "Add pos_x, pos_y, pos_z to settings for this Custom Team Spawn Wave type.");
                return false;
            }

            if (team.SpawnConditions.SpawnWave == WaveType.ScpDeath)
            {
                string target = team.SpawnConditions.GetTargetScp();
                if (string.IsNullOrWhiteSpace(target) || target.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    LogValidationError(file,
                        "Spawn Wave is 'ScpDeath' but no TargetScp is specified in settings.",
                        "Add 'target_scp: Scp173' (or other) to settings.");
                    return false;
                }
            }

            bool hasItemDefined = team.SpawnConditions.GetUsedItem() != ItemType.None || team.SpawnConditions.GetCustomItemId() != null;

            if (hasItemDefined && team.SpawnConditions.SpawnWave != WaveType.UsedItem)
            {
                LogValidationError(file,
                    "An Item is defined in settings, but SpawnWave is not 'UsedItem'.",
                    "Change SpawnWave to 'UsedItem' or remove the item from settings.");
                return false;
            }

            if (!hasItemDefined && team.SpawnConditions.SpawnWave == WaveType.UsedItem)
            {
                LogValidationError(file,
                    "SpawnWave is 'UsedItem', but no valid ItemType or Custom Item ID is provided in settings.",
                    "Add for example 'used_item: Medkit' or 'custom_item_id: 1' to settings.");
                return false;
            }

            if (Team.List.Any(t => t.Id == team.Id))
            {
                uint originalId = team.Id;
                uint newId = 1;
                HashSet<uint> usedIds = [.. Team.List.Select(t => t.Id)];

                while (usedIds.Contains(newId)) newId++;

                string warning = $"Duplicate team ID detected: {originalId}. Automatically reassigning to ID: {newId}.";
                string suggestion = $"Update config for team '{team.Name}' to use ID: {newId} to avoid this warning.";

                ErrorManager.Add(file, warning, suggestion: suggestion);
                LogManager.Warn(warning);

                team.Id = newId;
            }

            return true;
        }

        private static bool ValidateSpawnConditions(Team team, string filePath)
        {
            if (team.SpawnConditions == null)
            {
                LogValidationError(filePath, $"Missing 'SpawnConditions' for team {team.Name}!", "Ensure the 'SpawnConditions' block is present.");
                return false;
            }

            if (!Enum.IsDefined(typeof(WaveType), team.SpawnConditions.SpawnWave))
            {
                LogValidationError(filePath, $"Invalid SpawnWave '{team.SpawnConditions.SpawnWave}' for team {team.Name}.", $"Valid values: {string.Join(", ", Enum.GetNames(typeof(WaveType)))}");
                return false;
            }

            if (team.SpawnConditions.SpawnWave == WaveType.ScpDeath)
            {
                string targetScp = team.SpawnConditions.GetTargetScp();
                bool isValidScp = (Enum.TryParse(targetScp, true, out RoleTypeId _) && targetScp.StartsWith("Scp", StringComparison.OrdinalIgnoreCase))
                                  || targetScp.Equals("SCPs", StringComparison.OrdinalIgnoreCase);

                if (!isValidScp)
                {
                    LogValidationError(filePath, $"Invalid TargetSCP '{targetScp}' in settings for team {team.Name}.", "TargetSCP must be a valid SCP RoleTypeId or 'SCPs'.");
                    return false;
                }
            }

            if (team.SpawnConditions.SpawnDelay < 0)
            {
                LogValidationError(filePath, $"Invalid SpawnDelay for team {team.Name}.", "Set 'SpawnDelay' >= 0.");
                return false;
            }

            if (team.MinPlayers <= 0)
            {
                LogValidationError(filePath, $"Invalid MinPlayers for team {team.Name}.", "Set 'MinPlayers' >= 1.");
                return false;
            }

            if (team.SpawnChance < 0 || team.SpawnChance > 100)
            {
                LogValidationError(filePath, $"Invalid SpawnChance for team {team.Name}.", "Must be between 0 and 100.");
                return false;
            }

            return true;
        }

        private static bool ValidateRoles(Team team, string filePath)
        {
            if (team.TeamRoles == null || team.TeamRoles.Count == 0)
            {
                LogValidationError(filePath, $"Team {team.Name} has no roles defined!", "Define at least one role using 'roles:'.");
                return false;
            }

            HashSet<int> roleIds = [];

            foreach (var role in team.TeamRoles)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                {
                    LogValidationError(filePath, $"A role in team {team.Name} has no name!", "Each role must include a name.");
                    return false;
                }

                if (role is UncomplicatedCustomRole && !roleIds.Add(role.Id))
                {
                    LogValidationError(filePath, $"Duplicate role ID {role.Id} in team {team.Name}!", "Each role ID must be unique within its team.");
                    return false;
                }

                if (role.MaxPlayers <= 0)
                {
                    LogValidationError(filePath, $"Role '{role.Name}' has invalid MaxPlayers.", "Set 'MaxPlayers' >= 1.");
                    return false;
                }

                if (!Enum.IsDefined(typeof(RolePriority), role.Priority))
                {
                    LogValidationError(filePath, $"Role '{role.Name}' has invalid Priority.", $"Valid values: {string.Join(", ", Enum.GetNames(typeof(RolePriority)))}");
                    return false;
                }
            }

            return true;
        }

        private static void LogValidationError(string file, string message, string suggestion)
        {
            ErrorManager.Add(file, message, suggestion: suggestion);
            LogManager.Error($"{message}\n {suggestion}");
        }
    }
}
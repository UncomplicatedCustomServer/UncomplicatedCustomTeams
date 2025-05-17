using Exiled.Loader;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.API.Features;
using UnityEngine;

namespace UncomplicatedCustomTeams.Utilities
{
    public class YamlError
    {
        public string File { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string Message { get; set; }
        public string Suggestion { get; set; }
    }

    public static class ErrorManager
    {
        public static List<YamlError> Errors { get; } = new();

        public static void Add(string file, string message, int? line = null, int? column = null, string suggestion = "")
        {
            Errors.Add(new YamlError
            {
                File = file,
                Message = message,
                Line = line,
                Column = column,
                Suggestion = suggestion
            });
        }
        public static void Clear() => Errors.Clear();

        public static string GetSuggestionFromMessage(string message)
        {
            message = message.ToLowerInvariant();

            if (message.Contains("mapping values are not allowed"))
                return "Make sure there is a space after the colon (e.g., `name: GOC` instead of `name:GOC`).";

            if (message.Contains("expected 'mappingstart', got 'sequencestart'"))
                return "Your YAML file begins with a list (`- item`) but should begin with a mapping. Try adding a top-level key like `teams:` before your list.";

            if (message.Contains("while parsing a block mapping"))
                return "Check indentation and YAML structure — something might be misaligned or nested incorrectly.";

            if (message.Contains("expected <block end>, but found"))
                return "Possibly missing a `-` for a list item or the element ends prematurely.";

            if (message.Contains("did not find expected key"))
                return "A key may be missing or misaligned — ensure all keys are followed by colons and correctly indented.";

            if (message.Contains("unexpected end of stream"))
                return "The file might be cut off unexpectedly — check for missing closing brackets or incomplete blocks.";

            if (message.Contains("duplicate key"))
                return "You may have defined the same key twice in the same block — YAML requires keys to be unique.";

            if (message.Contains("found character that cannot start any token"))
                return "There's probably an illegal character or wrong symbol — double-check for stray tabs or weird characters.";

            if (message.Contains("found unexpected ':'"))
                return "There might be a colon `:` in a value that should be quoted — try wrapping the value in quotes.";

            if (message.Contains("anchor") && message.Contains("not defined"))
                return "You're referencing an anchor (&value or *value) that hasn't been defined.";

            if (message.Contains("alias") && message.Contains("not found"))
                return "YAML alias (*) points to something that doesn't exist — check spelling or anchor placement.";

            if (message.Contains("cannot convert") && message.Contains("to"))
                return "A value might be of the wrong type — make sure it's in the correct format (e.g., number vs string).";

            if (message.Contains("sequence entries are not allowed here"))
                return "You're probably using a list (`- item`) in an invalid place — check indentation and nesting.";

            if (message.Contains("unexpected key") || message.Contains("unexpected property"))
                return "This key may be misplaced or invalid — double-check your schema or property names.";

            return "Check your YAML syntax near this location. Be sure indentation, colons, and types are correct.";
        }

        public static bool CustomTypeChecker(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);

                if (!Regex.IsMatch(fileContent, @"(?m)^\s*teams\s*:"))
                {
                    const string message = "'teams:' section not found!";
                    const string suggestion = "Ensure your YAML has a 'teams:' section defined at the top level.";
                    ErrorManager.Add(filePath, message, suggestion: suggestion);
                    LogManager.Error($" {message} {suggestion}");
                    return false;
                }

                var deserialized = Loader.Deserializer.Deserialize<Dictionary<string, List<UncomplicatedCustomTeams.API.Features.Team>>>(fileContent);
                if (!deserialized.TryGetValue("teams", out var teamList))
                {
                    string message = "No 'teams' key found in YAML!";
                    string suggestion = "Make sure 'teams:' is correctly defined.";
                    ErrorManager.Add(filePath, message, suggestion: suggestion);
                    LogManager.Error($"{message}\n{suggestion}");
                    return false;
                }

                HashSet<int> teamIds = new();
                HashSet<string> teamNames = new();

                foreach (var team in teamList)
                {
                    int teamId;
                    try
                    {
                        teamId = checked((int)team.Id);
                    }
                    catch (OverflowException)
                    {
                        string message = $"Team ID {team.Id} is too large for an int!";
                        string suggestion = "Use a smaller number for the team ID.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(team.Name))
                    {
                        string message = $"Missing team name for ID {teamId}!";
                        string suggestion = "Make sure each team has a 'name' field with a valid value.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }
                }

                string[] validSpawnTypes = new[] { "AfterWarhead", "AfterDecontamination", "UsedItem", "RoundStarted", "NtfWave", "ChaosWave", "ScpDeath" };

                foreach (var team in teamList)
                {
                    if (team.SpawnConditions == null)
                    {
                        string message = $"Missing 'SpawnConditions' for team {team.Name} (ID: {team.Id})!";
                        string suggestion = "Ensure the 'SpawnConditions' block is present and properly defined for each team.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(team.SpawnConditions.SpawnWave) || !validSpawnTypes.Contains(team.SpawnConditions.SpawnWave))
                    {
                        string message = $"Invalid SpawnWave value '{team.SpawnConditions.SpawnWave}' for team {team.Name} (ID: {team.Id}).";
                        string suggestion = $"Valid values are: {string.Join(", ", validSpawnTypes)}.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    string targetScp = team.SpawnConditions.TargetScp;
                    if (!string.IsNullOrWhiteSpace(targetScp) && !targetScp.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isValidScp =
                            (Enum.TryParse(targetScp, true, out RoleTypeId parsedRole) && targetScp.StartsWith("Scp", StringComparison.OrdinalIgnoreCase))
                            || targetScp.Equals("SCPs", StringComparison.OrdinalIgnoreCase);

                        if (!isValidScp)
                        {
                            string message = $"Invalid TargetSCP value '{targetScp}' for team {team.Name} (ID: {team.Id}).";
                            string suggestion = "TargetSCP must be a valid SCP RoleTypeId (e.g., Scp173, Scp049, Scp939) or 'SCPs'.";
                            ErrorManager.Add(filePath, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n{suggestion}");
                            return false;
                        }
                    }

                    if (team.SpawnConditions.SpawnDelay < 0)
                    {
                        string message = $"Invalid SpawnDelay value ({team.SpawnConditions.SpawnDelay}) for team {team.Name} (ID: {team.Id}).";
                        string suggestion = "Set 'SpawnDelay' to a number greater than or equal to 0.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (team.MinPlayers <= 0)
                    {
                        string message = $"Invalid MinPlayers value ({team.MinPlayers}) for team {team.Name} (ID: {team.Id}).";
                        string suggestion = "Set 'MinPlayers' to a number greater than 0.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (team.TeamRoles == null || team.TeamRoles.Count == 0)
                    {
                        string message = $"Team {team.Name} (ID: {team.Id}) has no roles defined!";
                        string suggestion = "Define at least one role inside each team using the 'roles:' block.";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (team.SpawnChance < 0 || team.SpawnChance > 100)
                    {
                        string message = $"Team {team.Name} (ID: {team.Id}) has an invalid spawn chance value: {team.SpawnChance}.";
                        string suggestion = "Ensure 'spawnchance' is a number between 0 and 100 (inclusive).";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    if (string.IsNullOrEmpty(team.SoundPath))
                    {
                        string message = $"Team {team.Name} (ID: {team.Id}) has no sound path defined!";
                        string suggestion = "Specify a valid sound file path for the team. If the audio system is not intended to be used, set the path to a placeholder like \"/path/to/your/ogg/file\".";
                        ErrorManager.Add(filePath, message, suggestion: suggestion);
                        LogManager.Error($"{message}\n {suggestion}");
                        return false;
                    }

                    HashSet<int> roleIds = new();
                    foreach (var role in team.TeamRoles)
                    {
                        if (string.IsNullOrWhiteSpace(role.Name))
                        {
                            string message = $"A role in team {team.Name} has no 'role' (name) defined!";
                            string suggestion = "Each role must include a 'role' field with a valid value.";
                            ErrorManager.Add(filePath, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}");
                            return false;
                        }

                        if (role is UncomplicatedCustomRole && !roleIds.Add(role.Id))
                        {
                            string message = $"Duplicate role ID {role.Id} in team {team.Name}!";
                            string suggestion = "Each role ID must be unique within its team.";
                            ErrorManager.Add(filePath, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}");
                            return false;
                        }

                        if (role.MaxPlayers <= 0)
                        {
                            string message = $"Role '{role.Name}' in team {team.Name} (ID: {team.Id}) has invalid MaxPlayers: {role.MaxPlayers}.";
                            string suggestion = "Set 'MaxPlayers' to a value greater than 0.";
                            ErrorManager.Add(filePath, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}");
                            return false;
                        }

                        if (!Enum.IsDefined(typeof(RolePriority), role.Priority))
                        {
                            string message = $"Role '{role.Name}' in team {team.Name} (ID: {team.Id}) has an invalid Priority value: {role.Priority}.";
                            string suggestion = $"Valid values are: {string.Join(", ", Enum.GetNames(typeof(RolePriority)))}.";
                            ErrorManager.Add(filePath, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                string suggestion = ErrorManager.GetSuggestionFromMessage(message);
                ErrorManager.Add(filePath, message, suggestion: suggestion);
                LogManager.Error($"Exception: {message}\n Suggestion: {suggestion}");
                return false;
            }
        }
    }
}

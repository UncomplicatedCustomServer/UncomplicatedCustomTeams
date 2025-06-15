using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncomplicatedCustomRoles.API.Features;
using UncomplicatedCustomTeams.API.Features;
using UnityEngine;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class FileConfigs
    {
        internal string Dir = Path.Combine(Paths.Configs, "UncomplicatedCustomTeams");
        public List<string> LoadErrors { get; private set; } = new();

        public bool Is(string localDir = "")
        {
            return Directory.Exists(Path.Combine(Dir, localDir));
        }

        public string[] List(string localDir = "")
        {
            return Directory.GetFiles(Path.Combine(Dir, localDir));
        }

        public void LoadAll(string localDir = "")
        {
            LoadErrors.Clear();
            AutoUpdater.EnsureConfigIsUpToDate(localDir);
            AddCustomRoleTeams(localDir);
            LoadAction(Team.List.Add, localDir);
        }

        public void LoadAction(Action<Team> action, string localDir = "")
        {
            Team.List.Clear();
            foreach (string file in List(localDir))
            {
                try
                {
                    if (Directory.Exists(file))
                        continue;

                    if (file.Split().First() == ".")
                        return;

                    if (!ErrorManager.CustomTypeChecker(file))
                    {
                        LogManager.Error($"Skipping file {file} due to validation errors.");
                        continue;
                    }

                    Dictionary<string, List<Team>> data = Loader.Deserializer.Deserialize<Dictionary<string, List<Team>>>(File.ReadAllText(file));

                    foreach (Team team in data["teams"])
                    {
                        bool hasCustomSound = !string.IsNullOrEmpty(team.SoundPath) && team.SoundPath != "/path/to/your/ogg/file";
                        bool hasCassieMessage = !(string.IsNullOrWhiteSpace(team.CassieMessage) && string.IsNullOrWhiteSpace(team.CassieTranslation));

                        if (hasCustomSound && hasCassieMessage)
                        {
                            string warning = $"Team \"{team.Name}\" (ID: {team.Id}) has both a custom Cassie message and a sound file. Both will play simultaneously.";
                            string suggestion = "Use only one of 'CassieMessage' or 'SoundPath' for clarity. Team will be loaded. You have been warned.";
                            ErrorManager.Add(file, warning, suggestion: suggestion);
                            LogManager.Warn($"{warning}\n {suggestion}");
                        }

                        if (hasCustomSound)
                        {
                            string clipId = $"sound_{team.Id}";
                            AudioClipStorage.LoadClip(team.SoundPath, clipId);
                        }

                        if ((team.SpawnConditions.SpawnWave == "NtfWave" || team.SpawnConditions.SpawnWave == "ChaosWave")
                            && team.SpawnConditions.SpawnDelay > 0)
                        {
                            string warning = $"Setting SpawnWave '{team.SpawnConditions.SpawnWave}' with SpawnDelay won't work.";
                            string suggestion = "Remove 'SpawnDelay' if you're using NtfWave or ChaosWave.";
                            ErrorManager.Add(file, warning, suggestion: suggestion);
                            LogManager.Warn($"{warning}\n {suggestion} \nIgnoring delay for team '{team.Name}' (ID: {team.Id}).");
                            team.SpawnConditions.SpawnDelay = 0f;
                        }

                        if (team.SpawnConditions.RequiresSpawnPosition() && team.SpawnConditions.SpawnPosition == Vector3.zero)
                        {
                            string message = $"SpawnWave '{team.SpawnConditions.SpawnWave}' requires a SpawnPosition, but none was set.";
                            string suggestion = "Set a valid SpawnPosition (x,y,z) for custom spawn waves.";
                            ErrorManager.Add(file, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}\nCheck team -> {team.Name} with ID {team.Id}");
                            continue;
                        }

                        if (team.SpawnConditions.SpawnWave == "ScpDeath" &&
                            (string.IsNullOrWhiteSpace(team.SpawnConditions.TargetScp) ||
                             team.SpawnConditions.TargetScp.Equals("None", StringComparison.OrdinalIgnoreCase)))
                        {
                            string message = "You set 'ScpDeath' as spawn type but didn't specify an SCP role.";
                            string suggestion = "Set the 'TargetScp' field to an existing SCP role name.";
                            ErrorManager.Add(file, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}\nCheck team -> {team.Name} with ID {team.Id}");
                            continue;
                        }

                        if ((team.SpawnConditions.GetUsedItemType() != ItemType.None || team.SpawnConditions.GetCustomItemId() != null) &&
                            team.SpawnConditions.SpawnWave != "UsedItem")
                        {
                            string message = $"Item set but 'UsedItem' not used as spawn wave.";
                            string suggestion = "Change SpawnWave to 'UsedItem' or remove the item requirement.";
                            ErrorManager.Add(file, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n {suggestion}\nCheck team ->  {team.Name}  with ID  {team.Id}");
                            continue;
                        }

                        if (Team.List.Any(t => t.Id == team.Id))
                        {
                            uint originalId = team.Id;
                            uint newId = 1;
                            HashSet<uint> usedIds = Team.List.Select(t => t.Id).ToHashSet();

                            while (usedIds.Contains(newId))
                                newId++;

                            string warning = $"Duplicate team ID detected: {originalId}. Automatically assigned new ID: {newId}.";
                            string suggestion = $"Ensure team '{team.Name}' has a unique ID next time to avoid auto-correction.";
                            ErrorManager.Add(file, warning, suggestion: suggestion);
                            LogManager.Warn($"{warning}\n{suggestion}");

                            team.Id = newId;
                        }

                        if ((team.SpawnConditions.GetUsedItemType() == ItemType.None && team.SpawnConditions.GetCustomItemId() == null) &&
                            team.SpawnConditions.SpawnWave == "UsedItem")
                        {
                            string message = "UsedItem value is invalid or missing.";
                            string suggestion = "Provide a valid ItemType or Custom Item ID.";
                            ErrorManager.Add(file, message, suggestion: suggestion);
                            LogManager.Error($"{message}\n{suggestion}\nCheck team -> {team.Name} with ID {team.Id}");
                            continue;
                        }

                        HashSet<int> usedRoleIds = Team.List.SelectMany(t => t.Roles).Select(r => r.Id).ToHashSet();

                        foreach (var role in team.Roles)
                        {
                            if (usedRoleIds.Contains(role.Id))
                            {
                                int originalRoleId = role.Id;
                                int newRoleId = 1;
                                while (usedRoleIds.Contains(newRoleId))
                                    newRoleId++;

                                string warning = $"Duplicate Custom Role ID detected: {originalRoleId}. Automatically assigned new ID: {newRoleId}.";
                                string suggestion = "Use unique role IDs to avoid this in the future.";
                                ErrorManager.Add(file, warning, suggestion: suggestion);
                                LogManager.Warn($"{warning}\n{suggestion}");

                                role.Id = newRoleId;
                                usedRoleIds.Add(newRoleId);
                            }
                            else
                            {
                                usedRoleIds.Add(role.Id);
                            }
                        }

                        if (Plugin.Instance.Config.UseExiledCustomRoles)
                        {
                            Timing.CallDelayed(Plugin.Instance.Config.ExiledCustomRoleCheckDelay, () =>
                            {
                                foreach (var role in team.EcrRoles.ToList())
                                {
                                    if (!Exiled.CustomRoles.API.Features.CustomRole.TryGet((uint)role.Id, out var cRole))
                                    {
                                        string warning = $"Exiled Custom Role of ID {role.Id} not found!";
                                        string suggestion = "Ensure that the role is properly registered by its plugin in the future.";
                                        ErrorManager.Add(file, warning, suggestion: suggestion);
                                        LogManager.Warn($"{warning}\n{suggestion}");
                                        team.EcrRoles.Remove(role);
                                    }
                                }
                            });
                        }

                        LogManager.Debug($"Proposed to the registerer the external team '{team.Name}' (ID: {team.Id}) from file: {file}");
                        action(team);
                    }
                }
                catch (Exception ex)
                {
                    var line = (ex is YamlDotNet.Core.YamlException yamlEx) ? yamlEx.Start.Line : (int?)null;
                    var column = (ex is YamlDotNet.Core.YamlException yamlEx2) ? yamlEx2.Start.Column : (int?)null;

                    ErrorManager.Add(
                        file: file,
                        message: ex.Message,
                        line: line,
                        column: column,
                        suggestion: ErrorManager.GetSuggestionFromMessage(ex.Message)
                    );
                    LogManager.Error($"Failed to parse {file}. YAML Exception: {ex.Message}");
                }
            }
        }

        public void Welcome(string localDir = "")
        {
            if (!Is(localDir))
            {
                Directory.CreateDirectory(Path.Combine(Dir, localDir));

                File.WriteAllText(Path.Combine(Dir, localDir, "example-role.yml"), Loader.Serializer.Serialize(new Dictionary<string, List<Team>>() {
                  {
                    "teams", new List<Team>()
                    {
                        new()
                        {
                            Id = 1
                        }
                    }
                  }
                }));

                LogManager.Info($"Plugin does not have a role folder, generated one in {Path.Combine(Dir, localDir)}");
            }
        }

        public void AddCustomRoleTeams(string localDir = "")
        {
            string dir = Path.Combine(Paths.Configs, "UncomplicatedCustomTeams", localDir);

            if (!Directory.Exists(dir))
            {
                return;
            }

            foreach (string filePath in Directory.GetFiles(dir, "*.yml"))
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);

                    var configData = Loader.Deserializer.Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(fileContent);

                    if (!configData.ContainsKey("teams") || configData["teams"] == null)
                    {
                        LogManager.Error($"File {filePath} does not contain a 'teams' section. Skipping.");
                        continue;
                    }

                    foreach (var yamlTeam in configData["teams"])
                    {
                        if (!yamlTeam.ContainsKey("name") || yamlTeam["name"] == null)
                            continue;

                        string teamName = yamlTeam["name"].ToString();

                        if (!yamlTeam.ContainsKey("team_alive_to_win") || yamlTeam["team_alive_to_win"] == null)
                        {
                            yamlTeam["team_alive_to_win"] = new List<string>();
                        }

                        var teamAliveToWin = new List<object>();

                        if (!yamlTeam.ContainsKey("roles") || yamlTeam["roles"] == null)
                        {
                            continue;
                        }

                        if (yamlTeam["roles"] is not List<object> rolesList)
                        {
                            continue;
                        }

                        if (!yamlTeam.TryGetValue("ecr_roles", out var ecrRolesObj) || ecrRolesObj == null)
                        {
                            LogManager.Debug($"{teamName} has no ecr_roles entry, generating default...");
                            yamlTeam["ecr_roles"] = new List<Dictionary<string, object>> { new() { { "id", 999 }, { "max_players", 1 }, { "priority", "Fifth" } } };
                        }
                        else if (ecrRolesObj is List<object> ecrRolesList)
                        {
                            if (ecrRolesList.Count == 0)
                            {
                                LogManager.Debug($"{teamName} has empty ecr_roles list, generating default...");
                                ecrRolesList.Add(new Dictionary<string, object> { { "id", 999 }, { "max_players", 1 }, { "priority", "Fifth" } });
                            }
                        }
                        else
                        {
                            LogManager.Debug($"{teamName} has invalid ecr_roles format, skipping...");
                        }

                        var roles = new List<Dictionary<string, object>>();
                        foreach (var item in rolesList)
                        {
                            if (item is Dictionary<object, object> tempDict)
                            {
                                var fixedDict = tempDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
                                roles.Add(fixedDict);
                            }
                            else if (item is Dictionary<string, object> correctDict)
                            {
                                roles.Add(correctDict);
                            }
                            else
                            {
                                LogManager.Debug($"Error: Element in 'roles' is not a valid dictionary! Type: {item?.GetType()} | Value: {item}");
                            }
                        }

                        foreach (var roleData in roles)
                        {
                            if (!roleData.ContainsKey("team") || roleData["team"] == null)
                                continue;

                            string roleTeamName = roleData["team"].ToString();

                            if (!teamAliveToWin.Contains(roleTeamName))
                            {
                                teamAliveToWin.Add(roleTeamName);
                            }
                        }

                        yamlTeam["team_alive_to_win"] = teamAliveToWin;

                        string newYamlContent = Loader.Serializer.Serialize(configData);
                        if (File.ReadAllText(filePath) != newYamlContent)
                        {
                            File.WriteAllText(filePath, newYamlContent);
                            LogManager.Debug($"Updated file {filePath}!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Instance.Config.Debug)
                    {
                        LogManager.Error($"Error processing file {filePath}: {ex.Message}\n{ex.StackTrace}");
                    }
                    else
                    {
                        LogManager.Error($"Error processing file {filePath}: {ex.Message}");
                    }
                }
            }
        }
    }
}

using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            LoadAction(Team.List.Add, localDir);
            AddCustomRoleTeams();
        }

        public void LoadAction(Action<Team> action, string localDir = "")
        {
            foreach (string FileName in List(localDir))
            {
                try
                {
                    if (Directory.Exists(FileName))
                        continue;

                    if (FileName.Split().First() == ".")
                        return;

                    if (!ErrorManager.CustomTypeChecker(FileName))
                    {
                        LogManager.Error($"Skipping file {FileName} due to validation errors.");
                        continue;
                    }

                    Dictionary<string, List<Team>> Roles = Loader.Deserializer.Deserialize<Dictionary<string, List<Team>>>(File.ReadAllText(FileName));

                    if (!Roles.ContainsKey("teams"))
                    {
                        LogManager.Error($"Error during the deserialization of file {FileName}: Node name 'teams' not found!");
                        return;
                    }

                    foreach (Team team in Roles["teams"])
                    {
                        bool hasCustomSound = !string.IsNullOrEmpty(team.SoundPath) && team.SoundPath != "/path/to/your/ogg/file";
                        bool hasCassieMessage = !string.IsNullOrEmpty(team.CassieMessage) || !string.IsNullOrEmpty(team.CassieTranslation);

                        if (hasCustomSound && hasCassieMessage)
                        {
                            LogManager.Warn($"Team \"{team.Name}\"(ID: {team.Id}) has both a custom Cassie message and a sound file set. This is not recommended, as both will play simultaneously.");
                        }

                        if (hasCustomSound)
                        {
                            string clipId = $"sound_{team.Id}";
                            AudioClipStorage.LoadClip(team.SoundPath, clipId);
                        }

                        if ((team.SpawnConditions.SpawnWave == "NtfWave" || team.SpawnConditions.SpawnWave == "ChaosWave")
                            && team.SpawnConditions.SpawnDelay > 0)
                        {
                            LogManager.Warn($"Setting NtfWave or ChaosWave together with an SpawnDelay will not work. Ignoring SpawnDelay... (Team: {team.Name}, ID: {team.Id})");
                            team.SpawnConditions.SpawnDelay = 0f;
                        }

                        if (team.SpawnConditions.RequiresSpawnType() && team.SpawnConditions.SpawnPosition == Vector3.zero)
                        {
                            LogManager.Error($"SpawnWave '{team.SpawnConditions.SpawnWave}' requires a custom SpawnPosition, but none was set. The team '{team.Name}' (ID: {team.Id}) will not be loaded...");
                            continue;
                        }

                        if (team.SpawnConditions.SpawnWave == "UsedItem" &&
                            team.SpawnConditions.GetUsedItemType() == ItemType.None &&
                            team.SpawnConditions.GetCustomItemId() == null)
                        {
                            LogManager.Error($"You set 'UsedItem' spawn type but didn't specify an item. The team will not be loaded... (Team: {team.Name}, ID: {team.Id})");
                            continue;
                        }

                        if (team.SpawnConditions.SpawnWave == "ScpDeath" &&
                            (string.IsNullOrWhiteSpace(team.SpawnConditions.TargetScp) ||
                             team.SpawnConditions.TargetScp.Equals("None", StringComparison.OrdinalIgnoreCase)))
                        {
                            LogManager.Error($"You set 'ScpDeath' spawn type but didn't specify an SCP Role. The team will not be loaded... (Team: {team.Name}, ID: {team.Id})");
                            continue;
                        }

                        if ((team.SpawnConditions.GetUsedItemType() != ItemType.None || team.SpawnConditions.GetCustomItemId() != null) &&
                            team.SpawnConditions.SpawnWave != "UsedItem")
                        {
                            LogManager.Error($"You set an item ({team.SpawnConditions.GetUsedItemType()}{(team.SpawnConditions.GetCustomItemId() != null ? $" or Custom Item ID: {team.SpawnConditions.GetCustomItemId()}" : "")}) but didn't set 'UsedItem' as the spawn type. The team will not be loaded... (Team: {team.Name}, ID: {team.Id})");
                            continue;
                        }

                        if (Team.List.Any(t => t.Id == team.Id))
                        {
                            string message = $"Duplicate team ID detected! ID: {team.Id} already exists.";
                            string suggestion = $"Change the ID of the team \"{team.Name}\" in file {Path.GetFileName(FileName)} to a unique value";
                            LoadErrors.Add($"{Path.GetFileName(FileName)}: {message}");

                            ErrorManager.Add(
                                file: FileName,
                                message: message,
                                suggestion: suggestion
                            );

                            LogManager.Error($"{message} Skipping team \"{team.Name}\"");
                            continue;
                        }

                        if (Team.List.Any(t => t.Name.Equals(team.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            string message = $"Duplicate team name detected! Name: \"{team.Name}\" already exists.";
                            string suggestion = $"Change the name of the team in file {Path.GetFileName(FileName)} to something unique.";
                            LoadErrors.Add($"{Path.GetFileName(FileName)}: {message}");

                            ErrorManager.Add(
                                file: FileName,
                                message: message,
                                suggestion: suggestion
                            );

                            LogManager.Error($"{message} Skipping team with ID {team.Id}");
                            continue;
                        }

                        HashSet<int> roleIds = new();
                        foreach (var role in team.Roles)
                        {
                            if (Team.List.SelectMany(t => t.Roles).Any(r => r.Id == role.Id))
                            {
                                string message = $"Duplicate Custom Role ID {role.Id} detected in team \"{team.Name}\"!";
                                string suggestion = "Each Custom Role ID must be unique across all teams.";
                                LoadErrors.Add($"{Path.GetFileName(FileName)}: {message}");

                                ErrorManager.Add(FileName, message, suggestion: suggestion);
                                LogManager.Error($"{message}\n{suggestion}");
                                continue;
                            }
                        }

                        LogManager.Debug($"Proposed to the registerer the external team {team.Name} (ID: {team.Id}) from file:\n{FileName}");
                        action(team);
                    }
                }
                catch (Exception ex)
                {
                    var line = (ex is YamlDotNet.Core.YamlException yamlEx) ? yamlEx.Start.Line : (int?)null;
                    var column = (ex is YamlDotNet.Core.YamlException yamlEx2) ? yamlEx2.Start.Column : (int?)null;

                    ErrorManager.Add(
                        file: FileName,
                        message: ex.Message,
                        line: line,
                        column: column,
                        suggestion: ErrorManager.GetSuggestionFromMessage(ex.Message)
                    );
                    LogManager.Error($"Failed to parse {FileName}. YAML Exception: {ex.Message}");
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

                        var teamAliveToWin = yamlTeam["team_alive_to_win"] as List<object> ?? new List<object>();

                        if (!yamlTeam.ContainsKey("roles") || yamlTeam["roles"] == null)
                        {
                            continue;
                        }

                        if (!(yamlTeam["roles"] is List<object> rolesList))
                        {
                            continue;
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
                        File.WriteAllText(filePath, newYamlContent);
                        LogManager.Debug($"Updated file {filePath}!");
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

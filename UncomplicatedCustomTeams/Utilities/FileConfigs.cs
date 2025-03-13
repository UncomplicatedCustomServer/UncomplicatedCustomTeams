using Exiled.API.Features;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class FileConfigs
    {
        internal string Dir = Path.Combine(Paths.Configs, "UncomplicatedCustomTeams");

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

                    Dictionary<string, List<Team>> Roles = Loader.Deserializer.Deserialize<Dictionary<string, List<Team>>>(File.ReadAllText(FileName));

                    if (!Roles.ContainsKey("teams"))
                    {
                        LogManager.Error($"Error during the deserialization of file {FileName}: Node name 'teams' not found!");
                        return;
                    }

                    foreach (Team Team in Roles["teams"])
                    {
                        LogManager.Debug($"Proposed to the registerer the external team {Team.Id} [{Team.Name}] from file:\n{FileName}");
                        action(Team);
                    }
                }
                catch (Exception ex)
                {
                    if (!Plugin.Instance.Config.Debug)
                    {
                        LogManager.Error($"Failed to parse {FileName}. YAML Exception: {ex.Message}.");
                    }
                    else
                    {
                        LogManager.Error($"Failed to parse {FileName}. YAML Exception: {ex.Message}.\nStack trace: {ex.StackTrace}");
                    }
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
        public static void AddCustomRoleTeams(string localDir = "")
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
                    LogManager.Error($"[SEND TO DEV] Error processing file {filePath}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}

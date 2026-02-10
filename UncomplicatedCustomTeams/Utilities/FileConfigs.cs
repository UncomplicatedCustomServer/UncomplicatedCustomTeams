using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.API.Features.Services;
using UncomplicatedCustomTeams.Utilities.Errors;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class FileConfigs
    {
        private readonly string _baseDir = Path.Combine(PathManager.Configs.FullName, "UncomplicatedCustomTeams");
        internal string Dir;

        public FileConfigs()
        {
            Dir = Path.Combine(_baseDir, Server.Port.ToString());
        }

        public bool Is(string localDir = "") => Directory.Exists(Path.Combine(Dir, localDir));

        public string[] List(string localDir = "")
        {
            string path = Path.Combine(Dir, localDir);
            return Directory.Exists(path) ? Directory.GetFiles(path, "*.yml") : [];
        }

        public void LoadAll(string localDir = "")
        {
            Team.List.Clear();

            Welcome(localDir);
            LoadConfigs(localDir);
        }

        public void Reload(string localDir = "")
        {
            ErrorManager.Clear();
            Team.List.Clear();
            SummonedTeam.List.Clear();

            Plugin.Singleton.FileConfigs.LoadAll(localDir);

            UCTEvents.InvokeDefinitionsLoaded();

            LogManager.Info($"Process finished. Loaded {Team.List.Count} teams.");

            if (ErrorManager.Errors.Count > 0)
            {
                LogManager.Warn($"Warning: {ErrorManager.Errors.Count} errors detected in YAML files. Use 'uct errors' to view them.");
            }

            foreach (var team in Team.List)
            {
                LogManager.Debug($"Loaded team: {team.Name} (ID: {team.Id})");
            }
        }

        private void LoadConfigs(string localDir)
        {
            if (!Is(localDir))
            {
                LogManager.Warn($"Directory not found: {Path.Combine(Dir, localDir)}");
                return;
            }

            foreach (string file in List(localDir))
            {
                if (!TeamConfigValidator.ValidateFile(file))
                {
                    LogManager.Warn($"Skipping file {Path.GetFileName(file)} due to validation errors.");
                    continue;
                }

                try
                {
                    var data = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, List<Team>>>(File.ReadAllText(file));
                    if (data == null || !data.TryGetValue("teams", out var teams)) continue;

                    foreach (Team team in teams)
                    {
                        if (!TeamConfigValidator.ValidateAndSanitizeTeam(team, file))
                            continue;

                        LogManager.Debug($"Loaded definition for '{team.Name}' (ID: {team.Id}).");
                        Team.Register(team);

                        RoleManager.RegisterTeamRoles(team);
                        AudioService.PreloadTeamAudio(team);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to load {Path.GetFileName(file)}: {ex.Message}");
                    ErrorManager.Add(file, ex.Message, suggestion: "Check YAML syntax and structure.");
                }
            }
        }

        public void Welcome(string localDir = "")
        {
            if (!Is(localDir))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(Dir, localDir));

                    var exampleTeam = new Team
                    {
                        Id = 1,
                        Name = "Example Team"
                    };

                    exampleTeam.Roles.Add(new UncomplicatedCustomRole
                    {
                        Id = 1,
                        Team = PlayerRoles.Team.ClassD,
                        SpawnSettings = null,
                        CanEscape = false,
                        RoleAfterEscape = null,
                        MaxPlayers = 1,
                        Priority = API.Enums.RolePriority.First,
                        DropInventoryOnDeath = true,
                        IsGodmodeEnabled = false,
                        IsBypassEnabled = false,
                        IsNoclipEnabled = false,
                        CustomFlags = null
                    });

                    exampleTeam.Roles.Add(new UncomplicatedCustomRole
                    {
                        Id = 2,
                        Team = PlayerRoles.Team.ClassD,
                        SpawnSettings = null,
                        CanEscape = false,
                        RoleAfterEscape = null,
                        CustomFlags = null,
                        Priority = API.Enums.RolePriority.Second,
                        DropInventoryOnDeath = true,
                        IsGodmodeEnabled = false,
                        IsBypassEnabled = false,
                        IsNoclipEnabled = false,
                        MaxPlayers = 1
                    });

                    var exampleData = new Dictionary<string, List<Team>>
                    {
                        { "teams", [exampleTeam] }
                    };

                    File.WriteAllText(Path.Combine(Dir, "example-team.yml"), YamlConfigParser.Serializer.Serialize(exampleData));

                    LogManager.Info($"Plugin does not have a team folder, generated one in {Path.Combine(Dir)}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to generate example config: {ex.Message}");
                }
            }
        }
    }
}

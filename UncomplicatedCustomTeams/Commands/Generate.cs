using CommandSystem;
using LabApi.Loader.Features.Yaml;
using PlayerRoles;
using System.Collections.Generic;
using System.IO;
using UncomplicatedCustomTeams.API.Enums;
using UncomplicatedCustomTeams.Interfaces;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams.Commands
{
    internal class Generate : IUCTCommand
    {
        public string Name { get; } = "generate";
        public string Description { get; } = "Generates a default YAML file for a new team.";
        public string RequiredPermission { get; } = "uct.generate";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Unexpected number of arguments!\nUsage: uct generate <FileName>";
                return false;
            }

            string fileName = arguments[0].Replace(".yml", "") + ".yml";

            string directory = Plugin.Singleton.FileConfigs.Dir;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string filePath = Path.Combine(directory, fileName);

            if (File.Exists(filePath))
            {
                response = $"File {fileName} already exists!";
                return false;
            }

            var defaultTeamConfig = new Dictionary<string, List<Team>>
            {
                {
                    "teams", new List<Team>
                    {
                       new() {
                        Id = 1,
                        Name = "NewTeam",
                        Roles =
                        [
                             new()
                             {
                                Id = 1,
                                Role = RoleTypeId.ClassD,
                                SpawnSettings = null,
                                CanEscape = false,
                                RoleAfterEscape = null,
                                MaxPlayers = 1,
                                Priority = RolePriority.First,
                                DropInventoryOnDeath = true,
                                IsGodmodeEnabled = false,
                                IsBypassEnabled = false,
                                IsNoclipEnabled = false,
                                CustomFlags = null
                             },
                             new()
                             {
                                Id = 2,
                                Role = RoleTypeId.ClassD,
                                SpawnSettings = null,
                                CanEscape = false,
                                RoleAfterEscape = null,
                                CustomFlags = null,
                                Priority = RolePriority.Second,
                                DropInventoryOnDeath = true,
                                IsGodmodeEnabled = false,
                                IsBypassEnabled = false,
                                IsNoclipEnabled = false,
                                MaxPlayers = 1
                             }
                        ]
                       }
                    }
                }
            };

            File.WriteAllText(filePath, YamlConfigParser.Serializer.Serialize(defaultTeamConfig));
            response = $"New YAML file generated at {filePath}, but it has not been loaded yet! Use 'uct reload' to load it.";
            return true;
        }
    }
}
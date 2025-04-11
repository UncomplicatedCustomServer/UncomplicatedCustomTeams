using CommandSystem;
using Exiled.API.Features;
using Exiled.Loader;
using System.Collections.Generic;
using System.IO;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;

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
            string directory = Path.Combine(Plugin.Instance.FileConfigs.Dir, Server.Port.ToString());

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
                       }
                    }
                }
            };
            File.WriteAllText(filePath, Loader.Serializer.Serialize(defaultTeamConfig));

            response = $"New YAML file generated at {filePath}, but it has not been loaded yet!";
            return true;
        }
    }
}

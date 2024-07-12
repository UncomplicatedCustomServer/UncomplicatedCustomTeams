using Exiled.API.Features;
using Exiled.Loader;
using Respawning;
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
    }
}

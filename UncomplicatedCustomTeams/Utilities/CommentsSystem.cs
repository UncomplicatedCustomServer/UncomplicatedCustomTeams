using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class CommentsSystem
    {
        /// <summary>
        /// Adds comments to YAML files.
        /// </summary>
        public static void AddCommentsToYaml(string localDir = "")
        {
            try
            {
                string dir = Path.Combine(Paths.Configs, "UncomplicatedCustomTeams", localDir);

                if (!Directory.Exists(dir))
                {
                    LogManager.Debug($"Directory '{dir}' does not exist. Skipping comment addition.");
                    return;
                }

                foreach (string filePath in Directory.GetFiles(dir, "*.yml"))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(filePath);
                        List<string> newLines = new();
                        var commentMap = new Dictionary<string, string>
                        {
                            { "team_alive_to_win:", "# Here, you can define which teams will win against your custom team." },
                            { "used_item:", "# Specify the item or custom item ID that triggers this team spawn. Only works if SpawnWave is set to 'UsedItem'." },
                            { "target_scp:", "# Specify the SCP role (e.g., Scp106) or use the SCPs team (SCPs) whose death triggers this team spawn. Only SCPs is allowed when using a team. This setting only applies when SpawnWave is set to 'ScpDeath'." },
                            { "spawn_delay:", "# Setting a SpawnDelay greater than 0 will not work when using NtfWave or ChaosWave!" },
                            { "max_spawns:", "# The maximum number of times this team can be spawned in a single round. Set to -1 for unlimited." },
                            { "spawn_wave:", "# Available Spawn Waves: None, NtfWave, ChaosWave, AfterWarhead, AfterDecontamination, UsedItem, RoundStarted, ScpDeath." },
                            { "sound_path:", "# This is not a required option. If you are using Pterodactyl/another panel, you must replace '~' with '/home/container'." },
                            { "roles_affected_on_round_start:", "# Defines which starting roles can be converted into this team. At the start of the round, the plugin will randomly select players from these roles to respawn as this team. This option only works if 'SpawnWave' is set to 'RoundStarted'." },
                        };
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i];
                            string trimmedLine = line.Trim();

                            foreach (var pair in commentMap)
                            {
                                if (trimmedLine.StartsWith(pair.Key))
                                {
                                    if (i == 0 || !newLines.Last().Trim().Equals(pair.Value, StringComparison.OrdinalIgnoreCase))
                                    {
                                        newLines.Add(pair.Value);
                                    }
                                    break;
                                }
                            }

                            newLines.Add(line);
                        }
                        if (!lines.SequenceEqual(newLines))
                        {
                            File.WriteAllLines(filePath, newLines);
                            LogManager.Debug($"Added comments to {filePath}!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Error while processing file '{filePath}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Critical error in Comments System: {ex.Message}");
            }
        }
    }
}

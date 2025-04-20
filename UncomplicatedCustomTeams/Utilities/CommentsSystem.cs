using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                            { "target_scp:", "# Specify the SCP role whose death triggers this team spawn. Only works if SpawnWave is set to 'ScpDeath'." },
                            { "spawn_delay:", "# Setting a SpawnDelay greater than 0 will not work when using NtfWave or ChaosWave!" }
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

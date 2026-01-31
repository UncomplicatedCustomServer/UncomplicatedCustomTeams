using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class CommentsSystem
    {
        private static readonly Dictionary<string, string> CommentMap = new()
        {
            { "used_item:", "Specify the Game Base Item or EXILED Custom Item ID that triggers this team spawn. Only works if SpawnWave is set to 'UsedItem'." },
            { "target_scp:", "Specify the SCP role (e.g., Scp106) or use the SCPs team (SCPs) whose death triggers this team spawn. Only SCPs is allowed when using a team. This setting only applies when SpawnWave is set to 'ScpDeath'." },
            { "spawn_delay:", "Setting a SpawnDelay greater than 0 will not work when using NtfWave or ChaosWave!" },
            { "max_spawns:", "The maximum number of times this team can be spawned in a single round. Set to -1 for unlimited." },
            { "spawn_wave:", "Available Spawn Waves: None, NtfWave, ChaosWave, AfterWarhead, AfterDecontamination, UsedItem, RoundStarted, ScpDeath, TeamDependent." },
            { "sound_paths:", "This is not a required option. If you are using Pterodactyl/another panel, you must replace '~' with '/home/container'." },
            { "roles_affected_on_round_start:", "Defines which starting roles can be converted into this team. At the start of the round, the plugin will randomly select players from these roles to respawn as this team. This option only works if 'SpawnWave' is set to 'RoundStarted'." },
            { "after_team_spawn:", "Spawn this team AFTER the custom team with the specified ID has spawned. SpawnWave must be set to 'TeamDependent'." },
            { "after_team_death:", "Spawn this team AFTER the custom team with the specified ID has been completely eliminated. SpawnWave must be set to 'TeamDependent'." },
            { "required_alive_roles:", "List of roles where at least one of which must be alive for this team to spawn. Ignored if empty." },
            { "allow_concurrent_spawns:", "Set to true to allow this team to spawn alongside other teams during the same Spawn Wave. If false (default), it will be the only one." },
            { "prevent_round_end_if_alive:", "If true, the round will not end while this team is alive, effectively blocking standard round-end conditions. NOTE: This does NOT prevent the round from ending if this team wins by eliminating all enemies. If they win, the round ends immediately." },
            { "allied_teams:", "A list of vanilla teams that are considered allies. The Custom Team does NOT need to eliminate players from these teams to trigger the win condition." },
            { "winning_team:", "The specific faction to declare as the winner when the win condition is met." },
        };

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
                    ProcessFile(filePath);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Critical error in Comments System: {ex}");
            }
        }

        private static void ProcessFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                List<string> newLines = new(lines.Length + 20);
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmedLine = line.TrimStart();

                    foreach (var pair in CommentMap)
                    {
                        if (trimmedLine.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            string indentation = line.Substring(0, line.Length - trimmedLine.Length);
                            string commentLine = $"{indentation}# {pair.Value}";
                            bool previousLineIsComment = newLines.Count > 0 && newLines.Last().TrimStart().StartsWith("#");

                            if (!previousLineIsComment)
                            {
                                newLines.Add(commentLine);
                                changed = true;
                            }
                            break;
                        }
                    }
                    newLines.Add(line);
                }

                if (changed)
                {
                    File.WriteAllLines(filePath, newLines);
                    LogManager.Debug($"Updated comments in {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error while processing file '{filePath}': {ex.Message}");
            }
        }
    }
}
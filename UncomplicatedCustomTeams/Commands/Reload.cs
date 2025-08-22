using CommandSystem;
using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Reload : IUCTCommand
    {
        public string Name => "reload";
        public string Description => "Reloads every custom team loaded and searches for new ones to load.";
        public string RequiredPermission => "uct.reload";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "Round is not started yet!";
                return false;
            }

            if (SummonedTeam.List.Any(team => team.HasAlivePlayers()))
            {
                response = "An active custom team has been detected. Reloading has been cancelled.";
                return false;
            }
            new FileConfigs().Welcome(Server.Port.ToString());

            try
            {
                LogManager.Info("Starting team reload...");
                ErrorManager.Clear();
                Team.List.Clear();
                LogManager.Info("Cleared existing teams list.");

                FileConfigs fileConfigs = new();
                fileConfigs.LoadAll();
                fileConfigs.LoadAll(Server.Port.ToString());
                CommentsSystem.AddCommentsToYaml();
                CommentsSystem.AddCommentsToYaml(Server.Port.ToString());
                LogManager.Info($"Reloaded teams from the config. Current count: {Team.List.Count}");

                if (Team.List.Count == 0)
                {
                    response = "WARNING: No teams were loaded! Check your team config files!";
                    LogManager.Warn("WARNING: No teams were loaded! Check your team config files!");
                    return false;
                }

                if (ErrorManager.Errors.Any())
                {
                    StringBuilder sb = new();
                    sb.AppendLine("There were errors during the team config check:");
                    foreach (var e in ErrorManager.Errors)
                    {
                        sb.AppendLine($"{e.File}: {e.Message} ({e.Suggestion})");
                    }

                    response = sb.ToString();
                    LogManager.Warn(response);
                    return false;
                }

                if (fileConfigs.LoadErrors.Any())
                {
                    StringBuilder sb = new();
                    sb.AppendLine("There were errors during the team config check:");
                    foreach (var err in fileConfigs.LoadErrors)
                    {
                        sb.AppendLine(err);
                    }

                    response = sb.ToString();
                    LogManager.Warn(response);
                    return false;
                }

                LogManager.Info($"Successfully loaded {Team.List.Count} teams.");
                response = $"All custom teams have been reloaded successfully. Loaded {Team.List.Count} teams.";
                LogManager.Info(response);
                return true;
            }
            catch (System.Exception ex)
            {
                response = $"An error occurred while reloading teams: {ex.Message}. This was likely caused by a configuration mistake.";
                LogManager.Error(response);
                return false;
            }
        }
    }
}

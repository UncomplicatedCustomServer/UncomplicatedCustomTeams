using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class ReloadCommand : IUCTCommand
    {
        public string Name => "reload";
        public string Description => "Reloads every custom team loaded and searches for new ones.";
        public string RequiredPermission => "uct.reload";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!Round.IsStarted)
            {
                response = "Round is not started yet!";
                sender.Respond(response, false);
                return false;
            }

            if (SummonedTeam.List.Any(team => team.HasAlivePlayers()))
            {
                response = "An active custom team has been detected. Reloading has been cancelled.";
                sender.Respond(response, false);
                return false;
            }

            try
            {
                LogManager.Info("Starting team reload...");
                Team.List.Clear();
                LogManager.Debug("Cleared existing teams list.");

                FileConfigs fileConfigs = new();
                fileConfigs.LoadAll();
                fileConfigs.LoadAll(Server.Port.ToString());
                LogManager.Info($"Reloaded teams from the config. Current count: {Team.List.Count}");

                if (Team.List.Count == 0)
                {
                    response = "! WARNING !: No teams were loaded! Check your config files!";
                    LogManager.Warn(response);
                    sender.Respond(response, false);
                    return false;
                }

                LogManager.Info($"✔️  Successfully loaded {Team.List.Count} teams.");
                response = $"✔️  All custom teams have been reloaded successfully. Loaded {Team.List.Count} teams.";
                sender.Respond(response, true);
                return true;
            }
            catch (System.Exception ex)
            {
                response = $"An error occurred while reloading teams: {ex.Message}. This was likely caused by a configuration mistake.";
                LogManager.Error(response);
                LogManager.Error($"Stack Trace:\n{ex.StackTrace}");

                sender.Respond(response, false);
                return false;
            }
        }
    }
}

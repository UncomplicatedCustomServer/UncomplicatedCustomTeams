using CommandSystem;
using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.API.Events;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.Interfaces;
using UncomplicatedCustomTeams.Utilities;
using UncomplicatedCustomTeams.Utilities.Errors;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Reload : IUCTCommand
    {
        public string Name => "reload";
        public string Description => "Reloads every custom team loaded and searches for new ones.";
        public string RequiredPermission => "uct.reload";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (Round.IsRoundStarted && SummonedTeam.List.Any(team => team.Members.Any(m => m.Player.IsAlive)))
            {
                response = "Cannot reload configuration while a Custom Team is active/alive on the server.";
                return false;
            }

            try
            {
                LogManager.Info("Reloading configurations initiated by administrator...");

                ErrorManager.Clear();
                Team.List.Clear();
                SummonedTeam.List.Clear();

                Plugin.Singleton.FileConfigs.LoadAll();

                UCTEvents.InvokeDefinitionsLoaded();

                int teamCount = Team.List.Count;
                int errorCount = ErrorManager.Errors.Count;

                if (teamCount == 0)
                {
                    response = errorCount > 0
                        ? $"No teams loaded and {errorCount} errors detected! Check 'uct errors'."
                        : "Reload successful, but 0 teams were found/loaded.";

                    return false;
                }

                if (errorCount > 0)
                {
                    response = $"Reloaded {teamCount} teams, but {errorCount} errors were detected.\nUse uct errors to view details.";
                    return true;
                }

                response = $"Configuration reloaded successfully!\nLoaded <b>{teamCount}</b> custom teams.";
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Critical error during reload: {ex}");
                response = $"Critical error occurred while reloading: {ex.Message}";
                return false;
            }
        }
    }
}
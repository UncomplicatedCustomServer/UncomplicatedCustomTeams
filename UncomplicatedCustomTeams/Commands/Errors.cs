using CommandSystem;
using Exiled.Permissions.Extensions;
using System.Collections.Generic;
using System.Text;
using UncomplicatedCustomTeams.Interfaces;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class Errors : IUCTCommand
    {
        public string Name => "errors";
        public string Description => "Displays YAML errors detected during configuration loading.";
        public string RequiredPermission => "uct.errors";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(RequiredPermission))
            {
                response = "You do not have permission to use this command.";
                return false;
            }

            if (ErrorManager.Errors.Count == 0)
            {
                response = "No YAML errors were detected!";
                return true;
            }

            StringBuilder sb = new();
            foreach (var err in ErrorManager.Errors)
            {
                sb.AppendLine($"<color=#FFFFFF>📄</color> <b>File:</b> {System.IO.Path.GetFileName(err.File)}");

                if (err.Line.HasValue)
                    sb.AppendLine($"<color=#00FFFF>🔢</color> Line: {err.Line.Value}, Column: {err.Column}");

                sb.AppendLine($"<color=red>❌</color> Error: {err.Message}");
                sb.AppendLine($"<color=#FFFF00>💡</color> Suggestion: {err.Suggestion}");
                sb.AppendLine();
            }

            response = sb.ToString();
            return true;
        }
    }
}

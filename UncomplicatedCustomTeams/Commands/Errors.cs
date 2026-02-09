using CommandSystem;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UncomplicatedCustomTeams.Interfaces;
using UncomplicatedCustomTeams.Utilities.Errors;

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
            if (ErrorManager.Errors.Count == 0)
            {
                response = "<color=#4caf50>✅ No YAML configuration errors were detected.</color>";
                return true;
            }

            StringBuilder sb = new();
            sb.AppendLine($"<color=#ff5555>Found {ErrorManager.Errors.Count} error(s) in configuration files:</color>");
            sb.AppendLine();

            foreach (var err in ErrorManager.Errors)
            {
                string fileName = Path.GetFileName(err.File);

                sb.AppendLine($"<color=#FFFFFF>📄 <b>File:</b> {fileName}</color>");

                if (err.Line.HasValue)
                {
                    sb.AppendLine($"<color=#00FFFF>🔢 <b>Position:</b> Line {err.Line}, Column {err.Column}</color>");
                }

                sb.AppendLine($"<color=#ff5555>❌ <b>Error:</b> {err.Message}</color>");

                if (!string.IsNullOrWhiteSpace(err.Suggestion))
                {
                    sb.AppendLine($"<color=#FFFF00>💡 <b>Tip:</b> {err.Suggestion}</color>");
                }

                sb.AppendLine(new string('-', 30));
            }

            response = sb.ToString();
            return true;
        }
    }
}
using CommandSystem;
using System.Collections.Generic;
using System.Text;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Interfaces;


namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class List : IUCTCommand
    {
        public string Name { get; } = "list";

        public string Description { get; } = "Displays all registered custom teams.";

        public string RequiredPermission { get; } = "uct.list";

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (Team.List.Count == 0)
            {
                response = "There are no registered custom teams.";
                return false;
            }

            StringBuilder sb = new();
            sb.AppendLine("List of all registered Custom Teams:");
            foreach (var team in Team.List)
            {
                sb.AppendLine($"[{team.Id}] <b>{team.Name}</b>");
                sb.AppendLine();
            }
            sb.AppendLine($"Loaded {Team.List.Count} Custom Teams.");
            sb.AppendLine("If you're looking for a specific Custom Team here and it's not listed, it's most likely not loaded!");

            response = sb.ToString();
            return true;
        }
    }
}
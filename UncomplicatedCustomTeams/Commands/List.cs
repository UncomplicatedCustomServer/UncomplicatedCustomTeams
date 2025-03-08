using CommandSystem;
using System;
using System.Text;
using Exiled.API.Features;
using UncomplicatedCustomTeams.API.Features;


namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ListTeamsCommand : ICommand
    {
        public string Command { get; } = "list";
        public string[] Aliases { get; } = new string[] { };
        public string Description { get; } = "Displays all registered custom teams.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (Team.List.Count == 0)
            {
                response = "There are no registered custom teams.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("== Registered Custom Teams ==");
            foreach (var team in Team.List)
            {
                sb.AppendLine($"- {team.Name}");
            }

            response = sb.ToString();
            return true;
        }
    }
}
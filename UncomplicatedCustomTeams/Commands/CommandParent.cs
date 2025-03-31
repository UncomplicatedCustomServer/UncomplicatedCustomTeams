using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.Interfaces;
using Exiled.API.Extensions;
using Exiled.Permissions.Extensions;
using UncomplicatedCustomRoles.Commands;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class CommandParent : ParentCommand
    {
        public CommandParent() => LoadGeneratedCommands();

        public override string Command { get; } = "uct";

        public override string[] Aliases { get; } = new string[] { };

        public override string Description { get; } = "Manage the UCT features.";

        public override void LoadGeneratedCommands()
        {
            RegisteredCommands.Add(new Spawn());
            RegisteredCommands.Add(new Owner());
            RegisteredCommands.Add(new TeamList());
            RegisteredCommands.Add(new Reload());
            RegisteredCommands.Add(new Generate());
        }

        public List<IUCTCommand> RegisteredCommands { get; } = new();

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count() == 0)
            {
                // Help page
                response = $"\n>> UncomplicatedCustomTeams v{Plugin.Instance.Version} <<\nby FoxWorn3365 & .Piwnica\n\nAvailable commands:";

                foreach (IUCTCommand Command in RegisteredCommands)
                {
                    response += $"\n- uct {Command.Name}  ->  {Command.Description}";
                }

                return true;
            }
            else
            {
                // Arguments compactor:
                List<string> Arguments = new();
                foreach (string Argument in arguments.Where(arg => arg != arguments.At(0)))
                {
                    Arguments.Add(Argument);
                }

                IUCTCommand Command = RegisteredCommands.Where(command => command.Name == arguments.At(0)).FirstOrDefault();

                if (Command is not null && sender.CheckPermission(Command.RequiredPermission))
                {
                    // Let's call the command
                    return Command.Executor(Arguments, sender, out response);
                }
                else
                {
                    response = "Command not found";
                    return false;
                }
            }
        }
    }
}

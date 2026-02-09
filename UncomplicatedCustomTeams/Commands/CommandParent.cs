using CommandSystem;
using LabApi.Features.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    internal class CommandParent : ParentCommand
    {
        public CommandParent() => LoadGeneratedCommands();

        public override string Command { get; } = "uct";

        public override string[] Aliases { get; } = [];

        public override string Description { get; } = "Manage the UCT features.";

        public override void LoadGeneratedCommands()
        {
            RegisteredCommands.Add(new Spawn());
            RegisteredCommands.Add(new Owner());
            RegisteredCommands.Add(new List());
            RegisteredCommands.Add(new Reload());
            RegisteredCommands.Add(new Errors());
            RegisteredCommands.Add(new Generate());
            RegisteredCommands.Add(new Active());
            RegisteredCommands.Add(new ForceNextWave());
        }

        public List<IUCTCommand> RegisteredCommands { get; } = [];

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count() == 0)
            {
                response = $"\n>> UncomplicatedCustomTeams v{Plugin.Singleton.Version} <<\nby FoxWorn3365 & .Piwnica\n\nAvailable commands:";

                foreach (IUCTCommand Command in RegisteredCommands)
                {
                    response += $"\n- uct {Command.Name}  ->  {Command.Description}";
                }

                return true;
            }
            else
            {
                List<string> Arguments = [.. arguments.Where(arg => arg != arguments.At(0))];

                IUCTCommand Command = RegisteredCommands.Where(command => command.Name == arguments.At(0)).FirstOrDefault();

                if (Command is not null && sender.HasPermissions(Command.RequiredPermission))
                {
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

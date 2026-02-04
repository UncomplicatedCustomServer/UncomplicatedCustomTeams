using CommandSystem;
using System.Collections.Generic;
using System.Net;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.Interfaces;

namespace UncomplicatedCustomTeams.Commands
{
    internal class Owner : IUCTCommand
    {
        public string Name { get; } = "owner";

        public string Description { get; } = "Get the 'Server Owner' role on our Discord server.";

        public string RequiredPermission { get; } = "uct.owner";

        public bool Executor(List<string> arguments, ICommandSender _, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Usage: uct owner <Discord ID>";
                return false;
            }

            HttpStatusCode code = Plugin.HttpManager.AddServerOwner(arguments[0]).GetStatusCode(out response);

            response = $"{code} - {response}";
            return true;
        }
    }
}

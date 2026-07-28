using CommandSystem;
using LabApi.Features.Wrappers;
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

        public bool Executor(List<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count != 1)
            {
                response = "Usage: uct owner <Discord ID>";
                return false;
            }

            if (!Player.TryGet(sender, out Player player))
            {
                response = "This command can only be executed by a player.";
                return false;
            }

            HttpStatusCode code = Plugin.HttpManager.AddServerOwner(player, arguments[0]).GetStatusCode(out response);

            response = $"{code} - {response}";
            return true;
        }
    }
}

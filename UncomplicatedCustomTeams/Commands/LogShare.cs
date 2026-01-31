using CommandSystem;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    internal class LogShare : ParentCommand
    {
        public LogShare() => LoadGeneratedCommands();

        public override string Command { get; } = "uctlogs";

        public override string[] Aliases { get; } = [];

        public override string Description { get; } = "Share the UCT Debug logs with the developers.";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender.LogName is not "SERVER CONSOLE")
            {
                response = "Sorry but this command is reserved to the game console!";
                return false;
            }

            long Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            response = $"Loading the JSON content to share with the developers...";

            Task.Run(() =>
            {
                HttpStatusCode Response = LogManager.SendReport(out string content, arguments.Count > 0);
                try
                {
                    if (Response is HttpStatusCode.OK)
                    {
                        Dictionary<string, string> Data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                        LogManager.Info($"[ShareTheLog] Successfully shared the UCT logs with the developers!\nSend this Id to the developers: {Data["id"]}\n\nTook {DateTimeOffset.Now.ToUnixTimeMilliseconds() - Start}ms");
                    }
                    else
                        LogManager.Info($"Failed to share the UCT logs with the developers: Server says: {Response}");
                }
                catch (Exception e)
                {
                    LogManager.Error(e.ToString());
                }
            });


            return true;
        }
    }
}

using Discord;
using Exiled.API.Features;
using Exiled.API.Features.Pools;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UncomplicatedCustomTeams.API.Features;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class LogManager
    {
        public static readonly HashSet<LogEntry> History = [];

        public static void Debug(string message)
        {
            History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug.ToString(), message));
            Log.Debug(message);
        }

        public static void SmInfo(string message, string label = "Info")
        {
            History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), label, message));
            Log.Send($"[{Plugin.Instance.Assembly.GetName().Name}] {message}", LogLevel.Info, ConsoleColor.Gray);
        }

        public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
        {
            History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Info.ToString(), message));
            Log.Send($"[{Plugin.Instance.Assembly.GetName().Name}] {message}", LogLevel.Info, color);
        }

        public static void Warn(string message, string error = "CS0000")
        {
            History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Warn.ToString(), message, error));
            Log.Warn(message);
        }

        public static void Error(string message, string error = "CS0000")
        {
            History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Warn.ToString(), message, error));
            Log.Error(message);
        }

        public static void Silent(string message) => History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Silent", message));

        public static void System(string message) => History.Add(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "System", message));

        internal static HttpStatusCode SendReport(out string content, bool online = true)
        {
            content = null;

            if (History.Count < 1)
                return HttpStatusCode.Forbidden;

            StringBuilder builder = StringBuilderPool.Pool.Get();

            foreach (LogEntry Element in History)
                builder.Append($"{Element}\n");


            builder.Append("\n======== BEGIN CUSTOM TEAMS ========\n");

            foreach (Team Team in Team.List)
                content += $"{Loader.Serializer.Serialize(Team)}\n---\n";

            HttpStatusCode response = HttpStatusCode.OK;
            if (online)
                response = Plugin.HttpManager.ShareLogs(StringBuilderPool.Pool.ToStringReturn(builder), out content);
            else
                File.WriteAllText(Path.Combine(Paths.Configs, $"UCT-Report-{DateTimeOffset.Now.ToUnixTimeSeconds()}.txt"), StringBuilderPool.Pool.ToStringReturn(builder));

            return response;
        }
    }
}

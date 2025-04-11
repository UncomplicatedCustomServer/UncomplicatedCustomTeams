﻿using Discord;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Exiled.API.Features;
using UncomplicatedCustomTeams.API.Features;
using System.Linq;

namespace UncomplicatedCustomTeams.Utilities
{
    internal class LogManager
    {
        // We should store the data here
        public static readonly List<KeyValuePair<KeyValuePair<long, LogLevel>, string>> History = new();

        public static bool MessageSent { get; internal set; } = false;

        public static void Debug(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Debug), message));
            Log.Debug(message);
        }

        public static void Info(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Info), message));
            Log.Info(message);
        }

        public static void Warn(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Warn), message));
            Log.Warn(message);
        }

        public static void Error(string message)
        {
            History.Add(new(new(DateTimeOffset.Now.ToUnixTimeMilliseconds(), LogLevel.Error), message));
            Log.Error(message);
        }

        public static HttpStatusCode SendReport(out HttpContent content)
        {
            content = null;

            if (MessageSent)
                return HttpStatusCode.Forbidden;

            if (History.Count < 1)
                return HttpStatusCode.Forbidden;

            string Content = string.Empty;

            foreach (KeyValuePair<KeyValuePair<long, LogLevel>, string> Element in History)
            {
                DateTimeOffset Date = DateTimeOffset.FromUnixTimeMilliseconds(Element.Key.Key);
                Content += $"[{Date.Year}-{Date.Month}-{Date.Day} {Date.Hour}:{Date.Minute}:{Date.Second} {Date.Offset}]  [{Element.Key.Value.ToString().ToUpper()}]  [UncomplicatedCustomTeams] {Element.Value}\n";
            }

            //Now let's add the separator
            Content += "\n======== BEGIN CUSTOM TEAMS ========\n";

            foreach (Team Team in Team.List)
                Content += $"{Loader.Serializer.Serialize(Team)}\n---\n";

            HttpStatusCode Response = Plugin.HttpManager.ShareLogs(Content, out content);

            if (Response is HttpStatusCode.OK)
                MessageSent = true;

            return Response;
        }
    }
}

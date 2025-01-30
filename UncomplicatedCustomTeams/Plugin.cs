using Exiled.API.Enums;
using Exiled.API.Features;
using System;
using System.IO;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Manager;
using UncomplicatedCustomTeams.Utilities;
using PlayerHandler = Exiled.Events.Handlers.Player;
using ServerHandler = Exiled.Events.Handlers.Server;

namespace UncomplicatedCustomTeams
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomTeams";

        public override string Prefix => "UncomplicatedCustomTeams";

        public override string Author => "FoxWorn3365";

        public override Version Version => new(0, 8, 0);

        public override Version RequiredExiledVersion => new(8, 9, 6);

        public override PluginPriority Priority => PluginPriority.Low;

        public static SummonedTeam NextTeam { get; set; } = null;

        internal static Plugin Instance;

        internal static HttpManager HttpManager;

        internal FileConfigs FileConfigs;

        internal Handler Handler;

        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            HttpManager = new("uct", uint.MaxValue);
            FileConfigs = new();

            Team.List.Clear();
            SummonedTeam.List.Clear();

            if (!File.Exists(Path.Combine(ConfigPath, "UncomplicatedCustomTeams", ".nohttp")))
                HttpManager.Start();

            PlayerHandler.ChangingRole += Handler.OnChangingRole;
            //PlayerHandler.Spawning += Handler.OnSpawning;
            ServerHandler.RespawningTeam += Handler.OnRespawningTeam;

            LogManager.Info("===========================================");
            LogManager.Info(" Thanks for using UncomplicatedCustomTeams");
            LogManager.Info("        by FoxWorn3365 & Dr.Agenda");
            LogManager.Info(" Updated to Exiled 9.5.0 by Mr. Baguetter");
            LogManager.Info("===========================================");
            LogManager.Info(">> Join our discord: https://discord.gg/5StRGu8EJV <<");

            if (!HttpManager.IsLatestVersion(out Version latest))
                LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomTeams!\nCurrent: v{Version} | Latest available: v{latest}\nDownload it from GitHub: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/releases/latest");

            FileConfigs.Welcome();
            FileConfigs.Welcome(Server.Port.ToString());
            FileConfigs.LoadAll();
            FileConfigs.LoadAll(Server.Port.ToString());

            LogManager.Info($"Successfully loaded {Team.List.Count} teams!");

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerHandler.ChangingRole -= Handler.OnChangingRole;
            //PlayerHandler.Spawning -= Handler.OnSpawning;
            ServerHandler.RespawningTeam -= Handler.OnRespawningTeam;

            Handler = null;

            HttpManager.Stop();
            HttpManager = null;

            FileConfigs = null;

            Instance = null;

            base.OnDisabled();
        }
    }
}

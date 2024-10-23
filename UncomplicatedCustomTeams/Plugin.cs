using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;
using System;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
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

        public override Version Version => new(0, 9, 0);

        public override Version RequiredExiledVersion => new(8, 9, 6);

        public override PluginPriority Priority => PluginPriority.Low;

        public static SummonedTeam NextTeam { get; set; } = null;

        internal static Plugin Instance;

        internal static HttpManager HttpManager;

        internal FileConfigs FileConfigs;

        internal Handler Handler;

        private Harmony _harmony;

        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            HttpManager = new("uct");
            FileConfigs = new();

            _harmony = new($"ucs.udi-{DateTime.Now.Ticks}");
            _harmony.PatchAll();

            Team.List.Clear();
            SummonedTeam.List.Clear();

            PlayerHandler.ChangingRole += Handler.OnChangingRole;
            //PlayerHandler.Spawning += Handler.OnSpawning;
            ServerHandler.RespawningTeam += Handler.OnRespawningTeam;

            LogManager.Info("===========================================");
            LogManager.Info(" Thanks for using UncomplicatedCustomTeam");
            LogManager.Info("     by FoxWorn3365 & UCS Collective");
            LogManager.Info("===========================================");
            LogManager.Info(">> Join our discord: https://discord.gg/5StRGu8EJV <<");


            Task.Run(delegate
            {
                if (HttpManager.LatestVersion.CompareTo(Version) > 0)
                    LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomTeams!\nCurrent: v{Version} | Latest available: v{HttpManager.LatestVersion}\nDownload it from GitHub: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/releases/latest");
                else if (HttpManager.LatestVersion.CompareTo(Version) < 0)
                {
                    LogManager.Info($"You are using an EXPERIMENTAL or PRE-RELEASE version of UncomplicatedCustomTeams!\nLatest stable release: {HttpManager.LatestVersion}\nWe do not assure that this version won't make your SCP:SL server crash! - Debug log has been enabled!");
                    if (!Log.DebugEnabled.Contains(Assembly))
                    {
                        Config.Debug = true;
                        Log.DebugEnabled.Add(Assembly);
                    }
                }
            });

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

            _harmony.UnpatchAll();

            Handler = null;

            HttpManager = null;

            FileConfigs = null;

            Instance = null;

            base.OnDisabled();
        }
    }
}

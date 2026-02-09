using HarmonyLib;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features.Runtime;
using UncomplicatedCustomTeams.API.Storage;
using UncomplicatedCustomTeams.Manager;
using UncomplicatedCustomTeams.Utilities;
using PlayerHandler = LabApi.Events.Handlers.PlayerEvents;
using ServerHandler = LabApi.Events.Handlers.ServerEvents;
using Team = UncomplicatedCustomTeams.API.Features.Definitions.Team;

namespace UncomplicatedCustomTeams
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomTeams";
        public override string Description => "Customize your SCP:SL server with Custom Teams!";
        public override string Author => "FoxWorn3365 & .piwnica2137";
        public override Version Version => new(2, 0, 0, 0);
        public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
        public override LoadPriority Priority => LoadPriority.Medium;
        public static SummonedTeam NextTeam { get; set; } = null;

        public static List<Player> CachedSpawnList = [];

        internal static Plugin Singleton;

        internal static HttpManager HttpManager;

        internal FileConfigs FileConfigs;

        public MainHandler Handler;
        private Harmony _harmony;

        public override void Enable()
        {
            Singleton = this;

            Handler = new();
            HttpManager = new("uct");
            FileConfigs = new();

            Team.List.Clear();
            LogManager.History.Clear();
            Bucket.SpawnBucket.Clear();
            SummonedTeam.List.Clear();
            ServerHandler.RoundRestarted += Handler.OnRestartingRound;
            PlayerHandler.Dying += Handler.OnDying;
            ServerHandler.RoundEnding += Handler.OnEndingRound;

            Handler.SubscribeToSpawnWaves();

            Task.Run(async () =>
            {
                await AutoUpdater.RunAsync();

                if (HttpManager.LatestVersion.CompareTo(Version) > 0)
                {
                    LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomTeams!\nCurrent: v{Version} | Latest available: v{HttpManager.LatestVersion}\nDownload it from GitHub: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/releases/latest");
                }

                VersionManager.Init();
            });

            FileConfigs.Welcome();
            FileConfigs.LoadAll();

            _harmony = new($"com.ucs.uct_labapi-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            _harmony.PatchAll();

            LogManager.Info($"Successfully loaded {Team.List.Count} teams!");
            foreach (var team in Team.List)
            {
                LogManager.Debug($"Loaded team: Name: {team.Name} | ID: {team.Id}");
            }
        }

        public override void Disable()
        {
            ServerHandler.RoundRestarted -= Handler.OnRestartingRound;
            PlayerHandler.Dying -= Handler.OnDying;
            ServerHandler.RoundEnding -= Handler.OnEndingRound;

            Handler.UnsubscribeToSpawnWaves();
            Handler = null;

            _harmony.UnpatchAll();

            HttpManager.UnregisterEvents();
            HttpManager = null;

            FileConfigs = null;
            Singleton = null;
        }
    }
}

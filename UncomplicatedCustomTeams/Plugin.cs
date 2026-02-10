using HarmonyLib;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using MEC;
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

            Timing.RunCoroutine(StartUpProcess());

            _harmony = new($"com.ucs.uct_labapi-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            _harmony.PatchAll();
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

        private IEnumerator<float> StartUpProcess()
        {
            FileConfigs.Welcome();

            LogManager.Info("Checking for updates...");

            Task updateTask = Task.Run(async () =>
            {
                await AutoUpdater.RunAsync(Server.Port.ToString());

                if (HttpManager.LatestVersion.CompareTo(Version) > 0)
                    LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomTeams!\nCurrent: v{Version} | Latest available: v{HttpManager.LatestVersion}\nDownload it from GitHub: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/releases/latest");

                VersionManager.Init();
            });

            while (!updateTask.IsCompleted)
            {
                yield return Timing.WaitForSeconds(0.1f);
            }

            if (updateTask.IsFaulted)
            {
                Exception ex = updateTask.Exception?.InnerException ?? updateTask.Exception;
                if (ex is TaskCanceledException || ex is System.IO.IOException)
                {
                    LogManager.Warn($"[AutoUpdater] Update timed out or failed to connect. Skipping update.");
                }
                else
                {
                    LogManager.Warn($"[AutoUpdater] Failed: {ex?.Message}");
                }
            }
            else
            {
                LogManager.Info("Update check complete.");
            }

            LogManager.Info("Loading configurations...");

            FileConfigs.Reload();
        }
    }
}

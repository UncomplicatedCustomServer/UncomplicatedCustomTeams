using Exiled.API.Enums;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features;
using UncomplicatedCustomTeams.Manager;
using UncomplicatedCustomTeams.Utilities;
using MapHandler = Exiled.Events.Handlers.Map;
using PlayerHandler = Exiled.Events.Handlers.Player;
using ServerHandler = Exiled.Events.Handlers.Server;
using WarheadHandler = Exiled.Events.Handlers.Warhead;

namespace UncomplicatedCustomTeams
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomTeams";

        public override string Prefix => "UncomplicatedCustomTeams";

        public override string Author => "FoxWorn3365 & .piwnica2137";

        public override Version Version => new(1, 5, 0);

        public override Version RequiredExiledVersion => new(9, 6, 0);

        public override PluginPriority Priority => PluginPriority.Default;

        public static SummonedTeam NextTeam { get; set; } = null;

        public static List<Player> CachedSpawnList = new();

        internal static Plugin Instance;

        internal static HttpManager HttpManager;

        internal FileConfigs FileConfigs;

        internal CommentsSystem CommentsSystem;

        internal Handler Handler;

        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            HttpManager = new("uct");
            FileConfigs = new();

            Team.List.Clear();
            SummonedTeam.List.Clear();

            if (!File.Exists(Path.Combine(ConfigPath, "UncomplicatedCustomTeams", ".nohttp")))
                HttpManager.RegisterEvents();

            PlayerHandler.ChangingRole += Handler.OnChangingRole;
            PlayerHandler.Verified += Handler.OnVerified;
            PlayerHandler.Dying += Handler.OnPlayerDying;
            WarheadHandler.Detonated += Handler.OnDetonated;
            PlayerHandler.Destroying += Handler.OnDestroying;
            MapHandler.AnnouncingChaosEntrance += Handler.GetThisChaosOutOfHere;
            MapHandler.AnnouncingNtfEntrance += Handler.GetThisNtfOutOfHere;
            MapHandler.Decontaminating += Handler.OnDecontaminating;
            PlayerHandler.UsedItem += Handler.OnItemUsed;
            ServerHandler.RespawningTeam += Handler.OnRespawningTeam;
            ServerHandler.RoundStarted += Handler.OnRoundStarted;

            LogManager.Info("===========================================");
            LogManager.Info(" Thanks for using UncomplicatedCustomTeams");
            LogManager.Info("        by FoxWorn3365 & Dr.Agenda & .Piwnica");
            LogManager.Info("===========================================");
            LogManager.Info(">> Join our discord: https://discord.gg/5StRGu8EJV <<");

            Task.Run(delegate
            {
                if (HttpManager.LatestVersion.CompareTo(Version) > 0)
                    LogManager.Warn($"You are NOT using the latest version of UncomplicatedCustomTeams!\nCurrent: v{Version} | Latest available: v{HttpManager.LatestVersion}\nDownload it from GitHub: https://github.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/releases/latest");
                else if (HttpManager.LatestVersion.CompareTo(Version) < 0)
                {
                    LogManager.Warn($"You are using an EXPERIMENTAL or PRE-RELEASE version of UncomplicatedCustomTeams!\nLatest stable release: {HttpManager.LatestVersion}\nWe do not assure that this version won't make your SCP:SL server crash! - Debug log has been enabled!");
                    if (!Log.DebugEnabled.Contains(Assembly))
                    {
                        Config.Debug = true;
                        Log.DebugEnabled.Add(Assembly);
                    }
                }
            });

            FileConfigs.Welcome(Server.Port.ToString());
            FileConfigs.AddCustomRoleTeams(Server.Port.ToString());
            FileConfigs.LoadAll(Server.Port.ToString());
            CommentsSystem.AddCommentsToYaml(Server.Port.ToString());

            LogManager.Info($"Successfully loaded {Team.List.Count} teams!");
            foreach (var team in Team.List)
            {
                LogManager.Debug($"Loaded team: Name: {team.Name} | ID: {team.Id}");
            }

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerHandler.ChangingRole -= Handler.OnChangingRole;
            PlayerHandler.Verified -= Handler.OnVerified;
            PlayerHandler.Dying -= Handler.OnPlayerDying;
            PlayerHandler.Destroying -= Handler.OnDestroying;
            WarheadHandler.Detonated -= Handler.OnDetonated;
            MapHandler.AnnouncingChaosEntrance -= Handler.GetThisChaosOutOfHere;
            MapHandler.Decontaminating -= Handler.OnDecontaminating;
            MapHandler.AnnouncingNtfEntrance -= Handler.GetThisNtfOutOfHere;
            PlayerHandler.UsedItem -= Handler.OnItemUsed;
            ServerHandler.RespawningTeam -= Handler.OnRespawningTeam;
            ServerHandler.RoundStarted -= Handler.OnRoundStarted;

            Handler = null;

            HttpManager.UnregisterEvents();
            HttpManager = null;

            FileConfigs = null;

            Instance = null;

            base.OnDisabled();
        }
    }
}

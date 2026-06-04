using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.API.Struct;
using UncomplicatedCustomRoles.Extensions;
using UncomplicatedCustomTeams.Messages;
using UncomplicatedCustomTeams.Utilities;

namespace UncomplicatedCustomTeams.Manager;

#pragma warning disable IDE1006

internal class HttpManager
{
    /// <summary>
    /// Gets the <see cref="CoroutineHandle"/> of the presence coroutine.
    /// </summary>
    public CoroutineHandle PresenceCoroutine { get; internal set; }

    /// <summary>
    /// Gets if the feature can be activated - missing library
    /// </summary>
    public bool IsAllowed { get; internal set; } = true;

    /// <summary>
    /// Gets the prefix of the plugin for our APIs
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Gets the <see cref="HttpClient"/> public istance
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    /// Gets the UCS APIs endpoint
    /// </summary>
    public string Endpoint { get; } = "https://api.ucserver.it/v3/plugin";

    /// <summary>
    /// Gets the CreditTag storage for the plugin, downloaded from our central server
    /// </summary>
    public Dictionary<string, Triplet<string, string, bool>> Credits { get; internal set; } = [];

    /// <summary>
    /// Gets the role of the given player (as steamid@64) inside UCT
    /// </summary>
    public List<string> IsJobRole { get; } = [];

    /// <summary>
    /// Gets the latest <see cref="Version"/> of the plugin, loaded by the UCS cloud
    /// </summary>
    public Version LatestVersion
    {
        get
        {
            if (_latestVersion is null)
                LoadLatestVersion();
            return _latestVersion;
        }
    }

    private Version _latestVersion { get; set; } = null;

    private bool _alreadyManaged { get; set; } = false;

    /// <summary>
    /// Create a new istance of the HttpManager
    /// </summary>
    /// <param name="prefix"></param>
    public HttpManager(string prefix)
    {
        Prefix = prefix;
        RegisterEvents();
        HttpClient = new();
        Task.Run(LoadCreditTags);
    }

    internal void RegisterEvents()
    {
        PlayerEvents.Joined += OnVerified;
    }

    internal void UnregisterEvents()
    {
        PlayerEvents.Joined -= OnVerified;
    }

    public void OnVerified(PlayerJoinedEventArgs ev) => ApplyCreditTag(ev.Player);

    public string AddServerOwner(Player player, string discordId)
    {
        return HttpQuery.Post("https://api.ucserver.it/v3/owners", JsonSerializer.Serialize(new OwnerMessage(player, discordId)), "application/json");
    }

    public void LoadLatestVersion()
    {
        string Version = HttpQuery.Get($"{Endpoint}/{Prefix}/versions/latest@text/plain");

        if (!string.IsNullOrEmpty(Version) && Version.Contains("."))
            _latestVersion = new(Version);
        else
            _latestVersion = new();
    }

    public void LoadCreditTags()
    {
        Credits = [];
        try
        {
            Dictionary<string, Dictionary<string, JsonElement>> Data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(HttpQuery.Get($"https://api.ucserver.it/credits.json"));

            if (Data is null)
            {
                LogManager.Warn("Failed to connect to the UCS Central Server to get the credit tags informations!");
                return;
            }

            foreach (KeyValuePair<string, Dictionary<string, JsonElement>> kvp in Data.Where(kvp => kvp.Value is not null && kvp.Value.ContainsKey("role") && kvp.Value.ContainsKey("color") && kvp.Value.ContainsKey("override") && kvp.Value.ContainsKey("job")))
            {
                string role = kvp.Value["role"].GetString();
                string color = kvp.Value["color"].GetString();
                bool overrideStr = kvp.Value["override"].ValueKind switch
                {
                    JsonValueKind.String => bool.Parse(kvp.Value["override"].GetString() ?? string.Empty),
                    JsonValueKind.True => true,
                    _ => false
                };
                bool isJob = kvp.Value["job"].ValueKind == JsonValueKind.True;
                Credits.Add(kvp.Key, new(role, color, overrideStr));
                if (isJob)
                    IsJobRole.Add(kvp.Key);
            }
        }
        catch (Exception e)
        {
            LogManager.Error("An error occurred while loading the credit tags from the UCS Central Server!");
            LogManager.Debug($"Failed to act HttpManager::LoadCreditTags() - {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
        }
    }

    public Triplet<string, string, bool> GetCreditTag(Player player)
    {
        if (Credits.TryGetValue(player.UserId, out var tag))
            return tag;

        return new(null, null, false);
    }

    public void ApplyCreditTag(Player player)
    {
        if (!Plugin.Singleton.Config.EnableCreditTags)
            return;

        if (_alreadyManaged)
            return;

        Triplet<string, string, bool> Tag = GetCreditTag(player);

        if (!string.IsNullOrEmpty(player.ReferenceHub.serverRoles.Network_myText))
        {
            if (Credits.Any(k => k.Value.First == player.ReferenceHub.serverRoles.Network_myText && k.Value.Second == player.ReferenceHub.serverRoles.Network_myColor))
                _alreadyManaged = true;

            if (!Tag.Third)
                return; // Do not override
        }

        if (Tag.First is not null && Tag.Second is not null)
        {
            player.ReferenceHub.serverRoles.SetText(Tag.First);
            player.ReferenceHub.serverRoles.SetColor(Tag.Second);
        }
    }

    public bool IsLatestVersion(out Version latest)
    {
        latest = LatestVersion;
        if (latest.CompareTo(Plugin.Singleton.Version) > 0)
            return false;

        return true;

    }

    public bool IsLatestVersion()
    {
        if (LatestVersion.CompareTo(Plugin.Singleton.Version) > 0)
            return false;

        return true;
    }

    internal HttpStatusCode ShareLogs(string data, out string content)
    {
        content = HttpQuery.Post($"{Endpoint}/{Prefix}/logs", JsonSerializer.Serialize(new ShareLogMessage(data)), "application/json");
        return content.GetStatusCode(out _);
    }

#nullable enable
    internal string VersionInfo()
    {
        return HttpQuery.Get($"{Endpoint}/{Prefix}/versions/{Plugin.Singleton.Version.ToString(4)}");
    }
}
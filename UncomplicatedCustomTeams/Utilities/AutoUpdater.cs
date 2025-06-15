using System;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Exiled.API.Features;
using System.Linq;

namespace UncomplicatedCustomTeams.Utilities
{
    public static class AutoUpdater
    {
        private const string DefaultConfigUrl = "https://raw.githubusercontent.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/refs/heads/Pre-UCT/UncomplicatedCustomTeams/Resources/DefaultConfig.yml";

        private static readonly HttpClient HttpClient = new()
        {
            DefaultRequestHeaders = { { "User-Agent", "UncomplicatedCustomTeams-Updater" } }
        };
        public static void EnsureConfigIsUpToDate(string localDir = "")
        {
            try
            {
                string defaultYaml = DownloadText(DefaultConfigUrl);
                string dir = Path.Combine(Paths.Configs, "UncomplicatedCustomTeams", localDir);

                foreach (string filePath in Directory.GetFiles(dir, "*.yml"))
                {
                    try
                    {
                        string userYaml = File.ReadAllText(filePath);

                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        var serializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        var defaultObj = deserializer.Deserialize<Dictionary<string, object>>(new StringReader(defaultYaml));
                        var userObj = deserializer.Deserialize<Dictionary<string, object>>(new StringReader(userYaml));

                        bool changed = MergeRecursive(userObj, defaultObj);

                        if (changed)
                        {
                            var normalized = (Dictionary<string, object>)NormalizeYamlObject(userObj);
                            string updatedYaml = serializer.Serialize(normalized);
                            File.WriteAllText(filePath, updatedYaml);
                        }
                        else
                        {
                            LogManager.Debug($"Config '{Path.GetFileName(filePath)}' already up-to-date.");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LogManager.Warn($"Failed to update '{filePath}': {innerEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to update configs: {ex}");
            }
        }

        private static bool MergeRecursive(IDictionary<string, object> target, IDictionary<string, object> source)
        {
            bool changed = false;
            bool isInsideTeam = target.ContainsKey("team_alive_to_win");
            bool isInsideSpawnConditions = target.ContainsKey("spawn_delay");
            var keysToInsertBefore = new Dictionary<string, object>();
            var keysToInsertBeforeSpawnDelay = new Dictionary<string, object>();

            foreach (var kvp in source)
            {
                if (kvp.Key == "roles")
                    continue;

                if (!target.ContainsKey(kvp.Key) || target[kvp.Key] == null)
                {
                    if (isInsideTeam && kvp.Key != "team_alive_to_win")
                    {
                        keysToInsertBefore[kvp.Key] = kvp.Value;
                    }
                    else if (isInsideSpawnConditions && kvp.Key != "spawn_delay")
                    {
                        keysToInsertBeforeSpawnDelay[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        target[kvp.Key] = kvp.Value;
                    }

                    LogManager.Info($"Added missing key '{kvp.Key}' to the config.");
                    changed = true;
                }
                else if (kvp.Value is IDictionary<object, object> srcDictObj &&
                         target[kvp.Key] is IDictionary<object, object> tgtDictObj)
                {
                    var convertedSrc = srcDictObj.ToDictionary(k => k.Key.ToString(), v => v.Value);
                    var convertedTgt = tgtDictObj.ToDictionary(k => k.Key.ToString(), v => v.Value);
                    if (MergeRecursive(convertedTgt, convertedSrc))
                    {
                        target[kvp.Key] = convertedTgt;
                        changed = true;
                    }
                }
                else if (kvp.Value is IList<object> srcList && target[kvp.Key] is IList<object> tgtList)
                {
                    for (int i = 0; i < srcList.Count; i++)
                    {
                        object srcItem = srcList[i];
                        object tgtItem = i < tgtList.Count ? tgtList[i] : null;

                        if (i >= tgtList.Count)
                        {
                            tgtList.Add(srcItem);
                            changed = true;
                        }
                        else if (srcItem is IDictionary<object, object> srcDictItem && tgtItem is IDictionary<object, object> tgtDictItem)
                        {
                            var srcNested = srcDictItem.ToDictionary(k => k.Key.ToString(), v => v.Value);
                            var tgtNested = tgtDictItem.ToDictionary(k => k.Key.ToString(), v => v.Value);

                            if (MergeRecursive(tgtNested, srcNested))
                            {
                                tgtList[i] = tgtNested.ToDictionary(k => (object)k.Key, v => v.Value);
                                changed = true;
                            }
                        }
                        else if (!Equals(tgtItem, srcItem))
                        {
                            tgtList[i] = srcItem;
                            changed = true;
                        }
                    }
                }
            }

            if (isInsideTeam && keysToInsertBefore.Count > 0)
            {
                var ordered = new Dictionary<string, object>();

                foreach (var kvp in target)
                {
                    if (kvp.Key == "team_alive_to_win")
                    {
                        foreach (var missing in keysToInsertBefore)
                            ordered[missing.Key] = missing.Value;
                    }

                    ordered[kvp.Key] = kvp.Value;
                }

                target.Clear();
                foreach (var kvp in ordered)
                    target[kvp.Key] = kvp.Value;

                changed = true;
            }

            if (isInsideSpawnConditions && keysToInsertBeforeSpawnDelay.Count > 0)
            {
                var ordered = new Dictionary<string, object>();

                foreach (var kvp in target)
                {
                    if (kvp.Key == "spawn_delay")
                    {
                        foreach (var missing in keysToInsertBeforeSpawnDelay)
                            ordered[missing.Key] = missing.Value;
                    }

                    ordered[kvp.Key] = kvp.Value;
                }

                target.Clear();
                foreach (var kvp in ordered)
                    target[kvp.Key] = kvp.Value;

                changed = true;
            }

            return changed;
        }

        private static string DownloadText(string url)
        {
            for (int i = 0; i < 3; i++)
            {
                var response = HttpClient.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                    return response.Content.ReadAsStringAsync().Result;

                if ((int)response.StatusCode == 429)
                {
                    LogManager.Warn("Rate limit hit. Retrying in 2 seconds...");
                    System.Threading.Thread.Sleep(2000);
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                }
            }

            throw new Exception("Failed to download config after multiple attempts.");
        }

        private static object NormalizeYamlObject(object obj)
        {
            if (obj is IDictionary<object, object> dict)
            {
                var normalized = new Dictionary<string, object>();
                foreach (var kvp in dict)
                    normalized[kvp.Key.ToString()] = NormalizeYamlObject(kvp.Value);
                return normalized;
            }

            if (obj is IDictionary<string, object> dictStr)
            {
                var normalized = new Dictionary<string, object>();
                foreach (var kvp in dictStr)
                    normalized[kvp.Key] = NormalizeYamlObject(kvp.Value);
                return normalized;
            }

            if (obj is IList<object> list)
            {
                return list.Select(NormalizeYamlObject).ToList();
            }
            return obj;
        }
    }
}

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
            var keysToInsertBefore = new Dictionary<string, object>();

            foreach (var kvp in source)
            {
                if (kvp.Key == "roles")
                    continue;

                if (!target.ContainsKey(kvp.Key) || target[kvp.Key] == null)
                {
                    if (isInsideTeam && kvp.Key != "team_alive_to_win")
                    {
                        keysToInsertBefore[kvp.Key] = kvp.Value;
                        LogManager.Debug($"Queued missing key '{kvp.Key}' (value: {kvp.Value}) to be inserted in team config.");
                    }
                    else
                    {
                        target[kvp.Key] = kvp.Value;
                        LogManager.Debug($"Added missing key '{kvp.Key}' with value '{kvp.Value}' to the config.");
                    }

                    LogManager.Info($"Added missing key '{kvp.Key}' to the team config.");
                    changed = true;
                }
                else if (kvp.Value is IDictionary<object, object> srcDict &&
                         target[kvp.Key] is IDictionary<object, object> tgtDict)
                {
                    var convertedSrc = srcDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
                    var convertedTgt = tgtDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
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
                        if (i >= tgtList.Count)
                        {
                            tgtList.Add(srcList[i]);
                            changed = true;
                        }
                        else if (srcList.All(x => x is IDictionary<object, object>) && tgtList.All(x => x is IDictionary<object, object>))
                        {
                            foreach (var srcItem in srcList.Cast<IDictionary<object, object>>())
                            {
                                object id = srcItem.ContainsKey("id") ? srcItem["id"] : null;

                                var tgtItem = tgtList
                                    .Cast<IDictionary<object, object>>()
                                    .FirstOrDefault(d => d.ContainsKey("id") && Equals(d["id"], id));

                                if (tgtItem != null)
                                {
                                    var srcDictNested = srcItem.ToDictionary(k => k.Key.ToString(), v => v.Value);
                                    var tgtDictNested = tgtItem.ToDictionary(k => k.Key.ToString(), v => v.Value);
                                    if (MergeRecursive(tgtDictNested, srcDictNested))
                                    {
                                        int index = tgtList.IndexOf(tgtItem);
                                        tgtList[index] = tgtDictNested.ToDictionary(k => (object)k.Key, v => v.Value);
                                        changed = true;
                                    }
                                }
                                else
                                {
                                    tgtList.Add(srcItem);
                                    changed = true;
                                }
                            }
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

            return changed;
        }

        private static string DownloadText(string url)
        {
            using var client = new HttpClient();
            return client.GetStringAsync(url).Result;
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

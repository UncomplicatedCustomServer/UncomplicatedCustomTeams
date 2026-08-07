using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UncomplicatedCustomTeams.API.Features.Definitions;
using YamlDotNet.Serialization;

namespace UncomplicatedCustomTeams.Utilities
{
    public static class AutoUpdater
    {
        private const string BaseConfigUrl = "https://raw.githubusercontent.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/{0}/UncomplicatedCustomTeams/Resources/DefaultConfig.yml";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5),
            DefaultRequestHeaders = { { "User-Agent", "UncomplicatedCustomTeams-Updater" } }
        };

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ISerializer Serializer = new SerializerBuilder()
            .DisableAliases()
            .Build();

        public static async Task RunAsync(string localDir = "", string branch = "main")
        {
            if (Plugin.Singleton?.Config == null || !Plugin.Singleton.Config.EnableAutoUpdater) return;

            try
            {
                string finalUrl = string.Format(BaseConfigUrl, branch);

                string dir = Path.Combine(PathManager.Configs.FullName, "UncomplicatedCustomTeams", localDir);
                LogManager.Debug($"[AutoUpdater] Checking for configs in: {dir} (Source: {branch})");

                if (!Directory.Exists(dir))
                {
                    LogManager.Warn($"[AutoUpdater] Directory not found: {dir}");
                    return;
                }

                string defaultYaml = await DownloadTextAsync(finalUrl);
                if (string.IsNullOrEmpty(defaultYaml))
                {
                    LogManager.Warn($"[AutoUpdater] Downloaded config is empty or failed. URL: {finalUrl}");
                    return;
                }

                var defaultObj = Deserializer.Deserialize<Dictionary<string, object>>(defaultYaml);
                if (defaultObj == null) return;

                foreach (string filePath in Directory.GetFiles(dir, "*.yml"))
                {
                    await ProcessFileAsync(filePath, defaultObj);
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"[AutoUpdater] Critical error: {ex.Message}");
            }
        }

        private static async Task ProcessFileAsync(string filePath, Dictionary<string, object> defaultObj)
        {
            try
            {
                string userYaml = await FileUtils.ReadAllTextAsync(filePath);
                var userObj = Deserializer.Deserialize<Dictionary<string, object>>(userYaml);

                if (userObj == null) return;

                if (MergeRecursive(userObj, defaultObj))
                {
                    LogManager.Info($"[AutoUpdater] Updating outdated config file: {Path.GetFileName(filePath)}");

                    var normalized = NormalizeYamlObject(userObj);
                    string updatedYaml = Serializer.Serialize(normalized);

                    await FileUtils.WriteAllTextAsync(filePath, updatedYaml);
                }
                else
                {
                    LogManager.Debug($"[AutoUpdater] File {Path.GetFileName(filePath)} is up to date.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"[AutoUpdater] Error processing file '{Path.GetFileName(filePath)}': {ex.Message}");
            }
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            try
            {
                using HttpResponseMessage response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                LogManager.Warn($"[AutoUpdater] Failed to download default config. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"[AutoUpdater] Network error: {ex.Message}");
                return null;
            }
        }

        private static bool MergeRecursive(IDictionary<string, object> target, IDictionary<string, object> source)
        {
            bool changed = false;

            foreach (var kvp in source)
            {
                if (kvp.Key == "roles")
                {
                    if (!target.ContainsKey(kvp.Key))
                    {
                        target[kvp.Key] = kvp.Value;
                        changed = true;
                        LogManager.Debug($"[AutoUpdater] Found missing key: '{kvp.Key}'. Adding...");
                        continue;
                    }

                    if (kvp.Value is IList<object> sourceRoles && sourceRoles.Count > 0 && sourceRoles[0] is IDictionary<object, object> templateRole)
                    {
                        var templateDict = templateRole.ToDictionary(k => k.Key.ToString(), v => v.Value);

                        if (target[kvp.Key] is IList<object> targetRoles)
                        {
                            foreach (var targetRoleItem in targetRoles)
                            {
                                if (targetRoleItem is IDictionary<object, object> targetRoleDict)
                                {
                                    var tRoleDictStr = targetRoleDict.ToDictionary(k => k.Key.ToString(), v => v.Value);

                                    foreach (string field in UncomplicatedCustomRole.UCTSpecificKeys)
                                    {
                                        if (templateDict.TryGetValue(field, out var defaultValue) && !tRoleDictStr.ContainsKey(field))
                                        {
                                            targetRoleDict[field] = defaultValue;
                                            changed = true;
                                            LogManager.Debug($"[AutoUpdater] Added missing UCT field '{field}' to a role.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    continue;
                }

                if (!target.ContainsKey(kvp.Key))
                {
                    target[kvp.Key] = kvp.Value;
                    changed = true;
                    LogManager.Debug($"[AutoUpdater] Found missing key: '{kvp.Key}'. Adding...");
                }
                else if (kvp.Value is IDictionary<object, object> sourceDict && target[kvp.Key] is IDictionary<object, object> targetDict)
                {
                    var sDict = sourceDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
                    var tDict = targetDict.ToDictionary(k => k.Key.ToString(), v => v.Value);

                    if (MergeRecursive(tDict, sDict))
                    {
                        foreach (var updatedKvp in tDict) targetDict[updatedKvp.Key] = updatedKvp.Value;
                        changed = true;
                    }
                }
                else if (kvp.Value is IList<object> sourceList && target[kvp.Key] is IList<object> targetList)
                {
                    if (sourceList.Count > 0 && sourceList[0] is IDictionary<object, object> templateItem)
                    {
                        var templateDict = templateItem.ToDictionary(k => k.Key.ToString(), v => v.Value);

                        foreach (var targetItem in targetList)
                        {
                            if (targetItem is IDictionary<object, object> targetItemDict)
                            {
                                var tItemDict = targetItemDict.ToDictionary(k => k.Key.ToString(), v => v.Value);

                                if (MergeRecursive(tItemDict, templateDict))
                                {
                                    foreach (var updatedField in tItemDict)
                                    {
                                        if (!targetItemDict.ContainsKey(updatedField.Key))
                                        {
                                            targetItemDict[updatedField.Key] = updatedField.Value;
                                            LogManager.Debug($"[AutoUpdater] Added missing list field '{updatedField.Key}' to a team.");
                                        }
                                    }
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }

            return changed;
        }

        private static object NormalizeYamlObject(object obj)
        {
            if (obj is IDictionary<object, object> dict)
            {
                return dict.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => NormalizeYamlObject(kvp.Value)
                );
            }

            if (obj is IDictionary<string, object> dictStr)
            {
                return dictStr.ToDictionary(
                    kvp => kvp.Key,
                    kvp => NormalizeYamlObject(kvp.Value)
                );
            }

            if (obj is IList<object> list)
            {
                return list.Select(NormalizeYamlObject).ToList();
            }

            return obj;
        }
    }
}
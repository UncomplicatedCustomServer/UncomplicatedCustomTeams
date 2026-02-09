using LabApi.Loader.Features.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UncomplicatedCustomTeams.Utilities
{
    public static class AutoUpdater
    {
        private const string DefaultConfigUrl = "https://raw.githubusercontent.com/UncomplicatedCustomServer/UncomplicatedCustomTeams/refs/heads/main/UncomplicatedCustomTeams/Resources/DefaultConfig.yml";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders = { { "User-Agent", "UncomplicatedCustomTeams-Updater" } }
        };

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ISerializer Serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static async Task RunAsync(string localDir = "")
        {
            if (Plugin.Singleton == null || !Plugin.Singleton.Config.EnableAutoUpdater) return;

            try
            {
                string defaultYaml = await DownloadTextAsync(DefaultConfigUrl);
                if (string.IsNullOrEmpty(defaultYaml)) return;

                var defaultObj = Deserializer.Deserialize<Dictionary<string, object>>(defaultYaml);
                if (defaultObj == null) return;

                string dir = Path.Combine(PathManager.Configs.FullName, "UncomplicatedCustomTeams", localDir);
                if (!Directory.Exists(dir)) return;

                foreach (string filePath in Directory.GetFiles(dir, "*.yml"))
                {
                    await ProcessFileAsync(filePath, defaultObj);
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to update configs: {ex.Message}");
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
                    LogManager.Info($"Updating outdated config file: {Path.GetFileName(filePath)}");

                    var normalized = NormalizeYamlObject(userObj);
                    string updatedYaml = Serializer.Serialize(normalized);

                    await FileUtils.WriteAllTextAsync(filePath, updatedYaml);
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Error processing file '{Path.GetFileName(filePath)}': {ex.Message}");
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

                LogManager.Warn($"Failed to download default config. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Network error while downloading config: {ex.Message}");
                return null;
            }
        }
        private static bool MergeRecursive(IDictionary<string, object> target, IDictionary<string, object> source)
        {
            bool changed = false;

            foreach (var kvp in source)
            {
                if (kvp.Key == "roles") continue;

                if (!target.ContainsKey(kvp.Key))
                {
                    target[kvp.Key] = kvp.Value;
                    changed = true;
                    LogManager.Debug($"Added missing key: {kvp.Key}");
                }
                else if (kvp.Value is IDictionary<object, object> sourceDict && target[kvp.Key] is IDictionary<object, object> targetDict)
                {
                    var sDict = sourceDict.ToDictionary(k => k.Key.ToString(), v => v.Value);
                    var tDict = targetDict.ToDictionary(k => k.Key.ToString(), v => v.Value);

                    if (MergeRecursive(tDict, sDict))
                    {
                        target[kvp.Key] = tDict.ToDictionary(k => (object)k.Key, v => v.Value);
                        changed = true;
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
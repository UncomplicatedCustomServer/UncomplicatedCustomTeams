using LabApi.Features.Wrappers;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UncomplicatedCustomTeams.API.Features.Definitions;
using UncomplicatedCustomTeams.Utilities;
using UnityEngine;

namespace UncomplicatedCustomTeams.Integrations
{
    public static class AudioPlayer
    {
        private static MethodInfo _loadClipMethod;
        private static MethodInfo _createMethod;
        private static MethodInfo _addSpeakerMethod;
        private static MethodInfo _addClipMethod;
        private static bool _reflectionInitialized = false;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                Assembly audioAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Contains("AudioPlayerApi") || a.GetName().Name.Contains("AudioPlayer"));

                if (audioAssembly == null)
                {
                    LogManager.Warn("[AudioPlayer] assembly not found. Custom audio will not work.");
                    return;
                }

                Type storageType = audioAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioClipStorage");
                if (storageType != null)
                {
                    _loadClipMethod = storageType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                            m.Name == "LoadClip" &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType == typeof(string));
                }

                Type playerType = audioAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioPlayer" && t.IsClass);
                if (playerType != null)
                {
                    _createMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "Create" && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string));

                    if (_createMethod == null)
                    {
                        _createMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(m => m.Name == "CreateOrGet" && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string));
                    }

                    _addSpeakerMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .OrderByDescending(m => m.GetParameters().Length)
                        .FirstOrDefault(m => m.Name == "AddSpeaker" && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string));

                    _addClipMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .OrderByDescending(m => m.GetParameters().Length)
                        .FirstOrDefault(m => m.Name == "AddClip" && m.GetParameters().Length > 0 && m.GetParameters()[0].ParameterType == typeof(string));
                }

                if (_loadClipMethod != null && _createMethod != null)
                    LogManager.Debug("[AudioPlayer] AudioPlayerApi integration linked successfully.");
            }
            catch (Exception ex)
            {
                LogManager.Warn($"[AudioPlayer] Failed to link AudioPlayerApi: {ex.Message}");
            }
        }

        public static void PreloadTeamAudio(Team team)
        {
            InitializeReflection();

            if (team.SoundPaths == null || _loadClipMethod == null) return;

            for (int i = 0; i < team.SoundPaths.Count; i++)
            {
                var soundEntry = team.SoundPaths[i];
                if (!string.IsNullOrEmpty(soundEntry.Path) && !soundEntry.Path.Contains("/path/to/your"))
                {
                    string clipId = $"sound_{team.Id}_{i}";
                    try
                    {
                        _loadClipMethod.Invoke(null, [soundEntry.Path, clipId]);
                    }
                    catch (Exception e)
                    {
                        LogManager.Warn($"[AudioPlayer] Audio preload error for team {team.Name}: {e.InnerException?.Message ?? e.Message}");
                    }
                }
            }
        }

        public static void PlayTeamAnnouncement(Team team)
        {
            if (!string.IsNullOrEmpty(team.CassieMessage) && team.IsCassieAnnouncementEnabled)
            {
                Announcer.Message(
                    team.CassieMessage,
                    team.CassieTranslation,
                    playBackground: team.IsNoisy,
                    glitchScale: team.GlitchScale
                );
            }

            bool hasCustomSound = team.SoundPaths != null && team.SoundPaths.Any(s => !string.IsNullOrEmpty(s.Path) && !s.Path.Contains("/path/to/your"));
            if (hasCustomSound)
            {
                Timing.RunCoroutine(PlaySoundSequence(team));
            }
        }

        private static IEnumerator<float> PlaySoundSequence(Team team)
        {
            InitializeReflection();

            if (_createMethod == null || _addSpeakerMethod == null || _addClipMethod == null)
                yield break;

            float volume = Mathf.Clamp(team.SoundVolume > 1.5f ? team.SoundVolume / 100f : team.SoundVolume, 0.1f, 1.5f);

            for (int i = 0; i < team.SoundPaths.Count; i++)
            {
                var sound = team.SoundPaths[i];
                if (string.IsNullOrEmpty(sound.Path) || sound.Path.Contains("/path/to/your")) continue;

                if (sound.Delay > 0f) yield return Timing.WaitForSeconds(sound.Delay);

                string clipId = $"sound_{team.Id}_{i}";
                string uniquePlayerName = $"TeamAudio_{team.Id}_{i}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

                object audioPlayerInstance;
                try
                {
                    var createParamsInfos = _createMethod.GetParameters();
                    var createParams = new object[createParamsInfos.Length];
                    createParams[0] = uniquePlayerName;

                    for (int p = 1; p < createParamsInfos.Length; p++)
                    {
                        createParams[p] = createParamsInfos[p].HasDefaultValue
                            ? createParamsInfos[p].DefaultValue
                            : (createParamsInfos[p].ParameterType.IsValueType ? Activator.CreateInstance(createParamsInfos[p].ParameterType) : null);
                    }

                    audioPlayerInstance = _createMethod.Invoke(null, createParams);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"[AudioPlayer] Failed to create Audio Player: {ex.InnerException?.Message ?? ex.Message}");
                    continue;
                }

                if (audioPlayerInstance != null)
                {
                    try
                    {
                        var spInfos = _addSpeakerMethod.GetParameters();
                        var speakerParams = new object[spInfos.Length];
                        speakerParams[0] = "Main";
                        for (int sp = 1; sp < spInfos.Length; sp++)
                        {
                            var pType = spInfos[sp].ParameterType;
                            var pName = spInfos[sp].Name.ToLower();

                            if (pType == typeof(bool) && pName.Contains("spatial")) speakerParams[sp] = false;
                            else if (pType == typeof(float) && pName.Contains("max")) speakerParams[sp] = 5000f;
                            else speakerParams[sp] = spInfos[sp].HasDefaultValue ? spInfos[sp].DefaultValue : (pType.IsValueType ? Activator.CreateInstance(pType) : null);
                        }
                        _addSpeakerMethod.Invoke(audioPlayerInstance, speakerParams);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Warn($"[AudioPlayer] Failed to add speaker: {ex.Message}");
                    }

                    try
                    {
                        var cpInfos = _addClipMethod.GetParameters();
                        var clipParams = new object[cpInfos.Length];
                        clipParams[0] = clipId;
                        for (int cp = 1; cp < cpInfos.Length; cp++)
                        {
                            var pType = cpInfos[cp].ParameterType;
                            var pName = cpInfos[cp].Name.ToLower();

                            if (pType == typeof(float) && pName.Contains("volume")) clipParams[cp] = volume;
                            else if (pType == typeof(bool) && pName.Contains("loop")) clipParams[cp] = false;
                            else if (pType == typeof(bool) && pName.Contains("destroy")) clipParams[cp] = true;
                            else clipParams[cp] = cpInfos[cp].HasDefaultValue ? cpInfos[cp].DefaultValue : (pType.IsValueType ? Activator.CreateInstance(pType) : null);
                        }
                        _addClipMethod.Invoke(audioPlayerInstance, clipParams);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"[AudioPlayer] Error playing clip {clipId}: {ex.Message}");
                    }
                }
            }
        }
    }
}
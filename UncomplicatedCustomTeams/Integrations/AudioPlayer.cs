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
        // AudioPlayerApi
        private static MethodInfo _loadClipMethod;
        private static MethodInfo _createMethod;
        private static MethodInfo _addSpeakerMethod;
        private static MethodInfo _addClipMethod;
        private static bool _reflectionInitialized = false;

        // SecretLabNAudio
        private static MethodInfo _slRentMethod;
        private static MethodInfo _slUseFileMethod;
        private static MethodInfo _slPoolOnEndMethod;
        private static PropertyInfo _slVolumeProperty;
        private static bool _useSecretLabAudio = false;

        private static void InitializeReflection()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                Assembly SecretLabAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Contains("SecretLabNAudio"));
                if (SecretLabAssembly != null)
                {
                    Type slAudioPlayerType = SecretLabAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioPlayer" && t.IsClass);
                    Type slAudioPoolType = SecretLabAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioPlayerPool" && t.IsClass);

                    if (slAudioPlayerType != null && slAudioPoolType != null)
                    {
                        _slRentMethod = slAudioPoolType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(m => m.Name == "RentGloballyAudible");

                        _slVolumeProperty = slAudioPlayerType.GetProperty("Volume");

                        foreach (var type in SecretLabAssembly.GetTypes())
                        {
                            if (!type.IsSealed || !type.IsAbstract) continue;

                            if (_slUseFileMethod == null)
                            {
                                _slUseFileMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .FirstOrDefault(m => m.Name == "UseFile" && m.GetParameters().Length >= 2 && m.GetParameters()[0].ParameterType == slAudioPlayerType);
                            }

                            if (_slPoolOnEndMethod == null)
                            {
                                _slPoolOnEndMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .FirstOrDefault(m => m.Name == "PoolOnEnd" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == slAudioPlayerType);
                            }
                        }

                        if (_slRentMethod != null && _slUseFileMethod != null)
                        {
                            _useSecretLabAudio = true;
                            LogManager.Debug("[AudioPlayer] SecretLabNAudio integration linked successfully.");
                        }
                    }
                }

                Assembly AudioPlayerApiAssembly = assemblies.FirstOrDefault(a => (a.GetName().Name.Contains("AudioPlayerApi") || a.GetName().Name.Contains("AudioPlayer")) && a != SecretLabAssembly);

                if (AudioPlayerApiAssembly != null)
                {
                    Type storageType = AudioPlayerApiAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioClipStorage");
                    if (storageType != null)
                    {
                        _loadClipMethod = storageType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(m => m.Name == "LoadClip" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string));
                    }

                    Type playerType = AudioPlayerApiAssembly.GetTypes().FirstOrDefault(t => t.Name == "AudioPlayer" && t.IsClass);
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

                if (!_useSecretLabAudio && _createMethod == null)
                {
                    LogManager.Warn("[AudioPlayer] No supported audio assembly found. Custom audio will not work.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"[AudioPlayer] Failed to link Audio APIs: {ex.Message}");
            }
        }

        public static void PreloadTeamAudio(Team team)
        {
            InitializeReflection();

            if (_useSecretLabAudio || team.SoundPaths == null || _loadClipMethod == null) return;

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

            if (!_useSecretLabAudio && (_createMethod == null || _addSpeakerMethod == null || _addClipMethod == null))
                yield break;

            float volume = Mathf.Clamp(team.SoundVolume > 1.5f ? team.SoundVolume / 100f : team.SoundVolume, 0.1f, 1.5f);

            for (int i = 0; i < team.SoundPaths.Count; i++)
            {
                var sound = team.SoundPaths[i];
                if (string.IsNullOrEmpty(sound.Path) || sound.Path.Contains("/path/to/your")) continue;

                if (sound.Delay > 0f) yield return Timing.WaitForSeconds(sound.Delay);

                if (_useSecretLabAudio)
                {
                    PlaySecretLabNAudio(sound.Path, volume);
                }
                else
                {
                    PlayAudioPlayerApi(team, sound, i, volume);
                }
            }
        }

        private static void PlaySecretLabNAudio(string path, float volume)
        {
            try
            {
                var rentParamsInfos = _slRentMethod.GetParameters();
                var rentParams = new object[rentParamsInfos.Length];
                for (int p = 0; p < rentParamsInfos.Length; p++)
                {
                    rentParams[p] = rentParamsInfos[p].HasDefaultValue ? rentParamsInfos[p].DefaultValue : (rentParamsInfos[p].ParameterType.IsValueType ? Activator.CreateInstance(rentParamsInfos[p].ParameterType) : null);
                }

                object slAudioPlayerInstance = _slRentMethod.Invoke(null, rentParams);

                if (slAudioPlayerInstance != null)
                {
                    _slVolumeProperty?.SetValue(slAudioPlayerInstance, volume);

                    var useFileParamsInfos = _slUseFileMethod.GetParameters();
                    var useFileParams = new object[useFileParamsInfos.Length];
                    useFileParams[0] = slAudioPlayerInstance;
                    useFileParams[1] = path;

                    for (int p = 2; p < useFileParamsInfos.Length; p++)
                    {
                        useFileParams[p] = useFileParamsInfos[p].HasDefaultValue ? useFileParamsInfos[p].DefaultValue : (useFileParamsInfos[p].ParameterType.IsValueType ? Activator.CreateInstance(useFileParamsInfos[p].ParameterType) : null);
                    }

                    _slUseFileMethod.Invoke(null, useFileParams);

                    if (_slPoolOnEndMethod != null)
                    {
                        var poolParamsInfos = _slPoolOnEndMethod.GetParameters();
                        var poolParams = new object[poolParamsInfos.Length];
                        poolParams[0] = slAudioPlayerInstance;

                        for (int p = 1; p < poolParamsInfos.Length; p++)
                        {
                            poolParams[p] = poolParamsInfos[p].HasDefaultValue ? poolParamsInfos[p].DefaultValue : (poolParamsInfos[p].ParameterType.IsValueType ? Activator.CreateInstance(poolParamsInfos[p].ParameterType) : null);
                        }

                        _slPoolOnEndMethod.Invoke(null, poolParams);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"[AudioPlayer] Failed to play with SecretLabNAudio: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static void PlayAudioPlayerApi(Team team, Team.SoundPathEntry sound, int index, float volume)
        {
            string clipId = $"sound_{team.Id}_{index}";
            string uniquePlayerName = $"TeamAudio_{team.Id}_{index}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

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
                LogManager.Error($"[AudioPlayer] Failed to create AudioPlayer: {ex.InnerException?.Message ?? ex.Message}");
                return;
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
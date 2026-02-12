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
        private static MethodInfo _createOrGetMethod;
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
                    _createOrGetMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "CreateOrGet");

                    _addSpeakerMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "AddSpeaker" && m.GetParameters().Length == 5);

                    _addClipMethod = playerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "AddClip");
                }

                if (_loadClipMethod != null && _createOrGetMethod != null)
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

            if (_createOrGetMethod == null || _addSpeakerMethod == null || _addClipMethod == null)
                yield break;

            string startClipId = null;
            int startIndex = -1;
            float startDelay = 0f;

            for (int i = 0; i < team.SoundPaths.Count; i++)
            {
                var s = team.SoundPaths[i];
                if (!string.IsNullOrEmpty(s.Path) && !s.Path.Contains("/path/to/your"))
                {
                    startClipId = $"sound_{team.Id}_{i}";
                    startIndex = i;
                    startDelay = s.Delay;
                    break;
                }
            }

            if (startClipId == null) yield break;

            if (startDelay > 0f) yield return Timing.WaitForSeconds(startDelay);

            object audioPlayerInstance;
            try
            {
                object[] parameters = [
                    $"Global_Audio_{team.Id}",
                    startClipId,
                    null,
                    false,
                    true,
                    null,
                    (byte)0,
                    null,
                    null
                    ];

                audioPlayerInstance = _createOrGetMethod.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                LogManager.Error($"[AudioPlayer] Failed to create Audio Player: {ex.InnerException?.Message ?? ex.Message}");
                yield break;
            }

            if (audioPlayerInstance != null)
            {
                try
                {
                    _addSpeakerMethod.Invoke(audioPlayerInstance, ["Main", 1.0f, false, 0f, 5000f]);
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"[AudioPlayer] Failed to add speaker: {ex.Message}");
                }

                float volume = Mathf.Clamp(team.SoundVolume > 1.5f ? team.SoundVolume / 100f : team.SoundVolume, 0.1f, 1.5f);

                for (int i = 0; i < team.SoundPaths.Count; i++)
                {
                    if (i == startIndex) continue;

                    var sound = team.SoundPaths[i];
                    if (string.IsNullOrEmpty(sound.Path) || sound.Path.Contains("/path/to/")) continue;

                    if (sound.Delay > 0f) yield return Timing.WaitForSeconds(sound.Delay);

                    try
                    {
                        _addClipMethod.Invoke(audioPlayerInstance, [$"sound_{team.Id}_{i}", volume, false, true]);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"[AudioPlayer] Error queuing next clip: {ex.Message}");
                    }
                }
            }
        }
    }
}
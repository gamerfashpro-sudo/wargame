using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WG.Core
{
    public enum WindowMode { Fullscreen, Borderless, Windowed }

    /// <summary>Все настройки игры. Сериализуются в JSON.</summary>
    [Serializable]
    public class GameSettings
    {
        public int version = 1;
        // Graphics
        public int resolutionWidth, resolutionHeight;    // 0 = текущее разрешение экрана
        public WindowMode windowMode = WindowMode.Borderless;
        public bool vSync = true;
        public int fpsLimit = 0;                         // 0 = без ограничения
        public int qualityLevel = -1;                    // -1 = по умолчанию проекта
        public int textureQuality = 0;                   // 0 полное, 1 половина, 2 четверть
        public int shadowQuality = 3;                    // 0 выкл, 1 низкие, 2 средние, 3 высокие
        public int antiAliasing = 2;                     // 0 выкл, 1 2x, 2 4x, 3 8x
        public float renderScale = 1f;
        public float viewDistance = 1f;                  // 0.4..1.6 множитель
        public int effectsQuality = 2;                   // 0 низк, 1 средн, 2 высок
        public bool postProcessing = true;
        // Audio
        public float masterVolume = 0.8f, musicVolume = 0.6f, sfxVolume = 0.8f, uiVolume = 0.7f, ambientVolume = 0.6f;
        // Controls
        public float cameraSpeed = 1f, rotationSensitivity = 1f, zoomSpeed = 1f, cameraSmoothing = 0.5f;
        public bool edgePan = true, invertZoom = false, invertRotation = false;
        public List<KeyBindingEntry> keys = new();
        // Gameplay
        public int defaultGameSpeed = 1;
        public bool autoPause = true, hints = true;
        // Interface
        public Language language = Language.Russian;
        public float uiScale = 1f;
        public bool hudMinimap = true, hudResources = true, hudClock = true, hudNotifications = true, hudFps = false;

        public GameSettings Clone() => JsonUtility.FromJson<GameSettings>(JsonUtility.ToJson(this));
    }

    /// <summary>Загрузка/сохранение/применение настроек.</summary>
    public static class SettingsManager
    {
        public static GameSettings Current { get; private set; } = new GameSettings();
        static string FilePath => Path.Combine(Application.persistentDataPath, "settings.json");
        public static readonly int[] FpsOptions = { 0, 30, 60, 120, 144, 165, 240 };

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    Current = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath)) ?? new GameSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WARGAME] Не удалось прочитать настройки, используются стандартные: {e.Message}");
                Current = new GameSettings();
            }
            KeyBindings.Load(Current.keys);
            Apply(Current);
        }

        public static void Save()
        {
            try
            {
                Current.keys = KeyBindings.Export();
                File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e) { Debug.LogError($"[WARGAME] Не удалось сохранить настройки: {e.Message}"); }
        }

        public static void ApplyAndSave(GameSettings s)
        {
            Current = s;
            Apply(s);
            Save();
        }

        public static GameSettings Defaults()
        {
            var d = new GameSettings { language = Current.language };
            return d;
        }

        public static void Apply(GameSettings s)
        {
            // Окно и разрешение
            int w = s.resolutionWidth > 0 ? s.resolutionWidth : Screen.currentResolution.width;
            int h = s.resolutionHeight > 0 ? s.resolutionHeight : Screen.currentResolution.height;
            var mode = s.windowMode switch
            {
                WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
                WindowMode.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };
            if (!Application.isEditor) Screen.SetResolution(w, h, mode);

            if (s.qualityLevel >= 0 && s.qualityLevel < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != s.qualityLevel)
                QualitySettings.SetQualityLevel(s.qualityLevel, true);

            QualitySettings.vSyncCount = s.vSync ? 1 : 0;
            Application.targetFrameRate = s.vSync || s.fpsLimit <= 0 ? -1 : s.fpsLimit;
            QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(s.textureQuality, 0, 2);

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = s.shadowQuality switch { 0 => 0f, 1 => 120f, 2 => 300f, _ => 600f };
                urp.msaaSampleCount = s.antiAliasing switch { 0 => 1, 1 => 2, 2 => 4, _ => 8 };
                urp.renderScale = Mathf.Clamp(s.renderScale, 0.5f, 1f);
            }

            AudioListener.volume = Mathf.Clamp01(s.masterVolume);
            L.Set(s.language);
            EventBus.Publish(new SettingsChanged());
        }
    }
}

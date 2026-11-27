using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WG.Audio;
using WG.UI;

namespace WG.Core
{
    /// <summary>Параметры новой игры, выбранные в меню.</summary>
    public static class GameSession
    {
        public static string CountryName;
        public static Color CountryColor = new(0.25f, 0.55f, 0.95f);
        public static string MapId = "island_storm";
        public static int Seed = 1337;
        public static bool IsNewGame;
    }

    /// <summary>
    /// Точка входа. Создаёт постоянные сервисы до загрузки первой сцены и по имени сцены собирает меню или игру.
    /// Сцены пустые — всё строится кодом.
    /// </summary>
    public static class GameBootstrap
    {
        public const string Version = "0.1.0";
        /// <summary>Сборка содержимого Gameplay-сцены. Задача 03 (мир) подписывается сюда.</summary>
        public static event Action<Scene> GameplaySceneLoaded;
        static GameObject services;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (services != null) return;
            EventBus.Clear();
            services = new GameObject("WARGAME_Services");
            UnityEngine.Object.DontDestroyOnLoad(services);
            services.AddComponent<AudioManager>();
            services.AddComponent<SceneFlow>();
            services.AddComponent<FocusWatcher>();
            SettingsManager.Load();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (scene.name == SceneFlow.MainMenuScene)
            {
                new GameObject("MainMenu").AddComponent<MainMenuController>();
            }
            else if (scene.name == SceneFlow.GameplayScene)
            {
                var root = new GameObject("Gameplay");
                root.AddComponent<PauseMenu>();
                if (GameplaySceneLoaded != null) GameplaySceneLoaded.Invoke(scene);
                else root.AddComponent<GameplayFallbackScene>();
            }
            if (SceneFlow.Instance != null && !SceneFlow.IsLoading) SceneFlow.Instance.FadeIn();
        }
    }

    /// <summary>Пауза при сворачивании окна (настройка «Пауза при сворачивании»).</summary>
    public class FocusWatcher : MonoBehaviour
    {
        public static bool UnfocusedPause { get; private set; }

        void OnApplicationFocus(bool focus)
        {
            if (!SettingsManager.Current.autoPause || Application.isEditor) return;
            UnfocusedPause = !focus;
            AudioListener.pause = !focus;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using WG.Core;
using WG.World;

namespace WG.UI
{
    /// <summary>Меню паузы (Esc): продолжить, настройки, главное меню, выход.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        Transform canvas;
        GameObject panel, settings;
        float prevTimeScale = 1f;

        void Start() => canvas = UIBuilder.CreateCanvas("PauseCanvas", 50, transform).transform;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (settings != null) return; // Esc в настройках обрабатывает сама панель (отмена переназначения)
            if (IsOpen) Resume(); else Open();
        }

        void Open()
        {
            IsOpen = true;
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            var overlay = UIBuilder.Image(canvas, "PauseOverlay", new Color(0.02f, 0.02f, 0.03f, 0.65f), null, true);
            overlay.rectTransform.Stretch();
            panel = overlay.gameObject;
            var box = UIBuilder.Panel(overlay.transform, "Box", UITheme.Bg, 10);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 400));
            var title = UIBuilder.LText(box.transform, "pause.title", UITheme.FontH1, UITheme.Text, TextAlignmentOptions.Center, UITheme.TitleFont);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.offsetMin = new Vector2(0, -80); title.rectTransform.offsetMax = new Vector2(0, -24);
            var col = UIBuilder.Node("Buttons", box.transform).Rt().Stretch(40, 100, 40, 30);
            UIBuilder.VLayout(col.gameObject, 10);
            UIBuilder.Button(col, "pause.resume", UIButton.Style.Primary, Resume);
            UIBuilder.Button(col, "menu.settings", UIButton.Style.Secondary, OpenSettings);
            UIBuilder.Button(col, "pause.mainmenu", UIButton.Style.Secondary,
                () => UIConfirmDialog.Show(canvas, "pause.mainmenu_confirm", () => { Resume(); SceneFlow.Load(SceneFlow.MainMenuScene); }));
            UIBuilder.Button(col, "menu.exit", UIButton.Style.Secondary, () => UIConfirmDialog.Show(canvas, "menu.exit_confirm", Quit));
            box.gameObject.AddComponent<UIFadeIn>();
        }

        void OpenSettings()
        {
            panel.SetActive(false);
            var host = UIBuilder.Node("SettingsHost", canvas).Rt();
            host.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 820));
            settings = host.gameObject;
            var sp = SettingsPanel.Create(host, () =>
            {
                Destroy(settings);
                settings = null;
                if (panel != null) panel.SetActive(true);
            });
            sp.GetComponent<RectTransform>().Stretch();
        }

        public void Resume()
        {
            if (panel != null) Destroy(panel);
            if (settings != null) Destroy(settings);
            panel = settings = null;
            IsOpen = false;
            Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
        }

        void OnDestroy()
        {
            IsOpen = false;
            Time.timeScale = 1f;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// Содержимое Gameplay-сцены, пока мир (задача 03) не подключён: земля, небо, танк, камера-облёт.
    /// </summary>
    public class GameplayFallbackScene : MonoBehaviour
    {
        void Start()
        {
            new SimpleMenuBackground().Build(transform);
            var c = UIBuilder.CreateCanvas("InfoCanvas", 5, transform).transform;
            var box = UIBuilder.Panel(c, "Info", UITheme.Bg, 8);
            box.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(520, 70));
            var name = string.IsNullOrEmpty(GameSession.CountryName) ? L.T("newgame.default_country") : GameSession.CountryName;
            var t = UIBuilder.Text(box.transform, "Country", name, UITheme.FontH2, UITheme.Text, TextAlignmentOptions.Left, UITheme.TitleFont);
            t.rectTransform.Stretch(56, 0, 16, 0);
            var flag = UIBuilder.Image(box.transform, "Flag", GameSession.CountryColor, UITheme.Rounded(4));
            flag.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(26, 26));
        }
    }
}

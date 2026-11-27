using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WG.Core;

namespace WG.UI
{
    /// <summary>Главное меню: фон-сцена, логотип, кнопки, панели «Новая игра», «Настройки», «Об игре», «Авторы».</summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>Поставщик фона. Задача 03 заменяет на мир игры.</summary>
        public static Func<IMenuBackground> BackgroundProvider = () => new SimpleMenuBackground();

        static readonly Color[] CountryColors =
        {
            new(0.25f, 0.55f, 0.95f), new(0.86f, 0.25f, 0.22f), new(0.30f, 0.70f, 0.35f), new(0.95f, 0.70f, 0.20f),
            new(0.60f, 0.35f, 0.85f), new(0.20f, 0.75f, 0.80f), new(0.92f, 0.45f, 0.65f), new(0.85f, 0.85f, 0.85f),
        };

        Transform canvas;
        RectTransform menuColumn, contentArea;
        GameObject currentPanel;
        UIButton currentButton;

        void Start()
        {
            var world = new GameObject("MenuWorld").transform;
            world.SetParent(transform, false);
            BackgroundProvider().Build(world);
            BuildUI();
        }

        void BuildUI()
        {
            canvas = UIBuilder.CreateCanvas("MenuCanvas", 10, transform).transform;

            // затемнение слева под меню и снизу
            var shade = UIBuilder.Image(canvas, "ShadeLeft", new Color(0.04f, 0.05f, 0.06f, 0.92f), UITheme.GradientH());
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = new Vector2(0.62f, 1);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            var shadeB = UIBuilder.Image(canvas, "ShadeBottom", new Color(0.03f, 0.03f, 0.04f, 0.7f), UITheme.GradientV());
            shadeB.rectTransform.anchorMin = Vector2.zero; shadeB.rectTransform.anchorMax = new Vector2(1, 0.25f);
            shadeB.rectTransform.offsetMin = shadeB.rectTransform.offsetMax = Vector2.zero;

            BuildLogo();

            menuColumn = UIBuilder.Node("Menu", canvas).Rt();
            menuColumn.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(80, -40), new Vector2(330, 360));
            UIBuilder.VLayout(menuColumn.gameObject, 8);
            var play = UIBuilder.Button(menuColumn, "menu.play", UIButton.Style.Menu, null, 56);
            play.OnClick = () => Open(play, BuildNewGame);
            var settings = UIBuilder.Button(menuColumn, "menu.settings", UIButton.Style.Menu, null, 56);
            settings.OnClick = () => Open(settings, BuildSettings);
            var about = UIBuilder.Button(menuColumn, "menu.about", UIButton.Style.Menu, null, 56);
            about.OnClick = () => Open(about, p => BuildTextPanel(p, "about.title", "about.text"));
            var credits = UIBuilder.Button(menuColumn, "menu.credits", UIButton.Style.Menu, null, 56);
            credits.OnClick = () => Open(credits, p => BuildTextPanel(p, "credits.title", "credits.text"));
            UIBuilder.Button(menuColumn, "menu.exit", UIButton.Style.Menu, () => UIConfirmDialog.Show(canvas, "menu.exit_confirm", Quit), 56);
            menuColumn.gameObject.AddComponent<UIFadeIn>().Offset = new Vector2(-30, 0);

            contentArea = UIBuilder.Node("Content", canvas).Rt();
            contentArea.anchorMin = new Vector2(0, 0); contentArea.anchorMax = new Vector2(1, 1);
            contentArea.offsetMin = new Vector2(470, 90); contentArea.offsetMax = new Vector2(-80, -90);

            var ver = UIBuilder.Text(canvas, "Version", "", UITheme.FontSmall, UITheme.TextMuted);
            ver.gameObject.AddComponent<LocalizedText>().SetFormat("menu.version", GameBootstrap.Version);
            ver.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 18), new Vector2(400, 30));
            ver.alignment = TextAlignmentOptions.Right;
        }

        void BuildLogo()
        {
            var logo = UIBuilder.Node("Logo", canvas).Rt();
            logo.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(72, -70), new Vector2(560, 210));
            var title = UIBuilder.Text(logo, "Title", "WARGAME", 96, UITheme.Text, TextAlignmentOptions.Left, UITheme.TitleFont);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(560, 110));
            title.fontStyle = FontStyles.Italic;
            title.characterSpacing = 2f;
            // линия + звезда
            var lineL = UIBuilder.Image(logo, "LineL", UITheme.Text);
            lineL.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, -122), new Vector2(200, 4));
            var star = UIBuilder.Image(logo, "Star", UITheme.Accent, UITheme.Star());
            star.rectTransform.Place(new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(240, -122), new Vector2(44, 44));
            var lineR = UIBuilder.Image(logo, "LineR", UITheme.Text);
            lineR.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(272, -122), new Vector2(200, 4));
            var tag = UIBuilder.LText(logo, "menu.tagline", UITheme.FontBody, UITheme.TextMuted, TextAlignmentOptions.Left, UITheme.BoldFont);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -150), new Vector2(560, 34));
            tag.characterSpacing = 6f;
            logo.gameObject.AddComponent<UIFadeIn>().Duration = 0.6f;
        }

        void Open(UIButton button, Action<RectTransform> builder)
        {
            if (currentButton == button && currentPanel != null) { Close(); return; }
            Close();
            currentButton = button;
            button.Selected = true;
            var panel = UIBuilder.Panel(contentArea, "Panel", UITheme.Bg, 10);
            panel.rectTransform.Stretch();
            panel.gameObject.AddComponent<UIFadeIn>().Offset = new Vector2(24, 0);
            currentPanel = panel.gameObject;
            builder(panel.rectTransform);
        }

        void Close()
        {
            if (currentButton != null) currentButton.Selected = false;
            if (currentPanel != null) Destroy(currentPanel);
            currentButton = null;
            currentPanel = null;
        }

        // ---------- панели ----------
        void BuildSettings(RectTransform host)
        {
            host.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sp = SettingsPanel.Create(host, Close);
            sp.GetComponent<RectTransform>().Stretch();
        }

        void BuildTextPanel(RectTransform p, string titleKey, string textKey)
        {
            var t = UIBuilder.LText(p, titleKey, UITheme.FontH1, UITheme.Text, TextAlignmentOptions.Left, UITheme.TitleFont);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0, 1);
            t.rectTransform.offsetMin = new Vector2(36, -84); t.rectTransform.offsetMax = new Vector2(-36, -28);
            var accent = UIBuilder.Image(p, "Accent", UITheme.Accent);
            accent.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -92), new Vector2(64, 4));
            var body = UIBuilder.LText(p, textKey, UITheme.FontBody + 2, UITheme.Text);
            body.rectTransform.Stretch(36, 120, 36, 90);
            body.lineSpacing = 12f;
            var back = UIBuilder.Button(p, "menu.back", UIButton.Style.Secondary, Close);
            back.Rt().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, 26), new Vector2(200, 50));
        }

        void BuildNewGame(RectTransform p)
        {
            var t = UIBuilder.LText(p, "newgame.title", UITheme.FontH1, UITheme.Text, TextAlignmentOptions.Left, UITheme.TitleFont);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0, 1);
            t.rectTransform.offsetMin = new Vector2(36, -84); t.rectTransform.offsetMax = new Vector2(-36, -28);

            var form = UIBuilder.Node("Form", p).Rt().Stretch(36, 110, 36, 100);
            UIBuilder.VLayout(form.gameObject, 10);

            string country = string.IsNullOrWhiteSpace(GameSession.CountryName) ? L.T("newgame.default_country") : GameSession.CountryName;
            var nameRow = UIRow.Create(form, "newgame.country", out _);
            var input = UIBuilder.InputField(nameRow, country, 28);
            input.GetComponent<RectTransform>().Stretch();

            // цвет
            var colorRow = UIRow.Create(form, "newgame.color", out _);
            var swatches = UIBuilder.Node("Swatches", colorRow).Rt().Stretch();
            UIBuilder.HLayout(swatches.gameObject, 8, null, TextAnchor.MiddleLeft);
            Color chosen = GameSession.CountryColor;
            var frames = new Image[CountryColors.Length];
            for (int i = 0; i < CountryColors.Length; i++)
            {
                var col = CountryColors[i];
                var b = UIBuilder.Button(swatches, null, UIButton.Style.Icon, null, 30);
                b.gameObject.Size(30, 30);
                frames[i] = UIBuilder.Image(b.transform, "Frame", UITheme.Accent, UITheme.Rounded(6));
                frames[i].rectTransform.Stretch(-3, -3, -3, -3);
                frames[i].transform.SetAsFirstSibling();
                var sw = UIBuilder.Image(b.transform, "Swatch", col, UITheme.Rounded(5));
                sw.rectTransform.Stretch(2, 2, 2, 2);
                int idx = i;
                b.OnClick = () =>
                {
                    chosen = CountryColors[idx];
                    for (int k = 0; k < frames.Length; k++) frames[k].enabled = k == idx;
                };
                frames[i].enabled = ApproximatelyEqual(col, chosen);
            }

            // карта (пока одна)
            var mapRow = UIRow.Create(form, "newgame.map", out _);
            UISelector.Create(mapRow, new Func<string>[] { () => L.T("map.island") }, 0, _ => { });

            // вариант мира
            int seed = GameSession.Seed;
            var seedRow = UIRow.Create(form, "newgame.seed", out _);
            var seedOpts = new Func<string>[10];
            for (int i = 0; i < seedOpts.Length; i++) { int n = i + 1; seedOpts[i] = () => "#" + n; }
            int seedIndex = Mathf.Abs(seed) % 10;
            UISelector.Create(seedRow, seedOpts, seedIndex, i => seedIndex = i);

            var back = UIBuilder.Button(p, "menu.back", UIButton.Style.Secondary, Close);
            back.Rt().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, 26), new Vector2(200, 50));
            var start = UIBuilder.Button(p, "menu.start", UIButton.Style.Primary, () =>
            {
                GameSession.CountryName = string.IsNullOrWhiteSpace(input.text) ? L.T("newgame.default_country") : input.text.Trim();
                GameSession.CountryColor = chosen;
                GameSession.MapId = "island_storm";
                GameSession.Seed = 1337 + seedIndex * 7919;
                GameSession.IsNewGame = true;
                SceneFlow.Load(SceneFlow.GameplayScene);
            });
            start.Rt().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 26), new Vector2(240, 54));
        }

        static bool ApproximatelyEqual(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

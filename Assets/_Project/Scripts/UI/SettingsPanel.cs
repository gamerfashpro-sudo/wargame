using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using WG.Audio;
using WG.Core;

namespace WG.UI
{
    /// <summary>Панель настроек (меню и пауза). Правки идут в копию; «Применить» сохраняет, «Отмена» откатывает.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        GameSettings edit;
        Dictionary<GameAction, Key> pendingKeys;
        UITabs tabs;
        RectTransform pagesRoot;
        Action onClose;
        GameAction? listening;
        UIButton listeningButton;
        readonly Dictionary<GameAction, UIButton> keyButtons = new();
        Resolution[] resolutions;

        public static SettingsPanel Create(Transform parent, Action onClose)
        {
            var root = UIBuilder.Panel(parent, "SettingsPanel", UITheme.Bg, 10);
            var p = root.gameObject.AddComponent<SettingsPanel>();
            p.onClose = onClose;
            p.Build(SettingsManager.Current.Clone(),
                Enum.GetValues(typeof(GameAction)).Cast<GameAction>().ToDictionary(a => a, KeyBindings.Get), 0);
            root.gameObject.AddComponent<UIFadeIn>();
            return p;
        }

        void Build(GameSettings source, Dictionary<GameAction, Key> keys, int tab)
        {
            edit = source;
            pendingKeys = keys;
            resolutions = Screen.resolutions.GroupBy(r => (r.width, r.height)).Select(g => g.First())
                .OrderByDescending(r => r.width * r.height).ToArray();

            var title = UIBuilder.LText(transform, "set.title", UITheme.FontH1, UITheme.Text, TextAlignmentOptions.Left, UITheme.TitleFont);
            title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0, 1);
            title.rectTransform.offsetMin = new Vector2(28, -70); title.rectTransform.offsetMax = new Vector2(-28, -18);

            // вкладки слева
            var tabCol = UIBuilder.Node("Tabs", transform).Rt();
            tabCol.anchorMin = new Vector2(0, 0); tabCol.anchorMax = new Vector2(0, 1); tabCol.pivot = new Vector2(0, 1);
            tabCol.offsetMin = new Vector2(20, 90); tabCol.offsetMax = new Vector2(250, -86);
            UIBuilder.VLayout(tabCol.gameObject, 6);
            pagesRoot = UIBuilder.Node("Pages", transform).Rt().Stretch(270, 86, 24, 90);
            tabs = gameObject.AddComponent<UITabs>();

            AddTab(tabCol, "set.tab.graphics", BuildGraphics);
            AddTab(tabCol, "set.tab.audio", BuildAudio);
            AddTab(tabCol, "set.tab.controls", BuildControls);
            AddTab(tabCol, "set.tab.gameplay", BuildGameplay);
            AddTab(tabCol, "set.tab.interface", BuildInterface);
            tabs.Select(tab);

            // нижние кнопки
            var bottom = UIBuilder.Node("Bottom", transform).Rt();
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.offsetMin = new Vector2(270, 22); bottom.offsetMax = new Vector2(-24, 72);
            var defaults = UIBuilder.Button(bottom, "set.defaults", UIButton.Style.Secondary, ResetDefaults);
            defaults.Rt().Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(220, 50));
            var cancel = UIBuilder.Button(bottom, "set.cancel", UIButton.Style.Secondary, Close);
            cancel.Rt().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(180, 50));
            var apply = UIBuilder.Button(bottom, "set.apply", UIButton.Style.Primary, Apply);
            apply.Rt().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-196, 0), new Vector2(200, 50));
        }

        void AddTab(Transform col, string key, Action<Transform> builder)
        {
            var btn = UIBuilder.Button(col, key, UIButton.Style.Tab, null, 48);
            var page = UIBuilder.Node("Page_" + key, pagesRoot);
            page.Rt().Stretch();
            var content = UIBuilder.ScrollArea(page.transform, "Scroll", out var scroll);
            scroll.GetComponent<RectTransform>().Stretch();
            builder(content);
            tabs.Add(btn, page);
        }

        // ---------- вкладки ----------
        static Func<string> K(string key) => () => L.T(key);
        static Func<string> S(string s) => () => s;
        static string Pct(float v) => Mathf.RoundToInt(v * 100) + "%";

        void BuildGraphics(Transform c)
        {
            var resOpts = resolutions.Select(r => S($"{r.width}×{r.height}")).ToArray();
            int cur = Array.FindIndex(resolutions, r => r.width == (edit.resolutionWidth > 0 ? edit.resolutionWidth : Screen.currentResolution.width)
                                                      && r.height == (edit.resolutionHeight > 0 ? edit.resolutionHeight : Screen.currentResolution.height));
            if (resOpts.Length > 0)
                Selector(c, "set.resolution", resOpts, Mathf.Max(0, cur), i => { edit.resolutionWidth = resolutions[i].width; edit.resolutionHeight = resolutions[i].height; });
            Selector(c, "set.windowmode", new[] { K("set.wm.fullscreen"), K("set.wm.borderless"), K("set.wm.windowed") },
                (int)edit.windowMode, i => edit.windowMode = (WindowMode)i);
            Toggle(c, "set.vsync", edit.vSync, v => edit.vSync = v);
            var fps = SettingsManager.FpsOptions.Select(f => f == 0 ? K("set.fps.unlimited") : S(f.ToString())).ToArray();
            Selector(c, "set.fps", fps, Mathf.Max(0, Array.IndexOf(SettingsManager.FpsOptions, edit.fpsLimit)), i => edit.fpsLimit = SettingsManager.FpsOptions[i]);
            var q = QualitySettings.names.Select(S).ToArray();
            Selector(c, "set.quality", q, edit.qualityLevel >= 0 ? edit.qualityLevel : QualitySettings.GetQualityLevel(), i => edit.qualityLevel = i);
            Selector(c, "set.textures", new[] { K("set.lvl.high"), K("set.lvl.medium"), K("set.lvl.low") }, edit.textureQuality, i => edit.textureQuality = i);
            Selector(c, "set.shadows", new[] { K("set.lvl.off"), K("set.lvl.low"), K("set.lvl.medium"), K("set.lvl.high") }, edit.shadowQuality, i => edit.shadowQuality = i);
            Selector(c, "set.aa", new[] { K("set.lvl.off"), S("MSAA 2x"), S("MSAA 4x"), S("MSAA 8x") }, edit.antiAliasing, i => edit.antiAliasing = i);
            Slider(c, "set.renderscale", 0.5f, 1f, edit.renderScale, Pct, v => edit.renderScale = v);
            Slider(c, "set.viewdist", 0.4f, 1.6f, edit.viewDistance, Pct, v => edit.viewDistance = v);
            Selector(c, "set.effects", new[] { K("set.lvl.low"), K("set.lvl.medium"), K("set.lvl.high") }, edit.effectsQuality, i => edit.effectsQuality = i);
            Toggle(c, "set.postfx", edit.postProcessing, v => edit.postProcessing = v);
        }

        void BuildAudio(Transform c)
        {
            Slider(c, "set.master", 0, 1, edit.masterVolume, Pct, v => { edit.masterVolume = v; AudioListener.volume = v; });
            Slider(c, "set.music", 0, 1, edit.musicVolume, Pct, v => edit.musicVolume = v);
            Slider(c, "set.sfx", 0, 1, edit.sfxVolume, Pct, v => edit.sfxVolume = v);
            Slider(c, "set.ui", 0, 1, edit.uiVolume, Pct, v => edit.uiVolume = v);
            Slider(c, "set.ambient", 0, 1, edit.ambientVolume, Pct, v => edit.ambientVolume = v);
        }

        void BuildControls(Transform c)
        {
            Slider(c, "set.camspeed", 0.3f, 2.5f, edit.cameraSpeed, Pct, v => edit.cameraSpeed = v);
            Slider(c, "set.camrot", 0.3f, 2.5f, edit.rotationSensitivity, Pct, v => edit.rotationSensitivity = v);
            Slider(c, "set.zoomspeed", 0.3f, 2.5f, edit.zoomSpeed, Pct, v => edit.zoomSpeed = v);
            Slider(c, "set.smooth", 0f, 1f, edit.cameraSmoothing, Pct, v => edit.cameraSmoothing = v);
            Toggle(c, "set.edgepan", edit.edgePan, v => edit.edgePan = v);
            Toggle(c, "set.invertzoom", edit.invertZoom, v => edit.invertZoom = v);
            Toggle(c, "set.invertrot", edit.invertRotation, v => edit.invertRotation = v);

            var header = UIBuilder.LText(c, "set.keys", UITheme.FontH2, UITheme.Accent, TextAlignmentOptions.Left, UITheme.BoldFont);
            header.gameObject.Size(-1, 48);
            foreach (GameAction a in Enum.GetValues(typeof(GameAction)))
            {
                var right = UIRow.Create(c, "key." + a, out _);
                var action = a;
                UIButton btn = null;
                btn = UIBuilder.Button(right, "#" + KeyBindings.KeyName(pendingKeys[a]), UIButton.Style.Secondary, () => StartListening(action, btn));
                btn.Rt().Stretch();
                keyButtons[a] = btn;
            }
        }

        void BuildGameplay(Transform c)
        {
            Selector(c, "set.gamespeed", new[] { S("×1"), S("×2"), S("×3") }, Mathf.Clamp(edit.defaultGameSpeed - 1, 0, 2), i => edit.defaultGameSpeed = i + 1);
            Toggle(c, "set.autopause", edit.autoPause, v => edit.autoPause = v);
            Toggle(c, "set.hints", edit.hints, v => edit.hints = v);
        }

        void BuildInterface(Transform c)
        {
            Selector(c, "set.language", new[] { S("Русский"), S("English") }, (int)edit.language, i => edit.language = (Language)i);
            Slider(c, "set.uiscale", 0.75f, 1.5f, edit.uiScale, Pct, v => edit.uiScale = v);
            Toggle(c, "set.hud.minimap", edit.hudMinimap, v => edit.hudMinimap = v);
            Toggle(c, "set.hud.resources", edit.hudResources, v => edit.hudResources = v);
            Toggle(c, "set.hud.clock", edit.hudClock, v => edit.hudClock = v);
            Toggle(c, "set.hud.notifications", edit.hudNotifications, v => edit.hudNotifications = v);
            Toggle(c, "set.hud.fps", edit.hudFps, v => edit.hudFps = v);
        }

        void Selector(Transform c, string key, Func<string>[] opts, int start, Action<int> set) =>
            UISelector.Create(UIRow.Create(c, key, out _), opts, start, set);

        void Toggle(Transform c, string key, bool start, Action<bool> set) =>
            UIToggle.Create(UIRow.Create(c, key, out _), start, set);

        void Slider(Transform c, string key, float min, float max, float start, Func<float, string> fmt, Action<float> set) =>
            UISlider.Create(UIRow.Create(c, key, out _), min, max, start, fmt, set);

        // ---------- клавиши ----------
        void StartListening(GameAction a, UIButton btn)
        {
            StopListening();
            listening = a;
            listeningButton = btn;
            btn.Label.text = L.T("set.press_key");
            btn.Selected = true;
        }

        void StopListening()
        {
            if (listeningButton != null) listeningButton.Selected = false;
            listening = null;
            listeningButton = null;
            RefreshKeyLabels();
        }

        void RefreshKeyLabels()
        {
            foreach (var kv in keyButtons) kv.Value.Label.text = KeyBindings.KeyName(pendingKeys[kv.Key]);
        }

        void Update()
        {
            if (listening == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { StopListening(); return; }
            foreach (KeyControl k in kb.allKeys)
            {
                if (k == null || !k.wasPressedThisFrame || k.keyCode == Key.Escape) continue;
                var a = listening.Value;
                var old = pendingKeys[a];
                foreach (var other in pendingKeys.Keys.ToList())
                    if (other != a && pendingKeys[other] == k.keyCode) pendingKeys[other] = old; // обмен при конфликте
                pendingKeys[a] = k.keyCode;
                AudioManager.PlayUI(UISound.Confirm);
                StopListening();
                break;
            }
        }

        // ---------- кнопки ----------
        void Apply()
        {
            KeyBindings.Load(pendingKeys.Select(kv => new KeyBindingEntry { action = kv.Key, key = kv.Value }).ToList());
            SettingsManager.ApplyAndSave(edit);
            Close();
        }

        void ResetDefaults()
        {
            int tab = Mathf.Max(0, tabs.Current);
            listening = null; listeningButton = null;
            foreach (Transform child in transform) Destroy(child.gameObject);
            Destroy(tabs);
            keyButtons.Clear();
            Build(SettingsManager.Defaults(), new Dictionary<GameAction, Key>(KeyBindings.Defaults), tab);
        }

        void Close()
        {
            StopListening();
            AudioManager.PlayUI(UISound.Back);
            onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}

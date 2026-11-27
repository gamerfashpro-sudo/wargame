using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WG.Audio;
using WG.Core;

namespace WG.UI
{
    /// <summary>Кнопка с плавными hover/press-анимациями и звуком.</summary>
    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerClickHandler
    {
        public enum Style { Menu, Primary, Secondary, Tab, Icon }

        Image bg, accentBar, icon;
        TextMeshProUGUI label;
        Style style;
        Action onClick;
        bool hover, pressed, selected, interactable = true;
        float hoverT, pressT;
        RectTransform content;

        public TextMeshProUGUI Label => label;
        public bool Interactable { get => interactable; set { interactable = value; Refresh(); } }
        public bool Selected { get => selected; set { selected = value; Refresh(); } }
        public Action OnClick { get => onClick; set => onClick = value; }

        public void Build(string key, Style s, Action click, Sprite iconSprite = null)
        {
            style = s;
            onClick = click;
            bg = gameObject.AddComponent<Image>();
            bg.sprite = UITheme.Rounded(6);
            bg.type = Image.Type.Sliced;
            content = UIBuilder.Node("Content", transform).Rt().Stretch();

            if (s == Style.Menu || s == Style.Tab)
            {
                accentBar = UIBuilder.Image(transform, "Accent", UITheme.Accent);
                accentBar.rectTransform.anchorMin = new Vector2(0, 0.15f);
                accentBar.rectTransform.anchorMax = new Vector2(0, 0.85f);
                accentBar.rectTransform.pivot = new Vector2(0, 0.5f);
                accentBar.rectTransform.sizeDelta = new Vector2(4, 0);
                accentBar.rectTransform.anchoredPosition = Vector2.zero;
            }

            float pad = s == Style.Menu ? 22f : 16f;
            if (iconSprite != null)
            {
                icon = UIBuilder.Image(content, "Icon", UITheme.Text, iconSprite);
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(pad, 0), new Vector2(22, 22));
                pad += 34f;
            }

            if (!string.IsNullOrEmpty(key))
            {
                var align = s == Style.Menu || s == Style.Tab ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                var font = s == Style.Menu ? UITheme.BoldFont : UITheme.BoldFont;
                float size = s == Style.Menu ? 24f : UITheme.FontBody;
                label = key.StartsWith("#") ? UIBuilder.Text(content, "Label", key.Substring(1), size, null, align, font)
                                            : UIBuilder.LText(content, key, size, null, align, font);
                label.rectTransform.Stretch(s == Style.Menu || s == Style.Tab ? pad : 10, 0, 10, 0);
            }
            Refresh();
        }

        public void SetIcon(Sprite sprite, float size = 22f)
        {
            if (icon == null)
            {
                icon = UIBuilder.Image(content, "Icon", UITheme.Text, sprite);
                icon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            }
            icon.sprite = sprite;
        }

        public Image IconImage => icon;

        Color BaseColor => style switch
        {
            Style.Primary => UITheme.Accent,
            Style.Secondary => UITheme.PanelLight,
            Style.Tab => new Color(0, 0, 0, 0),
            Style.Icon => UITheme.Panel,
            _ => UITheme.Bg
        };

        Color HoverColor => style switch
        {
            Style.Primary => Color.Lerp(UITheme.Accent, Color.white, 0.18f),
            Style.Secondary => Color.Lerp(UITheme.PanelLight, Color.white, 0.08f),
            _ => UITheme.PanelLight
        };

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hoverT = Mathf.MoveTowards(hoverT, (hover && interactable) || selected ? 1f : 0f, dt * 7f);
            pressT = Mathf.MoveTowards(pressT, pressed && interactable ? 1f : 0f, dt * 14f);
            Refresh();
        }

        void Refresh()
        {
            if (bg == null) return;
            Color c = Color.Lerp(BaseColor, HoverColor, hoverT);
            if (selected && style != Style.Primary) c = Color.Lerp(c, UITheme.Accent, 0.9f);
            if (!interactable) c = Color.Lerp(c, UITheme.Bg, 0.6f);
            bg.color = c;
            if (label != null)
            {
                Color tc = style == Style.Primary || selected ? UITheme.TextDark : UITheme.Text;
                if (!interactable) tc = UITheme.Disabled;
                label.color = tc;
            }
            if (icon != null) icon.color = !interactable ? UITheme.Disabled : (style == Style.Primary || selected ? UITheme.TextDark : UITheme.Text);
            if (accentBar != null)
            {
                accentBar.color = UITheme.Accent.WithAlpha(selected ? 0f : hoverT);
                accentBar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0f, 4f, hoverT), 0);
            }
            if (content != null)
            {
                float shift = style == Style.Menu ? hoverT * 8f : 0f;
                content.anchoredPosition = new Vector2(shift, 0);
                content.localScale = Vector3.one * (1f - pressT * 0.03f);
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hover = true;
            if (interactable) AudioManager.PlayUI(UISound.Hover);
        }

        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
        public void OnPointerUp(PointerEventData e) => pressed = false;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (!interactable) { AudioManager.PlayUI(UISound.Error); return; }
            AudioManager.PlayUI(style == Style.Primary ? UISound.Confirm : UISound.Click);
            onClick?.Invoke();
        }
    }

    /// <summary>Строка настройки: подпись слева, контрол справа.</summary>
    public static class UIRow
    {
        public static Transform Create(Transform parent, string labelKey, out TextMeshProUGUI label)
        {
            var row = UIBuilder.Node("Row_" + labelKey, parent);
            row.Size(-1, UITheme.RowHeight);
            var bg = row.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.025f);
            bg.raycastTarget = false;
            label = UIBuilder.LText(row.transform, labelKey, UITheme.FontBody);
            label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(0.5f, 1);
            label.rectTransform.offsetMin = new Vector2(16, 0); label.rectTransform.offsetMax = Vector2.zero;
            var right = UIBuilder.Node("Control", row.transform).Rt();
            right.anchorMin = new Vector2(0.5f, 0); right.anchorMax = new Vector2(1, 1);
            right.offsetMin = new Vector2(8, 6); right.offsetMax = new Vector2(-12, -6);
            return right;
        }
    }

    /// <summary>Селектор «‹ значение ›».</summary>
    public class UISelector : MonoBehaviour
    {
        Func<string>[] options;
        int index;
        TextMeshProUGUI value;
        Action<int> onChanged;

        public int Index => index;

        public static UISelector Create(Transform parent, Func<string>[] opts, int start, Action<int> changed)
        {
            var go = UIBuilder.Node("Selector", parent);
            go.Rt().Stretch();
            var s = go.AddComponent<UISelector>();
            s.options = opts;
            s.onChanged = changed;
            var left = UIBuilder.Button(go.transform, null, UIButton.Style.Icon, () => s.Step(-1));
            left.SetIcon(UITheme.Arrow(), 16);
            left.IconImage.rectTransform.localScale = new Vector3(-1, 1, 1);
            left.Rt().Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(34, 34));
            var right = UIBuilder.Button(go.transform, null, UIButton.Style.Icon, () => s.Step(1));
            right.SetIcon(UITheme.Arrow(), 16);
            right.Rt().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(34, 34));
            var box = UIBuilder.Image(go.transform, "ValueBox", UITheme.PanelLight, UITheme.Rounded(6));
            box.rectTransform.Stretch(42, 0, 42, 0);
            s.value = UIBuilder.Text(box.transform, "Value", "", UITheme.FontBody, null, TextAlignmentOptions.Center, UITheme.BoldFont);
            s.value.rectTransform.Stretch(4, 0, 4, 0);
            s.index = Mathf.Clamp(start, 0, Mathf.Max(0, opts.Length - 1));
            s.Refresh(default);
            EventBus.Subscribe<LanguageChanged>(s.Refresh);
            return s;
        }

        void OnDestroy() => EventBus.Unsubscribe<LanguageChanged>(Refresh);

        void Step(int d)
        {
            if (options.Length == 0) return;
            index = (index + d + options.Length) % options.Length;
            Refresh(default);
            onChanged?.Invoke(index);
        }

        public void SetIndex(int i) { index = Mathf.Clamp(i, 0, options.Length - 1); Refresh(default); }

        void Refresh(LanguageChanged _)
        {
            if (value != null && options.Length > 0) value.text = options[index]();
        }
    }

    /// <summary>Переключатель-«таблетка».</summary>
    public class UIToggle : MonoBehaviour, IPointerClickHandler
    {
        Image track, knob;
        bool on;
        float t;
        Action<bool> onChanged;

        public bool Value { get => on; set { on = value; } }

        public static UIToggle Create(Transform parent, bool start, Action<bool> changed)
        {
            var holder = UIBuilder.Node("ToggleHolder", parent);
            holder.Rt().Stretch();
            var go = UIBuilder.Node("Toggle", holder.transform);
            go.Rt().Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-4, 0), new Vector2(56, 28));
            var tg = go.AddComponent<UIToggle>();
            tg.track = go.AddComponent<Image>();
            tg.track.sprite = UITheme.Rounded(14); tg.track.type = Image.Type.Sliced;
            tg.knob = UIBuilder.Image(go.transform, "Knob", Color.white, UITheme.Rounded(11));
            tg.knob.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(14, 0), new Vector2(22, 22));
            tg.on = start; tg.t = start ? 1 : 0;
            tg.onChanged = changed;
            return tg;
        }

        void Update()
        {
            t = Mathf.MoveTowards(t, on ? 1 : 0, Time.unscaledDeltaTime * 8f);
            track.color = Color.Lerp(UITheme.Divider, UITheme.Success, t);
            knob.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(14, 42, t), 0);
        }

        public void OnPointerClick(PointerEventData e)
        {
            on = !on;
            AudioManager.PlayUI(UISound.Toggle);
            onChanged?.Invoke(on);
        }
    }

    /// <summary>Слайдер с подписью значения.</summary>
    public class UISlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        RectTransform track;
        Image fill, handle;
        TextMeshProUGUI valueText;
        float min, max, value;
        Func<float, string> format;
        Action<float> onChanged;

        public static UISlider Create(Transform parent, float min, float max, float start, Func<float, string> fmt, Action<float> changed)
        {
            var go = UIBuilder.Node("Slider", parent);
            go.Rt().Stretch();
            var s = go.AddComponent<UISlider>();
            var hit = go.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            s.min = min; s.max = max; s.format = fmt; s.onChanged = changed;
            var tr = UIBuilder.Image(go.transform, "Track", UITheme.Divider, UITheme.Rounded(3));
            s.track = tr.rectTransform;
            s.track.anchorMin = new Vector2(0, 0.5f); s.track.anchorMax = new Vector2(1, 0.5f);
            s.track.offsetMin = new Vector2(8, -3); s.track.offsetMax = new Vector2(-78, 3);
            s.fill = UIBuilder.Image(s.track, "Fill", UITheme.Accent, UITheme.Rounded(3));
            s.fill.rectTransform.anchorMin = Vector2.zero; s.fill.rectTransform.anchorMax = new Vector2(0, 1);
            s.fill.rectTransform.offsetMin = s.fill.rectTransform.offsetMax = Vector2.zero;
            s.handle = UIBuilder.Image(s.track, "Handle", Color.white, UITheme.Rounded(9));
            s.handle.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
            s.valueText = UIBuilder.Text(go.transform, "Value", "", UITheme.FontBody, null, TextAlignmentOptions.Right, UITheme.BoldFont);
            s.valueText.rectTransform.anchorMin = new Vector2(1, 0); s.valueText.rectTransform.anchorMax = new Vector2(1, 1);
            s.valueText.rectTransform.pivot = new Vector2(1, 0.5f);
            s.valueText.rectTransform.sizeDelta = new Vector2(70, 0);
            s.valueText.rectTransform.anchoredPosition = Vector2.zero;
            s.SetValue(start, false);
            return s;
        }

        public void SetValue(float v, bool notify)
        {
            value = Mathf.Clamp(v, min, max);
            float k = Mathf.InverseLerp(min, max, value);
            fill.rectTransform.anchorMax = new Vector2(k, 1);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(k, 0.5f);
            valueText.text = format(value);
            if (notify) onChanged?.Invoke(value);
        }

        void FromPointer(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(track, e.position, e.pressEventCamera, out var local))
            {
                float k = Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, local.x);
                SetValue(Mathf.Lerp(min, max, k), true);
            }
        }

        public void OnPointerDown(PointerEventData e) => FromPointer(e);
        public void OnDrag(PointerEventData e) => FromPointer(e);
    }

    /// <summary>Модальное подтверждение.</summary>
    public static class UIConfirmDialog
    {
        public static void Show(Transform canvas, string messageKey, Action onYes)
        {
            var overlay = UIBuilder.Image(canvas, "ConfirmOverlay", new Color(0, 0, 0, 0.6f), null, true);
            overlay.rectTransform.Stretch();
            var box = UIBuilder.Panel(overlay.transform, "Box", UITheme.Panel);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 230));
            var msg = UIBuilder.LText(box.transform, messageKey, UITheme.FontH2, null, TextAlignmentOptions.Center, UITheme.BoldFont);
            msg.rectTransform.Stretch(30, 26, 30, 90);
            var row = UIBuilder.Node("Buttons", box.transform);
            row.Rt().Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(420, 50));
            UIBuilder.HLayout(row, 16, null, TextAnchor.MiddleCenter).childForceExpandWidth = true;
            UIBuilder.Button(row.transform, "menu.no", UIButton.Style.Secondary, () => UnityEngine.Object.Destroy(overlay.gameObject));
            UIBuilder.Button(row.transform, "menu.yes", UIButton.Style.Primary, () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                onYes?.Invoke();
            });
            overlay.gameObject.AddComponent<UIFadeIn>();
        }
    }

    /// <summary>Плавное появление (fade + сдвиг).</summary>
    public class UIFadeIn : MonoBehaviour
    {
        public float Duration = 0.25f;
        public Vector2 Offset = new(0, -12);
        CanvasGroup group;
        RectTransform rt;
        Vector2 target;
        float t;

        void Awake()
        {
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            rt = transform as RectTransform;
        }

        void OnEnable()
        {
            t = 0;
            target = rt.anchoredPosition;
            group.alpha = 0;
            rt.anchoredPosition = target + Offset;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Duration);
            float k = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = k;
            rt.anchoredPosition = Vector2.Lerp(target + Offset, target, k);
        }
    }

    /// <summary>Вкладки: кнопки слева, страницы справа.</summary>
    public class UITabs : MonoBehaviour
    {
        readonly List<UIButton> buttons = new();
        readonly List<GameObject> pages = new();
        public int Current { get; private set; } = -1;

        public void Add(UIButton button, GameObject page)
        {
            int i = buttons.Count;
            buttons.Add(button);
            pages.Add(page);
            button.OnClick = () => Select(i);
            page.SetActive(false);
        }

        public void Select(int i)
        {
            Current = i;
            for (int k = 0; k < buttons.Count; k++)
            {
                buttons[k].Selected = k == i;
                pages[k].SetActive(k == i);
            }
        }
    }
}

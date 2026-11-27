using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using WG.Core;

namespace WG.UI
{
    /// <summary>Фабрика элементов интерфейса (uGUI + TMP), всё в стиле UITheme.</summary>
    public static class UIBuilder
    {
        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            go.AddComponent<CanvasScaleFromSettings>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Rt(this Component c) => (RectTransform)c.transform;
        public static RectTransform Rt(this GameObject g) => (RectTransform)g.transform;

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Якорь + позиция + размер. anchor/pivot в долях (0..1).</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var go = Node(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }
            return img;
        }

        public static Image Panel(Transform parent, string name, Color? color = null, int radius = 8)
        {
            var img = Image(parent, name, color ?? UITheme.Bg, UITheme.Rounded(radius), true);
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
            Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Left, TMP_FontAsset font = null)
        {
            var go = Node(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font ?? UITheme.BodyFont;
            t.fontSize = size;
            t.color = color ?? UITheme.Text;
            t.alignment = align;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        /// <summary>Локализованный текст: обновляется при смене языка.</summary>
        public static TextMeshProUGUI LText(Transform parent, string key, float size, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Left, TMP_FontAsset font = null)
        {
            var t = Text(parent, key, L.T(key), size, color, align, font);
            t.gameObject.AddComponent<LocalizedText>().Key = key;
            return t;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.UpperLeft, bool expandWidth = true)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(0, 0, 0, 0);
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = expandWidth; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset(0, 0, 0, 0);
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement Size(this GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float minH = -1)
        {
            if (!go.TryGetComponent<LayoutElement>(out var le)) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH; le.flexibleWidth = flexW;
            if (minH >= 0) le.minHeight = minH;
            return le;
        }

        public static UIButton Button(Transform parent, string key, UIButton.Style style, Action onClick, float height = UITheme.ButtonHeight)
        {
            var go = Node("Btn_" + key, parent);
            go.Size(-1, height);
            var b = go.AddComponent<UIButton>();
            b.Build(key, style, onClick);
            return b;
        }

        public static Image Divider(Transform parent, float height = 1f)
        {
            var img = Image(parent, "Divider", UITheme.Divider);
            img.gameObject.Size(-1, height);
            return img;
        }

        /// <summary>Прокручиваемая область. Возвращает Content (с VerticalLayout и ContentSizeFitter).</summary>
        public static RectTransform ScrollArea(Transform parent, string name, out ScrollRect scroll)
        {
            var root = Node(name, parent);
            scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var viewport = Node("Viewport", root.transform);
            viewport.Rt().Stretch();
            viewport.AddComponent<RectMask2D>();
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);
            var content = Node("Content", viewport.transform).Rt();
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1); content.offsetMin = content.offsetMax = Vector2.zero;
            VLayout(content.gameObject, 6, new RectOffset(0, 12, 0, 0));
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.Rt();
            scroll.content = content;
            return content;
        }

        public static TMP_InputField InputField(Transform parent, string initial, int charLimit = 32)
        {
            var root = Node("Input", parent);
            root.Size(-1, UITheme.RowHeight);
            var bg = root.AddComponent<Image>();
            bg.sprite = UITheme.Rounded(6); bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = UITheme.PanelLight;
            var area = Node("TextArea", root.transform).Rt().Stretch(14, 6, 14, 6);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(area, "Placeholder", "", UITheme.FontBody, UITheme.TextMuted);
            ph.Rt().Stretch();
            var txt = Text(area, "Text", "", UITheme.FontBody);
            txt.Rt().Stretch();
            var input = root.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = charLimit;
            input.selectionColor = UITheme.Accent.WithAlpha(0.4f);
            input.caretColor = UITheme.Accent;
            input.text = initial;
            return input;
        }
    }

    /// <summary>Масштаб интерфейса из настроек (CanvasScaler 1920×1080).</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScaleFromSettings : MonoBehaviour
    {
        CanvasScaler scaler;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Apply(default);
            EventBus.Subscribe<SettingsChanged>(Apply);
        }

        void OnDestroy() => EventBus.Unsubscribe<SettingsChanged>(Apply);

        void Apply(SettingsChanged _)
        {
            float s = Mathf.Clamp(SettingsManager.Current.uiScale, 0.75f, 1.5f);
            scaler.referenceResolution = new Vector2(1920f / s, 1080f / s);
        }
    }

    /// <summary>Текст с ключом локализации.</summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        string key;
        object[] args;
        TextMeshProUGUI label;

        public string Key { get => key; set { key = value; args = null; Refresh(default); } }
        public void SetFormat(string k, params object[] a) { key = k; args = a; Refresh(default); }

        void Awake()
        {
            label = GetComponent<TextMeshProUGUI>();
            EventBus.Subscribe<LanguageChanged>(Refresh);
        }

        void OnDestroy() => EventBus.Unsubscribe<LanguageChanged>(Refresh);

        void Refresh(LanguageChanged _)
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            if (label != null && key != null) label.text = args == null ? L.T(key) : L.F(key, args);
        }
    }
}

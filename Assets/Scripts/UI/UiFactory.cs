using System;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The whole HUD is built from code — no prefabs, no binary assets — so the
    /// vertical slice stays reviewable as plain text. These helpers keep
    /// HudController readable.
    /// </summary>
    public static class UiFactory
    {
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = CreateRect(parent, name);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color bg, Color textColor,
            int fontSize, Action onClick)
        {
            var rt = CreatePanel(parent, name, bg);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();

            var colors = button.colors;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;

            var text = CreateText(rt, "Label", label, fontSize, textColor);
            Stretch((RectTransform)text.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Horizontal meter. Returns the fill rect; set fill with SetBarFill.</summary>
        public static RectTransform CreateBar(Transform parent, string name, Color background, Color fill)
        {
            var bgRt = CreatePanel(parent, name, background);
            var fillRt = CreatePanel(bgRt, "Fill", fill);
            Stretch(fillRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return fillRt;
        }

        public static void SetBarFill(RectTransform fill, float normalized)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);
        }
    }
}

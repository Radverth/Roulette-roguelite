using System;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Builders for the pixel-art UI. Everything is laid out in "virtual
    /// pixels" (1 canvas unit = 1 art pixel); the canvas scales that grid by an
    /// integer factor so the pixel art stays crisp. No prefabs, no binary scene
    /// assets — the HUD stays reviewable as code.
    /// </summary>
    public static class UiFactory
    {
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

        // --- Pixel-art widgets ---

        public static Image CreateSpriteImage(Transform parent, string name, string artPath, Vector2 size)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(artPath))
                img.sprite = ArtSprites.Get(artPath);
            img.raycastTarget = false;
            rt.sizeDelta = size;
            return img;
        }

        /// <summary>9-slice panel (panel/panel_danger have 8px borders in their import).</summary>
        public static Image CreateNineSlice(Transform parent, string name, string artPath)
        {
            var rt = CreateRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ArtSprites.Get(artPath);
            img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>
        /// Sprite button. Primary buttons swap idle/pressed/disabled sprites;
        /// secondary has one sprite and tints. Native size is 48x16, scaled by
        /// an integer factor to keep the grid.
        /// </summary>
        public static Button CreatePixelButton(Transform parent, string name, string label, bool primary,
            int scale, Action onClick, out PixelText labelText)
        {
            var rt = CreateRect(parent, name);
            rt.sizeDelta = new Vector2(48 * scale, 16 * scale);

            var img = rt.gameObject.AddComponent<Image>();
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            if (primary)
            {
                img.sprite = ArtSprites.Get("UI/button_primary_idle");
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    pressedSprite = ArtSprites.Get("UI/button_primary_pressed"),
                    selectedSprite = ArtSprites.Get("UI/button_primary_idle"),
                    disabledSprite = ArtSprites.Get("UI/button_primary_disabled")
                };
            }
            else
            {
                img.sprite = ArtSprites.Get("UI/button_secondary_idle");
                var colors = button.colors;
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                button.colors = colors;
            }

            labelText = PixelText.Create(rt, "Label", label, Palette.Bone, scale, PxAlign.Center);
            Place((RectTransform)labelText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), Vector2.zero);

            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Bar with the authored 64x8 track; returns the fill Image (type Filled).</summary>
        public static Image CreatePixelBar(Transform parent, string name, string fillArtPath, Vector2 anchor, Vector2 pos)
        {
            var track = CreateRect(parent, name);
            Place(track, anchor, pos, new Vector2(64f, 8f));
            var trackImg = track.gameObject.AddComponent<Image>();
            trackImg.sprite = ArtSprites.Get("UI/bar_track");
            trackImg.raycastTarget = false;

            var fill = CreateRect(track, "Fill");
            Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = ArtSprites.Get(fillArtPath);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.raycastTarget = false;
            return fillImg;
        }

        /// <summary>Snap fill to whole track pixels so the bar edge stays on the grid.</summary>
        public static void SetPixelBarFill(Image fill, float normalized)
        {
            fill.fillAmount = Mathf.Round(Mathf.Clamp01(normalized) * 64f) / 64f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    public enum PxAlign
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Renders text with the 5x7 bitmap font (UI/font_5x7): 16 columns of 8x8
    /// cells, glyph advance 6px. One pooled Image per glyph; children are only
    /// rebuilt when the string changes. Everything is upper-cased since the
    /// font has no lowercase.
    /// </summary>
    public sealed class PixelText : MonoBehaviour
    {
        private const int CellSize = 8;
        private const int Advance = 6;
        private const int LineHeight = 9;

        private static readonly Dictionary<char, Sprite> Glyphs = new Dictionary<char, Sprite>();
        private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/-.,'!&+:%?";

        private readonly List<Image> _pool = new List<Image>();
        private string _text = "";
        private int _scale = 1;
        private Color _color = Color.white;
        private PxAlign _align = PxAlign.Center;
        private int _wrapWidth; // in virtual px; 0 = no wrapping

        public string Text
        {
            get => _text;
            set
            {
                string v = value ?? "";
                if (_text == v) return;
                _text = v;
                Rebuild();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                foreach (var img in _pool)
                    if (img != null) img.color = value;
            }
        }

        public static PixelText Create(Transform parent, string name, string text, Color color,
            int scale = 1, PxAlign align = PxAlign.Center, int wrapWidth = 0)
        {
            var rt = UiFactory.CreateRect(parent, name);
            var pt = rt.gameObject.AddComponent<PixelText>();
            pt._scale = Mathf.Max(1, scale);
            pt._color = color;
            pt._align = align;
            pt._wrapWidth = wrapWidth;
            pt._text = text ?? "";
            pt.Rebuild();
            return pt;
        }

        private static void EnsureGlyphs()
        {
            if (Glyphs.Count > 0) return;

            Sprite fontSprite = ArtSprites.Get("UI/font_5x7");
            if (fontSprite == null) return;
            Texture2D tex = fontSprite.texture;

            for (int i = 0; i < Charset.Length; i++)
            {
                int col = i % 16;
                int row = i / 16;
                // Charset row 0 is the top strip; texture rects are bottom-origin.
                var rect = new Rect(col * CellSize, tex.height - (row + 1) * CellSize, CellSize, CellSize);
                Glyphs[Charset[i]] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 16f);
            }
        }

        private void Rebuild()
        {
            EnsureGlyphs();

            List<string> lines = WrapLines(_text.ToUpperInvariant());
            int used = 0;
            float totalH = lines.Count * LineHeight * _scale;

            for (int li = 0; li < lines.Count; li++)
            {
                string line = lines[li];
                float lineW = line.Length * Advance * _scale;
                float x0;
                switch (_align)
                {
                    case PxAlign.Left: x0 = 0f; break;
                    case PxAlign.Right: x0 = -lineW; break;
                    default: x0 = -lineW * 0.5f; break;
                }
                float y = (totalH - LineHeight * _scale) * 0.5f - li * LineHeight * _scale;

                for (int ci = 0; ci < line.Length; ci++)
                {
                    char c = line[ci];
                    if (c == ' ' || !Glyphs.TryGetValue(c, out Sprite glyph) || glyph == null)
                        continue;

                    Image img = GetPooled(used++);
                    img.sprite = glyph;
                    img.color = _color;
                    var rt = (RectTransform)img.transform;
                    rt.sizeDelta = new Vector2(CellSize * _scale, CellSize * _scale);
                    rt.anchoredPosition = new Vector2(
                        x0 + ci * Advance * _scale + CellSize * _scale * 0.5f, y);
                    img.gameObject.SetActive(true);
                }
            }

            for (int i = used; i < _pool.Count; i++)
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
        }

        private Image GetPooled(int index)
        {
            while (_pool.Count <= index)
            {
                var rt = UiFactory.CreateRect(transform, "Glyph");
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                _pool.Add(img);
            }
            return _pool[index];
        }

        private List<string> WrapLines(string text)
        {
            var result = new List<string>();
            foreach (string hardLine in text.Split('\n'))
            {
                if (_wrapWidth <= 0 || hardLine.Length * Advance * _scale <= _wrapWidth)
                {
                    result.Add(hardLine);
                    continue;
                }

                int maxChars = Mathf.Max(1, _wrapWidth / (Advance * _scale));
                string current = "";
                foreach (string word in hardLine.Split(' '))
                {
                    if (current.Length == 0)
                        current = word;
                    else if (current.Length + 1 + word.Length <= maxChars)
                        current += " " + word;
                    else
                    {
                        result.Add(current);
                        current = word;
                    }
                }
                if (current.Length > 0) result.Add(current);
            }
            return result;
        }
    }
}

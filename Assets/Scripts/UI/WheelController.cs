using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Renders the wheel as a procedurally generated pie texture and animates
    /// spins. The landing segment is pre-rolled by SpinSystem (weights decide
    /// fairness); the animation just eases onto it, so visuals always match
    /// resolution. Rebuild() supports future sins that warp the wheel itself
    /// (Wrath's extra segments, Lust's shuffles).
    /// </summary>
    public sealed class WheelController
    {
        private readonly GameContext _ctx;
        private readonly RectTransform _wheelRt;
        private readonly List<Text> _labels = new List<Text>();

        private List<SegmentConfig> _segments;
        private Sprite _wheelSprite;
        private float _currentRotation;

        public bool IsSpinning { get; private set; }

        public WheelController(GameContext ctx, RectTransform container, float diameter)
        {
            _ctx = ctx;

            _wheelRt = UiFactory.CreateRect(container, "Wheel");
            UiFactory.Place(_wheelRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(diameter, diameter));
            _wheelRt.gameObject.AddComponent<Image>();

            // Pointer sits above the wheel, apex pointing down at 12 o'clock.
            var pointerRt = UiFactory.CreateRect(container, "Pointer");
            UiFactory.Place(pointerRt, new Vector2(0.5f, 0.5f), new Vector2(0f, diameter * 0.5f + 8f), new Vector2(56f, 56f));
            var pointerImg = pointerRt.gameObject.AddComponent<Image>();
            pointerImg.sprite = BuildPointerSprite();
            pointerImg.color = Palette.Gold;

            Rebuild(ctx.Config.Wheel.segments);
        }

        public void Rebuild(List<SegmentConfig> segments)
        {
            _segments = segments;

            int size = _ctx.Config.Tuning.wheelTextureSize;
            var tex = BuildWheelTexture(segments, size);
            _wheelSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _wheelRt.GetComponent<Image>().sprite = _wheelSprite;

            RebuildLabels(segments);
        }

        /// <summary>Spin to put segment <paramref name="index"/> under the pointer.</summary>
        public void SpinTo(int index, float duration, Action onComplete)
        {
            if (IsSpinning) return;
            IsSpinning = true;
            _ctx.CoroutineHost.StartCoroutine(SpinRoutine(index, duration, onComplete));
        }

        private IEnumerator SpinRoutine(int index, float duration, Action onComplete)
        {
            float arc = 360f / _segments.Count;

            // Wheel rotation z (CCW+) puts segment floor((z % 360) / arc) under
            // the top pointer, so center segment `index` with z ≡ index*arc + arc/2.
            float jitter = ((float)_ctx.Rng.NextDouble() - 0.5f) * arc * 0.6f;
            float desiredMod = index * arc + arc * 0.5f + jitter;

            float start = _currentRotation;
            float baseTarget = start + 720f; // at least two full turns for weight
            float target = baseTarget + Mathf.Repeat(desiredMod - baseTarget, 360f);

            int lastTickSegment = Mathf.FloorToInt(Mathf.Repeat(start, 360f) / arc);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic: fast launch, weighty settle
                float z = Mathf.Lerp(start, target, eased);
                _wheelRt.localRotation = Quaternion.Euler(0f, 0f, z);

                int tickSegment = Mathf.FloorToInt(Mathf.Repeat(z, 360f) / arc);
                if (tickSegment != lastTickSegment)
                {
                    lastTickSegment = tickSegment;
                    Sfx.Tick();
                }
                yield return null;
            }

            _wheelRt.localRotation = Quaternion.Euler(0f, 0f, target);
            _currentRotation = Mathf.Repeat(target, 360f);

            Sfx.Land();
            Haptics.Light();
            IsSpinning = false;
            onComplete?.Invoke();
        }

        // --- Procedural art ---

        private Texture2D BuildWheelTexture(List<SegmentConfig> segments, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            float center = size * 0.5f;
            float radius = center - 2f;
            float rimWidth = size * 0.02f;
            float hubRadius = size * 0.10f;
            float arc = 360f / segments.Count;

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 rim = Palette.Gold;
            Color32 hub = Palette.Night;
            Color32 line = Palette.Night;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    int i = y * size + x;
                    if (dist > radius)
                    {
                        pixels[i] = clear;
                        continue;
                    }
                    if (dist > radius - rimWidth)
                    {
                        pixels[i] = rim;
                        continue;
                    }
                    if (dist < hubRadius)
                    {
                        pixels[i] = dist > hubRadius - rimWidth * 0.6f ? rim : hub;
                        continue;
                    }

                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;      // -180..180, 0 = +x
                    float rel = Mathf.Repeat(90f - angle, 360f);            // 0 at top, clockwise
                    int segIndex = Mathf.Min((int)(rel / arc), segments.Count - 1);

                    float within = rel - segIndex * arc;
                    float edge = Mathf.Min(within, arc - within) * dist * Mathf.Deg2Rad;
                    pixels[i] = edge < 1.5f ? line : (Color32)segments[segIndex].ParsedColor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private void RebuildLabels(List<SegmentConfig> segments)
        {
            foreach (var label in _labels)
                if (label != null) UnityEngine.Object.Destroy(label.gameObject);
            _labels.Clear();

            float arc = 360f / segments.Count;
            float radius = _wheelRt.sizeDelta.x * 0.36f;

            for (int i = 0; i < segments.Count; i++)
            {
                // Texture-space angle of this segment's center; labels are
                // parented to the wheel so they rotate with it.
                float centerAngle = 90f - (i * arc + arc * 0.5f);
                float rad = centerAngle * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

                var text = UiFactory.CreateText(_wheelRt, $"Label_{i}", segments[i].label, 30, Palette.Night);
                var rt = (RectTransform)text.transform;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(200f, 60f);
                rt.localRotation = Quaternion.Euler(0f, 0f, centerAngle - 90f);
                _labels.Add(text);
            }
        }

        private static Sprite BuildPointerSprite()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var pixels = new Color32[s * s];
            Color32 solid = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < s; y++)
            {
                // Apex at the bottom, widening toward the top.
                float rowHalfWidth = (y / (float)s) * (s * 0.5f);
                for (int x = 0; x < s; x++)
                    pixels[y * s + x] = Mathf.Abs(x - s * 0.5f) <= rowHalfWidth ? solid : clear;
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

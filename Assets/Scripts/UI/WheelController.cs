using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The authored pixel wheel, layered per the art spec:
    ///   glow → disc (rotates, carries wedge icons) → rim (static) →
    ///   segment flash (static, drawn at 12 o'clock) → hub → pointer.
    /// Wedge order is authored (risk at indices 1/4/7/10 so no two are
    /// adjacent) — wheel.json must list segments in the same order; see
    /// Art/Wheel/segment_layout.txt.
    /// The landing segment is pre-rolled by SpinSystem; the animation eases
    /// the disc onto it, so visuals always match resolution.
    /// </summary>
    public sealed class WheelController
    {
        private const float Arc = 30f;         // 12 wedges
        private const float IconRadius = 42f;  // wedge icon centers, in art px

        private readonly GameContext _ctx;
        private readonly RectTransform _container;
        private readonly RectTransform _disc;
        private readonly Image _flash;
        private readonly List<Image> _icons = new List<Image>();

        private float _currentRotation;
        private Coroutine _flashRoutine;

        public bool IsSpinning { get; private set; }

        public WheelController(GameContext ctx, RectTransform container)
        {
            _ctx = ctx;
            _container = container;

            var glow = UiFactory.CreateSpriteImage(container, "Glow", "Wheel/wheel_glow", new Vector2(128f, 128f));
            glow.color = new Color(1f, 1f, 1f, 0.55f);
            UiFactory.Place((RectTransform)glow.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));

            var discImg = UiFactory.CreateSpriteImage(container, "Disc", "Wheel/wheel_disc", new Vector2(128f, 128f));
            _disc = (RectTransform)discImg.transform;
            UiFactory.Place(_disc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));

            var rim = UiFactory.CreateSpriteImage(container, "Rim", "Wheel/wheel_rim", new Vector2(128f, 128f));
            UiFactory.Place((RectTransform)rim.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));

            _flash = UiFactory.CreateSpriteImage(container, "Flash", "Wheel/wheel_segment_flash", new Vector2(128f, 128f));
            UiFactory.Place((RectTransform)_flash.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));
            _flash.gameObject.SetActive(false);

            var hub = UiFactory.CreateSpriteImage(container, "Hub", "Wheel/wheel_hub", new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)hub.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));

            var pointer = UiFactory.CreateSpriteImage(container, "Pointer", "Wheel/wheel_pointer", new Vector2(16f, 24f));
            UiFactory.Place((RectTransform)pointer.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(16f, 24f));

            Rebuild(ctx.Config.Wheel.segments);
        }

        /// <summary>Re-lay the wedge icons (Lust's shuffles / Wrath's inserts will call this).</summary>
        public void Rebuild(List<SegmentConfig> segments)
        {
            foreach (var icon in _icons)
                if (icon != null) UnityEngine.Object.Destroy(icon.gameObject);
            _icons.Clear();

            float arc = 360f / segments.Count;
            for (int i = 0; i < segments.Count; i++)
            {
                // Wedge i is centered i*arc clockwise from 12 o'clock; icons are
                // children of the disc so they carry its rotation.
                float angle = (90f - i * arc) * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Cos(angle) * IconRadius, Mathf.Sin(angle) * IconRadius);

                var img = UiFactory.CreateSpriteImage(_disc, $"Icon_{i}", null, new Vector2(16f, 16f));
                img.sprite = ArtSprites.IconForSegment(segments[i]);
                UiFactory.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(16f, 16f));
                _icons.Add(img);
            }
        }

        /// <summary>Spin so wedge <paramref name="index"/> lands under the pointer.</summary>
        public void SpinTo(int index, float duration, Action onComplete)
        {
            if (IsSpinning) return;
            IsSpinning = true;
            if (_flashRoutine != null) _ctx.CoroutineHost.StopCoroutine(_flashRoutine);
            _flash.gameObject.SetActive(false);
            _ctx.CoroutineHost.StartCoroutine(SpinRoutine(index, duration, onComplete));
        }

        private IEnumerator SpinRoutine(int index, float duration, Action onComplete)
        {
            // Disc rotation z (CCW+) brings wedge k (authored k*30° clockwise
            // from top) under the pointer when z ≡ k*30.
            float jitter = ((float)_ctx.Rng.NextDouble() - 0.5f) * Arc * 0.5f;
            float desiredMod = index * Arc + jitter;

            float start = _currentRotation;
            float baseTarget = start + 720f;
            float target = baseTarget + Mathf.Repeat(desiredMod - baseTarget, 360f);

            int lastTick = Mathf.FloorToInt((Mathf.Repeat(start, 360f) + Arc * 0.5f) / Arc);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // fast launch, weighty settle
                float z = Mathf.Lerp(start, target, eased);
                _disc.localRotation = Quaternion.Euler(0f, 0f, z);

                int tick = Mathf.FloorToInt((Mathf.Repeat(z, 360f) + Arc * 0.5f) / Arc);
                if (tick != lastTick)
                {
                    lastTick = tick;
                    Sfx.Tick();
                }
                yield return null;
            }

            _disc.localRotation = Quaternion.Euler(0f, 0f, target);
            _currentRotation = Mathf.Repeat(target, 360f);

            Sfx.Land();
            Haptics.Light();
            _flashRoutine = _ctx.CoroutineHost.StartCoroutine(FlashRoutine());
            SpawnRingShock();

            IsSpinning = false;
            onComplete?.Invoke();
        }

        private IEnumerator FlashRoutine()
        {
            // The flash sprite is authored at 12 o'clock — exactly the landing wedge.
            for (int i = 0; i < 3; i++)
            {
                _flash.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.09f);
                _flash.gameObject.SetActive(false);
                yield return new WaitForSeconds(0.06f);
            }
            _flashRoutine = null;
        }

        private void SpawnRingShock()
        {
            var img = UiFactory.CreateSpriteImage(_container, "RingShock", "Particles/ring_shock", new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(32f, 32f));
            _ctx.CoroutineHost.StartCoroutine(RingShockRoutine(img));
        }

        private IEnumerator RingShockRoutine(Image img)
        {
            const float life = 0.32f;
            float elapsed = 0f;
            var rt = (RectTransform)img.transform;

            while (elapsed < life)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / life);
                rt.localScale = Vector3.one * (0.6f + 1.9f * t);
                img.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }
            UnityEngine.Object.Destroy(img.gameObject);
        }
    }
}

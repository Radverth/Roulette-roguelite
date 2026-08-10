using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The wheel, layered per the art spec: glow → disc (rotates, carries wedge
    /// icons) → rim (static) → landing flash → hub → pointer.
    ///
    /// The Forge makes the ring a deck, so wedge count changes between and
    /// during runs; the disc and flash are rasterised at runtime to match
    /// (see WheelDiscRenderer) whenever the ring version moves.
    ///
    /// The landing wedge is pre-rolled by SpinSystem — the animation only
    /// decides where inside that wedge the ticker comes to rest, which is where
    /// the near-miss bias lives.
    /// </summary>
    public sealed class WheelController
    {
        private const float IconRadius = 42f;
        private const float GhostRadius = 44f;

        private readonly GameContext _ctx;
        private readonly RectTransform _container;
        private readonly RectTransform _disc;
        private readonly Image _discImage;
        private readonly Image _flash;
        private Image _ghostLeft;
        private Image _ghostRight;
        private readonly List<Image> _icons = new List<Image>();

        private int _ringVersion = -1;
        private int _wedgeCount = 12;
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

            _discImage = UiFactory.CreateSpriteImage(container, "Disc", null, new Vector2(128f, 128f));
            _disc = (RectTransform)_discImage.transform;
            UiFactory.Place(_disc, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));

            var rim = UiFactory.CreateSpriteImage(container, "Rim", "Wheel/wheel_rim", new Vector2(128f, 128f));
            UiFactory.Place((RectTransform)rim.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));

            _flash = UiFactory.CreateSpriteImage(container, "Flash", null, new Vector2(128f, 128f));
            UiFactory.Place((RectTransform)_flash.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));
            _flash.gameObject.SetActive(false);

            var hub = UiFactory.CreateSpriteImage(container, "Hub", "Wheel/wheel_hub", new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)hub.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));

            var pointer = UiFactory.CreateSpriteImage(container, "Pointer", "Wheel/wheel_pointer", new Vector2(16f, 24f));
            UiFactory.Place((RectTransform)pointer.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(16f, 24f));

            SyncToRing();
        }

        public float Arc => 360f / Mathf.Max(1, _wedgeCount);

        /// <summary>Redraw if the ring changed shape since the last look. Cheap when it has not.</summary>
        public void SyncToRing()
        {
            if (_ctx.Ring.Version == _ringVersion) return;
            _ringVersion = _ctx.Ring.Version;

            var ring = _ctx.Ring.Effective;
            if (ring.Count == 0) return;

            _wedgeCount = ring.Count;

            // These are generated, not imported: release the previous pair or a
            // long session leaks a 64KB texture per ring change.
            DestroyGenerated(_discImage.sprite);
            DestroyGenerated(_flash.sprite);

            _discImage.sprite = WheelDiscRenderer.BuildDisc(new List<SegmentConfig>(ring));
            _flash.sprite = WheelDiscRenderer.BuildFlash(_wedgeCount);
            RebuildIcons(ring);
        }

        private static void DestroyGenerated(Sprite sprite)
        {
            if (sprite == null) return;
            Texture2D tex = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (tex != null) UnityEngine.Object.Destroy(tex);
        }

        private void RebuildIcons(IReadOnlyList<SegmentConfig> ring)
        {
            foreach (var icon in _icons)
                if (icon != null) UnityEngine.Object.Destroy(icon.gameObject);
            _icons.Clear();

            float arc = Arc;
            // A crowded ring needs smaller icons or they collide at the rim.
            float size = ring.Count > 16 ? 8f : (ring.Count > 13 ? 12f : 16f);

            for (int i = 0; i < ring.Count; i++)
            {
                float angle = (90f - i * arc) * Mathf.Deg2Rad;
                var pos = new Vector2(Mathf.Cos(angle) * IconRadius, Mathf.Sin(angle) * IconRadius);

                var img = UiFactory.CreateSpriteImage(_disc, $"Icon_{i}", null, new Vector2(size, size));
                img.sprite = ArtSprites.IconForSegment(ring[i]);
                UiFactory.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
                _icons.Add(img);
            }
        }

        /// <summary>
        /// Push the settled wheel to a neighbouring wedge. Snaps rather than
        /// eases: the spin is over, this is the player's hand on the rim.
        /// </summary>
        public void NudgeTo(int index)
        {
            float arc = Arc;
            float target = index * arc;
            _currentRotation = Mathf.Repeat(target, 360f);
            _disc.localRotation = Quaternion.Euler(0f, 0f, _currentRotation);
            Sfx.Tick();
        }

        /// <summary>
        /// Outline the two wedges a push would bring under the pointer. Never
        /// filled — a ghost must not be mistakable for a settled result. They
        /// live on the container rather than the disc, so they mark screen
        /// positions and stay put while the disc turns beneath them.
        /// </summary>
        public void ShowNudgeGhosts(bool show)
        {
            if (_ghostLeft == null)
            {
                _ghostLeft = CreateGhost("GhostLeft");
                _ghostRight = CreateGhost("GhostRight");
            }

            if (show)
            {
                float arc = Arc;
                PlaceGhost(_ghostLeft, 90f + arc);
                PlaceGhost(_ghostRight, 90f - arc);
            }

            _ghostLeft.gameObject.SetActive(show);
            _ghostRight.gameObject.SetActive(show);
        }

        private Image CreateGhost(string name)
        {
            var ghost = UiFactory.CreateSpriteImage(_container, name, "Pledges/nudge_ghost", new Vector2(24f, 32f));
            ghost.raycastTarget = false;
            ghost.gameObject.SetActive(false);
            return ghost;
        }

        private static void PlaceGhost(Image ghost, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rad) * GhostRadius, Mathf.Sin(rad) * GhostRadius);
            var rt = (RectTransform)ghost.transform;
            UiFactory.Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(24f, 32f));
            // Stands up along the spoke, so it reads as a wedge and not a badge.
            rt.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
        }

        /// <summary>Blind Wager: the wheel is hidden while it turns.</summary>
        public void SetHidden(bool hidden)
        {
            float alpha = hidden ? 0f : 1f;
            _discImage.color = new Color(1f, 1f, 1f, alpha);
            foreach (var icon in _icons)
                if (icon != null) icon.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>Spin so wedge <paramref name="index"/> lands under the pointer.</summary>
        public void SpinTo(int index, float duration, Action onComplete)
        {
            if (IsSpinning) return;
            IsSpinning = true;
            if (_flashRoutine != null) _ctx.CoroutineHost.StopCoroutine(_flashRoutine);
            _flash.gameObject.SetActive(false);
            if (_ctx.Pledges.HidesWheel) SetHidden(true);
            _ctx.CoroutineHost.StartCoroutine(SpinRoutine(index, duration, onComplete));
        }

        private IEnumerator SpinRoutine(int index, float duration, Action onComplete)
        {
            float arc = Arc;
            float desiredMod = index * arc + NearMissJitter(index, arc);

            float start = _currentRotation;
            float baseTarget = start + 720f; // at least two full turns for weight
            float target = baseTarget + Mathf.Repeat(desiredMod - baseTarget, 360f);

            int lastTick = Mathf.FloorToInt((Mathf.Repeat(start, 360f) + arc * 0.5f) / arc);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // fast launch, weighty settle
                float z = Mathf.Lerp(start, target, eased);
                _disc.localRotation = Quaternion.Euler(0f, 0f, z);

                int tick = Mathf.FloorToInt((Mathf.Repeat(z, 360f) + arc * 0.5f) / arc);
                if (tick != lastTick)
                {
                    lastTick = tick;
                    Sfx.Tick();
                }
                yield return null;
            }

            _disc.localRotation = Quaternion.Euler(0f, 0f, target);
            _currentRotation = Mathf.Repeat(target, 360f);

            SetHidden(false);
            Sfx.Land();
            Haptics.Light();
            _flashRoutine = _ctx.CoroutineHost.StartCoroutine(FlashRoutine());
            SpawnRingShock();

            IsSpinning = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Where inside the winning wedge the ticker settles. Biased toward the
        /// seam it shares with its richest neighbour, so the wheel frequently
        /// comes to rest *just* past a jackpot. This changes nothing about the
        /// odds — the outcome was chosen before the spin — only how it feels.
        /// </summary>
        private float NearMissJitter(int index, float arc)
        {
            var ring = _ctx.Ring.Effective;
            if (ring.Count < 2) return 0f;

            float half = arc * 0.45f; // stay inside the wedge
            int prev = (index - 1 + ring.Count) % ring.Count;
            int next = (index + 1) % ring.Count;

            // Rotation grows toward higher indices, so a positive offset parks
            // the ticker on the seam shared with the next wedge.
            int side = WedgeValue(ring[next]) >= WedgeValue(ring[prev]) ? 1 : -1;

            if (_ctx.Rng.NextDouble() < _ctx.Config.Tuning.nearMissChance)
                return side * Mathf.Lerp(0.55f, 0.92f, (float)_ctx.Rng.NextDouble()) * half;

            return ((float)_ctx.Rng.NextDouble() - 0.5f) * half;
        }

        private static float WedgeValue(SegmentConfig seg)
        {
            if (seg.IsRisk) return -1f;
            switch (seg.ParsedType)
            {
                case SegmentType.Coins: return seg.EffectiveAmount;
                case SegmentType.Xp: return seg.EffectiveAmount * 0.5f;
                default: return 5f;
            }
        }

        private IEnumerator FlashRoutine()
        {
            // The flash sprite is drawn at 12 o'clock — exactly the landing wedge.
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

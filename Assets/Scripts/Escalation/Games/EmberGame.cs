using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Ember (Wrath) — timing. A needle sweeps the track; stop it inside the
    /// band. The band narrows as the tables deepen. Miss and it burns you.
    /// </summary>
    public sealed class EmberGame : InterludeGame
    {
        private const float TrackWidth = 96f;

        private RectTransform _needle;
        private RectTransform _band;
        private float _position;   // 0..1 across the track
        private float _direction = 1f;
        private float _bandCentre;
        private float _bandHalfWidth;
        private bool _stopped;

        public override string Instruction => "STOP IT";

        protected override void Build()
        {
            // 36% of the track at Table I, down to 12% at the last.
            _bandHalfWidth = Mathf.Lerp(0.18f, 0.06f, Difficulty);
            _bandCentre = Mathf.Lerp(0.3f, 0.7f, (float)Ctx.Rng.NextDouble());

            Sprite("Escalation/timing_track", new Vector2(TrackWidth, 16f), Vector2.zero);

            var band = UiFactory.CreateRect(Root, "Band");
            var bandImg = band.gameObject.AddComponent<Image>();
            bandImg.color = Palette.Blood;
            bandImg.raycastTarget = false;
            _band = band;
            UiFactory.Place(_band, new Vector2(0.5f, 0.5f),
                new Vector2((_bandCentre - 0.5f) * TrackWidth, 0f),
                new Vector2(Mathf.Round(_bandHalfWidth * 2f * TrackWidth), 10f));

            var needle = Sprite("Escalation/timing_needle", new Vector2(6f, 20f), Vector2.zero);
            _needle = (RectTransform)needle.transform;
        }

        public override void Tick(float dt)
        {
            if (_stopped) return;

            // Faster sweeps deeper in, so the same band is a harder ask.
            float speed = Mathf.Lerp(0.85f, 1.8f, Difficulty);
            _position += _direction * speed * dt;
            if (_position >= 1f) { _position = 1f; _direction = -1f; }
            else if (_position <= 0f) { _position = 0f; _direction = 1f; }

            _needle.anchoredPosition = new Vector2(
                Mathf.Round((_position - 0.5f) * TrackWidth), 0f);
        }

        public override void OnPressed()
        {
            if (_stopped) return;
            _stopped = true;

            float offset = Mathf.Abs(_position - _bandCentre);
            if (offset > _bandHalfWidth)
            {
                Sfx.Damage();
                Finish(InterludeResult.Fail, 0f);
                return;
            }

            // Dead centre pays double what the edge does.
            float score = 1f - Mathf.Clamp01(offset / Mathf.Max(0.0001f, _bandHalfWidth));
            Sfx.Reward();
            Finish(score > 0.6f ? InterludeResult.Success : InterludeResult.Partial, score);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Vigil (Sloth) — hold. The ring fills while you hold; let go inside
    /// the window near the top. Hold too long and it laps and you have lost it.
    /// The window narrows as you descend.
    /// </summary>
    public sealed class VigilGame : InterludeGame
    {
        private Image _ring;
        private float _charge;      // 0..1, past 1 it has lapped
        private float _windowStart;
        private bool _holding;
        private bool _done;

        public override string Instruction => "HOLD IT";

        protected override void Build()
        {
            _windowStart = Mathf.Lerp(0.74f, 0.88f, Difficulty);
            _ring = Sprite("Escalation/vigil_ring_0", new Vector2(32f, 32f), Vector2.zero);
        }

        public override void OnPressed() => _holding = true;

        public override void OnReleased()
        {
            if (_done) return;
            _holding = false;

            if (_charge >= _windowStart && _charge <= 1f)
            {
                _done = true;
                Sfx.Reward();
                Finish(InterludeResult.Success, 1f);
                return;
            }

            _done = true;
            Sfx.Damage();
            Finish(InterludeResult.Fail, _charge);
        }

        public override void Tick(float dt)
        {
            if (_done) return;

            if (_holding)
            {
                _charge += dt * 0.42f;
                if (_charge > 1.12f)
                {
                    // Lapped: Sloth wins by simply outlasting you.
                    _done = true;
                    Sfx.Damage();
                    Finish(InterludeResult.Fail, 0f);
                    return;
                }
            }

            string sprite = _charge >= _windowStart
                ? "Escalation/vigil_ring_100"
                : (_charge >= 0.4f ? "Escalation/vigil_ring_50" : "Escalation/vigil_ring_0");
            _ring.sprite = ArtSprites.Get(sprite);
        }

        public override void OnTimeout()
        {
            if (_done) return;
            _done = true;
            Finish(InterludeResult.Fail, 0f);
        }
    }
}

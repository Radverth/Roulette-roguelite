using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Feast (Gluttony) — push your luck. Hold to keep taking; the meter
    /// climbs and so does the payout, but past the safe mark it can burst at any
    /// moment. Release to walk away with what you have. The safe stretch shrinks
    /// as you descend, which is the whole joke of the sin.
    /// </summary>
    public sealed class FeastGame : InterludeGame
    {
        private const float MeterWidth = 72f;

        private Image _fill;
        private float _amount;      // 0..1 of the meter
        private float _safeLimit;
        private float _burstChancePerSecond;
        private bool _holding;
        private bool _done;

        public override string Instruction => "TAKE MORE";

        protected override void Build()
        {
            _safeLimit = Mathf.Lerp(0.62f, 0.34f, Difficulty);
            _burstChancePerSecond = Mathf.Lerp(0.9f, 2.2f, Difficulty);

            Sprite("Escalation/feast_meter", new Vector2(MeterWidth, 14f), Vector2.zero);

            var fill = UiFactory.CreateRect(Root, "Fill");
            UiFactory.Place(fill, new Vector2(0.5f, 0.5f),
                new Vector2(-MeterWidth * 0.5f, 0f), new Vector2(0f, 8f));
            fill.pivot = new Vector2(0f, 0.5f);
            _fill = fill.gameObject.AddComponent<Image>();
            _fill.color = Palette.Gold;
            _fill.raycastTarget = false;

            // A single pip marking where safety ends. After that it is nerve.
            var mark = UiFactory.CreateRect(Root, "SafeMark");
            UiFactory.Place(mark, new Vector2(0.5f, 0.5f),
                new Vector2(Mathf.Round((_safeLimit - 0.5f) * MeterWidth), 0f), new Vector2(1f, 14f));
            var markImg = mark.gameObject.AddComponent<Image>();
            markImg.color = Palette.Bone;
            markImg.raycastTarget = false;
        }

        public override void OnPressed() => _holding = true;

        public override void OnReleased()
        {
            _holding = false;
            if (_done || _amount <= 0f) return;
            Stop();
        }

        public override void Tick(float dt)
        {
            if (_done || !_holding) return;

            _amount = Mathf.Min(1f, _amount + dt * 0.34f);
            _fill.rectTransform.sizeDelta = new Vector2(Mathf.Round(_amount * MeterWidth), 8f);
            _fill.color = _amount > _safeLimit ? Palette.Blood : Palette.Gold;

            if (_amount >= 1f)
            {
                Burst();
                return;
            }

            // Past the mark, every moment is a roll.
            if (_amount > _safeLimit && Ctx.Rng.NextDouble() < _burstChancePerSecond * dt)
                Burst();
        }

        private void Stop()
        {
            _done = true;
            Sfx.Reward();
            Finish(_amount > _safeLimit ? InterludeResult.Success : InterludeResult.Partial, _amount);
        }

        private void Burst()
        {
            _done = true;
            Sfx.Damage();
            Finish(InterludeResult.Fail, 0f);
        }

        public override void OnTimeout()
        {
            if (_done) return;
            if (_amount > 0f) Stop();
            else Finish(InterludeResult.Fail, 0f);
        }
    }
}

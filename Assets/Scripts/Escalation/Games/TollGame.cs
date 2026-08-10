using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Toll (Greed) — rhythm. Five beats at a fixed tempo; tap on each one.
    /// The tempo quickens with depth. Payout scales with accuracy, so a sloppy
    /// run still pays something.
    /// </summary>
    public sealed class TollGame : InterludeGame
    {
        private const int Beats = 5;

        private readonly List<Image> _pips = new List<Image>();
        private float _interval;
        private float _window;
        private float _elapsed;
        private int _nextBeat;
        private float _accuracyTotal;
        private int _hits;

        public override string Instruction => "KEEP TIME";

        protected override void Build()
        {
            _interval = Mathf.Lerp(1.1f, 0.62f, Difficulty);
            _window = Mathf.Lerp(0.22f, 0.12f, Difficulty);

            for (int i = 0; i < Beats; i++)
            {
                var pip = Sprite("Escalation/toll_beat_pending", new Vector2(12f, 12f),
                    new Vector2(-24f + i * 12f, 0f));
                _pips.Add(pip);
            }
        }

        public override void Tick(float dt)
        {
            _elapsed += dt;

            // A beat sails past unanswered: mark it missed and move on.
            while (_nextBeat < Beats && _elapsed > BeatTime(_nextBeat) + _window)
            {
                SetSprite(_pips[_nextBeat], "Escalation/toll_beat_miss");
                _nextBeat++;
                if (_nextBeat >= Beats) Score();
            }
        }

        public override void OnPressed()
        {
            if (_nextBeat >= Beats) return;

            float error = Mathf.Abs(_elapsed - BeatTime(_nextBeat));
            if (error > _window)
            {
                // Early taps are misses too, otherwise mashing wins.
                SetSprite(_pips[_nextBeat], "Escalation/toll_beat_miss");
            }
            else
            {
                float accuracy = 1f - error / _window;
                _accuracyTotal += accuracy;
                _hits++;
                SetSprite(_pips[_nextBeat], accuracy > 0.65f
                    ? "Escalation/toll_beat_perfect" : "Escalation/toll_beat_hit");
                Sfx.Tick();
            }

            _nextBeat++;
            if (_nextBeat >= Beats) Score();
        }

        private float BeatTime(int index) => _interval * (index + 1);

        private void Score()
        {
            if (_hits == 0)
            {
                Finish(InterludeResult.Fail, 0f);
                return;
            }
            float score = _accuracyTotal / Beats;
            Sfx.Reward();
            Finish(_hits == Beats && score > 0.6f ? InterludeResult.Success : InterludeResult.Partial, score);
        }

        public override void OnTimeout() => Score();
    }
}

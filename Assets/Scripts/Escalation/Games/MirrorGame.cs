using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Mirror (Pride) — memory. A pattern lights across a 3x3 grid, then you
    /// reproduce it in order. Longer patterns and shorter looks as you descend.
    /// </summary>
    public sealed class MirrorGame : InterludeGame
    {
        private const int Cells = 9;

        private readonly List<Image> _cells = new List<Image>();
        private readonly List<int> _pattern = new List<int>();
        private int _inputIndex;
        private int _showIndex = -1;
        private float _timer;
        private bool _showing = true;
        private float _litDuration;

        public override string Instruction => "REPEAT IT";

        protected override void Build()
        {
            int length = 3 + Mathf.RoundToInt(Difficulty * 2f); // 3 at Table I, 5 deep
            _litDuration = Mathf.Lerp(0.55f, 0.34f, Difficulty);

            for (int i = 0; i < Cells; i++)
            {
                int index = i;
                var button = SpriteButton("Escalation/memory_cell_idle", new Vector2(16f, 16f),
                    new Vector2(-20f + (i % 3) * 20f, 20f - (i / 3) * 20f),
                    () => OnCellTapped(index));
                _cells.Add(button.GetComponent<Image>());
            }

            // No immediate repeats in the pattern: it reads as a sequence, not a stutter.
            int last = -1;
            for (int i = 0; i < length; i++)
            {
                int pick;
                do { pick = Ctx.Rng.Next(Cells); } while (pick == last && Cells > 1);
                _pattern.Add(pick);
                last = pick;
            }
        }

        public override void Tick(float dt)
        {
            if (!_showing) return;

            _timer -= dt;
            if (_timer > 0f) return;

            if (_showIndex >= 0 && _showIndex < _pattern.Count)
                SetSprite(_cells[_pattern[_showIndex]], "Escalation/memory_cell_idle");

            _showIndex++;
            if (_showIndex >= _pattern.Count)
            {
                _showing = false;
                return;
            }

            SetSprite(_cells[_pattern[_showIndex]], "Escalation/memory_cell_lit");
            Sfx.Tick();
            _timer = _litDuration;
        }

        private void OnCellTapped(int index)
        {
            if (_showing) return;

            if (_pattern[_inputIndex] == index)
            {
                SetSprite(_cells[index], "Escalation/memory_cell_correct");
                _inputIndex++;
                Sfx.Tick();
                if (_inputIndex >= _pattern.Count)
                {
                    Sfx.Reward();
                    Finish(InterludeResult.Success, 1f);
                }
                return;
            }

            SetSprite(_cells[index], "Escalation/memory_cell_wrong");
            Sfx.Damage();
            Finish(InterludeResult.Fail, _inputIndex / (float)_pattern.Count);
        }

        public override void OnTimeout()
        {
            Finish(InterludeResult.Fail, _inputIndex / (float)Mathf.Max(1, _pattern.Count));
        }
    }
}

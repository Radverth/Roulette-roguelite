using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Shell (Lust) — tracking. Three cups, one holds the coin, they trade
    /// places. Pick the right one. More swaps and faster ones deeper down.
    /// </summary>
    public sealed class ShellGame : InterludeGame
    {
        private const int Cups = 3;
        private static readonly float[] Slots = { -28f, 0f, 28f };

        private readonly List<RectTransform> _cups = new List<RectTransform>();
        private readonly List<Image> _images = new List<Image>();
        private int[] _slotOfCup;   // cup index -> slot index
        private int _coinCup;
        private int _swapsLeft;
        private float _swapTimer;
        private float _swapInterval;
        private bool _revealing = true;
        private float _revealTimer = 0.8f;
        private bool _picked;

        public override string Instruction => "FOLLOW IT";

        protected override void Build()
        {
            _swapsLeft = 4 + Mathf.RoundToInt(Difficulty * 5f);
            _swapInterval = Mathf.Lerp(0.42f, 0.2f, Difficulty);
            _coinCup = Ctx.Rng.Next(Cups);
            _slotOfCup = new int[Cups];

            for (int i = 0; i < Cups; i++)
            {
                int index = i;
                _slotOfCup[i] = i;
                var button = SpriteButton(
                    i == _coinCup ? "Escalation/shell_cup_marked" : "Escalation/shell_cup_down",
                    new Vector2(24f, 24f), new Vector2(Slots[i], 0f), () => OnCupTapped(index));
                _cups.Add((RectTransform)button.transform);
                _images.Add(button.GetComponent<Image>());
            }
        }

        public override void Tick(float dt)
        {
            // A beat to show where it starts, then the cups go down and move.
            if (_revealing)
            {
                _revealTimer -= dt;
                if (_revealTimer > 0f) return;
                _revealing = false;
                for (int i = 0; i < Cups; i++)
                    SetSprite(_images[i], "Escalation/shell_cup_down");
                return;
            }

            if (_swapsLeft <= 0) return;

            _swapTimer -= dt;
            if (_swapTimer > 0f) return;
            _swapTimer = _swapInterval;
            _swapsLeft--;

            int a = Ctx.Rng.Next(Cups);
            int b = (a + 1 + Ctx.Rng.Next(Cups - 1)) % Cups;
            (_slotOfCup[a], _slotOfCup[b]) = (_slotOfCup[b], _slotOfCup[a]);

            _cups[a].anchoredPosition = new Vector2(Slots[_slotOfCup[a]], 0f);
            _cups[b].anchoredPosition = new Vector2(Slots[_slotOfCup[b]], 0f);
            Sfx.Tick();
        }

        private void OnCupTapped(int cup)
        {
            if (_revealing || _picked) return;
            _picked = true;

            SetSprite(_images[cup], "Escalation/shell_cup_lifted");
            bool right = cup == _coinCup;
            if (!right) SetSprite(_images[_coinCup], "Escalation/shell_cup_marked");

            if (right) Sfx.Reward(); else Sfx.Damage();
            Finish(right ? InterludeResult.Success : InterludeResult.Fail, right ? 1f : 0f);
        }
    }
}

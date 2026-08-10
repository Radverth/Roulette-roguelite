using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Understudy (Envy) — compare. Two frames, near enough identical; one
    /// carries the flaw. Find it. Deeper down the flaw is fainter and there is
    /// less time to look.
    /// </summary>
    public sealed class UnderstudyGame : InterludeGame
    {
        private readonly List<Image> _marks = new List<Image>();
        private int _flawed;
        private bool _picked;

        public override string Instruction => "FIND THE FLAW";

        protected override void Build()
        {
            _flawed = Ctx.Rng.Next(2);

            for (int i = 0; i < 2; i++)
            {
                int index = i;
                SpriteButton("Escalation/diff_frame", new Vector2(40f, 40f),
                    new Vector2(-26f + i * 52f, 0f), () => OnPicked(index));
            }

            // The flaw itself: a mark laid over one frame, fainter as the tables
            // deepen, so the same art carries the whole difficulty curve.
            var flaw = UiFactory.CreateSpriteImage(Root, "Flaw", "Escalation/diff_frame_marked",
                new Vector2(40f, 40f));
            UiFactory.Place((RectTransform)flaw.transform, new Vector2(0.5f, 0.5f),
                new Vector2(-26f + _flawed * 52f, 0f), new Vector2(40f, 40f));
            flaw.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0.4f, Difficulty));
            _marks.Add(flaw);
        }

        private void OnPicked(int index)
        {
            if (_picked) return;
            _picked = true;

            bool right = index == _flawed;
            foreach (var mark in _marks) mark.color = Color.white;

            if (right) Sfx.Reward(); else Sfx.Damage();
            Finish(right ? InterludeResult.Success : InterludeResult.Fail, right ? 1f : 0f);
        }
    }
}

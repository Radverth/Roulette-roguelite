using System;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// One mini-game. Built into a container the host owns, ticked by the host,
    /// and finished exactly once. Constraints from the design doc: ten seconds
    /// maximum, one thumb, no tutorial text, and failure never ends the run.
    /// </summary>
    public abstract class InterludeGame
    {
        protected GameContext Ctx;
        protected RectTransform Root;
        /// <summary>0 at Table I, 1 at the last table. Bands narrow, patterns lengthen.</summary>
        protected float Difficulty;

        private Action<InterludeResult, float> _finished;
        private bool _done;

        /// <summary>Two words, shown by the host. If it needs more, the game is wrong.</summary>
        public abstract string Instruction { get; }

        public void Start(GameContext ctx, RectTransform root, float difficulty,
            Action<InterludeResult, float> finished)
        {
            Ctx = ctx;
            Root = root;
            Difficulty = Mathf.Clamp01(difficulty);
            _finished = finished;
            _done = false;
            Build();
        }

        protected abstract void Build();

        public virtual void Tick(float dt) { }
        public virtual void OnPressed() { }
        public virtual void OnReleased() { }

        /// <summary>Time is up. Most games treat that as a failure; some score what they have.</summary>
        public virtual void OnTimeout() => Finish(InterludeResult.Fail, 0f);

        protected void Finish(InterludeResult result, float score)
        {
            if (_done) return;
            _done = true;
            _finished?.Invoke(result, Mathf.Clamp01(score));
        }

        // --- shared building blocks ---

        protected Image Sprite(string path, Vector2 size, Vector2 pos)
        {
            var img = UiFactory.CreateSpriteImage(Root, "Art", path, size);
            UiFactory.Place((RectTransform)img.transform, new Vector2(0.5f, 0.5f), pos, size);
            return img;
        }

        /// <summary>A tappable sprite, for the games that pick between things.</summary>
        protected Button SpriteButton(string path, Vector2 size, Vector2 pos, Action onClick)
        {
            var rt = UiFactory.CreateRect(Root, "Pick");
            UiFactory.Place(rt, new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ArtSprites.Get(path);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        protected static void SetSprite(Component target, string path)
        {
            var img = target as Image;
            if (img != null) img.sprite = ArtSprites.Get(path);
        }
    }
}

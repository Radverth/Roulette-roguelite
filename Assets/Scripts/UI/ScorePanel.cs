using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Take x Mult, assembled in front of the player.
    ///
    /// Every multiplier in the game — the table, the streak, blessings, the
    /// sins, the Pledges — arrives here as one chip, colour-coded by operation,
    /// and the running figure ticks as each lands. A spin that resolves
    /// silently into a number teaches nothing; a spin the player watches being
    /// built teaches them their own build.
    ///
    /// Pure presentation. It reads a resolved ScoreBreakdown and changes
    /// nothing about it.
    /// </summary>
    public sealed class ScorePanel
    {
        /// <summary>Rows the column can hold before the rest is folded into one.</summary>
        private const int MaxRows = 4;
        private const float ChipStagger = 0.09f;
        private const float ChipDuration = 0.18f;
        private const float HoldSeconds = 0.75f;
        private const float FadeSeconds = 0.25f;

        private sealed class Row
        {
            public GameObject Go;
            public Image Chip;
            public PixelText Label;
            public PixelText Value;
            public RectTransform Rect;
        }

        private readonly GameContext _ctx;

        private GameObject _panel;
        private CanvasGroup _group;
        private PixelText _takeValue;
        private PixelText _multValue;
        private GameObject _totalGroup;
        private Image _plate;
        private PixelText _totalValue;
        private readonly List<Row> _rows = new List<Row>();
        private Coroutine _routine;

        public bool IsShowing => _panel != null && _panel.activeSelf;

        public ScorePanel(GameContext ctx, RectTransform frame)
        {
            _ctx = ctx;
            Build(frame);
        }

        private static Vector2 Top => new Vector2(0.5f, 1f);

        private void Build(RectTransform frame)
        {
            var root = UiFactory.CreateRect(frame, "ScorePanel");
            UiFactory.Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = root.gameObject;
            _group = _panel.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false; // the player can still reach the wheel

            // Dims the wheel behind the sum without hiding where it landed.
            var backdrop = UiFactory.CreatePanel(root, "Backdrop", new Color(0.08f, 0.06f, 0.10f, 0.86f));
            UiFactory.Place(backdrop, Top, new Vector2(0f, -199f), new Vector2(180f, 150f));

            var frameImg = UiFactory.CreateSpriteImage(root, "Frame", "Pledges/score_panel", new Vector2(128f, 44f));
            UiFactory.Place((RectTransform)frameImg.transform, Top, new Vector2(0f, -150f), new Vector2(128f, 44f));

            var takeLabel = PixelText.Create(root, "TakeLabel", "TAKE", Palette.Dim);
            UiFactory.Place((RectTransform)takeLabel.transform, Top, new Vector2(-40f, -140f), Vector2.zero);

            _takeValue = PixelText.Create(root, "TakeValue", "0", Palette.Bone, 2);
            UiFactory.Place((RectTransform)_takeValue.transform, Top, new Vector2(-40f, -159f), Vector2.zero);

            var times = UiFactory.CreateSpriteImage(root, "OpTimes", "Pledges/op_times", new Vector2(12f, 12f));
            UiFactory.Place((RectTransform)times.transform, Top, new Vector2(0f, -152f), new Vector2(12f, 12f));

            var multLabel = PixelText.Create(root, "MultLabel", "MULT", Palette.Dim);
            UiFactory.Place((RectTransform)multLabel.transform, Top, new Vector2(38f, -140f), Vector2.zero);

            _multValue = PixelText.Create(root, "MultValue", "X1.0", Palette.Gold, 2);
            UiFactory.Place((RectTransform)_multValue.transform, Top, new Vector2(38f, -159f), Vector2.zero);

            for (int i = 0; i < MaxRows + 1; i++)
                _rows.Add(BuildRow(root, i));

            _totalGroup = UiFactory.CreateRect(root, "Total").gameObject;
            UiFactory.Stretch((RectTransform)_totalGroup.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var equals = UiFactory.CreateSpriteImage(_totalGroup.transform, "OpEquals", "Pledges/op_equals", new Vector2(12f, 12f));
            UiFactory.Place((RectTransform)equals.transform, Top, new Vector2(-46f, -252f), new Vector2(12f, 12f));

            _plate = UiFactory.CreateSpriteImage(_totalGroup.transform, "Plate", "Pledges/big_number_plate", new Vector2(64f, 36f));
            UiFactory.Place((RectTransform)_plate.transform, Top, new Vector2(6f, -252f), new Vector2(64f, 36f));

            _totalValue = PixelText.Create(_totalGroup.transform, "TotalValue", "0", Palette.Gold, 2);
            UiFactory.Place((RectTransform)_totalValue.transform, Top, new Vector2(6f, -252f), Vector2.zero);

            _panel.SetActive(false);
        }

        private Row BuildRow(RectTransform parent, int index)
        {
            var rect = UiFactory.CreateRect(parent, $"Term_{index}");
            UiFactory.Place(rect, Top, new Vector2(0f, -180f - index * 15f), new Vector2(180f, 14f));

            var row = new Row { Go = rect.gameObject, Rect = rect };

            row.Label = PixelText.Create(rect, "Label", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)row.Label.transform, new Vector2(0f, 0.5f), new Vector2(4f, 0f), Vector2.zero);

            row.Chip = UiFactory.CreateSpriteImage(rect, "Chip", "Pledges/term_chip_mult", new Vector2(52f, 14f));
            UiFactory.Place((RectTransform)row.Chip.transform, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(52f, 14f));

            row.Value = PixelText.Create(rect, "Value", "", Palette.Bone);
            UiFactory.Place((RectTransform)row.Value.transform, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), Vector2.zero);

            row.Go.SetActive(false);
            return row;
        }

        /// <summary>
        /// Play the assembly. Below the minimum term count the animation is
        /// noise and the caller should not have bothered — we honour that here
        /// so no call site has to remember the rule.
        /// </summary>
        public void Show(ScoreBreakdown score)
        {
            if (score == null || !score.WorthShowing) return;
            if (_routine != null) _ctx.CoroutineHost.StopCoroutine(_routine);
            _routine = _ctx.CoroutineHost.StartCoroutine(Assemble(score));
        }

        public void HideImmediate()
        {
            if (_routine != null)
            {
                _ctx.CoroutineHost.StopCoroutine(_routine);
                _routine = null;
            }
            if (_panel != null) _panel.SetActive(false);
        }

        private IEnumerator Assemble(ScoreBreakdown score)
        {
            List<MultTerm> shown = Condense(score);

            _panel.SetActive(true);
            _group.alpha = 1f;
            _takeValue.Text = score.Take.ToString();
            _multValue.Text = "X1.0";
            _multValue.Color = Palette.Bone;
            _totalGroup.SetActive(false);

            for (int i = 0; i < _rows.Count; i++) _rows[i].Go.SetActive(false);

            var running = new List<MultTerm>();
            for (int i = 0; i < shown.Count && i < _rows.Count; i++)
            {
                var term = shown[i];
                var row = _rows[i];

                row.Chip.sprite = ArtSprites.Get(ScoreBreakdown.ChipSprite(term.Op));
                row.Label.Text = Truncate(term.Label);
                row.Label.Color = ColorFor(term.Op);
                row.Value.Text = Format(term);
                row.Go.SetActive(true);

                running.Add(term);
                _multValue.Text = $"X{ScoreBreakdown.ResolveTerms(running):0.0}";

                Sfx.Tick();
                yield return SlideIn(row.Rect);

                float wait = ChipStagger - ChipDuration;
                if (wait > 0f) yield return new WaitForSeconds(wait);
            }

            _multValue.Text = $"X{score.Mult:0.0}";
            _multValue.Color = score.Mult >= 1f ? Palette.Gold : Palette.Blood;

            _totalValue.Text = score.Total.ToString();
            _plate.sprite = ArtSprites.Get(score.Mult >= 2f
                ? "Pledges/big_number_plate_hot" : "Pledges/big_number_plate");
            _totalGroup.SetActive(true);
            Sfx.Reward();
            if (score.Mult >= 2f) Haptics.Light();

            yield return new WaitForSeconds(HoldSeconds);

            float elapsed = 0f;
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.deltaTime;
                _group.alpha = 1f - Mathf.Clamp01(elapsed / FadeSeconds);
                yield return null;
            }

            _panel.SetActive(false);
            _routine = null;
        }

        /// <summary>
        /// The chip flies in from the right and lands. Position only, rounded
        /// to whole pixels every frame — a scaled sprite would come off the
        /// grid, and this art is only ever allowed integer factors.
        /// </summary>
        private IEnumerator SlideIn(RectTransform rect)
        {
            Vector2 home = rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < ChipDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ChipDuration);
                float offset = Mathf.Round(Mathf.Lerp(28f, 0f, t * (2f - t)));
                rect.anchoredPosition = home + new Vector2(offset, 0f);
                yield return null;
            }
            rect.anchoredPosition = home;
        }

        /// <summary>
        /// The column holds four rows. A build that beats that gets its three
        /// loudest terms named and the remainder folded honestly into one, so
        /// the figures still multiply out to the total on the plate.
        /// </summary>
        private static List<MultTerm> Condense(ScoreBreakdown score)
        {
            var terms = new List<MultTerm>(score.Terms);
            if (terms.Count <= MaxRows) return terms;

            terms.Sort((a, b) => Impact(b).CompareTo(Impact(a)));

            var kept = terms.GetRange(0, MaxRows - 1);
            float rest = score.Mult / Mathf.Max(0.0001f, ScoreBreakdown.ResolveTerms(kept));
            kept.Add(new MultTerm
            {
                Label = $"OTHERS X{terms.Count - kept.Count}",
                Op = rest < 1f ? MultOp.Reduce : MultOp.Multiply,
                Value = rest
            });
            return kept;
        }

        private static float Impact(MultTerm term) =>
            term.Op == MultOp.Add ? Mathf.Abs(term.Value) : Mathf.Abs(term.Value - 1f);

        private static string Format(MultTerm term) =>
            term.Op == MultOp.Add ? $"+{term.Value:0.0}" : $"X{term.Value:0.00}";

        private static Color ColorFor(MultOp op)
        {
            switch (op)
            {
                case MultOp.Add: return Palette.Gold;
                case MultOp.Reduce: return Palette.Blood;
                default: return Palette.Purple;
            }
        }

        /// <summary>Twelve glyphs is what fits left of the chip at scale 1.</summary>
        private static string Truncate(string label)
        {
            if (string.IsNullOrEmpty(label)) return "";
            return label.Length <= 12 ? label : label.Substring(0, 12);
        }
    }
}

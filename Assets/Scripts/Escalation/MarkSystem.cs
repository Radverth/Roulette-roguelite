using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Tables ramp within a run; Marks ramp across them. As the debt clears the
    /// house raises the stakes permanently — seven Marks, taken in order, never
    /// chosen from a menu and never given back. Difficulty arrives as a
    /// consequence of paying, in the game's own voice.
    /// </summary>
    public sealed class MarkSystem
    {
        private readonly GameContext _ctx;

        public MarkSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public int EarnedCount => Mathf.Clamp(_ctx.Save.Data.marksEarned, 0, Total);
        public int Total => _ctx.Config.Marks.marks.Count;

        public MarkConfig Mark(int index)
        {
            foreach (var m in _ctx.Config.Marks.marks)
                if (m.index == index) return m;
            return null;
        }

        public bool IsEarned(int index) => index <= EarnedCount;

        /// <summary>
        /// Debt cleared since the start buys Marks. Returns any newly earned
        /// this settlement so the ledger can announce them.
        /// </summary>
        public List<MarkConfig> CheckForNewMarks()
        {
            var gained = new List<MarkConfig>();
            var data = _ctx.Save.Data;

            int cleared = Mathf.Max(0, _ctx.Config.Tuning.debtStart - data.debt);
            int deserved = Mathf.Clamp(cleared / Mathf.Max(1, _ctx.Config.Marks.debtIntervalPerMark), 0, Total);

            while (data.marksEarned < deserved)
            {
                data.marksEarned++;
                var mark = Mark(data.marksEarned);
                if (mark != null) gained.Add(mark);
                _ctx.Analytics.Track("mark_earned", "mark", data.marksEarned, "debt", data.debt);
            }
            return gained;
        }

        // --- Effects, folded over every earned Mark ---

        private float Sum(System.Func<MarkConfig, float> pick)
        {
            float total = 0f;
            for (int i = 1; i <= EarnedCount; i++)
            {
                var mark = Mark(i);
                if (mark != null) total += pick(mark);
            }
            return total;
        }

        /// <summary>Mark I: the quota rises.</summary>
        public float QuotaMultiplier => 1f + Sum(m => m.quotaPercent) / 100f;

        /// <summary>Mark II: a wound is in the ring before the first spin.</summary>
        public int ExtraRiskWedges => Mathf.RoundToInt(Sum(m => m.extraRiskWedges));

        /// <summary>Mark III: sins outstay their welcome.</summary>
        public int SinDurationBonus => Mathf.RoundToInt(Sum(m => m.sinDurationBonus));

        /// <summary>Mark IV: the eye is already open when you sit.</summary>
        public float NoticeStartFraction => Mathf.Clamp01(Sum(m => m.noticeStartPercent) / 100f);

        /// <summary>Mark V: paying draws twice the attention.</summary>
        public int TitheNoticeMultiplier
        {
            get
            {
                int multiplier = 1;
                for (int i = 1; i <= EarnedCount; i++)
                {
                    var mark = Mark(i);
                    if (mark != null && mark.titheNoticeMultiplier > 1)
                        multiplier *= mark.titheNoticeMultiplier;
                }
                return multiplier;
            }
        }

        /// <summary>Mark VI: every break condition wants one more.</summary>
        public int BreakTargetBonus => Mathf.RoundToInt(Sum(m => m.breakTargetBonus));

        /// <summary>Mark VII: he stops waiting for Table VII.</summary>
        public int CroupierFromTable
        {
            get
            {
                int earliest = _ctx.Config.Tables.croupierTable;
                for (int i = 1; i <= EarnedCount; i++)
                {
                    var mark = Mark(i);
                    if (mark != null && mark.croupierFromTable > 0)
                        earliest = Mathf.Min(earliest, mark.croupierFromTable);
                }
                return earliest;
            }
        }
    }
}

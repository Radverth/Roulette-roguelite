using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// The escalation, made visible. Summon pressure used to be an invisible
    /// rising number, so encounters read as bad luck; here it is eight segments
    /// with an eye that opens as they fill. At full, the next risk wedge
    /// guarantees a summon and the meter resets.
    /// </summary>
    public sealed class NoticeSystem
    {
        private readonly GameContext _ctx;

        public float Value { get; private set; }

        public float Segments => _ctx.Config.Tuning.noticeSegments;
        public float Fill => Mathf.Clamp01(Value / Mathf.Max(1f, Segments));
        public bool IsFull => Value >= Segments;

        /// <summary>Eye stage 0-3, for notice_eye_0..3.</summary>
        public int EyeStage => Mathf.Clamp(Mathf.FloorToInt(Fill * 4f), 0, 3);

        /// <summary>Fill sprite tier: cold below half, warm, critical near full.</summary>
        public string FillSprite
        {
            get
            {
                if (Fill >= 0.75f) return "Loop/notice_fill_critical";
                if (Fill >= 0.4f) return "Loop/notice_fill_warm";
                return "Loop/notice_fill_cold";
            }
        }

        public NoticeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            Value = 0f;
        }

        public void Add(float amount)
        {
            Value = Mathf.Clamp(Value + amount, 0f, Segments);
        }

        /// <summary>Spins draw attention; so does walking around with a full purse.</summary>
        public void OnSpin(int runPurse)
        {
            var t = _ctx.Config.Tuning;
            Add(t.noticePerSpin);
            if (runPurse >= t.noticeHighPurseThreshold)
                Add(t.noticeHighPurseBonus);
        }

        public void OnTithe() => Add(_ctx.Config.Tuning.noticePerTithe);
        public void OnHumility() => Add(_ctx.Config.Tuning.noticeOnHumility);
        public void OnSinBroken() => Add(_ctx.Config.Tuning.noticeOnSinBroken);
        public void OnEncounterEnded() => Add(_ctx.Config.Tuning.noticeOnEncounterEnd);

        /// <summary>Spend the full meter to force a summon.</summary>
        public void ConsumeFull()
        {
            Value = 0f;
        }
    }
}

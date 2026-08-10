using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// The decision inside the spin. After the wheel settles but before it
    /// resolves, you may push it a wedge either way — paid in Notice, because
    /// you are cheating the house and the house notices.
    ///
    /// The roll was still fair: nudging changes the outcome after the fact, not
    /// the odds. At full Notice it is disabled rather than punished — being
    /// unable to cheat when you most need to is the better sting.
    /// </summary>
    public sealed class NudgeSystem
    {
        private readonly GameContext _ctx;

        public bool WindowOpen { get; private set; }
        public float WindowRemaining { get; private set; }
        public float WindowLength { get; private set; } = 1.2f;
        /// <summary>Wedges pushed this spin; the second one costs more.</summary>
        public int Pushed { get; private set; }

        public NudgeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Deliberation kills the pace, so the window tightens as you descend.</summary>
        public float WindowForTable()
        {
            var t = _ctx.Config.Tuning;
            return Mathf.Max(t.nudgeWindowFloor,
                t.nudgeWindowBase - t.nudgeWindowPerTable * (_ctx.Tables.CurrentTable - 1));
        }

        /// <summary>Cost in Notice segments of the next push this spin.</summary>
        public int NextCost
        {
            get
            {
                var t = _ctx.Config.Tuning;
                // The Thumb moves two for the price of one, so the second push
                // it grants is never billed.
                int step = _ctx.Pledges.NudgeTwoForOne ? 0 : Pushed;
                int cost = step == 0 ? t.nudgeCostOne : t.nudgeCostTwo - t.nudgeCostOne;
                return Mathf.Max(1, cost - _ctx.Pledges.NudgeNoticeDiscount);
            }
        }

        public bool CanAfford
        {
            get
            {
                if (_ctx.Pledges.NudgeCostsHealth)
                    return _ctx.Health.CurrentHp > _ctx.Pledges.NudgeHealthCost;
                // At full Notice you simply cannot cheat any more.
                if (_ctx.Notice.IsFull) return false;
                return _ctx.Notice.Value + NextCost <= _ctx.Notice.Segments;
            }
        }

        public bool CanNudge => WindowOpen && CanAfford && Pushed < 2;

        public void Open()
        {
            WindowLength = WindowForTable();
            WindowRemaining = WindowLength;
            WindowOpen = true;
            Pushed = 0;
        }

        public void Close()
        {
            WindowOpen = false;
            WindowRemaining = 0f;
        }

        /// <summary>Ticks the window down. Returns true the frame it expires.</summary>
        public bool Tick(float dt)
        {
            if (!WindowOpen) return false;
            WindowRemaining -= dt;
            if (WindowRemaining > 0f) return false;
            Close();
            return true;
        }

        /// <summary>
        /// Push the settled wheel one wedge. Returns the direction actually
        /// taken — The Thumb sometimes lets the house choose instead.
        /// </summary>
        public int Push(int direction)
        {
            if (!CanNudge) return 0;

            if (_ctx.Pledges.NudgeTwoForOne
                && _ctx.Rng.NextDouble() < _ctx.Pledges.ThumbHijackPercent / 100f)
            {
                direction = _ctx.Rng.Next(2) == 0 ? -1 : 1;
                _ctx.Hud?.Toast("THE HOUSE PICKS", Palette.Blood);
            }

            if (_ctx.Pledges.NudgeCostsHealth)
                _ctx.Health.ApplyDamage(_ctx.Pledges.NudgeHealthCost);
            else
                _ctx.Notice.Add(NextCost);

            Pushed++;
            _ctx.Analytics.Track("nudge", "direction", direction, "pushed", Pushed,
                "table", _ctx.Tables.CurrentTable);
            Sfx.Tick();
            Haptics.Light();
            return direction;
        }

        public string ButtonSprite(bool left)
        {
            string side = left ? "left" : "right";
            if (!WindowOpen || !CanAfford || Pushed >= 2) return $"Pledges/nudge_{side}_disabled";
            return NextCost > 1 ? $"Pledges/nudge_{side}_costly" : $"Pledges/nudge_{side}_ready";
        }
    }
}

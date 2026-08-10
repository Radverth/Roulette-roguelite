using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>Live state of one sin encounter overlaying the normal wheel.</summary>
    public sealed class BossEncounter
    {
        public SinBossConfig Config;
        public BossModifierBase Modifier;
        public int SpinsRemaining;
        public int SpinsElapsed;
        public int Resist;            // Sloth
        public int BreakProgress;     // generic "N of something" counter
        public string LastWedgeId;    // Lust
        public int GreedPool;         // Greed's tithe pool
        public bool Broken;

        public float RewardMultiplier =>
            Config.rewardMultiplierStart + Config.rewardMultiplierPerSpin * SpinsElapsed;
    }

    /// <summary>
    /// A sin boss is a bundle of hooks stacked onto the normal wheel for the
    /// duration of an encounter. Every sin states how to break it, and the
    /// break is thematically the thing that sin cannot survive; breaking one
    /// leaves a permanent benefit for the rest of the run.
    /// </summary>
    public abstract class BossModifierBase
    {
        protected readonly GameContext Ctx;
        protected readonly SinBossConfig Cfg;

        protected BossModifierBase(GameContext ctx, SinBossConfig cfg)
        {
            Ctx = ctx;
            Cfg = cfg;
        }

        /// <summary>Warp the ring for the encounter (Wrath's teeth, Pride's humility wedges).</summary>
        public virtual List<SegmentConfig> ModifySegments(List<SegmentConfig> segments) => segments;

        /// <summary>Warp the post-spin cooldown (Sloth doubles it).</summary>
        public virtual float ModifyCooldown(float cooldown) => cooldown;

        /// <summary>Scale every reward payout (Pride shrinks them).</summary>
        public virtual float ModifyRewardMultiplier(float multiplier) => multiplier;

        /// <summary>Tax or reshape a coin payout (Greed skims into its pool).</summary>
        public virtual int ModifyCoinGain(int coins) => coins;

        /// <summary>Scale incoming damage (Envy turns your own blessing on you).</summary>
        public virtual float ModifyDamage(float damage) => damage;

        /// <summary>Charge an entry cost per spin (Gluttony eats banked currency).</summary>
        public virtual void OnSpinStarted(BossEncounter encounter) { }

        /// <summary>Advance break progress on the wedge that just landed.</summary>
        public virtual void OnSpinResolved(BossEncounter encounter, SegmentConfig landed) { }

        /// <summary>The player paid mid-encounter. Gluttony's whole character is this moment.</summary>
        public virtual void OnTithe(BossEncounter encounter) { }

        /// <summary>True once the break condition has been met.</summary>
        public virtual bool IsDefeated(BossEncounter encounter) => encounter.Broken;

        /// <summary>Grant the permanent, run-long benefit for breaking this sin.</summary>
        public virtual void OnBroken(BossEncounter encounter) { }

        /// <summary>Status line for the encounter strip.</summary>
        public virtual string StatusText(BossEncounter encounter) =>
            "ENDURE";

        public string BreakHint => Cfg.breakHint ?? "";

        // --- shared helpers ---

        protected float ResistUpgrade => Ctx.Upgrades.SinResistValue(Cfg.id);

        /// <summary>Mark VI: every counted break condition wants one more.</summary>
        protected int BreakTarget => Cfg.breakTarget + Ctx.Marks.BreakTargetBonus;

        protected int ResistTarget => Cfg.resistThreshold + Ctx.Marks.BreakTargetBonus;

        protected static List<SegmentConfig> Splice(List<SegmentConfig> ring, SegmentConfig wedge, int count)
        {
            if (wedge == null || count <= 0) return ring;
            for (int i = 0; i < count; i++)
            {
                var copy = wedge.Clone();
                // Spread the splices around the ring rather than clumping them.
                int at = Mathf.Clamp(1 + (i + 1) * ring.Count / (count + 1), 0, ring.Count);
                ring.Insert(at, copy);
            }
            return ring;
        }
    }

    /// <summary>
    /// Sloth — the wheel grows heavy. Cooldown doubles; the only way out early
    /// is a resist meter filled by unbroken spins, and a wound breaks the
    /// rhythm. Broken, the cooldown settles below baseline for the rest of the run.
    /// </summary>
    public sealed class SlothModifier : BossModifierBase
    {
        public SlothModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override float ModifyCooldown(float cooldown)
        {
            float mult = Mathf.Max(1f, Cfg.cooldownMultiplier - ResistUpgrade);
            return cooldown * mult;
        }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            if (landed.IsRisk)
            {
                encounter.Resist = 0; // the rhythm breaks
                return;
            }
            encounter.Resist++;
            if (ResistTarget > 0 && encounter.Resist >= ResistTarget)
                encounter.Broken = true;
        }

        public override void OnBroken(BossEncounter encounter)
        {
            Ctx.Run.SlothCooldownBonus = 0.3f;
        }

        public override string StatusText(BossEncounter encounter) =>
            $"RESIST {encounter.Resist}/{ResistTarget}";
    }

    /// <summary>
    /// Gluttony — every spin feeds it from the purse. The escape hatch costs
    /// you: tithe during the encounter and it takes its cut and leaves.
    /// </summary>
    public sealed class GluttonyModifier : BossModifierBase
    {
        public GluttonyModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override void OnSpinStarted(BossEncounter encounter)
        {
            float percent = Mathf.Max(0f, Cfg.spinCostPercentOfBank - ResistUpgrade);
            int eaten = Ctx.Wallet.ChargeMetaCoinsPercent(percent);
            if (eaten > 0) Ctx.Hud?.Toast($"GLUTTONY TAKES {eaten}", Palette.Sickly);
        }

        public override void OnTithe(BossEncounter encounter)
        {
            encounter.Broken = true;
        }

        public override string StatusText(BossEncounter encounter) =>
            "TITHE TO ESCAPE";
    }

    /// <summary>
    /// Pride — rewards wither beneath its gaze. It splices in humility wedges
    /// and dares you to take three in a row; do it and its gaze cannot narrow
    /// your odds again this run.
    /// </summary>
    public sealed class PrideModifier : BossModifierBase
    {
        public PrideModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override List<SegmentConfig> ModifySegments(List<SegmentConfig> segments)
        {
            return Splice(segments, Ctx.Ring.Template("humility"), Cfg.humilityWedges);
        }

        public override float ModifyRewardMultiplier(float multiplier)
        {
            if (Ctx.Run.PrideOddsLocked) return multiplier;
            float shrink = Mathf.Max(0f, Cfg.rewardShrinkPercent - ResistUpgrade) / 100f;
            return multiplier * (1f - Mathf.Clamp01(shrink));
        }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            if (landed.ParsedType == SegmentType.Humility)
            {
                encounter.BreakProgress++;
                if (encounter.BreakProgress >= BreakTarget) encounter.Broken = true;
            }
            else
            {
                encounter.BreakProgress = 0;
            }
        }

        public override void OnBroken(BossEncounter encounter)
        {
            Ctx.Run.PrideOddsLocked = true;
        }

        public override string StatusText(BossEncounter encounter) =>
            $"HUMILITY {encounter.BreakProgress}/{BreakTarget}";
    }

    /// <summary>
    /// Greed — a third of everything, held in a pool behind it. Land the
    /// jackpot and the whole pool comes back at once.
    /// </summary>
    public sealed class GreedModifier : BossModifierBase
    {
        public GreedModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        private BossEncounter _encounter;

        public override int ModifyCoinGain(int coins)
        {
            float percent = Mathf.Max(0f, Cfg.coinTaxPercent - ResistUpgrade);
            int tax = Mathf.RoundToInt(coins * Mathf.Clamp01(percent / 100f));
            if (_encounter != null) _encounter.GreedPool += tax;
            return coins - tax;
        }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            _encounter = encounter;
            if (landed.id == "jackpot")
                encounter.Broken = true;
        }

        public override void OnSpinStarted(BossEncounter encounter)
        {
            _encounter = encounter;
        }

        public override void OnBroken(BossEncounter encounter)
        {
            if (encounter.GreedPool <= 0) return;
            Ctx.Wallet.AddRunCoins(encounter.GreedPool);
            Ctx.Hud?.Toast($"POOL RECLAIMED +{encounter.GreedPool}", Palette.Gold);
        }

        public override string StatusText(BossEncounter encounter) =>
            $"POOL {encounter.GreedPool}";
    }

    /// <summary>
    /// Wrath — the wheel sprouts teeth. Take three wounds while staying above
    /// a quarter of your resilience and the teeth turn to coin for the rest of
    /// the run; drop below and the count starts again.
    /// </summary>
    public sealed class WrathModifier : BossModifierBase
    {
        public WrathModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override List<SegmentConfig> ModifySegments(List<SegmentConfig> segments)
        {
            var tooth = Ctx.Ring.Template("damage_large");
            if (tooth == null) return segments;

            var scaled = tooth.Clone();
            scaled.amount *= Mathf.Max(0.1f, 1f - ResistUpgrade / 100f);
            return Splice(segments, scaled, Cfg.extraDamageSegments);
        }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            if (landed.ParsedType != SegmentType.Damage) return;

            float floor = Ctx.Health.MaxHp * Cfg.wrathHealthFloorPercent / 100f;
            if (Ctx.Health.CurrentHp < floor)
            {
                encounter.BreakProgress = 0;
                return;
            }

            encounter.BreakProgress++;
            if (encounter.BreakProgress >= BreakTarget) encounter.Broken = true;
        }

        public override void OnBroken(BossEncounter encounter)
        {
            // The teeth stay, as coin.
            for (int i = 0; i < Cfg.extraDamageSegments; i++)
                Ctx.Run.ExtraWedges.Add("coin_mid");
        }

        public override string StatusText(BossEncounter encounter) =>
            $"WOUNDS {encounter.BreakProgress}/{BreakTarget}";
    }

    /// <summary>
    /// Envy — whatever you love most, it has already copied, and turned on
    /// you. Land a wedge it has not seen you take all run and it has nothing
    /// left to copy.
    /// </summary>
    public sealed class EnvyModifier : BossModifierBase
    {
        public EnvyModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override float ModifyDamage(float damage)
        {
            var best = Ctx.Buffs.BestBuff;
            if (best == null) return damage;
            float mirrored = 1f + (best.Multiplier - 1f) * Mathf.Max(0f, 1f - ResistUpgrade / 100f);
            return damage * mirrored;
        }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            // WedgesHit is recorded after the modifier runs, so an id missing
            // here is genuinely one this run has never produced.
            if (!Ctx.Run.WedgesHit.Contains(landed.id))
                encounter.Broken = true;
        }

        public override string StatusText(BossEncounter encounter) =>
            "FIND SOMETHING NEW";
    }

    /// <summary>
    /// Lust — nothing stays where you left it. Land the same wedge twice in a
    /// row through the shuffling and the ring locks for the rest of the run.
    /// </summary>
    public sealed class LustModifier : BossModifierBase
    {
        public LustModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override void OnSpinResolved(BossEncounter encounter, SegmentConfig landed)
        {
            if (encounter.LastWedgeId == landed.id)
            {
                encounter.Broken = true;
                return;
            }
            encounter.LastWedgeId = landed.id;

            int period = Mathf.Max(1, Cfg.shufflePeriodSpins + Mathf.RoundToInt(ResistUpgrade));
            if (!Ctx.Run.LustRingLocked && encounter.SpinsElapsed % period == 0)
            {
                Ctx.Ring.Shuffle();
                Ctx.Hud?.Toast("THE RING TURNS", Palette.Purple);
            }
        }

        public override void OnBroken(BossEncounter encounter)
        {
            Ctx.Run.LustRingLocked = true;
            Ctx.Ring.ClearShuffle();
        }

        public override string StatusText(BossEncounter encounter) =>
            "REPEAT A WEDGE";
    }

    public static class BossModifierFactory
    {
        public static BossModifierBase Create(GameContext ctx, SinBossConfig cfg)
        {
            switch (cfg.id)
            {
                case "sloth": return new SlothModifier(ctx, cfg);
                case "gluttony": return new GluttonyModifier(ctx, cfg);
                case "pride": return new PrideModifier(ctx, cfg);
                case "greed": return new GreedModifier(ctx, cfg);
                case "wrath": return new WrathModifier(ctx, cfg);
                case "envy": return new EnvyModifier(ctx, cfg);
                case "lust": return new LustModifier(ctx, cfg);
                default:
                    Debug.LogError($"[Boss] No modifier implemented for sin '{cfg.id}'");
                    return new SlothModifier(ctx, cfg);
            }
        }
    }
}

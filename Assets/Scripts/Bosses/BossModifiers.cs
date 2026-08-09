using System.Collections.Generic;
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
        public int Resist; // Sloth: fills with consecutive spins

        public float RewardMultiplier =>
            Config.rewardMultiplierStart + Config.rewardMultiplierPerSpin * SpinsElapsed;
    }

    /// <summary>
    /// A sin boss is a bundle of hooks stacked onto the normal wheel for the
    /// duration of an encounter. Each of the seven sins subclasses this and
    /// overrides only what it warps. Kept as virtual methods (not default
    /// interface methods) so adding a hook never breaks existing sins.
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

        /// <summary>Warp the segment list for the next spin (Wrath adds damage segments, Pride shrinks rewards...).</summary>
        public virtual List<SegmentConfig> ModifySegments(List<SegmentConfig> segments) => segments;

        /// <summary>Warp the post-spin cooldown (Sloth doubles it).</summary>
        public virtual float ModifyCooldown(float cooldown) => cooldown;

        /// <summary>Tax or reshape a coin payout (Greed skims into the jackpot).</summary>
        public virtual int ModifyCoinGain(int coins) => coins;

        /// <summary>Charge an entry cost per spin (Gluttony eats banked currency).</summary>
        public virtual void OnSpinStarted(BossEncounter encounter) { }

        /// <summary>Advance sin-specific state each resolved spin (Sloth's resist meter).</summary>
        public virtual void OnSpinResolved(BossEncounter encounter) { }

        /// <summary>True once the player has broken the encounter early.</summary>
        public virtual bool IsDefeated(BossEncounter encounter) => false;

        /// <summary>Status line for the boss banner.</summary>
        public virtual string StatusText(BossEncounter encounter) =>
            $"{encounter.SpinsRemaining} SPINS REMAIN";
    }

    /// <summary>
    /// Sloth — the vertical-slice sin. Doubles the spin cooldown; the only way
    /// out early is a resist meter filled by consecutive spins. The sloth
    /// resistance upgrade tree shaves the cooldown multiplier per tier.
    /// </summary>
    public sealed class SlothModifier : BossModifierBase
    {
        public SlothModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }

        public override float ModifyCooldown(float cooldown)
        {
            float mult = Mathf.Max(1f, Cfg.cooldownMultiplier - Ctx.Upgrades.SinResistValue(Cfg.id));
            return cooldown * mult;
        }

        public override void OnSpinResolved(BossEncounter encounter)
        {
            encounter.Resist++;
        }

        public override bool IsDefeated(BossEncounter encounter)
        {
            return Cfg.resistThreshold > 0 && encounter.Resist >= Cfg.resistThreshold;
        }

        public override string StatusText(BossEncounter encounter)
        {
            // 5x7 font charset only — no parens or middots in UI strings.
            return $"RESIST {encounter.Resist}/{Cfg.resistThreshold} - {encounter.SpinsRemaining} LEFT";
        }
    }

    /// <summary>
    /// Placeholder for sins not yet implemented (Pride, Greed, Wrath, Envy,
    /// Lust, Gluttony). They stay locked out of the summon pool until their
    /// modifier lands, but their configs already ship so balance work can start.
    /// </summary>
    public sealed class NoOpModifier : BossModifierBase
    {
        public NoOpModifier(GameContext ctx, SinBossConfig cfg) : base(ctx, cfg) { }
    }

    public static class BossModifierFactory
    {
        public static BossModifierBase Create(GameContext ctx, SinBossConfig cfg)
        {
            switch (cfg.id)
            {
                case "sloth":
                    return new SlothModifier(ctx, cfg);
                default:
                    Debug.LogWarning($"[Boss] No modifier implemented for sin '{cfg.id}', using no-op");
                    return new NoOpModifier(ctx, cfg);
            }
        }
    }
}

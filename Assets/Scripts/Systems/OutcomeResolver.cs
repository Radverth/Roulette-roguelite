using UnityEngine;

namespace SinWheel
{
    public struct OutcomeResult
    {
        public SegmentType Type;
        public string Text;
        public Color Color;
        public bool BigHit; // drives shake/haptics juice
    }

    /// <summary>
    /// Applies a landed wedge to the run. Reward payouts stack the wedge's
    /// temper tier, active blessings, the streak chain and the sin's own
    /// escalation — and are then shrunk by any sin that taxes them.
    /// </summary>
    public static class OutcomeResolver
    {
        public static OutcomeResult Apply(GameContext ctx, SegmentConfig seg)
        {
            var t = ctx.Config.Tuning;

            float rewardMult = ctx.Buffs.RewardMultiplier
                * ctx.Bosses.CurrentRewardMultiplier
                * ctx.Streak.Multiplier;
            rewardMult = ctx.Bosses.ModifyRewardMultiplier(rewardMult);

            switch (seg.ParsedType)
            {
                case SegmentType.Coins:
                {
                    int gain = Mathf.RoundToInt(seg.EffectiveAmount * rewardMult);
                    gain = ctx.Bosses.ModifyCoinGain(gain); // Greed's tithe hook
                    ctx.Wallet.AddRunCoins(gain);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"+{gain} COINS",
                        Color = Palette.Gold,
                        BigHit = seg.EffectiveAmount >= 50
                    };
                }

                case SegmentType.Xp:
                {
                    int gain = Mathf.RoundToInt(seg.EffectiveAmount * rewardMult);
                    ctx.Xp.AddXp(gain);
                    return new OutcomeResult { Type = seg.ParsedType, Text = $"+{gain} XP", Color = Palette.Teal };
                }

                case SegmentType.Gems:
                {
                    int gain = Mathf.Max(1, Mathf.RoundToInt(seg.EffectiveAmount));
                    ctx.Wallet.AddGems(gain);
                    return new OutcomeResult { Type = seg.ParsedType, Text = $"+{gain} SHARD", Color = Palette.Purple, BigHit = true };
                }

                case SegmentType.Buff:
                {
                    ctx.Buffs.AddBuff("blessing", t.buffRewardMultiplier, t.buffDurationSpins);
                    int pct = Mathf.RoundToInt((t.buffRewardMultiplier - 1f) * 100f);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"BLESS +{pct}% X{t.buffDurationSpins}",
                        Color = Palette.Bone
                    };
                }

                case SegmentType.Humility:
                {
                    // Lowering your eyes draws the house's attention away.
                    ctx.Notice.OnHumility();
                    int gain = Mathf.RoundToInt(seg.EffectiveAmount * rewardMult);
                    ctx.Wallet.AddRunCoins(gain);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = gain > 0 ? $"HUMILITY +{gain}" : "HUMILITY",
                        Color = Palette.Dim
                    };
                }

                case SegmentType.Damage:
                {
                    float raw = seg.EffectiveAmount * ctx.Buffs.DamageMultiplier;
                    float dmg = ctx.Health.ApplyDamage(ctx.Bosses.ModifyDamage(raw));
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"-{Mathf.RoundToInt(dmg)} HP",
                        Color = Palette.Blood,
                        BigHit = true
                    };
                }

                case SegmentType.CoinLoss:
                {
                    int lost = ctx.Wallet.LoseRunCoinsPercent(seg.EffectiveAmount);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = lost > 0 ? $"-{lost} COINS" : "NOTHING TO TAKE",
                        Color = Palette.Blood
                    };
                }

                case SegmentType.Debuff:
                {
                    ctx.Buffs.AddDebuff("hex", t.debuffDamageMultiplier, t.debuffDurationSpins);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"HEXED X{t.debuffDurationSpins} SPINS",
                        Color = Palette.Sickly
                    };
                }

                case SegmentType.SinSummon:
                default:
                {
                    string text = ctx.Bosses.OnSummonSegmentHit();
                    return new OutcomeResult
                    {
                        Type = SegmentType.SinSummon,
                        Text = text,
                        Color = Palette.Purple,
                        BigHit = ctx.Bosses.EncounterActive
                    };
                }
            }
        }
    }
}

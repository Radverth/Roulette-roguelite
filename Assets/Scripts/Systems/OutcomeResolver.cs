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
    /// Applies a landed segment to the run. Reward multiplier stacks buffs and
    /// the active sin's risk/reward escalation; damage stacks debuffs.
    /// </summary>
    public static class OutcomeResolver
    {
        public static OutcomeResult Apply(GameContext ctx, SegmentConfig seg)
        {
            var t = ctx.Config.Tuning;
            float rewardMult = ctx.Buffs.RewardMultiplier * ctx.Bosses.CurrentRewardMultiplier;

            switch (seg.ParsedType)
            {
                case SegmentType.Coins:
                {
                    int gain = Mathf.RoundToInt(seg.amount * rewardMult);
                    gain = ctx.Bosses.ModifyCoinGain(gain); // Greed's tax hook
                    ctx.Wallet.AddRunCoins(gain);
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"+{gain} COINS",
                        Color = Palette.Gold,
                        BigHit = seg.amount >= 50
                    };
                }

                case SegmentType.Xp:
                {
                    int gain = Mathf.RoundToInt(seg.amount * rewardMult);
                    ctx.Xp.AddXp(gain);
                    return new OutcomeResult { Type = seg.ParsedType, Text = $"+{gain} XP", Color = Palette.Teal };
                }

                case SegmentType.Gems:
                {
                    int gain = Mathf.Max(1, Mathf.RoundToInt(seg.amount));
                    ctx.Wallet.AddGems(gain);
                    return new OutcomeResult { Type = seg.ParsedType, Text = $"+{gain} GEM", Color = Palette.Purple, BigHit = true };
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

                case SegmentType.Damage:
                {
                    float dmg = ctx.Health.ApplyDamage(seg.amount * ctx.Buffs.DamageMultiplier);
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
                    int lost = ctx.Wallet.LoseRunCoinsPercent(seg.amount);
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

using UnityEngine;

namespace SinWheel
{
    public struct OutcomeResult
    {
        public SegmentType Type;
        public string Text;
        public Color Color;
        public bool BigHit; // drives shake/haptics juice
        /// <summary>The arithmetic behind a payout, for the Take x Mult panel. Null when there was none.</summary>
        public ScoreBreakdown Score;
    }

    /// <summary>
    /// Applies a landed wedge to the run. Every multiplier in the game — the
    /// table, the streak, blessings, the sins, the Pledges — folds into one
    /// figure that is assembled in front of the player rather than resolving
    /// silently into a number.
    /// </summary>
    public static class OutcomeResolver
    {
        /// <summary>Collect every term that speaks to this spin's payout.</summary>
        public static ScoreBreakdown BuildScore(GameContext ctx)
        {
            var score = new ScoreBreakdown();

            if (ctx.Tables.CurrentTable > 1)
                score.Multiply($"TABLE {TableInviteScreen.Roman(ctx.Tables.CurrentTable)}", ctx.Tables.RewardMultiplier);

            if (ctx.Streak.IsLive)
                score.Multiply($"STREAK X{ctx.Streak.Count}", ctx.Streak.Multiplier);

            float blessings = ctx.Buffs.RewardMultiplier;
            if (!Mathf.Approximately(blessings, 1f))
                score.Multiply("BLESSING", blessings);

            foreach (var encounter in ctx.Bosses.Encounters)
                score.Multiply(encounter.Config.displayName.ToUpperInvariant(), encounter.RewardMultiplier);

            ctx.Pledges.ContributeMultTerms(score);

            // Sins that shrink rewards get the last word, as a reduction.
            float before = score.Resolve();
            float after = ctx.Bosses.ModifyRewardMultiplier(before);
            if (!Mathf.Approximately(before, after) && before > 0f)
                score.Multiply("THE GAZE", after / before);

            score.Resolve();
            return score;
        }

        public static OutcomeResult Apply(GameContext ctx, SegmentConfig seg)
        {
            var t = ctx.Config.Tuning;
            var score = BuildScore(ctx);
            float rewardMult = score.Mult;

            // Widow's Ring inverts the wheel's whole risk language: what would
            // wound you pays instead, at a discount.
            if (seg.IsRisk && ctx.Pledges.RiskWedgesPayCoin && seg.ParsedType != SegmentType.SinSummon)
            {
                int paid = Mathf.RoundToInt(seg.EffectiveAmount * ctx.Pledges.RiskCoinPercent / 100f * rewardMult);
                paid = Mathf.Max(1, paid);
                ctx.Wallet.AddRunCoins(paid);
                ctx.Tables.RecordCoinsEarned(paid);
                score.Take = Mathf.RoundToInt(seg.EffectiveAmount * ctx.Pledges.RiskCoinPercent / 100f);
                score.Total = paid;
                return new OutcomeResult
                {
                    Type = SegmentType.Coins,
                    Text = $"+{paid} COINS",
                    Color = Palette.Gold,
                    Score = score
                };
            }

            switch (seg.ParsedType)
            {
                case SegmentType.Coins:
                {
                    int gain = Mathf.RoundToInt(seg.EffectiveAmount * rewardMult);
                    gain = ctx.Bosses.ModifyCoinGain(gain); // Greed's tithe hook
                    ctx.Wallet.AddRunCoins(gain);
                    ctx.Tables.RecordCoinsEarned(gain);
                    score.Take = Mathf.RoundToInt(seg.EffectiveAmount);
                    score.Total = gain;
                    return new OutcomeResult
                    {
                        Type = seg.ParsedType,
                        Text = $"+{gain} COINS",
                        Color = Palette.Gold,
                        BigHit = seg.EffectiveAmount >= 50,
                        Score = score
                    };
                }

                case SegmentType.Xp:
                {
                    int gain = Mathf.RoundToInt(seg.EffectiveAmount * rewardMult);
                    ctx.Xp.AddXp(gain);
                    return new OutcomeResult { Type = seg.ParsedType, Text = $"+{gain} XP", Color = Palette.Teal };
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
                    ctx.Tables.RecordCoinsEarned(gain);
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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    public enum InterludeResult
    {
        Success,
        Partial,
        Fail
    }

    /// <summary>
    /// Picks which mini-games get offered and pays out what they win. The
    /// rotation rules matter more than the games: never the same one twice
    /// running, the full set before any repeat, and never one whose sin is
    /// currently at the table.
    /// </summary>
    public sealed class InterludeSystem
    {
        private readonly GameContext _ctx;
        private readonly List<string> _rotation = new List<string>();
        private string _lastOffered;

        public InterludeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public InterludesConfig Config => _ctx.Config.Interludes;
        public int SkipReward => Config.skipReward;

        public InterludeConfig Get(string id) => Config.interludes.FirstOrDefault(i => i.id == id);

        public void ResetForRun()
        {
            _rotation.Clear();
            _lastOffered = null;
        }

        /// <summary>Two of seven. Refills and reshuffles the rotation when it runs dry.</summary>
        public List<InterludeConfig> Offer()
        {
            var offers = new List<InterludeConfig>();
            var activeSins = _ctx.Bosses.ActiveSinIds();

            for (int guard = 0; guard < 32 && offers.Count < Config.offerCount; guard++)
            {
                if (_rotation.Count == 0) RefillRotation();
                if (_rotation.Count == 0) break;

                string id = _rotation[0];
                _rotation.RemoveAt(0);

                var cfg = Get(id);
                if (cfg == null) continue;
                // Never offer a mini-game whose sin is at the table: thematic,
                // and it stops the player reading its reward as an encounter effect.
                if (activeSins.Contains(cfg.sin)) continue;
                if (offers.Count == 0 && id == _lastOffered && _rotation.Count > 0) continue;
                if (offers.Any(o => o.id == id)) continue;

                offers.Add(cfg);
            }

            if (offers.Count > 0) _lastOffered = offers[0].id;
            return offers;
        }

        private void RefillRotation()
        {
            _rotation.Clear();
            foreach (var cfg in Config.interludes.OrderBy(_ => _ctx.Rng.Next()))
                _rotation.Add(cfg.id);
        }

        /// <summary>Skip is always available, and deliberately worth less than an average play.</summary>
        public void Skip()
        {
            int reward = Mathf.RoundToInt(SkipReward * _ctx.Tables.RewardMultiplier);
            _ctx.Wallet.AddRunCoins(reward);
            _ctx.Tables.RecordCoinsEarned(reward);
            _ctx.Hud?.Toast($"WALKED PAST +{reward}", Palette.Dim);
            _ctx.Analytics.Track("interlude_skipped", "table", _ctx.Tables.CurrentTable);
        }

        /// <summary>
        /// Pay out a finished mini-game. Failure always costs something small
        /// and never ends the run — an interlude is upside with a price.
        /// </summary>
        public void Resolve(InterludeConfig cfg, InterludeResult result, float score, bool sideTable)
        {
            float mult = _ctx.Tables.RewardMultiplier;
            string message;
            Color color;

            switch (cfg.id)
            {
                case "ember":
                    if (result == InterludeResult.Fail)
                    {
                        float dmg = _ctx.Health.ApplyDamage(12f);
                        message = $"BURNED -{Mathf.RoundToInt(dmg)} HP";
                        color = Palette.Blood;
                    }
                    else
                    {
                        int coins = Mathf.RoundToInt(Mathf.Lerp(40f, 140f, score) * mult);
                        Award(coins);
                        message = $"EMBER +{coins}";
                        color = Palette.Gold;
                    }
                    break;

                case "mirror":
                    if (result == InterludeResult.Success)
                    {
                        var slots = _ctx.Ring.TemperableSlots();
                        if (slots.Count > 0)
                        {
                            _ctx.Ring.TemperWedge(slots[_ctx.Rng.Next(slots.Count)], 1);
                            message = "A WEDGE TEMPERED";
                        }
                        else
                        {
                            int coins = Mathf.RoundToInt(90f * mult);
                            Award(coins);
                            message = $"NOTHING TO TEMPER +{coins}";
                        }
                        color = Palette.Teal;
                    }
                    else
                    {
                        message = "THE GLASS IS BLANK";
                        color = Palette.Dim;
                    }
                    break;

                case "shell":
                    if (result == InterludeResult.Success)
                    {
                        _ctx.Run.GuaranteedRewardSpins++;
                        message = "NEXT SPIN IS KIND";
                        color = Palette.Teal;
                    }
                    else
                    {
                        _ctx.Notice.Add(1f);
                        message = "LOST IT - THE EYE OPENS";
                        color = Palette.Sickly;
                    }
                    break;

                case "feast":
                    if (result == InterludeResult.Fail)
                    {
                        int lost = _ctx.Wallet.LoseRunCoinsPercent(25f);
                        message = lost > 0 ? $"GORGED -{lost}" : "NOTHING LEFT TO TAKE";
                        color = Palette.Blood;
                    }
                    else
                    {
                        int coins = Mathf.RoundToInt(Mathf.Lerp(30f, 200f, score) * mult);
                        Award(coins);
                        message = $"FEASTED +{coins}";
                        color = Palette.Gold;
                    }
                    break;

                case "toll":
                    if (result == InterludeResult.Fail)
                    {
                        message = "OUT OF TIME";
                        color = Palette.Dim;
                    }
                    else
                    {
                        int coins = Mathf.RoundToInt(Mathf.Lerp(30f, 160f, score) * mult);
                        Award(coins);
                        message = $"TOLL PAID +{coins}";
                        color = Palette.Gold;
                    }
                    break;

                case "vigil":
                    if (result == InterludeResult.Success)
                    {
                        _ctx.Run.TableCooldownBonus = 0.4f;
                        message = "THE HOUR IS YOURS";
                        color = Palette.Teal;
                    }
                    else
                    {
                        _ctx.Notice.Add(1f);
                        message = "YOU BLINKED";
                        color = Palette.Sickly;
                    }
                    break;

                default: // understudy
                    if (result == InterludeResult.Success)
                    {
                        _ctx.Run.ForeseeSpins(_ctx, 3);
                        message = "YOU SEE THREE AHEAD";
                        color = Palette.Purple;
                    }
                    else
                    {
                        _ctx.Notice.Add(1f);
                        message = "IT LOOKED THE SAME";
                        color = Palette.Sickly;
                    }
                    break;
            }

            // The Side Table exists to give the player a hand on the one dial
            // they otherwise only watch move.
            if (sideTable && result != InterludeResult.Fail)
            {
                _ctx.Notice.Add(-Config.sideTableNoticeRelief);
                message += " - EYE CLOSES";
            }

            _ctx.Hud?.Toast(message, color);
            _ctx.Analytics.Track("interlude_played",
                "id", cfg.id, "result", result.ToString(), "score", Mathf.RoundToInt(score * 100),
                "table", _ctx.Tables.CurrentTable, "side_table", sideTable);
        }

        private void Award(int coins)
        {
            _ctx.Wallet.AddRunCoins(coins);
            _ctx.Tables.RecordCoinsEarned(coins);
        }
    }
}

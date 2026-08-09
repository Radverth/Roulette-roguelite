using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    // narrative_lines.json shapes — field names match the JSON keys exactly.

    [Serializable]
    public class CroupierLines
    {
        public string[] first_ever;
        public string[] run_start;
        public string[] run_start_late;
        public string[] bank;
        public string[] bust;
        public string[] bust_deep;
    }

    [Serializable]
    public class SinLines
    {
        public string[] arrival;
        public string[] taunt;
        public string[] humility_progress;
        public string[] defeated;
        public string[] expired;
        public string[] player_fled;
    }

    [Serializable]
    public class ReactiveLine
    {
        public string speaker;
        public string line;
    }

    [Serializable]
    public class ReactiveLines
    {
        public ReactiveLine banked_instantly_thrice;
        public ReactiveLine never_banked_this_run;
        public ReactiveLine same_sin_third_time;
        public ReactiveLine survived_all_seven;
        public ReactiveLine bust_at_high_purse;
    }

    [Serializable]
    public class FragmentLines
    {
        public string pride_3;
        public string greed_3;
        public string wrath_3;
        public string envy_3;
        public string lust_3;
        public string gluttony_3;
        public string sloth_3;
        public string all_7;
    }

    [Serializable]
    public class NarrativeConfig
    {
        public CroupierLines croupier;
        public SinLines pride;
        public SinLines greed;
        public SinLines wrath;
        public SinLines envy;
        public SinLines lust;
        public SinLines gluttony;
        public SinLines sloth;
        public ReactiveLines reactive;
        public FragmentLines fragments;
    }

    /// <summary>
    /// Voice of the wheel. Design rule: narrative never gates a spin — it
    /// arrives beside play (plates over the wheel, quotes on the ledger),
    /// never in front of it. Taunts draw without replacement per encounter so
    /// nothing repeats within one visit.
    /// </summary>
    public sealed class NarrativeSystem
    {
        private readonly GameContext _ctx;
        private readonly NarrativeConfig _cfg;

        private readonly List<string> _tauntPool = new List<string>();
        private string _runEndQuote;
        private int _runEndQuotePriority;
        private string _pendingFragment;

        public NarrativeSystem(GameContext ctx)
        {
            _ctx = ctx;
            TextAsset asset = Resources.Load<TextAsset>("Narrative/narrative_lines");
            if (asset == null)
            {
                Debug.LogError("[Narrative] Missing Resources/Narrative/narrative_lines.json");
                _cfg = new NarrativeConfig();
            }
            else
            {
                _cfg = JsonUtility.FromJson<NarrativeConfig>(asset.text) ?? new NarrativeConfig();
            }
        }

        // --- Croupier bookends ---

        public string RunStartLine()
        {
            var c = _cfg.croupier;
            if (c == null) return "";
            if (_ctx.Save.Data.runsCompleted == 0)
                return Pick(c.first_ever);
            if (_ctx.Save.Data.runsCompleted >= 40 && _ctx.Rng.NextDouble() < 0.35)
                return Pick(c.run_start_late);
            return Pick(c.run_start);
        }

        /// <summary>Croupier's verdict for the ledger screen, unless something more specific claimed it.</summary>
        public void ChooseRunEndQuote(bool banked, int purseAtEnd)
        {
            var c = _cfg.croupier;
            if (c == null) return;

            if (banked)
            {
                SetRunEndQuote(Pick(c.bank), 1);
            }
            else if (purseAtEnd >= 150 && _cfg.reactive?.bust_at_high_purse != null)
            {
                SetRunEndQuote(_cfg.reactive.bust_at_high_purse.line, 3);
            }
            else if (purseAtEnd >= 80)
            {
                SetRunEndQuote(Pick(c.bust_deep), 1);
            }
            else
            {
                SetRunEndQuote(Pick(c.bust), 1);
            }
        }

        /// <summary>Higher priority wins the single ledger-quote slot (reactive 3 > fled 2 > bookend 1).</summary>
        public void SetRunEndQuote(string line, int priority)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (_runEndQuote != null && priority <= _runEndQuotePriority) return;
            _runEndQuote = line;
            _runEndQuotePriority = priority;
        }

        public string ConsumeRunEndQuote()
        {
            string q = _runEndQuote;
            _runEndQuote = null;
            _runEndQuotePriority = 0;
            return q;
        }

        public ReactiveLines Reactive => _cfg.reactive;

        // --- Encounter voice ---

        public void BeginEncounter(string sinId, bool thirdVisit)
        {
            _tauntPool.Clear();
            var lines = SinLinesFor(sinId);
            if (lines?.taunt != null)
                _tauntPool.AddRange(lines.taunt.OrderBy(_ => _ctx.Rng.Next()));

            // "You again." — the sin recognises a regular.
            if (thirdVisit && _cfg.reactive?.same_sin_third_time != null)
                _tauntPool.Insert(0, _cfg.reactive.same_sin_third_time.line);
        }

        /// <summary>Next taunt, drawn without replacement. Null once the pool is dry.</summary>
        public string NextTaunt()
        {
            if (_tauntPool.Count == 0) return null;
            string line = _tauntPool[0];
            _tauntPool.RemoveAt(0);
            return line;
        }

        public string EncounterEndLine(string sinId, string outcome)
        {
            var lines = SinLinesFor(sinId);
            if (lines == null) return "";
            switch (outcome)
            {
                case "defeated": return Pick(lines.defeated);
                case "expired": return Pick(lines.expired);
                case "player_fled": return Pick(lines.player_fled);
                default: return "";
            }
        }

        // --- Fragments (the long game) ---

        public string FragmentFor(string sinId)
        {
            var f = _cfg.fragments;
            if (f == null) return "";
            switch (sinId)
            {
                case "pride": return f.pride_3;
                case "greed": return f.greed_3;
                case "wrath": return f.wrath_3;
                case "envy": return f.envy_3;
                case "lust": return f.lust_3;
                case "gluttony": return f.gluttony_3;
                case "sloth": return f.sloth_3;
                case "all": return f.all_7;
                default: return "";
            }
        }

        public void SetPendingFragment(string text) => _pendingFragment = text;

        public string ConsumePendingFragment()
        {
            string f = _pendingFragment;
            _pendingFragment = null;
            return f;
        }

        public int FragmentCount => _ctx.Save.Data.unlockedFragments.Count;
        public const int TotalFragments = 8;

        // --- Speakers ---

        public static string SpeakerName(string speakerId)
        {
            return speakerId == "croupier" ? "THE CROUPIER" : speakerId.ToUpperInvariant();
        }

        public Color SpeakerColor(string speakerId)
        {
            if (speakerId == "croupier") return Palette.Bone;
            var sin = _ctx.Config.Sins.sins.FirstOrDefault(s => s.id == speakerId);
            if (sin != null && ColorUtility.TryParseHtmlString(sin.colorHex, out Color c)) return c;
            return Palette.Purple;
        }

        private SinLines SinLinesFor(string sinId)
        {
            switch (sinId)
            {
                case "pride": return _cfg.pride;
                case "greed": return _cfg.greed;
                case "wrath": return _cfg.wrath;
                case "envy": return _cfg.envy;
                case "lust": return _cfg.lust;
                case "gluttony": return _cfg.gluttony;
                case "sloth": return _cfg.sloth;
                default: return null;
            }
        }

        private string Pick(string[] pool)
        {
            if (pool == null || pool.Length == 0) return "";
            return pool[_ctx.Rng.Next(pool.Length)];
        }
    }
}

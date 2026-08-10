using System;
using System.Collections.Generic;

namespace SinWheel
{
    /// <summary>One rung of the descent. States absolute effects, not deltas.</summary>
    [Serializable]
    public class TableConfig
    {
        public int index = 1;
        public string name;
        public string blurb;
        public float rewardMultiplier = 1f;
        public int extraRiskWedges;
        public float noticeRateMultiplier = 1f;
        public int maxActiveSins = 1;
        public bool restoresResilience = true;
        public bool cursedWedgePerTable;
        public bool croupierSeated;
    }

    [Serializable]
    public class TablesConfig
    {
        public int thresholdBase = 340;
        public float thresholdGrowth = 1.4f;
        public int croupierTable = 7;
        public List<TableConfig> tables = new List<TableConfig>();
    }

    /// <summary>A permanent, earned difficulty step. Taken in order, never reversible.</summary>
    [Serializable]
    public class MarkConfig
    {
        public int index = 1;
        public string name;
        public string description;
        public float quotaPercent;
        public int extraRiskWedges;
        public int sinDurationBonus;
        public float noticeStartPercent;
        public int titheNoticeMultiplier = 1;
        public int breakTargetBonus;
        public int croupierFromTable;
    }

    [Serializable]
    public class MarksConfig
    {
        public int debtIntervalPerMark = 1200;
        public List<MarkConfig> marks = new List<MarkConfig>();
    }

    [Serializable]
    public class InterludeConfig
    {
        public string id;
        public string sin;
        public string name;
        public string verb;
        public float seconds = 8f;

        public string CardSprite => "Escalation/interlude_" + id;
        public string EmblemSprite => "Escalation/emblem_" + id;
    }

    [Serializable]
    public class InterludesConfig
    {
        public int offerCount = 2;
        public int skipReward = 40;
        public float sideTableNoticeGate = 0.75f;
        public int sideTableNoticeRelief = 3;
        public List<InterludeConfig> interludes = new List<InterludeConfig>();
    }
}

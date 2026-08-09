using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Cached access to the pixel-art sprite set under Resources/Art.
    /// Paths are relative to the Art folder, e.g. "Wheel/wheel_disc".
    /// </summary>
    public static class ArtSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path)
        {
            if (Cache.TryGetValue(path, out Sprite cached)) return cached;

            Sprite sprite = Resources.Load<Sprite>("Art/" + path);
            if (sprite == null)
                Debug.LogError($"[Art] Missing sprite: Resources/Art/{path}");
            Cache[path] = sprite;
            return sprite;
        }

        /// <summary>Wedge icon for a segment: explicit config icon, else by type.</summary>
        public static Sprite IconForSegment(SegmentConfig seg)
        {
            if (!string.IsNullOrEmpty(seg.icon))
                return Get("Icons/" + seg.icon);

            switch (seg.ParsedType)
            {
                case SegmentType.Coins: return Get("Icons/seg_coin");
                case SegmentType.Xp: return Get("Icons/seg_xp");
                case SegmentType.Gems: return Get("Icons/seg_shard");
                case SegmentType.Buff: return Get("Icons/seg_buff");
                case SegmentType.Damage: return Get("Icons/seg_damage");
                case SegmentType.CoinLoss: return Get("Icons/seg_currency_loss");
                case SegmentType.Debuff: return Get("Icons/seg_debuff");
                case SegmentType.SinSummon: return Get("Icons/seg_sin_summon");
                default: return Get("Icons/seg_coin");
            }
        }

        public static Sprite SigilFor(string sinId) => Get("Sins/sigil_" + sinId);
        public static Sprite CardFor(string sinId) => Get("Sins/card_" + sinId);
    }
}

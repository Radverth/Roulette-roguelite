using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// The authored 32-colour palette, mirrored from Tools/palette32.py so
    /// runtime-generated pixels (the wheel disc, which changes size as the
    /// Forge reshapes the ring) stay inside the same palette as the baked art.
    /// Index order matters: ramps are contiguous.
    /// </summary>
    public static class Palette32
    {
        public const int VOID = 0, ABYSS = 1, INK = 2, DEEP = 3, SLATE = 4, SLATE2 = 5, SLATE3 = 6;
        public const int STEEL = 7, PALE = 8, BONE = 9, BRIGHT = 10;
        public const int BRASS_DK = 11, BRASS = 12, BRASS_LT = 13, BRASS_PALE = 14;
        public const int WINE_DK = 15, WINE = 16, WINE_LT = 17;

        private static readonly string[] Hex =
        {
            "05060B", "0A0C16", "11141F", "1A1F30", "252C42", "333C57", "47526F",
            "6A7695", "93A0BC", "C2CADD", "EDF0F8",
            "4A3B1E", "75602E", "A88C46", "D6BE7E",
            "2E0C1C", "58162F", "8C2A4E",
            "6B3FA0", "8F9B3F", "B02A44", "2E8C7A", "3C4FA8", "A85535", "4A6478",
            "9B6FD4", "C6D46A", "E05A72", "5CCBB0", "7086DA", "D98A5A", "86A2B8",
        };

        public static readonly Color32[] Colors = BuildColors();

        /// <summary>Sin accents in canonical order: pride, greed, wrath, envy, lust, gluttony, sloth.</summary>
        public static int SinBase(int sinIndex) => 18 + Mathf.Clamp(sinIndex, 0, 6);
        public static int SinLight(int sinIndex) => 25 + Mathf.Clamp(sinIndex, 0, 6);

        private static Color32[] BuildColors()
        {
            var colors = new Color32[Hex.Length];
            for (int i = 0; i < Hex.Length; i++)
            {
                colors[i] = new Color32(
                    (byte)System.Convert.ToInt32(Hex[i].Substring(0, 2), 16),
                    (byte)System.Convert.ToInt32(Hex[i].Substring(2, 2), 16),
                    (byte)System.Convert.ToInt32(Hex[i].Substring(4, 2), 16),
                    255);
            }
            return colors;
        }

        private static readonly int[,] Bayer4 =
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 },
        };

        /// <summary>Ordered 4x4 threshold: true when the lighter entry wins at this pixel.</summary>
        public static bool Dither(int x, int y, float t)
        {
            return t * 16f > Bayer4[((y % 4) + 4) % 4, ((x % 4) + 4) % 4];
        }

        /// <summary>Two adjacent ramp steps mixed by ordered dither — bands, never blends.</summary>
        public static int RampDither(int[] ramp, float t, int x, int y)
        {
            if (ramp == null || ramp.Length == 0) return VOID;
            float pos = Mathf.Clamp01(t) * (ramp.Length - 1);
            int lo = Mathf.Clamp(Mathf.FloorToInt(pos), 0, ramp.Length - 1);
            int hi = Mathf.Min(ramp.Length - 1, lo + 1);
            return Dither(x, y, pos - lo) ? ramp[hi] : ramp[lo];
        }
    }
}

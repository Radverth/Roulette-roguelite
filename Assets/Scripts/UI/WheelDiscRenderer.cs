using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Rasterises the wheel disc at 128x128 native, in polar space, for an
    /// arbitrary wedge count. The Forge turns the ring into a deck, so the disc
    /// can no longer be a baked 12-wedge sprite — but it still has to look like
    /// one. This is a port of Tools/gen_wheel_px.py's build_disc/build_flash,
    /// same palette, same dither, same seam rules.
    /// </summary>
    public static class WheelDiscRenderer
    {
        private const int N = 128;
        private const float C = (N - 1) / 2f;

        private const float RSegOut = 54f;
        private const float RSegIn = 19f;

        // Three alternating grounds so neighbouring reward wedges stay distinct,
        // and a near-void ground for risk so danger reads before it is understood.
        private static readonly int[] RewardRampA = { Palette32.INK, Palette32.DEEP, Palette32.SLATE };
        private static readonly int[] RewardRampB = { Palette32.WINE_DK, Palette32.WINE, Palette32.WINE_LT };
        private static readonly int[] RiskRamp = { Palette32.VOID, Palette32.VOID, Palette32.ABYSS };
        private static readonly int[] FlashRamp = { Palette32.PALE, Palette32.BONE, Palette32.BRIGHT };

        public static Sprite BuildDisc(IList<SegmentConfig> ring)
        {
            int count = Mathf.Max(1, ring.Count);
            float step = 360f / count;

            var px = NewCanvas();

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float r, a;
                    Polar(x, y, out r, out a);
                    if (r > RSegOut || r < RSegIn) continue;

                    int i = SegIndex(a, step, count);
                    int[] ramp = IsRisk(ring[i]) ? RiskRamp : (i % 2 == 0 ? RewardRampA : RewardRampB);

                    float t = (r - RSegIn) / (RSegOut - RSegIn);
                    Set(px, x, y, Palette32.RampDither(ramp, t * 0.92f, x, y));
                }
            }

            // Seams, drawn on top of the fill and warmed to brass beside a risk
            // wedge so the danger edge is felt at a glance.
            for (int i = 0; i < count; i++)
            {
                float a = (i * step - step * 0.5f - 90f) * Mathf.Deg2Rad;
                bool leftRisk = IsRisk(ring[i]);
                bool rightRisk = IsRisk(ring[(i - 1 + count) % count]);
                int col = (leftRisk || rightRisk) ? Palette32.BRASS_LT : Palette32.BRASS_DK;
                Line(px,
                    C + Mathf.Cos(a) * RSegIn, C + Mathf.Sin(a) * RSegIn,
                    C + Mathf.Cos(a) * RSegOut, C + Mathf.Sin(a) * RSegOut, col);
            }

            // Two measure rings — the instrument register, kept to single pixels.
            Circle(px, C, C, 34, Palette32.INK);
            Circle(px, C, C, 44, Palette32.INK);
            for (int i = 0; i < count * 2; i++)
            {
                float a = (i * (360f / (count * 2)) - 90f) * Mathf.Deg2Rad;
                Set(px, Mathf.RoundToInt(C + Mathf.Cos(a) * 44), Mathf.RoundToInt(C + Mathf.Sin(a) * 44), Palette32.BRASS_DK);
            }

            Ring(px, RSegOut, RSegOut - 1, Palette32.BRASS_DK);
            Ring(px, RSegIn + 1, RSegIn, Palette32.BRASS_DK);

            return ToSprite(px, "wheel_disc_runtime");
        }

        /// <summary>Winning-wedge highlight, drawn at the twelve o'clock position.</summary>
        public static Sprite BuildFlash(int wedgeCount)
        {
            int count = Mathf.Max(1, wedgeCount);
            float step = 360f / count;
            var px = NewCanvas();

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float r, a;
                    Polar(x, y, out r, out a);
                    if (r < RSegIn || r > RSegOut) continue;
                    if (SegIndex(a, step, count) != 0) continue;

                    float t = (r - RSegIn) / (RSegOut - RSegIn);
                    Set(px, x, y, Palette32.RampDither(FlashRamp, t, x, y));
                }
            }

            return ToSprite(px, "wheel_flash_runtime");
        }

        private static bool IsRisk(SegmentConfig seg)
        {
            return seg != null && seg.IsRisk;
        }

        // --- raster helpers (python-space y, flipped on write) ---

        private static Color32[] NewCanvas()
        {
            var px = new Color32[N * N];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            return px;
        }

        private static void Polar(int x, int y, out float r, out float a)
        {
            float dx = x - C;
            float dy = y - C;
            r = Mathf.Sqrt(dx * dx + dy * dy);
            a = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90f, 360f);
        }

        private static int SegIndex(float a, float step, int count)
        {
            return Mathf.FloorToInt(Mathf.Repeat(a + step * 0.5f, 360f) / step) % count;
        }

        private static void Set(Color32[] px, int x, int y, int paletteIndex)
        {
            if (x < 0 || y < 0 || x >= N || y >= N) return;
            px[(N - 1 - y) * N + x] = Palette32.Colors[paletteIndex];
        }

        private static void Line(Color32[] px, float fx0, float fy0, float fx1, float fy1, int idx)
        {
            int x0 = Mathf.RoundToInt(fx0), y0 = Mathf.RoundToInt(fy0);
            int x1 = Mathf.RoundToInt(fx1), y1 = Mathf.RoundToInt(fy1);
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                Set(px, x0, y0, idx);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static void Circle(Color32[] px, float cx, float cy, int radius, int idx)
        {
            int x = radius, y = 0, d = 1 - radius;
            while (x >= y)
            {
                int icx = Mathf.RoundToInt(cx), icy = Mathf.RoundToInt(cy);
                Set(px, icx + x, icy + y, idx); Set(px, icx + y, icy + x, idx);
                Set(px, icx - x, icy + y, idx); Set(px, icx - y, icy + x, idx);
                Set(px, icx + x, icy - y, idx); Set(px, icx + y, icy - x, idx);
                Set(px, icx - x, icy - y, idx); Set(px, icx - y, icy - x, idx);
                y++;
                if (d < 0) d += 2 * y + 1;
                else { x--; d += 2 * (y - x) + 1; }
            }
        }

        private static void Ring(Color32[] px, float rOut, float rIn, int idx)
        {
            int lo = Mathf.FloorToInt(C - rOut), hi = Mathf.CeilToInt(C + rOut);
            for (int y = lo; y <= hi; y++)
            {
                for (int x = lo; x <= hi; x++)
                {
                    float dx = x - C, dy = y - C;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= rIn * rIn && d2 <= rOut * rOut)
                        Set(px, x, y, idx);
                }
            }
        }

        private static Sprite ToSprite(Color32[] px, string name)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}

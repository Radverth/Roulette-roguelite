"""Sin Wheel — pixel art foundation.

Discipline, in order of importance:
  1. Every sprite is drawn at its NATIVE resolution. Nothing is downsampled.
  2. Every pixel is snapped to the 32-colour palette. No stray colours, ever.
  3. No anti-aliasing anywhere. Edges are decided, not averaged.
  4. Gradients are ordered dither between two palette entries, never blends.
  5. Upscaling for preview is nearest-neighbour at integer factors only.

Palette: dark, cool, occult. Cold slate neutrals, a bone ramp, muted cold
brass, wine, and seven sin accents each with one highlight.
"""
from PIL import Image, ImageDraw
import math
import os

# ----------------------------------------------------------- the palette ---
# 32 entries. Index order matters: ramps are contiguous so shading is index
# arithmetic rather than guesswork.

PALETTE_HEX = [
    # 0-6  cold neutral ramp, void -> slate
    "05060B", "0A0C16", "11141F", "1A1F30", "252C42", "333C57", "47526F",
    # 7-10 bone ramp
    "6A7695", "93A0BC", "C2CADD", "EDF0F8",
    # 11-14 cold brass
    "4A3B1E", "75602E", "A88C46", "D6BE7E",
    # 15-17 wine
    "2E0C1C", "58162F", "8C2A4E",
    # 18-24 sin base
    "6B3FA0",  # pride    violet
    "8F9B3F",  # greed    sickly gold-green
    "B02A44",  # wrath    cold crimson
    "2E8C7A",  # envy     teal
    "3C4FA8",  # lust     indigo
    "A85535",  # gluttony rust
    "4A6478",  # sloth    steel blue
    # 25-31 sin highlight
    "9B6FD4", "C6D46A", "E05A72", "5CCBB0", "7086DA", "D98A5A", "86A2B8",
]


def _rgb(h):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


PAL = [_rgb(h) for h in PALETTE_HEX]
TRANSPARENT = (0, 0, 0, 0)

# Named indices — reads far better than magic numbers in the generators.
VOID, ABYSS, INK, DEEP, SLATE, SLATE2, SLATE3 = 0, 1, 2, 3, 4, 5, 6
STEEL, PALE, BONE, BRIGHT = 7, 8, 9, 10
BRASS_DK, BRASS, BRASS_LT, BRASS_PALE = 11, 12, 13, 14
WINE_DK, WINE, WINE_LT = 15, 16, 17

SIN_ORDER = ["pride", "greed", "wrath", "envy", "lust", "gluttony", "sloth"]
SIN_BASE = {n: 18 + i for i, n in enumerate(SIN_ORDER)}
SIN_LIGHT = {n: 25 + i for i, n in enumerate(SIN_ORDER)}

SIN_EPITHETS = {
    "pride": "THE MIRROR", "greed": "THE TITHE", "wrath": "THE FORGE",
    "envy": "THE ECHO", "lust": "THE SHUFFLE", "gluttony": "THE MAW",
    "sloth": "THE WEIGHT",
}

# Ramps used for shading a surface across 3-4 discrete steps.
NEUTRAL_RAMP = [VOID, ABYSS, INK, DEEP, SLATE, SLATE2, SLATE3, STEEL, PALE, BONE, BRIGHT]
BRASS_RAMP = [BRASS_DK, BRASS, BRASS_LT, BRASS_PALE]
WINE_RAMP = [WINE_DK, WINE, WINE_LT]


def sin_ramp(name):
    """Four-step ramp for one sin: shadow, base, light, bone tip."""
    return [INK, SIN_BASE[name], SIN_LIGHT[name], BONE]


# ------------------------------------------------------------- surfaces ---

class Px:
    """A native-resolution indexed canvas. Every write is a palette index or
    None for transparent, which makes stray colours structurally impossible."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.buf = [[None] * w for _ in range(h)]

    def set(self, x, y, idx):
        x, y = int(x), int(y)
        if idx is None or x < 0 or y < 0 or x >= self.w or y >= self.h:
            return
        self.buf[y][x] = idx

    def get(self, x, y):
        x, y = int(x), int(y)
        if x < 0 or y < 0 or x >= self.w or y >= self.h:
            return None
        return self.buf[y][x]

    def fill(self, idx):
        for y in range(self.h):
            for x in range(self.w):
                self.buf[y][x] = idx

    def rect(self, x0, y0, x1, y1, idx):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, idx)

    def frame(self, x0, y0, x1, y1, idx):
        for x in range(int(x0), int(x1) + 1):
            self.set(x, y0, idx); self.set(x, y1, idx)
        for y in range(int(y0), int(y1) + 1):
            self.set(x0, y, idx); self.set(x1, y, idx)

    def hline(self, x0, x1, y, idx):
        if x1 < x0: x0, x1 = x1, x0
        for x in range(int(x0), int(x1) + 1):
            self.set(x, y, idx)

    def vline(self, x, y0, y1, idx):
        if y1 < y0: y0, y1 = y1, y0
        for y in range(int(y0), int(y1) + 1):
            self.set(x, y, idx)

    def line(self, x0, y0, x1, y1, idx):
        """Bresenham. Single-pixel-wide, no anti-aliasing."""
        x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy
        while True:
            self.set(x0, y0, idx)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy; x0 += sx
            if e2 <= dx:
                err += dx; y0 += sy

    def circle(self, cx, cy, r, idx):
        """Midpoint circle outline, one pixel thick."""
        x, y, d = int(r), 0, 1 - int(r)
        while x >= y:
            for sx, sy in ((x, y), (y, x), (-x, y), (-y, x),
                           (x, -y), (y, -x), (-x, -y), (-y, -x)):
                self.set(cx + sx, cy + sy, idx)
            y += 1
            if d < 0:
                d += 2 * y + 1
            else:
                x -= 1
                d += 2 * (y - x) + 1

    def disc(self, cx, cy, r, idx):
        r2 = r * r
        for y in range(int(cy - r), int(cy + r) + 1):
            for x in range(int(cx - r), int(cx + r) + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r2:
                    self.set(x, y, idx)

    def ring(self, cx, cy, r_out, r_in, idx):
        for y in range(int(cy - r_out), int(cy + r_out) + 1):
            for x in range(int(cx - r_out), int(cx + r_out) + 1):
                d2 = (x - cx) ** 2 + (y - cy) ** 2
                if r_in * r_in <= d2 <= r_out * r_out:
                    self.set(x, y, idx)

    def blit(self, other, ox, oy, skip_none=True):
        for y in range(other.h):
            for x in range(other.w):
                v = other.buf[y][x]
                if v is None and skip_none:
                    continue
                self.set(ox + x, oy + y, v)

    def outline(self, idx, only_over=None):
        """Add a 1px border around every non-empty pixel. The single most
        effective way to make a sprite read against any background."""
        adds = []
        for y in range(self.h):
            for x in range(self.w):
                if self.buf[y][x] is not None:
                    continue
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    v = self.get(nx, ny)
                    if v is not None and (only_over is None or v in only_over):
                        adds.append((x, y))
                        break
        for x, y in adds:
            self.set(x, y, idx)

    def to_image(self):
        img = Image.new("RGBA", (self.w, self.h), TRANSPARENT)
        px = img.load()
        for y in range(self.h):
            for x in range(self.w):
                v = self.buf[y][x]
                px[x, y] = PAL[v] if v is not None else TRANSPARENT
        return img


# --------------------------------------------------------------- dither ---

BAYER4 = [
    [0, 8, 2, 10],
    [12, 4, 14, 6],
    [3, 11, 1, 9],
    [15, 7, 13, 5],
]


def dither(x, y, t):
    """Ordered 4x4 threshold. Returns True when the lighter of two palette
    entries should win at this pixel for blend factor t in 0..1."""
    return t * 16.0 > BAYER4[int(y) % 4][int(x) % 4]


def ramp_pick(ramp, t):
    """Snap a 0..1 value onto a discrete ramp. Bands, not blends."""
    if not ramp:
        return None
    i = int(t * len(ramp))
    return ramp[max(0, min(len(ramp) - 1, i))]


def ramp_dither(ramp, t, x, y):
    """Two adjacent ramp steps mixed by ordered dither — gives the illusion of
    more shades than the palette holds without ever leaving it."""
    if not ramp:
        return None
    pos = t * (len(ramp) - 1)
    lo = max(0, min(len(ramp) - 1, int(math.floor(pos))))
    hi = min(len(ramp) - 1, lo + 1)
    return ramp[hi] if dither(x, y, pos - lo) else ramp[lo]


# ------------------------------------------------------------- pixelmaps ---

def parse_map(rows, key):
    """Build a Px from ASCII art. '.' and ' ' are transparent; every other
    character maps through `key` to a palette index.

    Hand-placed pixels are how icons at 16px get to look intentional rather
    than like a shrunken drawing."""
    h = len(rows)
    w = max(len(r) for r in rows)
    p = Px(w, h)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in ".  ":
                continue
            if ch in key:
                p.set(x, y, key[ch])
    return p


# ----------------------------------------------------------------- io ---

def save(px_or_img, path, scale=1):
    img = px_or_img.to_image() if isinstance(px_or_img, Px) else px_or_img
    if scale > 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, "PNG", optimize=True)
    return path


def verify_palette(img):
    """Fail loudly if anything escaped the palette."""
    allowed = {p for p in PAL} | {TRANSPARENT}
    bad = set()
    for c in img.convert("RGBA").getdata():
        if c not in allowed:
            bad.add(c)
    return bad


def sheet(images, cols, cell, bg=PAL[ABYSS], scale=1, pad=4):
    """Contact sheet for visual review, nearest-neighbour only."""
    rows = (len(images) + cols - 1) // cols
    step = cell * scale + pad
    out = Image.new("RGBA", (cols * step + pad, rows * step + pad), bg)
    for i, im in enumerate(images):
        if isinstance(im, Px):
            im = im.to_image()
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        out.alpha_composite(im, ((i % cols) * step + pad, (i // cols) * step + pad))
    return out

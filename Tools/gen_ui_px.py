"""Boss cards, UI kit, particles, branding — and the 5x7 bitmap font they need.

The font is shipped as an asset in its own right: the HUD numerals, card names
and banners all use it, so the whole set stays in one visual language instead
of pairing pixel art with a smooth system typeface.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *
from gen_icons_sigils_px import SIGILS, ICONS

ART = "Assets/Resources/Art"

# ------------------------------------------------------------- 5x7 font ---

GLYPHS = {
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "B": ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    "C": ["01110", "10001", "10000", "10000", "10000", "10001", "01110"],
    "D": ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "F": ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
    "G": ["01110", "10001", "10000", "10111", "10001", "10001", "01111"],
    "H": ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
    "I": ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
    "J": ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
    "K": ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    "M": ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    "N": ["10001", "11001", "10101", "10101", "10011", "10001", "10001"],
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "P": ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
    "Q": ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "U": ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
    "V": ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
    "W": ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
    "X": ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
    "Y": ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
    "Z": ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
    "0": ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
    "1": ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
    "2": ["01110", "10001", "00001", "00110", "01000", "10000", "11111"],
    "3": ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
    "4": ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
    "5": ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
    "6": ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
    "7": ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
    "8": ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
    "9": ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
    "/": ["00001", "00010", "00010", "00100", "01000", "01000", "10000"],
    "-": ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
    ".": ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
    ",": ["00000", "00000", "00000", "00000", "01100", "00100", "01000"],
    "'": ["00100", "00100", "01000", "00000", "00000", "00000", "00000"],
    "!": ["00100", "00100", "00100", "00100", "00100", "00000", "00100"],
    "&": ["01100", "10010", "10100", "01000", "10101", "10010", "01101"],
    "+": ["00000", "00100", "00100", "11111", "00100", "00100", "00000"],
    ":": ["00000", "01100", "01100", "00000", "01100", "01100", "00000"],
    "%": ["11001", "11010", "00010", "00100", "01000", "01011", "10011"],
    "?": ["01110", "10001", "00001", "00110", "00100", "00000", "00100"],
    " ": ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
}

FW, FH = 5, 7


def text_width(s, tracking=1):
    return len(s) * (FW + tracking) - tracking if s else 0


def draw_text(p, s, x, y, idx, tracking=1, shadow=None):
    """Blit a string. Shadow is a one-pixel offset in a darker index — the
    cheapest way to keep small type legible over busy art."""
    cx = x
    for ch in s.upper():
        g = GLYPHS.get(ch)
        if g is None:
            cx += FW + tracking
            continue
        for gy, row in enumerate(g):
            for gx, bit in enumerate(row):
                if bit != "1":
                    continue
                if shadow is not None:
                    p.set(cx + gx + 1, y + gy + 1, shadow)
                p.set(cx + gx, y + gy, idx)
        cx += FW + tracking
    return cx


def draw_text_center(p, s, cx, y, idx, tracking=1, shadow=None):
    draw_text(p, s, int(cx - text_width(s, tracking) / 2), y, idx, tracking, shadow)


def wrap(s, max_chars):
    words, lines, cur = s.split(), [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if len(t) > max_chars and cur:
            lines.append(cur); cur = w
        else:
            cur = t
    if cur:
        lines.append(cur)
    return lines


def build_font_sheet():
    """16 columns, one glyph per cell, 8x8 cells. Importable as a Unity sprite
    sheet or usable directly by the runtime text renderer."""
    order = [c for c in GLYPHS if c != " "]
    cols = 16
    rows = (len(order) + cols - 1) // cols
    p = Px(cols * 8, rows * 8)
    for i, ch in enumerate(order):
        ox, oy = (i % cols) * 8 + 1, (i // cols) * 8 + 1
        for gy, row in enumerate(GLYPHS[ch]):
            for gx, bit in enumerate(row):
                if bit == "1":
                    p.set(ox + gx, oy + gy, BONE)
    return p, order


# ------------------------------------------------------------ boss cards ---

CW, CH = 96, 144

# Kept to three lines of fifteen. On a card this size, copy that overflows the
# border is worse than copy that says less.
EFFECT = {
    "pride":    "REWARDS NARROW LAND THREE HUMILITIES",
    "greed":    "A THIRD OF EACH COIN FEEDS THE JACKPOT",
    "wrath":    "TWO MORE WEDGES OPEN ONTO FIRE",
    "envy":     "YOUR BEST REWARD TURNS ON YOU",
    "lust":     "THE WEDGES CHANGE PLACES EVERY THIRD",
    "gluttony": "EVERY SPIN EATS WHAT YOU BANKED",
    "sloth":    "A SLOW WHEEL SPIN TO BUY IT BACK",
}


def boss_card(name):
    b, l = SIN_BASE[name], SIN_LIGHT[name]
    p = Px(CW, CH)

    # Ground: vertical band wash, dithered, with a bloom behind the sigil
    for y in range(CH):
        t = y / CH
        for x in range(CW):
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS, VOID], t * 0.95, x, y))
    for y in range(CH):
        for x in range(CW):
            d = math.hypot(x - CW / 2, y - 50) / 44.0
            if d < 1.0 and dither(x, y, (1.0 - d) * 0.30):
                p.set(x, y, b)

    # Double engraved border with corner keys
    p.frame(2, 2, CW - 3, CH - 3, b)
    p.frame(4, 4, CW - 5, CH - 5, INK)
    for cx, cy, sx, sy in ((2, 2, 1, 1), (CW - 3, 2, -1, 1),
                           (2, CH - 3, 1, -1), (CW - 3, CH - 3, -1, -1)):
        for k in range(6):
            p.set(cx + sx * k, cy, l)
            p.set(cx, cy + sy * k, l)

    # Sigil at 2x — nearest-neighbour, so it stays pixel-true
    sig = SIGILS[name]()
    for y in range(sig.h):
        for x in range(sig.w):
            v = sig.buf[y][x]
            if v is None:
                continue
            for dy in range(2):
                for dx in range(2):
                    p.set(16 + x * 2 + dx, 16 + y * 2 + dy, v)

    # Ordinal, clinical, top-left
    idx = SIN_ORDER.index(name) + 1
    draw_text(p, f"{idx:02d}/07", 8, 9, STEEL)

    # Rule, name, epithet
    y = 88
    p.hline(14, CW - 15, y, b)
    p.set(14, y, l); p.set(CW - 15, y, l)
    draw_text_center(p, name, CW / 2, y + 6, BONE, tracking=2, shadow=VOID)
    draw_text_center(p, SIN_EPITHETS[name], CW / 2, y + 17, l, tracking=1)

    for i, line in enumerate(wrap(EFFECT[name], 15)[:3]):
        draw_text_center(p, line, CW / 2, y + 28 + i * 7, STEEL, tracking=0)
    return p


# ---------------------------------------------------------------- UI kit ---

def panel(w=32, h=32, accent=BRASS_LT):
    """9-slice safe: 8px corners, tileable centre."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, ABYSS)
    p.rect(1, 1, w - 2, h - 2, INK)
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, accent)
    p.frame(3, 3, w - 4, h - 4, DEEP)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (w - 2, 1, -1, 1),
                           (1, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        for k in range(3):
            p.set(cx + sx * k, cy, BRASS_PALE)
            p.set(cx, cy + sy * k, BRASS_PALE)
    return p


def button(w=48, h=16, accent=BRASS_LT, state="idle"):
    p = Px(w, h)
    if state == "disabled":
        top, bot, edge, gloss = SLATE, INK, SLATE2, None
    elif state == "pressed":
        top, bot, edge, gloss = VOID, ABYSS, accent, None
    else:
        top, bot, edge, gloss = WINE, WINE_DK, accent, WINE_LT

    for y in range(h):
        t = y / (h - 1)
        for x in range(w):
            p.set(x, y, ramp_dither([top, bot], t, x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, edge)
    if gloss is not None:
        p.hline(3, w - 4, 2, gloss)
    # corner cuts — a chamfered plate rather than a rounded rectangle
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    p.set(3, h // 2, accent); p.set(w - 4, h // 2, accent)
    return p


def bar(w=64, h=8, kind="track", ramp=None):
    p = Px(w, h)
    if kind == "track":
        p.rect(0, 0, w - 1, h - 1, VOID)
        p.frame(0, 0, w - 1, h - 1, BRASS_DK)
        for x in range(6, w - 1, 6):
            p.vline(x, 2, h - 3, DEEP)
    else:
        ramp = ramp or [WINE_LT, WINE, WINE_DK]
        for y in range(h):
            for x in range(w):
                p.set(x, y, ramp_dither(ramp, y / (h - 1), x, y))
        p.hline(1, w - 2, 1, ramp[0])
        p.frame(0, 0, w - 1, h - 1, VOID)
    return p


def banner(w=80, h=24, accent=BRASS_LT):
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, accent)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
        p.set(cx, cy, None)
    p.hline(4, 9, h // 2, accent)
    p.hline(w - 10, w - 5, h // 2, accent)
    return p


# ------------------------------------------------------------- particles ---

def pt_spark():
    p = Px(8, 8)
    p.set(3, 0, PALE); p.set(4, 0, PALE)
    p.vline(3, 1, 6, BRIGHT); p.vline(4, 1, 6, BRIGHT)
    p.hline(1, 6, 3, BRIGHT); p.hline(1, 6, 4, BRIGHT)
    p.set(0, 3, PALE); p.set(7, 4, PALE)
    p.set(3, 7, PALE); p.set(4, 7, PALE)
    return p


def pt_ember():
    p = Px(8, 8)
    p.disc(3.5, 3.5, 3, SIN_BASE["gluttony"])
    p.disc(3.5, 3.5, 2, SIN_LIGHT["gluttony"])
    p.set(3, 3, BRIGHT)
    return p


def pt_dust():
    p = Px(4, 4)
    p.disc(1.5, 1.5, 1.4, STEEL)
    p.set(1, 1, PALE)
    return p


def pt_glow():
    p = Px(16, 16)
    c = 7.5
    for y in range(16):
        for x in range(16):
            d = math.hypot(x - c, y - c) / c
            if d > 1:
                continue
            t = (1 - d) ** 1.5
            if t > 0.66:
                p.set(x, y, BRASS_PALE)
            elif t > 0.34 and dither(x, y, (t - 0.34) / 0.32):
                p.set(x, y, BRASS_LT)
            elif t > 0.10 and dither(x, y, (t - 0.10) / 0.24):
                p.set(x, y, BRASS)
    return p


def pt_shock():
    p = Px(32, 32)
    p.circle(15.5, 15.5, 14, BRIGHT)
    p.circle(15.5, 15.5, 13, PALE)
    p.circle(15.5, 15.5, 11, STEEL)
    return p


# -------------------------------------------------------------- branding ---

def app_icon():
    """128x128 — the wheel reduced to its essentials plus the seven-point star."""
    N = 128
    C = (N - 1) / 2.0
    p = Px(N, N)
    for y in range(N):
        for x in range(N):
            d = math.hypot(x - C, y - C) / C
            p.set(x, y, ramp_dither([WINE_DK, INK, ABYSS, VOID], min(1, d * 1.1), x, y))

    risk = {1, 4, 7, 10}
    for y in range(N):
        for x in range(N):
            dx, dy = x - C, y - C
            r = math.hypot(dx, dy)
            if r > 52 or r < 14:
                continue
            a = (math.degrees(math.atan2(dy, dx)) + 90) % 360
            i = int(((a + 15) % 360) / 30) % 12
            t = (r - 14) / 38.0
            ramp = [VOID, VOID, ABYSS] if i in risk else (
                [INK, DEEP, SLATE] if i % 2 == 0 else [WINE_DK, WINE, WINE_LT])
            p.set(x, y, ramp_dither(ramp, t, x, y))

    for i in range(12):
        a = math.radians(i * 30 - 15 - 90)
        p.line(C + math.cos(a) * 14, C + math.sin(a) * 14,
               C + math.cos(a) * 52, C + math.sin(a) * 52, BRASS_DK)
    p.ring(C, C, 60, 53, BRASS)
    p.ring(C, C, 60, 58, BRASS_DK)
    p.ring(C, C, 55, 53, BRASS_LT)

    pts = [(C + math.cos(math.radians(-90 + i * 360 / 7)) * 30,
            C + math.sin(math.radians(-90 + i * 360 / 7)) * 30) for i in range(7)]
    order = [pts[(i * 3) % 7] for i in range(7)]
    for i in range(7):
        x0, y0 = order[i]; x1, y1 = order[(i + 1) % 7]
        p.line(x0, y0, x1, y1, BRASS_PALE)
        p.line(x0 + 1, y0, x1 + 1, y1, BRASS_LT)
    p.disc(C, C, 6, WINE_DK)
    p.circle(C, C, 6, BRASS_PALE)
    return p


def feature_graphic():
    """256x125 native, upscales x4 to the 1024x500 store slot."""
    W, H = 256, 125
    p = Px(W, H)
    for y in range(H):
        for x in range(W):
            t = (y / H) * 0.7 + (x / W) * 0.3
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS, VOID], t, x, y))

    disc = Px(128, 128)
    import gen_wheel_px as gw
    d, rim = gw.build_disc(), gw.build_rim()
    disc.blit(d, 0, 0); disc.blit(rim, 0, 0); disc.blit(gw.build_hub(), 48, 48)
    p.blit(disc, 152, 4)

    draw_text(p, "SIN", 20, 40, BONE, tracking=3, shadow=VOID)
    draw_text(p, "WHEEL", 20, 54, BRASS_PALE, tracking=3, shadow=VOID)
    p.hline(20, 44, 34, BRASS_LT)
    draw_text(p, "SEVEN WILL ANSWER", 20, 76, STEEL, tracking=1)
    return p


def splash():
    """135x240 native, x8 to 1080x1920."""
    W, H = 135, 240
    p = Px(W, H)
    for y in range(H):
        for x in range(W):
            d = math.hypot(x - W / 2, y - H * 0.38) / (W * 0.9)
            p.set(x, y, ramp_dither([WINE_DK, INK, ABYSS, VOID], min(1, d * 1.2), x, y))
    icon = app_icon()
    small = Px(64, 64)
    for y in range(64):
        for x in range(64):
            small.set(x, y, icon.buf[y * 2][x * 2])
    p.blit(small, (W - 64) // 2, 60)
    draw_text_center(p, "SIN WHEEL", W / 2, 145, BONE, tracking=2, shadow=VOID)
    draw_text_center(p, "SEVEN WILL ANSWER", W / 2, 160, BRASS_LT, tracking=1)
    return p


def palette_swatch():
    p = Px(8 * 8, 4 * 8)
    for i in range(32):
        ox, oy = (i % 8) * 8, (i // 8) * 8
        p.rect(ox, oy, ox + 7, oy + 7, i)
    return p


if __name__ == "__main__":
    font, order = build_font_sheet()
    save(font, f"{ART}/UI/font_5x7.png")
    with open(f"{ART}/UI/font_5x7_order.txt", "w") as f:
        f.write("16 cols, 8x8 cells, glyph at +1,+1\n" + "".join(order) + "\n")

    cards = []
    for n in SIN_ORDER:
        c = boss_card(n)
        save(c, f"{ART}/Sins/card_{n}.png")
        cards.append(c)

    save(panel(), f"{ART}/UI/panel.png")
    save(panel(accent=SIN_LIGHT["wrath"]), f"{ART}/UI/panel_danger.png")
    for st in ("idle", "pressed", "disabled"):
        save(button(state=st), f"{ART}/UI/button_primary_{st}.png")
    save(button(accent=STEEL), f"{ART}/UI/button_secondary_idle.png")
    save(bar(kind="track"), f"{ART}/UI/bar_track.png")
    save(bar(kind="fill", ramp=[WINE_LT, WINE, WINE_DK]), f"{ART}/UI/bar_fill_hp.png")
    save(bar(kind="fill", ramp=[BRASS_PALE, BRASS_LT, BRASS]), f"{ART}/UI/bar_fill_xp.png")
    save(bar(kind="fill", ramp=[SIN_LIGHT["sloth"], SIN_BASE["sloth"], SLATE]),
         f"{ART}/UI/bar_fill_resist.png")
    save(banner(), f"{ART}/UI/banner_bank.png")
    save(banner(accent=SIN_LIGHT["wrath"]), f"{ART}/UI/banner_bust.png")

    save(pt_spark(), f"{ART}/Particles/spark.png")
    save(pt_ember(), f"{ART}/Particles/ember.png")
    save(pt_dust(), f"{ART}/Particles/dust.png")
    save(pt_glow(), f"{ART}/Particles/glow.png")
    save(pt_shock(), f"{ART}/Particles/ring_shock.png")

    icon = app_icon()
    save(icon, f"{ART}/Branding/app_icon_128.png")
    save(icon, f"{ART}/Branding/app_icon_512.png", scale=4)
    save(feature_graphic(), f"{ART}/Branding/feature_graphic_256x125.png")
    save(feature_graphic(), f"{ART}/Branding/feature_graphic_1024x500.png", scale=4)
    save(splash(), f"{ART}/Branding/splash_135x240.png")
    save(splash(), f"{ART}/Branding/splash_1080x1920.png", scale=8)
    save(palette_swatch(), f"{ART}/Branding/palette_32.png", scale=4)

    sheet(cards[:4], 4, 0, scale=3).save("/home/claude/px/preview_cards.png")
    row = [panel(), button(), button(state="pressed"), bar(kind="track"),
           bar(kind="fill"), banner()]
    sheet([app_icon()], 1, 128, scale=2).save("/home/claude/px/preview_icon.png")
    save(feature_graphic(), "/home/claude/px/preview_feature.png", scale=3)
    print("cards, ui, particles, branding written")

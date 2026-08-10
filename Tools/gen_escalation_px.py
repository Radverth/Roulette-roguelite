"""Sin Wheel — escalation and interlude asset set.

Two systems, one problem: the run currently has no shape. Tables give it an
arc that steepens; interludes sit on the beats of that arc rather than
interrupting it at random.

Seven tables, seven Marks, seven interludes — the number is already the game's,
so everything counts to it.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *
from gen_ui_px import draw_text, draw_text_center, text_width, wrap

OUT = "Assets/Resources/Art/Escalation"

ROMAN = ["I", "II", "III", "IV", "V", "VI", "VII"]

# Tables get colder and then hotter: brass while the house is polite, wine as
# it stops being, wrath red when the Croupier sits down.
TABLE_COLOUR = [
    (BRASS, BRASS_PALE), (BRASS, BRASS_PALE),
    (WINE, WINE_LT), (WINE, WINE_LT),
    (SIN_BASE["pride"], SIN_LIGHT["pride"]),
    (SIN_BASE["pride"], SIN_LIGHT["pride"]),
    (SIN_BASE["wrath"], SIN_LIGHT["wrath"]),
]

INTERLUDES = ["ember", "mirror", "shell", "feast", "toll", "vigil", "understudy"]
INTERLUDE_SIN = {
    "ember": "wrath", "mirror": "pride", "shell": "lust", "feast": "gluttony",
    "toll": "greed", "vigil": "sloth", "understudy": "envy",
}


# --------------------------------------------------------- table plaque ---

def table_plaque(tier=0, w=44, h=30):
    """Brass plate screwed to the table you have been moved to. The numeral is
    the only thing on it — the player should be able to read their depth from
    across the room."""
    base, light = TABLE_COLOUR[tier]
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, base)
    p.frame(3, 3, w - 4, h - 4, DEEP)
    for cx, cy in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        p.set(cx, cy, light)
    # mounting screws
    for sx, sy in ((4, 4), (w - 5, 4), (4, h - 5), (w - 5, h - 5)):
        p.set(sx, sy, light)
    draw_text_center(p, "TABLE", w / 2, 6, base, tracking=0)
    draw_text_center(p, ROMAN[tier], w / 2, 15, light, tracking=2, shadow=VOID)
    p.hline(8, w - 9, 13, base)
    return p


def table_invite(w=112, h=44):
    """The prompt when the house moves you inward. Accept or cash out — the
    single most important decision in the new loop, so it gets its own plate."""
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            d = math.hypot(x - w / 2, y - h / 2) / (w * 0.55)
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], min(1.0, d * 1.2), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(2, 2, w - 3, h - 3, BRASS_LT)
    for cx, cy in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3)):
        p.set(cx, cy, BONE)
    draw_text_center(p, "YOU ARE INVITED", w / 2, 9, BRASS_PALE, tracking=1)
    draw_text_center(p, "DEEPER", w / 2, 21, BONE, tracking=3, shadow=VOID)
    p.hline(22, w - 23, 32, BRASS_LT)
    draw_text_center(p, "STAKES RISE", w / 2, 35, STEEL, tracking=0)
    return p


def depth_pip(state="passed"):
    """One rung of the descent track along the HUD edge."""
    p = Px(10, 10)
    col = {"passed": BRASS_PALE, "current": BONE, "locked": SLATE2}[state]
    for i in range(6):
        a0 = math.radians(i * 60)
        a1 = math.radians((i + 1) * 60)
        p.line(4.5 + math.cos(a0) * 4, 4.5 + math.sin(a0) * 4,
               4.5 + math.cos(a1) * 4, 4.5 + math.sin(a1) * 4, col)
    if state == "passed":
        p.disc(4.5, 4.5, 1.8, BRASS)
    elif state == "current":
        p.disc(4.5, 4.5, 2.4, BONE)
        p.set(4, 4, BRIGHT)
    return p


# ---------------------------------------------------------------- marks ---

MARK_GLYPH = {
    0: "bar",      # quota raised
    1: "wedge",    # extra risk wedge
    2: "clock",    # sins last longer
    3: "eye",      # notice starts filled
    4: "split",    # tithe costs double
    5: "chain",    # break conditions harder
    6: "hat",      # the Croupier plays
}


def mark_seal(index=0, earned=True):
    """Permanent run modifier, awarded as the debt clears. Ascension wearing the
    game's own vocabulary — you do not pick a difficulty, the house raises it
    because you are paying."""
    S_ = 32
    p = Px(S_, S_)
    c = (S_ - 1) / 2.0
    col = SIN_BASE["wrath"] if earned else SLATE
    light = SIN_LIGHT["wrath"] if earned else SLATE2
    ink = BONE if earned else PALE

    for y in range(S_):
        for x in range(S_):
            d = math.hypot(x - c, y - c)
            if d > 14:
                continue
            p.set(x, y, ramp_dither([light, col, INK], d / 14.0, x, y))
    p.circle(c, c, 14, VOID)
    p.circle(c, c, 12, light)

    g = MARK_GLYPH[index]
    if g == "bar":                       # quota raised
        p.rect(7, 18, 24, 21, ink)
        p.rect(7, 13, 18, 16, ink)
        p.rect(7, 8, 12, 11, ink)
        p.line(21, 12, 25, 8, ink); p.line(25, 8, 25, 12, ink)
    elif g == "wedge":                   # an extra risk wedge
        p.disc(c, c, 11, VOID)
        for i in range(8):
            a0 = math.radians(i * 45 - 90)
            a1 = math.radians((i + 1) * 45 - 90)
            p.line(c + math.cos(a0) * 11, c + math.sin(a0) * 11,
                   c + math.cos(a1) * 11, c + math.sin(a1) * 11, ink)
            p.line(c, c, c + math.cos(a0) * 11, c + math.sin(a0) * 11, ink)
        for yy in range(-10, 1):
            wdt = int(abs(yy) * 0.42)
            p.hline(c - wdt, c + wdt, c + yy, ink)
    elif g == "clock":                   # sins last longer
        p.circle(c, c, 10, ink)
        p.circle(c, c, 9, ink)
        p.line(c, c, c, c - 7, ink); p.line(c + 1, c, c + 1, c - 7, ink)
        p.line(c, c, c + 6, c + 4, ink)
        p.disc(c, c, 1.6, ink)
    elif g == "eye":                     # notice starts filled
        for x in range(-11, 12):
            span = math.sqrt(max(0.0, 1 - (x / 11.4) ** 2)) * 6.5
            for yy in range(int(-span), int(span) + 1):
                p.set(c + x, c + yy, VOID)
            p.set(c + x, c - span, ink); p.set(c + x, c + span, ink)
        p.disc(c, c, 4.4, ink)
        p.disc(c, c, 1.8, VOID)
    elif g == "split":                   # tithe costs double
        p.disc(c, c, 9, ink)
        p.disc(c, c, 7, col)
        p.rect(int(c) - 1, int(c) - 10, int(c) + 1, int(c) + 10, VOID)
        p.disc(c - 5, c, 3, ink)
    elif g == "chain":                   # break conditions harder
        p.circle(c - 5, c, 5, ink); p.circle(c - 5, c, 4, ink)
        p.circle(c + 5, c, 5, ink); p.circle(c + 5, c, 4, ink)
        p.hline(c - 2, c + 2, c, ink)
    elif g == "hat":                     # the Croupier plays
        p.rect(5, 19, 26, 21, ink)
        p.rect(10, 10, 21, 19, ink)
        p.rect(11, 11, 20, 18, col)
        p.rect(10, 16, 21, 18, ink)
    p.outline(VOID)
    return p


def mark_track(w=104, h=16):
    """Seven sockets along the ledger, filled as Marks are taken."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    for i in range(7):
        x = 6 + i * 14
        p.circle(x, h / 2, 4, SLATE2)
        p.set(x, h / 2, DEEP)
    return p


# ------------------------------------------------------ interlude emblems ---

def interlude_emblem(name):
    """24x24. Each reads as its verb, not its theme — the player is choosing a
    thing to *do*, and needs to know which at a glance."""
    p = Px(24, 24)
    c = 11.5
    sin = INTERLUDE_SIN[name]
    b, l = SIN_BASE[sin], SIN_LIGHT[sin]

    if name == "ember":                     # timing — a needle over a hot zone
        p.circle(c, c + 2, 9, b)
        for i in range(9):
            a = math.radians(-160 + i * 20)
            p.set(c + math.cos(a) * 9, c + 2 + math.sin(a) * 9, l)
        p.line(c, c + 2, c + math.cos(math.radians(-115)) * 8,
               c + 2 + math.sin(math.radians(-115)) * 8, BONE)
        p.disc(c, c + 2, 1.8, l)
    elif name == "mirror":                  # memory — a pattern half recalled
        p.frame(4, 4, 19, 19, b)
        for gx, gy in ((7, 7), (14, 7), (7, 14)):
            p.rect(gx - 2, gy - 2, gx + 2, gy + 2, l)
        p.frame(12, 12, 16, 16, b)
    elif name == "shell":                   # tracking — three cups
        for i, sx in enumerate((5, 11, 17)):
            top = 9 + (2 if i == 1 else 0)
            p.line(sx - 3, 17, sx - 2, top, l if i == 1 else b)
            p.line(sx + 3, 17, sx + 2, top, l if i == 1 else b)
            p.line(sx - 2, top, sx + 2, top, l if i == 1 else b)
        p.hline(2, 21, 18, b)
    elif name == "feast":                   # push your luck — a filling bowl
        p.line(4, 10, 6, 18, b); p.line(19, 10, 17, 18, b)
        p.hline(6, 17, 18, b)
        for y in range(13, 18):
            p.hline(6 + (y - 13) // 2, 17 - (y - 13) // 2, y, l)
        p.hline(3, 20, 10, l)
    elif name == "toll":                    # rhythm — beats on a line
        p.hline(2, 21, 15, b)
        for i, x in enumerate((5, 11, 17)):
            h = 6 if i == 1 else 4
            p.vline(x, 15 - h, 15, l)
            p.disc(x, 15 - h - 1, 1.6, l)
    elif name == "vigil":                   # hold — an hourglass
        p.hline(6, 17, 4, l); p.hline(6, 17, 19, l)
        p.line(6, 4, c, 11, b); p.line(17, 4, c, 11, b)
        p.line(6, 19, c, 12, b); p.line(17, 19, c, 12, b)
        for y in range(6, 10):
            p.hline(8 + (y - 6), 15 - (y - 6), y, l)
        p.set(c, 11, l); p.set(c, 12, l)
    elif name == "understudy":              # compare — two near-identical marks
        p.frame(3, 6, 10, 17, b)
        p.frame(13, 6, 20, 17, l)
        p.rect(5, 9, 8, 12, b)
        p.rect(15, 9, 18, 12, l)
        p.set(18, 15, BRIGHT)
    p.outline(VOID)
    return p


def interlude_card(name, w=56, h=76):
    """One of two offers at a table transition. Skipping is always available,
    so this has to sell itself in one emblem and one word."""
    sin = INTERLUDE_SIN[name]
    b, l = SIN_BASE[sin], SIN_LIGHT[sin]
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS], y / h, x, y))
    for y in range(h):
        for x in range(w):
            d = math.hypot(x - w / 2, y - 30) / 22.0
            if d < 1.0 and dither(x, y, (1.0 - d) * 0.26):
                p.set(x, y, b)
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, b)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (w - 2, 1, -1, 1),
                           (1, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        for k in range(3):
            p.set(cx + sx * k, cy, l); p.set(cx, cy + sy * k, l)

    p.blit(interlude_emblem(name), (w - 24) // 2, 16)
    p.hline(6, w - 7, 48, b)
    draw_text_center(p, name, w / 2, 54, BONE, tracking=1, shadow=VOID)
    draw_text_center(p, sin, w / 2, 65, l, tracking=0)
    return p


# ------------------------------------------------ shared interlude parts ---

def timing_track(w=96, h=16):
    """The Ember. A sweep with a scoring band; needle is separate so it can be
    driven independently."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    p.rect(w // 2 - 12, 2, w // 2 + 11, h - 3, SIN_BASE["wrath"])
    p.rect(w // 2 - 5, 2, w // 2 + 4, h - 3, SIN_LIGHT["wrath"])
    p.rect(w // 2 - 1, 2, w // 2, h - 3, BRIGHT)
    for x in range(4, w - 3, 8):
        p.vline(x, h - 4, h - 3, DEEP)
    return p


def timing_needle():
    p = Px(6, 20)
    p.vline(2, 1, 18, BONE)
    p.vline(3, 1, 18, BRIGHT)
    p.rect(1, 0, 4, 2, BRASS_PALE)
    p.rect(1, 17, 4, 19, BRASS_PALE)
    p.outline(VOID)
    return p


def memory_cell(state="idle"):
    """The Mirror. 16x16, four states."""
    p = Px(16, 16)
    col, fill = {
        "idle":    (SLATE2, None),
        "lit":     (BRASS_PALE, BRASS),
        "correct": (SIN_LIGHT["envy"], SIN_BASE["envy"]),
        "wrong":   (SIN_LIGHT["wrath"], SIN_BASE["wrath"]),
    }[state]
    if fill is not None:
        p.rect(2, 2, 13, 13, fill)
    p.frame(1, 1, 14, 14, col)
    p.frame(0, 0, 15, 15, VOID)
    for cx, cy in ((1, 1), (14, 1), (1, 14), (14, 14)):
        p.set(cx, cy, col)
    return p


def shell_cup(state="down"):
    """The Shell. 24x24."""
    p = Px(24, 24)
    b, l = SIN_BASE["lust"], SIN_LIGHT["lust"]
    if state == "lifted":
        for y in range(4, 12):
            t = (y - 4) / 8.0
            wdt = int(3 + t * 5)
            p.hline(11 - wdt, 12 + wdt, y, ramp_dither([l, b, INK], t, 11, y))
        p.hline(3, 20, 12, b)
        p.disc(11.5, 18, 3.4, BRASS)
        p.circle(11.5, 18, 3.8, BRASS_PALE)
    else:
        for y in range(8, 21):
            t = (y - 8) / 13.0
            wdt = int(3 + t * 6)
            p.hline(11 - wdt, 12 + wdt, y, ramp_dither([l, b, INK], t, 11, y))
        p.hline(2, 21, 21, VOID)
        if state == "marked":
            p.disc(11.5, 14, 2, BRIGHT)
    p.outline(VOID)
    return p


def feast_meter(w=72, h=14):
    """The Feast. Safe band, then a widening danger band — the player can see
    exactly how much rope they have."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    safe = int(w * 0.58)
    for x in range(1, safe):
        for y in range(2, h - 2):
            p.set(x, y, ramp_dither([SIN_LIGHT["envy"], SIN_BASE["envy"]],
                                    (y - 2) / (h - 5), x, y))
    for x in range(safe, w - 1):
        t = (x - safe) / max(1, (w - 1 - safe))
        for y in range(2, h - 2):
            p.set(x, y, ramp_dither([SIN_LIGHT["gluttony"], SIN_BASE["gluttony"],
                                     SIN_BASE["wrath"]], t, x, y))
    p.vline(safe, 1, h - 2, BONE)
    return p


def toll_beat(state="pending"):
    """The Toll. 12x12 beat marker."""
    p = Px(12, 12)
    col = {"pending": SLATE2, "hit": SIN_LIGHT["greed"],
           "perfect": BRIGHT, "miss": SIN_LIGHT["wrath"]}[state]
    p.circle(5.5, 5.5, 4.6, col)
    if state == "pending":
        p.circle(5.5, 5.5, 2.0, SLATE)
    elif state == "miss":
        p.line(3, 3, 8, 8, col); p.line(8, 3, 3, 8, col)
    else:
        p.disc(5.5, 5.5, 2.6, col)
        if state == "perfect":
            for i in range(4):
                a = math.radians(45 + i * 90)
                p.set(5.5 + math.cos(a) * 6.5, 5.5 + math.sin(a) * 6.5, col)
    return p


def vigil_ring(fill=0.0):
    """The Sloth hold. 32x32 — arc fills clockwise while the player holds."""
    p = Px(32, 32)
    c = 15.5
    b, l = SIN_BASE["sloth"], SIN_LIGHT["sloth"]
    for r in (13, 12, 11):
        p.circle(c, c, r, DEEP)
    steps = int(144 * max(0.0, min(1.0, fill)))
    for i in range(steps):
        a = math.radians(-90 + i * 2.5)
        p.set(c + math.cos(a) * 13, c + math.sin(a) * 13, BONE)
        p.set(c + math.cos(a) * 12, c + math.sin(a) * 12, l)
        p.set(c + math.cos(a) * 11, c + math.sin(a) * 11, b)
    # the release window, marked on the dial
    for i in range(6):
        a = math.radians(-90 + 300 + i * 5)
        p.set(c + math.cos(a) * 15, c + math.sin(a) * 15, BONE)
    p.disc(c, c, 3, b)
    p.circle(c, c, 3.4, l)
    return p


def diff_frame(w=40, h=40, marked=False):
    """The Understudy. Two of these sit side by side; one has the flaw."""
    p = Px(w, h)
    b, l = SIN_BASE["envy"], SIN_LIGHT["envy"]
    p.rect(1, 1, w - 2, h - 2, INK)
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, b)
    for y in range(4, h - 4, 3):
        for x in range(4, w - 4, 3):
            p.set(x, y, DEEP)
    if marked:
        for cx, cy, sx, sy in ((1, 1, 1, 1), (w - 2, 1, -1, 1),
                               (1, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
            for k in range(4):
                p.set(cx + sx * k, cy, l); p.set(cx, cy + sy * k, l)
    return p


# -------------------------------------------------------------- results ---

def result_banner(kind="success", w=96, h=24):
    col, light, label = {
        "success": (SIN_BASE["envy"], SIN_LIGHT["envy"], "TAKEN"),
        "partial": (BRASS, BRASS_PALE, "SOME OF IT"),
        "fail":    (SIN_BASE["wrath"], SIN_LIGHT["wrath"], "NOTHING"),
    }[kind]
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, col)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    draw_text_center(p, label, w / 2, 9, light, tracking=1, shadow=VOID)
    p.hline(6, 14, h // 2, col); p.hline(w - 15, w - 7, h // 2, col)
    return p


def skip_button(w=40, h=12):
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([DEEP, INK], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, SLATE3)
    draw_text_center(p, "SKIP", w / 2, 3, PALE, tracking=0)
    return p


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)

    for i in range(7):
        save(table_plaque(i), f"{OUT}/table_plaque_{i+1}.png")
        save(mark_seal(i, True), f"{OUT}/mark_{i+1}_earned.png")
        save(mark_seal(i, False), f"{OUT}/mark_{i+1}_locked.png")
    save(table_invite(), f"{OUT}/table_invite.png")
    save(mark_track(), f"{OUT}/mark_track.png")
    for st in ("passed", "current", "locked"):
        save(depth_pip(st), f"{OUT}/depth_pip_{st}.png")

    for n in INTERLUDES:
        save(interlude_emblem(n), f"{OUT}/emblem_{n}.png")
        save(interlude_card(n), f"{OUT}/interlude_{n}.png")

    save(timing_track(), f"{OUT}/timing_track.png")
    save(timing_needle(), f"{OUT}/timing_needle.png")
    for st in ("idle", "lit", "correct", "wrong"):
        save(memory_cell(st), f"{OUT}/memory_cell_{st}.png")
    for st in ("down", "lifted", "marked"):
        save(shell_cup(st), f"{OUT}/shell_cup_{st}.png")
    save(feast_meter(), f"{OUT}/feast_meter.png")
    for st in ("pending", "hit", "perfect", "miss"):
        save(toll_beat(st), f"{OUT}/toll_beat_{st}.png")
    for f in (0.0, 0.5, 1.0):
        save(vigil_ring(f), f"{OUT}/vigil_ring_{int(f*100)}.png")
    save(diff_frame(), f"{OUT}/diff_frame.png")
    save(diff_frame(marked=True), f"{OUT}/diff_frame_marked.png")

    for k in ("success", "partial", "fail"):
        save(result_banner(k), f"{OUT}/result_{k}.png")
    save(skip_button(), f"{OUT}/button_skip.png")

    # ------------------------------------------------------------ preview ---
    from PIL import Image
    W, H = 1180, 720
    o = Image.new("RGBA", (W, H), PAL[VOID])

    for i in range(7):
        o.alpha_composite(table_plaque(i).to_image().resize((88, 60), Image.NEAREST),
                          (20 + i * 96, 20))
    o.alpha_composite(table_invite().to_image().resize((336, 132), Image.NEAREST), (700, 20))

    for i in range(7):
        o.alpha_composite(mark_seal(i).to_image().resize((60, 60), Image.NEAREST),
                          (20 + i * 68, 100))
    o.alpha_composite(mark_track().to_image().resize((312, 48), Image.NEAREST), (520, 106))

    for i, n in enumerate(INTERLUDES):
        o.alpha_composite(interlude_card(n).to_image().resize((56 * 2, 76 * 2), Image.NEAREST),
                          (20 + i * 122, 190))

    o.alpha_composite(timing_track().to_image().resize((288, 48), Image.NEAREST), (20, 370))
    o.alpha_composite(timing_needle().to_image().resize((18, 60), Image.NEAREST), (150, 364))
    for i, st in enumerate(("idle", "lit", "correct", "wrong")):
        o.alpha_composite(memory_cell(st).to_image().resize((48, 48), Image.NEAREST),
                          (340 + i * 56, 370))
    for i, st in enumerate(("down", "lifted", "marked")):
        o.alpha_composite(shell_cup(st).to_image().resize((72, 72), Image.NEAREST),
                          (580 + i * 80, 360))
    o.alpha_composite(feast_meter().to_image().resize((216, 42), Image.NEAREST), (830, 375))

    for i, st in enumerate(("pending", "hit", "perfect", "miss")):
        o.alpha_composite(toll_beat(st).to_image().resize((48, 48), Image.NEAREST),
                          (20 + i * 56, 460))
    for i, f in enumerate((0.0, 0.5, 1.0)):
        o.alpha_composite(vigil_ring(f).to_image().resize((80, 80), Image.NEAREST),
                          (260 + i * 88, 445))
    o.alpha_composite(diff_frame().to_image().resize((80, 80), Image.NEAREST), (540, 445))
    o.alpha_composite(diff_frame(marked=True).to_image().resize((80, 80), Image.NEAREST),
                      (630, 445))
    for i, k in enumerate(("success", "partial", "fail")):
        o.alpha_composite(result_banner(k).to_image().resize((192, 48), Image.NEAREST),
                          (740 + (i % 2) * 208, 445 + (i // 2) * 56))
    o.alpha_composite(skip_button().to_image().resize((120, 36), Image.NEAREST), (20, 545))
    for i, st in enumerate(("passed", "passed", "current", "locked", "locked")):
        o.alpha_composite(depth_pip(st).to_image().resize((40, 40), Image.NEAREST),
                          (170 + i * 48, 543))

    o.convert("RGB").save("/home/claude/px/preview_esc.png")
    n = len([f for f in os.listdir(OUT) if f.endswith(".png")])
    print(f"{n} escalation assets written")

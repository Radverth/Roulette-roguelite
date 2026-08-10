"""Sin Wheel — Pledges, the Nudge, and the unified score display.

Three systems, one intent: put agency inside the spin, and let persistent
rule-changers combo with each other.

Pledges are the joker slot — things put up against the debt, granting power
while the house holds them. They change rules, never just numbers, which is
what makes them combine instead of stack.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *
from gen_ui_px import draw_text, draw_text_center, text_width, wrap

OUT = "Assets/Resources/Art/Pledges"

# Rarity language carried forward unchanged from the Forge, so the player
# learns it once and it holds everywhere.
RARITY = {
    "common":   (STEEL, PALE),
    "uncommon": (SIN_BASE["envy"], SIN_LIGHT["envy"]),
    "rare":     (BRASS, BRASS_PALE),
    "cursed":   (SIN_BASE["pride"], SIN_LIGHT["pride"]),
}

# name -> (rarity, emblem key)
PLEDGES = [
    ("WIDOWS RING",   "common",   "ring"),
    ("LONG COAT",     "common",   "coat"),
    ("SEXTONS KEY",   "common",   "key"),
    ("PAUPERS LUCK",  "common",   "bowl"),
    ("THE TALLY",     "common",   "tally"),
    ("CRACKED MIRROR", "uncommon", "mirror"),
    ("THE THUMB",     "uncommon", "thumb"),
    ("ASH LEDGER",    "uncommon", "book"),
    ("IRON TITHE",    "uncommon", "splitbar"),
    ("UNDERSTUDY",    "uncommon", "twomasks"),
    ("GRAVEDIGGERS CUT", "uncommon", "spade"),
    ("BLIND WAGER",   "rare",     "blindfold"),
    ("SEVENTH HOUR",  "rare",     "clock7"),
    ("DEBTORS CROWN", "rare",     "crown"),
    ("HOLLOW COIN",   "rare",     "holed"),
    ("THE LONG GAME", "rare",     "hourglass"),
    ("CROUPIERS FAVOUR", "cursed", "hatcard"),
    ("BLOOD PRICE",   "cursed",   "drop"),
    ("OPEN LEDGER",   "cursed",   "bookeye"),
    ("WIDOWS DEBT",   "cursed",   "knot"),
]

E = 24
EC = (E - 1) / 2.0


# --------------------------------------------------------------- emblems ---

def emblem(kind, base, light):
    """24x24. Silhouette first — five of these sit in a row on a phone, so each
    must be identifiable by outline alone before any detail registers."""
    p = Px(E, E)
    c = EC

    if kind == "ring":
        p.circle(c, c, 8, light); p.circle(c, c, 7, base)
        p.circle(c, c, 4, base)
        p.disc(c, c - 8, 2, light)
    elif kind == "coat":
        # long lapels and a hem — silhouette does the work
        p.line(7, 3, 3, 21, light); p.line(16, 3, 20, 21, light)
        p.hline(3, 20, 21, light)
        p.line(7, 3, c, 12, base); p.line(16, 3, c, 12, base)
        p.line(8, 4, c - 1, 12, light); p.line(15, 4, c + 1, 12, light)
        p.vline(c, 12, 21, base)
        for y in (14, 17, 20):
            p.set(c - 1, y, light)
    elif kind == "key":
        p.circle(7, 6, 5, light); p.circle(7, 6, 4, light)
        p.disc(7, 6, 2, VOID)
        for off in (0, 1):
            p.line(9 + off, 10, 18 + off, 19, light)
        p.line(13, 19, 16, 16, light)
        p.line(16, 22, 19, 19, light)
        p.set(19, 20, base); p.set(20, 21, base)
    elif kind == "bowl":
        p.line(4, 10, 7, 18, base); p.line(19, 10, 16, 18, base)
        p.hline(7, 16, 18, base)
        p.hline(3, 20, 10, light)
        p.set(c, 6, light); p.set(c - 3, 7, base); p.set(c + 3, 7, base)
    elif kind == "tally":
        for i in range(4):
            p.vline(5 + i * 3, 6, 17, light)
        p.line(3, 17, 17, 6, base)
    elif kind == "mirror":
        for x in range(-7, 8):
            span = int(math.sqrt(max(0, 1 - (x / 7.6) ** 2)) * 9)
            for yy in range(-span, span + 1):
                p.set(c + x, c + yy, base)
            p.set(c + x, c - span, light); p.set(c + x, c + span, light)
        # the crack, in void so it reads as absence
        for pts in (((c - 3, 3), (c + 1, 10)), ((c + 1, 10), (c - 2, 20)),
                    ((c + 1, 10), (c + 6, 14))):
            p.line(pts[0][0], pts[0][1], pts[1][0], pts[1][1], VOID)
            p.line(pts[0][0] + 1, pts[0][1], pts[1][0] + 1, pts[1][1], light)
    elif kind == "thumb":
        p.circle(c, c, 8, base)
        for r in (2, 4, 6):
            for i in range(0, 300, 12):
                a = math.radians(i)
                p.set(c + math.cos(a) * r, c + math.sin(a) * r, light)
    elif kind == "book":
        p.rect(4, 6, 19, 18, base)
        p.frame(4, 6, 19, 18, light)
        p.vline(c, 6, 18, light)
        for y in (9, 12, 15):
            p.hline(6, 10, y, light); p.hline(14, 18, y, light)
    elif kind == "splitbar":
        p.disc(c, c, 7, base)
        p.circle(c, c, 7, light)
        p.rect(int(c), 4, int(c) + 1, 19, VOID)
        p.hline(3, 20, 4, light); p.hline(3, 20, 19, light)
    elif kind == "twomasks":
        for ox, col, fill in ((-4, base, INK), (4, light, base)):
            for x in range(-5, 6):
                span = int(math.sqrt(max(0, 1 - (x / 5.6) ** 2)) * 8)
                for yy in range(-span, span + 1):
                    p.set(c + ox + x, c + yy, fill)
                p.set(c + ox + x, c - span, col); p.set(c + ox + x, c + span, col)
            p.set(c + ox - 2, c - 3, col); p.set(c + ox + 2, c - 3, col)
            p.hline(c + ox - 2, c + ox + 2, c + 4, col)
    elif kind == "spade":
        p.line(c, 4, 6, 12, light); p.line(c, 4, 17, 12, light)
        p.line(6, 12, 8, 15, light); p.line(17, 12, 15, 15, light)
        p.hline(8, 15, 15, light)
        p.vline(c, 15, 20, base)
        p.hline(c - 3, c + 3, 20, base)
    elif kind == "blindfold":
        # an eye, then the band across it — both must read
        for x in range(-8, 9):
            span = int(math.sqrt(max(0, 1 - (x / 8.4) ** 2)) * 6)
            for yy in range(-span, span + 1):
                p.set(c + x, c + yy, base)
            p.set(c + x, c - span, light); p.set(c + x, c + span, light)
        p.disc(c, c, 3, light)
        p.rect(1, int(c) - 2, 22, int(c) + 2, VOID)
        p.hline(1, 22, int(c) - 3, light); p.hline(1, 22, int(c) + 3, light)
        for x in range(2, 22, 4):
            p.vline(x, int(c) - 2, int(c) + 2, light)
    elif kind == "clock7":
        p.circle(c, c, 9, light); p.circle(c, c, 8, base)
        p.line(c, c, c, c - 6, light)
        p.line(c, c, c + 5, c + 3, light)
        for i in range(12):
            a = math.radians(-90 + i * 30)
            p.set(c + math.cos(a) * 7, c + math.sin(a) * 7, base)
        p.disc(c, c, 1.4, light)
    elif kind == "crown":
        p.hline(4, 19, 17, light)
        p.line(4, 17, 4, 8, light); p.line(19, 17, 19, 8, light)
        for i, x in enumerate((4, 9, 14, 19)):
            p.line(x, 8, x - 2 if i else x + 2, 12, light)
        p.line(4, 8, 9, 13, light); p.line(9, 8, c + 2, 13, light)
        p.line(14, 8, 9, 13, light); p.line(19, 8, 14, 13, light)
        p.hline(6, 17, 14, base)
    elif kind == "holed":
        p.disc(c, c, 8, base)
        p.circle(c, c, 8, light)
        p.disc(c, c, 3, VOID)
        p.circle(c, c, 3, light)
        for i in range(6):
            a = math.radians(i * 60)
            p.set(c + math.cos(a) * 5.6, c + math.sin(a) * 5.6, light)
    elif kind == "hourglass":
        p.hline(5, 18, 4, light); p.hline(5, 18, 19, light)
        p.line(5, 4, c, 11, base); p.line(18, 4, c, 11, base)
        p.line(5, 19, c, 12, base); p.line(18, 19, c, 12, base)
        for y in range(6, 10):
            p.hline(7 + (y - 6), 16 - (y - 6), y, light)
        p.vline(c, 11, 12, light)
        p.line(19, 8, 21, 11, light); p.line(21, 11, 19, 14, light)
    elif kind == "hatcard":
        # the Croupier's brim, with a card tucked into the band
        p.hline(1, 18, 11, light); p.hline(1, 18, 12, base)
        p.rect(5, 3, 14, 11, base)
        p.frame(5, 3, 14, 11, light)
        p.rect(5, 8, 14, 10, light)
        p.rect(14, 13, 21, 22, base)
        p.frame(14, 13, 21, 22, light)
        p.set(17, 16, light); p.set(17, 19, light); p.set(18, 17, light)
    elif kind == "drop":
        for y in range(4, 20):
            t = (y - 4) / 16.0
            w = int(1 + t * 6) if t < 0.75 else int(7 - (t - 0.75) * 16)
            p.hline(c - w, c + w, y, base if t < 0.5 else light)
        p.set(c - 2, 13, light)
        p.outline(VOID)
        return p
    elif kind == "bookeye":
        p.rect(2, 11, 21, 21, base)
        p.frame(2, 11, 21, 21, light)
        p.vline(c, 11, 21, light)
        for y in (14, 17):
            p.hline(4, 9, y, light); p.hline(14, 19, y, light)
        for x in range(-7, 8):
            span = int(math.sqrt(max(0, 1 - (x / 7.4) ** 2)) * 4.4)
            for yy in range(-span, span + 1):
                p.set(c + x, 6 + yy, INK)
            p.set(c + x, 6 - span, light); p.set(c + x, 6 + span, light)
        p.disc(c, 6, 2.2, light)
        p.disc(c, 6, 1, VOID)
    elif kind == "knot":
        p.circle(c - 4, c - 2, 5, base)
        p.circle(c + 4, c - 2, 5, base)
        p.circle(c, c + 5, 5, light)
        p.set(c, c - 2, light)
    p.outline(VOID)
    return p


# ----------------------------------------------------------- pledge card ---

PW, PH = 40, 56


def pledge_card(rarity="common", kind="ring"):
    """Small — five must sit in a row on a phone without scrolling."""
    base, light = RARITY[rarity]
    p = Px(PW, PH)
    for y in range(PH):
        for x in range(PW):
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS], y / PH, x, y))
    for y in range(PH):
        for x in range(PW):
            d = math.hypot(x - PW / 2, y - 22) / 17.0
            if d < 1.0 and dither(x, y, (1.0 - d) * 0.26):
                p.set(x, y, base)
    p.frame(0, 0, PW - 1, PH - 1, VOID)
    p.frame(1, 1, PW - 2, PH - 2, base)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (PW - 2, 1, -1, 1),
                           (1, PH - 2, 1, -1), (PW - 2, PH - 2, -1, -1)):
        for k in range(3):
            p.set(cx + sx * k, cy, light); p.set(cx, cy + sy * k, light)
    p.blit(emblem(kind, base, light), (PW - E) // 2, 10)
    p.hline(4, PW - 5, 40, base)
    # rarity ticks, bottom edge — reads at a glance without text
    n = list(RARITY).index(rarity) + 1
    for i in range(n):
        x = PW // 2 - (n - 1) * 3 + i * 6
        p.rect(x - 1, PH - 8, x + 1, PH - 6, light)
    return p


def pledge_slot(state="empty"):
    """A held slot. Five on the HUD, always visible — the player's build should
    never be more than a glance away."""
    W, H = 44, 60
    p = Px(W, H)
    col = {"empty": SLATE2, "locked": DEEP, "highlight": BRASS_PALE}[state]
    p.rect(1, 1, W - 2, H - 2, VOID)
    p.frame(0, 0, W - 1, H - 1, INK)
    p.frame(1, 1, W - 2, H - 2, col)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (W - 2, 1, -1, 1),
                           (1, H - 2, 1, -1), (W - 2, H - 2, -1, -1)):
        for k in range(4):
            p.set(cx + sx * k, cy, col); p.set(cx, cy + sy * k, col)
    if state == "empty":
        p.hline(W // 2 - 4, W // 2 + 4, H // 2, SLATE)
        p.vline(W // 2, H // 2 - 4, H // 2 + 4, SLATE)
    elif state == "locked":
        p.rect(18, 26, 25, 34, SLATE2)
        p.circle(21.5, 25, 4, SLATE2)
    return p


def pledge_sell():
    p = Px(16, 16)
    p.circle(7.5, 7.5, 6.4, BRASS_DK)
    p.disc(7.5, 7.5, 5, BRASS)
    p.line(4, 7, 11, 7, VOID)
    p.line(4, 8, 11, 8, VOID)
    p.outline(VOID)
    return p


# ---------------------------------------------------------------- nudge ---

def nudge_button(direction="right", state="ready"):
    """The one piece of agency inside the spin. Sized for a thumb, not a
    cursor — 28px native reads as roughly 11mm at x6 on a phone."""
    S_ = 28
    p = Px(S_, S_)
    c = (S_ - 1) / 2.0
    base, light = {
        "ready":    (BRASS, BRASS_PALE),
        "costly":   (SIN_BASE["gluttony"], SIN_LIGHT["gluttony"]),
        "disabled": (SLATE, SLATE2),
    }[state]

    for y in range(S_):
        for x in range(S_):
            if math.hypot(x - c, y - c) <= 12:
                p.set(x, y, ramp_dither([INK, ABYSS, VOID],
                                        math.hypot(x - c, y - c) / 12.0, x, y))
    p.circle(c, c, 12, VOID)
    p.circle(c, c, 11, base)
    p.circle(c, c, 10, INK)

    s = 1 if direction == "right" else -1
    # Chevron: tip out, two arms back. Drawn thick and doubled so it survives
    # under a thumb at speed.
    tipx = c + s * 5
    for off in (0, 1, 2):
        p.line(tipx - s * off, c, c - s * 2 - s * off, c - 7, light)
        p.line(tipx - s * off, c, c - s * 2 - s * off, c + 7, light)
    p.line(tipx + s, c, c - s * 1, c - 7, base)
    p.line(tipx + s, c, c - s * 1, c + 7, base)
    if state == "disabled":
        p.line(c - 9, c - 9, c + 9, c + 9, SLATE2)
        p.line(c - 9, c - 8, c + 8, c + 9, SLATE2)
    return p


def nudge_cost_pip(spent=True):
    """One Notice segment, shown beside the nudge button as the price."""
    p = Px(8, 10)
    col = SIN_LIGHT["wrath"] if spent else SLATE2
    p.rect(1, 1, 6, 8, col if spent else VOID)
    p.frame(0, 0, 7, 9, VOID)
    p.frame(1, 1, 6, 8, col)
    return p


def nudge_ghost():
    """Preview marker showing where the ticker would land. Drawn as an outline
    only — it must never be mistaken for a settled result."""
    p = Px(24, 32)
    for y in range(0, 32, 3):
        p.set(2, y, BONE); p.set(21, y, BONE)
    for x in range(0, 24, 3):
        p.set(x, 1, BONE); p.set(x, 30, BONE)
    p.line(9, 8, 14, 8, BRIGHT)
    p.line(11, 6, 11, 14, BRIGHT); p.line(12, 6, 12, 14, BRIGHT)
    p.line(8, 11, 11, 14, BRIGHT); p.line(15, 11, 12, 14, BRIGHT)
    return p


def nudge_charge_bar(w=48, h=10):
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    for i in range(1, 4):
        p.vline(int(i * w / 4), 1, h - 2, ABYSS)
    return p


# ------------------------------------------------------- score display ---

def score_panel(w=128, h=44):
    """Take x Mult. One number, assembled in front of the player, term by term.
    Everything that used to be a separate multiplier feeds this."""
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(2, 2, w - 3, h - 3, BRASS_LT)
    for cx, cy in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3)):
        p.set(cx, cy, BONE)
    # two wells with an operator between
    p.frame(6, 12, 54, 36, BRASS_DK)
    p.frame(w - 55, 12, w - 7, 36, BRASS_DK)
    draw_text(p, "TAKE", 8, 5, STEEL, tracking=0)
    draw_text(p, "MULT", w - 53, 5, STEEL, tracking=0)
    # multiplication cross, centred
    cx, cy = w // 2, 24
    for i in range(-4, 5):
        p.set(cx + i, cy + i, BRASS_PALE)
        p.set(cx + i, cy - i, BRASS_PALE)
    return p


def term_chip(kind="mult", w=52, h=14):
    """One line of the maths, flying in as it applies. Colour encodes the
    operation so the player reads gain or loss before they read the number."""
    base, light = {
        "add":    (BRASS, BRASS_PALE),
        "mult":   (SIN_BASE["pride"], SIN_LIGHT["pride"]),
        "reduce": (SIN_BASE["wrath"], SIN_LIGHT["wrath"]),
    }[kind]
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, base)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    # operator mark, left edge
    if kind == "add":
        p.hline(4, 9, 7, light); p.vline(6, 4, 10, light)
    elif kind == "mult":
        for i in range(-3, 4):
            p.set(6 + i, 7 + i, light); p.set(6 + i, 7 - i, light)
    else:
        p.hline(4, 9, 7, light)
    p.vline(12, 2, h - 3, base)
    return p


def big_number_plate(w=64, h=36, hot=False):
    """The assembled total. Deliberately oversized — this is the payoff beat."""
    col = SIN_LIGHT["wrath"] if hot else BRASS_PALE
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            d = math.hypot(x - w / 2, y - h / 2) / (w * 0.5)
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], min(1.0, d), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, col)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (w - 2, 1, -1, 1),
                           (1, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        for k in range(5):
            p.set(cx + sx * k, cy, col); p.set(cx, cy + sy * k, col)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    return p


def operator_glyph(kind="times"):
    p = Px(12, 12)
    c = 5.5
    if kind == "times":
        for i in range(-4, 5):
            p.set(c + i, c + i, BRASS_PALE); p.set(c + i, c - i, BRASS_PALE)
    elif kind == "plus":
        p.hline(1, 10, 5, BRASS_PALE); p.hline(1, 10, 6, BRASS_PALE)
        p.vline(5, 1, 10, BRASS_PALE); p.vline(6, 1, 10, BRASS_PALE)
    elif kind == "equals":
        p.hline(1, 10, 3, BONE); p.hline(1, 10, 4, BONE)
        p.hline(1, 10, 7, BONE); p.hline(1, 10, 8, BONE)
    elif kind == "minus":
        p.hline(1, 10, 5, SIN_LIGHT["wrath"]); p.hline(1, 10, 6, SIN_LIGHT["wrath"])
    p.outline(VOID)
    return p


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)

    for name, rarity, kind in PLEDGES:
        slug = name.lower().replace(" ", "_")
        base, light = RARITY[rarity]
        save(emblem(kind, base, light), f"{OUT}/emblem_{slug}.png")
        save(pledge_card(rarity, kind), f"{OUT}/pledge_{slug}.png")

    for st in ("empty", "locked", "highlight"):
        save(pledge_slot(st), f"{OUT}/pledge_slot_{st}.png")
    save(pledge_sell(), f"{OUT}/pledge_sell.png")

    for d in ("left", "right"):
        for st in ("ready", "costly", "disabled"):
            save(nudge_button(d, st), f"{OUT}/nudge_{d}_{st}.png")
    save(nudge_cost_pip(True), f"{OUT}/nudge_cost_pip_spent.png")
    save(nudge_cost_pip(False), f"{OUT}/nudge_cost_pip_free.png")
    save(nudge_ghost(), f"{OUT}/nudge_ghost.png")
    save(nudge_charge_bar(), f"{OUT}/nudge_charge_bar.png")

    save(score_panel(), f"{OUT}/score_panel.png")
    for k in ("add", "mult", "reduce"):
        save(term_chip(k), f"{OUT}/term_chip_{k}.png")
    save(big_number_plate(), f"{OUT}/big_number_plate.png")
    save(big_number_plate(hot=True), f"{OUT}/big_number_plate_hot.png")
    for k in ("times", "plus", "equals", "minus"):
        save(operator_glyph(k), f"{OUT}/op_{k}.png")

    # ------------------------------------------------------------ preview ---
    from PIL import Image
    W, H = 1180, 760
    o = Image.new("RGBA", (W, H), PAL[VOID])

    for i, (name, rarity, kind) in enumerate(PLEDGES):
        col = i % 10
        row = i // 10
        o.alpha_composite(pledge_card(rarity, kind).to_image()
                          .resize((PW * 2, PH * 2), Image.NEAREST),
                          (16 + col * 116, 16 + row * 128))
    for i, st in enumerate(("empty", "locked", "highlight")):
        o.alpha_composite(pledge_slot(st).to_image().resize((88, 120), Image.NEAREST),
                          (16 + i * 100, 280))
    o.alpha_composite(pledge_sell().to_image().resize((48, 48), Image.NEAREST), (320, 300))

    for i, (d, st) in enumerate((("left", "ready"), ("right", "ready"),
                                 ("right", "costly"), ("right", "disabled"))):
        o.alpha_composite(nudge_button(d, st).to_image().resize((84, 84), Image.NEAREST),
                          (400 + i * 92, 290))
    o.alpha_composite(nudge_ghost().to_image().resize((72, 96), Image.NEAREST), (780, 285))
    o.alpha_composite(nudge_charge_bar().to_image().resize((144, 30), Image.NEAREST), (870, 300))
    for i in range(4):
        o.alpha_composite(nudge_cost_pip(i < 2).to_image().resize((24, 30), Image.NEAREST),
                          (870 + i * 30, 340))

    o.alpha_composite(score_panel().to_image().resize((384, 132), Image.NEAREST), (16, 430))
    for i, k in enumerate(("add", "mult", "reduce")):
        o.alpha_composite(term_chip(k).to_image().resize((156, 42), Image.NEAREST),
                          (430, 430 + i * 50))
    o.alpha_composite(big_number_plate().to_image().resize((192, 108), Image.NEAREST), (620, 430))
    o.alpha_composite(big_number_plate(hot=True).to_image().resize((192, 108), Image.NEAREST),
                      (830, 430))
    for i, k in enumerate(("times", "plus", "equals", "minus")):
        o.alpha_composite(operator_glyph(k).to_image().resize((36, 36), Image.NEAREST),
                          (620 + i * 44, 550))

    o.convert("RGB").save("/home/claude/px/preview_pledge.png")
    n = len([f for f in os.listdir(OUT) if f.endswith(".png")])
    print(f"{n} pledge/nudge/score assets written")

"""Sin Wheel — revised loop asset set.

Assets for the four systems that turn the wheel from a slot machine into a
roguelite: the Forge (wedge drafting), the Notice (visible summon pressure),
the Streak (per-spin tension), and the Quota (banking that costs something).

Plus a break-condition glyph per sin, so every encounter states how to end it.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *
from gen_ui_px import draw_text, draw_text_center, text_width, wrap

OUT = "Assets/Resources/Art/Loop"

# Rarity is a colour language, used identically on every draft surface.
RARITY = {
    "common": (STEEL, PALE, "COMMON"),
    "rare":   (BRASS, BRASS_PALE, "RARE"),
    "cursed": (SIN_BASE["pride"], SIN_LIGHT["pride"], "CURSED"),
}


# ------------------------------------------------------------ draft card ---

DW, DH = 48, 68


def draft_card(rarity="common", action="add"):
    """One offer in the Forge. The action band across the top is what the
    player actually reads — add, strike or temper — so it sits above the art,
    not below it."""
    base, light, label = RARITY[rarity]
    p = Px(DW, DH)

    for y in range(DH):
        for x in range(DW):
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS], y / DH, x, y))

    # bloom behind the icon well
    for y in range(DH):
        for x in range(DW):
            d = math.hypot(x - DW / 2, y - 26) / 20.0
            if d < 1.0 and dither(x, y, (1.0 - d) * 0.28):
                p.set(x, y, base)

    p.frame(0, 0, DW - 1, DH - 1, VOID)
    p.frame(1, 1, DW - 2, DH - 2, base)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (DW - 2, 1, -1, 1),
                           (1, DH - 2, 1, -1), (DW - 2, DH - 2, -1, -1)):
        for k in range(3):
            p.set(cx + sx * k, cy, light)
            p.set(cx, cy + sy * k, light)

    # action band
    p.rect(2, 2, DW - 3, 10, VOID)
    p.hline(2, DW - 3, 11, base)
    draw_text_center(p, action.upper(), DW / 2, 4, light, tracking=1)

    # icon well — Unity drops the wedge sprite in at 16x16
    p.frame(15, 16, 32, 33, base)
    p.rect(16, 17, 31, 32, VOID)

    # rarity footer
    p.hline(4, DW - 5, DH - 12, base)
    draw_text_center(p, label, DW / 2, DH - 9, light, tracking=0)
    return p


def draft_card_back():
    p = Px(DW, DH)
    for y in range(DH):
        for x in range(DW):
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], (x + y) / (DW + DH), x, y))
    p.frame(0, 0, DW - 1, DH - 1, VOID)
    p.frame(1, 1, DW - 2, DH - 2, BRASS_DK)
    pts = [(DW / 2 + math.cos(math.radians(-90 + i * 360 / 7)) * 13,
            DH / 2 + math.sin(math.radians(-90 + i * 360 / 7)) * 13) for i in range(7)]
    order = [pts[(i * 3) % 7] for i in range(7)]
    for i in range(7):
        p.line(*order[i], *order[(i + 1) % 7], BRASS_DK)
    return p


def action_icon(kind):
    """16x16. Each reads as a verb applied to a wedge."""
    p = Px(16, 16)
    c = 7.5
    if kind == "add":
        for i in range(4):
            a0 = math.radians(-150 + i * 20)
            p.line(c - 1, c + 1, c - 1 + math.cos(a0) * 7, c + 1 + math.sin(a0) * 7, STEEL)
        p.line(c - 1, c + 1, c - 1, c - 6, PALE)
        p.line(c - 1, c + 1, c - 8, c + 1, PALE)
        p.rect(10, 2, 14, 3, BRASS_PALE)
        p.rect(11, 1, 13, 5, BRASS_PALE)
        p.frame(9, 0, 15, 6, BRASS_DK)
    elif kind == "strike":
        p.circle(c, c, 6.4, STEEL)
        p.line(3, 3, 12, 12, SIN_LIGHT["wrath"])
        p.line(3, 4, 11, 12, SIN_BASE["wrath"])
        p.line(12, 3, 3, 12, SIN_LIGHT["wrath"])
    elif kind == "temper":
        p.rect(3, 11, 12, 13, BRASS)
        p.frame(2, 10, 13, 14, BRASS_DK)
        p.line(7, 8, 3, 8, BRASS_PALE); p.line(8, 8, 12, 8, BRASS_PALE)
        p.line(3, 8, 7, 3, BRASS_PALE); p.line(12, 8, 8, 3, BRASS_PALE)
        p.line(7, 3, 8, 3, BRIGHT)
        p.vline(7, 4, 7, BRASS_LT); p.vline(8, 4, 7, BRASS_LT)
    elif kind == "reroll":
        for i in range(26):
            a0 = math.radians(-50 + i * 11)
            p.set(c + math.cos(a0) * 6.0, c + math.sin(a0) * 6.0, BRASS_PALE)
            p.set(c + math.cos(a0) * 5.0, c + math.sin(a0) * 5.0, BRASS)
        p.line(11, 0, 14, 3, BRASS_PALE)
        p.line(14, 3, 10, 5, BRASS_PALE)
        p.disc(c, c, 1.6, BRASS_LT)
    p.outline(VOID)
    return p


def tier_pips(tier=1):
    """Upgrade level, shown on a tempered wedge. Three states."""
    p = Px(16, 6)
    for i in range(3):
        x = 2 + i * 5
        if i < tier:
            p.rect(x, 1, x + 2, 4, BRASS_PALE)
            p.frame(x - 1, 0, x + 3, 5, BRASS_DK)
        else:
            p.frame(x - 1, 0, x + 3, 5, SLATE2)
    return p


# ---------------------------------------------------------- notice meter ---

def notice_track(w=64, h=10):
    """Segmented gauge. Eight cells, because the player should be able to
    count remaining safety at a glance rather than estimate a bar."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    for i in range(1, 8):
        p.vline(int(i * w / 8), 1, h - 2, ABYSS)
    p.hline(1, w - 2, 1, DEEP)
    return p


def notice_fill(w=64, h=10, state="cold"):
    ramp = {
        "cold":     [SIN_LIGHT["sloth"], SIN_BASE["sloth"], SLATE],
        "warm":     [BRASS_PALE, BRASS_LT, BRASS],
        "critical": [SIN_LIGHT["wrath"], SIN_BASE["wrath"], WINE_DK],
    }[state]
    p = Px(w, h)
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            p.set(x, y, ramp_dither(ramp, (y - 1) / (h - 3), x, y))
    p.hline(2, w - 3, 1, ramp[0])
    return p


def notice_eye(openness=0):
    """The house noticing you, in four stages. 0 shut, 3 wide.
    Sclera is filled, not implied — an outlined eye at 16px reads as a slot."""
    p = Px(16, 16)
    c = 7.5
    col = [SLATE2, SIN_BASE["sloth"], BRASS_LT, SIN_LIGHT["wrath"]][openness]

    if openness == 0:
        for x in range(-6, 7):
            y = int(abs(x) * 0.22)
            p.set(c + x, c + 1 + y, col)
            p.set(c + x, c + 2 + y, INK)
        for x in (-5, -2, 2, 5):
            p.set(c + x, c + 4 + int(abs(x) * 0.2), SLATE)
        p.outline(VOID)
        return p

    ry = [0, 2.4, 4.0, 5.6][openness]
    for x in range(-7, 8):
        span = math.sqrt(max(0.0, 1 - (x / 7.2) ** 2)) * ry
        top, bot = int(round(-span)), int(round(span))
        for y in range(top, bot + 1):
            p.set(c + x, c + y, PALE if openness > 1 else STEEL)
        p.set(c + x, c + top - 1, col)
        p.set(c + x, c + bot + 1, col)

    pr = max(1.4, ry * 0.66)
    p.disc(c, c, pr, col)
    p.disc(c, c, pr * 0.45, VOID)
    p.set(c - 1, c - 1, BRIGHT)
    if openness == 3:
        for i in range(6):
            ar = math.radians(i * 60 + 15)
            p.set(c + math.cos(ar) * 9.5, c + math.sin(ar) * 9.5, col)
    p.outline(VOID)
    return p


# ---------------------------------------------------------------- streak ---

def streak_pip(filled=True, hot=False):
    p = Px(8, 8)
    if filled:
        col = SIN_LIGHT["wrath"] if hot else BRASS_PALE
        p.disc(3.5, 3.5, 3.0, col)
        p.disc(3.5, 3.5, 1.6, BRIGHT if hot else BRASS_LT)
        p.circle(3.5, 3.5, 3.4, VOID)
    else:
        p.circle(3.5, 3.5, 3.0, SLATE2)
    return p


def streak_frame(w=56, h=14):
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    p.hline(1, w - 2, 1, INK)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    return p


def streak_break():
    """Played when a risk wedge wipes the chain. A ring snapping outward."""
    p = Px(32, 32)
    c = 15.5
    for i in range(12):
        a = math.radians(i * 30)
        p.line(c + math.cos(a) * 7, c + math.sin(a) * 7,
               c + math.cos(a) * 14, c + math.sin(a) * 14, SIN_LIGHT["wrath"])
    p.circle(c, c, 6, SIN_BASE["wrath"])
    for i in range(6):
        a = math.radians(i * 60 + 30)
        p.set(c + math.cos(a) * 11, c + math.sin(a) * 11, BRIGHT)
    return p


def multiplier_badge(w=28, h=14, hot=False):
    col = SIN_LIGHT["wrath"] if hot else BRASS_PALE
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, col)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    return p


# ----------------------------------------------------------- quota / debt ---

def quota_track(w=96, h=12):
    """The run's obligation. A marker sits at the quota point so the player can
    see, at every moment, whether leaving now is enough."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, VOID)
    p.frame(0, 0, w - 1, h - 1, BRASS_DK)
    for x in range(6, w - 1, 8):
        p.vline(x, h - 4, h - 2, DEEP)
    p.hline(1, w - 2, 1, INK)
    return p


def quota_marker():
    """Notched flag driven along the quota track."""
    p = Px(9, 16)
    p.vline(4, 3, 15, BRASS_PALE)
    p.rect(1, 0, 7, 5, BRASS_DK)
    p.frame(1, 0, 7, 5, BRASS_PALE)
    p.set(4, 2, BRIGHT)
    p.line(2, 13, 4, 15, BRASS_PALE)
    p.line(6, 13, 4, 15, BRASS_PALE)
    return p


def debt_seal(state="owed"):
    """32x32. The debt itself — a wax seal over the ledger. Cracks as it clears."""
    col, light = {
        "owed":    (WINE, WINE_LT),
        "reduced": (BRASS, BRASS_PALE),
        "grown":   (SIN_BASE["wrath"], SIN_LIGHT["wrath"]),
    }[state]
    p = Px(32, 32)
    c = 15.5
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - c, y - c)
            if d > 13:
                continue
            p.set(x, y, ramp_dither([light, col, INK], d / 13.0, x, y))
    p.circle(c, c, 13, VOID)
    p.circle(c, c, 11, light)
    pts = [(c + math.cos(math.radians(-90 + i * 360 / 7)) * 7,
            c + math.sin(math.radians(-90 + i * 360 / 7)) * 7) for i in range(7)]
    order = [pts[(i * 3) % 7] for i in range(7)]
    for i in range(7):
        p.line(*order[i], *order[(i + 1) % 7], light)
    if state == "reduced":
        p.line(6, 6, 25, 25, VOID)
        p.line(7, 6, 25, 24, BRIGHT)
    if state == "grown":
        for a in (20, 140, 260):
            ar = math.radians(a)
            p.line(c, c, c + math.cos(ar) * 12, c + math.sin(ar) * 12, VOID)
    p.outline(VOID)
    return p


def tithe_button(w=48, h=16):
    """Partial bank. Deliberately styled apart from the full-bank button so the
    two are never confused under thumb pressure."""
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([SLATE, DEEP, INK], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, BRASS_LT)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    # split-coin mark: one half paid, one half kept
    cy = h / 2
    p.disc(10, cy, 5, BRASS_DK)
    for y in range(int(cy - 5), int(cy + 6)):
        for x in range(5, 11):
            if (x - 10) ** 2 + (y - cy) ** 2 <= 25:
                p.set(x, y, BRASS_PALE)
    p.circle(10, cy, 5, VOID)
    p.circle(10, cy, 4, BRASS_LT)
    p.vline(10, int(cy - 4), int(cy + 4), VOID)
    p.vline(11, int(cy - 4), int(cy + 4), BRASS_PALE)
    return p


# ------------------------------------------------- break-condition glyphs ---

def break_glyph(sin):
    """16x16. Every encounter must state how to end it. One glyph per sin,
    shown in the status strip beside the sin's own name."""
    p = Px(16, 16)
    b, l = SIN_BASE[sin], SIN_LIGHT[sin]
    c = 7.5

    if sin == "pride":                     # three descending humilities
        for i in range(3):
            p.disc(3 + i * 4.5, 4 + i * 3, 1.8, l if i == 0 else b)
            p.circle(3 + i * 4.5, 4 + i * 3, 2.2, l)
    elif sin == "greed":                   # land the jackpot
        for i in range(8):
            a = math.radians(-90 + i * 45)
            p.line(c, c, c + math.cos(a) * 6.4, c + math.sin(a) * 6.4, l)
        p.disc(c, c, 2.4, b)
        p.circle(c, c, 2.8, BRIGHT)
    elif sin == "wrath":                   # survive three hits
        pts = [(c, 1), (13, 4), (12, 11), (c, 15), (3, 11), (2, 4)]
        for i in range(len(pts)):
            p.line(*pts[i], *pts[(i + 1) % len(pts)], l)
        for i in range(3):
            p.vline(5 + i * 3, 6, 10, b)
    elif sin == "envy":                    # a wedge it has not seen
        p.circle(c, c, 6.4, b)
        for i in range(3):
            a = math.radians(-90 + i * 120)
            p.line(c, c, c + math.cos(a) * 6.4, c + math.sin(a) * 6.4, b)
        p.line(10, 2, 10, 6, l); p.line(8, 4, 12, 4, l)
        p.set(10, 4, BRIGHT)
    elif sin == "lust":                    # the same wedge twice
        for sx in (3, 9):
            p.frame(sx, 5, sx + 4, 11, l)
            p.set(sx + 2, 8, b)
        p.line(7, 3, 9, 3, l); p.set(8, 2, l)
    elif sin == "gluttony":                # bank during the encounter
        p.disc(c, 11, 4, b)
        p.circle(c, 11, 4.4, l)
        p.set(c, 11, l)
        p.vline(c, 1, 6, l)
        p.line(4, 4, c, 1, l); p.line(11, 4, c, 1, l)
        p.set(c, 1, BRIGHT)
    elif sin == "sloth":                   # fill the resist meter
        p.frame(1, 5, 14, 10, l)
        p.rect(3, 7, 9, 8, b)
        p.set(11, 7, l); p.set(12, 8, l)
    p.outline(VOID)
    return p


# -------------------------------------------------------- forge surfaces ---

def forge_banner(w=112, h=20):
    p = Px(w, h)
    for y in range(h):
        for x in range(w):
            p.set(x, y, ramp_dither([INK, ABYSS], y / (h - 1), x, y))
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, BRASS_LT)
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        p.set(cx, cy, None)
    draw_text_center(p, "THE FORGE", w / 2, 6, BRASS_PALE, tracking=2, shadow=VOID)
    return p


def wedge_slot(state="empty"):
    """Shows a wheel position in the Forge. 20x20."""
    p = Px(20, 20)
    col = {"empty": SLATE2, "filled": BRASS_LT, "marked": SIN_LIGHT["wrath"]}[state]
    for i in range(8):
        a0 = math.radians(i * 45 - 22.5)
        a1 = math.radians((i + 1) * 45 - 22.5)
        p.line(9.5 + math.cos(a0) * 8, 9.5 + math.sin(a0) * 8,
               9.5 + math.cos(a1) * 8, 9.5 + math.sin(a1) * 8, col)
    if state == "empty":
        p.line(7, 9, 12, 9, SLATE)
    elif state == "filled":
        p.disc(9.5, 9.5, 3.4, BRASS)
        p.circle(9.5, 9.5, 3.8, BRASS_PALE)
    else:
        p.line(6, 6, 13, 13, col); p.line(13, 6, 6, 13, col)
    return p


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)

    for r in RARITY:
        for a in ("add", "strike", "temper"):
            save(draft_card(r, a), f"{OUT}/draft_{r}_{a}.png")
    save(draft_card_back(), f"{OUT}/draft_back.png")

    for k in ("add", "strike", "temper", "reroll"):
        save(action_icon(k), f"{OUT}/action_{k}.png")
    for t in (0, 1, 2, 3):
        save(tier_pips(t), f"{OUT}/tier_pips_{t}.png")

    save(notice_track(), f"{OUT}/notice_track.png")
    for st in ("cold", "warm", "critical"):
        save(notice_fill(state=st), f"{OUT}/notice_fill_{st}.png")
    for i in range(4):
        save(notice_eye(i), f"{OUT}/notice_eye_{i}.png")

    save(streak_pip(True), f"{OUT}/streak_pip_full.png")
    save(streak_pip(True, hot=True), f"{OUT}/streak_pip_hot.png")
    save(streak_pip(False), f"{OUT}/streak_pip_empty.png")
    save(streak_frame(), f"{OUT}/streak_frame.png")
    save(streak_break(), f"{OUT}/streak_break.png")
    save(multiplier_badge(), f"{OUT}/multiplier_badge.png")
    save(multiplier_badge(hot=True), f"{OUT}/multiplier_badge_hot.png")

    save(quota_track(), f"{OUT}/quota_track.png")
    save(quota_marker(), f"{OUT}/quota_marker.png")
    for st in ("owed", "reduced", "grown"):
        save(debt_seal(st), f"{OUT}/debt_seal_{st}.png")
    save(tithe_button(), f"{OUT}/button_tithe.png")

    for s in SIN_ORDER:
        save(break_glyph(s), f"{OUT}/break_{s}.png")

    save(forge_banner(), f"{OUT}/forge_banner.png")
    for st in ("empty", "filled", "marked"):
        save(wedge_slot(st), f"{OUT}/wedge_slot_{st}.png")

    # preview
    from PIL import Image
    W, H = 1180, 640
    out = Image.new("RGBA", (W, H), PAL[VOID])
    cards = [draft_card("common", "add"), draft_card("rare", "temper"),
             draft_card("cursed", "strike"), draft_card_back()]
    for i, c in enumerate(cards):
        out.alpha_composite(c.to_image().resize((DW * 3, DH * 3), Image.NEAREST),
                            (20 + i * (DW * 3 + 12), 20))
    for i, k in enumerate(("add", "strike", "temper", "reroll")):
        out.alpha_composite(action_icon(k).to_image().resize((64, 64), Image.NEAREST),
                            (640 + i * 72, 20))
    for i in range(4):
        out.alpha_composite(notice_eye(i).to_image().resize((64, 64), Image.NEAREST),
                            (640 + i * 72, 100))
    nt = notice_track().to_image(); nf = notice_fill(state="critical").to_image()
    base = Image.new("RGBA", (64, 10), (0, 0, 0, 0))
    base.alpha_composite(nt); base.alpha_composite(nf.crop((0, 0, 44, 10)))
    out.alpha_composite(base.resize((256, 40), Image.NEAREST), (640, 180))
    for i, s in enumerate(SIN_ORDER):
        out.alpha_composite(break_glyph(s).to_image().resize((64, 64), Image.NEAREST),
                            (20 + i * 72, 260))
    for i, st in enumerate(("owed", "reduced", "grown")):
        out.alpha_composite(debt_seal(st).to_image().resize((96, 96), Image.NEAREST),
                            (20 + i * 108, 350))
    out.alpha_composite(tithe_button().to_image().resize((144, 48), Image.NEAREST), (360, 350))
    out.alpha_composite(forge_banner().to_image().resize((336, 60), Image.NEAREST), (360, 420))
    for i, st in enumerate(("empty", "filled", "marked")):
        out.alpha_composite(wedge_slot(st).to_image().resize((60, 60), Image.NEAREST),
                            (740 + i * 68, 350))
    sf = streak_frame().to_image()
    out.alpha_composite(sf.resize((224, 56), Image.NEAREST), (20, 470))
    for i in range(3):
        pip = streak_pip(True, hot=(i == 2)).to_image()
        out.alpha_composite(pip.resize((32, 32), Image.NEAREST), (40 + i * 44, 482))
    out.alpha_composite(streak_break().to_image().resize((96, 96), Image.NEAREST), (280, 460))
    out.alpha_composite(quota_track().to_image().resize((288, 36), Image.NEAREST), (420, 490))
    out.alpha_composite(quota_marker().to_image().resize((27, 48), Image.NEAREST), (600, 480))
    out.convert("RGB").save("/home/claude/px/preview_loop.png")

    n = len([f for f in os.listdir(OUT) if f.endswith(".png")])
    print(f"{n} loop assets written")

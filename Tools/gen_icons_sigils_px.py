"""Segment icons at 16x16 and sin sigils at 32x32.

At these sizes a shape either reads in the first half-second or it has failed.
Everything is built from hard-edged primitives, then given a VOID outline so it
survives on top of both the wine wedges and the near-black risk wedges.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *

ICON_OUT = "Assets/Resources/Art/Icons"
SIG_OUT = "Assets/Resources/Art/Sins"

I = 16
IC = (I - 1) / 2.0
S = 32
SC = (S - 1) / 2.0


def ngon(p, cx, cy, r, n, idx, rot=-90, fill=False):
    pts = [(cx + math.cos(math.radians(rot + i * 360.0 / n)) * r,
            cy + math.sin(math.radians(rot + i * 360.0 / n)) * r) for i in range(n)]
    for i in range(n):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % n]
        p.line(x0, y0, x1, y1, idx)
    return pts


def star_pts(cx, cy, r_out, r_in, points, rot=-90):
    out = []
    for i in range(points * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180.0 / points)
        out.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return out


def poly(p, pts, idx):
    for i in range(len(pts)):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % len(pts)]
        p.line(x0, y0, x1, y1, idx)


def flood_ish(p, cx, cy, r, idx, test):
    """Fill by predicate — safer than a real flood fill at these sizes."""
    for y in range(int(cy - r), int(cy + r) + 1):
        for x in range(int(cx - r), int(cx + r) + 1):
            if test(x, y):
                p.set(x, y, idx)


# ------------------------------------------------------------- icons 16 ---

def ic_coin():
    p = Px(I, I)
    p.disc(IC, IC, 6.6, BRASS_DK)
    p.disc(IC, IC, 5.6, BRASS)
    p.disc(IC, IC, 4.2, BRASS_LT)
    # six-point star notch, so it is a coin and not a dot
    for i in range(6):
        a = math.radians(-90 + i * 60)
        p.line(IC, IC, IC + math.cos(a) * 3.4, IC + math.sin(a) * 3.4, BRASS_DK)
    p.set(IC, IC, BRASS_DK)
    # highlight arc, upper-left
    p.set(IC - 3, IC - 3, BRASS_PALE); p.set(IC - 2, IC - 4, BRASS_PALE)
    p.set(IC - 4, IC - 2, BRASS_PALE)
    return p


def ic_xp():
    p = Px(I, I)
    pts = [(IC, IC - 6), (IC + 6, IC + 4), (IC - 6, IC + 4)]
    poly(p, pts, BONE)
    flood_ish(p, IC, IC, 7, PALE,
              lambda x, y: y > IC - 4 and y < IC + 4
              and abs(x - IC) < (y - (IC - 6)) * 0.58)
    poly(p, pts, BONE)
    p.line(IC, IC - 4, IC, IC + 3, BRIGHT)
    return p


def ic_buff():
    p = Px(I, I)
    for k, (y, w) in enumerate(((IC + 4, 6), (IC + 1, 5), (IC - 2, 4))):
        idx = [BRASS, BRASS_LT, BRASS_PALE][k]
        p.line(IC - w, y, IC, y - 3, idx)
        p.line(IC, y - 3, IC + w, y, idx)
        p.line(IC - w, y + 1, IC, y - 2, idx)
        p.line(IC, y - 2, IC + w, y + 1, idx)
    return p


def ic_shard():
    p = Px(I, I)
    pts = [(IC, IC - 7), (IC + 4, IC - 1), (IC + 3, IC + 6),
           (IC - 3, IC + 6), (IC - 4, IC - 1)]
    flood_ish(p, IC, IC, 8, SLATE3,
              lambda x, y: -7 <= y - IC <= 6 and abs(x - IC) <= 4 - abs(y - IC) * 0.12)
    poly(p, pts, BONE)
    p.line(IC, IC - 6, IC, IC + 5, PALE)
    p.line(IC - 3, IC - 1, IC + 3, IC - 1, PALE)
    p.set(IC + 2, IC - 3, BRIGHT)
    return p


def ic_jackpot():
    p = Px(I, I)
    pts = star_pts(IC, IC, 7.2, 3.0, 8)
    flood_ish(p, IC, IC, 8, BRASS,
              lambda x, y: math.hypot(x - IC, y - IC) <= 3.4)
    poly(p, pts, BRASS_LT)
    for i in range(8):
        a = math.radians(-90 + i * 45)
        p.line(IC, IC, IC + math.cos(a) * 6.4, IC + math.sin(a) * 6.4, BRASS_LT)
    p.disc(IC, IC, 2.4, WINE_DK)
    p.circle(IC, IC, 2.4, BRASS_PALE)
    p.set(IC, IC, BRASS_PALE)
    return p


def ic_damage():
    """A fracture with mass, not a scratch."""
    key = {"K": VOID, "d": WINE_DK, "m": SIN_BASE["wrath"],
           "l": SIN_LIGHT["wrath"], "p": BRIGHT}
    rows = [
        "......KKK.......",
        ".....KmmdK......",
        ".....KlmdK......",
        "....KKlmdK......",
        "..KKdlmmdK......",
        ".KdlmmmddK......",
        ".KdlmmdKK.......",
        "..KdlmmmKKK.....",
        "...KKdlmmmdK....",
        ".....KKlmmdK....",
        "......KlmmdK....",
        "......KlmmdK....",
        ".....KdlmmdK....",
        ".....KdmmdK.....",
        "......KddK......",
        ".......KK.......",
    ]
    return parse_map(rows, key)


def ic_currency_loss():
    p = Px(I, I)
    p.disc(IC, IC - 3, 4.6, BRASS_DK)
    p.disc(IC, IC - 3, 3.4, BRASS)
    p.set(IC - 2, IC - 5, BRASS_LT)
    w, wl = SIN_BASE["wrath"], SIN_LIGHT["wrath"]
    for dx, col in ((-1, wl), (0, w)):
        p.vline(IC + dx, IC - 5, IC + 6, col)
    p.line(IC - 4, IC + 2, IC - 1, IC + 6, wl)
    p.line(IC + 3, IC + 2, IC, IC + 6, w)
    p.line(IC - 3, IC + 2, IC - 1, IC + 5, wl)
    p.line(IC + 2, IC + 2, IC, IC + 5, w)
    return p


def ic_debuff():
    p = Px(I, I)
    for k, (y, w) in enumerate(((IC - 5, 6), (IC - 1, 5), (IC + 3, 4))):
        idx = [SLATE3, STEEL, PALE][2 - k]
        p.line(IC - w, y, IC, y + 3, idx)
        p.line(IC, y + 3, IC + w, y, idx)
        p.line(IC - w, y + 1, IC, y + 4, idx)
        p.line(IC, y + 4, IC + w, y + 1, idx)
    return p


def ic_sin_summon():
    """Seven-pointed star — the whole cycle, before you know which answers."""
    p = Px(I, I)
    g, gl = SIN_BASE["gluttony"], SIN_LIGHT["gluttony"]
    pts = [(IC + math.cos(math.radians(-90 + i * 360.0 / 7)) * 6.4,
            IC + math.sin(math.radians(-90 + i * 360.0 / 7)) * 6.4) for i in range(7)]
    order = [pts[(i * 2) % 7] for i in range(7)]
    for i in range(7):
        x0, y0 = order[i]; x1, y1 = order[(i + 1) % 7]
        p.line(x0, y0, x1, y1, gl)
        p.line(x0, y0 + 1, x1, y1 + 1, g)
    for x, y in pts:
        p.set(x, y, BRIGHT)
    p.disc(IC, IC, 1.6, VOID)
    p.set(IC, IC, gl)
    return p


def ic_hp():
    p = Px(I, I)
    key = {"K": VOID, "d": WINE_DK, "m": WINE, "l": WINE_LT, "p": PALE, "b": BONE}
    rows = [
        "....KKKKKKK.....",
        "..KKdddddddKK...",
        ".KdmmmmmmmmmdK..",
        ".KdmmlllllmmmdK.",
        ".KdmlllllllmmdK.",
        ".KdmlllblllmmdK.",
        ".KdmlllblllmmdK.",
        ".KdmlbbbbblmmdK.",
        ".KdmlllblllmmdK.",
        ".KdmlllblllmmdK.",
        "..KdmlllllmmdK..",
        "..KddmlllmmddK..",
        "...KKdmlmmdKK...",
        ".....KdmmdK.....",
        "......KddK......",
        ".......KK.......",
    ]
    return parse_map(rows, key)


def ic_relic():
    p = Px(I, I)
    ngon(p, IC, IC, 6.4, 6, BRASS_LT)
    flood_ish(p, IC, IC, 7, SLATE,
              lambda x, y: math.hypot(x - IC, y - IC) <= 5.0)
    ngon(p, IC, IC, 6.4, 6, BRASS_LT)
    ngon(p, IC, IC, 3.4, 6, BRASS_PALE)
    p.set(IC - 2, IC - 2, BRIGHT)
    return p


def ic_spin_charge():
    p = Px(I, I)
    p.circle(IC, IC, 6.4, SLATE2)
    for i in range(30):
        ar = math.radians(-90 + i * (280.0 / 30))
        p.set(IC + math.cos(ar) * 6.4, IC + math.sin(ar) * 6.4, BRASS_PALE)
        p.set(IC + math.cos(ar) * 5.4, IC + math.sin(ar) * 5.4, BRASS)
    ar = math.radians(-90 + 280.0)
    p.disc(IC + math.cos(ar) * 6.0, IC + math.sin(ar) * 6.0, 1.6, BRIGHT)
    p.disc(IC, IC, 1.6, STEEL)
    return p


ICONS = {
    "seg_coin": ic_coin, "seg_xp": ic_xp, "seg_buff": ic_buff,
    "seg_shard": ic_shard, "seg_jackpot": ic_jackpot, "seg_damage": ic_damage,
    "seg_currency_loss": ic_currency_loss, "seg_debuff": ic_debuff,
    "seg_sin_summon": ic_sin_summon, "ui_hp": ic_hp, "ui_relic": ic_relic,
    "ui_spin_charge": ic_spin_charge,
}


# ------------------------------------------------------------ sigils 32 ---

def frame(p, base):
    """Shared containing frame so the seven read as one series."""
    for i in range(7):
        a0 = math.radians(-90 + i * 360.0 / 7)
        a1 = math.radians(-90 + (i + 1) * 360.0 / 7)
        p.line(SC + math.cos(a0) * 15, SC + math.sin(a0) * 15,
               SC + math.cos(a1) * 15, SC + math.sin(a1) * 15, base)
    for i in range(7):
        a = math.radians(-90 + i * 360.0 / 7)
        p.set(SC + math.cos(a) * 15, SC + math.sin(a) * 15, BONE)


def sg_pride():
    """Bilateral symmetry, absolute. A crowned eye."""
    p = Px(S, S)
    b, l = SIN_BASE["pride"], SIN_LIGHT["pride"]
    frame(p, b)
    for dx in range(-11, 12):
        y = int(round(math.sqrt(max(0, 121 - dx * dx)) * 0.46))
        p.set(SC + dx, SC - y, l); p.set(SC + dx, SC + y, l)
    p.circle(SC, SC, 4, l)
    p.disc(SC, SC, 2, BONE)
    for i in range(7):
        a = math.radians(-90 + (i - 3) * 16)
        ln = 4 + (3 - abs(i - 3))
        p.line(SC + math.cos(a) * 7, SC + math.sin(a) * 7,
               SC + math.cos(a) * (7 + ln), SC + math.sin(a) * (7 + ln), b)
        p.set(SC + math.cos(a) * (7 + ln), SC + math.sin(a) * (7 + ln), l)
    return p


def sg_greed():
    """Everything contracts toward a throat."""
    p = Px(S, S)
    b, l = SIN_BASE["greed"], SIN_LIGHT["greed"]
    frame(p, b)
    for k, r in enumerate((13, 10.5, 8, 6, 4)):
        p.circle(SC, SC + k * 0.9, r, l if k % 2 else b)
    p.disc(SC, SC + 4, 2, BONE)
    for i in range(8):
        a = math.radians(-90 + i * 45)
        p.line(SC + math.cos(a) * 13, SC + math.sin(a) * 13,
               SC + math.cos(a) * 5, SC + math.sin(a) * 5 + 3, b)
    return p


def sg_wrath():
    """Pure radiation. The frame is the only thing trying to hold it."""
    p = Px(S, S)
    b, l = SIN_BASE["wrath"], SIN_LIGHT["wrath"]
    frame(p, b)
    for i in range(12):
        a = math.radians(-90 + i * 30)
        long = i % 2 == 0
        r1 = 14 if long else 9
        p.line(SC + math.cos(a) * 4, SC + math.sin(a) * 4,
               SC + math.cos(a) * r1, SC + math.sin(a) * r1, l if long else b)
        if long:
            tip = (SC + math.cos(a) * r1, SC + math.sin(a) * r1)
            p.set(tip[0], tip[1], BONE)
            for da in (-14, 14):
                ab = math.radians(-90 + i * 30 + da)
                p.line(tip[0], tip[1], SC + math.cos(ab) * (r1 - 3),
                       SC + math.sin(ab) * (r1 - 3), l)
    p.circle(SC, SC, 4, l)
    p.disc(SC, SC, 2, BONE)
    return p


def sg_envy():
    """A form beside its stolen copy. The copy is brighter than the original."""
    p = Px(S, S)
    b, l = SIN_BASE["envy"], SIN_LIGHT["envy"]
    frame(p, b)
    ngon(p, SC - 4, SC + 3, 8, 5, b)          # the original, dim
    ngon(p, SC + 3, SC - 2, 8, 5, l)          # the copy, bright
    p.disc(SC + 3, SC - 2, 2, BONE)
    p.circle(SC - 4, SC + 3, 2, b)
    return p


def sg_lust():
    """Nothing keeps its place."""
    p = Px(S, S)
    b, l = SIN_BASE["lust"], SIN_LIGHT["lust"]
    frame(p, b)
    for i in range(3):
        a = math.radians(-90 + i * 120)
        p.circle(SC + math.cos(a) * 5, SC + math.sin(a) * 5, 7, l)
    # a single ghost, mid-slide
    p.circle(SC + 3, SC + 4, 7, b)
    p.disc(SC, SC, 2, BONE)
    return p


def sg_gluttony():
    """A ring of teeth turned inward. The mouth is the whole shape."""
    p = Px(S, S)
    b, l = SIN_BASE["gluttony"], SIN_LIGHT["gluttony"]
    frame(p, b)
    teeth = 12
    for i in range(teeth):
        a = math.radians(-90 + i * 360.0 / teeth)
        tip = (SC + math.cos(a) * 4, SC + math.sin(a) * 4)
        for da in (-13, 13):
            ab = math.radians(-90 + i * 360.0 / teeth + da)
            p.line(tip[0], tip[1], SC + math.cos(ab) * 12, SC + math.sin(ab) * 12,
                   l if i % 2 == 0 else b)
    p.circle(SC, SC, 12, l)
    p.circle(SC, SC, 3, BONE)
    return p


def sg_sloth():
    """Load-bearing horizontals under a sag. Gravity wins by default."""
    p = Px(S, S)
    b, l = SIN_BASE["sloth"], SIN_LIGHT["sloth"]
    frame(p, b)
    for k, (y, w) in enumerate(((SC - 8, 11), (SC - 3, 9), (SC + 2, 6))):
        idx = [BONE, l, b][k]
        p.hline(SC - w, SC + w, y, idx)
        p.hline(SC - w, SC + w, y + 1, b)
        p.set(SC - w, y - 1, idx); p.set(SC + w, y - 1, idx)
        # sag beneath the bar
        for dx in range(-w + 1, w):
            p.set(SC + dx, y + 2 + int((1 - (dx / w) ** 2) * 1.6), b)
    p.vline(SC, SC - 8, SC + 9, l)
    p.disc(SC, SC + 11, 2.6, l)
    p.circle(SC, SC + 11, 4, b)
    return p


SIGILS = {
    "pride": sg_pride, "greed": sg_greed, "wrath": sg_wrath, "envy": sg_envy,
    "lust": sg_lust, "gluttony": sg_gluttony, "sloth": sg_sloth,
}


if __name__ == "__main__":
    icons = []
    for name, fn in ICONS.items():
        p = fn()
        p.outline(VOID)
        save(p, f"{ICON_OUT}/{name}.png")
        icons.append(p)

    sigils = []
    for name, fn in SIGILS.items():
        p = fn()
        save(p, f"{SIG_OUT}/sigil_{name}.png")
        sigils.append(p)

    sheet(icons, 6, I, scale=6).save("/home/claude/px/preview_icons.png")
    sheet(sigils, 4, S, scale=4).save("/home/claude/px/preview_sigils.png")

    bad = 0
    for p in icons + sigils:
        bad += len(verify_palette(p.to_image()))
    print(f"{len(icons)} icons, {len(sigils)} sigils | off-palette: {bad}")

"""Sin Wheel — narrative asset set.

The sigils are geometry: what a sin *does*. These are faces: who is saying it.
Deliberately figurative where the sigils are abstract, so an announcement reads
as someone arriving rather than a status effect being applied.

Every mask is cut from one shared base — same workshop, same hand, seven
different commissions. The family resemblance is the point.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *
from gen_ui_px import (GLYPHS, FW, FH, draw_text, draw_text_center,
                       text_width, wrap)

OUT = "Assets/Resources/Art/Narrative"

M = 48                      # mask canvas
MC = (M - 1) / 2.0


# ---------------------------------------------------------- mask base ---

def mask_base(shadow, mid, light):
    """The blank the seven are cut from: a tall oval with a brow ridge, two
    sockets and a jaw. Left empty in the middle so each sin can carve it."""
    p = Px(M, M)

    for y in range(M):
        for x in range(M):
            dx = (x - MC) / 17.0
            dy = (y - MC + 1) / 21.0
            d = dx * dx + dy * dy
            if d > 1.0:
                continue
            # light falls from upper-left; three bands, dithered
            t = max(0.0, min(0.999, 0.5 + (dx * 0.46 + dy * 0.32)))
            band = ramp_pick([light, mid, mid, shadow], t)
            # dither only in the narrow seam between planes, so the face reads
            # as carved rather than airbrushed
            edge = abs((t * 4.0) % 1.0 - 0.5)
            if edge > 0.38:
                band = ramp_dither([light, mid, mid, shadow], t, x, y)
            p.set(x, y, band)

    # brow ridge — the single feature that makes an oval read as a face
    for x in range(-13, 14):
        y = int(-6 - math.cos(x / 13.0 * 1.4) * 3)
        p.set(MC + x, MC + y, light)
        p.set(MC + x, MC + y + 1, shadow)

    # jaw shading
    for x in range(-11, 12):
        y = int(13 - abs(x) * 0.30)
        p.set(MC + x, MC + y, shadow)

    p.outline(VOID)
    return p


def sockets(p, shadow, style="round", offset=0):
    """Cut the eye holes. A mask is defined by what is missing from it."""
    for sx in (-7, 7):
        cx, cy = MC + sx, MC - 1 + offset
        if style == "round":
            p.disc(cx, cy, 3.2, VOID)
            p.circle(cx, cy, 3.6, shadow)
        elif style == "slit":
            p.rect(cx - 4, cy - 1, cx + 4, cy + 1, VOID)
            p.frame(cx - 4, cy - 2, cx + 4, cy + 2, shadow)
        elif style == "hollow":
            p.disc(cx, cy, 4.2, VOID)
        elif style == "heavy":       # half-lidded
            p.disc(cx, cy, 3.4, VOID)
            p.rect(cx - 4, cy - 4, cx + 4, cy - 1, shadow)


def mouth(p, shadow, light, style="line"):
    y = MC + 8
    if style == "line":
        p.hline(MC - 6, MC + 6, y, shadow)
    elif style == "slot":            # a coin slot for a mouth
        p.rect(MC - 7, y - 1, MC + 7, y + 1, VOID)
        p.frame(MC - 8, y - 2, MC + 8, y + 2, light)
    elif style == "teeth":
        p.rect(MC - 10, y - 4, MC + 10, y + 5, VOID)
        for i in range(-10, 11, 3):
            p.vline(MC + i, y - 4, y - 1, light)
            p.vline(MC + i + 1, y + 2, y + 5, light)
    elif style == "sewn":
        p.hline(MC - 7, MC + 7, y, VOID)
        for i in range(-6, 7, 3):
            p.vline(MC + i, y - 2, y + 2, light)
    elif style == "sag":
        for x in range(-7, 8):
            p.set(MC + x, y + int(abs(x) * -0.28) + 2, shadow)
    elif style == "open":
        p.disc(MC, y + 1, 4.0, VOID)
        p.circle(MC, y + 1, 4.4, shadow)


# ------------------------------------------------------- the seven masks ---

def mk_pride():
    """Perfect bilateral symmetry, and a crown it awarded itself."""
    b, l = SIN_BASE["pride"], SIN_LIGHT["pride"]
    p = mask_base(INK, b, l)
    sockets(p, l, "round")
    mouth(p, INK, l, "line")
    for i in range(7):
        a = math.radians(-90 + (i - 3) * 15)
        ln = 5 + (3 - abs(i - 3)) * 2
        p.line(MC + math.cos(a) * 17, MC + math.sin(a) * 17 - 2,
               MC + math.cos(a) * (17 + ln), MC + math.sin(a) * (17 + ln) - 2, l)
        p.set(MC + math.cos(a) * (17 + ln), MC + math.sin(a) * (17 + ln) - 2, BONE)
    p.vline(MC, MC - 8, MC + 7, alpha_hint(b))
    return p


def alpha_hint(idx):
    """A dimmer companion for a sin colour — used for interior linework."""
    return INK if idx in (SIN_BASE["pride"],) else DEEP


def mk_greed():
    """A coin slot where a mouth should be. It does not speak so much as accept."""
    b, l = SIN_BASE["greed"], SIN_LIGHT["greed"]
    p = mask_base(INK, b, l)
    sockets(p, l, "slit")
    mouth(p, INK, l, "slot")
    for r in (12, 9, 6):
        for i in range(0, 360, 30):
            a = math.radians(i)
            p.set(MC + math.cos(a) * r, MC + 8 + math.sin(a) * r * 0.42, b)
    return p


def mk_wrath():
    """Split by the heat it contains. The crack is not damage; it is a vent."""
    b, l = SIN_BASE["wrath"], SIN_LIGHT["wrath"]
    p = mask_base(INK, b, l)
    sockets(p, l, "hollow")
    mouth(p, INK, l, "open")
    fracture = [(MC - 2, MC - 20), (MC + 2, MC - 12), (MC - 3, MC - 6),
                (MC + 3, MC + 2), (MC - 2, MC + 10), (MC + 1, MC + 18)]
    for i in range(len(fracture) - 1):
        p.line(*fracture[i], *fracture[i + 1], VOID)
        p.line(fracture[i][0] + 1, fracture[i][1], fracture[i + 1][0] + 1,
               fracture[i + 1][1], BONE)
    for a in (-150, -30, 40, 150):
        ar = math.radians(a)
        p.line(MC + math.cos(ar) * 14, MC + math.sin(ar) * 14,
               MC + math.cos(ar) * 20, MC + math.sin(ar) * 20, l)
    return p


def mk_envy():
    """Wearing a second face over the first, one pixel out of register."""
    b, l = SIN_BASE["envy"], SIN_LIGHT["envy"]
    p = mask_base(INK, b, l)
    # the ghost of the copied face, offset
    ghost = mask_base(INK, INK, b)
    for y in range(M):
        for x in range(M):
            v = ghost.buf[y][x]
            if v == b and p.get(x - 3, y + 2) is not None:
                p.set(x - 3, y + 2, b)
    sockets(p, l, "round")
    mouth(p, INK, l, "line")
    for sx in (-7, 7):
        p.circle(MC + sx - 3, MC + 1, 3.6, b)
    return p


def mk_lust():
    """More eyes than a face should have, none of them settled."""
    b, l = SIN_BASE["lust"], SIN_LIGHT["lust"]
    p = mask_base(INK, b, l)
    sockets(p, l, "round")
    for sx, sy, r in ((-11, -6, 2.2), (11, -6, 2.2), (-4, 4, 1.8), (4, 4, 1.8)):
        p.disc(MC + sx, MC + sy, r, VOID)
        p.circle(MC + sx, MC + sy, r + 0.6, b)
    mouth(p, INK, l, "line")
    for i in range(3):
        a = math.radians(-90 + i * 120 + 30)
        p.circle(MC + math.cos(a) * 5, MC + math.sin(a) * 5 + 2, 9, b)
    return p


def mk_gluttony():
    """Mostly mouth. The eyes are an afterthought and it shows."""
    b, l = SIN_BASE["gluttony"], SIN_LIGHT["gluttony"]
    p = mask_base(INK, b, l)
    sockets(p, l, "slit", offset=-4)
    mouth(p, INK, l, "teeth")
    p.circle(MC, MC + 8, 13, b)
    p.circle(MC, MC + 8, 15, INK)
    return p


def mk_sloth():
    """Everything about it is on its way down."""
    b, l = SIN_BASE["sloth"], SIN_LIGHT["sloth"]
    p = mask_base(INK, b, l)
    sockets(p, l, "heavy")
    mouth(p, INK, l, "sag")
    for k, y in enumerate((MC + 2, MC + 6, MC + 10)):
        w = 12 - k * 3
        for dx in range(-w, w + 1):
            p.set(MC + dx, y + int((1 - (dx / w) ** 2) * 1.8), b)
    p.disc(MC, MC + 20, 2.4, l)
    p.vline(MC, MC + 14, MC + 18, b)
    return p


def mk_croupier():
    """The one who turns it. Bone where the seven have colour — he is not a
    sin, he is the terms."""
    p = mask_base(INK, STEEL, PALE)
    sockets(p, BONE, "hollow")
    mouth(p, INK, BONE, "sewn")
    # brimmed hat, the only silhouette in the set that breaks the oval
    for x in range(-22, 23):
        y = int(-17 - math.cos(x / 22.0 * 1.5) * 2)
        p.set(MC + x, MC + y, INK)
        p.set(MC + x, MC + y + 1, SLATE2)
        p.set(MC + x, MC + y + 2, INK)
    for x in range(-10, 11):
        for y in range(-26, -17):
            if abs(x) < 10 - abs(y + 22) * 0.4:
                p.set(MC + x, MC + y, ramp_dither([SLATE2, INK, VOID],
                                                  (y + 26) / 9.0, x, y))
    p.hline(MC - 10, MC + 10, MC - 19, BRASS_DK)
    p.outline(VOID)
    return p


MASKS = {
    "pride": mk_pride, "greed": mk_greed, "wrath": mk_wrath, "envy": mk_envy,
    "lust": mk_lust, "gluttony": mk_gluttony, "sloth": mk_sloth,
    "croupier": mk_croupier,
}


# ------------------------------------------------- announcement plates ---

PW, PH = 160, 64


def announce_plate(name, line):
    """The wide banner that slides in when a sin arrives. Mask on the left,
    the sin's own words on the right — never a description of its effect. The
    card already does mechanics; this does voice."""
    is_sin = name in SIN_BASE
    b = SIN_BASE[name] if is_sin else STEEL
    l = SIN_LIGHT[name] if is_sin else BONE

    p = Px(PW, PH)
    for y in range(PH):
        for x in range(PW):
            t = 0.25 + (x / PW) * 0.55 + (y / PH) * 0.20
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], t, x, y))

    p.frame(0, 0, PW - 1, PH - 1, VOID)
    p.frame(1, 1, PW - 2, PH - 2, b)
    p.hline(2, PW - 3, 3, INK)
    for cx, cy in ((0, 0), (PW - 1, 0), (0, PH - 1), (PW - 1, PH - 1)):
        p.set(cx, cy, None)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (PW - 2, 1, -1, 1),
                           (1, PH - 2, 1, -1), (PW - 2, PH - 2, -1, -1)):
        for k in range(4):
            p.set(cx + sx * k, cy, l); p.set(cx, cy + sy * k, l)

    # mask, inset left, with a dividing rule
    m = MASKS[name]()
    p.blit(m, 6, 8)
    p.vline(58, 6, PH - 7, b)

    # speaker name, then the line
    label = name.upper() if is_sin else "THE CROUPIER"
    draw_text(p, label, 64, 9, l, tracking=1)
    p.hline(64, PW - 8, 19, b)
    for i, ln in enumerate(wrap(line.upper(), 15)[:4]):
        draw_text(p, ln, 64, 24 + i * 9, BONE if i == 0 else PALE, tracking=0)
    return p


# ------------------------------------------------------- dialogue frame ---

def dialogue_box(w=64, h=40, accent=BRASS_LT):
    """9-slice, 8px corners, with a reserved 16px portrait well on the left."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, ABYSS)
    p.rect(1, 1, w - 2, h - 2, INK)
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.frame(1, 1, w - 2, h - 2, accent)
    p.frame(3, 3, w - 4, h - 4, DEEP)
    for cx, cy, sx, sy in ((1, 1, 1, 1), (w - 2, 1, -1, 1),
                           (1, h - 2, 1, -1), (w - 2, h - 2, -1, -1)):
        for k in range(4):
            p.set(cx + sx * k, cy, BRASS_PALE); p.set(cx, cy + sy * k, BRASS_PALE)
    # portrait well
    p.frame(4, 4, 21, 21, BRASS_DK)
    p.rect(5, 5, 20, 20, VOID)
    return p


def speech_tail(idx=BRASS_LT):
    p = Px(8, 6)
    for i in range(6):
        p.hline(i, 7 - i, i, INK if i else idx)
    p.line(0, 0, 5, 5, idx)
    p.line(7, 0, 3, 5, idx)
    return p


def continue_caret():
    """The blinking 'tap to continue' mark."""
    p = Px(6, 6)
    p.line(1, 1, 4, 3, BRASS_PALE)
    p.line(4, 3, 1, 5, BRASS_PALE)
    p.line(1, 2, 3, 3, BRASS_LT)
    return p


# ---------------------------------------------------------- intertitles ---

TW, TH = 128, 72


def intertitle(top, mid, bottom, accent=BRASS_LT):
    """Run-start and run-end plates. Three registers: a small label, the line
    that matters, and a small consequence."""
    p = Px(TW, TH)
    for y in range(TH):
        for x in range(TW):
            d = math.hypot(x - TW / 2, y - TH / 2) / (TW * 0.6)
            p.set(x, y, ramp_dither([INK, ABYSS, VOID], min(1.0, d * 1.15), x, y))
    p.frame(0, 0, TW - 1, TH - 1, VOID)
    p.frame(2, 2, TW - 3, TH - 3, accent)
    for cx, cy in ((2, 2), (TW - 3, 2), (2, TH - 3), (TW - 3, TH - 3)):
        p.set(cx, cy, BONE)

    draw_text_center(p, top, TW / 2, 14, accent, tracking=2)
    p.hline(24, TW - 25, 24, accent)
    for i, ln in enumerate(wrap(mid.upper(), 20)[:2]):
        draw_text_center(p, ln, TW / 2, 32 + i * 9, BONE, tracking=1, shadow=VOID)
    draw_text_center(p, bottom, TW / 2, 56, STEEL, tracking=0)
    return p


def fragment_card():
    """Collectible lore plate. Blank body — Unity fills the text at runtime so
    one sprite serves every fragment."""
    W, H = 80, 112
    p = Px(W, H)
    for y in range(H):
        for x in range(W):
            p.set(x, y, ramp_dither([DEEP, INK, ABYSS], y / H, x, y))
    p.frame(0, 0, W - 1, H - 1, VOID)
    p.frame(2, 2, W - 3, H - 3, BRASS_DK)
    p.frame(4, 4, W - 5, H - 5, INK)
    p.hline(10, W - 11, 16, BRASS_DK)
    p.hline(10, W - 11, H - 17, BRASS_DK)
    for x in (10, W - 11):
        p.set(x, 16, BRASS_PALE); p.set(x, H - 17, BRASS_PALE)
    draw_text_center(p, "FRAGMENT", W / 2, 8, BRASS_LT, tracking=1)
    return p


def ledger_row(w=96, h=12, marked=False):
    """One line of the debt ledger — the meta-progression screen."""
    p = Px(w, h)
    p.rect(0, 0, w - 1, h - 1, INK if not marked else DEEP)
    p.frame(0, 0, w - 1, h - 1, VOID)
    p.hline(1, w - 2, h - 2, ABYSS)
    p.vline(14, 1, h - 2, BRASS_DK)
    p.vline(w - 16, 1, h - 2, BRASS_DK)
    if marked:
        p.line(4, 4, 9, 8, BRASS_PALE)
        p.line(9, 8, 6, 3, BRASS_PALE)
    return p


def ornament(w=32, h=8, accent=BRASS_LT):
    """Divider. Used between a speaker name and their line."""
    p = Px(w, h)
    c = h // 2
    p.hline(0, w - 1, c, accent)
    p.disc(w / 2, c, 2.2, accent)
    p.set(w // 2, c, VOID)
    for x in (4, w - 5):
        p.set(x, c - 1, accent); p.set(x, c + 1, accent)
    return p


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)

    for name, fn in MASKS.items():
        save(fn(), f"{OUT}/mask_{name}.png")

    # Plates carry the canonical arrival line for each speaker.
    ARRIVAL = {
        "pride":    "Look how well you were doing.",
        "greed":    "A third. That is all. A third of everything.",
        "wrath":    "I have opened the wheel wider for you.",
        "envy":     "Whatever you love most, I have already copied.",
        "lust":     "Nothing is where you left it.",
        "gluttony": "You have carried that long enough. Set it down.",
        "sloth":    "There is no hurry. There never was.",
        "croupier": "Again. Sit. You know the terms.",
    }
    for name, line in ARRIVAL.items():
        save(announce_plate(name, line), f"{OUT}/plate_{name}.png")

    save(dialogue_box(), f"{OUT}/dialogue_box.png")
    save(dialogue_box(accent=SIN_LIGHT["wrath"]), f"{OUT}/dialogue_box_danger.png")
    save(speech_tail(), f"{OUT}/speech_tail.png")
    save(continue_caret(), f"{OUT}/continue_caret.png")

    save(intertitle("THE WHEEL", "SEVEN WILL ANSWER", "TAP TO BEGIN"),
         f"{OUT}/intertitle_start.png")
    save(intertitle("PAID", "THE HOUSE NOTES IT", "DEBT REDUCED"),
         f"{OUT}/intertitle_bank.png")
    save(intertitle("TAKEN", "THE HOUSE KEEPS WHAT IT IS OWED",
                    "NOTHING CARRIED OUT", accent=SIN_LIGHT["wrath"]),
         f"{OUT}/intertitle_bust.png")

    save(fragment_card(), f"{OUT}/fragment_card.png")
    save(ledger_row(), f"{OUT}/ledger_row.png")
    save(ledger_row(marked=True), f"{OUT}/ledger_row_marked.png")
    save(ornament(), f"{OUT}/ornament_divider.png")

    # previews
    masks = [fn() for fn in MASKS.values()]
    sheet(masks, 4, M, scale=4).save("/home/claude/px/preview_masks.png")

    from PIL import Image
    plates = ["croupier", "greed", "sloth"]
    ims = [announce_plate(n, ARRIVAL[n]).to_image() for n in plates]
    out = Image.new("RGBA", (PW * 4 + 16, (PH * 4 + 8) * 3 + 8), PAL[VOID])
    for i, im in enumerate(ims):
        out.alpha_composite(im.resize((PW * 4, PH * 4), Image.NEAREST),
                            (8, 8 + i * (PH * 4 + 8)))
    out.convert("RGB").save("/home/claude/px/preview_plates.png")

    bad = sum(len(verify_palette(m.to_image())) for m in masks)
    print(f"{len(masks)} masks, {len(ARRIVAL)} plates | off-palette: {bad}")

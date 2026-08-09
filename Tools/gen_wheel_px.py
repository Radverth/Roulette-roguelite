"""The wheel, at 128x128 native.

Rasterised in polar space rather than drawn with shape primitives: for every
pixel we compute radius and angle and decide what it is. That is the only way
to get clean, deliberate segment seams at this size — PIL's pieslice would
give ragged edges that read as mistakes rather than as pixels.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from palette32 import *

OUT = "Assets/Resources/Art/Wheel"

N = 128
C = (N - 1) / 2.0

R_RIM_OUT = 63.0
R_RIM_IN = 55.0
R_SEG_OUT = 54.0
R_SEG_IN = 19.0

SEGMENTS = [
    ("coin", "reward"), ("damage", "risk"), ("xp", "reward"), ("coin", "reward"),
    ("sin_summon", "risk"), ("buff", "reward"), ("coin", "reward"),
    ("currency_loss", "risk"), ("xp", "reward"), ("shard", "reward"),
    ("debuff", "risk"), ("jackpot", "reward"),
]
SEG_COUNT = len(SEGMENTS)
STEP = 360.0 / SEG_COUNT

# Three alternating grounds so neighbouring reward wedges stay distinct, and a
# near-void ground for risk so danger reads before it is understood.
REWARD_RAMP_A = [INK, DEEP, SLATE]
REWARD_RAMP_B = [WINE_DK, WINE, WINE_LT]
RISK_RAMP = [VOID, VOID, ABYSS]


def polar(x, y):
    dx, dy = x - C, y - C
    r = math.hypot(dx, dy)
    a = (math.degrees(math.atan2(dy, dx)) + 90.0) % 360.0
    return r, a


def seg_index(a):
    return int(((a + STEP * 0.5) % 360.0) / STEP) % SEG_COUNT


def build_disc():
    p = Px(N, N)

    for y in range(N):
        for x in range(N):
            r, a = polar(x, y)
            if r > R_SEG_OUT or r < R_SEG_IN:
                continue

            i = seg_index(a)
            kind = SEGMENTS[i][1]
            ramp = RISK_RAMP if kind == "risk" else (
                REWARD_RAMP_A if i % 2 == 0 else REWARD_RAMP_B)

            # Radial shading in bands, dithered between steps.
            t = (r - R_SEG_IN) / (R_SEG_OUT - R_SEG_IN)
            p.set(x, y, ramp_dither(ramp, t * 0.92, x, y))

    # Seams. Drawn after the fill so they sit cleanly on top, and warmed to
    # brass beside a risk wedge so the danger edge is felt at a glance.
    for i in range(SEG_COUNT):
        a = math.radians(i * STEP - STEP * 0.5 - 90.0)
        left_risk = SEGMENTS[i][1] == "risk"
        right_risk = SEGMENTS[(i - 1) % SEG_COUNT][1] == "risk"
        col = BRASS_LT if (left_risk or right_risk) else BRASS_DK
        x0, y0 = C + math.cos(a) * R_SEG_IN, C + math.sin(a) * R_SEG_IN
        x1, y1 = C + math.cos(a) * R_SEG_OUT, C + math.sin(a) * R_SEG_OUT
        p.line(x0, y0, x1, y1, col)

    # Two measure rings — the instrument register, kept to single pixels.
    p.circle(C, C, 34, INK)
    p.circle(C, C, 44, INK)
    for i in range(SEG_COUNT * 2):
        a = math.radians(i * (360.0 / (SEG_COUNT * 2)) - 90.0)
        p.set(C + math.cos(a) * 44, C + math.sin(a) * 44, BRASS_DK)

    # Inner and outer containment
    p.ring(C, C, R_SEG_OUT, R_SEG_OUT - 1, BRASS_DK)
    p.ring(C, C, R_SEG_IN + 1, R_SEG_IN, BRASS_DK)
    return p


def build_rim():
    """Static ornate rim. Four-step brass ramp across eight pixels, with studs
    on the twelve seams."""
    p = Px(N, N)

    for y in range(N):
        for x in range(N):
            r, a = polar(x, y)
            if not (R_RIM_IN <= r <= R_RIM_OUT):
                continue
            t = (r - R_RIM_IN) / (R_RIM_OUT - R_RIM_IN)
            # Bright band sits a third of the way out — reads as a rolled edge
            # catching light, rather than a flat band.
            lit = 1.0 - abs(t - 0.32) * 2.2
            idx = ramp_dither(BRASS_RAMP, max(0.0, min(1.0, lit)), x, y)
            p.set(x, y, idx)

    p.ring(C, C, R_RIM_OUT, R_RIM_OUT - 1, VOID)
    p.ring(C, C, R_RIM_IN + 1, R_RIM_IN, VOID)

    r_mid = (R_RIM_OUT + R_RIM_IN) / 2.0
    for i in range(SEG_COUNT):
        a = math.radians(i * STEP - STEP * 0.5 - 90.0)
        sx, sy = C + math.cos(a) * r_mid, C + math.sin(a) * r_mid
        p.disc(sx, sy, 2.2, BRASS_DK)
        p.set(sx, sy, BRASS_PALE)
        p.set(sx - 1, sy - 1, BRASS_PALE)
    # Small rivets between the studs
    for i in range(SEG_COUNT):
        a = math.radians(i * STEP - 90.0)
        p.set(C + math.cos(a) * r_mid, C + math.sin(a) * r_mid, BRASS_LT)
    return p


def build_hub():
    """32x32 centre boss with a twelve-rayed star."""
    S = 32
    c = (S - 1) / 2.0
    p = Px(S, S)
    for y in range(S):
        for x in range(S):
            r = math.hypot(x - c, y - c)
            if r > 15:
                continue
            t = 1.0 - r / 15.0
            p.set(x, y, ramp_dither([VOID, WINE_DK, WINE], t, x, y))
    p.circle(c, c, 15, BRASS_DK)
    p.circle(c, c, 13, BRASS)
    for i in range(12):
        a = math.radians(i * 30 - 90)
        long = (i % 3 == 0)
        r1 = 10 if long else 7
        p.line(c + math.cos(a) * 3, c + math.sin(a) * 3,
               c + math.cos(a) * r1, c + math.sin(a) * r1,
               BRASS_LT if long else BRASS_DK)
    p.disc(c, c, 3, BRASS_LT)
    p.disc(c, c, 1.5, WINE_DK)
    return p


def build_pointer():
    """16x24 ticker. Hand-shaped: a blade with a pivot mount, outlined so it
    never disappears against the rim."""
    key = {
        "K": VOID, "d": BRASS_DK, "m": BRASS, "l": BRASS_LT, "p": BRASS_PALE,
        "w": WINE_DK,
    }
    rows = [
        ".....KKKKKK.....",
        "....KddmmddK....",
        "...KdmllmmdK....",
        "...KdmlpwmmdK...",
        "...KdmlpwmmdK...",
        "...KdmllmmddK...",
        "....KdmmmmdK....",
        ".....KdmmdK.....",
        ".....KdmmdK.....",
        ".....KdlmdK.....",
        ".....KdlmdK.....",
        "......KlmdK.....",
        "......KlmdK.....",
        "......KlmdK.....",
        "......KlmdK.....",
        ".......KmdK.....",
        ".......KmdK.....",
        ".......KmdK.....",
        ".......KmdK.....",
        "........KdK.....",
        "........KdK.....",
        "........KdK.....",
        ".........KK.....",
        "..........K.....",
    ]
    return parse_map(rows, key)


def build_flash():
    """Winning-wedge highlight, drawn at the twelve o'clock position."""
    p = Px(N, N)
    for y in range(N):
        for x in range(N):
            r, a = polar(x, y)
            if not (R_SEG_IN <= r <= R_SEG_OUT):
                continue
            if seg_index(a) != 0:
                continue
            t = (r - R_SEG_IN) / (R_SEG_OUT - R_SEG_IN)
            p.set(x, y, ramp_dither([PALE, BONE, BRIGHT], t, x, y))
    return p


def build_glow():
    """64x64 additive-ish bloom, dithered so it stays in palette."""
    S = 64
    c = (S - 1) / 2.0
    p = Px(S, S)
    for y in range(S):
        for x in range(S):
            d = math.hypot(x - c, y - c) / c
            if d > 1.0:
                continue
            t = (1.0 - d) ** 1.6
            if t > 0.72:
                p.set(x, y, BRASS_PALE)
            elif t > 0.40 and dither(x, y, (t - 0.40) / 0.32):
                p.set(x, y, BRASS_LT)
            elif t > 0.14 and dither(x, y, (t - 0.14) / 0.26):
                p.set(x, y, BRASS)
    return p


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    disc, rim, hub = build_disc(), build_rim(), build_hub()
    pointer, flash, glow = build_pointer(), build_flash(), build_glow()

    save(disc, f"{OUT}/wheel_disc.png")
    save(rim, f"{OUT}/wheel_rim.png")
    save(hub, f"{OUT}/wheel_hub.png")
    save(pointer, f"{OUT}/wheel_pointer.png")
    save(flash, f"{OUT}/wheel_segment_flash.png")
    save(glow, f"{OUT}/wheel_glow.png")

    with open(f"{OUT}/segment_layout.txt", "w") as f:
        f.write("idx  angle   class   payload\n")
        for i, (k, t) in enumerate(SEGMENTS):
            f.write(f"{i:3d}  {i*STEP:6.1f}  {t:6s}  {k}\n")

    # Composite check at 5x
    comp = Px(N, N)
    comp.blit(disc, 0, 0)
    comp.blit(rim, 0, 0)
    comp.blit(hub, 48, 48)
    img = comp.to_image()
    img.alpha_composite(pointer.to_image(), (56, 0))
    bg = Image.new("RGBA", (N, N), PAL[ABYSS])
    bg.alpha_composite(img)
    bg.resize((N * 5, N * 5), Image.NEAREST).save("/home/claude/px/preview_wheel.png")

    bad = verify_palette(disc.to_image())
    print("wheel written | off-palette colours:", len(bad))

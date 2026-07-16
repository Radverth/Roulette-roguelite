# Velvet Spin — Infernal Roulette Roguelite

A mobile-first roguelite where you gamble against the Devil's roulette wheel —
Dante's Inferno played across a betting table. Each Ante is a circle of the
descent, ruled by one of the Seven Deadly Sins the Devil turns against you.
Reach the chip target before your hands run out, buy Joker cards in the Velvet
Shop between circles, and survive the boss modifiers the House throws at you
every third circle.

Built with **Godot 4.3** (Mobile renderer, 1080×1920 portrait).

## How to play

- Pick a chip denomination (5 / 25 / 100) and tap the table to place bets.
- **SPIN** — the wheel decides. Straight bets pay 35:1, dozens/columns 2:1,
  even-chance bets 1:1.
- Reach the **GOAL** before your 4 hands are spent to clear the Ante.
  Fall short, or drop below 5 chips, and you are **RUINED**.
- Clearing an Ante pays a **bounty**: a flat reward plus interest on your
  banked chips — greed is rewarded, up to a cap.
- Between Antes, the Velvet Shop deals three rarity-weighted Jokers but
  reveals them **one at a time**: BUY the card in front of you, or TWIST to
  burn it forever and see the next. No going back — the shop is a gamble too.
- Survive **Circle VIII** — Cocytus, the Devil's own — and you climb out to
  see the stars again… or descend into **endless mode**, where the circles
  repeat forever.
- Each circle of the descent is ruled by one of the **Seven Deadly Sins**,
  a curse the Devil turns against you until the circle is cleared:
  - **LUST** (I) — the reds seduce and betray: winning red bets pay only half.
  - **GLUTTONY** (II) — the wheel refuses any stake under 50 chips.
  - **GREED** (III) — the House takes a fifth of every winning spin.
  - **SLOTH** (IV) — torpor: one fewer hand to clear the circle.
  - **WRATH** (V) — every total loss burns an extra quarter of your stake.
  - **ENVY** (VI) — the House covets your boldest wager: your largest bet
    pays nothing.
  - **PRIDE** (VII) — each consecutive win pays 15% less (max −60%).
  - **TREACHERY** (VIII) — the Devil's pocket yawns wide: zero strikes twice
    as often.
- Every third Ante is a **Boss Blind** with a house rule that lasts the whole
  Ante:
  - **THE CROUPIER** — red numbers pay nothing.
  - **THE COLLECTOR** — the House skims 10% of your chips after every spin.
  - **THE MIRROR** — Odd and Even swap their payouts.

## Project layout

```
scenes/            One scene per screen (UI is built in code)
scripts/autoloads/ Global singletons: GameManager, CardManager, SaveManager,
                   AudioManager (synthesized SFX), UpdateManager
scripts/game/      Game screen + betting table
scripts/ui/        Menu, shop, transitions, game over
scripts/utils/     Constants (payouts, wheel sequence, prices, bosses)
assets/            Art (wheel, table, cards, UI)
```

## Building

Open the project in Godot 4.3 and run, or export the Android preset.
CI (`.github/workflows/build.yml`) builds and releases a signed APK on every
push to `main` and bumps the version automatically. Provide
`KEYSTORE_BASE64`, `KEYSTORE_ALIAS`, and `KEYSTORE_PASSWORD` repository
secrets to sign with a release keystore; otherwise a debug keystore is used.

In-app update checks query this repository's latest GitHub release.

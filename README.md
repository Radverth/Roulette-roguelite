# Sin Wheel

A mobile idle/arcade roguelite for Android, built with Unity 2D (2022.3 LTS, C#).
Spin a 12-segment roulette wheel, push your luck against the Seven Deadly Sins,
and decide every spin whether to keep going or bank out. Sessions are designed
for 2–4 minutes of "one more spin".

**Vertical slice status:** core loop + progression + one sin boss (**Sloth**)
playable end-to-end. The other six sins ship as balance config with a modifier
seam waiting for their implementations.

## Core loop

1. Tap **SPIN** (1.5s cooldown, upgrade-reducible). Landing segment is rolled by
   weight, then the wheel animates onto it — visuals always match resolution.
2. 8 reward segments (coins, XP, blessing buffs, gems) vs 4 risk segments
   (damage, coin loss, hex debuffs, **sin summon**).
3. Sin summon chance scales with every summon segment hit until a boss awakens.
4. A run ends when resilience (shared HP meter) hits zero — unbanked coins are
   forfeit — or when the player banks out, which is never punished.
5. Surviving a sin boss escalates the reward multiplier every spin: risk and
   reward climb together.

## Art

The UI is built from the authored 32-colour pixel-art set in
`Assets/Resources/Art` (see its README for palette and sizing rules):

- Layered wheel: glow → disc (rotates, carries wedge icons) → rim → landing
  flash → hub → pointer. Wedge order is authored — risk wedges sit at indices
  1/4/7/10 so no two are adjacent — and `wheel.json` lists segments in that
  same order (`Art/Wheel/segment_layout.txt` is the contract).
- All text renders through the 5x7 bitmap font (`PixelText`); strings are
  upper-cased and restricted to the font's charset.
- The HUD is laid out on a 180x320 virtual-pixel grid; `CanvasScaler` uses
  constant-pixel-size with an integer factor (x6 on 1080x1920) so sprites never
  leave the pixel grid.
- Sin encounter cards (`card_*`) drop in as a tap-to-dismiss overlay when a
  boss awakens; sigils (`sigil_*`) mark the persistent encounter strip.
- Everything regenerates from `Tools/gen_*_px.py` — the palette in
  `Tools/palette32.py` is the single source of colour.

## Narrative

The wheel is a debt being serviced (design doc:
`Assets/Resources/Art/Narrative/NARRATIVE_DESIGN.md`; every line lives in
`Assets/Resources/Narrative/narrative_lines.json`). Delivery rule: narrative
never gates a spin. The Croupier bookends each run (speech plate at start,
ledger quote at end), sins announce themselves with authored arrival plates,
taunt mid-encounter without repeats, and get a last word if you flee. Reactive
lines watch behaviour (instant-bank streaks, long unbanked runs, repeat
encounters). Defeating a sin three times unlocks a lore fragment, shown on the
ledger between runs — eight in total.

The game opens on a main menu (intertitle card, PLAY, music volume slider,
fragment count). "Cathedral Rift" loops underneath at a default volume of 0.35,
adjustable and persisted in the save.

## Architecture

Everything is composed in code from a single scene (`Assets/Scenes/Main.unity`)
containing one `GameBootstrap` component. Apart from the pixel-art sprites, no
prefabs or binary scene assets — the wiring stays reviewable as text.

```
GameBootstrap (MonoBehaviour, scene entry)
 └─ GameContext (composition root, no singletons)
     ├─ ConfigLoader ──── JSON in Assets/Resources/Config (all balance data)
     ├─ SaveSystem ────── local JSON + ICloudSaveProvider seam (GPGS stub)
     ├─ AnalyticsSystem ─ IAnalyticsSink seam (spin freq, session length,
     │                    boss-encounter drop-off events built in)
     ├─ UpgradeSystem ─── permanent meta tree + per-sin resistance trees
     ├─ HealthSystem / CurrencySystem / XpSystem / BuffSystem
     ├─ SinBossSystem ─── summon scaling, weighted boss pick, encounter state
     │    └─ BossModifierBase hooks: ModifySegments, ModifyCooldown,
     │       ModifyCoinGain, OnSpinStarted, OnSpinResolved, IsDefeated
     │       (SlothModifier implemented; factory maps the other six)
     ├─ SpinSystem ────── state machine: Idle → Spinning → Resolving → Cooldown
     ├─ GameManager ───── run lifecycle: start, death, bank-out
     └─ HudController ─── UGUI built at runtime + WheelController
                          (procedural wheel texture, eased spin, juice)
```

### Spin state machine

`SpinSystem` owns `Idle → Spinning → Resolving → Cooldown → Idle`. The outcome
is pre-rolled from segment weights; `WheelController` just eases the wheel onto
the winning segment (ease-out cubic, per-segment ticks, land thunk + haptics).
Cooldown length passes through upgrades first, then the active sin's
`ModifyCooldown` hook — which is how Sloth doubles it.

### Sin bosses

Each sin is a `SinBossConfig` entry in `sins.json` plus a `BossModifierBase`
subclass. An encounter overlays the normal wheel for N spins; ending conditions
are surviving the duration or the sin-specific early-out. **Sloth** (unlocked at
level 1): cooldown ×2 (softened by its 3-tier resistance upgrade), broken early
by filling a resist meter with consecutive spins. Bosses unlock by level and are
then drawn weighted-random, so returning players face variety.

### Data-driven balance

All tuning lives in `Assets/Resources/Config/`:

| File | Contents |
|---|---|
| `tuning.json` | cooldowns, HP, summon-chance scaling, XP curve, buff/debuff values |
| `wheel.json` | the 12 segments: type, weight, amount, label, color |
| `sins.json` | all seven sins: unlock level, duration, modifier params, payouts |
| `upgrades.json` | meta tree (cooldown/HP/banking) + per-sin resistance trees |

Rebalancing requires no code changes; segment types are the only enum contract.

### Save & progression

`SaveData` (meta coins, gems, level/XP, upgrade tiers, records) persists as JSON
in `Application.persistentDataPath`, written at every commit point (bank, run
end, purchase, level-up, app pause). `ICloudSaveProvider` is the Google Play
Games Services seam — last-write-wins on timestamp; `GooglePlayCloudSaveProvider`
is stubbed for the GPGS plugin.

### Analytics

`AnalyticsSystem` fans out to `IAnalyticsSink` implementations (debug-log sink
included; plug Firebase/Unity Analytics in later). First-class events: `spin`,
`session_start/end` (length + spin count), `boss_encounter_start/end` with
outcome (`defeated` / `survived` / `banked_out` / `died`) — the drop-off data
that sin difficulty curves get tuned on.

## CI/CD

`.github/workflows/android-build.yml` builds an APK with
[game-ci/unity-builder](https://game.ci/) on every push/PR to `main`
(plus manual dispatch), caches `Library/` keyed on project content, uploads the
APK as a workflow artifact, and fails loudly on any compile error.

### Setup (one-time)

1. Add repo secrets `UNITY_LICENSE` (contents of your `.ulf` license file),
   `UNITY_EMAIL`, `UNITY_PASSWORD`.
   See [game-ci activation docs](https://game.ci/docs/github/activation).
2. Optional release signing: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`,
   `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`. Without these the APK is
   debug-signed — still installable for testing.

## Running locally

Open the project in Unity **2022.3.45f1** (or any 2022.3 LTS), open
`Assets/Scenes/Main.unity`, press Play. The game is portrait 1080×1920
reference; everything scales with `CanvasScaler`.

## Roadmap

- Remaining six sin modifiers (hooks already in place — see `BossModifiers.cs`)
- Wheel configurations unlocked by level (13th segment, rare-odds boost)
- Cosmetics layer: wheel skins, spin VFX, sin art variants (IAP, cosmetic-only)
- Rewarded ads: small run-currency boost, one extra continue per run
- GPGS auth + cloud saves, real analytics sink
- Authored art/audio replacing the procedural placeholders

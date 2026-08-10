# Sin Wheel

A mobile idle/arcade roguelite for Android, built with Unity 2D (2022.3 LTS, C#).
Spin a 12-segment roulette wheel, push your luck against the Seven Deadly Sins,
and decide every spin whether to keep going or bank out. Sessions are designed
for 2–4 minutes of "one more spin".

**Status:** all seven sins implemented with break conditions, the Forge
(wedge drafting), the seven-table descent, the seven Marks, and seven
interlude mini-games. Every number lives in JSON under
`Assets/Resources/Config/`.

## Core loop

The wheel is a debt being serviced, and the loop puts a decision on five
different clocks (design docs: `Art/Loop/LOOP_DESIGN.md` and
`Art/Escalation/ESCALATION_DESIGN.md`):

| Horizon | System | Decision |
|---|---|---|
| Per spin | **Streak** | protect the chain or chase the bigger wedge |
| Per encounter | **Break conditions** | fight the sin or wait it out |
| Per table | **The invite** | go deeper, or cash out and end the run |
| Per run | **Quota + Notice** | when to leave, and how much to leave with |
| Between runs | **The Forge + Marks** | what wheel you build, and how hard it gets |

1. Tap **SPIN** (1.5s cooldown, upgrade-reducible). The landing wedge is rolled
   by weight, then the wheel eases onto it — visuals always match resolution,
   and the resting angle is biased toward the seam it shares with its richest
   neighbour, so the ticker often settles *just* past a jackpot.
2. Reward wedges (coins, XP, blessings, shards, jackpot) vs risk wedges
   (wounds, coin loss, hexes, **sin summon**).
3. **Streak**: three reward wedges in a row starts a chain, each further reward
   adds +0.25x, any risk wedge wipes it. Capped at 3.0x.
4. **Notice** is the escalation made visible: eight segments that fill with
   spins, tithes and a fat purse, with an eye that opens in four stages. At
   full, the next risk wedge guarantees a summon and the meter resets.
5. **Tithe** converts half the purse without ending the run — safety now,
   bought with a segment of Notice. **Bank** takes it all and walks.
6. Every run carries a **quota** drawn from the debt. Meet it and the debt
   falls and the quota eases; miss it and the debt grows, the quota compounds,
   and the house splices another risk wedge into your ring. Leaving early is
   not the safe option, it is a different risk.
7. A run ends when resilience hits zero — unbanked coins forfeit — or when the
   player banks out.

### How often do sins appear?

The summon wedge is 5 of 66 weight on the starting ring — **7.6% of spins** —
and landing it rolls `sinSummonBaseChance + noticeFill` (0.15 up to a 0.95 cap).
Separately, a full Notice meter forces a summon on the next risk wedge, and risk
wedges are **28.8%** of the starting ring. After any encounter ends the house
looks away for `summonGraceSpins` spins, so sins stay events rather than the
default state.

Simulated over a 40-spin run: **~1.2 encounters**, each 11–14 spins — roughly a
third of a run spent inside a sin. (Before the grace period and the notice
retune it was ~1.8 encounters, or over half the run, which is what the first
playtest ran into.) All five knobs — `sinSummonBaseChance`, `noticePerSpin`,
`noticeOnEncounterEnd`, `summonGraceSpins` and the summon wedge's `weight` —
live in JSON.

### How to play

A nine-plate wizard covers the debt, the spin, the chain, the Notice, the
quota, the tithe, the seven and the Forge. It runs automatically before a
player's first spin and is available afterwards from **HOW TO PLAY** on the
menu. The copy is in `Assets/Resources/Config/tutorial.json` — keep body lines
to 24 characters and titles to 12, or they overflow the plate.

### The descent: seven tables

A run is not a flat sequence of spins. Cross a coin threshold and the house
**invites you deeper**: accept and the stakes multiply along with the danger,
decline and you are cashing out — the run ends there. The fork recurs six times.

| Table | Adds | Reward x |
|---|---|---|
| I | baseline | 1.0 |
| II | one extra risk wedge | 1.3 |
| III | Notice fills 25% faster | 1.7 |
| IV | **sins stack** — two at once | 2.2 |
| V | resilience no longer restores | 2.8 |
| VI | a cursed wedge each table | 3.5 |
| VII | **the Croupier sits down** | 5.0 |

Advancement is tied to winnings, not spin count, so a cautious player and a
greedy one see different amounts of the game. At his table the Croupier takes
one trick for the run, rotating: he calls the wedge before you spin, or taxes
every bank a fifth, or disables break conditions, or withholds the result until
you commit to another spin.

`thresholdBase` was simulated rather than guessed — the doc is right that it is
the most important number here. At **340 with 1.40 growth**, a player who takes
every invite ends: 19% Table II, 17% III, 17% IV, 34% V, 11% VI, 1% VII. Most
runs finish at IV or V and reaching VII stays a story worth telling; cashing out
at even a quarter of invites pulls the whole curve back to II–III.

### Marks: the ramp across runs

Every 1200 of debt cleared takes a Mark, in order, permanently: the quota rises
20%, a wound joins the ring at run start, sins last four more spins, Notice
begins a quarter full, tithes cost two segments, every counted break condition
wants one more, and finally the Croupier sits from Table V instead of VII.
Difficulty is never picked from a menu — it arrives as a consequence of paying.

### Interludes: seven mini-games

Two are offered at each table transition, and once mid-run when Notice passes
75% (the **Side Table**, where winning closes the eye by three segments). One
verb each, ten seconds maximum, one thumb, and failure never ends a run:

| Interlude | Sin | Verb | Win | Lose |
|---|---|---|---|---|
| The Ember | Wrath | stop the needle in the band | coin by accuracy | take a wound |
| The Mirror | Pride | reproduce the pattern | temper a wedge free | nothing |
| The Shell | Lust | follow the coin | next spin is a reward | Notice +1 |
| The Feast | Gluttony | hold to take, stop in time | escalating coin | lose 25% of the purse |
| The Toll | Greed | five taps on the beat | coin by accuracy | nothing |
| The Vigil | Sloth | release inside the window | cooldown down for the table | Notice +1 |
| The Understudy | Envy | find the flaw | see the next three spins | Notice +1 |

Rotation rules do the work: never the same one twice running, the full set
before any repeat, never one whose sin is at the table, and skip is always
available for a flat 40 coins — deliberately worse than an average play. Bands
narrow, patterns lengthen and windows tighten with table depth.

### The Forge

Between runs, three cards: **Add** a wedge, **Strike** one permanently, or
**Temper** one up a tier (three max). Take one, the rest burn; one reroll per
visit costs relics. Offers are weighted, not uniform — Strike never appears at
twelve wedges or fewer, Temper only targets wedges you own, Add and Strike are
each guaranteed within any three visits, and cursed offers (a strong reward
that drags a risk wedge in with it) only appear once the ring reaches fifteen.

Because the ring is a deck, the wheel disc is rasterised at runtime for
whatever wedge count you have built (`WheelDiscRenderer`, a C# port of the
authored generator's polar rasteriser — same palette, same dither, same seam
rules).

### Breaking a sin

Every sin states how to break it, and breaking it leaves a benefit for the rest
of the run — that is what makes fighting worth the risk over waiting out the timer.

| Sin | Break | Reward |
|---|---|---|
| Pride | three Humility wedges in a row | its shrink cannot apply again this run |
| Greed | land the Jackpot | reclaim the whole tithe pool at once |
| Wrath | three wounds while above 25% resilience | its spliced teeth become coin |
| Envy | land a wedge untouched all run | nothing left to copy; it leaves |
| Lust | land the same wedge twice running | the ring locks in place |
| Gluttony | tithe during the encounter | it takes a cut and goes |
| Sloth | fill the resist meter with unbroken spins | cooldown drops below baseline |

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
     ├─ WheelRingSystem ─ the ring is the build: persistent wedge slots with
     │                    temper tiers, warped per run by sin splices, Lust's
     │                    shuffles and unpaid-debt penalty wedges
     ├─ NoticeSystem ──── eight segments of visible summon pressure
     ├─ StreakSystem ──── the per-spin chain and its multiplier
     ├─ DebtSystem ────── quota, debt, tithe accounting
     ├─ ForgeSystem ───── weighted draft offers between runs
     ├─ TableSystem ───── the descent: invites, per-table effects, the
     │                    Croupier's seat at the deepest table
     ├─ MarkSystem ────── seven permanent difficulty steps bought by debt
     ├─ InterludeSystem ─ mini-game rotation, offers and payouts
     │    └─ InterludeGame: Ember, Mirror, Shell, Feast, Toll, Vigil,
     │       Understudy — one verb each, hosted by InterludeScreen
     ├─ GameManager ───── run lifecycle: start, tithe, death, bank-out, settle
     └─ HudController ─── UGUI built at runtime + WheelController + ForgeScreen
                          (runtime-rasterised disc, eased spin, juice)
```

### Spin state machine

`SpinSystem` owns `Idle → Spinning → Resolving → Cooldown → Idle`. The outcome
is pre-rolled from segment weights; `WheelController` just eases the wheel onto
the winning segment (ease-out cubic, per-segment ticks, land thunk + haptics).
Cooldown length passes through upgrades first, then the active sin's
`ModifyCooldown` hook — which is how Sloth doubles it.

### Sin bosses

Each sin is a `SinBossConfig` entry in `sins.json` plus a `BossModifierBase`
subclass, hooking the ring, cooldown, reward multiplier, coin gain, damage,
tithes and the landed wedge. All seven are implemented, each with the break
condition and run-long boon in the table above. They unlock by level and are
then drawn weighted-random, so returning players face variety.

### Data-driven balance

All tuning lives in `Assets/Resources/Config/`:

| File | Contents |
|---|---|
| `tuning.json` | cooldowns, HP, XP curve, buffs, Notice, Streak, quota/debt, Forge, near-miss |
| `wheel.json` | the wedge catalog (type, weight, amount, class, rarity, temper scale) and the starting ring |
| `sins.json` | all seven sins: unlock level, duration, modifier params, break targets, payouts |
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

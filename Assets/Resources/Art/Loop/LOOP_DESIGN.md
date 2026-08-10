# Sin Wheel — Revised Loop

## Diagnosis

As designed, the player has one decision — spin or bank — and it is solvable.
Once someone works out the expected-value threshold, they execute it every run
and the rest is watching an animation. That is a slot machine wearing a
roguelite's clothes.

The four systems below add decisions in three different time horizons:

| Horizon | System | Decision |
|---|---|---|
| Per spin | Streak | Protect the chain or chase the bigger wedge |
| Per encounter | Break conditions | Fight the sin or wait it out |
| Per run | Quota + Notice | When to leave, and how much to leave with |
| Between runs | The Forge | What kind of wheel you are building |

None of them replace the spin-or-bank tension. They give it context.

---

## 1. The Forge — the wheel *is* the build

Between runs the player is offered three cards. Each is one of three actions:

- **Add** — splice a new wedge into the ring
- **Strike** — remove a wedge permanently
- **Temper** — upgrade an existing wedge one tier (three tiers max)

Take one. The other two are discarded. One reroll per visit, costing relics.

This is the single most important change in the document, because it converts a
fixed wheel into a deck. Run-to-run variety, a mastery curve, and legible risk
all fall out of it — the player can *see* their wheel getting greedier.

It also costs almost nothing architecturally: `WheelDefinition.BuildActiveRing`
already assembles the ring at runtime from a list.

### Offer rules

Randomly generated offers produce boring wheels, so the pool is weighted:

- **Never offer Strike when the ring is at 12 wedges or fewer.** A ring below
  twelve stops reading as a wheel.
- **Never offer a Temper for a wedge the player does not own.**
- **Guarantee at least one Add and one Strike across any three consecutive
  visits**, so nobody gets locked into one shape by variance.
- **Cursed offers appear from ring size 15 upward** — the more wedges, the more
  the house has to work with.

### Rarity

| Rarity | Effect | Cost |
|---|---|---|
| Common | Standard wedge, tier 1 | Free |
| Rare | Higher base value, or a Temper of two tiers | Free |
| Cursed | Strong reward wedge that adds a Risk wedge alongside it | Free, but permanent |

Cursed is where the interesting decisions live. A second Jackpot that drags a
second Damage wedge in with it is a real question, not a stat check.

### The trap to avoid

**Do not add per-run relics on top of wedge drafting.** Two build systems
fighting for the same attention makes both feel thin. The wheel is the build.

---

## 2. Sins need win conditions

Five of the seven are currently weather — you endure them for twelve spins.
Only Pride and Sloth have counterplay. Every sin should state how to break it,
and the break should be *thematically* the thing that sin cannot survive.

| Sin | Break condition | Reward for breaking |
|---|---|---|
| **Pride** | Land three Humility wedges consecutively | Keeps its narrowed odds off for the rest of the run |
| **Greed** | Land the Jackpot | Reclaim the entire tithe pool at once |
| **Wrath** | Take three Damage wedges without dropping below 25% resilience | The two spliced wedges become Coin for the rest of the run |
| **Envy** | Land a wedge you have not hit all run | It has nothing left to copy; encounter ends |
| **Lust** | Land the same wedge twice running after a shuffle | Ring locks in place for the rest of the run |
| **Gluttony** | Bank *during* the encounter | It takes a cut and leaves; run continues |
| **Sloth** | Fill the resist meter with unbroken spins | Cooldown drops below baseline for the rest of the run |

Gluttony's is the one I would build first. An escape hatch that costs you,
attached to the sin whose entire theme is *you should have stopped* — the
mechanic and the character are the same statement.

Note the pattern in the rewards: breaking a sin does not just end it, it leaves
a **permanent benefit for the remainder of the run**. That is what makes
fighting worth the risk over simply waiting out twelve spins.

---

## 3. Quota — make banking cost something

Currently banking is free, so it is always correct the moment EV turns
negative. There is no tension, only arithmetic.

**Each run carries a quota** drawn from the debt. Bank at or above it and the
debt reduces. Bank below it and the debt *grows* — the next run starts with a
higher quota and one extra Risk wedge.

Now leaving early is not the safe option. It is a different risk.

### Partial banking — the better version

Let the player bank a **tithe**: convert part of the purse and keep spinning.
Each tithe raises the Notice meter by one full segment.

This is strictly better than the all-or-nothing version because it keeps the
player in the run. They get a real trade — safety now against attention later —
without a loading screen. Build this one if you only build one.

### Starting numbers

```
quota_base        = 250
quota_growth      = 1.15   per unpaid run, compounding
quota_relief      = 0.92   per paid run, floored at quota_base
tithe_notice_cost = 1 segment (of 8)
debt_start        = 5000
```

---

## 4. Notice — make the escalation visible

`summonChancePerSpin` already escalates the encounter rate. The player cannot
see it, so it reads as bad luck rather than mounting pressure.

Surface it as an eight-segment meter with an eye that opens in four stages as
it fills. At full, the next Risk wedge guarantees a summon and the meter resets.

Dread you can watch is worth far more than dread you cannot. This is the
cheapest fun in the document — the underlying maths already exists.

Notice rises from: spins taken, tithes banked, high purse held. It falls from:
breaking a sin, landing Humility, ending an encounter.

---

## 5. Streak — tension on every spin

Three reward wedges in a row starts a chain. Each further reward adds `+0.25x`
to a multiplier. **Any Risk wedge wipes it.**

This puts a decision on every single spin rather than once per run: with a 2x
chain live, the correct play may be to tithe immediately and bank the
multiplier's value rather than risk one more spin.

Cap at 3.0x so it cannot dominate the economy.

---

## 6. Near-miss tuning

`RunController.RequestSpin` currently jitters the landing angle uniformly
within the wedge. Bias it instead so the ticker frequently settles *just* past
a Jackpot or Cursed wedge.

This is the entire psychological engine of wheel games and it costs nothing but
a change to one line of animation maths. It does not alter odds — the outcome
is still chosen before the spin — only where within the wedge the wheel comes
to rest.

Weight the jitter toward the boundary shared with the highest-value neighbour,
roughly 60/40.

---

## 7. What I would not build

- **Branching path maps.** No room in a two-minute run, and it fights the
  wheel for the role of "the interesting object".
- **Total loss on bust, forever.** Loss aversion makes players bank early and
  never see the content you spent the most time on. Bust should forfeit the
  purse, not the run's Forge progress.
- **A second currency for the Forge.** Relics already exist. Adding a third
  purse to a game whose whole subject is debt is thematically funny and
  practically miserable.
- **Timed events or daily login streaks.** They belong to a different genre and
  would undercut the framing entirely — the wheel should feel like it is always
  there, not like it is waiting for you.

---

## Asset mapping

| Asset | System |
|---|---|
| `draft_{rarity}_{action}.png` | Forge offers — 9 combinations, plus card back |
| `action_add / strike / temper / reroll.png` | Forge verbs |
| `tier_pips_0..3.png` | Temper level on a wedge |
| `wedge_slot_{empty/filled/marked}.png` | Ring editor positions |
| `forge_banner.png` | Forge screen header |
| `notice_track.png`, `notice_fill_{cold/warm/critical}.png` | Notice meter |
| `notice_eye_0..3.png` | Notice stage indicator |
| `streak_frame.png`, `streak_pip_{full/hot/empty}.png` | Chain display |
| `streak_break.png` | Chain-wipe burst |
| `multiplier_badge{,_hot}.png` | Live multiplier |
| `quota_track.png`, `quota_marker.png` | Run obligation |
| `debt_seal_{owed/reduced/grown}.png` | Ledger state |
| `button_tithe.png` | Partial bank |
| `break_{sin}.png` | Break-condition glyph, one per sin |

---

## Build order

If this ships incrementally, the order that delivers the most fun per unit of
work:

1. **Notice meter** — the maths exists, it just needs a face
2. **Streak** — one counter, one multiplier, huge tension gain
3. **Gluttony's break condition** — proves the pattern with one sin
4. **Tithe** — converts banking from arithmetic into a decision
5. **The Forge** — the largest job, and the one that makes it a roguelite
6. **Remaining six break conditions** — polish once the pattern is proven

One through four are a weekend each and would carry the game a long way on
their own.

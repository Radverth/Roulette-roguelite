# Sin Wheel — Pledges, the Nudge & Unified Scoring

## The gap this closes

Everything built so far puts decisions *around* the wheel. The Forge is before
the run. The invite and interludes sit between tables. Banking happens after a
spin resolves.

The spin itself — the verb the player performs forty times a run — is still
tap, watch, receive.

These three changes put a decision inside that moment, give the player
persistent objects that combo, and make the maths visible.

---

# Part One — The Nudge

After the wheel settles but **before it resolves**, the player may push it one
wedge in either direction.

## The cost: Notice

| Push | Cost |
|---|---|
| One wedge | 1 Notice segment |
| Two wedges | 3 Notice segments |

No new resource. No allowance meter. The price is paid in the currency of being
watched, because that is what the fiction says is happening — **you are
cheating the house, and the house notices.**

### Why Notice and not a per-run allowance

An allowance would be cleaner to read, and it is what most games would reach
for. Two reasons against it:

1. It would be a fifth meter in a game that already asks the player to track
   coin, relics, resilience, quota and Notice.
2. It makes nothing else deeper.

Paying in Notice does the opposite. It converts Notice from a **passive dread
meter into a spending decision** — currently the player only watches it fill.
Now it carries two opposed meanings at once: *how much danger am I in*, and
*how much control can I still buy*.

It also retroactively upgrades systems already built. "Reduce Notice by 3" as
an interlude reward stops being purely defensive and becomes **fuel for three
more nudges**. Sexton's Key refunding Notice on a broken sin becomes an engine.
Systems that ran in parallel start interlocking, which is the whole point.

## Rules

- **Nudge is offered on every spin**, with a short window (roughly 1.2s) before
  auto-resolve. The window shortens by table depth.
- **The ghost marker previews the destination** — outline only, never filled,
  so it can never be mistaken for a settled result.
- **Nudging never changes the odds**, only the outcome after the fact. The roll
  was still fair. This distinction matters if you ever publish odds.
- **Break conditions count nudged landings.** Nudging onto a Humility still
  counts toward breaking Pride. It costs Notice to do it, which is the balance.
- **At full Notice, nudging is disabled** rather than triggering a summon. Being
  unable to cheat when you most need to is a better punishment than a summon,
  and it makes the meter's top end genuinely frightening.

---

# Part Two — Pledges

Five slots, always visible on the HUD. Things put up against the debt, granting
power while the house holds them.

**The design rule: a Pledge changes a rule, never just a number.** That is what
makes them combine rather than stack. "+10% coin" is arithmetic. "Risk wedges
pay coin instead of damage" rewrites what a wheel *is*, and every other
decision in the game bends around it.

## The starter set

### Common
| Pledge | Effect |
|---|---|
| **Widow's Ring** | Risk wedges pay coin at half value instead of dealing damage |
| **Long Coat** | Nudges cost 1 less Notice, minimum 1 |
| **Sexton's Key** | Breaking a sin refunds 3 Notice |
| **Pauper's Luck** | +1 Mult for every empty Pledge slot |
| **The Tally** | Every seventh spin is a guaranteed reward wedge |

### Uncommon
| Pledge | Effect |
|---|---|
| **Cracked Mirror** | Pride never appears; every other sin lasts 2 spins longer |
| **The Thumb** | Nudge two wedges for the price of one — but the house picks the direction 40% of the time |
| **Ash Ledger** | Banking grants +2 Mult on your next run, once |
| **Iron Tithe** | Tithing costs no Notice, but caps at 30% of the purse |
| **Understudy** | Permanently copy the break-reward of the last sin you broke |
| **Gravedigger's Cut** | Every wedge struck in the Forge grants +0.5 Mult, permanently |

### Rare
| Pledge | Effect |
|---|---|
| **Blind Wager** | The wheel is hidden while spinning; all rewards ×1.5 |
| **Seventh Hour** | At Table VII, all sins are disabled |
| **Debtor's Crown** | +1 Mult per Mark taken |
| **Hollow Coin** | The Jackpot wedge appears twice in the ring |
| **The Long Game** | +0.2 Mult per spin this run; resets to zero on banking |

### Cursed
| Pledge | Effect |
|---|---|
| **Croupier's Favour** | +3 Mult, but he sits from Table III |
| **Blood Price** | Nudges cost resilience instead of Notice |
| **Open Ledger** | See the next wedge before spinning — Notice fills at double rate |
| **Widow's Debt** | ×2 Mult, but busting doubles your outstanding debt |

## Where the combos live

The set is deliberately built so several pairs produce a *build* rather than a
sum:

- **Widow's Ring + a risk-heavy Forge.** Strike your safe wedges, load the ring
  with Damage, and the wheel becomes a coin machine. The player has inverted
  the game's own risk language, which is the best feeling a roguelite offers.
- **Blood Price + Long Coat + high resilience.** Nudge constantly, pay in
  health, never touch Notice.
- **The Long Game + Iron Tithe.** Never fully bank, tithe to stay solvent, ride
  a multiplier that only grows.
- **Pauper's Luck + one Cursed.** A deliberately near-empty board, running on a
  single overwhelming effect.

## Acquisition and slots

- Offered in the Forge alongside wedge cards, roughly one offer in three.
- Five slots. A sixth requires selling one — **selling refunds half in
  relics**, so experimentation is not punished.
- Cursed Pledges cannot be sold. That is the curse.
- Pledges persist across runs but are **lost when a Mark is taken** — the house
  reclaims what was pledged as the debt clears. This is what stops a permanent
  runaway build and gives ascension real teeth.

---

# Part Three — Take × Mult

## The problem

Right now a spin resolves silently into a number. The player sees the result
but never the reasoning, which means the systems feeding it are invisible.

## The fix

**Unify every multiplier into one figure and assemble it in front of the
player**, term by term, with sound:

```
        TAKE  40   ×   MULT  1.0
   + Table III            × 1.7
   + Streak x3            × 2.0
   + Debtor's Crown       × 1.4
   − Greed's tithe        × 0.7
   ─────────────────────────────
        TAKE  40   ×   MULT  3.3   =   132
```

Each term flies in as a chip, colour-coded by operation — brass for additions,
violet for multipliers, red for reductions — so gain or loss registers before
the number does.

This is **pure presentation**. Zero mechanical change. It is also probably the
highest-value item in this document, because it is the difference between a
game that has systems and a game where the player can *feel* their systems
firing.

## It also fixes the competing-multiplier problem

Table gives a multiplier. Streak gives another. Pledges would add more. Three
multipliers displayed separately means none of them is felt.

Feeding all of them into one visible Mult resolves this — Streak and Table stop
competing for attention the moment they contribute to the same number.

---

## The trade: cut Shards

Coin, Relics, Shards, debt, quota, tithe, and now Pledges is too many ledgers
for a game whose central question is *push or leave*.

**Fold cosmetics onto Relics and delete Shards entirely.** The Shard wedge
becomes a second Coin wedge at higher value. This reclaims the headroom Pledges
need, and nothing of value is lost — Shards were only ever a cosmetic gate
wearing a third purse's clothes.

---

## What we are deliberately *not* taking from Balatro

- **Hand selection.** Balatro's core decision is choosing five cards from eight.
  Sin Wheel's is push-or-leave. Grafting a selection step onto a wheel would
  produce a worse card game, not a better wheel game.
- **Exponential scoring.** Balatro's numbers reach the absurd because that
  escalation *is* its joke. Sin Wheel's tone is a debt you cannot clear — the
  numbers should stay legible and grim.
- **Twenty-plus joker rarities and editions.** Foil, holographic, polychrome
  are collection hooks for a game built around a long meta-collection. Twenty
  Pledges with four rarities is the right size here.
- **Endless mode.** Same reasoning as the Tables ceiling — reaching the
  Croupier should remain an ending.

---

## Starting numbers

```
nudge_cost_one          = 1     Notice segment
nudge_cost_two          = 3     Notice segments
nudge_window            = 1.2s  at Table I, −0.1s per table, floor 0.6s
nudge_disabled_at       = 1.0   Notice full

pledge_slots            = 5
pledge_offer_rate       = 0.33  of Forge offers
pledge_sell_refund      = 0.5
pledge_lost_on_mark     = true

mult_base               = 1.0
mult_display_min_terms  = 2     below this, skip the assembly animation
term_chip_duration      = 0.18s each, overlapping
```

`nudge_window` is the number to watch. Too long and every spin becomes a
deliberation, which destroys the pace that makes the wheel feel good. Too short
and the mechanic feels unfair. 1.2s is a starting guess — it wants playtesting
before anything else here.

---

## Asset mapping

| Asset | System |
|---|---|
| `pledge_{name}.png` | 20 Pledge cards, rarity ticks along the bottom edge |
| `emblem_{name}.png` | 20 emblems, 24px, silhouette-first |
| `pledge_slot_{empty/locked/highlight}.png` | The five HUD slots |
| `pledge_sell.png` | Refund action |
| `nudge_{left/right}_{ready/costly/disabled}.png` | The spin-moment decision |
| `nudge_cost_pip_{spent/free}.png` | Price, in Notice segments |
| `nudge_ghost.png` | Destination preview — outline only, never filled |
| `nudge_charge_bar.png` | Remaining affordable nudges |
| `score_panel.png` | Take × Mult frame |
| `term_chip_{add/mult/reduce}.png` | One line of the maths, colour by operation |
| `big_number_plate{,_hot}.png` | The assembled total |
| `op_{times/plus/equals/minus}.png` | Operator glyphs |

---

## Build order

1. **Take × Mult display.** Pure presentation, no mechanical risk, and it makes
   every system you already shipped legible. Do this first regardless of what
   else gets built.
2. **The Nudge**, costing Notice. Small surface area, transforms the spin.
3. **Five Common Pledges.** Proves the slot UI and the combo principle with the
   lowest-variance effects.
4. **Cut Shards**, fold cosmetics onto Relics.
5. **Uncommon and Rare Pledges**, once the Common set has been played enough to
   know which effects people actually build around.
6. **Cursed Pledges last.** They are the most likely to break the economy and
   the easiest to tune once everything beneath them is stable.

Items one and two together are perhaps a week and would change how every
existing system reads.

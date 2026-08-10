# Sin Wheel — Escalation & Interludes

## Diagnosis

Both requests point at the same underlying problem: **the run has no shape.**

Spin one and spin forty are mechanically identical. Nothing builds, nothing
crests, nothing tells the player where they are. That is what makes a good loop
feel repetitive — not the number of systems, but the absence of an arc.

So these two changes are one change. **Tables give the run an arc that
steepens. Interludes sit on the beats of that arc** rather than interrupting it
at random intervals, which would fight the spin rhythm rather than punctuate it.

---

# Part One — Tables

## The descent

A run is no longer a flat sequence. It is a descent through seven tables.

You start at **Table I**. Cross a coin threshold and the house **invites you
deeper**. Accept and the stakes multiply — and so does the danger. Decline and
you bank immediately and the run ends.

That framing matters. Declining is not "playing safe and continuing." It is
cashing out. The invite is a hard fork, and it recurs six times a run.

**Advancement is tied to success, not spin count.** The house moves you inward
because you are winning, which is exactly what a house does. It also means a
cautious player and an aggressive player see different amounts of the game,
which is the whole point of a difficulty ramp.

## The seven tables

| Table | Adds | Reward × |
|---|---|---|
| **I** | Baseline | 1.0 |
| **II** | One extra Risk wedge spliced into the ring | 1.3 |
| **III** | Notice fills 25% faster | 1.7 |
| **IV** | **Sins may stack** — two active at once | 2.2 |
| **V** | Resilience no longer restores between tables | 2.8 |
| **VI** | One Cursed wedge spliced in uninvited, each table | 3.5 |
| **VII** | **The Croupier sits down** | 5.0 |

Table IV is where the design gets genuinely interesting, because sin modifiers
were built as independent hooks — Greed taxing payouts while Lust reshuffles
the ring composes without any new code. The existing `SinEncounterController`
needs to hold a list rather than a single `Active`, and the stacking falls out.

Table VII should be rare. Most runs should end at IV or V. Reaching VII is the
story a player tells someone else, and it stops being that if it is routine.

### The Croupier's seat

At Table VII he takes one modifier for the run, rotating each time:

- He calls the wedge before you spin. If he is right, you get nothing.
- Every bank is taxed a flat 20%.
- Break conditions are disabled — sins run their full duration.
- The wheel spins without showing you the result until you commit to another.

One per run, announced on arrival. He is the difficulty *and* the payoff for
getting that far.

---

# Part Two — Marks

Tables ramp within a run. **Marks ramp across runs.**

As the debt clears, the house raises the stakes permanently. Seven Marks, taken
in order, each a named modifier on all future runs.

| # | Mark | Effect |
|---|---|---|
| I | **The Ledger** | Quota +20% |
| II | **The Extra Wedge** | One Risk wedge in the ring at run start |
| III | **The Long Hour** | Sins last four more spins |
| IV | **The Open Eye** | Notice begins at 25% |
| V | **The Doubled Tithe** | A tithe costs two Notice segments, not one |
| VI | **The Tighter Chain** | Every break condition requires one more |
| VII | **The Croupier's Seat** | He sits from Table V, not Table VII |

This is ascension, but it never asks the player to pick a difficulty from a
menu. It arrives as a consequence of paying, in the game's own voice. The
player who has taken four Marks has visibly earned a harder game.

**Marks are not optional and not reversible.** A "play without Marks" toggle
would let players opt out of the ramp, which defeats it. If a difficulty valve
is needed later, make it a *cosmetic* prestige reset, not a modifier switch.

---

# Part Three — Interludes

Seven mini-games, one per sin. They fire at **table transitions** — the natural
breath point, where the player has just made a decision and is about to enter
harder territory.

Two are offered. Pick one, or skip.

| Interlude | Sin | Verb | ~Time | Success | Failure |
|---|---|---|---|---|---|
| **The Ember** | Wrath | Timing — stop the needle in the band | 6s | Coin, scaled by band | Take damage |
| **The Mirror** | Pride | Memory — reproduce the pattern | 10s | Temper a wedge, free | Nothing |
| **The Shell** | Lust | Tracking — follow the coin | 8s | Next spin guaranteed reward | Notice +1 |
| **The Feast** | Gluttony | Push-your-luck — tap to take, stop in time | 10s | Escalating coin | Lose 25% of purse |
| **The Toll** | Greed | Rhythm — five taps on the beat | 8s | Coin × accuracy | Nothing |
| **The Vigil** | Sloth | Hold — release inside the window | 7s | Cooldown down for the table | Notice +1 |
| **The Understudy** | Envy | Compare — find the flaw | 8s | See the next three outcomes | Notice +1 |

Seven distinct verbs. That is the design constraint that matters most — two
tracking games or two push-your-luck games would collapse into "the mini-game"
in the player's memory regardless of how differently they are dressed.

## The rules that stop them going stale

1. **Never offer the same interlude twice running.**
2. **Rotate the full set** before any repeats. Seven interludes at up to six
   transitions a run means a player sees most of them once per run and rarely
   the same one twice.
3. **Never offer an interlude whose sin is currently active.** Thematic, and it
   avoids the player confusing an interlude reward with an encounter effect.
4. **Skip is always available**, for a small flat coin reward. This is not
   optional. A forced mini-game is a chore by session twenty, and the skip
   reward being deliberately *worse* than an average play is what keeps skipping
   from becoming the correct move.
5. **Interludes scale with table depth.** Narrower bands, longer patterns,
   faster shuffles, tighter release windows. The mini-game ramping is what
   stops it becoming a free reward by Table V.

## The Side Table

One optional extra placement: when Notice reaches 75%, offer a single interlude
mid-run. Winning **reduces Notice by three segments**.

This gives the player agency over the one system they currently just watch
happen to them, and it puts a mini-game where the tension is highest rather
than only at the calm points.

## Hard constraints

- **Ten seconds maximum.** Anything longer competes with the wheel.
- **One thumb, no precision dragging.** Tap, hold, release. Nothing else.
- **No tutorial text.** Each has a two-word instruction and one demonstration
  beat. If it needs explaining, redesign it.
- **Failure never ends the run.** Interludes are upside with a cost, never a
  death. Losing a run to a mini-game you did not choose to play is the fastest
  way to make players skip every one of them forever.

---

## Starting numbers

```
table_threshold_base   = 400      coins to trigger the first invite
table_threshold_growth = 1.45     compounding per table
table_reward_mult      = [1.0, 1.3, 1.7, 2.2, 2.8, 3.5, 5.0]

interlude_offer_count  = 2        of 7, no repeat, full rotation
interlude_skip_reward  = 40       flat coin, deliberately below average play
sidetable_notice_gate  = 0.75
sidetable_notice_relief= 3        segments

mark_debt_interval     = 1200     debt cleared per Mark
croupier_table         = 7        or 5 once Mark VII is taken
```

`table_threshold_growth` at 1.45 is a starting guess. It sets how many tables a
competent player reaches, which is the single most important number in this
document — too low and everyone sees Table VII in week one, too high and nobody
ever does. Simulate it before it goes near players.

---

## What I would not build

- **A difficulty menu.** Marks already do this, in-fiction and earned.
- **Mini-games that can kill you.** Covered above, but worth repeating — it is
  the most common version of this mistake.
- **More than seven interludes.** The rotation rule only works if the set is
  small enough to cycle. An eighth would dilute rather than add.
- **Tables that continue past VII.** An endless ladder removes the ending, and
  "I reached the Croupier" is the achievement the whole structure exists to
  produce.
- **Adaptive difficulty that reads player skill.** It feels like cheating the
  moment a player suspects it, and in a game about a house that may be rigging
  the wheel, that suspicion is guaranteed.

---

## Asset mapping

| Asset | System |
|---|---|
| `table_plaque_1..7.png` | Depth marker — colour escalates brass → wine → violet → red |
| `table_invite.png` | The deeper/cash-out fork |
| `depth_pip_{passed/current/locked}.png` | Descent track on the HUD edge |
| `mark_1..7_{earned/locked}.png` | Ascension seals |
| `mark_track.png` | Seven sockets on the ledger |
| `interlude_{name}.png` | Offer card, one per mini-game |
| `emblem_{name}.png` | Mini-game emblem, 24px |
| `timing_track.png`, `timing_needle.png` | The Ember |
| `memory_cell_{idle/lit/correct/wrong}.png` | The Mirror |
| `shell_cup_{down/lifted/marked}.png` | The Shell |
| `feast_meter.png` | The Feast — safe band, then widening danger |
| `toll_beat_{pending/hit/perfect/miss}.png` | The Toll |
| `vigil_ring_{0/50/100}.png` | The Vigil |
| `diff_frame{,_marked}.png` | The Understudy |
| `result_{success/partial/fail}.png` | Shared outcome banner |
| `button_skip.png` | Always present |

---

## Build order

1. **Tables I–III with the invite prompt.** This alone gives the run an arc and
   is the largest single improvement available. No mini-games needed.
2. **The Ember and The Toll.** Timing and rhythm are the two cheapest to build
   and cover the widest span of feel. Two is enough to prove the placement.
3. **Marks I–III.** Ascension arriving as a consequence of paying.
4. **Tables IV–VI**, with sin stacking at IV. The stacking is the one item here
   that touches existing architecture.
5. **The remaining five interludes**, in rotation order.
6. **Table VII and the Croupier's seat**, last — it is the payoff, and it should
   be built once everything beneath it is stable.

Steps one and two are perhaps two weekends and would transform how the game
feels on the tenth run, which is the run that decides whether there is an
eleventh.

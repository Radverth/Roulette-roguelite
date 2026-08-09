# Sin Wheel — Narrative Design

## The problem this has to solve

A run is two to four minutes. Anything that blocks a spin will be resented by
the third session and skipped by the tenth. So the rule underneath every
decision here is: **narrative never gates the wheel.** It arrives beside the
spin, not in front of it.

That constraint is not a limitation. It pushes the writing toward the only
register that actually works at this length — short, spoken, and reactive.

## The frame: you owe something

The wheel is not a game you chose to play. It is a debt you are servicing.

- **The Croupier** turns the wheel. He is not a villain and not a guide. He is
  the terms of the agreement, wearing a hat.
- **The seven** are the house's collectors. Crucially, they do not attack you
  because you are losing. They arrive **because you are doing well** — they
  come to take their cut.
- **Banking is paying down.** Busting is falling deeper.

This single reframe does most of the work. The core tension of the game already
is *push or bank*; the frame makes that choice mean something without adding a
mechanic. A player banking early is not playing safe, they are paying their
debt. A player pushing on is not optimising, they are gambling with money they
owe someone.

## Four layers, cheapest first

### 1. Arrival lines — the sin announces itself

One line, in the sin's own voice, on the announcement plate. Never a
description of the mechanic; the boss card already carries that. Greed does not
say "coins are taxed 30%", it says *"A third. That is all. A third of
everything."*

The distinction matters. The card informs. The plate characterises. Putting the
mechanic in both wastes the only line the sin gets.

### 2. Taunts — the encounter has a middle

Two or three lines per sin, fired on a spin during the encounter, drawn without
replacement so nothing repeats within one visit. These are pure texture and
cost nothing. They are also where each sin's personality actually lands, since
the arrival line is doing structural work.

### 3. The Croupier's bookends

He speaks at run start and run end, and nowhere else. That restraint is what
makes him land — he is the only constant in a game built on variety, so he
should be sparing.

His run-end lines fork on outcome and on *how* the run ended. Busting with a
large purse gets a different line to busting with nothing, because those are
different mistakes and he notices.

### 4. Fragments — the long game

Defeat a sin three times, unlock a fragment. Eight in total. They are delivered
on the ledger screen between runs, never mid-run.

Each fragment answers a little of *why you are at the wheel* — and never all of
it. The final one, gated behind all seven, reframes the Croupier: he turns the
wheel, he does not spin it. You have always spun it.

That is the whole story. Roughly sixty words, delivered over perhaps forty
runs. It is enough, because the player has been living the premise the entire
time.

## Reactive lines — the wheel is watching

The highest value per line of code in this entire document. The game observes
behaviour and comments on it:

| Trigger | Speaker | Effect |
|---|---|---|
| Banked immediately three runs running | Croupier | Player feels seen |
| Never banked this run | Greed | Approval from the wrong quarter |
| Same sin, third encounter | That sin | "You again." |
| Survived all seven in one run | Croupier | Acknowledges a real achievement |
| Bust while holding a large purse | Croupier | Names the specific mistake |

None of these need new systems. They read counters the analytics layer already
tracks. The payoff is disproportionate: a game that comments on your habits
feels authored, and a game that comments on your *bad* habits feels alive.

## Delivery rules

1. **Plates slide in over the wheel, never over the spin button.** The player
   can spin through an announcement.
2. **Auto-dismiss after roughly a second and a half.** No tap required. The
   caret sprite exists for the ledger screen, where reading is the activity.
3. **Taunts are a single line in the status strip**, not a plate. Plates are
   for arrivals only, or they stop being an event.
4. **Nothing is ever modal during a run.** Fragments wait for the ledger.
5. **Sound over animation.** A short stinger per sin does more than a long
   entrance and costs less frame time.

## What I would not do

- **No branching dialogue.** There is no room for it in a two-minute run and it
  would fight the roguelite structure.
- **No cutscenes, not even short ones.** The moment the player cannot spin,
  they are waiting.
- **No text on the wheel itself.** The wheel is the one place that must stay
  purely readable.
- **No narrator commentary on individual spins.** It would exhaust its welcome
  inside one session.

## Asset mapping

| Asset | Carries |
|---|---|
| `plate_*.png` | Arrival lines — mask, speaker, one line |
| `mask_*.png` | The seven, plus the Croupier. Faces, where sigils are geometry |
| `dialogue_box.png` | Ledger-screen conversation, 16px portrait well |
| `intertitle_start/bank/bust.png` | Run bookends |
| `fragment_card.png` | Lore plate — blank body, Unity fills at runtime |
| `ledger_row.png` | Debt screen, marked and unmarked |
| `ornament_divider.png` | Between speaker and line |
| `narrative_lines.json` | Every line above, keyed by sin and trigger |

## Why masks

The sigils are abstract — they say what a sin *does*. The masks are figurative;
they say who is *speaking*. Keeping those two registers separate means an
announcement reads as somebody arriving rather than a status effect being
applied.

All eight are cut from one base: same oval, same brow ridge, same jaw. The
family resemblance is deliberate. These came from one workshop, and one of them
is not a sin at all — the Croupier is bone where the seven have colour, because
he is not a temptation. He is the paperwork.

extends Node

var APP_VERSION: String:
	get: return ProjectSettings.get_setting("application/config/version", "1.0.0")
const GITHUB_OWNER := "radverth"
const GITHUB_REPO := "roulette-roguelite"
const GITHUB_API_LATEST := "https://api.github.com/repos/radverth/roulette-roguelite/releases/latest"

const STARTING_CHIPS  := 640
const STARTING_TARGET := 1000
const HANDS_PER_ANTE  := 4
const ANTE_SCALE      := 1.6
const GAME_OVER_CHIPS := 5
const MAX_OWNED_CARDS := 5
const WIN_ANTE        := 8    # clear this ante to beat the House

# Ante-clear bounty: flat payout plus interest on banked chips
const BOUNTY_FLAT         := 50
const BOUNTY_INTEREST_DIV := 10   # +1 chip per 10 banked
const BOUNTY_INTEREST_CAP := 150

const REROLL_BASE_COST := 10  # shop reroll price, escalates per use

const SHOP_RARITY_WEIGHTS := {
	"common":    50,
	"uncommon":  30,
	"rare":      15,
	"legendary": 5,
}

const CHIP_DENOMINATIONS: Array[int] = [5, 25, 100]
const DEFAULT_CHIP := 25
const MAX_BET := 5000

const WHEEL_SEQUENCE: Array[int] = [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26]
const RED_NUMBERS: Array[int]    = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36]
const PRIMES: Array[int]         = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31]

const PAYOUT_STRAIGHT    := 35
const PAYOUT_COLUMN      := 2
const PAYOUT_DOZEN       := 2
const PAYOUT_EVEN_CHANCE := 1

const CARD_PRICES := {
	"common":    40,
	"uncommon":  60,
	"rare":      90,
	"legendary": 140,
}

const CARD_RARITY_COLORS := {
	"common":    Color(0.60, 0.60, 0.60),
	"uncommon":  Color(0.18, 0.61, 0.53),
	"rare":      Color(0.75, 0.22, 0.17),
	"legendary": Color(0.79, 0.66, 0.30),
}

# The Descent — the run is Dante's Inferno played on a roulette wheel.
# Each ante is a circle of Hell, and the Devil turns one of the Seven Deadly
# Sins against you: a curse that corrupts the wheel until the circle is
# cleared. Ordered by the descent — Lust is the shallowest circle, Pride the
# deepest of the sins, and the eighth circle is Cocytus, the Devil's own.
# In endless mode the circles repeat, forever.
#   curse  — what the sin does to the player, in rules terms
#   herald — the Devil's announcement as you descend into the circle
#   lines  — the Devil's taunts while the sin does its work
const SIN_MODIFIERS: Array[Dictionary] = [
	{
		"id": "lust", "name": "LUST",
		"curse": "The reds seduce and betray — winning red bets pay only half.",
		"herald": "The first circle, mortal. Desire is the softest chain — see how the red glitters?",
		"lines": [
			"You cannot resist the red, can you? Neither could they.",
			"Desire pays half and takes double. It always has.",
			"The storm of the lustful howls just for you.",
		],
	},
	{
		"id": "gluttony", "name": "GLUTTONY",
		"curse": "Insatiable — the wheel refuses any stake under 50 chips.",
		"herald": "Deeper now. Here appetite is force-fed — the wheel demands a proper offering.",
		"lines": [
			"More. You always crave more.",
			"Feed the wheel, glutton. It is never full.",
			"Cerberus ate lighter meals than your stakes.",
		],
	},
	{
		"id": "greed", "name": "GREED",
		"curse": "The House hoards — a fifth of every winning spin is taken.",
		"herald": "Hoarder or spendthrift, it matters not. All gold rolls downhill to me.",
		"lines": [
			"Every fifth coin was always mine. Now I collect.",
			"Count your winnings. I already have.",
			"You clutch, I harvest.",
		],
	},
	{
		"id": "sloth", "name": "SLOTH",
		"curse": "Torpor sets in — one fewer hand to clear this circle.",
		"herald": "The sullen drown slowly here. Rest, mortal — sleep through your one chance.",
		"lines": [
			"Time slips. You let it.",
			"The idle soul drowns in the mire, gurgling hymns.",
			"One hand fewer. You would only have wasted it.",
		],
	},
	{
		"id": "wrath", "name": "WRATH",
		"curse": "Fury feeds the flames — every total loss burns an extra quarter of your stake.",
		"herald": "Feel it rising? Good. Your fury is my kindling — every loss will burn hotter.",
		"lines": [
			"Strike the table. It only feeds me.",
			"Rage splendidly, mortal. The wheel drinks it.",
			"Your losses burn brighter here. I do love a bonfire.",
		],
	},
	{
		"id": "envy", "name": "ENVY",
		"curse": "The House covets your boldest wager — your largest bet pays nothing.",
		"herald": "You covet, mortal. So do I — and what I covet is whatever you prize most.",
		"lines": [
			"That precious wager of yours? Mine now.",
			"Envy eats its own eyes first.",
			"Spread your coins thin — I still take the fattest stack.",
		],
	},
	{
		"id": "pride", "name": "PRIDE",
		"curse": "Pride goeth before the fall — each consecutive win pays 15% less (max −60%).",
		"herald": "So high, so sure. The proudest fall furthest — and you are almost at the bottom.",
		"lines": [
			"Pride was my sin first, little gambler.",
			"Each triumph hollows the next. Climb on.",
			"How the mighty stack their chips before the fall.",
		],
	},
	{
		"id": "treachery", "name": "TREACHERY",
		"curse": "The Devil's pocket yawns wide — zero strikes twice as often.",
		"herald": "Cocytus. The lake of ice. Here even the wheel betrays you — every pocket is mine.",
		"lines": [
			"Zero again? How strange.",
			"I do not cheat, mortal. I merely never lose.",
			"The ice cracks beneath your bets. Listen.",
			"This is my circle. The wheel remembers its maker.",
		],
	},
]

const BOSS_MODIFIERS: Array[Dictionary] = [
	{"id": "red_pays_nothing", "name": "THE CROUPIER",  "desc": "Red numbers pay nothing for the whole circle."},
	{"id": "house_skim",       "name": "THE COLLECTOR", "desc": "The House skims 10% of your chips after every spin."},
	{"id": "odds_swap",        "name": "THE MIRROR",    "desc": "Odd and Even swap their payouts until the circle is cleared."},
]

const COLOR_BG      := Color(0.051, 0.051, 0.051)
const COLOR_GOLD    := Color(0.788, 0.659, 0.298)
const COLOR_CRIMSON := Color(0.545, 0.016, 0.016)
const COLOR_TEXT    := Color(0.941, 0.902, 0.827)

func rom(n: int) -> String:
	var parts := ["","I","II","III","IV","V","VI","VII","VIII","IX","X"]
	if n >= 0 and n < parts.size():
		return parts[n]
	return str(n)

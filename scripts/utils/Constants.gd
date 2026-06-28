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
	"common":    4,
	"uncommon":  6,
	"rare":      9,
	"legendary": 14,
}

const CARD_RARITY_COLORS := {
	"common":    Color(0.60, 0.60, 0.60),
	"uncommon":  Color(0.18, 0.61, 0.53),
	"rare":      Color(0.75, 0.22, 0.17),
	"legendary": Color(0.79, 0.66, 0.30),
}

const BOSS_MODIFIERS: Array[Dictionary] = [
	{"id": "red_pays_nothing", "name": "THE CROUPIER",  "desc": "Red numbers pay nothing for the whole ante."},
	{"id": "house_skim",       "name": "THE COLLECTOR", "desc": "The House skims 10% of your chips after every spin."},
	{"id": "odds_swap",        "name": "THE MIRROR",    "desc": "Odd and Even swap their payouts until the ante is cleared."},
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

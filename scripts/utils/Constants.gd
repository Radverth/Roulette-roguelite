extends Node

const APP_VERSION := "1.0.0"
const GITHUB_OWNER := "radverth"
const GITHUB_REPO := "roulette-roguelite"
const GITHUB_API_LATEST := "https://api.github.com/repos/radverth/roulette-roguelite/releases/latest"

const STARTING_CHIPS := 500
const BASE_ANTE_TARGET := 1000
const ANTE_SCALE := 1.5
const FLOORS_BEFORE_BOSS := 3
const MAX_OWNED_CARDS := 5

const MIN_BET := 10
const DEFAULT_BET := 50
const MAX_BET := 500
const BET_STEP := 10

const WHEEL_SEQUENCE: Array[int] = [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26]
const RED_NUMBERS: Array[int] = [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36]
const PRIMES: Array[int] = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31]

const PAYOUT_STRAIGHT := 35
const PAYOUT_COLUMN := 2
const PAYOUT_DOZEN := 2
const PAYOUT_EVEN_CHANCE := 1

const CARD_PRICES := {
	"common": 50,
	"uncommon": 100,
	"rare": 200,
	"legendary": 400,
}

const CARD_RARITY_COLORS := {
	"common": Color(0.72, 0.72, 0.72),
	"uncommon": Color(0.3, 0.85, 0.3),
	"rare": Color(0.35, 0.55, 1.0),
	"legendary": Color(1.0, 0.72, 0.12),
}

const BOSS_MODIFIERS: Array[Dictionary] = [
	{"id": "half_payout", "name": "Devil's Cut", "desc": "All winnings halved this floor."},
	{"id": "forced_bet", "name": "House Rules", "desc": "Minimum bet is doubled this floor."},
	{"id": "no_outside", "name": "Inside Only", "desc": "Outside bets pay nothing this floor."},
	{"id": "jackpot_zero", "name": "Zero Jackpot", "desc": "Landing on zero triples all bonuses."},
	{"id": "double_zero", "name": "Cursed Wheel", "desc": "The zero pocket has twice the pull."},
]

const COLOR_BG := Color(0.06, 0.01, 0.01)
const COLOR_GOLD := Color(0.85, 0.68, 0.15)
const COLOR_CRIMSON := Color(0.55, 0.04, 0.06)
const COLOR_TEXT := Color(0.95, 0.90, 0.80)
const COLOR_DARK_TEXT := Color(0.15, 0.08, 0.05)

extends Node

const CARDS: Array[Dictionary] = [
	{
		"id": "black_rider", "name": "Black Rider", "rarity": "uncommon",
		"desc": "Black bets pay 50% more.",
		"icon": "res://assets/cards/icon_black_rider.png",
	},
	{
		"id": "red_rider", "name": "Red Rider", "rarity": "uncommon",
		"desc": "Red bets pay 50% more.",
		"icon": "res://assets/cards/icon_red_rider.png",
	},
	{
		"id": "croupiers_tip", "name": "Croupier's Tip", "rarity": "common",
		"desc": "Gain 25 chips after every spin.",
		"icon": "res://assets/cards/icon_croupiers_tip.png",
	},
	{
		"id": "even_keel", "name": "Even Keel", "rarity": "rare",
		"desc": "Odd/Even bets pay double.",
		"icon": "res://assets/cards/icon_even_keel.png",
	},
	{
		"id": "ghost_ball", "name": "Ghost Ball", "rarity": "rare",
		"desc": "30% chance to refund your bet on any loss.",
		"icon": "res://assets/cards/icon_ghost_ball.png",
	},
	{
		"id": "house_edge_reversal", "name": "House Edge Reversal", "rarity": "legendary",
		"desc": "Zero refunds all bets plus 100 chip bonus.",
		"icon": "res://assets/cards/icon_house_edge_reversal.png",
	},
	{
		"id": "magnetic_sector", "name": "Magnetic Sector", "rarity": "uncommon",
		"desc": "Straight-up wins also pay 5x on adjacent pocket bets.",
		"icon": "res://assets/cards/icon_magnetic_sector.png",
	},
	{
		"id": "perpetual_motion", "name": "Perpetual Motion", "rarity": "rare",
		"desc": "Every 5 spins, gain 50 chips.",
		"icon": "res://assets/cards/icon_perpetual_motion.png",
	},
	{
		"id": "pocket_blocker", "name": "Pocket Blocker", "rarity": "common",
		"desc": "Blocks pocket 0 — the ball bounces to an adjacent number.",
		"icon": "res://assets/cards/icon_pocket_blocker.png",
	},
	{
		"id": "pocket_lint", "name": "Pocket Lint", "rarity": "common",
		"desc": "Junk. Sell from the Shop for 10 chips.",
		"icon": "res://assets/cards/icon_pocket_lint.png",
	},
	{
		"id": "prime_protocol", "name": "Prime Protocol", "rarity": "uncommon",
		"desc": "Wins on prime numbers pay 25% extra.",
		"icon": "res://assets/cards/icon_prime_protocol.png",
	},
	{
		"id": "streak_counter", "name": "Streak Counter", "rarity": "rare",
		"desc": "Each consecutive win adds 10% payout (max +50%).",
		"icon": "res://assets/cards/icon_streak_counter.png",
	},
	{
		"id": "the_dozen", "name": "The Dozen", "rarity": "common",
		"desc": "Dozen bets pay 50% more.",
		"icon": "res://assets/cards/icon_the_dozen.png",
	},
	{
		"id": "triple_ball", "name": "Triple Ball", "rarity": "legendary",
		"desc": "Three balls spin each round. Win if any lands on your bet.",
		"icon": "res://assets/cards/icon_triple_ball.png",
	},
	{
		"id": "velvet_hand", "name": "Velvet Hand", "rarity": "legendary",
		"desc": "Keep all your cards when advancing to the next floor.",
		"icon": "res://assets/cards/icon_velvet_hand.png",
	},
	{
		"id": "zero_bounty", "name": "Zero Bounty", "rarity": "rare",
		"desc": "Ball on zero refunds all bets and pays 100 chip bonus.",
		"icon": "res://assets/cards/icon_zero_bounty.png",
	},
]

func get_card(card_id: String) -> Dictionary:
	for card in CARDS:
		if card.id == card_id:
			return card.duplicate()
	return {}

func get_shop_offer(count: int = 3) -> Array[Dictionary]:
	var pool: Array[Dictionary] = []
	for card in CARDS:
		if not GameManager.has_card(card.id):
			pool.append(card.duplicate())
	pool.shuffle()
	var offer: Array[Dictionary] = []
	for i in range(min(count, pool.size())):
		offer.append(pool[i])
	return offer

func get_card_price(card: Dictionary) -> int:
	return Constants.CARD_PRICES.get(card.get("rarity", "common"), 50)

func calculate_winnings(bets: Dictionary, winning_number: int) -> int:
	var numbers_to_check: Array[int] = [winning_number]
	if GameManager.has_card("triple_ball") and not GameManager.triple_ball_numbers.is_empty():
		numbers_to_check = GameManager.triple_ball_numbers

	var best_return := 0
	for n in numbers_to_check:
		var r := _base_return(bets, n)
		best_return = max(best_return, r)

	best_return = _apply_card_bonuses(bets, winning_number, best_return)
	best_return = _apply_boss_modifier(winning_number, best_return, _total_bet(bets))
	return best_return

func _total_bet(bets: Dictionary) -> int:
	var total := 0
	for key in bets:
		total += int(bets[key])
	return total

func _base_return(bets: Dictionary, number: int) -> int:
	var total := 0
	for bet_key in bets:
		var amount: int = bets[bet_key]
		var ratio := _payout_ratio(bet_key, number)
		if ratio > 0:
			total += amount * (ratio + 1)
	return total

func _payout_ratio(bet_key: String, number: int) -> int:
	if GameManager.is_boss_floor and GameManager.current_boss_modifier.get("id") == "no_outside":
		if not bet_key.begins_with("straight_"):
			return 0

	if bet_key.begins_with("straight_"):
		var n := int(bet_key.trim_prefix("straight_"))
		return Constants.PAYOUT_STRAIGHT if n == number else 0

	match bet_key:
		"red":
			return Constants.PAYOUT_EVEN_CHANCE if number in Constants.RED_NUMBERS else 0
		"black":
			return Constants.PAYOUT_EVEN_CHANCE if (number != 0 and not (number in Constants.RED_NUMBERS)) else 0
		"odd":
			return Constants.PAYOUT_EVEN_CHANCE if (number != 0 and number % 2 == 1) else 0
		"even":
			return Constants.PAYOUT_EVEN_CHANCE if (number != 0 and number % 2 == 0) else 0
		"low":
			return Constants.PAYOUT_EVEN_CHANCE if (number >= 1 and number <= 18) else 0
		"high":
			return Constants.PAYOUT_EVEN_CHANCE if (number >= 19 and number <= 36) else 0
		"dozen1":
			return Constants.PAYOUT_DOZEN if (number >= 1 and number <= 12) else 0
		"dozen2":
			return Constants.PAYOUT_DOZEN if (number >= 13 and number <= 24) else 0
		"dozen3":
			return Constants.PAYOUT_DOZEN if (number >= 25 and number <= 36) else 0
		"col1":
			return Constants.PAYOUT_COLUMN if (number != 0 and number % 3 == 1) else 0
		"col2":
			return Constants.PAYOUT_COLUMN if (number != 0 and number % 3 == 2) else 0
		"col3":
			return Constants.PAYOUT_COLUMN if (number != 0 and number % 3 == 0) else 0
	return 0

func _apply_card_bonuses(bets: Dictionary, number: int, base: int) -> int:
	var total_bet := _total_bet(bets)
	var result := base

	# Zero special handling
	if number == 0:
		if GameManager.has_card("house_edge_reversal") or GameManager.has_card("zero_bounty"):
			return total_bet + 100
		return result

	# Loss handling
	if result == 0:
		if GameManager.has_card("ghost_ball") and randf() < 0.3:
			return total_bet
		return 0

	# Win bonuses
	if GameManager.has_card("red_rider") and "red" in bets and _payout_ratio("red", number) > 0:
		result += int(bets["red"] * 0.5)

	if GameManager.has_card("black_rider") and "black" in bets and _payout_ratio("black", number) > 0:
		result += int(bets["black"] * 0.5)

	if GameManager.has_card("even_keel"):
		for key in ["odd", "even"]:
			if key in bets and _payout_ratio(key, number) > 0:
				result += int(bets[key])

	if GameManager.has_card("the_dozen"):
		for key in ["dozen1", "dozen2", "dozen3"]:
			if key in bets and _payout_ratio(key, number) > 0:
				result += int(bets[key] * 0.5)

	if GameManager.has_card("prime_protocol") and number in Constants.PRIMES:
		result = int(result * 1.25)

	if GameManager.has_card("streak_counter"):
		result = int(result * GameManager.get_streak_multiplier())

	if GameManager.has_card("magnetic_sector"):
		var adj := _adjacent_numbers(number)
		for bet_key in bets:
			if bet_key.begins_with("straight_"):
				var n := int(bet_key.trim_prefix("straight_"))
				if n in adj:
					result += int(bets[bet_key]) * 5

	return result

func _apply_boss_modifier(number: int, result: int, total_bet: int) -> int:
	if not GameManager.is_boss_floor:
		return result
	match GameManager.current_boss_modifier.get("id", ""):
		"half_payout":
			return result / 2
		"jackpot_zero":
			if number == 0:
				return total_bet * 3 + 300
		"double_zero":
			pass
	return result

func _adjacent_numbers(number: int) -> Array[int]:
	var seq := Constants.WHEEL_SEQUENCE
	var idx := seq.find(number)
	if idx < 0:
		return []
	var size := seq.size()
	return [seq[(idx - 1 + size) % size], seq[(idx + 1) % size]]

extends Node

signal chips_changed(new_amount: int)
signal ante_progress_changed(current: int, target: int)
signal floor_changed(new_floor: int)
signal cards_changed()

var chips: int = Constants.STARTING_CHIPS
var floor_number: int = 1
var ante_target: int = Constants.BASE_ANTE_TARGET
var ante_progress: int = 0
var spin_count: int = 0
var win_streak: int = 0
var owned_cards: Array[Dictionary] = []
var current_boss_modifier: Dictionary = {}
var is_boss_floor: bool = false
var pocket_blocked: int = -1
var perpetual_counter: int = 0
var triple_ball_numbers: Array[int] = []
var game_active: bool = false

func start_new_game() -> void:
	chips = Constants.STARTING_CHIPS
	floor_number = 1
	ante_target = Constants.BASE_ANTE_TARGET
	ante_progress = 0
	spin_count = 0
	win_streak = 0
	owned_cards.clear()
	current_boss_modifier = {}
	is_boss_floor = false
	pocket_blocked = -1
	perpetual_counter = 0
	triple_ball_numbers.clear()
	game_active = true
	emit_signal("chips_changed", chips)
	emit_signal("floor_changed", floor_number)
	emit_signal("ante_progress_changed", ante_progress, ante_target)
	emit_signal("cards_changed")

func add_chips(amount: int) -> void:
	chips += amount
	if amount > 0:
		ante_progress += amount
		emit_signal("ante_progress_changed", ante_progress, ante_target)
	emit_signal("chips_changed", chips)

func spend_chips(amount: int) -> bool:
	if chips < amount:
		return false
	chips -= amount
	emit_signal("chips_changed", chips)
	return true

func has_card(card_id: String) -> bool:
	for card in owned_cards:
		if card.id == card_id:
			return true
	return false

func add_card(card: Dictionary) -> bool:
	if owned_cards.size() >= Constants.MAX_OWNED_CARDS:
		return false
	owned_cards.append(card.duplicate())
	emit_signal("cards_changed")
	return true

func remove_card(card_id: String) -> void:
	for i in range(owned_cards.size()):
		if owned_cards[i].id == card_id:
			owned_cards.remove_at(i)
			emit_signal("cards_changed")
			return

func advance_floor() -> void:
	floor_number += 1
	ante_progress = 0
	ante_target = int(Constants.BASE_ANTE_TARGET * pow(Constants.ANTE_SCALE, floor_number - 1))
	is_boss_floor = (floor_number % Constants.FLOORS_BEFORE_BOSS == 0)
	if is_boss_floor:
		var mods := Constants.BOSS_MODIFIERS
		current_boss_modifier = mods[randi() % mods.size()]
	else:
		current_boss_modifier = {}
	emit_signal("floor_changed", floor_number)
	emit_signal("ante_progress_changed", ante_progress, ante_target)

func check_ante_complete() -> bool:
	return ante_progress >= ante_target

func check_bankrupt() -> bool:
	return chips <= 0

func on_spin_complete(won: bool) -> void:
	spin_count += 1
	if won:
		win_streak += 1
	else:
		win_streak = 0
	perpetual_counter += 1
	if has_card("perpetual_motion") and perpetual_counter >= 5:
		add_chips(50)
		perpetual_counter = 0
	if has_card("croupiers_tip"):
		add_chips(25)

func get_streak_multiplier() -> float:
	if has_card("streak_counter"):
		return 1.0 + min(win_streak * 0.1, 0.5)
	return 1.0

func get_effective_min_bet() -> int:
	if is_boss_floor and current_boss_modifier.get("id") == "forced_bet":
		return Constants.MIN_BET * 2
	return Constants.MIN_BET

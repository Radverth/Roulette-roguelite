extends Node

signal chips_changed(new_amount: int)
signal hand_changed(hand: int, max_hand: int)
signal ante_changed(ante: int, chips: int, target: int)
signal ante_up(new_ante: int)
signal cards_changed()

var chips:     int = Constants.STARTING_CHIPS
var target:    int = Constants.STARTING_TARGET
var ante:      int = 1
var hand:      int = 1
var max_hand:  int = Constants.HANDS_PER_ANTE
var spin_count: int = 0
var win_streak: int = 0
var owned_cards: Array[Dictionary] = []
var shop_variant: int = 0
var game_active: bool = false

# Legacy fields kept for card system compatibility
var floor_number:          int = 1
var is_boss_floor:         bool = false
var current_boss_modifier: Dictionary = {}
var pocket_blocked:        int = -1
var perpetual_counter:     int = 0
var triple_ball_numbers:   Array[int] = []
var ante_progress:         int = 0
var ante_target:           int = Constants.STARTING_TARGET

func start_new_game() -> void:
	chips        = Constants.STARTING_CHIPS
	target       = Constants.STARTING_TARGET
	ante         = 1
	hand         = 1
	max_hand     = Constants.HANDS_PER_ANTE
	spin_count   = 0
	win_streak   = 0
	shop_variant = 0
	owned_cards.clear()
	is_boss_floor = false
	current_boss_modifier = {}
	pocket_blocked = -1
	perpetual_counter = 0
	triple_ball_numbers.clear()
	ante_progress = 0
	ante_target   = Constants.STARTING_TARGET
	floor_number  = 1
	game_active   = true
	emit_signal("chips_changed", chips)
	emit_signal("hand_changed", hand, max_hand)
	emit_signal("ante_changed", ante, chips, target)
	emit_signal("cards_changed")

func add_chips(amount: int) -> void:
	chips += amount
	emit_signal("chips_changed", chips)

func spend_chips(amount: int) -> bool:
	if chips < amount:
		return false
	chips -= amount
	emit_signal("chips_changed", chips)
	return true

func check_game_over() -> bool:
	return chips < Constants.GAME_OVER_CHIPS

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
	if has_card("pocket_lint"):
		add_chips(1)

	# Advance hand; when all hands done, ante up
	hand += 1
	if hand > max_hand:
		hand = 1
		ante += 1
		target = int(round(float(target) * Constants.ANTE_SCALE))
		floor_number  = ante
		ante_progress = 0
		ante_target   = target
		emit_signal("ante_up", ante)
	emit_signal("hand_changed", hand, max_hand)
	emit_signal("ante_changed", ante, chips, target)

func get_streak_multiplier() -> float:
	if has_card("streak_counter"):
		return 1.0 + min(win_streak * 0.05, 0.30)
	return 1.0

func get_effective_min_bet() -> int:
	return 1

# Legacy helpers kept for CardManager compatibility
func check_ante_complete() -> bool:
	return false

func check_bankrupt() -> bool:
	return check_game_over()

func advance_floor() -> void:
	pass

extends Control

var _wheel: RouletteWheel
var _table: BettingTable
var _hud: HUD
var _popup: WinPopup
var _spin_btn: Button
var _clear_btn: Button
var _bet_minus: Button
var _bet_plus: Button
var _bet_amount_lbl: Label
var _cards_row: HBoxContainer
var _block_number_dialog: AcceptDialog

var _is_spinning := false
var _pending_number := -1
var _pending_net := 0

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	_connect_signals()
	if GameManager.is_boss_floor:
		_hud.show_boss_modifier()
	_refresh_cards_row()
	GameManager.cards_changed.connect(_refresh_cards_row)

func _build_ui() -> void:
	var bg := ColorRect.new()
	bg.color = Constants.COLOR_BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 0)
	add_child(root)

	_hud = HUD.new()
	root.add_child(_hud)

	_wheel = RouletteWheel.new()
	root.add_child(_wheel)

	_cards_row = HBoxContainer.new()
	_cards_row.custom_minimum_size = Vector2(1080, 120)
	_cards_row.alignment = BoxContainer.ALIGNMENT_CENTER
	_cards_row.add_theme_constant_override("separation", 12)
	root.add_child(_cards_row)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(1080, 44)
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	_table = BettingTable.new()
	root.add_child(_table)

	var controls := _build_controls()
	root.add_child(controls)

	var hud_frame := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/hud_frame.png"):
		hud_frame.texture = load("res://assets/ui/hud_frame.png")
	hud_frame.stretch_mode = TextureRect.STRETCH_SCALE
	hud_frame.set_anchors_preset(Control.PRESET_FULL_RECT)
	hud_frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(hud_frame)

	_popup = WinPopup.new()
	_popup.hide()
	add_child(_popup)

func _build_controls() -> Control:
	var container := ColorRect.new()
	container.color = Color(0.04, 0.0, 0.0, 1.0)
	container.custom_minimum_size = Vector2(1080, 140)

	var hbox := HBoxContainer.new()
	hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 16)
	hbox.offset_left = 16.0
	hbox.offset_right = -16.0
	container.add_child(hbox)

	_bet_minus = _make_small_btn("-")
	hbox.add_child(_bet_minus)

	var bet_label_wrap := VBoxContainer.new()
	bet_label_wrap.alignment = BoxContainer.ALIGNMENT_CENTER
	bet_label_wrap.custom_minimum_size = Vector2(160, 0)
	hbox.add_child(bet_label_wrap)

	var bet_caption := Label.new()
	bet_caption.text = "BET"
	bet_caption.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	bet_caption.add_theme_color_override("font_color", Color(0.7, 0.7, 0.6))
	bet_caption.add_theme_font_size_override("font_size", 20)
	bet_label_wrap.add_child(bet_caption)

	_bet_amount_lbl = Label.new()
	_bet_amount_lbl.text = str(Constants.DEFAULT_BET)
	_bet_amount_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_bet_amount_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_bet_amount_lbl.add_theme_font_size_override("font_size", 36)
	bet_label_wrap.add_child(_bet_amount_lbl)

	_bet_plus = _make_small_btn("+")
	hbox.add_child(_bet_plus)

	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(spacer)

	_spin_btn = _make_big_btn("SPIN", Constants.COLOR_CRIMSON)
	hbox.add_child(_spin_btn)

	_clear_btn = _make_big_btn("CLEAR", Color(0.2, 0.2, 0.2))
	hbox.add_child(_clear_btn)

	return container

func _make_small_btn(text: String) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(72, 72)
	btn.add_theme_font_size_override("font_size", 36)
	_style_btn(btn, Color(0.25, 0.1, 0.1))
	return btn

func _make_big_btn(text: String, color: Color) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(200, 90)
	btn.add_theme_font_size_override("font_size", 30)
	_style_btn(btn, color)
	return btn

func _style_btn(btn: Button, color: Color) -> void:
	for state in ["normal", "hover", "pressed", "disabled"]:
		var s := StyleBoxFlat.new()
		s.bg_color = color if state != "disabled" else Color(0.15, 0.15, 0.15)
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(8)
		if state == "hover":
			s.bg_color = color.lightened(0.2)
		if state == "pressed":
			s.bg_color = color.darkened(0.2)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

func _connect_signals() -> void:
	_spin_btn.pressed.connect(_on_spin_pressed)
	_clear_btn.pressed.connect(_on_clear_pressed)
	_bet_minus.pressed.connect(_on_bet_minus)
	_bet_plus.pressed.connect(_on_bet_plus)
	_wheel.ball_landed.connect(_on_ball_landed)
	_popup.dismissed.connect(_on_popup_dismissed)

func _on_spin_pressed() -> void:
	if _is_spinning:
		return
	var total := _table.get_total_bet()
	if total == 0:
		_show_toast("Place a bet first!")
		return
	if total > GameManager.chips:
		_show_toast("Not enough chips!")
		return

	GameManager.spend_chips(total)
	_is_spinning = true
	_spin_btn.disabled = true
	_clear_btn.disabled = true

	var number := _roll_number()
	if GameManager.has_card("triple_ball"):
		GameManager.triple_ball_numbers = [number, _roll_raw(), _roll_raw()]
	else:
		GameManager.triple_ball_numbers = []

	_wheel.spin(number)
	AudioManager.play_spin()

func _roll_raw() -> int:
	return randi() % 37

func _roll_number() -> int:
	var n := randi() % 37
	if GameManager.has_card("pocket_blocker") and n == 0:
		n = Constants.WHEEL_SEQUENCE[1]
	if GameManager.is_boss_floor and GameManager.current_boss_modifier.get("id") == "double_zero":
		if randf() < 0.054:
			n = 0
	return n

func _on_ball_landed(number: int) -> void:
	var bets := _table.get_bets()
	var total_bet := _table.get_total_bet()
	var returned := CardManager.calculate_winnings(bets, number)
	var net := returned - total_bet

	GameManager.add_chips(returned)
	GameManager.on_spin_complete(returned > total_bet)
	_hud.update_spin_count(GameManager.spin_count)

	if returned > total_bet:
		_wheel.show_win_burst()
		AudioManager.play_win()
	else:
		AudioManager.play_loss()

	var notes := _build_card_notes(number, returned, total_bet)
	_popup.show_result(number, net, notes)
	_pending_number = number
	_pending_net = net
	_table.clear_bets()

func _build_card_notes(number: int, returned: int, total_bet: int) -> String:
	var parts: Array[String] = []
	if GameManager.has_card("croupiers_tip"):
		parts.append("Croupier's Tip: +25")
	if GameManager.has_card("perpetual_motion") and GameManager.perpetual_counter == 0:
		parts.append("Perpetual Motion: +50")
	if returned == total_bet and returned > 0 and GameManager.has_card("ghost_ball"):
		parts.append("Ghost Ball saved your bet!")
	if number == 0 and (GameManager.has_card("house_edge_reversal") or GameManager.has_card("zero_bounty")):
		parts.append("Zero refund + bonus!")
	return "\n".join(parts)

func _on_popup_dismissed() -> void:
	_is_spinning = false
	_spin_btn.disabled = false
	_clear_btn.disabled = false
	_wheel.reset_for_next_spin()

	if GameManager.check_bankrupt():
		_go_to_game_over()
		return
	if GameManager.check_ante_complete():
		_go_to_shop()

func _go_to_shop() -> void:
	get_tree().change_scene_to_file("res://scenes/Shop.tscn")

func _go_to_game_over() -> void:
	SaveManager.save_run(GameManager.chips, GameManager.floor_number)
	get_tree().change_scene_to_file("res://scenes/GameOver.tscn")

func _on_clear_pressed() -> void:
	_table.clear_bets()

func _on_bet_minus() -> void:
	var current := _table.get_chip_amount()
	var new_amt := max(current - Constants.BET_STEP, GameManager.get_effective_min_bet())
	_table.set_chip_amount(new_amt)
	_bet_amount_lbl.text = str(new_amt)

func _on_bet_plus() -> void:
	var current := _table.get_chip_amount()
	var new_amt := min(current + Constants.BET_STEP, Constants.MAX_BET)
	_table.set_chip_amount(new_amt)
	_bet_amount_lbl.text = str(new_amt)

func _refresh_cards_row() -> void:
	for child in _cards_row.get_children():
		child.queue_free()
	for card in GameManager.owned_cards:
		var icon := ModIcon.new(card)
		_cards_row.add_child(icon)

func _show_toast(msg: String) -> void:
	var toast := Label.new()
	toast.text = msg
	toast.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	toast.add_theme_font_size_override("font_size", 28)
	toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	toast.position = Vector2(540 - 200, 900)
	toast.size = Vector2(400, 60)
	add_child(toast)
	var tween := create_tween()
	tween.tween_property(toast, "modulate:a", 0.0, 2.0).set_delay(0.8)
	tween.tween_callback(toast.queue_free)

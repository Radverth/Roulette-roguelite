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
var _devil: DevilDialogue

var _is_spinning := false
var _pending_number := -1
var _pending_net := 0

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	_devil = DevilDialogue.new()
	add_child(_devil)
	_connect_signals()
	if GameManager.is_boss_floor:
		_hud.show_boss_modifier()
		_show_boss_reveal()
	else:
		_devil.say("game_start", 3.5)

# ── Layout (sums to exactly 1920px) ────────────────────────────────────────
# HUD          150 px
# RouletteWheel 600 px
# Flame divider  30 px
# BettingTable  940 px
# Controls      200 px
# Total:       1920 px
func _build_ui() -> void:
	var bg := ColorRect.new()
	bg.color = Constants.COLOR_BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 0)
	add_child(root)

	_hud = HUD.new()
	root.add_child(_hud)

	_wheel = RouletteWheel.new()
	root.add_child(_wheel)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(1080, 30)
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	_table = BettingTable.new()
	root.add_child(_table)

	root.add_child(_build_controls())

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

# ── Controls strip (200px, absolute-positioned children) ───────────────────
func _build_controls() -> Control:
	var ctr := Control.new()
	ctr.custom_minimum_size = Vector2(1080, 200)

	var bg := ColorRect.new()
	bg.color = Color(0.04, 0.0, 0.0, 1.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ctr.add_child(bg)

	# Separator line at top
	var sep := ColorRect.new()
	sep.color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.4)
	sep.position = Vector2(0, 0)
	sep.size = Vector2(1080, 2)
	sep.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ctr.add_child(sep)

	# BET caption
	var bet_cap := Label.new()
	bet_cap.text = "BET"
	bet_cap.position = Vector2(14, 20)
	bet_cap.size = Vector2(100, 30)
	bet_cap.add_theme_color_override("font_color", Color(0.65, 0.62, 0.52))
	bet_cap.add_theme_font_size_override("font_size", 20)
	bet_cap.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ctr.add_child(bet_cap)

	_bet_amount_lbl = Label.new()
	_bet_amount_lbl.text = str(Constants.DEFAULT_BET)
	_bet_amount_lbl.position = Vector2(14, 50)
	_bet_amount_lbl.size = Vector2(100, 50)
	_bet_amount_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_bet_amount_lbl.add_theme_font_size_override("font_size", 40)
	_bet_amount_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ctr.add_child(_bet_amount_lbl)

	_bet_minus = _small_btn("-")
	_bet_minus.position = Vector2(124, 42)
	_bet_minus.size = Vector2(80, 80)
	ctr.add_child(_bet_minus)

	_bet_plus = _small_btn("+")
	_bet_plus.position = Vector2(214, 42)
	_bet_plus.size = Vector2(80, 80)
	ctr.add_child(_bet_plus)

	# SPIN button — textured
	_spin_btn = _action_btn("SPIN", true)
	_spin_btn.position = Vector2(320, 28)
	_spin_btn.size = Vector2(410, 144)
	ctr.add_child(_spin_btn)

	# CLEAR button — textured
	_clear_btn = _action_btn("CLEAR", false)
	_clear_btn.position = Vector2(746, 55)
	_clear_btn.size = Vector2(320, 90)
	ctr.add_child(_clear_btn)

	return ctr

func _small_btn(text: String) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 38)
	_style_flat(btn, Color(0.22, 0.08, 0.08))
	return btn

func _action_btn(text: String, is_spin: bool) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 46 if is_spin else 32)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

	if ResourceLoader.exists("res://assets/ui/btn_normal.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/ui/btn_normal.png")
		if not is_spin:
			sn.modulate_color = Color(0.6, 0.6, 0.6)
		btn.add_theme_stylebox_override("normal", sn)
		btn.add_theme_stylebox_override("focus",  sn)
	else:
		_style_flat(btn, Constants.COLOR_CRIMSON if is_spin else Color(0.2, 0.2, 0.2))

	if ResourceLoader.exists("res://assets/ui/btn_hover.png"):
		var sh := StyleBoxTexture.new()
		sh.texture = load("res://assets/ui/btn_hover.png")
		btn.add_theme_stylebox_override("hover",   sh)
		btn.add_theme_stylebox_override("pressed", sh)
	else:
		var sh := StyleBoxFlat.new()
		var base := Constants.COLOR_CRIMSON if is_spin else Color(0.2, 0.2, 0.2)
		sh.bg_color = base.lightened(0.25)
		sh.border_color = Constants.COLOR_GOLD
		sh.set_border_width_all(2)
		sh.set_corner_radius_all(8)
		btn.add_theme_stylebox_override("hover",   sh)
		btn.add_theme_stylebox_override("pressed", sh)

	if ResourceLoader.exists("res://assets/ui/btn_disabled.png"):
		var sd := StyleBoxTexture.new()
		sd.texture = load("res://assets/ui/btn_disabled.png")
		btn.add_theme_stylebox_override("disabled", sd)

	return btn

func _style_flat(btn: Button, col: Color) -> void:
	for state in ["normal", "hover", "pressed", "focus", "disabled"]:
		var s := StyleBoxFlat.new()
		s.bg_color = col if state != "disabled" else Color(0.15, 0.15, 0.15)
		if state == "hover" or state == "pressed":
			s.bg_color = col.lightened(0.2)
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(6)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

func _connect_signals() -> void:
	_spin_btn.pressed.connect(_on_spin_pressed)
	_clear_btn.pressed.connect(_on_clear_pressed)
	_bet_minus.pressed.connect(_on_bet_minus)
	_bet_plus.pressed.connect(_on_bet_plus)
	_wheel.ball_landed.connect(_on_ball_landed)
	_popup.dismissed.connect(_on_popup_dismissed)

# ── Boss reveal overlay ─────────────────────────────────────────────────────
func _show_boss_reveal() -> void:
	var overlay := Control.new()
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.z_index = 50
	add_child(overlay)

	var dim := ColorRect.new()
	dim.color = Color(0.0, 0.0, 0.0, 0.96)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(dim)

	var frame := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/boss_card_frame.png"):
		frame.texture = load("res://assets/ui/boss_card_frame.png")
	frame.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	frame.size = Vector2(700, 900)
	frame.position = Vector2(190, 80)
	frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(frame)

	var title := Label.new()
	title.text = "BOSS FLOOR"
	title.position = Vector2(0, 110)
	title.size = Vector2(1080, 120)
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 80)
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(title)

	var mod := GameManager.current_boss_modifier
	var mod_lbl := Label.new()
	mod_lbl.text = "%s\n%s" % [mod.get("name", ""), mod.get("desc", "")]
	mod_lbl.position = Vector2(60, 880)
	mod_lbl.size = Vector2(960, 120)
	mod_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mod_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	mod_lbl.add_theme_font_size_override("font_size", 30)
	mod_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	mod_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(mod_lbl)

	var tap_lbl := Label.new()
	tap_lbl.text = "Tap to face your fate..."
	tap_lbl.position = Vector2(0, 1730)
	tap_lbl.size = Vector2(1080, 60)
	tap_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	tap_lbl.add_theme_color_override("font_color", Color(0.55, 0.55, 0.55))
	tap_lbl.add_theme_font_size_override("font_size", 28)
	tap_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.add_child(tap_lbl)

	var pulse := create_tween().set_loops()
	pulse.tween_property(tap_lbl, "modulate:a", 0.25, 0.9)
	pulse.tween_property(tap_lbl, "modulate:a", 1.0, 0.9)

	_devil.say("boss_floor", 0.0)

	overlay.gui_input.connect(func(event: InputEvent):
		if event is InputEventScreenTouch and event.pressed:
			_devil.hide_now()
			overlay.queue_free()
		elif event is InputEventMouseButton and event.pressed:
			_devil.hide_now()
			overlay.queue_free()
	)

# ── Spin logic ──────────────────────────────────────────────────────────────
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
	var bets   := _table.get_bets()
	var total  := _table.get_total_bet()
	var ret    := CardManager.calculate_winnings(bets, number)
	var net    := ret - total

	GameManager.add_chips(ret)
	GameManager.on_spin_complete(ret > total)
	_hud.update_spin_count(GameManager.spin_count)

	if ret > total:
		_wheel.show_win_burst()
		AudioManager.play_win()
	else:
		AudioManager.play_loss()

	# Devil reacts to result
	if number == 0:
		_devil.say("zero", 3.5)
	elif net > total * 2:
		_devil.say("win_big", 3.5)
	elif net > 0:
		_devil.say("win_small", 3.5)
	elif net == 0:
		_devil.say("push", 3.5)
	else:
		_devil.say("loss", 3.5)

	var notes := _build_card_notes(number, ret, total)
	_popup.show_result(number, net, notes)
	_pending_number = number
	_pending_net    = net
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
	get_tree().change_scene_to_file("res://scenes/FloorTransition.tscn")

func _go_to_game_over() -> void:
	SaveManager.save_run(GameManager.chips, GameManager.floor_number)
	get_tree().change_scene_to_file("res://scenes/GameOver.tscn")

func _on_clear_pressed() -> void:
	_table.clear_bets()

func _on_bet_minus() -> void:
	var cur := _table.get_chip_amount()
	var nxt := max(cur - Constants.BET_STEP, GameManager.get_effective_min_bet())
	_table.set_chip_amount(nxt)
	_bet_amount_lbl.text = str(nxt)

func _on_bet_plus() -> void:
	var cur := _table.get_chip_amount()
	var nxt := min(cur + Constants.BET_STEP, Constants.MAX_BET)
	_table.set_chip_amount(nxt)
	_bet_amount_lbl.text = str(nxt)

func _show_toast(msg: String) -> void:
	var toast := Label.new()
	toast.text = msg
	toast.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	toast.add_theme_font_size_override("font_size", 30)
	toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	toast.position = Vector2(540 - 250, 850)
	toast.size     = Vector2(500, 70)
	add_child(toast)
	var tw := create_tween()
	tw.tween_property(toast, "modulate:a", 0.0, 1.8).set_delay(0.6)
	tw.tween_callback(toast.queue_free)

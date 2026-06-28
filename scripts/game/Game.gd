extends Control

# ── UI refs ─────────────────────────────────────────────────────────────────
var _table: BettingTable
var _ante_lbl: Label
var _chips_lbl: Label
var _hand_lbl: Label
var _goal_lbl: Label
var _bar_fill_wrap: Control
var _bar_fill: TextureRect
var _bar_label: Label
var _joker_row: HBoxContainer
var _msg_lbl: Label
var _staked_lbl: Label
var _chip_btns: Array[Button] = []
var _spin_btn: Button
var _clear_btn: Button
var _spin_overlay: Control
var _wheel_img: TextureRect
var _ball_img: TextureRect
var _result_circle: Control
var _result_number_lbl: Label
var _dialogue_lbl: Label
var _overlay_msg_lbl: Label
var _continue_btn: Button

# ── State ────────────────────────────────────────────────────────────────────
var _is_spinning  := false
var _rot_accum    := 0.0
var _ball_accum   := 0.0
var _pending_ante_up := false

const WIN_LINES := [
	"Beginner's luck. Savour it — the House has a long memory.",
	"The wheel smiles on you. It will not smile twice.",
	"Take your winnings. Consider it a loan against your soul.",
	"Fortune is a fickle mistress… enjoy her while she stays.",
]
const LOSS_LINES := [
	"The House always remembers. And the House always wins.",
	"Ah. The wheel knows what you truly deserve.",
	"Another offering to the table. How generous of you.",
	"Did you feel that? That was hope, leaving you.",
]
const PUSH_LINES := [
	"A push. The wheel toys with you before it feasts.",
	"Even fate hesitates tonight. Do not mistake it for mercy.",
]

# ── Layout heights (sum = 1920px) ────────────────────────────────────────────
const H_ANTE   := 140  # "Ante I" top bar
const H_STATS  := 110  # chips | hand | goal
const H_BAR    := 70   # progress bar
const H_JOKERS := 100  # joker icon row
const H_LABEL  := 90   # "Place Your Wager"
const H_TABLE  := 585  # betting table (1040 × 575 image)
const H_MSG    := 95   # message row
const H_CHIPS  := 150  # chip selector
const H_BTNS   := 290  # CLEAR + SPIN
const H_PAD    := 290  # bottom padding

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	_connect_signals()
	_refresh_hud()
	_refresh_jokers()

# ─────────────────────────────────────────────────────────────────────────────
func _build_ui() -> void:
	# Background
	var bg := ColorRect.new()
	bg.color = Constants.COLOR_BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# Radial gradient accent (dark red glow behind table area)
	var glow_bg := ColorRect.new()
	glow_bg.color = Color(0.165, 0.047, 0.047, 0.35)
	glow_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	glow_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(glow_bg)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.add_theme_constant_override("separation", 0)
	add_child(vbox)

	vbox.add_child(_build_ante_bar())
	vbox.add_child(_build_stats_row())
	vbox.add_child(_build_progress_bar())
	vbox.add_child(_build_joker_row())
	vbox.add_child(_build_wager_label())
	vbox.add_child(_build_table_section())
	vbox.add_child(_build_msg_row())
	vbox.add_child(_build_chip_selector())
	vbox.add_child(_build_action_buttons())

	_build_spin_overlay()

# ── Ante bar ─────────────────────────────────────────────────────────────────
func _build_ante_bar() -> Control:
	var bar := _section(H_ANTE, Color(0.0, 0.0, 0.0, 0.5))

	_ante_lbl = Label.new()
	var ante_lbl := _ante_lbl
	ante_lbl.text = "Ante %s" % Constants.rom(GameManager.ante)
	ante_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	ante_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	ante_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	ante_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	ante_lbl.add_theme_font_size_override("font_size", 26)
	ante_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar.add_child(ante_lbl)

	# Gold bottom separator
	var sep := ColorRect.new()
	sep.color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.35)
	sep.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	sep.size.y = 1.0
	sep.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar.add_child(sep)

	return bar

# ── Stats row ─────────────────────────────────────────────────────────────────
func _build_stats_row() -> Control:
	var row := _section(H_STATS, Color(0, 0, 0, 0))

	_chips_lbl = _gold_label("640", 34)
	_chips_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_chips_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	_chips_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_chips_lbl.offset_left = 60.0
	row.add_child(_chips_lbl)

	_hand_lbl = _plain_label("HAND 1 / 4", 22)
	_hand_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_hand_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hand_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	row.add_child(_hand_lbl)

	_goal_lbl = _gold_label("GOAL 1,000", 22)
	_goal_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_goal_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_goal_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_goal_lbl.offset_right = -60.0
	row.add_child(_goal_lbl)

	return row

# ── Progress bar ─────────────────────────────────────────────────────────────
func _build_progress_bar() -> Control:
	var section := _section(H_BAR, Color(0, 0, 0, 0))

	var bar_w := 900.0
	var bar_h := 28.0
	var bar_x := (1080.0 - bar_w) / 2.0
	var bar_y := (H_BAR - bar_h) / 2.0

	# Background
	var bar_bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/bar_bg.png"):
		bar_bg.texture = load("res://assets/ui/bar_bg.png")
	bar_bg.stretch_mode = TextureRect.STRETCH_SCALE
	bar_bg.position = Vector2(bar_x, bar_y)
	bar_bg.size = Vector2(bar_w, bar_h)
	bar_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(bar_bg)

	# Fill wrapper (clip)
	_bar_fill_wrap = Control.new()
	_bar_fill_wrap.position = Vector2(bar_x + 3, bar_y + 3)
	_bar_fill_wrap.size = Vector2(0, bar_h - 6)
	_bar_fill_wrap.clip_contents = true
	_bar_fill_wrap.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(_bar_fill_wrap)

	_bar_fill = TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/bar_fill.png"):
		_bar_fill.texture = load("res://assets/ui/bar_fill.png")
	_bar_fill.stretch_mode = TextureRect.STRETCH_SCALE
	_bar_fill.position = Vector2(0, 0)
	_bar_fill.size = Vector2(bar_w - 6, bar_h - 6)
	_bar_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_bar_fill_wrap.add_child(_bar_fill)

	# Progress text
	_bar_label = Label.new()
	_bar_label.position = Vector2(bar_x, bar_y)
	_bar_label.size = Vector2(bar_w, bar_h)
	_bar_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_bar_label.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_bar_label.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_bar_label.add_theme_font_size_override("font_size", 18)
	_bar_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(_bar_label)

	return section

# ── Joker row ─────────────────────────────────────────────────────────────────
func _build_joker_row() -> Control:
	var section := _section(H_JOKERS, Color(0, 0, 0, 0))

	var hbox := HBoxContainer.new()
	hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 10)
	hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(hbox)

	var cap := _plain_label("JOKERS", 18)
	cap.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	hbox.add_child(cap)

	_joker_row = HBoxContainer.new()
	_joker_row.add_theme_constant_override("separation", 8)
	_joker_row.alignment = BoxContainer.ALIGNMENT_BEGIN
	_joker_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(_joker_row)

	return section

# ── "Place Your Wager" label ───────────────────────────────────────────────
func _build_wager_label() -> Control:
	var section := _section(H_LABEL, Color(0, 0, 0, 0))

	var lbl := Label.new()
	lbl.text = "Place Your Wager"
	lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	lbl.add_theme_font_size_override("font_size", 34)
	lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(lbl)

	return section

# ── Betting table ─────────────────────────────────────────────────────────────
func _build_table_section() -> Control:
	var section := _section(H_TABLE, Color(0, 0, 0, 0))
	_table = BettingTable.new()
	section.add_child(_table)
	return section

# ── Message row ───────────────────────────────────────────────────────────────
func _build_msg_row() -> Control:
	var section := _section(H_MSG, Color(0, 0, 0, 0))

	_msg_lbl = Label.new()
	_msg_lbl.text = "Place your bets"
	_msg_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_msg_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_TOP
	_msg_lbl.offset_top = 14.0
	_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_msg_lbl.add_theme_font_size_override("font_size", 28)
	_msg_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(_msg_lbl)

	_staked_lbl = Label.new()
	_staked_lbl.text = "STAKED 0"
	_staked_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_staked_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_staked_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_BOTTOM
	_staked_lbl.offset_bottom = -14.0
	_staked_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_staked_lbl.add_theme_font_size_override("font_size", 24)
	_staked_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(_staked_lbl)

	return section

# ── Chip selector ─────────────────────────────────────────────────────────────
func _build_chip_selector() -> Control:
	var section := _section(H_CHIPS, Color(0, 0, 0, 0))

	var hbox := HBoxContainer.new()
	hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 50)
	section.add_child(hbox)

	for v in Constants.CHIP_DENOMINATIONS:
		var btn := _make_chip_button(v)
		_chip_btns.append(btn)
		hbox.add_child(btn)

	_update_chip_selection()
	return section

func _make_chip_button(value: int) -> Button:
	var btn := Button.new()
	btn.custom_minimum_size = Vector2(100, 100)
	btn.text = str(value)
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 30)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_style_chip_btn(btn, false)

	if ResourceLoader.exists("res://assets/layout/chip_default.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/layout/chip_default.png")
		btn.add_theme_stylebox_override("normal", sn)
		btn.add_theme_stylebox_override("focus",  sn)
	btn.pressed.connect(_on_chip_selected.bind(value))
	return btn

func _style_chip_btn(btn: Button, selected: bool) -> void:
	if ResourceLoader.exists("res://assets/layout/chip_default.png"):
		return
	for state in ["normal", "hover", "pressed", "focus"]:
		var s := StyleBoxFlat.new()
		s.bg_color = Color(0.22, 0.15, 0.05) if selected else Color(0.12, 0.08, 0.03)
		s.border_color = Constants.COLOR_GOLD if selected else Color(0.5, 0.4, 0.1, 0.5)
		s.set_border_width_all(3 if selected else 1)
		s.set_corner_radius_all(50)
		btn.add_theme_stylebox_override(state, s)

func _update_chip_selection() -> void:
	var selected := _table.get_chip_amount() if _table else Constants.DEFAULT_CHIP
	for i in range(Constants.CHIP_DENOMINATIONS.size()):
		var btn := _chip_btns[i]
		var v := Constants.CHIP_DENOMINATIONS[i]
		btn.modulate = Color.WHITE if v == selected else Color(1,1,1,0.55)
		if v == selected:
			btn.scale = Vector2(1.15, 1.15)
		else:
			btn.scale = Vector2(1.0, 1.0)

# ── CLEAR + SPIN buttons ──────────────────────────────────────────────────────
func _build_action_buttons() -> Control:
	var section := _section(H_BTNS, Color(0, 0, 0, 0))

	# CLEAR button
	_clear_btn = _action_btn("CLEAR", false)
	_clear_btn.position = Vector2(60, 40)
	_clear_btn.size = Vector2(280, 130)
	section.add_child(_clear_btn)

	# SPIN button
	_spin_btn = _action_btn("SPIN", true)
	_spin_btn.position = Vector2(380, 20)
	_spin_btn.size = Vector2(640, 170)
	section.add_child(_spin_btn)

	return section

# ── Spin overlay (full screen) ────────────────────────────────────────────────
func _build_spin_overlay() -> void:
	_spin_overlay = Control.new()
	_spin_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	_spin_overlay.z_index = 60
	_spin_overlay.hide()
	add_child(_spin_overlay)

	# Dark overlay background
	var dim := ColorRect.new()
	dim.color = Color(0.027, 0.012, 0.012, 0.97)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(dim)

	# Overlay title (e.g. "Ante I")
	var overlay_title := Label.new()
	overlay_title.name = "OverlayTitle"
	overlay_title.text = "Ante %s" % Constants.rom(GameManager.ante)
	overlay_title.set_anchors_preset(Control.PRESET_TOP_WIDE)
	overlay_title.size.y = 80.0
	overlay_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	overlay_title.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	overlay_title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	overlay_title.add_theme_font_size_override("font_size", 24)
	overlay_title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(overlay_title)

	# Wheel container (centered)
	var wheel_cx := 540.0
	var wheel_cy := 640.0
	var wheel_size := 600.0

	# Wheel base image (rotates)
	_wheel_img = TextureRect.new()
	if ResourceLoader.exists("res://assets/wheel/wheel_base.png"):
		_wheel_img.texture = load("res://assets/wheel/wheel_base.png")
	_wheel_img.stretch_mode = TextureRect.STRETCH_SCALE
	_wheel_img.position = Vector2(wheel_cx - wheel_size/2, wheel_cy - wheel_size/2)
	_wheel_img.size = Vector2(wheel_size, wheel_size)
	_wheel_img.pivot_offset = Vector2(wheel_size/2, wheel_size/2)
	_wheel_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_wheel_img)

	# Wheel numbers layer (rotates with base)
	var wheel_nums := TextureRect.new()
	wheel_nums.name = "WheelNums"
	if ResourceLoader.exists("res://assets/wheel/wheel_numbers.png"):
		wheel_nums.texture = load("res://assets/wheel/wheel_numbers.png")
	wheel_nums.stretch_mode = TextureRect.STRETCH_SCALE
	wheel_nums.position = Vector2(wheel_cx - wheel_size/2, wheel_cy - wheel_size/2)
	wheel_nums.size = Vector2(wheel_size, wheel_size)
	wheel_nums.pivot_offset = Vector2(wheel_size/2, wheel_size/2)
	wheel_nums.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(wheel_nums)

	# Rim
	var rim_size := wheel_size + 60.0
	var rim := TextureRect.new()
	if ResourceLoader.exists("res://assets/wheel/wheel_rim.png"):
		rim.texture = load("res://assets/wheel/wheel_rim.png")
	rim.stretch_mode = TextureRect.STRETCH_SCALE
	rim.position = Vector2(wheel_cx - rim_size/2, wheel_cy - rim_size/2)
	rim.size = Vector2(rim_size, rim_size)
	rim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(rim)

	# Pointer / marker at top
	var pointer := ColorRect.new()
	pointer.color = Constants.COLOR_GOLD
	pointer.size = Vector2(14, 28)
	pointer.position = Vector2(wheel_cx - 7, wheel_cy - wheel_size/2 - 8)
	pointer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(pointer)

	# Ball
	_ball_img = TextureRect.new()
	if ResourceLoader.exists("res://assets/wheel/ball.png"):
		_ball_img.texture = load("res://assets/wheel/ball.png")
	_ball_img.stretch_mode = TextureRect.STRETCH_SCALE
	_ball_img.size = Vector2(36, 36)
	_ball_img.pivot_offset = Vector2(18, 18)
	_ball_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_ball_img)

	# Result circle (number display in center)
	_result_circle = Control.new()
	_result_circle.position = Vector2(wheel_cx - 50, wheel_cy - 50)
	_result_circle.size = Vector2(100, 100)
	_result_circle.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_circle.hide()
	_spin_overlay.add_child(_result_circle)

	var circle_bg := ColorRect.new()
	circle_bg.name = "CircleBg"
	circle_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	circle_bg.color = Color(0.1, 0.05, 0.05)
	circle_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_circle.add_child(circle_bg)

	_result_number_lbl = Label.new()
	_result_number_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_result_number_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_number_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_result_number_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_result_number_lbl.add_theme_font_size_override("font_size", 48)
	_result_number_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_circle.add_child(_result_number_lbl)

	# Win burst
	var burst := TextureRect.new()
	burst.name = "WinBurst"
	if ResourceLoader.exists("res://assets/effects/win_burst.png"):
		burst.texture = load("res://assets/effects/win_burst.png")
	burst.stretch_mode = TextureRect.STRETCH_SCALE
	burst.position = Vector2(wheel_cx - 256, wheel_cy - 256)
	burst.size = Vector2(512, 512)
	burst.mouse_filter = Control.MOUSE_FILTER_IGNORE
	burst.modulate.a = 0.0
	burst.hide()
	_spin_overlay.add_child(burst)

	# Dialogue box (below wheel)
	var dlg_box := Panel.new()
	dlg_box.name = "DialogueBox"
	dlg_box.position = Vector2(90, 1020)
	dlg_box.size = Vector2(900, 200)
	dlg_box.hide()
	var dlg_style := StyleBoxFlat.new()
	dlg_style.bg_color = Color(0.078, 0.024, 0.024, 0.92)
	dlg_style.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.5)
	dlg_style.set_border_width_all(1)
	dlg_style.set_corner_radius_all(12)
	dlg_box.add_theme_stylebox_override("panel", dlg_style)
	_spin_overlay.add_child(dlg_box)

	var devil_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		devil_icon.texture = load("res://assets/effects/devil_watermark.png")
	devil_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	devil_icon.position = Vector2(16, 20)
	devil_icon.size = Vector2(80, 160)
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	dlg_box.add_child(devil_icon)

	_dialogue_lbl = Label.new()
	_dialogue_lbl.position = Vector2(110, 16)
	_dialogue_lbl.size = Vector2(770, 168)
	_dialogue_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	_dialogue_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_dialogue_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	_dialogue_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_dialogue_lbl.add_theme_font_size_override("font_size", 30)
	_dialogue_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	dlg_box.add_child(_dialogue_lbl)

	# Overlay message (win/loss amount)
	_overlay_msg_lbl = Label.new()
	_overlay_msg_lbl.position = Vector2(0, 1240)
	_overlay_msg_lbl.size = Vector2(1080, 60)
	_overlay_msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_overlay_msg_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_overlay_msg_lbl.add_theme_font_size_override("font_size", 30)
	_overlay_msg_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_overlay_msg_lbl)

	# Continue button
	_continue_btn = _action_btn("CONTINUE", true)
	_continue_btn.position = Vector2(540 - 220, 1320)
	_continue_btn.size = Vector2(440, 140)
	_continue_btn.hide()
	_continue_btn.pressed.connect(_on_continue_pressed)
	_spin_overlay.add_child(_continue_btn)

# ─────────────────────────────────────────────────────────────────────────────
func _connect_signals() -> void:
	_spin_btn.pressed.connect(_on_spin_pressed)
	_clear_btn.pressed.connect(_on_clear_pressed)
	_table.bet_placed.connect(_on_bet_placed)
	GameManager.chips_changed.connect(_on_chips_changed)
	GameManager.hand_changed.connect(_on_hand_changed)
	GameManager.ante_changed.connect(_on_ante_changed)
	GameManager.ante_up.connect(func(_a): _pending_ante_up = true)
	GameManager.cards_changed.connect(_refresh_jokers)

# ── HUD refresh ───────────────────────────────────────────────────────────────
func _refresh_hud() -> void:
	_on_chips_changed(GameManager.chips)
	_on_hand_changed(GameManager.hand, GameManager.max_hand)
	_on_ante_changed(GameManager.ante, GameManager.chips, GameManager.target)

func _on_chips_changed(amount: int) -> void:
	_chips_lbl.text = _fmt(amount)

func _on_hand_changed(hand: int, max_hand: int) -> void:
	_hand_lbl.text = "HAND %d / %d" % [hand, max_hand]

func _on_ante_changed(ante: int, chips_amount: int, target: int) -> void:
	_goal_lbl.text = "GOAL %s" % _fmt(target)
	# Update ante labels in main and overlay
	if _ante_lbl:
		_ante_lbl.text = "Ante %s" % Constants.rom(ante)
	var ot := _spin_overlay.get_node_or_null("OverlayTitle")
	if ot: ot.text = "Ante %s" % Constants.rom(ante)

	# Progress: chips vs target
	var ratio := clamp(float(chips_amount) / float(max(target, 1)), 0.0, 1.0)
	_bar_fill_wrap.size.x = (900.0 - 6.0) * ratio
	_bar_label.text = "%s / %s" % [_fmt(chips_amount), _fmt(target)]

func _refresh_jokers() -> void:
	for child in _joker_row.get_children():
		child.queue_free()
	for card in GameManager.owned_cards:
		var icon_path := "res://assets/cards/icon_%s.png" % card.get("id", "")
		var rarity := card.get("rarity", "common")
		var accent: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)
		var slot := Panel.new()
		slot.custom_minimum_size = Vector2(60, 60)
		var ss := StyleBoxFlat.new()
		ss.bg_color = Color(0, 0, 0, 0.42)
		ss.border_color = accent
		ss.set_border_width_all(2)
		ss.set_corner_radius_all(8)
		slot.add_theme_stylebox_override("panel", ss)
		_joker_row.add_child(slot)
		if ResourceLoader.exists(icon_path):
			var ic := TextureRect.new()
			ic.texture = load(icon_path)
			ic.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			ic.set_anchors_preset(Control.PRESET_FULL_RECT)
			ic.mouse_filter = Control.MOUSE_FILTER_IGNORE
			slot.add_child(ic)

func _on_bet_placed(_key: String, _amount: int) -> void:
	var total := _table.get_total_bet()
	_staked_lbl.text = "STAKED %s" % _fmt(total)
	_msg_lbl.text = "Wager placed — spin when ready"

# ── Chip selection ─────────────────────────────────────────────────────────────
func _on_chip_selected(value: int) -> void:
	_table.set_chip_amount(value)
	_update_chip_selection()

# ── CLEAR ─────────────────────────────────────────────────────────────────────
func _on_clear_pressed() -> void:
	_table.clear_bets()
	_staked_lbl.text = "STAKED 0"
	_msg_lbl.text = "Bets cleared"

# ── SPIN ──────────────────────────────────────────────────────────────────────
func _on_spin_pressed() -> void:
	if _is_spinning:
		return
	var staked := _table.get_total_bet()
	if staked == 0:
		_msg_lbl.text = "Place a bet first"
		return
	if staked > GameManager.chips:
		_msg_lbl.text = "Not enough chips!"
		return

	_is_spinning = true
	_spin_btn.disabled = true
	_clear_btn.disabled = true

	var number := randi() % 37
	_open_spin_overlay(number, staked)

func _open_spin_overlay(number: int, staked: int) -> void:
	# Show overlay
	_spin_overlay.show()
	_result_circle.hide()
	_continue_btn.hide()
	_spin_overlay.get_node_or_null("DialogueBox").hide()
	_overlay_msg_lbl.text = "No more bets…"
	_overlay_msg_lbl.modulate.a = 1.0
	var burst := _spin_overlay.get_node_or_null("WinBurst")
	if burst: burst.hide()

	# Ball initial orbit position
	var ball_start_angle := -TAU / 4.0
	_orbit_ball(_ball_accum)
	_ball_img.show()

	AudioManager.play_spin()

	# Compute target wheel angle
	var seq := Constants.WHEEL_SEQUENCE
	var idx := seq.find(number)
	var pocket_angle := float(idx) / float(seq.size()) * TAU
	var target_rot := (TAU - pocket_angle) # bring pocket to top
	var prev_rot := _rot_accum
	var base_rot := prev_rot - fmod(prev_rot, TAU)
	var new_rot := base_rot + TAU * 6.0 + target_rot
	if new_rot <= prev_rot:
		new_rot += TAU
	_rot_accum = new_rot

	var ball_new := _ball_accum - TAU * 9.0

	# Animate
	var tw := create_tween()
	tw.set_parallel(true)
	tw.tween_method(_rotate_wheel, prev_rot, new_rot, 4.6).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUART)
	tw.tween_method(_orbit_ball, _ball_accum, ball_new, 3.8).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	_ball_accum = ball_new

	tw.set_parallel(false)
	tw.tween_interval(0.15)
	tw.tween_callback(func(): _land_ball(number, staked))

func _rotate_wheel(angle: float) -> void:
	_wheel_img.rotation = angle
	var nums := _spin_overlay.get_node_or_null("WheelNums")
	if nums: nums.rotation = angle

func _orbit_ball(arc: float) -> void:
	var cx := 540.0; var cy := 640.0
	var orbit_r := 275.0
	_ball_img.position = Vector2(
		cx + orbit_r * cos(arc) - 18.0,
		cy + orbit_r * sin(arc) - 18.0
	)

func _land_ball(number: int, staked: int) -> void:
	var seq := Constants.WHEEL_SEQUENCE
	var idx := seq.find(number)
	var pocket_a := (float(idx) / float(seq.size())) * TAU - TAU / 4.0 + _rot_accum
	var cx := 540.0; var cy := 640.0
	var land_r := 230.0
	var target_pos := Vector2(cx + land_r * cos(pocket_a) - 18.0, cy + land_r * sin(pocket_a) - 18.0)
	var tw := create_tween()
	tw.tween_property(_ball_img, "position", target_pos, 0.35).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BOUNCE)
	tw.tween_callback(func(): _show_result(number, staked))

func _show_result(number: int, staked: int) -> void:
	# Calculate payout
	var bets := _table.get_bets()
	var payout := CardManager.calculate_winnings(bets, number)
	var net := payout - staked

	GameManager.spend_chips(staked)
	GameManager.add_chips(payout)
	GameManager.on_spin_complete(payout > staked)

	# Show number in wheel center
	_result_circle.show()
	_result_number_lbl.text = str(number)
	var circ_bg := _result_circle.get_node_or_null("CircleBg") as ColorRect
	if circ_bg:
		if number == 0:
			circ_bg.color = Color(0.1, 0.25, 0.1)
		elif number in Constants.RED_NUMBERS:
			circ_bg.color = Color(0.55, 0.04, 0.04)
		else:
			circ_bg.color = Color(0.05, 0.05, 0.05)

	# Dialogue
	var pool: Array
	var outcome_text: String
	if net > 0:
		pool = WIN_LINES
		outcome_text = "+%s chips" % _fmt(net)
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
		_trigger_win_burst()
	elif payout > 0:
		pool = PUSH_LINES
		outcome_text = "Pushed — %s returned" % _fmt(payout)
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	else:
		pool = LOSS_LINES
		outcome_text = "Lost %s chips" % _fmt(staked)
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_CRIMSON)

	_dialogue_lbl.text = pool[randi() % pool.size()]
	_spin_overlay.get_node_or_null("DialogueBox").show()
	_overlay_msg_lbl.text = outcome_text

	# Table win glows
	_table.show_win_zones(number)

	_continue_btn.show()
	_continue_btn.text = _continue_label()

func _continue_label() -> String:
	if GameManager.check_game_over():
		return "RUIN ACCEPTED"
	return "CONTINUE"

func _trigger_win_burst() -> void:
	var burst := _spin_overlay.get_node_or_null("WinBurst")
	if not burst:
		return
	burst.show()
	burst.modulate.a = 0.0
	var tw := create_tween()
	tw.tween_property(burst, "modulate:a", 0.9, 0.25)
	tw.tween_property(burst, "modulate:a", 0.0, 1.2)
	tw.tween_callback(burst.hide)

func _on_continue_pressed() -> void:
	_spin_overlay.hide()
	_ball_img.hide()
	_table.clear_bets()
	_staked_lbl.text = "STAKED 0"
	_msg_lbl.text = "Place your bets"
	_is_spinning = false
	_spin_btn.disabled = false
	_clear_btn.disabled = false

	if GameManager.check_game_over():
		_go_game_over()
		return

	if _pending_ante_up:
		_pending_ante_up = false
		_go_to_shop()

func _go_to_shop() -> void:
	get_tree().change_scene_to_file("res://scenes/FloorTransition.tscn")

func _go_game_over() -> void:
	SaveManager.save_run(GameManager.chips, GameManager.ante)
	get_tree().change_scene_to_file("res://scenes/GameOver.tscn")

# ── Helpers ───────────────────────────────────────────────────────────────────
func _section(height: int, col: Color) -> Control:
	var s := Control.new()
	s.custom_minimum_size = Vector2(1080, height)
	if col.a > 0.0:
		var bg := ColorRect.new()
		bg.color = col
		bg.set_anchors_preset(Control.PRESET_FULL_RECT)
		bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		s.add_child(bg)
	return s

func _gold_label(text: String, size: int) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	l.add_theme_font_size_override("font_size", size)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l

func _plain_label(text: String, size: int) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.8))
	l.add_theme_font_size_override("font_size", size)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l

func _action_btn(text: String, is_primary: bool) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 44 if is_primary else 30)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

	if is_primary and ResourceLoader.exists("res://assets/ui/btn_normal.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/ui/btn_normal.png")
		btn.add_theme_stylebox_override("normal", sn)
		btn.add_theme_stylebox_override("focus",  sn)
	else:
		for state in ["normal", "hover", "pressed", "focus", "disabled"]:
			var s := StyleBoxFlat.new()
			if is_primary:
				s.bg_color = Color(0.35, 0.04, 0.04) if state not in ["disabled"] else Color(0.18,0.18,0.18)
				s.bg_color = s.bg_color.lightened(0.2) if state in ["hover","pressed"] else s.bg_color
			else:
				s.bg_color = Color(0.12, 0.04, 0.04) if state not in ["disabled"] else Color(0.15,0.15,0.15)
				s.bg_color = s.bg_color.lightened(0.15) if state in ["hover","pressed"] else s.bg_color
			s.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.5)
			s.set_border_width_all(2)
			s.set_corner_radius_all(8)
			btn.add_theme_stylebox_override(state, s)

	if ResourceLoader.exists("res://assets/ui/btn_hover.png"):
		var sh := StyleBoxTexture.new()
		sh.texture = load("res://assets/ui/btn_hover.png")
		btn.add_theme_stylebox_override("hover",   sh)
		btn.add_theme_stylebox_override("pressed", sh)

	if ResourceLoader.exists("res://assets/ui/btn_disabled.png"):
		var sd := StyleBoxTexture.new()
		sd.texture = load("res://assets/ui/btn_disabled.png")
		btn.add_theme_stylebox_override("disabled", sd)

	return btn

func _fmt(n: int) -> String:
	return str(n)

extends Control

# ── UI refs ─────────────────────────────────────────────────────────────────
var _table: BettingTable
var _ante_lbl: Label
var _sin_lbl: Label
var _chips_lbl: Label
var _hand_lbl: Label
var _goal_lbl: Label
var _bar_fill_wrap: Control
var _bar_fill: TextureRect
var _bar_label: Label
var _joker_row: HBoxContainer
var _msg_lbl: Label
var _staked_lbl: Label
var _mode_btn: Button
var _chip_btns: Array[Button] = []
var _spin_btn: Button
var _clear_btn: Button
var _spin_overlay: Control
var _wheel_view: WheelView
var _ball_img: TextureRect
var _result_circle: Panel
var _result_circle_style: StyleBoxFlat
var _result_number_lbl: Label
var _bets_grid: GridContainer
var _dialogue_lbl: Label
var _overlay_msg_lbl: Label
var _continue_btn: Button

# ── State ────────────────────────────────────────────────────────────────────
var _is_spinning  := false
var _rot_accum    := 0.0
var _ball_accum   := 0.0
var _pending_ante_up := false
var _shown_chips  := 0
var _chips_tween: Tween
var _spin_tween: Tween
var _ball_from    := 0.0
var _ball_to      := 0.0
var _last_bets: Dictionary = {}
var _rebet_btn: Button
var _change_btn: Button
var _ov_goal_lbl: Label
var _ov_bar_fill: ColorRect
var _joker_bonus_lbl: Label

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
const ZERO_LINES := [
	"ZERO! That is MY pocket, mortal. How generous of you to visit.",
	"The green void claims all. Even my appetite has limits… almost.",
	"The house's favourite number. Mine too. What a coincidence.",
]

# ── Layout heights (sum = 1920px) ────────────────────────────────────────────
const H_ANTE   := 130  # "Ante I" top bar
const H_STATS  := 100  # chips | hand | goal
const H_BAR    := 60   # progress bar
const H_JOKERS := 90   # joker icon row
const H_LABEL  := 80   # "Place Your Wager"
const H_TABLE  := 600  # betting table (1080 × 600 image, full width)
const H_MSG    := 90   # message row
const H_CHIPS  := 140  # chip selector
const H_BTNS   := 280  # CLEAR + SPIN

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_shown_chips = GameManager.chips
	_build_ui()
	_table.set_simple_mode(SaveManager.get_setting("simple_betting", true))
	_update_mode_btn()
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
	ante_lbl.offset_bottom = -34.0  # leave room for the sin line beneath
	ante_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	ante_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	ante_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	ante_lbl.add_theme_font_size_override("font_size", 26)
	ante_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar.add_child(ante_lbl)

	# The reigning sin's blessing, spelled out under the ante title
	_sin_lbl = Label.new()
	_sin_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_sin_lbl.offset_bottom = -16.0
	_sin_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_sin_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_BOTTOM
	_sin_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_sin_lbl.add_theme_font_size_override("font_size", 18)
	_sin_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar.add_child(_sin_lbl)

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

# ── "Place Your Wager" label + table-mode toggle ─────────────────────────────
func _build_wager_label() -> Control:
	var section := _section(H_LABEL, Color(0, 0, 0, 0))

	var lbl := Label.new()
	lbl.text = "Place Your Wager"
	lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	lbl.offset_left = 60.0
	lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	lbl.add_theme_font_size_override("font_size", 34)
	lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	section.add_child(lbl)

	_mode_btn = Button.new()
	_mode_btn.position = Vector2(1080.0 - 60.0 - 260.0, 10.0)
	_mode_btn.size = Vector2(260, 60)
	_mode_btn.focus_mode = Control.FOCUS_NONE
	_mode_btn.add_theme_font_size_override("font_size", 24)
	_mode_btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	var s := StyleBoxFlat.new()
	s.bg_color = Color(0, 0, 0, 0.35)
	s.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.5)
	s.set_border_width_all(1)
	s.set_corner_radius_all(8)
	_mode_btn.add_theme_stylebox_override("normal", s)
	_mode_btn.add_theme_stylebox_override("focus", s)
	var sh := s.duplicate() as StyleBoxFlat
	sh.bg_color = Color(0, 0, 0, 0.55)
	_mode_btn.add_theme_stylebox_override("hover", sh)
	_mode_btn.add_theme_stylebox_override("pressed", sh)
	_mode_btn.pressed.connect(_on_mode_toggled)
	section.add_child(_mode_btn)

	return section

func _on_mode_toggled() -> void:
	AudioManager.play_ui_click()
	var simple := not _table.is_simple_mode()
	_table.set_simple_mode(simple)
	SaveManager.set_setting("simple_betting", simple)
	_update_mode_btn()

func _update_mode_btn() -> void:
	# Label shows what tapping switches TO
	_mode_btn.text = "FULL TABLE" if _table.is_simple_mode() else "EASY BETS"

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
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.size_flags_vertical   = Control.SIZE_SHRINK_CENTER
	btn.text = str(value)
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 30)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_style_chip_btn(btn, false)

	if ResourceLoader.exists("res://assets/layout/chip_default.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/layout/chip_default.png")
		# Override every state — otherwise the default gray theme stylebox
		# shows through on hover/press (touch leaves buttons in hover state)
		for state in ["normal", "focus", "hover", "pressed", "disabled"]:
			btn.add_theme_stylebox_override(state, sn)
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
		btn.modulate = Color.WHITE if v == selected else Color(1, 1, 1, 0.5)

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
# Layout:
#   y=0-90:      run header (ante · hand | chips/goal) + progress bar
#   y=150-850:   Wheel (700×700 centered at x=540, y=500)
#   y=860-942:   "No more bets…" / big outcome text
#   y=960-1260:  YOUR WAGERS panel (per-bet returns + joker bonus)
#   y=1280-1430: Devil dialogue box
#   y=1460-1590: CHANGE BETS + REBET & SPIN (or single continue)
const _WCX := 540.0   # wheel center x
const _WCY := 500.0   # wheel center y
const _WSZ := 700.0   # wheel diameter
const _ORB := 320.0   # ball orbit radius (rim area)
const _LND := 268.0   # ball land radius (number-pocket band)

func _build_spin_overlay() -> void:
	# Use explicit size so there is no anchor-system ambiguity
	_spin_overlay = Control.new()
	_spin_overlay.position = Vector2.ZERO
	_spin_overlay.size     = Vector2(1080, 1920)
	_spin_overlay.z_index  = 60
	_spin_overlay.hide()
	add_child(_spin_overlay)

	# Opaque background — the betting table must not ghost through
	var dim := ColorRect.new()
	dim.color = Color(0.02, 0.008, 0.008, 1.0)
	dim.position = Vector2.ZERO
	dim.size = Vector2(1080, 1920)
	dim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(dim)

	# Run header: "ANTE N · HAND h/H" left, "chips / goal" right, progress bar
	var result_hdr := Label.new()
	result_hdr.name = "OverlayTitle"
	result_hdr.text = "THE RESULT"
	result_hdr.position = Vector2(90, 0)
	result_hdr.size     = Vector2(600, 70)
	result_hdr.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	result_hdr.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	result_hdr.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	result_hdr.add_theme_font_size_override("font_size", 26)
	result_hdr.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(result_hdr)

	_ov_goal_lbl = Label.new()
	_ov_goal_lbl.position = Vector2(540, 0)
	_ov_goal_lbl.size     = Vector2(450, 70)
	_ov_goal_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_ov_goal_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_ov_goal_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_ov_goal_lbl.add_theme_font_size_override("font_size", 24)
	_ov_goal_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_ov_goal_lbl)

	var ov_bar_bg := ColorRect.new()
	ov_bar_bg.color = Color(0.12, 0.08, 0.06)
	ov_bar_bg.position = Vector2(90, 76)
	ov_bar_bg.size = Vector2(900, 10)
	ov_bar_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(ov_bar_bg)

	_ov_bar_fill = ColorRect.new()
	_ov_bar_fill.color = Constants.COLOR_GOLD
	_ov_bar_fill.position = Vector2(90, 76)
	_ov_bar_fill.size = Vector2(0, 10)
	_ov_bar_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_ov_bar_fill)

	# Procedurally drawn wheel — sectors, numbers and rim always in sync
	_wheel_view = WheelView.new()
	_wheel_view.position = Vector2(_WCX - _WSZ / 2.0, _WCY - _WSZ / 2.0)
	_wheel_view.size     = Vector2(_WSZ, _WSZ)
	_wheel_view.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_wheel_view)

	# Gold pointer triangle at top of wheel
	var pointer := ColorRect.new()
	pointer.color    = Constants.COLOR_GOLD
	pointer.size     = Vector2(14, 30)
	pointer.position = Vector2(_WCX - 7.0, _WCY - _WSZ / 2.0 - 16.0)
	pointer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(pointer)

	# Ball (positioned by _orbit_ball / _ball_flight)
	_ball_img = TextureRect.new()
	if ResourceLoader.exists("res://assets/wheel/ball.png"):
		_ball_img.texture = load("res://assets/wheel/ball.png")
	_ball_img.stretch_mode = TextureRect.STRETCH_SCALE
	_ball_img.size         = Vector2(40, 40)
	_ball_img.pivot_offset = Vector2(20, 20)
	_ball_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_ball_img)

	# Result number circle (centered on wheel hub, shown after spin)
	_result_circle = Panel.new()
	_result_circle.position = Vector2(_WCX - 65.0, _WCY - 65.0)
	_result_circle.size     = Vector2(130, 130)
	_result_circle.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_circle_style = StyleBoxFlat.new()
	_result_circle_style.bg_color = Color(0.08, 0.03, 0.03)
	_result_circle_style.border_color = Constants.COLOR_GOLD
	_result_circle_style.set_border_width_all(3)
	_result_circle_style.set_corner_radius_all(65)
	_result_circle.add_theme_stylebox_override("panel", _result_circle_style)
	_result_circle.hide()
	_spin_overlay.add_child(_result_circle)

	_result_number_lbl = Label.new()
	_result_number_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	_result_number_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_number_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_result_number_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_result_number_lbl.add_theme_font_size_override("font_size", 56)
	_result_number_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_result_circle.add_child(_result_number_lbl)

	# Win burst (centered on wheel)
	var burst := TextureRect.new()
	burst.name = "WinBurst"
	if ResourceLoader.exists("res://assets/effects/win_burst.png"):
		burst.texture = load("res://assets/effects/win_burst.png")
	burst.stretch_mode = TextureRect.STRETCH_SCALE
	burst.position     = Vector2(_WCX - 256.0, _WCY - 256.0)
	burst.size         = Vector2(512, 512)
	burst.mouse_filter = Control.MOUSE_FILTER_IGNORE
	burst.modulate.a   = 0.0
	burst.hide()
	_spin_overlay.add_child(burst)

	# "No more bets…" / outcome text
	_overlay_msg_lbl = Label.new()
	_overlay_msg_lbl.position = Vector2(0, 860)
	_overlay_msg_lbl.size     = Vector2(1080, 82)
	_overlay_msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_overlay_msg_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_overlay_msg_lbl.add_theme_font_size_override("font_size", 30)
	_overlay_msg_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_spin_overlay.add_child(_overlay_msg_lbl)

	# YOUR WAGERS panel — the answer to "what did I bet on?"
	var bets_panel := Panel.new()
	bets_panel.position = Vector2(60, 960)
	bets_panel.size     = Vector2(960, 300)
	bets_panel.clip_contents = true
	bets_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var bets_style := StyleBoxFlat.new()
	bets_style.bg_color     = Color(0.06, 0.02, 0.02, 0.9)
	bets_style.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.4)
	bets_style.set_border_width_all(1)
	bets_style.set_corner_radius_all(10)
	bets_panel.add_theme_stylebox_override("panel", bets_style)
	_spin_overlay.add_child(bets_panel)

	var bets_vbox := VBoxContainer.new()
	bets_vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	bets_vbox.offset_left = 30.0
	bets_vbox.offset_top = 14.0
	bets_vbox.offset_right = -30.0
	bets_vbox.offset_bottom = -14.0
	bets_vbox.add_theme_constant_override("separation", 8)
	bets_vbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bets_panel.add_child(bets_vbox)

	var bets_hdr := Label.new()
	bets_hdr.text = "YOUR WAGERS"
	bets_hdr.add_theme_color_override("font_color", Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.85))
	bets_hdr.add_theme_font_size_override("font_size", 20)
	bets_hdr.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bets_vbox.add_child(bets_hdr)

	_bets_grid = GridContainer.new()
	_bets_grid.columns = 2
	_bets_grid.add_theme_constant_override("h_separation", 40)
	_bets_grid.add_theme_constant_override("v_separation", 6)
	_bets_grid.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bets_vbox.add_child(_bets_grid)

	# Joker/bonus contribution line — makes the build's power visible
	_joker_bonus_lbl = Label.new()
	_joker_bonus_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_joker_bonus_lbl.add_theme_font_size_override("font_size", 24)
	_joker_bonus_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_joker_bonus_lbl.hide()
	bets_vbox.add_child(_joker_bonus_lbl)

	# Devil dialogue box
	var dlg_box := Panel.new()
	dlg_box.name     = "DialogueBox"
	dlg_box.position = Vector2(60, 1280)
	dlg_box.size     = Vector2(960, 150)
	dlg_box.hide()
	var dlg_style := StyleBoxFlat.new()
	dlg_style.bg_color     = Color(0.08, 0.02, 0.02, 0.95)
	dlg_style.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.5)
	dlg_style.set_border_width_all(1)
	dlg_style.set_corner_radius_all(10)
	dlg_box.add_theme_stylebox_override("panel", dlg_style)
	_spin_overlay.add_child(dlg_box)

	var devil_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		devil_icon.texture = load("res://assets/effects/devil_watermark.png")
	devil_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	devil_icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	# Scale into the assigned rect — the raw texture is larger and would
	# otherwise draw at native size, spilling out of the dialogue box
	devil_icon.expand_mode  = TextureRect.EXPAND_IGNORE_SIZE
	devil_icon.position     = Vector2(16, 10)
	devil_icon.size         = Vector2(70, 130)
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	dlg_box.add_child(devil_icon)

	_dialogue_lbl = Label.new()
	_dialogue_lbl.position     = Vector2(100, 10)
	_dialogue_lbl.size         = Vector2(846, 130)
	_dialogue_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_LEFT
	_dialogue_lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
	_dialogue_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	_dialogue_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_dialogue_lbl.add_theme_font_size_override("font_size", 28)
	_dialogue_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	dlg_box.add_child(_dialogue_lbl)

	# Terminal continue button (ANTE CLEARED / ACCEPT RUIN) — wide enough
	# for its longest label so text never overflows
	_continue_btn = _action_btn("NEXT SPIN", true)
	_continue_btn.position = Vector2(540 - 290, 1460)
	_continue_btn.size     = Vector2(580, 130)
	_continue_btn.add_theme_font_size_override("font_size", 38)
	_continue_btn.hide()
	_continue_btn.pressed.connect(_on_continue_pressed)
	_spin_overlay.add_child(_continue_btn)

	# Fast loop: change bets vs. instantly re-stake and spin again
	_change_btn = _action_btn("CHANGE BETS", false)
	_change_btn.position = Vector2(60, 1460)
	_change_btn.size     = Vector2(330, 130)
	_change_btn.hide()
	_change_btn.pressed.connect(_on_continue_pressed)
	_spin_overlay.add_child(_change_btn)

	_rebet_btn = _action_btn("REBET & SPIN", true)
	_rebet_btn.position = Vector2(430, 1460)
	_rebet_btn.size     = Vector2(590, 130)
	_rebet_btn.add_theme_font_size_override("font_size", 40)
	_rebet_btn.hide()
	_rebet_btn.pressed.connect(_on_rebet_pressed)
	_spin_overlay.add_child(_rebet_btn)

	# Tap anywhere during the spin to fast-forward to the result
	_spin_overlay.gui_input.connect(_on_overlay_input)

# ─────────────────────────────────────────────────────────────────────────────
func _connect_signals() -> void:
	_spin_btn.pressed.connect(_on_spin_pressed)
	_clear_btn.pressed.connect(_on_clear_pressed)
	_table.bet_placed.connect(_on_bet_placed)
	_table.bet_rejected.connect(_on_bet_rejected)
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
	if amount == _shown_chips:
		_chips_lbl.text = _fmt(amount)
		return
	# Animated count-up/down with a scale pop — makes every payout feel tactile
	if _chips_tween and _chips_tween.is_valid():
		_chips_tween.kill()
	_chips_lbl.pivot_offset = _chips_lbl.size / 2.0
	_chips_lbl.scale = Vector2.ONE
	_chips_tween = create_tween()
	_chips_tween.set_parallel(true)
	_chips_tween.tween_method(_set_chips_text, _shown_chips, amount, 0.45)
	var pop := Vector2(1.18, 1.18) if amount > _shown_chips else Vector2(0.9, 0.9)
	_chips_tween.tween_property(_chips_lbl, "scale", pop, 0.10)
	_chips_tween.set_parallel(false)
	_chips_tween.tween_property(_chips_lbl, "scale", Vector2.ONE, 0.18)

func _set_chips_text(value: int) -> void:
	_shown_chips = value
	_chips_lbl.text = _fmt(value)

func _on_hand_changed(hand: int, max_hand: int) -> void:
	_hand_lbl.text = "HAND %d / %d" % [hand, max_hand]

func _on_ante_changed(ante: int, chips_amount: int, target: int) -> void:
	_goal_lbl.text = "GOAL %s" % _fmt(target)
	# Update ante labels in main and overlay
	var sin: Dictionary = GameManager.current_sin
	if _ante_lbl:
		_ante_lbl.text = "Ante %s" % Constants.rom(ante)
		if not sin.is_empty():
			_ante_lbl.text += "  ·  %s" % sin.name
	if _sin_lbl:
		_sin_lbl.text = str(sin.get("desc", ""))
	if _ov_goal_lbl:
		_update_overlay_header()

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
			ic.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
			ic.set_anchors_preset(Control.PRESET_FULL_RECT)
			ic.mouse_filter = Control.MOUSE_FILTER_IGNORE
			slot.add_child(ic)
		else:
			# No icon art yet — show the card's monogram in its rarity colour
			var mono := Label.new()
			mono.text = _monogram(card.get("name", "?"))
			mono.set_anchors_preset(Control.PRESET_FULL_RECT)
			mono.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			mono.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
			mono.add_theme_color_override("font_color", accent)
			mono.add_theme_font_size_override("font_size", 22)
			mono.mouse_filter = Control.MOUSE_FILTER_IGNORE
			slot.add_child(mono)

func _on_bet_placed(_key: String, _amount: int) -> void:
	var total := _table.get_total_bet()
	_staked_lbl.text = "STAKED %s" % _fmt(total)
	_msg_lbl.text = "Wager placed — spin when ready"

func _on_bet_rejected() -> void:
	_msg_lbl.text = "Not enough chips for that wager"

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
	_begin_spin()

func _begin_spin() -> void:
	_is_spinning = true
	_spin_btn.disabled = true
	_clear_btn.disabled = true
	_last_bets = _table.get_bets()
	var staked := _table.get_total_bet()

	var number := randi() % 37

	# pocket_blocker: if 0 lands, redirect to an adjacent pocket
	if number == 0 and GameManager.has_card("pocket_blocker"):
		var seq := Constants.WHEEL_SEQUENCE
		var idx := seq.find(0)
		var size := seq.size()
		var step := 1 if randi() % 2 == 0 else -1
		number = seq[(idx + step + size) % size]

	# triple_ball: spin 3 balls; CardManager picks best payout automatically
	GameManager.triple_ball_numbers.clear()
	if GameManager.has_card("triple_ball"):
		var extras: Array[int] = [number]
		while extras.size() < 3:
			var n := randi() % 37
			if n not in extras:
				extras.append(n)
		GameManager.triple_ball_numbers.assign(extras)

	_open_spin_overlay(number, staked)

# One tap re-places the identical bets and spins again — the core loop
func _on_rebet_pressed() -> void:
	var staked := 0
	for key in _last_bets:
		staked += int(_last_bets[key])
	if staked == 0 or staked > GameManager.chips:
		_on_continue_pressed()
		return
	AudioManager.play_ui_click()
	_table.restore_bets(_last_bets)
	_staked_lbl.text = "STAKED %s" % _fmt(staked)
	_begin_spin()

func _on_overlay_input(event: InputEvent) -> void:
	if not (_spin_tween and _spin_tween.is_valid() and _spin_tween.is_running()):
		return
	# is_pressed() exists on the InputEvent base class; .pressed does not,
	# and accessing it through a typed InputEvent fails to compile
	if (event is InputEventMouseButton or event is InputEventScreenTouch) and event.is_pressed():
		_spin_tween.custom_step(30.0)

func _open_spin_overlay(number: int, staked: int) -> void:
	# Show overlay
	_spin_overlay.show()
	_result_circle.hide()
	_continue_btn.hide()
	_rebet_btn.hide()
	_change_btn.hide()
	_spin_overlay.get_node_or_null("DialogueBox").hide()
	_overlay_msg_lbl.text = "No more bets…"
	_overlay_msg_lbl.modulate.a = 1.0
	_overlay_msg_lbl.add_theme_font_size_override("font_size", 30)
	_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	var burst := _spin_overlay.get_node_or_null("WinBurst")
	if burst: burst.hide()

	_update_overlay_header()
	# Show what's riding on this spin right away
	_populate_bets_panel(_table.get_bets(), -1, 0)

	# Ball initial orbit position
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

	# Ball flight ends exactly on the top pocket as the wheel stops —
	# no separate landing hop, so it never appears to switch pockets.
	_ball_from = _ball_accum
	var ball_end := floorf((_ball_from - TAU * 5.0) / TAU) * TAU - TAU / 4.0
	_ball_to = ball_end
	_ball_accum = ball_end

	var spin_dur := 3.4
	_spin_tween = create_tween()
	_spin_tween.set_parallel(true)
	_spin_tween.tween_method(_rotate_wheel, prev_rot, new_rot, spin_dur)\
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUART)
	_spin_tween.tween_method(_ball_flight, 0.0, 1.0, spin_dur)\
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUART)
	_spin_tween.set_parallel(false)
	_spin_tween.tween_interval(0.25)
	_spin_tween.tween_callback(func(): _show_result(number, staked))

func _rotate_wheel(angle: float) -> void:
	_wheel_view.wheel_rotation = angle

func _orbit_ball(arc: float) -> void:
	_ball_img.position = Vector2(
		_WCX + _ORB * cos(arc) - 20.0,
		_WCY + _ORB * sin(arc) - 20.0
	)

# t in [0,1] (eased): angle decelerates toward the top pocket while the
# radius drops from the rim into the pocket band over the final stretch
func _ball_flight(t: float) -> void:
	var a := lerpf(_ball_from, _ball_to, t)
	var r := lerpf(_ORB, _LND, clampf((t - 0.72) / 0.28, 0.0, 1.0))
	_ball_img.position = Vector2(
		_WCX + r * cos(a) - 20.0,
		_WCY + r * sin(a) - 20.0
	)

func _show_result(number: int, staked: int) -> void:
	# Calculate payout
	var bets := _table.get_bets()
	var payout := CardManager.calculate_winnings(bets, number)
	var net := payout - staked

	GameManager.spend_chips(staked)
	GameManager.add_chips(payout)
	GameManager.on_spin_complete(payout > staked)

	# Show number in wheel center with a pop-in
	_result_circle.show()
	_result_circle.pivot_offset = _result_circle.size / 2.0
	_result_circle.scale = Vector2(0.2, 0.2)
	var pop_tw := create_tween()
	pop_tw.tween_property(_result_circle, "scale", Vector2.ONE, 0.35)\
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	_result_number_lbl.text = str(number)
	if number == 0:
		_result_circle_style.bg_color = Color(0.1, 0.25, 0.1)
	elif number in Constants.RED_NUMBERS:
		_result_circle_style.bg_color = Color(0.55, 0.04, 0.04)
	else:
		_result_circle_style.bg_color = Color(0.05, 0.05, 0.05)

	# Mark every wager with its return
	_populate_bets_panel(bets, number, payout)
	_update_overlay_header()

	# Dialogue
	var pool: Array
	var outcome_text: String
	if net > 0:
		pool = WIN_LINES
		outcome_text = "+%s chips" % _fmt(net)
		if GameManager.win_streak >= 2:
			outcome_text += "   ·   STREAK ×%d" % GameManager.win_streak
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
		_trigger_win_burst()
		AudioManager.play_win()
		# Big wins rattle the table
		if net >= staked * 2:
			_shake_overlay(10.0)
		else:
			_shake_overlay(4.0)
	elif net == 0:
		pool = PUSH_LINES
		outcome_text = "Pushed — %s returned" % _fmt(payout)
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	else:
		pool = LOSS_LINES
		outcome_text = "Lost %s chips" % _fmt(-net)
		_overlay_msg_lbl.add_theme_color_override("font_color", Constants.COLOR_CRIMSON)
		AudioManager.play_loss()
	if number == 0 and net <= 0:
		pool = ZERO_LINES

	_dialogue_lbl.text = pool[randi() % pool.size()]
	_spin_overlay.get_node_or_null("DialogueBox").show()
	_overlay_msg_lbl.text = outcome_text
	_overlay_msg_lbl.add_theme_font_size_override("font_size", 46)

	# Table win glows
	_table.show_win_zones(number)

	# Terminal states get one big button; otherwise offer the fast loop
	if GameManager.check_game_over() or GameManager.run_won or _pending_ante_up:
		_continue_btn.text = _continue_label()
		_continue_btn.show()
	else:
		_rebet_btn.disabled = staked > GameManager.chips
		_rebet_btn.show()
		_change_btn.show()

# Fill the YOUR WAGERS panel. result_number == -1 means the spin is still
# running (neutral list); otherwise each bet shows what it returned, and any
# difference from the total payout is credited to jokers/bonuses.
func _populate_bets_panel(bets: Dictionary, result_number: int, payout: int) -> void:
	for child in _bets_grid.get_children():
		child.queue_free()
	var keys := bets.keys()
	keys.sort()
	var base_sum := 0
	for key in keys:
		var amount := int(bets[key])
		var lbl := Label.new()
		var text := "%s  ·  %s" % [_bet_label(key), _fmt(amount)]
		var col := Constants.COLOR_TEXT
		if result_number >= 0:
			var ret := CardManager.bet_return(key, amount, result_number)
			if ret > 0:
				text += "  →  +%s" % _fmt(ret)
				col = Constants.COLOR_GOLD
				base_sum += ret
			else:
				text += "  —  LOST"
				col = Color(0.72, 0.30, 0.26)
		lbl.text = text
		lbl.add_theme_color_override("font_color", col)
		lbl.add_theme_font_size_override("font_size", 24)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_bets_grid.add_child(lbl)

	if result_number >= 0 and payout > base_sum:
		_joker_bonus_lbl.text = "JOKERS & BONUSES  →  +%s" % _fmt(payout - base_sum)
		_joker_bonus_lbl.show()
	else:
		_joker_bonus_lbl.hide()

func _bet_label(key: String) -> String:
	if key.begins_with("straight_"):
		return "NUMBER %s" % key.trim_prefix("straight_")
	match key:
		"red":    return "RED"
		"black":  return "BLACK"
		"odd":    return "ODD"
		"even":   return "EVEN"
		"low":    return "LOW 1-18"
		"high":   return "HIGH 19-36"
		"dozen1": return "1ST DOZEN"
		"dozen2": return "2ND DOZEN"
		"dozen3": return "3RD DOZEN"
		"col1":   return "COLUMN 1"
		"col2":   return "COLUMN 2"
		"col3":   return "COLUMN 3"
	return key.to_upper()

func _continue_label() -> String:
	if GameManager.check_game_over():
		return "ACCEPT RUIN"
	if GameManager.run_won:
		return "THE HOUSE FALLS"
	if _pending_ante_up:
		return "ANTE CLEARED"
	return "NEXT SPIN"

func _update_overlay_header() -> void:
	var ot := _spin_overlay.get_node_or_null("OverlayTitle") as Label
	if ot:
		var sin_name: String = GameManager.current_sin.get("name", "")
		var ante_part := "ANTE %s" % Constants.rom(GameManager.ante)
		if sin_name != "":
			ante_part += "   ·   %s" % sin_name
		ot.text = "%s   ·   HAND %d / %d" % [
			ante_part,
			mini(GameManager.hand, GameManager.max_hand),
			GameManager.max_hand,
		]
	_ov_goal_lbl.text = "%s / %s" % [_fmt(GameManager.chips), _fmt(GameManager.target)]
	var ratio := clampf(float(GameManager.chips) / float(maxi(GameManager.target, 1)), 0.0, 1.0)
	_ov_bar_fill.size.x = 900.0 * ratio

func _shake_overlay(intensity: float) -> void:
	var tw := create_tween()
	for i in range(5):
		var offset := Vector2(randf_range(-1.0, 1.0), randf_range(-1.0, 1.0)) * intensity
		tw.tween_property(_spin_overlay, "position", offset, 0.04)
	tw.tween_property(_spin_overlay, "position", Vector2.ZERO, 0.05)

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

	if GameManager.run_won:
		# Beat the final ante — victory screen offers glory or endless descent
		_pending_ante_up = false
		get_tree().change_scene_to_file("res://scenes/GameOver.tscn")
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

func _monogram(card_name: String) -> String:
	var letters := ""
	for word in card_name.split(" ", false):
		letters += word.substr(0, 1).to_upper()
		if letters.length() >= 2:
			break
	return letters

func _fmt(n: int) -> String:
	var neg := n < 0
	var s := str(absi(n))
	var out := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		out = s[i] + out
		count += 1
		if count % 3 == 0 and i > 0:
			out = "," + out
	return ("-" + out) if neg else out

class_name BettingTable
extends Control

signal bet_placed(key: String, amount: int)
signal bet_rejected()
signal bets_cleared()

var bets: Dictionary = {}
var _chip_amount: int = Constants.DEFAULT_CHIP
var _chip_labels: Dictionary = {}
var _chip_sprites: Dictionary = {}
var _win_glows: Dictionary = {}

# Easy-bets layer: big thumb-sized buttons for the common outside bets,
# toggled against the classic full table. Both write into the same `bets`.
var _full_layer: Control
var _simple_layer: Control
var _simple_labels: Dictionary = {}   # ui_key -> amount Label
var _lucky_btn: Button
var _lucky_number: int = -1

# Table display dimensions — must match actual bet_layout.png (1080×600)
const TABLE_W := 1080.0
const TABLE_H := 600.0

# All zones: [key, cx%, cy%, w%, h%, numbers, odds]
# cx/cy are zone CENTERS as % of table image (same as prototype makeZones())
static func _make_zones() -> Array:
	var Z := []
	var gL := 9.3; var cW := 6.62
	var rC := [29.6, 40.9, 52.2]

	# Straight number bets 1-36
	for c in range(12):
		for r in range(3):
			var v: int
			if r == 0:   v = 3 * (c + 1)
			elif r == 1: v = 3 * c + 2
			else:        v = 3 * c + 1
			Z.append(["straight_%d" % v, gL + (c + 0.5) * cW, rC[r], 6.2, 10.6, [v], 35])

	# Zero
	Z.append(["straight_0", 6.0, 40.9, 5.0, 33.0, [0], 35])

	# Column bets (right side) — keys must match CardManager: col1=%3==1, col2=%3==2, col3=%3==0
	# rC[0]=top row (3,6,9,...,36 = %3==0) → col3
	# rC[1]=mid row (2,5,8,...,35 = %3==2) → col2
	# rC[2]=bot row (1,4,7,...,34 = %3==1) → col1
	var col_nums := [
		[3,6,9,12,15,18,21,24,27,30,33,36],  # top row → col3
		[2,5,8,11,14,17,20,23,26,29,32,35],  # mid row → col2
		[1,4,7,10,13,16,19,22,25,28,31,34],  # bot row → col1
	]
	var col_keys := ["col3", "col2", "col1"]
	for r in range(3):
		Z.append([col_keys[r], 92.4, rC[r], 6.0, 10.6, col_nums[r], 2])

	# Dozen bets
	var dozen_ranges := [[1,12],[13,24],[25,36]]
	var dcx := [22.6, 49.1, 75.6]
	var dozen_keys := ["dozen1", "dozen2", "dozen3"]
	for i in range(3):
		var ns := []
		for k in range(dozen_ranges[i][0], dozen_ranges[i][1] + 1):
			ns.append(k)
		Z.append([dozen_keys[i], dcx[i], 63.4, 25.0, 9.0, ns, 2])

	# Even-chance bets (keys match CardManager: low, red, black, even, odd, high)
	var red_set := Constants.RED_NUMBERS
	var all := []
	for k in range(1, 37):
		all.append(k)
	var low  := all.filter(func(n): return n <= 18)
	var high := all.filter(func(n): return n >= 19)
	var reds  := all.filter(func(n): return n in red_set)
	var blk   := all.filter(func(n): return not (n in red_set))
	var evens := all.filter(func(n): return n % 2 == 0)
	var odds  := all.filter(func(n): return n % 2 == 1)
	var ec_groups := [
		["low",   low],  ["red",  reds], ["black", blk],
		["even", evens], ["odd",  odds], ["high",  high],
	]
	var ecx := [15.9, 29.2, 42.4, 55.6, 68.9, 82.1]
	for i in range(6):
		Z.append([ec_groups[i][0], ecx[i], 74.0, 12.6, 9.0, ec_groups[i][1], 1])

	return Z

var _zones: Array = []

func _ready() -> void:
	custom_minimum_size = Vector2(1080, TABLE_H)
	_zones = _make_zones()
	_full_layer = Control.new()
	_full_layer.position = Vector2.ZERO
	_full_layer.size = Vector2(TABLE_W, TABLE_H)
	add_child(_full_layer)
	_build()
	_build_simple_layer()

func set_simple_mode(simple: bool) -> void:
	_simple_layer.visible = simple
	_full_layer.visible = not simple

func is_simple_mode() -> bool:
	return _simple_layer.visible

func _build() -> void:
	# Table image background — image is 1080×600, fills full width with no margin
	var margin := 0.0
	var table_img := TextureRect.new()
	if ResourceLoader.exists("res://assets/layout/bet_layout.png"):
		table_img.texture = load("res://assets/layout/bet_layout.png")
	table_img.stretch_mode = TextureRect.STRETCH_SCALE
	table_img.position = Vector2(0, 0)
	table_img.size = Vector2(TABLE_W, TABLE_H)
	table_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_full_layer.add_child(table_img)

	# Win-glow layer (behind chips, above table)
	for zone in _zones:
		var key: String = zone[0]
		var glow := ColorRect.new()
		glow.color = Color(0.788, 0.659, 0.298, 0.0)
		glow.position = _zone_pos(zone, margin)
		glow.size = _zone_size(zone)
		glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
		_full_layer.add_child(glow)
		_win_glows[key] = glow

	# Hotspot buttons (transparent, interactive)
	for zone in _zones:
		var key: String = zone[0]
		var btn := Button.new()
		btn.position = _zone_pos(zone, margin)
		btn.size = _zone_size(zone)
		btn.flat = true
		btn.text = ""
		btn.focus_mode = Control.FOCUS_NONE

		var sn := StyleBoxFlat.new()
		sn.bg_color = Color(0, 0, 0, 0)
		btn.add_theme_stylebox_override("normal", sn)
		btn.add_theme_stylebox_override("focus",  sn)

		var sh := StyleBoxFlat.new()
		sh.bg_color = Color(0.788, 0.659, 0.298, 0.22)
		sh.border_color = Color(0.788, 0.659, 0.298, 0.8)
		sh.set_border_width_all(2)
		sh.set_corner_radius_all(3)
		btn.add_theme_stylebox_override("hover",   sh)
		btn.add_theme_stylebox_override("pressed", sh)

		btn.pressed.connect(_on_zone_pressed.bind(key))
		_full_layer.add_child(btn)

		# Chip sprite per zone
		var chip := TextureRect.new()
		if ResourceLoader.exists("res://assets/layout/chip_default.png"):
			chip.texture = load("res://assets/layout/chip_default.png")
		chip.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		chip.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		var zs := _zone_size(zone)
		var cs := min(min(zs.x, zs.y) * 0.7, 56.0)
		chip.size = Vector2(cs, cs)
		chip.position = Vector2((zs.x - cs) / 2.0, (zs.y - cs) / 2.0)
		chip.mouse_filter = Control.MOUSE_FILTER_IGNORE
		chip.hide()
		btn.add_child(chip)
		_chip_sprites[key] = chip

		# Amount label — outlined so it reads over any zone colour
		var lbl := Label.new()
		lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
		lbl.add_theme_color_override("font_color", Color.WHITE)
		lbl.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
		lbl.add_theme_constant_override("outline_size", 8)
		lbl.add_theme_font_size_override("font_size", 22)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		lbl.hide()
		btn.add_child(lbl)
		_chip_labels[key] = lbl

# ── Easy-bets layer ──────────────────────────────────────────────────────────
func _build_simple_layer() -> void:
	_simple_layer = Control.new()
	_simple_layer.position = Vector2.ZERO
	_simple_layer.size = Vector2(TABLE_W, TABLE_H)
	_simple_layer.hide()
	add_child(_simple_layer)

	var vbox := VBoxContainer.new()
	vbox.position = Vector2(40, 4)
	vbox.size = Vector2(TABLE_W - 80, TABLE_H - 8)
	vbox.add_theme_constant_override("separation", 12)
	_simple_layer.add_child(vbox)

	var red_col   := Color(0.545, 0.086, 0.086)
	var black_col := Color(0.16, 0.15, 0.15)
	var gold_dim  := Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.55)

	vbox.add_child(_simple_row([
		_simple_btn("red",   "RED",        "pays 1:1", red_col),
		_simple_btn("black", "BLACK",      "pays 1:1", black_col),
	]))
	vbox.add_child(_simple_row([
		_simple_btn("odd",   "ODD",        "pays 1:1", gold_dim),
		_simple_btn("even",  "EVEN",       "pays 1:1", gold_dim),
	]))
	vbox.add_child(_simple_row([
		_simple_btn("low",   "LOW 1-18",   "pays 1:1", gold_dim),
		_simple_btn("high",  "HIGH 19-36", "pays 1:1", gold_dim),
	]))
	vbox.add_child(_simple_row([
		_simple_btn("dozen1", "1ST 12", "pays 2:1", gold_dim),
		_simple_btn("dozen2", "2ND 12", "pays 2:1", gold_dim),
		_simple_btn("dozen3", "3RD 12", "pays 2:1", gold_dim),
	]))
	_lucky_btn = _simple_btn("lucky", "LUCKY NUMBER", "tap to draw · pays 35:1", Constants.COLOR_GOLD)
	vbox.add_child(_simple_row([_lucky_btn]))

func _simple_row(buttons: Array) -> HBoxContainer:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	row.custom_minimum_size = Vector2(0, 102)
	for btn in buttons:
		row.add_child(btn)
	return row

func _simple_btn(ui_key: String, title: String, subtitle: String, accent: Color) -> Button:
	var btn := Button.new()
	btn.text = "%s\n%s" % [title, subtitle]
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.custom_minimum_size = Vector2(0, 102)
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 28)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

	var s := StyleBoxFlat.new()
	s.bg_color = Color(0.07, 0.03, 0.03, 0.92)
	s.border_color = accent
	s.set_border_width_all(2)
	s.set_corner_radius_all(12)
	btn.add_theme_stylebox_override("normal", s)
	btn.add_theme_stylebox_override("focus", s)
	var sh := s.duplicate() as StyleBoxFlat
	sh.bg_color = sh.bg_color.lightened(0.12)
	btn.add_theme_stylebox_override("hover", sh)
	btn.add_theme_stylebox_override("pressed", sh)

	if ui_key == "lucky":
		btn.pressed.connect(_on_lucky_pressed)
	else:
		btn.pressed.connect(_on_zone_pressed.bind(ui_key))

	# Staked-amount badge, right-hand side of the button
	var amt := Label.new()
	amt.anchor_left = 1.0
	amt.anchor_top = 0.0
	amt.anchor_right = 1.0
	amt.anchor_bottom = 1.0
	amt.offset_left = -110.0
	amt.offset_right = -18.0
	amt.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	amt.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	amt.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	amt.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
	amt.add_theme_constant_override("outline_size", 6)
	amt.add_theme_font_size_override("font_size", 26)
	amt.mouse_filter = Control.MOUSE_FILTER_IGNORE
	amt.hide()
	btn.add_child(amt)
	_simple_labels[ui_key] = amt

	return btn

func _on_lucky_pressed() -> void:
	# First tap draws this spin's lucky number, further taps raise the stake
	if _lucky_number < 0:
		_lucky_number = randi() % 37
	_on_zone_pressed("straight_%d" % _lucky_number)

func _refresh_simple_visuals() -> void:
	for ui_key in _simple_labels:
		var bet_key: String = ui_key
		if ui_key == "lucky":
			if _lucky_number < 0:
				_simple_labels[ui_key].hide()
				continue
			bet_key = "straight_%d" % _lucky_number
		var amt: int = bets.get(bet_key, 0)
		var lbl: Label = _simple_labels[ui_key]
		lbl.text = str(amt)
		lbl.visible = amt > 0
	if _lucky_btn:
		if _lucky_number >= 0:
			_lucky_btn.text = "LUCKY %d\npays 35:1" % _lucky_number
		else:
			_lucky_btn.text = "LUCKY NUMBER\ntap to draw · pays 35:1"

func _zone_pos(zone: Array, margin: float) -> Vector2:
	var cx: float = zone[1]; var cy: float = zone[2]
	var w: float  = zone[3]; var h: float  = zone[4]
	return Vector2(
		margin + (cx / 100.0) * TABLE_W - (w / 100.0) * TABLE_W / 2.0,
		(cy / 100.0) * TABLE_H - (h / 100.0) * TABLE_H / 2.0
	)

func _zone_size(zone: Array) -> Vector2:
	var w: float = zone[3]; var h: float = zone[4]
	return Vector2((w / 100.0) * TABLE_W, (h / 100.0) * TABLE_H)

func _on_zone_pressed(key: String) -> void:
	# Never let the total stake exceed the player's chips
	if get_total_bet() + _chip_amount > GameManager.chips:
		emit_signal("bet_rejected")
		return
	if AudioManager.has_method("play_chip"):
		AudioManager.play_chip()
	bets[key] = bets.get(key, 0) + _chip_amount

	var lbl: Label = _chip_labels.get(key)
	if lbl:
		lbl.text = str(bets[key])
		lbl.show()
	var chip: TextureRect = _chip_sprites.get(key)
	if chip:
		chip.show()
	# Persistent gold tint so staked zones stay obvious at a glance
	var glow: ColorRect = _win_glows.get(key)
	if glow:
		glow.color = Color(0.788, 0.659, 0.298, 0.18)
	_refresh_simple_visuals()
	emit_signal("bet_placed", key, bets[key])

func set_chip_amount(amount: int) -> void:
	_chip_amount = clamp(amount, 1, Constants.MAX_BET)

func get_chip_amount() -> int:
	return _chip_amount

func get_total_bet() -> int:
	var total := 0
	for key in bets:
		total += int(bets[key])
	return total

func get_bets() -> Dictionary:
	return bets.duplicate()

# Re-place a previously captured set of bets (REBET & SPIN)
func restore_bets(saved: Dictionary) -> void:
	clear_bets()
	for key in saved:
		bets[key] = int(saved[key])
		if key.begins_with("straight_") and _lucky_number < 0:
			_lucky_number = int(key.trim_prefix("straight_"))
		var lbl: Label = _chip_labels.get(key)
		if lbl:
			lbl.text = str(bets[key])
			lbl.show()
		var chip: TextureRect = _chip_sprites.get(key)
		if chip:
			chip.show()
		var glow: ColorRect = _win_glows.get(key)
		if glow:
			glow.color = Color(0.788, 0.659, 0.298, 0.18)
	_refresh_simple_visuals()
	emit_signal("bet_placed", "", get_total_bet())

func clear_bets() -> void:
	bets.clear()
	_lucky_number = -1
	for key in _chip_labels:
		_chip_labels[key].hide()
		_chip_labels[key].text = ""
	for key in _chip_sprites:
		_chip_sprites[key].hide()
	for key in _win_glows:
		_win_glows[key].color = Color(0.788, 0.659, 0.298, 0.0)
	_refresh_simple_visuals()
	emit_signal("bets_cleared")

func show_win_zones(result_number: int) -> void:
	for zone in _zones:
		var key: String = zone[0]
		var numbers: Array = zone[5]
		if result_number in numbers:
			var glow: ColorRect = _win_glows.get(key)
			if glow:
				glow.color = Color(0.788, 0.659, 0.298, 0.35)
				var tw := create_tween().set_loops(3)
				tw.tween_property(glow, "color:a", 0.55, 0.35)
				tw.tween_property(glow, "color:a", 0.15, 0.35)

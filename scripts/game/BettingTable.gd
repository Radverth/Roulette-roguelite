class_name BettingTable
extends Control

signal bet_placed(key: String, amount: int)
signal bets_cleared()

var bets: Dictionary = {}
var _chip_amount: int = Constants.DEFAULT_CHIP
var _chip_labels: Dictionary = {}
var _chip_sprites: Dictionary = {}
var _win_glows: Dictionary = {}

# Table display dimensions (natural aspect ratio of the betting layout image)
const TABLE_W := 1040.0
const TABLE_H := 575.0

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
		var ns := []; for k in range(dozen_ranges[i][0], dozen_ranges[i][1]+1): ns.append(k)
		Z.append([dozen_keys[i], dcx[i], 63.4, 25.0, 9.0, ns, 2])

	# Even-chance bets (keys match CardManager: low, red, black, even, odd, high)
	var red_set := Constants.RED_NUMBERS
	var all := []; for k in range(1,37): all.append(k)
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
	custom_minimum_size = Vector2(1080, TABLE_H + 10)
	_zones = _make_zones()
	_build()

func _build() -> void:
	# Table image background
	var margin := (1080.0 - TABLE_W) / 2.0
	var table_img := TextureRect.new()
	if ResourceLoader.exists("res://assets/layout/bet_layout.png"):
		table_img.texture = load("res://assets/layout/bet_layout.png")
	table_img.stretch_mode = TextureRect.STRETCH_SCALE
	table_img.position = Vector2(margin, 5)
	table_img.size = Vector2(TABLE_W, TABLE_H)
	table_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(table_img)

	# Win-glow layer (behind chips, above table)
	for zone in _zones:
		var key: String = zone[0]
		var glow := ColorRect.new()
		glow.color = Color(0.788, 0.659, 0.298, 0.0)
		glow.position = _zone_pos(zone, margin)
		glow.size = _zone_size(zone)
		glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
		add_child(glow)
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
		add_child(btn)

		# Chip sprite per zone
		var chip := TextureRect.new()
		if ResourceLoader.exists("res://assets/layout/chip_default.png"):
			chip.texture = load("res://assets/layout/chip_default.png")
		chip.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		var zs := _zone_size(zone)
		var cs := min(min(zs.x, zs.y) * 0.7, 56.0)
		chip.size = Vector2(cs, cs)
		chip.position = Vector2((zs.x - cs) / 2.0, (zs.y - cs) / 2.0)
		chip.mouse_filter = Control.MOUSE_FILTER_IGNORE
		chip.hide()
		btn.add_child(chip)
		_chip_sprites[key] = chip

		# Amount label
		var lbl := Label.new()
		lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
		lbl.add_theme_color_override("font_color", Color.WHITE)
		lbl.add_theme_font_size_override("font_size", 16)
		lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
		lbl.hide()
		btn.add_child(lbl)
		_chip_labels[key] = lbl

func _zone_pos(zone: Array, margin: float) -> Vector2:
	var cx: float = zone[1]; var cy: float = zone[2]
	var w: float  = zone[3]; var h: float  = zone[4]
	return Vector2(
		margin + (cx / 100.0) * TABLE_W - (w / 100.0) * TABLE_W / 2.0,
		5.0    + (cy / 100.0) * TABLE_H - (h / 100.0) * TABLE_H / 2.0
	)

func _zone_size(zone: Array) -> Vector2:
	var w: float = zone[3]; var h: float = zone[4]
	return Vector2((w / 100.0) * TABLE_W, (h / 100.0) * TABLE_H)

func _on_zone_pressed(key: String) -> void:
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

func clear_bets() -> void:
	bets.clear()
	for key in _chip_labels:
		_chip_labels[key].hide()
		_chip_labels[key].text = ""
	for key in _chip_sprites:
		_chip_sprites[key].hide()
	for key in _win_glows:
		_win_glows[key].color = Color(0.788, 0.659, 0.298, 0.0)
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

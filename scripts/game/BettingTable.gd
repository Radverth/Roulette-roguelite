class_name BettingTable
extends Control

signal bet_placed(key: String, amount: int)
signal bets_cleared()

var bets: Dictionary = {}
var _bet_labels: Dictionary = {}
var _chip_amount: int = Constants.DEFAULT_BET

const LAYOUT_W := 1040.0
const LAYOUT_H := 380.0
const MARGIN_X := 20.0
const MARGIN_Y := 10.0
const ZERO_W := 55.0
const COL_W_NUM := 75.0
const COL_SIDE := 38.0
const ROW_H_NUM := 95.0
const ROW_H_OUT := 65.0

func _ready() -> void:
	custom_minimum_size = Vector2(1080, 420)
	_build()

func _build() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.08, 0.01, 0.01)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var layout_bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/layout/bet_layout.png"):
		layout_bg.texture = load("res://assets/layout/bet_layout.png")
	layout_bg.stretch_mode = TextureRect.STRETCH_SCALE
	layout_bg.position = Vector2(MARGIN_X, MARGIN_Y)
	layout_bg.size = Vector2(LAYOUT_W, LAYOUT_H)
	layout_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(layout_bg)

	_add_zone("straight_0", "0", MARGIN_X, MARGIN_Y, ZERO_W, ROW_H_NUM * 3.0)

	for col in range(12):
		var x := MARGIN_X + ZERO_W + col * COL_W_NUM
		for row in range(3):
			var num := col * 3 + (3 - row)
			var y := MARGIN_Y + row * ROW_H_NUM
			_add_zone("straight_%d" % num, str(num), x, y, COL_W_NUM, ROW_H_NUM, _number_color(num))

	var col_x := MARGIN_X + ZERO_W + 12 * COL_W_NUM
	_add_zone("col1", "2:1", col_x, MARGIN_Y + ROW_H_NUM * 2.0, COL_SIDE, ROW_H_NUM, Color(0.3, 0.3, 0.3, 0.5))
	_add_zone("col2", "2:1", col_x, MARGIN_Y + ROW_H_NUM * 1.0, COL_SIDE, ROW_H_NUM, Color(0.3, 0.3, 0.3, 0.5))
	_add_zone("col3", "2:1", col_x, MARGIN_Y, COL_SIDE, ROW_H_NUM, Color(0.3, 0.3, 0.3, 0.5))

	var dozen_y := MARGIN_Y + ROW_H_NUM * 3.0
	var dozen_w := COL_W_NUM * 4.0
	_add_zone("dozen1", "1st 12", MARGIN_X + ZERO_W, dozen_y, dozen_w, ROW_H_OUT, Color(0.2, 0.2, 0.4, 0.5))
	_add_zone("dozen2", "2nd 12", MARGIN_X + ZERO_W + dozen_w, dozen_y, dozen_w, ROW_H_OUT, Color(0.2, 0.2, 0.4, 0.5))
	_add_zone("dozen3", "3rd 12", MARGIN_X + ZERO_W + dozen_w * 2.0, dozen_y, dozen_w, ROW_H_OUT, Color(0.2, 0.2, 0.4, 0.5))

	var even_y := MARGIN_Y + ROW_H_NUM * 3.0 + ROW_H_OUT
	var even_w := COL_W_NUM * 2.0
	_add_zone("low",   "1-18",  MARGIN_X + ZERO_W,              even_y, even_w, ROW_H_OUT, Color(0.3, 0.3, 0.0, 0.5))
	_add_zone("even",  "EVEN",  MARGIN_X + ZERO_W + even_w,     even_y, even_w, ROW_H_OUT, Color(0.3, 0.3, 0.0, 0.5))
	_add_zone("red",   "RED",   MARGIN_X + ZERO_W + even_w * 2.0, even_y, even_w, ROW_H_OUT, Color(0.6, 0.05, 0.05, 0.5))
	_add_zone("black", "BLACK", MARGIN_X + ZERO_W + even_w * 3.0, even_y, even_w, ROW_H_OUT, Color(0.1, 0.1, 0.1, 0.5))
	_add_zone("odd",   "ODD",   MARGIN_X + ZERO_W + even_w * 4.0, even_y, even_w, ROW_H_OUT, Color(0.3, 0.3, 0.0, 0.5))
	_add_zone("high",  "19-36", MARGIN_X + ZERO_W + even_w * 5.0, even_y, even_w, ROW_H_OUT, Color(0.3, 0.3, 0.0, 0.5))

func _number_color(n: int) -> Color:
	if n == 0:
		return Color(0.1, 0.4, 0.1, 0.4)
	if n in Constants.RED_NUMBERS:
		return Color(0.55, 0.05, 0.05, 0.4)
	return Color(0.08, 0.08, 0.08, 0.4)

func _add_zone(key: String, label_text: String, x: float, y: float, w: float, h: float, col: Color = Color(0.2, 0.2, 0.2, 0.35)) -> void:
	var btn := Button.new()
	btn.position = Vector2(x, y)
	btn.size = Vector2(w, h)
	btn.flat = true
	btn.text = ""

	var style := StyleBoxFlat.new()
	style.bg_color = col
	style.border_color = Constants.COLOR_GOLD
	style.set_border_width_all(1)
	style.set_corner_radius_all(3)
	btn.add_theme_stylebox_override("normal", style)

	var hover_style := style.duplicate() as StyleBoxFlat
	hover_style.bg_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.35)
	btn.add_theme_stylebox_override("hover", hover_style)

	btn.pressed.connect(_on_zone_pressed.bind(key))
	add_child(btn)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	lbl.add_theme_font_size_override("font_size", 13)
	lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	btn.add_child(lbl)

	var bet_lbl := Label.new()
	bet_lbl.name = "BetLbl"
	bet_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	bet_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	bet_lbl.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	bet_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	bet_lbl.add_theme_font_size_override("font_size", 12)
	bet_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bet_lbl.hide()
	btn.add_child(bet_lbl)
	_bet_labels[key] = bet_lbl

func _on_zone_pressed(key: String) -> void:
	AudioManager.play_chip()
	bets[key] = bets.get(key, 0) + _chip_amount
	var lbl: Label = _bet_labels.get(key)
	if lbl:
		lbl.text = str(bets[key])
		lbl.show()
	emit_signal("bet_placed", key, bets[key])

func set_chip_amount(amount: int) -> void:
	_chip_amount = clamp(amount, GameManager.get_effective_min_bet(), Constants.MAX_BET)

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
	for key in _bet_labels:
		var lbl: Label = _bet_labels[key]
		lbl.hide()
		lbl.text = ""
	emit_signal("bets_cleared")

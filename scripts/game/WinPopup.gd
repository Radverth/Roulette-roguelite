class_name WinPopup
extends Panel

signal dismissed()

var _title_lbl: Label
var _amount_lbl: Label
var _pocket_lbl: Label
var _cards_lbl: Label
var _continue_btn: Button

func _ready() -> void:
	anchor_left   = 0.5
	anchor_top    = 0.5
	anchor_right  = 0.5
	anchor_bottom = 0.5
	offset_left   = -380.0
	offset_top    = -260.0
	offset_right  =  380.0
	offset_bottom =  260.0
	_build()

func _build() -> void:
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.05, 0.01, 0.01, 0.97)
	style.border_color = Constants.COLOR_GOLD
	style.set_border_width_all(3)
	style.set_corner_radius_all(14)
	add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left   = 24.0
	vbox.offset_top    = 24.0
	vbox.offset_right  = -24.0
	vbox.offset_bottom = -24.0
	vbox.add_theme_constant_override("separation", 16)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(vbox)

	_title_lbl = Label.new()
	_title_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_lbl.add_theme_font_size_override("font_size", 64)
	vbox.add_child(_title_lbl)

	_amount_lbl = Label.new()
	_amount_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_amount_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_amount_lbl.add_theme_font_size_override("font_size", 46)
	vbox.add_child(_amount_lbl)

	_pocket_lbl = Label.new()
	_pocket_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_pocket_lbl.add_theme_color_override("font_color", Color(0.75, 0.70, 0.60))
	_pocket_lbl.add_theme_font_size_override("font_size", 26)
	vbox.add_child(_pocket_lbl)

	_cards_lbl = Label.new()
	_cards_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_cards_lbl.add_theme_color_override("font_color", Color(0.75, 0.80, 0.60))
	_cards_lbl.add_theme_font_size_override("font_size", 22)
	_cards_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(_cards_lbl)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(0, 36)
	div.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(div)

	_continue_btn = Button.new()
	_continue_btn.text = "CONTINUE"
	_continue_btn.focus_mode = Control.FOCUS_NONE
	_continue_btn.add_theme_font_size_override("font_size", 34)
	_continue_btn.custom_minimum_size = Vector2(300, 80)
	_continue_btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	_continue_btn.pressed.connect(_on_continue)
	_style_btn(_continue_btn)
	vbox.add_child(_continue_btn)

func show_result(number: int, net_gain: int, extra_notes: String = "") -> void:
	if net_gain > 0:
		_title_lbl.text = "WIN!"
		_title_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
		_amount_lbl.text = "+%d chips" % net_gain
	elif net_gain == 0:
		_title_lbl.text = "PUSH"
		_title_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
		_amount_lbl.text = "Bets returned"
	else:
		_title_lbl.text = "LOSS"
		_title_lbl.add_theme_color_override("font_color", Constants.COLOR_CRIMSON)
		_amount_lbl.text = "%d chips" % net_gain

	_pocket_lbl.text = "Ball landed on %s" % _pocket_label(number)
	_cards_lbl.text = extra_notes
	_cards_lbl.visible = not extra_notes.is_empty()
	show()

func _pocket_label(number: int) -> String:
	if number == 0:
		return "0  (Zero)"
	var colour := "Red" if number in Constants.RED_NUMBERS else "Black"
	return "%d  (%s)" % [number, colour]

func _on_continue() -> void:
	hide()
	emit_signal("dismissed")

func _style_btn(btn: Button) -> void:
	for state in ["normal", "hover", "pressed", "focus"]:
		var s := StyleBoxFlat.new()
		s.bg_color = Constants.COLOR_CRIMSON if state == "normal" or state == "focus" else Constants.COLOR_CRIMSON.lightened(0.3)
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(8)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

class_name WinPopup
extends Panel

signal dismissed()

var _title_lbl: Label
var _amount_lbl: Label
var _cards_lbl: Label
var _continue_btn: Button

func _ready() -> void:
	custom_minimum_size = Vector2(700, 400)
	anchor_left = 0.5
	anchor_top = 0.5
	anchor_right = 0.5
	anchor_bottom = 0.5
	offset_left = -350.0
	offset_top = -200.0
	offset_right = 350.0
	offset_bottom = 200.0
	_build()

func _build() -> void:
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.01, 0.01, 0.97)
	style.border_color = Constants.COLOR_GOLD
	style.set_border_width_all(3)
	style.set_corner_radius_all(12)
	add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 20.0
	vbox.offset_top = 20.0
	vbox.offset_right = -20.0
	vbox.offset_bottom = -20.0
	vbox.add_theme_constant_override("separation", 16)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(vbox)

	_title_lbl = Label.new()
	_title_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_lbl.add_theme_font_size_override("font_size", 52)
	vbox.add_child(_title_lbl)

	_amount_lbl = Label.new()
	_amount_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_amount_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	_amount_lbl.add_theme_font_size_override("font_size", 42)
	vbox.add_child(_amount_lbl)

	_cards_lbl = Label.new()
	_cards_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_cards_lbl.add_theme_color_override("font_color", Color(0.8, 0.8, 0.6))
	_cards_lbl.add_theme_font_size_override("font_size", 22)
	_cards_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(_cards_lbl)

	var divider := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		divider.texture = load("res://assets/effects/flame_divider.png")
	divider.stretch_mode = TextureRect.STRETCH_SCALE
	divider.custom_minimum_size = Vector2(0, 40)
	divider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	vbox.add_child(divider)

	_continue_btn = Button.new()
	_continue_btn.text = "CONTINUE"
	_continue_btn.add_theme_font_size_override("font_size", 30)
	_continue_btn.custom_minimum_size = Vector2(260, 70)
	_continue_btn.pressed.connect(_on_continue)
	vbox.add_child(_continue_btn)
	_style_button(_continue_btn)

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

	var pocket_name := _pocket_label(number)
	_cards_lbl.text = "Ball landed: %s%s" % [pocket_name, "\n" + extra_notes if not extra_notes.is_empty() else ""]
	show()

func _pocket_label(number: int) -> String:
	if number == 0:
		return "0 (Zero)"
	var colour := "Red" if number in Constants.RED_NUMBERS else "Black"
	return "%d (%s)" % [number, colour]

func _on_continue() -> void:
	hide()
	emit_signal("dismissed")

func _style_button(btn: Button) -> void:
	var s := StyleBoxFlat.new()
	s.bg_color = Constants.COLOR_CRIMSON
	s.border_color = Constants.COLOR_GOLD
	s.set_border_width_all(2)
	s.set_corner_radius_all(6)
	btn.add_theme_stylebox_override("normal", s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

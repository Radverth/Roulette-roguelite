class_name ModIcon
extends Control

var _card: Dictionary

func _init(card: Dictionary) -> void:
	_card = card

func _ready() -> void:
	custom_minimum_size = Vector2(100, 110)
	_build()

func _build() -> void:
	var rarity: String = _card.get("rarity", "common")
	var rarity_color: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)

	var bg := StyleBoxFlat.new()
	bg.bg_color = Color(0.08, 0.02, 0.02)
	bg.border_color = rarity_color
	bg.set_border_width_all(2)
	bg.set_corner_radius_all(8)

	var panel := Panel.new()
	panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	panel.add_theme_stylebox_override("panel", bg)
	add_child(panel)

	var icon_path: String = _card.get("icon", "")
	if icon_path and ResourceLoader.exists(icon_path):
		var tex := TextureRect.new()
		tex.texture = load(icon_path)
		tex.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		tex.position = Vector2(5, 5)
		tex.size = Vector2(90, 70)
		tex.mouse_filter = Control.MOUSE_FILTER_IGNORE
		panel.add_child(tex)

	var name_lbl := Label.new()
	name_lbl.text = _card.get("name", "?")
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.add_theme_color_override("font_color", rarity_color)
	name_lbl.add_theme_font_size_override("font_size", 13)
	name_lbl.position = Vector2(2, 78)
	name_lbl.size = Vector2(96, 28)
	name_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	panel.add_child(name_lbl)

	tooltip_text = _card.get("desc", "")

func get_card() -> Dictionary:
	return _card

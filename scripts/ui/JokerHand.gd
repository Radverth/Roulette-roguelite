extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()

func _build_ui() -> void:
	# Dark radial gradient background
	var bg := ColorRect.new()
	bg.color = Color(0.086, 0.024, 0.027)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var dark_overlay := ColorRect.new()
	dark_overlay.color = Color(0.0, 0.0, 0.0, 0.45)
	dark_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	dark_overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(dark_overlay)

	# Center VBox
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_theme_constant_override("separation", 20)
	root.offset_left = 30.0
	root.offset_right = -30.0
	add_child(root)

	# "YOUR JOKERS" title
	var title := Label.new()
	title.text = "YOUR JOKERS"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 56)
	root.add_child(title)

	# "{count} / 5 — bound to your fate"
	var count_lbl := Label.new()
	count_lbl.text = "%d / %d — bound to your fate" % [GameManager.owned_cards.size(), Constants.MAX_OWNED_CARDS]
	count_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	count_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.85))
	count_lbl.add_theme_font_size_override("font_size", 28)
	root.add_child(count_lbl)

	# Flame divider 600px
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.custom_minimum_size = Vector2(600, 48)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	# Card grid — wrapping rows of up to 3, centered
	var grid_wrap := Control.new()
	grid_wrap.custom_minimum_size = Vector2(0, 360)
	grid_wrap.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_child(grid_wrap)

	var grid := HFlowContainer.new()
	grid.set_anchors_preset(Control.PRESET_FULL_RECT)
	grid.alignment = FlowContainer.ALIGNMENT_CENTER
	grid.add_theme_constant_override("h_separation", 28)
	grid.add_theme_constant_override("v_separation", 28)
	grid_wrap.add_child(grid)

	var max_slots := Constants.MAX_OWNED_CARDS
	for i in range(max_slots):
		if i < GameManager.owned_cards.size():
			grid.add_child(_build_filled_card(GameManager.owned_cards[i]))
		else:
			grid.add_child(_build_empty_slot())

	# Bottom divider
	var div2 := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div2.texture = load("res://assets/effects/flame_divider.png")
	div2.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div2.custom_minimum_size = Vector2(480, 44)
	div2.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div2.modulate.a = 0.7
	div2.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div2)

	# "Jokers trigger left → right on every spin."
	var hint := Label.new()
	hint.text = "Jokers trigger left → right on every spin."
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	hint.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.7))
	hint.add_theme_font_size_override("font_size", 24)
	root.add_child(hint)

	# Continue button
	var cont_btn := _make_image_btn("CONTINUE", 460, 100)
	cont_btn.pressed.connect(_on_continue)
	root.add_child(cont_btn)

func _build_filled_card(card: Dictionary) -> Control:
	var rarity: String = card.get("rarity", "common")
	var rarity_color: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)

	# 200×280px double-size of prototype 100×140
	var panel := Panel.new()
	panel.custom_minimum_size = Vector2(200, 280)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.01, 0.01, 0.95)
	style.border_color = rarity_color
	style.set_border_width_all(2)
	style.set_corner_radius_all(10)
	panel.add_theme_stylebox_override("panel", style)

	# Template background
	var template_path := "res://assets/cards/card_template_%s.png" % rarity
	if ResourceLoader.exists(template_path):
		var card_bg := TextureRect.new()
		card_bg.texture = load(template_path)
		card_bg.stretch_mode = TextureRect.STRETCH_SCALE
		card_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
		card_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		panel.add_child(card_bg)

	# Rarity tag (top-left)
	var rarity_lbl := Label.new()
	rarity_lbl.text = rarity.to_upper()
	rarity_lbl.add_theme_color_override("font_color", rarity_color)
	rarity_lbl.add_theme_font_size_override("font_size", 14)
	rarity_lbl.position = Vector2(14, 10)
	panel.add_child(rarity_lbl)

	# Icon (centered upper half)
	var icon_path: String = card.get("icon", "")
	if icon_path and ResourceLoader.exists(icon_path):
		var icon := TextureRect.new()
		icon.texture = load(icon_path)
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.custom_minimum_size = Vector2(80, 80)
		icon.position = Vector2(60, 34)
		icon.size = Vector2(80, 80)
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		panel.add_child(icon)

	# Name
	var name_lbl := Label.new()
	name_lbl.text = card.get("name", "?")
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	name_lbl.add_theme_font_size_override("font_size", 18)
	name_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	name_lbl.anchor_left = 0.0; name_lbl.anchor_right = 1.0
	name_lbl.offset_left = 10.0; name_lbl.offset_right = -10.0
	name_lbl.position.y = 128

	panel.add_child(name_lbl)

	# Description
	var desc_lbl := Label.new()
	desc_lbl.text = card.get("desc", "")
	desc_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	desc_lbl.add_theme_color_override("font_color", Color(0.827, 0.769, 0.643))
	desc_lbl.add_theme_font_size_override("font_size", 15)
	desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	desc_lbl.anchor_left = 0.0; desc_lbl.anchor_right = 1.0
	desc_lbl.offset_left = 12.0; desc_lbl.offset_right = -12.0
	desc_lbl.position.y = 168

	panel.add_child(desc_lbl)

	return panel

func _build_empty_slot() -> Control:
	var slot := Panel.new()
	slot.custom_minimum_size = Vector2(200, 280)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0, 0, 0, 0.25)
	style.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.3)
	style.set_border_width_all(3)
	style.set_corner_radius_all(10)
	style.border_blend = false
	slot.add_theme_stylebox_override("panel", style)

	var lbl := Label.new()
	lbl.text = "EMPTY"
	lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.5))
	lbl.add_theme_font_size_override("font_size", 22)
	slot.add_child(lbl)

	return slot

func _make_image_btn(text: String, w: int, h: int) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(w, h)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 36)

	if ResourceLoader.exists("res://assets/ui/btn_normal.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/ui/btn_normal.png")
		btn.add_theme_stylebox_override("normal", sn)
	if ResourceLoader.exists("res://assets/ui/btn_hover.png"):
		var sh := StyleBoxTexture.new()
		sh.texture = load("res://assets/ui/btn_hover.png")
		btn.add_theme_stylebox_override("hover", sh)
		btn.add_theme_stylebox_override("pressed", sh)

	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

func _on_continue() -> void:
	AudioManager.play_ui_click()
	get_tree().change_scene_to_file("res://scenes/Shop.tscn")

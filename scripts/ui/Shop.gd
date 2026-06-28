extends Control

var _offer: Array[Dictionary] = []

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_offer = CardManager.get_shop_offer(3)
	_build_ui()
	var devil := DevilDialogue.new()
	add_child(devil)
	get_tree().create_timer(0.6).timeout.connect(func(): devil.say("shop", 4.0))

func _build_ui() -> void:
	var bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/shop_bg.png"):
		bg.texture = load("res://assets/ui/shop_bg.png")
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var tint := ColorRect.new()
	tint.color = Color(0.0, 0.0, 0.0, 0.55)
	tint.set_anchors_preset(Control.PRESET_FULL_RECT)
	tint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(tint)

	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 32)
	root.offset_left = 40.0
	root.offset_top = 80.0
	root.offset_right = -40.0
	root.offset_bottom = -40.0
	add_child(root)

	var watermark := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		watermark.texture = load("res://assets/effects/devil_watermark.png")
	watermark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	watermark.custom_minimum_size = Vector2(100, 100)
	watermark.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	watermark.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(watermark)

	var title := Label.new()
	title.text = "THE VELVET SHOP"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 62)
	root.add_child(title)

	var boss_info := _boss_label()
	if boss_info:
		root.add_child(boss_info)

	var chips_lbl := Label.new()
	chips_lbl.text = "Your chips: %d" % GameManager.chips
	chips_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	chips_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	chips_lbl.add_theme_font_size_override("font_size", 32)
	root.add_child(chips_lbl)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(0, 44)
	div.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_child(div)

	var cards_grid := HBoxContainer.new()
	cards_grid.alignment = BoxContainer.ALIGNMENT_CENTER
	cards_grid.add_theme_constant_override("separation", 30)
	root.add_child(cards_grid)

	if _offer.is_empty():
		var lbl := Label.new()
		lbl.text = "No cards available.\nYou own everything worth buying!"
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.add_theme_font_size_override("font_size", 28)
		lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
		cards_grid.add_child(lbl)
	else:
		for card in _offer:
			cards_grid.add_child(_build_card_panel(card, chips_lbl))

	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(spacer)

	if not GameManager.owned_cards.is_empty():
		var sell_section := _build_sell_section(chips_lbl)
		root.add_child(sell_section)

	var continue_btn := _make_btn("NEXT FLOOR →", Constants.COLOR_CRIMSON)
	continue_btn.pressed.connect(_on_continue)
	root.add_child(continue_btn)

func _boss_label() -> Control:
	if not GameManager.is_boss_floor:
		return null
	var next_floor := GameManager.floor_number + 1
	if next_floor % Constants.FLOORS_BEFORE_BOSS != 0:
		return null
	var upcoming_boss_idx := randi() % Constants.BOSS_MODIFIERS.size()
	var mod := Constants.BOSS_MODIFIERS[upcoming_boss_idx]
	var lbl := Label.new()
	lbl.text = "⚠  Next floor: BOSS — %s: %s" % [mod.name, mod.desc]
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	lbl.add_theme_font_size_override("font_size", 24)
	return lbl

func _build_card_panel(card: Dictionary, chips_lbl: Label) -> Control:
	var rarity: String = card.get("rarity", "common")
	var rarity_color: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)
	var price := CardManager.get_card_price(card)
	var can_afford := GameManager.chips >= price
	var is_full := GameManager.owned_cards.size() >= Constants.MAX_OWNED_CARDS

	var panel := Panel.new()
	panel.custom_minimum_size = Vector2(280, 480)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.01, 0.01, 0.95)
	style.border_color = rarity_color
	style.set_border_width_all(3)
	style.set_corner_radius_all(12)
	panel.add_theme_stylebox_override("panel", style)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.add_theme_constant_override("separation", 12)
	vbox.offset_left = 16.0
	vbox.offset_top = 16.0
	vbox.offset_right = -16.0
	vbox.offset_bottom = -16.0
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	panel.add_child(vbox)

	var template_path := "res://assets/cards/card_template_%s.png" % rarity
	if ResourceLoader.exists(template_path):
		var card_bg := TextureRect.new()
		card_bg.texture = load(template_path)
		card_bg.stretch_mode = TextureRect.STRETCH_SCALE
		card_bg.custom_minimum_size = Vector2(0, 200)
		card_bg.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		card_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		vbox.add_child(card_bg)

	var icon_path: String = card.get("icon", "")
	if icon_path and ResourceLoader.exists(icon_path):
		var icon := TextureRect.new()
		icon.texture = load(icon_path)
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.custom_minimum_size = Vector2(140, 140)
		icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		vbox.add_child(icon)

	var name_lbl := Label.new()
	name_lbl.text = card.get("name", "?")
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.add_theme_color_override("font_color", rarity_color)
	name_lbl.add_theme_font_size_override("font_size", 26)
	vbox.add_child(name_lbl)

	var rarity_lbl := Label.new()
	rarity_lbl.text = rarity.to_upper()
	rarity_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	rarity_lbl.add_theme_color_override("font_color", rarity_color)
	rarity_lbl.add_theme_font_size_override("font_size", 18)
	vbox.add_child(rarity_lbl)

	var desc_lbl := Label.new()
	desc_lbl.text = card.get("desc", "")
	desc_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	desc_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	desc_lbl.add_theme_font_size_override("font_size", 20)
	desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(desc_lbl)

	var buy_btn := _make_btn("%d chips" % price, can_afford and not is_full ? Constants.COLOR_CRIMSON : Color(0.25, 0.25, 0.25))
	buy_btn.disabled = not can_afford or is_full
	buy_btn.pressed.connect(func():
		if not GameManager.spend_chips(price):
			return
		GameManager.add_card(card)
		buy_btn.disabled = true
		buy_btn.text = "PURCHASED"
		chips_lbl.text = "Your chips: %d" % GameManager.chips
		AudioManager.play_card_pickup()
	)
	vbox.add_child(buy_btn)

	if is_full and not can_afford:
		var full_lbl := Label.new()
		full_lbl.text = "Card slots full"
		full_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		full_lbl.add_theme_color_override("font_color", Color(0.6, 0.4, 0.4))
		full_lbl.add_theme_font_size_override("font_size", 18)
		vbox.add_child(full_lbl)

	return panel

func _build_sell_section(chips_lbl: Label) -> Control:
	var section := VBoxContainer.new()
	section.add_theme_constant_override("separation", 12)

	var lbl := Label.new()
	lbl.text = "— SELL CARDS —"
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	lbl.add_theme_font_size_override("font_size", 26)
	section.add_child(lbl)

	var row := HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	row.add_theme_constant_override("separation", 16)
	section.add_child(row)

	for card in GameManager.owned_cards.duplicate():
		var sell_price := 10 if card.id == "pocket_lint" else CardManager.get_card_price(card) / 3
		var btn := _make_btn("%s\n+%d" % [card.get("name", "?"), sell_price], Color(0.1, 0.1, 0.3))
		btn.custom_minimum_size = Vector2(160, 80)
		btn.add_theme_font_size_override("font_size", 18)
		btn.pressed.connect(func():
			GameManager.remove_card(card.id)
			GameManager.add_chips(sell_price)
			chips_lbl.text = "Your chips: %d" % GameManager.chips
			btn.queue_free()
		)
		row.add_child(btn)

	return section

func _on_continue() -> void:
	AudioManager.play_ui_click()
	if not GameManager.has_card("velvet_hand"):
		GameManager.owned_cards.clear()
		GameManager.emit_signal("cards_changed")
	GameManager.advance_floor()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _make_btn(text: String, color: Color) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(220, 80)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.add_theme_font_size_override("font_size", 28)
	for state in ["normal", "hover", "pressed", "disabled"]:
		var s := StyleBoxFlat.new()
		s.bg_color = color if state != "disabled" else Color(0.2, 0.2, 0.2)
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(8)
		if state == "hover":
			s.bg_color = color.lightened(0.2)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

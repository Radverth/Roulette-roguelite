extends Control

var _offer: Array[Dictionary] = []
var _chips_lbl: Label = null
var _rerolls_left: int = 3
var _reroll_btn: Button = null
var _cards_container: HBoxContainer = null

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_offer = CardManager.get_shop_offer(3)
	_build_ui()
	var devil := DevilDialogue.new()
	add_child(devil)
	get_tree().create_timer(0.6).timeout.connect(func(): devil.say("shop", 4.0))

func _build_ui() -> void:
	# Background
	var bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/shop_bg.png"):
		bg.texture = load("res://assets/ui/shop_bg.png")
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# Dark tint overlay
	var tint := ColorRect.new()
	tint.color = Color(0.0, 0.0, 0.0, 0.45)
	tint.set_anchors_preset(Control.PRESET_FULL_RECT)
	tint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(tint)

	# Scrollable main content
	var scroll := ScrollContainer.new()
	scroll.set_anchors_preset(Control.PRESET_FULL_RECT)
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(scroll)

	var root := VBoxContainer.new()
	root.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_theme_constant_override("separation", 18)
	root.offset_left = 0.0
	scroll.add_child(root)

	# Top padding
	var top_pad := Control.new()
	top_pad.custom_minimum_size = Vector2(0, 120)
	root.add_child(top_pad)

	# "THE VELVET SHOP" title
	var title := Label.new()
	title.text = "THE VELVET SHOP"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 52)
	root.add_child(title)

	# "Spend your sins wisely" subtitle
	var sub := Label.new()
	sub.text = "Spend your sins wisely"
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.85))
	sub.add_theme_font_size_override("font_size", 28)
	root.add_child(sub)

	# YOUR PURSE chip pill
	var purse_row := HBoxContainer.new()
	purse_row.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_child(purse_row)

	var purse_pill := _build_purse_pill()
	purse_row.add_child(purse_pill)

	# Card grid
	_cards_container = HBoxContainer.new()
	_cards_container.alignment = BoxContainer.ALIGNMENT_CENTER
	_cards_container.add_theme_constant_override("separation", 16)
	root.add_child(_cards_container)
	_populate_card_grid()

	# Shop message
	var msg_lbl := Label.new()
	msg_lbl.text = _get_shop_message()
	msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	msg_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.85))
	msg_lbl.add_theme_font_size_override("font_size", 24)
	root.add_child(msg_lbl)

	# Flame divider 300px
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.custom_minimum_size = Vector2(300, 44)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	# Sell section (if player owns cards)
	if not GameManager.owned_cards.is_empty():
		var sell_section := _build_sell_section()
		root.add_child(sell_section)

	# Spacer
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 16)
	root.add_child(spacer)

	# Reroll button
	_reroll_btn = _make_image_btn("REROLL  ·  %d" % _rerolls_left, 460, 100)
	_reroll_btn.pressed.connect(_on_reroll)
	root.add_child(_reroll_btn)

	# LEAVE button
	var leave_btn := _make_border_btn("LEAVE", 320, 80)
	leave_btn.pressed.connect(_on_leave)
	root.add_child(leave_btn)

	# Bottom padding
	var bot_pad := Control.new()
	bot_pad.custom_minimum_size = Vector2(0, 60)
	root.add_child(bot_pad)

func _build_purse_pill() -> Control:
	var pill_bg := Panel.new()
	pill_bg.custom_minimum_size = Vector2(280, 60)
	pill_bg.size_flags_horizontal = Control.SIZE_SHRINK_CENTER

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.039, 0.016, 0.016, 0.55)
	style.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.4)
	style.set_border_width_all(2)
	style.set_corner_radius_all(30)
	pill_bg.add_theme_stylebox_override("panel", style)

	var hbox := HBoxContainer.new()
	hbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	hbox.add_theme_constant_override("separation", 10)
	pill_bg.add_child(hbox)

	var purse_lbl := Label.new()
	purse_lbl.text = "YOUR PURSE"
	purse_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.85))
	purse_lbl.add_theme_font_size_override("font_size", 20)
	hbox.add_child(purse_lbl)

	var chip_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/layout/chip_default.png"):
		chip_icon.texture = load("res://assets/layout/chip_default.png")
	chip_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	chip_icon.custom_minimum_size = Vector2(30, 30)
	chip_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(chip_icon)

	_chips_lbl = Label.new()
	_chips_lbl.text = str(GameManager.chips)
	_chips_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_chips_lbl.add_theme_font_size_override("font_size", 32)
	hbox.add_child(_chips_lbl)

	return pill_bg

func _populate_card_grid() -> void:
	for child in _cards_container.get_children():
		child.queue_free()

	if _offer.is_empty():
		var lbl := Label.new()
		lbl.text = "Nothing for sale.\nYou own it all."
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.add_theme_font_size_override("font_size", 26)
		lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
		_cards_container.add_child(lbl)
	else:
		for card in _offer:
			_cards_container.add_child(_build_card_panel(card))

func _get_shop_message() -> String:
	if GameManager.owned_cards.size() >= Constants.MAX_OWNED_CARDS:
		return "Your hand is full. Sell a Joker to make room."
	if _offer.is_empty():
		return "The parlour has nothing more to offer."
	return ""

func _build_card_panel(card: Dictionary) -> Control:
	var rarity: String = card.get("rarity", "common")
	var rarity_color: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)
	var price := CardManager.get_card_price(card)
	var can_afford := GameManager.chips >= price
	var is_full := GameManager.owned_cards.size() >= Constants.MAX_OWNED_CARDS

	# 208×290px — double the prototype 104×145 to fit portrait phone resolution
	var panel := Panel.new()
	panel.custom_minimum_size = Vector2(208, 290)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.01, 0.01, 0.95)
	style.border_color = rarity_color
	style.set_border_width_all(2)
	style.set_corner_radius_all(10)
	panel.add_theme_stylebox_override("panel", style)

	# Card template background
	var template_path := "res://assets/cards/card_template_%s.png" % rarity
	if ResourceLoader.exists(template_path):
		var card_bg := TextureRect.new()
		card_bg.texture = load(template_path)
		card_bg.stretch_mode = TextureRect.STRETCH_SCALE
		card_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
		card_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		panel.add_child(card_bg)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 12.0; vbox.offset_top = 10.0
	vbox.offset_right = -12.0; vbox.offset_bottom = -12.0
	vbox.add_theme_constant_override("separation", 6)
	panel.add_child(vbox)

	# Rarity tag
	var rarity_lbl := Label.new()
	rarity_lbl.text = rarity.to_upper()
	rarity_lbl.add_theme_color_override("font_color", rarity_color)
	rarity_lbl.add_theme_font_size_override("font_size", 16)
	vbox.add_child(rarity_lbl)

	# Icon
	var icon_path: String = card.get("icon", "")
	if icon_path and ResourceLoader.exists(icon_path):
		var icon := TextureRect.new()
		icon.texture = load(icon_path)
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.custom_minimum_size = Vector2(72, 72)
		icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		vbox.add_child(icon)

	# Name
	var name_lbl := Label.new()
	name_lbl.text = card.get("name", "?")
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	name_lbl.add_theme_font_size_override("font_size", 20)
	name_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(name_lbl)

	# Description
	var desc_lbl := Label.new()
	desc_lbl.text = card.get("desc", "")
	desc_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	desc_lbl.add_theme_color_override("font_color", Color(0.827, 0.769, 0.643))
	desc_lbl.add_theme_font_size_override("font_size", 17)
	desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(desc_lbl)

	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(spacer)

	# Buy / Owned
	if GameManager.has_card(card.get("id", "")):
		var owned_lbl := Label.new()
		owned_lbl.text = "OWNED"
		owned_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		owned_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
		owned_lbl.add_theme_font_size_override("font_size", 18)
		vbox.add_child(owned_lbl)
	else:
		var buy_btn := _make_price_btn(price, can_afford and not is_full)
		buy_btn.pressed.connect(func():
			if not GameManager.spend_chips(price):
				return
			GameManager.add_card(card)
			buy_btn.disabled = true
			buy_btn.text = "PURCHASED"
			if _chips_lbl:
				_chips_lbl.text = str(GameManager.chips)
			AudioManager.play_card_pickup()
		)
		vbox.add_child(buy_btn)

	return panel

func _make_price_btn(price: int, enabled: bool) -> Button:
	var btn := Button.new()
	btn.text = str(price)
	btn.disabled = not enabled
	btn.custom_minimum_size = Vector2(0, 38)
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 20)

	var border_col := Constants.COLOR_GOLD if enabled else Color(0.4, 0.4, 0.4)
	var s := StyleBoxFlat.new()
	s.bg_color = Color(0.545, 0.0, 0.0, 0.4) if enabled else Color(0.2, 0.2, 0.2, 0.4)
	s.border_color = border_col
	s.set_border_width_all(1)
	s.set_corner_radius_all(5)
	btn.add_theme_stylebox_override("normal", s)
	var sh := s.duplicate() as StyleBoxFlat
	sh.bg_color = sh.bg_color.lightened(0.15)
	btn.add_theme_stylebox_override("hover", sh)
	btn.add_theme_stylebox_override("pressed", sh)
	var sd := s.duplicate() as StyleBoxFlat
	sd.bg_color = Color(0.15, 0.15, 0.15, 0.4)
	sd.border_color = Color(0.3, 0.3, 0.3)
	btn.add_theme_stylebox_override("disabled", sd)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

func _build_sell_section(  ) -> Control:
	var section := VBoxContainer.new()
	section.add_theme_constant_override("separation", 12)

	var lbl := Label.new()
	lbl.text = "— YOUR JOKERS  (tap to sell) —"
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	lbl.add_theme_font_size_override("font_size", 26)
	section.add_child(lbl)

	var row := HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	row.add_theme_constant_override("separation", 12)
	section.add_child(row)

	for card in GameManager.owned_cards.duplicate():
		var sell_price := 10 if card.id == "pocket_lint" else CardManager.get_card_price(card) / 3
		var rarity: String = card.get("rarity", "common")
		var rc: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)

		var btn := Button.new()
		btn.text = "%s\n+%d" % [card.get("name", "?"), sell_price]
		btn.custom_minimum_size = Vector2(160, 80)
		btn.add_theme_font_size_override("font_size", 18)
		btn.focus_mode = Control.FOCUS_NONE

		var s := StyleBoxFlat.new()
		s.bg_color = Color(0.06, 0.04, 0.12, 0.9)
		s.border_color = rc
		s.set_border_width_all(2)
		s.set_corner_radius_all(8)
		btn.add_theme_stylebox_override("normal", s)
		var sh := s.duplicate() as StyleBoxFlat
		sh.bg_color = sh.bg_color.lightened(0.2)
		btn.add_theme_stylebox_override("hover", sh)
		btn.add_theme_stylebox_override("pressed", sh)
		btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

		btn.pressed.connect(func():
			GameManager.remove_card(card.id)
			GameManager.add_chips(sell_price)
			if _chips_lbl:
				_chips_lbl.text = str(GameManager.chips)
			btn.queue_free()
		)
		row.add_child(btn)

	return section

func _on_reroll() -> void:
	if _rerolls_left <= 0:
		return
	AudioManager.play_ui_click()
	_rerolls_left -= 1
	_offer = CardManager.get_shop_offer(3)
	_populate_card_grid()
	if _reroll_btn:
		if _rerolls_left > 0:
			_reroll_btn.text = "REROLL  ·  %d" % _rerolls_left
		else:
			_reroll_btn.text = "REROLL  ·  0"
			_reroll_btn.disabled = true

func _on_leave() -> void:
	AudioManager.play_ui_click()
	if not GameManager.has_card("velvet_hand"):
		GameManager.owned_cards.clear()
		GameManager.emit_signal("cards_changed")
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _make_image_btn(text: String, w: int, h: int) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(w, h)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 34)

	if ResourceLoader.exists("res://assets/ui/btn_normal.png"):
		var sn := StyleBoxTexture.new()
		sn.texture = load("res://assets/ui/btn_normal.png")
		btn.add_theme_stylebox_override("normal", sn)
	if ResourceLoader.exists("res://assets/ui/btn_hover.png"):
		var sh := StyleBoxTexture.new()
		sh.texture = load("res://assets/ui/btn_hover.png")
		btn.add_theme_stylebox_override("hover", sh)
		btn.add_theme_stylebox_override("pressed", sh)
	var sd := StyleBoxFlat.new()
	sd.bg_color = Color(0.2, 0.2, 0.2, 0.6)
	sd.set_corner_radius_all(6)
	btn.add_theme_stylebox_override("disabled", sd)

	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

func _make_border_btn(text: String, w: int, h: int) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(w, h)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 28)

	var s := StyleBoxFlat.new()
	s.bg_color = Color(0, 0, 0, 0.3)
	s.border_color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.45)
	s.set_border_width_all(2)
	s.set_corner_radius_all(8)
	btn.add_theme_stylebox_override("normal", s)
	var sh := s.duplicate() as StyleBoxFlat
	sh.bg_color = Color(0, 0, 0, 0.55)
	btn.add_theme_stylebox_override("hover", sh)
	btn.add_theme_stylebox_override("pressed", sh)
	btn.add_theme_stylebox_override("focus", s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

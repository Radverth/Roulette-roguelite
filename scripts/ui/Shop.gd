extends Control

# The Velvet Shop, stick-or-twist edition: three Jokers are drawn for the
# visit but revealed ONE at a time. Buy the card in front of you, or TWIST
# to burn it forever and reveal the next. No going back — the shop itself
# is a gamble.

var _queue: Array[Dictionary] = []
var _idx: int = 0
var _bought: bool = false
var _chips_lbl: Label = null
var _offer_lbl: Label = null
var _msg_lbl: Label = null
var _card_slot: Control = null
var _buy_btn: Button = null
var _twist_btn: Button = null

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_queue = CardManager.get_shop_offer(3)
	_build_ui()
	_show_current(false)
	var devil := DevilDialogue.new()
	add_child(devil)
	get_tree().create_timer(0.6).timeout.connect(func():
		if is_instance_valid(devil):
			devil.say("shop", 4.0)
	)

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
	scroll.add_child(root)

	var top_pad := Control.new()
	top_pad.custom_minimum_size = Vector2(0, 70)
	root.add_child(top_pad)

	var title := Label.new()
	title.text = "THE VELVET SHOP"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 52)
	root.add_child(title)

	var sub := Label.new()
	sub.text = "Stick… or twist?"
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.85))
	sub.add_theme_font_size_override("font_size", 28)
	root.add_child(sub)

	# YOUR PURSE chip pill
	var purse_row := HBoxContainer.new()
	purse_row.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_child(purse_row)
	purse_row.add_child(_build_purse_pill())

	# Offer counter
	_offer_lbl = Label.new()
	_offer_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_offer_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.8))
	_offer_lbl.add_theme_font_size_override("font_size", 24)
	root.add_child(_offer_lbl)

	# Current card slot (one large card, centered)
	_card_slot = Control.new()
	_card_slot.custom_minimum_size = Vector2(0, 620)
	_card_slot.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_child(_card_slot)

	# BUY + TWIST buttons
	var btn_row := HBoxContainer.new()
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_row.add_theme_constant_override("separation", 24)
	root.add_child(btn_row)

	_buy_btn = _make_image_btn("BUY", 420, 110)
	_buy_btn.pressed.connect(_on_buy)
	btn_row.add_child(_buy_btn)

	_twist_btn = _make_border_btn("TWIST", 380, 110)
	_twist_btn.add_theme_font_size_override("font_size", 32)
	_twist_btn.pressed.connect(_on_twist)
	btn_row.add_child(_twist_btn)

	# Status message
	_msg_lbl = Label.new()
	_msg_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_msg_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.85))
	_msg_lbl.add_theme_font_size_override("font_size", 24)
	root.add_child(_msg_lbl)

	# Flame divider
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	div.custom_minimum_size = Vector2(300, 44)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	# Sell section (if player owns cards)
	if not GameManager.owned_cards.is_empty():
		root.add_child(_build_sell_section())

	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 10)
	root.add_child(spacer)

	# LEAVE button
	var leave_btn := _make_border_btn("LEAVE", 320, 80)
	leave_btn.pressed.connect(_on_leave)
	root.add_child(leave_btn)

	var bot_pad := Control.new()
	bot_pad.custom_minimum_size = Vector2(0, 60)
	root.add_child(bot_pad)

# ── Stick or twist ───────────────────────────────────────────────────────────
func _current_card() -> Dictionary:
	if _idx >= 0 and _idx < _queue.size():
		return _queue[_idx]
	return {}

func _show_current(animate: bool = true) -> void:
	for child in _card_slot.get_children():
		child.queue_free()

	if _queue.is_empty():
		var lbl := Label.new()
		lbl.text = "Nothing for sale.\nYou own it all."
		lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		lbl.add_theme_font_size_override("font_size", 28)
		lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
		_card_slot.add_child(lbl)
		_offer_lbl.text = ""
	else:
		var panel := _build_card_panel(_current_card())
		panel.position = Vector2((1080.0 - 460.0) / 2.0, 0)
		_card_slot.add_child(panel)
		_offer_lbl.text = "OFFER %d OF %d" % [_idx + 1, _queue.size()]
		if animate:
			panel.modulate.a = 0.0
			panel.pivot_offset = Vector2(230, 300)
			panel.scale = Vector2(0.85, 0.85)
			var tw := create_tween()
			tw.set_parallel(true)
			tw.tween_property(panel, "modulate:a", 1.0, 0.25)
			tw.tween_property(panel, "scale", Vector2.ONE, 0.3)\
				.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)

	_update_buttons()

func _update_buttons() -> void:
	if _queue.is_empty():
		_buy_btn.hide()
		_twist_btn.hide()
		_msg_lbl.text = "The parlour has nothing more to offer."
		return

	var card := _current_card()
	var price := CardManager.get_card_price(card)
	var is_full := GameManager.owned_cards.size() >= GameManager.get_max_cards()
	var remaining := _queue.size() - 1 - _idx

	if _bought:
		_buy_btn.text = "PURCHASED"
		_buy_btn.disabled = true
		_twist_btn.text = "TWIST"
		_twist_btn.disabled = true
		_msg_lbl.text = "It is yours. The House thanks you."
		return

	_buy_btn.text = "BUY  ·  %d" % price
	_buy_btn.disabled = is_full or GameManager.chips < price
	_twist_btn.text = "TWIST  ·  %d LEFT" % remaining if remaining > 0 else "NO MORE CARDS"
	_twist_btn.disabled = remaining <= 0

	if is_full:
		_msg_lbl.text = "Your hand is full. Sell a Joker to make room."
	elif GameManager.chips < price:
		_msg_lbl.text = "Too poor for this one. Twist, sell… or leave."
	elif remaining > 0:
		_msg_lbl.text = "Twist and this card is gone forever."
	else:
		_msg_lbl.text = "The final offer. Take it or leave it."

func _on_buy() -> void:
	if _bought or _queue.is_empty():
		return
	var card := _current_card()
	var price := CardManager.get_card_price(card)
	if GameManager.owned_cards.size() >= GameManager.get_max_cards():
		return
	if not GameManager.spend_chips(price):
		return
	GameManager.add_card(card)
	_bought = true
	if _chips_lbl:
		_chips_lbl.text = str(GameManager.chips)
	AudioManager.play_card_pickup()
	_update_buttons()

func _on_twist() -> void:
	if _bought or _idx >= _queue.size() - 1:
		return
	AudioManager.play_ui_click()
	_idx += 1
	_show_current()

# ── UI pieces ─────────────────────────────────────────────────────────────────
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
	chip_icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	chip_icon.custom_minimum_size = Vector2(30, 30)
	chip_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hbox.add_child(chip_icon)

	_chips_lbl = Label.new()
	_chips_lbl.text = str(GameManager.chips)
	_chips_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	_chips_lbl.add_theme_font_size_override("font_size", 32)
	hbox.add_child(_chips_lbl)

	return pill_bg

# One large, readable card: 460×600 with roomy padding
func _build_card_panel(card: Dictionary) -> Control:
	var rarity: String = card.get("rarity", "common")
	var rarity_color: Color = Constants.CARD_RARITY_COLORS.get(rarity, Color.WHITE)

	var panel := Panel.new()
	panel.size = Vector2(460, 600)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.06, 0.01, 0.01, 0.95)
	style.border_color = rarity_color
	style.set_border_width_all(3)
	style.set_corner_radius_all(16)
	panel.add_theme_stylebox_override("panel", style)

	# Card template background
	var template_path := "res://assets/cards/card_template_%s.png" % rarity
	if ResourceLoader.exists(template_path):
		var card_bg := TextureRect.new()
		card_bg.texture = load(template_path)
		card_bg.stretch_mode = TextureRect.STRETCH_SCALE
		card_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
		card_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
		card_bg.modulate.a = 0.55
		panel.add_child(card_bg)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 30.0
	vbox.offset_top = 24.0
	vbox.offset_right = -30.0
	vbox.offset_bottom = -24.0
	vbox.add_theme_constant_override("separation", 14)
	panel.add_child(vbox)

	# Rarity tag
	var rarity_lbl := Label.new()
	rarity_lbl.text = rarity.to_upper()
	rarity_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	rarity_lbl.add_theme_color_override("font_color", rarity_color)
	rarity_lbl.add_theme_font_size_override("font_size", 22)
	vbox.add_child(rarity_lbl)

	# Icon, or a big monogram in the rarity colour when no art exists
	var icon_path: String = card.get("icon", "")
	if icon_path and ResourceLoader.exists(icon_path):
		var icon := TextureRect.new()
		icon.texture = load(icon_path)
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		icon.custom_minimum_size = Vector2(0, 160)
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		vbox.add_child(icon)
	else:
		var mono := Label.new()
		mono.text = _monogram(card.get("name", "?"))
		mono.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		mono.custom_minimum_size = Vector2(0, 160)
		mono.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		mono.add_theme_color_override("font_color", rarity_color)
		mono.add_theme_font_size_override("font_size", 96)
		vbox.add_child(mono)

	# Name
	var name_lbl := Label.new()
	name_lbl.text = card.get("name", "?")
	name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	name_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	name_lbl.add_theme_font_size_override("font_size", 36)
	name_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(name_lbl)

	# Description
	var desc_lbl := Label.new()
	desc_lbl.text = card.get("desc", "")
	desc_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	desc_lbl.add_theme_color_override("font_color", Color(0.827, 0.769, 0.643))
	desc_lbl.add_theme_font_size_override("font_size", 26)
	desc_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	vbox.add_child(desc_lbl)

	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(spacer)

	# Price line
	var price_lbl := Label.new()
	price_lbl.text = "COSTS %d CHIPS" % CardManager.get_card_price(card)
	price_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	price_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	price_lbl.add_theme_font_size_override("font_size", 26)
	vbox.add_child(price_lbl)

	return panel

func _monogram(card_name: String) -> String:
	var letters := ""
	for word in card_name.split(" ", false):
		letters += word.substr(0, 1).to_upper()
		if letters.length() >= 2:
			break
	return letters

func _build_sell_section() -> Control:
	var section := VBoxContainer.new()
	section.add_theme_constant_override("separation", 12)

	var lbl := Label.new()
	lbl.text = "— YOUR JOKERS  (tap to sell) —"
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	lbl.add_theme_font_size_override("font_size", 26)
	section.add_child(lbl)

	var row := HFlowContainer.new()
	row.alignment = FlowContainer.ALIGNMENT_CENTER
	row.add_theme_constant_override("h_separation", 12)
	row.add_theme_constant_override("v_separation", 12)
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
		btn.add_theme_stylebox_override("focus", s)
		btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)

		btn.pressed.connect(func():
			GameManager.remove_card(card.id)
			GameManager.add_chips(sell_price)
			if _chips_lbl:
				_chips_lbl.text = str(GameManager.chips)
			btn.queue_free()
			# Freed chips/slots may re-enable the buy button
			_update_buttons()
		)
		row.add_child(btn)

	return section

func _on_leave() -> void:
	AudioManager.play_ui_click()
	# Jokers persist for the whole run — they are only lost by selling them.
	if GameManager.game_active:
		get_tree().change_scene_to_file("res://scenes/Game.tscn")
	else:
		get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")

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
		btn.add_theme_stylebox_override("focus", sn)
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
	var sd := s.duplicate() as StyleBoxFlat
	sd.bg_color = Color(0.1, 0.1, 0.1, 0.4)
	sd.border_color = Color(0.35, 0.35, 0.35, 0.5)
	btn.add_theme_stylebox_override("disabled", sd)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()

func _build_ui() -> void:
	# Dark red radial gradient background
	var bg := ColorRect.new()
	bg.color = Color(0.05, 0.01, 0.01)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	# Pulsing amber glow circle (simulated with semi-transparent rect centered)
	var glow := ColorRect.new()
	glow.color = Color(1.0, 0.42, 0.21, 0.18)
	glow.custom_minimum_size = Vector2(680, 680)
	glow.position = Vector2((1080 - 680) / 2.0, 90)
	glow.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(glow)

	# "BOSS BLIND" header
	var header := Label.new()
	header.text = "BOSS BLIND"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	header.add_theme_font_size_override("font_size", 32)
	header.anchor_left   = 0.0
	header.anchor_top    = 0.0
	header.anchor_right  = 1.0
	header.anchor_bottom = 0.0
	header.offset_top    = 80.0
	header.offset_bottom = 140.0
	add_child(header)

	# Boss card frame — 600×900px centered
	var frame_wrap := Control.new()
	frame_wrap.anchor_left   = 0.5
	frame_wrap.anchor_top    = 0.0
	frame_wrap.anchor_right  = 0.5
	frame_wrap.anchor_bottom = 0.0
	frame_wrap.offset_left   = -300.0
	frame_wrap.offset_top    = 160.0
	frame_wrap.offset_right  = 300.0
	frame_wrap.offset_bottom = 1060.0
	add_child(frame_wrap)

	if ResourceLoader.exists("res://assets/ui/boss_card_frame.png"):
		var frame_img := TextureRect.new()
		frame_img.texture = load("res://assets/ui/boss_card_frame.png")
		frame_img.stretch_mode = TextureRect.STRETCH_SCALE
		frame_img.set_anchors_preset(Control.PRESET_FULL_RECT)
		frame_img.mouse_filter = Control.MOUSE_FILTER_IGNORE
		frame_wrap.add_child(frame_img)

	# Content inside card frame
	var card_content := VBoxContainer.new()
	card_content.anchor_left   = 0.0
	card_content.anchor_top    = 0.0
	card_content.anchor_right  = 1.0
	card_content.anchor_bottom = 1.0
	card_content.offset_left   = 48.0
	card_content.offset_top    = 140.0
	card_content.offset_right  = -48.0
	card_content.offset_bottom = -60.0
	card_content.alignment = BoxContainer.ALIGNMENT_CENTER
	card_content.add_theme_constant_override("separation", 22)
	frame_wrap.add_child(card_content)

	# "The House Awakens" sub-label
	var awakens_lbl := Label.new()
	awakens_lbl.text = "THE HOUSE AWAKENS"
	awakens_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	awakens_lbl.add_theme_color_override("font_color", Color(1.0, 0.42, 0.21))
	awakens_lbl.add_theme_font_size_override("font_size", 24)
	card_content.add_child(awakens_lbl)

	# Devil icon
	var devil_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		devil_icon.texture = load("res://assets/effects/devil_watermark.png")
	devil_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	devil_icon.custom_minimum_size = Vector2(164, 164)
	devil_icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card_content.add_child(devil_icon)

	# Boss name
	var boss_modifier := _get_boss_modifier()
	var boss_name_lbl := Label.new()
	boss_name_lbl.text = boss_modifier.get("name", "THE COLLECTOR")
	boss_name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	boss_name_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	boss_name_lbl.add_theme_font_size_override("font_size", 44)
	card_content.add_child(boss_name_lbl)

	# Flame divider 340px
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.custom_minimum_size = Vector2(340, 44)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card_content.add_child(div)

	# "MODIFIER" label
	var mod_tag := Label.new()
	mod_tag.text = "MODIFIER"
	mod_tag.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mod_tag.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.7))
	mod_tag.add_theme_font_size_override("font_size", 22)
	card_content.add_child(mod_tag)

	# Boss modifier description
	var mod_desc := Label.new()
	mod_desc.text = boss_modifier.get("desc", "")
	mod_desc.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	mod_desc.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	mod_desc.add_theme_font_size_override("font_size", 28)
	mod_desc.autowrap_mode = TextServer.AUTOWRAP_WORD
	card_content.add_child(mod_desc)

	# Taunt box
	var taunt_text := _get_boss_taunt(boss_modifier.get("id", ""))
	if taunt_text != "":
		var taunt_panel := Panel.new()
		taunt_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var ts := StyleBoxFlat.new()
		ts.bg_color = Color(0.031, 0.012, 0.012, 0.55)
		ts.border_color = Color(1.0, 0.42, 0.21, 0.32)
		ts.set_border_width_all(2)
		ts.set_corner_radius_all(9)
		taunt_panel.add_theme_stylebox_override("panel", ts)
		card_content.add_child(taunt_panel)

		var taunt_lbl := Label.new()
		taunt_lbl.text = '"%s"' % taunt_text
		taunt_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		taunt_lbl.add_theme_color_override("font_color", Color(1.0, 0.42, 0.21))
		taunt_lbl.add_theme_font_size_override("font_size", 24)
		taunt_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
		taunt_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
		taunt_lbl.offset_left = 16.0; taunt_lbl.offset_top = 14.0
		taunt_lbl.offset_right = -16.0; taunt_lbl.offset_bottom = -14.0
		taunt_panel.add_child(taunt_lbl)
		taunt_panel.custom_minimum_size = Vector2(0, 90)

	# "FACE THE HOUSE" button at bottom
	var btn := _make_image_btn("FACE THE HOUSE", 508, 110)
	btn.anchor_left   = 0.5
	btn.anchor_top    = 1.0
	btn.anchor_right  = 0.5
	btn.anchor_bottom = 1.0
	btn.offset_left   = -254.0
	btn.offset_top    = -200.0
	btn.offset_right  = 254.0
	btn.offset_bottom = -90.0
	btn.pressed.connect(_on_continue)
	add_child(btn)

func _get_boss_modifier() -> Dictionary:
	if GameManager.is_boss_floor and not GameManager.current_boss_modifier.is_empty():
		return GameManager.current_boss_modifier
	# Pick one for this ante
	var idx := (GameManager.ante - 1) % Constants.BOSS_MODIFIERS.size()
	var mod: Dictionary = Constants.BOSS_MODIFIERS[idx]
	GameManager.is_boss_floor = true
	GameManager.current_boss_modifier = mod
	return mod

func _get_boss_taunt(mod_id: String) -> String:
	match mod_id:
		"red_pays_nothing":
			return "Red is the colour of your loss tonight."
		"house_skim":
			return "A tithe for the House. Consider it... obligatory."
		"odds_swap":
			return "The wheel has a sense of humour. Do you?"
	return "The House always wins. Always."

func _make_image_btn(text: String, w: int, h: int) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(w, h)
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
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

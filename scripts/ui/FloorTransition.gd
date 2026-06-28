extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()

func _build_ui() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.02, 0.0, 0.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var watermark := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		watermark.texture = load("res://assets/effects/devil_watermark.png")
	watermark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	watermark.set_anchors_preset(Control.PRESET_FULL_RECT)
	watermark.modulate = Color(0.6, 0.0, 0.0, 0.3)
	watermark.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(watermark)

	var center := VBoxContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	center.add_theme_constant_override("separation", 30)
	add_child(center)

	var ante_lbl := Label.new()
	ante_lbl.text = "ANTE COMPLETE!"
	ante_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	ante_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	ante_lbl.add_theme_font_size_override("font_size", 72)
	ante_lbl.modulate.a = 0.0
	center.add_child(ante_lbl)

	var floor_lbl := Label.new()
	floor_lbl.text = "Ante %s Complete" % Constants.rom(GameManager.ante - 1)
	floor_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	floor_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	floor_lbl.add_theme_font_size_override("font_size", 42)
	floor_lbl.modulate.a = 0.0
	center.add_child(floor_lbl)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(700, 48)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.modulate.a = 0.0
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center.add_child(div)

	# Determine next destination label
	var is_boss := (GameManager.ante % 3 == 0)
	var next_text := "Entering the Boss Blind..." if is_boss else "Entering the Velvet Shop..."
	var entering_lbl := Label.new()
	entering_lbl.text = next_text
	entering_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	entering_lbl.add_theme_color_override("font_color", Color(0.7, 0.6, 0.5))
	entering_lbl.add_theme_font_size_override("font_size", 30)
	entering_lbl.modulate.a = 0.0
	center.add_child(entering_lbl)

	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(ante_lbl,     "modulate:a", 1.0, 0.7)
	tween.tween_property(floor_lbl,    "modulate:a", 1.0, 0.7).set_delay(0.3)
	tween.tween_property(div,          "modulate:a", 1.0, 0.7).set_delay(0.6)
	tween.tween_property(entering_lbl, "modulate:a", 1.0, 0.7).set_delay(0.9)

	var devil := DevilDialogue.new()
	add_child(devil)
	get_tree().create_timer(1.2).timeout.connect(func(): devil.say("floor_complete", 0.0))

	await get_tree().create_timer(2.8).timeout
	var fade := create_tween()
	fade.tween_property(self, "modulate:a", 0.0, 0.5)

	if is_boss:
		# Set up boss modifier for this ante
		var idx := (GameManager.ante - 1) % Constants.BOSS_MODIFIERS.size()
		GameManager.is_boss_floor = true
		GameManager.current_boss_modifier = Constants.BOSS_MODIFIERS[idx]
		fade.tween_callback(func(): get_tree().change_scene_to_file("res://scenes/BossReveal.tscn"))
	else:
		GameManager.is_boss_floor = false
		GameManager.current_boss_modifier = {}
		# Show joker hand review if player has cards, otherwise go straight to shop
		if not GameManager.owned_cards.is_empty():
			fade.tween_callback(func(): get_tree().change_scene_to_file("res://scenes/JokerHand.tscn"))
		else:
			fade.tween_callback(func(): get_tree().change_scene_to_file("res://scenes/Shop.tscn"))

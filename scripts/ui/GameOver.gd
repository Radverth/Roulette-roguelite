extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	GameManager.game_active = false
	var devil := DevilDialogue.new()
	add_child(devil)
	get_tree().create_timer(0.8).timeout.connect(func(): devil.say("game_over", 5.0))

func _build_ui() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.04, 0.0, 0.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var watermark := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		watermark.texture = load("res://assets/effects/devil_watermark.png")
	watermark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	watermark.set_anchors_preset(Control.PRESET_FULL_RECT)
	watermark.modulate = Color(0.3, 0.0, 0.0, 0.4)
	watermark.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(watermark)

	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_theme_constant_override("separation", 36)
	root.offset_left = 60.0
	root.offset_right = -60.0
	add_child(root)

	var boss_frame := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/boss_card_frame.png"):
		boss_frame.texture = load("res://assets/ui/boss_card_frame.png")
	boss_frame.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	boss_frame.custom_minimum_size = Vector2(500, 160)
	boss_frame.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	boss_frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(boss_frame)

	var title := Label.new()
	title.text = "BANKRUPT"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_CRIMSON)
	title.add_theme_font_size_override("font_size", 96)
	root.add_child(title)

	var subtitle := Label.new()
	subtitle.text = "The house always wins."
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	subtitle.add_theme_font_size_override("font_size", 32)
	root.add_child(subtitle)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(0, 48)
	div.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.add_child(div)

	var stats := VBoxContainer.new()
	stats.add_theme_constant_override("separation", 12)
	stats.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	root.add_child(stats)

	_stat_row(stats, "Final Chips", str(GameManager.chips))
	_stat_row(stats, "Ante Reached", Constants.rom(GameManager.ante))
	_stat_row(stats, "Total Spins", str(GameManager.spin_count))
	_stat_row(stats, "Cards Owned", str(GameManager.owned_cards.size()))

	var scores := SaveManager.get_high_scores()
	if not scores.is_empty():
		var best: Dictionary = scores[0]
		_stat_row(stats, "Best Run", "%d chips (Floor %d)" % [best.chips, best.floor])

	var btn_row := HBoxContainer.new()
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_row.add_theme_constant_override("separation", 30)
	root.add_child(btn_row)

	var play_again := _make_btn("PLAY AGAIN", Constants.COLOR_CRIMSON)
	play_again.pressed.connect(_on_play_again)
	btn_row.add_child(play_again)

	var menu_btn := _make_btn("MAIN MENU", Color(0.15, 0.08, 0.05))
	menu_btn.pressed.connect(_on_main_menu)
	btn_row.add_child(menu_btn)

func _stat_row(parent: VBoxContainer, label_text: String, value_text: String) -> void:
	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 20)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.custom_minimum_size = Vector2(280, 0)
	lbl.add_theme_color_override("font_color", Color(0.7, 0.65, 0.55))
	lbl.add_theme_font_size_override("font_size", 28)
	hbox.add_child(lbl)

	var val := Label.new()
	val.text = value_text
	val.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	val.add_theme_font_size_override("font_size", 28)
	hbox.add_child(val)

	parent.add_child(hbox)

func _on_play_again() -> void:
	AudioManager.play_ui_click()
	GameManager.start_new_game()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _on_main_menu() -> void:
	AudioManager.play_ui_click()
	get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")

func _make_btn(text: String, color: Color) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(340, 100)
	btn.add_theme_font_size_override("font_size", 34)
	for state in ["normal", "hover", "pressed"]:
		var s := StyleBoxFlat.new()
		s.bg_color = color if state == "normal" else (color.lightened(0.25) if state == "hover" else color.darkened(0.2))
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(10)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

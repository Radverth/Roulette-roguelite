extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	GameManager.game_active = false

func _build_ui() -> void:
	# Dark red radial gradient bg
	var bg := ColorRect.new()
	bg.color = Color(0.04, 0.0, 0.0)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	# Radial vignette
	var vignette := ColorRect.new()
	vignette.color = Color(0.157, 0.016, 0.016, 0.6)
	vignette.set_anchors_preset(Control.PRESET_FULL_RECT)
	vignette.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(vignette)

	# Center content
	var root := VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_theme_constant_override("separation", 18)
	root.offset_left = 30.0
	root.offset_right = -30.0
	add_child(root)

	# Devil icon
	var devil_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		devil_icon.texture = load("res://assets/effects/devil_watermark.png")
	devil_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	devil_icon.custom_minimum_size = Vector2(84, 84)
	devil_icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(devil_icon)

	# RUINED title
	var title := Label.new()
	title.text = "RUINED"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_CRIMSON)
	title.add_theme_font_size_override("font_size", 96)
	root.add_child(title)

	# Flame divider - 240px wide
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.custom_minimum_size = Vector2(240, 48)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	# Subtitle
	var subtitle := Label.new()
	subtitle.text = "The House has taken everything.\nYour seat at the table grows cold."
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	subtitle.add_theme_font_size_override("font_size", 28)
	subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD
	root.add_child(subtitle)

	# Stats row: REACHED | JOKERS
	var stats_row := HBoxContainer.new()
	stats_row.alignment = BoxContainer.ALIGNMENT_CENTER
	stats_row.add_theme_constant_override("separation", 0)
	stats_row.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	root.add_child(stats_row)

	_add_stat_col(stats_row, "REACHED", "Ante %s" % Constants.rom(GameManager.ante))

	var divider := ColorRect.new()
	divider.color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.3)
	divider.custom_minimum_size = Vector2(2, 60)
	divider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	stats_row.add_child(divider)

	_add_stat_col(stats_row, "JOKERS", str(GameManager.owned_cards.size()))

	# Spacer
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 20)
	root.add_child(spacer)

	# "Start New Run" button using btn_normal.png
	var new_run_btn := _make_image_btn("START NEW RUN", 520, 110)
	new_run_btn.pressed.connect(_on_play_again)
	root.add_child(new_run_btn)

	# "Exit to Main Menu" — bordered secondary button
	var menu_btn := _make_border_btn("EXIT TO MAIN MENU", 420, 86)
	menu_btn.pressed.connect(_on_main_menu)
	root.add_child(menu_btn)

func _add_stat_col(parent: HBoxContainer, label_text: String, value_text: String) -> void:
	var col := VBoxContainer.new()
	col.alignment = BoxContainer.ALIGNMENT_CENTER
	col.add_theme_constant_override("separation", 4)
	col.custom_minimum_size = Vector2(200, 0)
	parent.add_child(col)

	var lbl := Label.new()
	lbl.text = label_text
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.add_theme_color_override("font_color", Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.8))
	lbl.add_theme_font_size_override("font_size", 22)
	col.add_child(lbl)

	var val := Label.new()
	val.text = value_text
	val.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	val.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	val.add_theme_font_size_override("font_size", 38)
	col.add_child(val)

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

func _make_border_btn(text: String, w: int, h: int) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(w, h)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 26)

	var s := StyleBoxFlat.new()
	s.bg_color = Color(0, 0, 0, 0.35)
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

func _on_play_again() -> void:
	AudioManager.play_ui_click()
	GameManager.start_new_game()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _on_main_menu() -> void:
	AudioManager.play_ui_click()
	get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")

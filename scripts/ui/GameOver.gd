extends Control

# Doubles as the victory screen: beating Ante 8 routes here with run_won set,
# offering the choice between banking the win and descending into endless mode.
var _is_victory := false

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_is_victory = GameManager.run_won
	_build_ui()
	if not _is_victory:
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
	devil_icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	devil_icon.custom_minimum_size = Vector2(84, 84)
	devil_icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(devil_icon)

	# RUINED / THE HOUSE FALLS title
	var title := Label.new()
	title.text = "THE HOUSE FALLS" if _is_victory else "RUINED"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD if _is_victory else Constants.COLOR_CRIMSON)
	title.add_theme_font_size_override("font_size", 76 if _is_victory else 96)
	root.add_child(title)

	# Flame divider - 240px wide
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	div.custom_minimum_size = Vector2(240, 48)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(div)

	# Subtitle
	var subtitle := Label.new()
	if _is_victory:
		subtitle.text = "The lake of ice cracks. Circle %s falls, and you climb out\nto see the stars again… or turn, and descend once more." % Constants.rom(Constants.WIN_ANTE)
	elif GameManager.run_failed:
		subtitle.text = "The House demanded %d chips. You fell short.\nYour seat at the table grows cold." % GameManager.target
	else:
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

	var reached_ante := GameManager.ante - 1 if _is_victory else GameManager.ante
	_add_stat_col(stats_row, "CONQUERED" if _is_victory else "REACHED", "Circle %s" % Constants.rom(reached_ante))
	stats_row.add_child(_make_stat_divider())
	_add_stat_col(stats_row, "JOKERS", str(GameManager.owned_cards.size()))

	# Personal best from saved runs
	var best_floor := 0
	for entry in SaveManager.get_high_scores():
		best_floor = maxi(best_floor, int(entry.get("floor", 0)))
	if best_floor > 0:
		stats_row.add_child(_make_stat_divider())
		_add_stat_col(stats_row, "BEST", "Circle %s" % Constants.rom(best_floor))

	# Spacer
	var spacer := Control.new()
	spacer.custom_minimum_size = Vector2(0, 20)
	root.add_child(spacer)

	if _is_victory:
		var endless_btn := _make_image_btn("DESCEND FURTHER", 520, 110)
		endless_btn.pressed.connect(_on_descend_endless)
		root.add_child(endless_btn)

		var glory_btn := _make_border_btn("CLAIM YOUR GLORY", 420, 86)
		glory_btn.pressed.connect(_on_claim_glory)
		root.add_child(glory_btn)
	else:
		# "Start New Run" button using btn_normal.png
		var new_run_btn := _make_image_btn("START NEW RUN", 520, 110)
		new_run_btn.pressed.connect(_on_play_again)
		root.add_child(new_run_btn)

		# "Exit to Main Menu" — bordered secondary button
		var menu_btn := _make_border_btn("EXIT TO MAIN MENU", 420, 86)
		menu_btn.pressed.connect(_on_main_menu)
		root.add_child(menu_btn)

func _make_stat_divider() -> ColorRect:
	var divider := ColorRect.new()
	divider.color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.3)
	divider.custom_minimum_size = Vector2(2, 60)
	divider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	return divider

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

func _on_descend_endless() -> void:
	AudioManager.play_ui_click()
	GameManager.run_won = false
	GameManager.endless_mode = true
	# Continue the run through the usual between-antes flow (shop or boss)
	get_tree().change_scene_to_file("res://scenes/FloorTransition.tscn")

func _on_claim_glory() -> void:
	AudioManager.play_ui_click()
	SaveManager.save_run(GameManager.chips, GameManager.ante - 1)
	GameManager.run_won = false
	GameManager.game_active = false
	get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")

func _on_play_again() -> void:
	AudioManager.play_ui_click()
	GameManager.start_new_game()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _on_main_menu() -> void:
	AudioManager.play_ui_click()
	get_tree().change_scene_to_file("res://scenes/MainMenu.tscn")

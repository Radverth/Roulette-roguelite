extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	UpdateManager.check_for_updates()
	UpdateManager.update_available.connect(_on_update_available)

func _build_ui() -> void:
	var bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/menu_bg.png"):
		bg.texture = load("res://assets/ui/menu_bg.png")
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var bg_tint := ColorRect.new()
	bg_tint.color = Color(0.0, 0.0, 0.0, 0.45)
	bg_tint.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg_tint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg_tint)

	var center := VBoxContainer.new()
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.add_theme_constant_override("separation", 28)
	add_child(center)

	var watermark := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		watermark.texture = load("res://assets/effects/devil_watermark.png")
	watermark.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	watermark.custom_minimum_size = Vector2(240, 240)
	watermark.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	watermark.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center.add_child(watermark)

	var title := Label.new()
	title.text = "VELVET SPIN"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 80)
	center.add_child(title)

	var subtitle := Label.new()
	subtitle.text = "Infernal Roulette Roguelite"
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	subtitle.add_theme_font_size_override("font_size", 30)
	center.add_child(subtitle)

	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_SCALE
	div.custom_minimum_size = Vector2(700, 48)
	div.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	center.add_child(div)

	var new_game_btn := _make_btn("NEW GAME")
	new_game_btn.pressed.connect(_on_new_game)
	center.add_child(new_game_btn)

	var scores_btn := _make_btn("HIGH SCORES")
	scores_btn.pressed.connect(_on_high_scores)
	center.add_child(scores_btn)

	var ver_lbl := Label.new()
	ver_lbl.text = "v" + Constants.APP_VERSION
	ver_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	ver_lbl.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	ver_lbl.add_theme_font_size_override("font_size", 22)
	ver_lbl.position = Vector2(1080 - 180, 1860)
	ver_lbl.size = Vector2(160, 40)
	add_child(ver_lbl)

func _make_btn(text: String) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(500, 100)
	btn.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	btn.add_theme_font_size_override("font_size", 38)

	for state in ["normal", "hover", "pressed"]:
		var s := StyleBoxFlat.new()
		s.border_color = Constants.COLOR_GOLD
		s.set_border_width_all(2)
		s.set_corner_radius_all(10)
		match state:
			"normal":
				s.bg_color = Color(0.18, 0.03, 0.03, 0.88)
			"hover":
				s.bg_color = Color(0.38, 0.06, 0.06, 0.95)
			"pressed":
				s.bg_color = Color(0.08, 0.01, 0.01, 0.98)
		btn.add_theme_stylebox_override(state, s)
	btn.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	return btn

func _on_new_game() -> void:
	AudioManager.play_ui_click()
	GameManager.start_new_game()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _on_high_scores() -> void:
	AudioManager.play_ui_click()
	_show_scores_dialog()

func _show_scores_dialog() -> void:
	var dialog := AcceptDialog.new()
	dialog.title = "High Scores"
	dialog.size = Vector2(600, 700)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 10)
	dialog.add_child(vbox)

	var scores := SaveManager.get_high_scores()
	if scores.is_empty():
		var lbl := Label.new()
		lbl.text = "No scores yet.\nPlay a game first!"
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.add_theme_font_size_override("font_size", 26)
		vbox.add_child(lbl)
	else:
		for i in range(scores.size()):
			var entry: Dictionary = scores[i]
			var lbl := Label.new()
			lbl.text = "%d.  %d chips  —  Floor %d  (%s)" % [i + 1, entry.chips, entry.floor, entry.date]
			lbl.add_theme_font_size_override("font_size", 22)
			vbox.add_child(lbl)

	add_child(dialog)
	dialog.popup_centered()
	dialog.confirmed.connect(dialog.queue_free)

func _on_update_available(latest_version: String, download_url: String) -> void:
	var dialog := ConfirmationDialog.new()
	dialog.title = "Update Available"
	dialog.dialog_text = "Version %s is available!\nWould you like to download it?" % latest_version
	dialog.confirmed.connect(func(): OS.shell_open(download_url))
	dialog.confirmed.connect(dialog.queue_free)
	dialog.canceled.connect(dialog.queue_free)
	add_child(dialog)
	dialog.popup_centered()

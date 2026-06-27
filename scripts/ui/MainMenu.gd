extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	# Delay update check so the dialog never blocks the very first frame
	get_tree().create_timer(2.5).timeout.connect(_start_update_check)

func _start_update_check() -> void:
	UpdateManager.update_available.connect(_on_update_available)
	UpdateManager.check_for_updates()

func _build_ui() -> void:
	# ── Full-screen background ──────────────────────────────────────────────
	var bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/menu_bg.png"):
		bg.texture = load("res://assets/ui/menu_bg.png")
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# Gradient overlay – dark on bottom half so buttons are readable
	var overlay := ColorRect.new()
	overlay.color = Color(0.0, 0.0, 0.0, 0.72)
	overlay.anchor_left   = 0.0
	overlay.anchor_top    = 0.38
	overlay.anchor_right  = 1.0
	overlay.anchor_bottom = 1.0
	overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(overlay)

	# ── Menu content – sits in the bottom 60 % of the screen ───────────────
	var content := VBoxContainer.new()
	content.anchor_left   = 0.0
	content.anchor_top    = 0.40
	content.anchor_right  = 1.0
	content.anchor_bottom = 1.0
	content.offset_left   = 80.0
	content.offset_right  = -80.0
	content.offset_bottom = -100.0
	content.alignment = BoxContainer.ALIGNMENT_CENTER
	content.add_theme_constant_override("separation", 28)
	add_child(content)

	# Title
	var title := Label.new()
	title.text = "VELVET SPIN"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 88)
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	content.add_child(title)

	# Subtitle
	var subtitle := Label.new()
	subtitle.text = "Infernal Roulette Roguelite"
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	subtitle.add_theme_font_size_override("font_size", 28)
	subtitle.mouse_filter = Control.MOUSE_FILTER_IGNORE
	content.add_child(subtitle)

	# Flame divider
	var div := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/flame_divider.png"):
		div.texture = load("res://assets/effects/flame_divider.png")
	div.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	div.custom_minimum_size = Vector2(0, 52)
	div.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	div.mouse_filter = Control.MOUSE_FILTER_IGNORE
	content.add_child(div)

	# Buttons
	var new_game_btn := _make_btn("NEW GAME")
	new_game_btn.pressed.connect(_on_new_game)
	content.add_child(new_game_btn)

	var scores_btn := _make_btn("HIGH SCORES")
	scores_btn.pressed.connect(_on_high_scores)
	content.add_child(scores_btn)

	# ── Version label – bottom-right, anchored ──────────────────────────────
	var ver_lbl := Label.new()
	var ver: String = ProjectSettings.get_setting("application/config/version", "1.0.0")
	ver_lbl.text = "v" + ver
	ver_lbl.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	ver_lbl.add_theme_font_size_override("font_size", 22)
	ver_lbl.anchor_left   = 1.0
	ver_lbl.anchor_top    = 1.0
	ver_lbl.anchor_right  = 1.0
	ver_lbl.anchor_bottom = 1.0
	ver_lbl.offset_left   = -200.0
	ver_lbl.offset_top    = -60.0
	ver_lbl.offset_right  = -20.0
	ver_lbl.offset_bottom = -20.0
	ver_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	ver_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(ver_lbl)

func _make_btn(text: String) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(0, 110)
	btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	btn.focus_mode = Control.FOCUS_NONE
	btn.add_theme_font_size_override("font_size", 42)

	var s_normal := StyleBoxFlat.new()
	s_normal.bg_color = Color(0.18, 0.03, 0.03, 0.92)
	s_normal.border_color = Constants.COLOR_GOLD
	s_normal.set_border_width_all(3)
	s_normal.set_corner_radius_all(8)
	btn.add_theme_stylebox_override("normal", s_normal)

	var s_hover := s_normal.duplicate() as StyleBoxFlat
	s_hover.bg_color = Color(0.40, 0.06, 0.06, 0.97)
	btn.add_theme_stylebox_override("hover", s_hover)
	btn.add_theme_stylebox_override("pressed", s_hover)
	btn.add_theme_stylebox_override("focus", s_normal)

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
	dialog.size = Vector2(700, 720)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 14)
	dialog.add_child(vbox)

	var scores := SaveManager.get_high_scores()
	if scores.is_empty():
		var lbl := Label.new()
		lbl.text = "No scores yet.\nPlay a game first!"
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		lbl.add_theme_font_size_override("font_size", 28)
		vbox.add_child(lbl)
	else:
		for i in range(scores.size()):
			var entry: Dictionary = scores[i]
			var lbl := Label.new()
			lbl.text = "%d.  %d chips  —  Floor %d  (%s)" % [
				i + 1, entry.chips, entry.floor, entry.date
			]
			lbl.add_theme_font_size_override("font_size", 24)
			vbox.add_child(lbl)

	add_child(dialog)
	dialog.popup_centered()
	dialog.confirmed.connect(dialog.queue_free)
	dialog.canceled.connect(dialog.queue_free)

func _on_update_available(latest_version: String, download_url: String) -> void:
	var dialog := ConfirmationDialog.new()
	dialog.title = "Update Available"
	dialog.dialog_text = "Version %s is available!\nDownload now?" % latest_version
	dialog.confirmed.connect(func():
		OS.shell_open(download_url)
		dialog.queue_free()
	)
	dialog.canceled.connect(dialog.queue_free)
	add_child(dialog)
	dialog.popup_centered()

extends Control

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_ui()
	get_tree().create_timer(2.5).timeout.connect(_start_update_check)

func _start_update_check() -> void:
	UpdateManager.update_available.connect(_on_update_available)
	UpdateManager.check_for_updates()

func _build_ui() -> void:
	# Background image
	var bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/menu_bg.png"):
		bg.texture = load("res://assets/ui/menu_bg.png")
	bg.stretch_mode = TextureRect.STRETCH_SCALE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# Dark overlay (full screen)
	var overlay := ColorRect.new()
	overlay.color = Color(0.02, 0.0, 0.0, 0.86)
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(overlay)

	# Single VBox — title at top, expanding spacer in middle, buttons at bottom
	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left   = 60.0
	vbox.offset_right  = -60.0
	vbox.offset_top    = 80.0
	vbox.offset_bottom = -80.0
	vbox.add_theme_constant_override("separation", 20)
	add_child(vbox)

	# Devil icon
	var devil_icon := TextureRect.new()
	if ResourceLoader.exists("res://assets/effects/devil_watermark.png"):
		devil_icon.texture = load("res://assets/effects/devil_watermark.png")
	devil_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	devil_icon.custom_minimum_size = Vector2(116, 116)
	devil_icon.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	devil_icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(devil_icon)

	# VELVET SPIN
	var title := Label.new()
	title.text = "VELVET SPIN"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	title.add_theme_font_size_override("font_size", 88)
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(title)

	# Infernal Roulette subtitle
	var subtitle := Label.new()
	subtitle.text = "Infernal Roulette"
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	subtitle.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.82))
	subtitle.add_theme_font_size_override("font_size", 32)
	subtitle.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(subtitle)

	# Expanding spacer — pushes buttons to bottom while showing background art
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(spacer)

	var enter_btn := _make_btn("ENTER THE PARLOUR")
	enter_btn.pressed.connect(_on_new_game)
	vbox.add_child(enter_btn)

	var shop_btn := _make_btn("THE VELVET SHOP")
	shop_btn.pressed.connect(_on_velvet_shop)
	vbox.add_child(shop_btn)

	var howto_btn := _make_btn("HOW TO PLAY")
	howto_btn.pressed.connect(_on_how_to_play)
	vbox.add_child(howto_btn)

	# Version text
	var ver_lbl := Label.new()
	var ver: String = ProjectSettings.get_setting("application/config/version", "1.0.0")
	ver_lbl.text = "The house always remembers · v" + ver
	ver_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	ver_lbl.add_theme_color_override("font_color", Color(Constants.COLOR_TEXT.r, Constants.COLOR_TEXT.g, Constants.COLOR_TEXT.b, 0.5))
	ver_lbl.add_theme_font_size_override("font_size", 22)
	ver_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	vbox.add_child(ver_lbl)

func _make_btn(text: String) -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(580, 110)
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

func _on_new_game() -> void:
	AudioManager.play_ui_click()
	GameManager.start_new_game()
	get_tree().change_scene_to_file("res://scenes/Game.tscn")

func _on_velvet_shop() -> void:
	AudioManager.play_ui_click()
	# Show the shop as a preview (without starting game state)
	get_tree().change_scene_to_file("res://scenes/Shop.tscn")

func _on_how_to_play() -> void:
	AudioManager.play_ui_click()
	_show_howto_dialog()

func _show_howto_dialog() -> void:
	var dialog := AcceptDialog.new()
	dialog.title = "How to Play"
	dialog.size = Vector2(800, 900)

	var vbox := VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 16)
	dialog.add_child(vbox)

	var lines := [
		"VELVET SPIN — Infernal Roulette Roguelite",
		"",
		"• Place bets on the roulette table before each spin.",
		"• Reach the chip target each Ante to advance.",
		"• Visit the Velvet Shop between Antes to buy Joker cards.",
		"• Jokers grant powerful bonuses — collect wisely.",
		"• If your chips drop below " + str(Constants.GAME_OVER_CHIPS) + ", you are RUINED.",
		"",
		"BET TYPES:",
		"  Straight (35:1) — Single number",
		"  Column / Dozen (2:1) — 12 numbers",
		"  Even Chance (1:1) — Red/Black, Odd/Even, Low/High",
	]
	for line in lines:
		var lbl := Label.new()
		lbl.text = line
		lbl.add_theme_font_size_override("font_size", 26)
		lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
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

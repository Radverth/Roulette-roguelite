class_name HUD
extends Control

var _chips_lbl: Label
var _floor_lbl: Label
var _spin_lbl: Label
var _ante_bar: ProgressBar
var _ante_lbl: Label
var _boss_banner: Panel

func _ready() -> void:
	custom_minimum_size = Vector2(1080, 160)
	_build()
	GameManager.chips_changed.connect(_on_chips_changed)
	GameManager.floor_changed.connect(_on_floor_changed)
	GameManager.ante_progress_changed.connect(_on_ante_changed)

func _build() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.04, 0.0, 0.0, 0.9)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var top := HBoxContainer.new()
	top.position = Vector2(16, 8)
	top.size = Vector2(1048, 70)
	top.add_theme_constant_override("separation", 20)
	add_child(top)

	_chips_lbl = _make_label("Chips: 500", 32)
	_chips_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(_chips_lbl)

	_floor_lbl = _make_label("Floor 1", 28)
	_floor_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_floor_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(_floor_lbl)

	_spin_lbl = _make_label("Spin: 0", 24)
	_spin_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_spin_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(_spin_lbl)

	var bar_container := HBoxContainer.new()
	bar_container.position = Vector2(16, 86)
	bar_container.size = Vector2(1048, 50)
	add_child(bar_container)

	_ante_lbl = _make_label("Ante: 0 / 1000", 20)
	_ante_lbl.custom_minimum_size = Vector2(240, 50)
	bar_container.add_child(_ante_lbl)

	var bar_wrap := Control.new()
	bar_wrap.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	bar_wrap.custom_minimum_size = Vector2(0, 50)
	bar_container.add_child(bar_wrap)

	var bar_bg_tex := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/bar_bg.png"):
		bar_bg_tex.texture = load("res://assets/ui/bar_bg.png")
	bar_bg_tex.stretch_mode = TextureRect.STRETCH_SCALE
	bar_bg_tex.set_anchors_preset(Control.PRESET_FULL_RECT)
	bar_bg_tex.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar_wrap.add_child(bar_bg_tex)

	_ante_bar = ProgressBar.new()
	_ante_bar.set_anchors_preset(Control.PRESET_FULL_RECT)
	_ante_bar.margin_left = 4.0
	_ante_bar.margin_right = -4.0
	_ante_bar.margin_top = 8.0
	_ante_bar.margin_bottom = -8.0
	_ante_bar.value = 0
	_ante_bar.max_value = 1000
	_ante_bar.show_percentage = false
	var bar_style := StyleBoxTexture.new()
	if ResourceLoader.exists("res://assets/ui/bar_fill.png"):
		bar_style.texture = load("res://assets/ui/bar_fill.png")
	_ante_bar.add_theme_stylebox_override("fill", bar_style)
	var bg_style := StyleBoxFlat.new()
	bg_style.bg_color = Color(0, 0, 0, 0)
	_ante_bar.add_theme_stylebox_override("background", bg_style)
	bar_wrap.add_child(_ante_bar)

	_boss_banner = Panel.new()
	_boss_banner.position = Vector2(16, 8)
	_boss_banner.size = Vector2(1048, 144)
	var boss_style := StyleBoxFlat.new()
	boss_style.bg_color = Color(0.35, 0.0, 0.0, 0.92)
	boss_style.border_color = Constants.COLOR_GOLD
	boss_style.set_border_width_all(2)
	_boss_banner.add_theme_stylebox_override("panel", boss_style)
	_boss_banner.hide()
	add_child(_boss_banner)

	var boss_lbl := Label.new()
	boss_lbl.name = "BossLbl"
	boss_lbl.set_anchors_preset(Control.PRESET_FULL_RECT)
	boss_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	boss_lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	boss_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	boss_lbl.add_theme_font_size_override("font_size", 26)
	_boss_banner.add_child(boss_lbl)

func _make_label(text: String, size: int) -> Label:
	var lbl := Label.new()
	lbl.text = text
	lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	lbl.add_theme_font_size_override("font_size", size)
	return lbl

func show_boss_modifier() -> void:
	if not GameManager.is_boss_floor:
		return
	var mod := GameManager.current_boss_modifier
	var lbl := _boss_banner.get_node("BossLbl") as Label
	lbl.text = "⚠  BOSS FLOOR — %s\n%s" % [mod.get("name", ""), mod.get("desc", "")]
	_boss_banner.show()

func hide_boss_modifier() -> void:
	_boss_banner.hide()

func update_spin_count(count: int) -> void:
	_spin_lbl.text = "Spin: %d" % count

func _on_chips_changed(amount: int) -> void:
	_chips_lbl.text = "Chips: %d" % amount

func _on_floor_changed(floor_num: int) -> void:
	_floor_lbl.text = "Floor %d" % floor_num
	if GameManager.is_boss_floor:
		_floor_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	else:
		_floor_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)

func _on_ante_changed(current: int, target: int) -> void:
	_ante_bar.max_value = target
	_ante_bar.value = current
	_ante_lbl.text = "Ante: %d / %d" % [current, target]

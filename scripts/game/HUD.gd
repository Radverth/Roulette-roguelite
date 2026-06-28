class_name HUD
extends Control

var _chips_lbl: Label
var _floor_lbl: Label
var _spin_lbl: Label
var _ante_fill: TextureRect
var _ante_fill_wrap: Control
var _ante_lbl: Label
var _boss_banner: Panel
var _ante_max: int = 1000
var _ante_current: int = 0

func _ready() -> void:
	custom_minimum_size = Vector2(1080, 150)
	_build()
	GameManager.chips_changed.connect(_on_chips_changed)
	GameManager.floor_changed.connect(_on_floor_changed)
	GameManager.ante_progress_changed.connect(_on_ante_changed)

func _build() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.04, 0.0, 0.0, 0.95)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	# ── Stats row ──────────────────────────────────────────────────────────
	var stats_row := Control.new()
	stats_row.position = Vector2(16, 8)
	stats_row.size = Vector2(1048, 62)
	stats_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(stats_row)

	_chips_lbl = _lbl("Chips: 500", 34)
	_chips_lbl.position = Vector2(0, 10)
	_chips_lbl.size = Vector2(400, 50)
	stats_row.add_child(_chips_lbl)

	_floor_lbl = _lbl("Floor 1", 28)
	_floor_lbl.position = Vector2(374, 14)
	_floor_lbl.size = Vector2(300, 44)
	_floor_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	stats_row.add_child(_floor_lbl)

	_spin_lbl = _lbl("Spin 0", 24)
	_spin_lbl.position = Vector2(698, 18)
	_spin_lbl.size = Vector2(350, 40)
	_spin_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	stats_row.add_child(_spin_lbl)

	# ── Ante bar row ────────────────────────────────────────────────────────
	var bar_row := Control.new()
	bar_row.position = Vector2(16, 78)
	bar_row.size = Vector2(1048, 54)
	bar_row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bar_row)

	_ante_lbl = _lbl("Ante: 0 / 1000", 20)
	_ante_lbl.position = Vector2(0, 10)
	_ante_lbl.size = Vector2(240, 40)
	bar_row.add_child(_ante_lbl)

	# Bar background
	var bar_bg := TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/bar_bg.png"):
		bar_bg.texture = load("res://assets/ui/bar_bg.png")
	bar_bg.stretch_mode = TextureRect.STRETCH_SCALE
	bar_bg.position = Vector2(248, 12)
	bar_bg.size = Vector2(800, 32)
	bar_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar_row.add_child(bar_bg)

	# Clip wrapper for fill bar
	_ante_fill_wrap = Control.new()
	_ante_fill_wrap.position = Vector2(252, 16)
	_ante_fill_wrap.size = Vector2(0, 24)
	_ante_fill_wrap.clip_contents = true
	_ante_fill_wrap.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bar_row.add_child(_ante_fill_wrap)

	_ante_fill = TextureRect.new()
	if ResourceLoader.exists("res://assets/ui/bar_fill.png"):
		_ante_fill.texture = load("res://assets/ui/bar_fill.png")
	_ante_fill.stretch_mode = TextureRect.STRETCH_SCALE
	_ante_fill.position = Vector2(0, 0)
	_ante_fill.size = Vector2(792, 24)
	_ante_fill.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_ante_fill_wrap.add_child(_ante_fill)

	# ── Boss banner overlay ─────────────────────────────────────────────────
	_boss_banner = Panel.new()
	_boss_banner.set_anchors_preset(Control.PRESET_FULL_RECT)
	_boss_banner.offset_left = 0.0
	_boss_banner.offset_top = 0.0
	_boss_banner.offset_right = 0.0
	_boss_banner.offset_bottom = 0.0
	var boss_style := StyleBoxFlat.new()
	boss_style.bg_color = Color(0.3, 0.0, 0.0, 0.95)
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
	boss_lbl.add_theme_font_size_override("font_size", 24)
	boss_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD
	_boss_banner.add_child(boss_lbl)

func _lbl(text: String, size: int) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_color_override("font_color", Constants.COLOR_TEXT)
	l.add_theme_font_size_override("font_size", size)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l

func show_boss_modifier() -> void:
	if not GameManager.is_boss_floor:
		return
	var mod := GameManager.current_boss_modifier
	var lbl := _boss_banner.get_node("BossLbl") as Label
	lbl.text = "BOSS FLOOR  —  %s\n%s" % [mod.get("name", ""), mod.get("desc", "")]
	_boss_banner.show()

func hide_boss_modifier() -> void:
	_boss_banner.hide()

func update_spin_count(count: int) -> void:
	_spin_lbl.text = "Spin %d" % count

func _on_chips_changed(amount: int) -> void:
	_chips_lbl.text = "Chips: %d" % amount

func _on_floor_changed(floor_num: int) -> void:
	_floor_lbl.text = "Floor %d" % floor_num
	if GameManager.is_boss_floor:
		_floor_lbl.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	else:
		_floor_lbl.add_theme_color_override("font_color", Constants.COLOR_TEXT)

func _on_ante_changed(current: int, target: int) -> void:
	_ante_current = current
	_ante_max = target
	_ante_lbl.text = "Ante: %d / %d" % [current, target]
	var ratio := float(current) / float(max(target, 1))
	_ante_fill_wrap.size.x = 792.0 * ratio

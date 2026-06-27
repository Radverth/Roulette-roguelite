class_name RouletteWheel
extends Control

signal spin_started()
signal ball_landed(number: int)

const WHEEL_DIAMETER := 540.0
const BALL_ORBIT_RADIUS := 240.0
const BALL_LAND_RADIUS := 195.0

var wheel_base: TextureRect
var wheel_numbers: TextureRect
var wheel_rim: TextureRect
var ball: TextureRect
var pocket_highlight: TextureRect
var win_burst: TextureRect
var number_label: Label

var _is_spinning := false
var _target_number := 0
var _wheel_angle := 0.0

func _ready() -> void:
	custom_minimum_size = Vector2(1080, 600)
	_build()

func _build() -> void:
	var bg := ColorRect.new()
	bg.color = Constants.COLOR_BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var cx := 540.0
	var cy := 300.0
	var half := WHEEL_DIAMETER / 2.0

	wheel_base = _make_tex("res://assets/wheel/wheel_base.png", cx - half, cy - half, WHEEL_DIAMETER, WHEEL_DIAMETER)
	wheel_base.pivot_offset = Vector2(half, half)
	add_child(wheel_base)

	wheel_numbers = _make_tex("res://assets/wheel/wheel_numbers.png", cx - half, cy - half, WHEEL_DIAMETER, WHEEL_DIAMETER)
	wheel_numbers.pivot_offset = Vector2(half, half)
	add_child(wheel_numbers)

	var rim_size := WHEEL_DIAMETER + 60.0
	wheel_rim = _make_tex("res://assets/wheel/wheel_rim.png", cx - rim_size / 2, cy - rim_size / 2, rim_size, rim_size)
	wheel_rim.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(wheel_rim)

	pocket_highlight = _make_tex("res://assets/wheel/pocket_highlight.png", 0, 0, 128, 64)
	pocket_highlight.pivot_offset = Vector2(64, 32)
	pocket_highlight.hide()
	pocket_highlight.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(pocket_highlight)

	ball = _make_tex("res://assets/wheel/ball.png", 0, 0, 48, 48)
	ball.pivot_offset = Vector2(24, 24)
	ball.hide()
	ball.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(ball)

	win_burst = _make_tex("res://assets/effects/win_burst.png", cx - 256, cy - 256, 512, 512)
	win_burst.mouse_filter = Control.MOUSE_FILTER_IGNORE
	win_burst.hide()
	add_child(win_burst)

	number_label = Label.new()
	number_label.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	number_label.add_theme_font_size_override("font_size", 64)
	number_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	number_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	number_label.size = Vector2(200, 80)
	number_label.position = Vector2(cx - 100, cy - 40)
	number_label.hide()
	add_child(number_label)

func _make_tex(path: String, x: float, y: float, w: float, h: float) -> TextureRect:
	var tr := TextureRect.new()
	if ResourceLoader.exists(path):
		tr.texture = load(path)
	tr.stretch_mode = TextureRect.STRETCH_SCALE
	tr.position = Vector2(x, y)
	tr.size = Vector2(w, h)
	tr.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return tr

func spin(target_number: int) -> void:
	if _is_spinning:
		return
	_is_spinning = true
	_target_number = target_number
	pocket_highlight.hide()
	number_label.hide()
	ball.show()

	emit_signal("spin_started")
	AudioManager.play_spin()

	var tween := create_tween()
	tween.set_parallel(true)

	var wheel_rotations := randf_range(4.0, 7.0) * TAU
	var spin_dur := randf_range(3.5, 5.0)
	tween.tween_method(_rotate_wheel, 0.0, _wheel_angle + wheel_rotations, spin_dur).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUART)

	var ball_arc := -randf_range(5.0, 9.0) * TAU
	tween.tween_method(_orbit_ball, 0.0, ball_arc, spin_dur - 0.8).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)

	tween.set_parallel(false)
	tween.tween_interval(0.1)
	tween.tween_callback(_land_ball)

func _rotate_wheel(angle: float) -> void:
	_wheel_angle = angle
	wheel_base.rotation = angle
	wheel_numbers.rotation = angle

func _orbit_ball(arc: float) -> void:
	var cx := 540.0
	var cy := 300.0
	ball.position = Vector2(
		cx + BALL_ORBIT_RADIUS * cos(arc) - 24.0,
		cy + BALL_ORBIT_RADIUS * sin(arc) - 24.0
	)

func _land_ball() -> void:
	var pocket_angle := _pocket_world_angle(_target_number)
	var cx := 540.0
	var cy := 300.0
	var target_pos := Vector2(
		cx + BALL_LAND_RADIUS * cos(pocket_angle) - 24.0,
		cy + BALL_LAND_RADIUS * sin(pocket_angle) - 24.0
	)
	var tween := create_tween()
	tween.tween_property(ball, "position", target_pos, 0.35).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BOUNCE)
	tween.tween_callback(_show_result)

func _show_result() -> void:
	_is_spinning = false
	var pocket_angle := _pocket_world_angle(_target_number)
	var cx := 540.0
	var cy := 300.0
	pocket_highlight.position = Vector2(
		cx + BALL_LAND_RADIUS * cos(pocket_angle) - 64.0,
		cy + BALL_LAND_RADIUS * sin(pocket_angle) - 32.0
	)
	pocket_highlight.rotation = pocket_angle + PI / 2.0
	pocket_highlight.show()
	number_label.text = str(_target_number)
	number_label.show()
	emit_signal("ball_landed", _target_number)

func show_win_burst() -> void:
	win_burst.show()
	win_burst.modulate = Color.WHITE
	var tween := create_tween()
	tween.tween_property(win_burst, "modulate:a", 0.0, 1.5)
	tween.tween_callback(win_burst.hide)

func reset_for_next_spin() -> void:
	pocket_highlight.hide()
	number_label.hide()
	ball.hide()
	win_burst.hide()

func _pocket_world_angle(number: int) -> float:
	var seq := Constants.WHEEL_SEQUENCE
	var idx := seq.find(number)
	if idx < 0:
		return 0.0
	var base_angle := (float(idx) / float(seq.size())) * TAU - PI / 2.0
	return base_angle + _wheel_angle

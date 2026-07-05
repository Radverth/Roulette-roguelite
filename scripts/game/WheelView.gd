class_name WheelView
extends Control

# Procedurally drawn roulette wheel. Pockets, numbers, separators, hub and rim
# are all rendered in _draw() from the same geometry, so the ball, pointer and
# winning sector always align exactly — no layered PNG art to drift apart.

var wheel_rotation := 0.0:
	set(value):
		wheel_rotation = value
		queue_redraw()

const RIM_GOLD     := Color(0.788, 0.659, 0.298)
const POCKET_RED   := Color(0.545, 0.086, 0.086)
const POCKET_BLACK := Color(0.10, 0.09, 0.09)
const POCKET_GREEN := Color(0.075, 0.35, 0.15)
const NUMBER_TINT  := Color(0.94, 0.90, 0.83)

func _draw() -> void:
	var c := size / 2.0
	var r := minf(size.x, size.y) / 2.0
	var seq := Constants.WHEEL_SEQUENCE
	var step := TAU / seq.size()

	# Body and rim rings
	draw_circle(c, r, Color(0.05, 0.02, 0.02))
	draw_arc(c, r - 3.0, 0.0, TAU, 128, RIM_GOLD, 3.0, true)
	draw_arc(c, r * 0.94, 0.0, TAU, 128, RIM_GOLD.darkened(0.25), 2.0, true)

	# Pocket sectors — sector i is centred at (-TAU/4 + i*step + wheel_rotation),
	# i.e. pocket 0 sits at the top pointer when wheel_rotation == 0
	var r_out := r * 0.93
	var r_in := r * 0.60
	for i in range(seq.size()):
		var n: int = seq[i]
		var a0 := -TAU / 4.0 - step / 2.0 + i * step + wheel_rotation
		var col := POCKET_GREEN
		if n != 0:
			col = POCKET_RED if n in Constants.RED_NUMBERS else POCKET_BLACK
		draw_colored_polygon(_wedge(c, r_in, r_out, a0, a0 + step), col)

	# Sector separators
	for i in range(seq.size()):
		var a := -TAU / 4.0 - step / 2.0 + i * step + wheel_rotation
		var dirv := Vector2(cos(a), sin(a))
		draw_line(c + dirv * r_in, c + dirv * r_out, RIM_GOLD.darkened(0.45), 1.5, true)

	# Numbers, upright along the pocket band
	var font := ThemeDB.fallback_font
	var fsize := int(r * 0.085)
	for i in range(seq.size()):
		var a := -TAU / 4.0 + i * step + wheel_rotation
		var pos := c + Vector2(cos(a), sin(a)) * r * 0.80
		draw_set_transform(pos, a + TAU / 4.0, Vector2.ONE)
		draw_string(font, Vector2(-30.0, fsize * 0.35), str(seq[i]),
			HORIZONTAL_ALIGNMENT_CENTER, 60.0, fsize, NUMBER_TINT)
	draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)

	# Hub
	draw_circle(c, r_in - 4.0, Color(0.24, 0.05, 0.05))
	draw_arc(c, r_in - 4.0, 0.0, TAU, 96, RIM_GOLD.darkened(0.2), 2.0, true)
	draw_circle(c, r * 0.16, RIM_GOLD)
	draw_circle(c, r * 0.05, Color(0.1, 0.03, 0.03))

func _wedge(c: Vector2, r_in: float, r_out: float, a0: float, a1: float) -> PackedVector2Array:
	var pts := PackedVector2Array()
	var segs := 4
	for s in range(segs + 1):
		var a := lerpf(a0, a1, float(s) / segs)
		pts.append(c + Vector2(cos(a), sin(a)) * r_out)
	for s in range(segs, -1, -1):
		var a := lerpf(a0, a1, float(s) / segs)
		pts.append(c + Vector2(cos(a), sin(a)) * r_in)
	return pts

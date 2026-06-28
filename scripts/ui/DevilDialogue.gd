class_name DevilDialogue
extends Control

const LINES: Dictionary = {
	"game_start": [
		"Welcome, mortal. The wheel has been... expecting you.",
		"Ah, another soul seeking fortune. How refreshingly naive.",
		"Step into my parlour. The odds are merely... suggestions.",
		"The house greets you warmly. Your chips, however, are on borrowed time.",
	],
	"win_small": [
		"Fortune favours you... for now. Savour it while it lasts.",
		"A pittance! But even scraps taste sweet to the desperate.",
		"The wheel shows mercy. I find this deeply suspicious.",
		"Ha! You won. Surely you will press your luck further?",
	],
	"win_big": [
		"Magnificent! But remember — what the wheel gives, it reclaims.",
		"By the fires below... you actually won. Interesting.",
		"A considerable sum! I shall require it back presently.",
		"Bold play, rewarded boldly. This pleases and irritates me equally.",
	],
	"loss": [
		"Ah. The wheel has spoken its verdict — and found you wanting.",
		"Another soul's offerings consumed. The house thanks you.",
		"Failure has such an elegant inevitability, does it not?",
		"Your chips return to my keeping. All things return to me eventually.",
		"The numbers were never going to be kind. They whispered to me.",
	],
	"push": [
		"Neither victory nor defeat. The limbo of fate claims you.",
		"Even my wheel shows mercy... occasionally. How terribly dull.",
		"A draw! The universe holds its breath. So do I, barely.",
	],
	"zero": [
		"ZERO! That is MY pocket, mortal. How generous of you to visit.",
		"The green void claims all! Even my appetite has limits... almost.",
		"Zero — the great equaliser, bowing to no one. Except me.",
		"The house's favourite number. Mine too. What a coincidence.",
	],
	"boss_floor": [
		"Welcome to my personal parlour. Special conditions apply here.",
		"A boss floor! Do try not to weep openly. It is most unseemly.",
		"Here the house has written new rules. Guess who the house is.",
		"Cursed conditions are my gift to you, mortal. You are welcome.",
	],
	"floor_complete": [
		"You advance. Curious. This was not in my arrangement.",
		"The ante is met. The shop awaits — spend wisely, if you are able.",
		"Progress! How... unexpected. The wheel and I are quite displeased.",
		"Another floor falls. Luck or skill? I suspect the former. Deeply.",
	],
	"shop": [
		"My wares are fairly priced. For ME, that is.",
		"Choose carefully. These trinkets carry... certain obligations.",
		"Welcome to the Velvet Shop. All sales are final. ALL of them.",
		"Browse freely, mortal. Your soul is accepted as payment in extremis.",
	],
	"game_over": [
		"Bankrupt! How absolutely, perfectly expected of you.",
		"Your chips, your dignity — mine now. Do return. I insist.",
		"The great wheel has rendered its verdict. Case closed. Enjoy ruin.",
		"The house always wins. I have always known this. Now so do you.",
	],
}

var _panel: Panel
var _lbl: RichTextLabel
var _tween: Tween

func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	z_index = 20
	_build()

func _build() -> void:
	_panel = Panel.new()
	_panel.anchor_left   = 0.0
	_panel.anchor_top    = 1.0
	_panel.anchor_right  = 1.0
	_panel.anchor_bottom = 1.0
	_panel.offset_top    = -190.0
	_panel.offset_left   = 10.0
	_panel.offset_right  = -10.0
	_panel.offset_bottom = -10.0
	_panel.mouse_filter  = Control.MOUSE_FILTER_IGNORE
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.04, 0.00, 0.07, 0.94)
	style.border_color = Constants.COLOR_GOLD
	style.set_border_width_all(2)
	style.set_corner_radius_all(6)
	_panel.add_theme_stylebox_override("panel", style)
	add_child(_panel)

	var header := Label.new()
	header.text = "☩  THE DEVIL  ☩"
	header.position = Vector2(14, 8)
	header.size = Vector2(700, 34)
	header.add_theme_color_override("font_color", Constants.COLOR_GOLD)
	header.add_theme_font_size_override("font_size", 22)
	header.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_panel.add_child(header)

	var sep := ColorRect.new()
	sep.color = Color(Constants.COLOR_GOLD.r, Constants.COLOR_GOLD.g, Constants.COLOR_GOLD.b, 0.6)
	sep.position = Vector2(14, 48)
	sep.size = Vector2(1052, 1)
	sep.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_panel.add_child(sep)

	_lbl = RichTextLabel.new()
	_lbl.position = Vector2(14, 56)
	_lbl.size = Vector2(1052, 120)
	_lbl.bbcode_enabled = true
	_lbl.fit_content = false
	_lbl.scroll_active = false
	_lbl.add_theme_color_override("default_color", Constants.COLOR_TEXT)
	_lbl.add_theme_font_size_override("normal_font_size", 28)
	_lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_panel.add_child(_lbl)

	modulate = Color(1.0, 1.0, 1.0, 0.0)

func say(category: String, auto_hide_secs: float = 3.8) -> void:
	var pool: Array = LINES.get(category, LINES["game_start"])
	var line: String = pool[randi() % pool.size()]
	_lbl.text = "[i]\"" + line + "\"[/i]"
	if _tween:
		_tween.kill()
	_tween = create_tween()
	_tween.tween_property(self, "modulate:a", 1.0, 0.25)
	if auto_hide_secs > 0.0:
		_tween.tween_interval(auto_hide_secs)
		_tween.tween_property(self, "modulate:a", 0.0, 0.6)

func hide_now() -> void:
	if _tween:
		_tween.kill()
	modulate = Color(1.0, 1.0, 1.0, 0.0)

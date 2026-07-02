extends Node

# Lightweight synthesized SFX — generated at startup so the game ships with
# sound without bundling audio assets. Swap streams for real recordings later.

const MIX_RATE := 22050
const POOL_SIZE := 8

var _streams: Dictionary = {}
var _pool: Array[AudioStreamPlayer] = []
var _pool_idx := 0

func _ready() -> void:
	for i in range(POOL_SIZE):
		var p := AudioStreamPlayer.new()
		p.volume_db = -8.0
		add_child(p)
		_pool.append(p)

	_streams["click"]  = _make_melody([520.0], 0.05)
	_streams["chip"]   = _make_melody([880.0], 0.06)
	_streams["card"]   = _make_melody([660.0, 990.0], 0.07)
	_streams["win"]    = _make_melody([523.25, 659.25, 783.99, 1046.5], 0.09)
	_streams["loss"]   = _make_melody([330.0, 262.0, 208.0], 0.12)
	_streams["spin"]   = _make_spin_noise(1.4)

func play_spin() -> void:
	_play("spin")

func play_win() -> void:
	_play("win")

func play_loss() -> void:
	_play("loss")

func play_card_pickup() -> void:
	_play("card")

func play_chip() -> void:
	_play("chip")

func play_ui_click() -> void:
	_play("click")

func _play(key: String) -> void:
	var stream: AudioStream = _streams.get(key)
	if not stream:
		return
	var p := _pool[_pool_idx]
	_pool_idx = (_pool_idx + 1) % POOL_SIZE
	p.stream = stream
	p.play()

func _make_melody(freqs: Array, note_dur: float) -> AudioStreamWAV:
	var total_samples := int(MIX_RATE * note_dur) * freqs.size()
	var data := PackedByteArray()
	data.resize(total_samples * 2)
	var pos := 0
	for f in freqs:
		var note_samples := int(MIX_RATE * note_dur)
		for i in range(note_samples):
			var t := float(i) / MIX_RATE
			var env := minf(t * 200.0, 1.0) * exp(-6.0 * t / note_dur)
			var sample := sin(TAU * f * t) * env * 0.6
			var v := int(clampf(sample, -1.0, 1.0) * 32767.0)
			data.encode_s16(pos * 2, v)
			pos += 1
	return _wav(data)

func _make_spin_noise(dur: float) -> AudioStreamWAV:
	var samples := int(MIX_RATE * dur)
	var data := PackedByteArray()
	data.resize(samples * 2)
	var rng := RandomNumberGenerator.new()
	rng.seed = 1337
	var prev := 0.0
	for i in range(samples):
		var t := float(i) / samples
		# Low-passed noise fading out, like a ball losing momentum
		var white := rng.randf_range(-1.0, 1.0)
		prev = prev * 0.92 + white * 0.08
		var env := (1.0 - t) * (1.0 - t)
		var v := int(clampf(prev * env * 2.2, -1.0, 1.0) * 32767.0)
		data.encode_s16(i * 2, v)
	return _wav(data)

func _wav(data: PackedByteArray) -> AudioStreamWAV:
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = MIX_RATE
	wav.stereo = false
	wav.data = data
	return wav

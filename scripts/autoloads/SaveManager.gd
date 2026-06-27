extends Node

const SAVE_PATH := "user://save_data.json"

var high_scores: Array[Dictionary] = []

func _ready() -> void:
	_load()

func save_run(chips: int, floor_num: int) -> void:
	high_scores.append({"chips": chips, "floor": floor_num, "date": Time.get_date_string_from_system()})
	high_scores.sort_custom(func(a, b): return a.chips > b.chips)
	if high_scores.size() > 10:
		high_scores.resize(10)
	_save()

func get_high_scores() -> Array[Dictionary]:
	return high_scores

func has_save() -> bool:
	return FileAccess.file_exists(SAVE_PATH)

func _save() -> void:
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify({"high_scores": high_scores}))
		file.close()

func _load() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		return
	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if not file:
		return
	var data = JSON.parse_string(file.get_as_text())
	file.close()
	if data and data.has("high_scores"):
		high_scores.clear()
		for entry in data["high_scores"]:
			high_scores.append({"chips": int(entry.get("chips", 0)), "floor": int(entry.get("floor", 1)), "date": str(entry.get("date", ""))})

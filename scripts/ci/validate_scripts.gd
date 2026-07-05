extends SceneTree

# CI gate: load every GDScript in the project so the engine fully compiles
# them, and fail the build if any script has errors. Catches semantic errors
# (bad property access, unknown methods) that syntax-only linters miss —
# a single broken script otherwise ships and gray-screens its whole scene.
#
# Run with: godot --headless --script res://scripts/ci/validate_scripts.gd

func _initialize() -> void:
	var failed := 0
	for path in _collect_scripts("res://scripts"):
		var script: Script = load(path)
		if script == null or not script.can_instantiate():
			printerr("COMPILE FAILURE: %s" % path)
			failed += 1
		else:
			print("OK: %s" % path)
	if failed > 0:
		printerr("%d script(s) failed to compile" % failed)
	quit(1 if failed > 0 else 0)

func _collect_scripts(dir_path: String) -> PackedStringArray:
	var out := PackedStringArray()
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return out
	dir.list_dir_begin()
	var entry := dir.get_next()
	while entry != "":
		var full := dir_path + "/" + entry
		if dir.current_is_dir():
			out.append_array(_collect_scripts(full))
		elif entry.ends_with(".gd"):
			out.append(full)
		entry = dir.get_next()
	dir.list_dir_end()
	return out

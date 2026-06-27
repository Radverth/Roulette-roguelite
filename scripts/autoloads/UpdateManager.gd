extends Node

signal update_available(latest_version: String, download_url: String)
signal update_check_done()

var _http: HTTPRequest
var _checking := false

func _ready() -> void:
	_http = HTTPRequest.new()
	add_child(_http)
	_http.request_completed.connect(_on_request_completed)

func check_for_updates() -> void:
	if _checking:
		return
	_checking = true
	var current := ProjectSettings.get_setting("application/config/version", "1.0.0") as String
	var err := _http.request(
		Constants.GITHUB_API_LATEST,
		["User-Agent: VelvetSpin/" + current, "Accept: application/vnd.github+json"]
	)
	if err != OK:
		_checking = false
		emit_signal("update_check_done")

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	_checking = false
	if result != HTTPRequest.RESULT_SUCCESS or response_code != 200:
		emit_signal("update_check_done")
		return

	var data = JSON.parse_string(body.get_string_from_utf8())
	if not data or not data.has("tag_name"):
		emit_signal("update_check_done")
		return

	var remote_tag: String = data["tag_name"]
	var remote_version := remote_tag.trim_prefix("v")
	var download_url := ""

	if data.has("assets"):
		for asset in data["assets"]:
			if str(asset.get("name", "")).ends_with(".apk"):
				download_url = str(asset.get("browser_download_url", ""))
				break

	if download_url.is_empty() and data.has("html_url"):
		download_url = str(data["html_url"])

	var current_version := ProjectSettings.get_setting("application/config/version", "1.0.0") as String
	if _is_newer(remote_version, current_version):
		emit_signal("update_available", remote_version, download_url)
	else:
		emit_signal("update_check_done")

func _is_newer(remote: String, current: String) -> bool:
	var r := remote.split(".")
	var c := current.split(".")
	for i in range(max(r.size(), c.size())):
		var rv := int(r[i]) if i < r.size() else 0
		var cv := int(c[i]) if i < c.size() else 0
		if rv > cv:
			return true
		if rv < cv:
			return false
	return false

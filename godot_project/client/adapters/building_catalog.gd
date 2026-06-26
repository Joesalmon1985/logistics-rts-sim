class_name BuildingCatalog
extends RefCounted

## Display-only building metadata from sim config (no legality rules).

const CONFIG_PATH := "res://sim/data/config/buildings.json"

static var _buildings: Dictionary = {}
static var _loaded: bool = false

static var ROLE_TEXT: Dictionary = {
	"headquarters": "Stores goods and connects your settlement to the road network.",
	"woodcutter": "Cuts timber from nearby forest hexes when connected by roads.",
	"forester": "Renews forest so woodcutters can keep producing timber.",
	"sawmill": "Turns timber into planks for construction.",
	"quarry": "Produces stone from rock or quarryable terrain nearby.",
}

static var TIP_TEXT: Dictionary = {
	"woodcutter": "Place on forest next to a road. Connect roads back to Headquarters.",
	"forester": "Place on forest to regrow trees for your woodcutters.",
	"sawmill": "Place on grass near roads. Needs timber from a Woodcutter.",
	"quarry": "Place on rock or quarryable terrain near roads.",
	"headquarters": "Your central storehouse — keep roads connected to new buildings.",
}


static func load_catalog() -> void:
	if _loaded:
		return
	var file := FileAccess.open(CONFIG_PATH, FileAccess.READ)
	if file == null:
		_loaded = true
		return
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) == TYPE_DICTIONARY:
		_buildings = parsed.get("buildings", {})
	_loaded = true


static func display_name(building_type: String) -> String:
	load_catalog()
	var entry: Dictionary = _buildings.get(building_type, {})
	var name: String = str(entry.get("display_name", ""))
	if name != "":
		return name
	return building_type.replace("_", " ").capitalize()


static func role_text(building_type: String) -> String:
	return str(ROLE_TEXT.get(building_type, "Produces or stores settlement goods."))


static func tip_text(building_type: String) -> String:
	return str(TIP_TEXT.get(building_type, ""))


static func footprint_offsets(building_type: String) -> Array:
	load_catalog()
	var entry: Dictionary = _buildings.get(building_type, {})
	var fp: Array = entry.get("footprint", [[0, 0]])
	return fp


static func occupies_hex(building: Dictionary, q: int, r: int) -> bool:
	var btype: String = str(building.get("building_type", ""))
	var aq: int = int(building.get("anchor_q", building.get("q", -1)))
	var ar: int = int(building.get("anchor_r", building.get("r", -1)))
	for off in footprint_offsets(btype):
		if aq + int(off[0]) == q and ar + int(off[1]) == r:
			return true
	return false

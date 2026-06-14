class_name HexTile
extends RefCounted

var terrain_type: String = "grass"
var height_level: int = 0
var passability: int = 1
var buildability: int = 1
var owner_player_id: String = ""
var tree_density: int = 0
var deposit_type: String = ""
var deposit_amount: int = 0
var deposit_revealed: int = 0
var fertility: int = 0
var has_water_source: int = 0
var road_segment_id: int = 0


func _init(
	p_terrain: String = "grass",
	p_height: int = 0,
	p_passability: int = 1,
	p_buildability: int = 1,
) -> void:
	terrain_type = p_terrain
	height_level = p_height
	passability = p_passability
	buildability = p_buildability


static func from_dict(data: Dictionary) -> HexTile:
	var tile := HexTile.new(
		str(data.get("terrain_type", "grass")),
		int(data.get("height_level", 0)),
		int(data.get("passability", 1)),
		int(data.get("buildability", 1)),
	)
	tile.owner_player_id = str(data.get("owner_player_id", ""))
	tile.tree_density = int(data.get("tree_density", 0))
	tile.deposit_type = str(data.get("deposit_type", ""))
	tile.deposit_amount = int(data.get("deposit_amount", 0))
	tile.deposit_revealed = int(data.get("deposit_revealed", 0))
	tile.fertility = int(data.get("fertility", 0))
	tile.has_water_source = int(data.get("has_water_source", 0))
	tile.road_segment_id = int(data.get("road_segment_id", 0))
	return tile


func to_dict() -> Dictionary:
	var data := {
		"terrain_type": terrain_type,
		"height_level": height_level,
		"passability": passability,
		"buildability": buildability,
	}
	if owner_player_id != "":
		data["owner_player_id"] = owner_player_id
	if tree_density > 0:
		data["tree_density"] = tree_density
	if deposit_type != "":
		data["deposit_type"] = deposit_type
		data["deposit_amount"] = deposit_amount
		data["deposit_revealed"] = deposit_revealed
	if fertility > 0:
		data["fertility"] = fertility
	if has_water_source > 0:
		data["has_water_source"] = has_water_source
	if road_segment_id > 0:
		data["road_segment_id"] = road_segment_id
	return data

class_name HexTile
extends RefCounted

var terrain_type: String = "grass"
var height_level: int = 0
var passability: int = 1
var buildability: int = 1


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
	return HexTile.new(
		str(data.get("terrain_type", "grass")),
		int(data.get("height_level", 0)),
		int(data.get("passability", 1)),
		int(data.get("buildability", 1)),
	)


func to_dict() -> Dictionary:
	return {
		"terrain_type": terrain_type,
		"height_level": height_level,
		"passability": passability,
		"buildability": buildability,
	}

class_name HexMap
extends RefCounted

var width: int = 0
var height: int = 0
var tiles: Dictionary = {}


func _init(map_width: int = 0, map_height: int = 0) -> void:
	width = map_width
	height = map_height


func in_bounds(coord: HexCoord) -> bool:
	return coord.q >= 0 and coord.q < width and coord.r >= 0 and coord.r < height


func get_tile(coord: HexCoord) -> HexTile:
	var tile: Variant = tiles.get(coord.key())
	if tile == null:
		return null
	return tile as HexTile


func set_tile(coord: HexCoord, tile: HexTile) -> void:
	tiles[coord.key()] = tile


func to_dict() -> Dictionary:
	var hexes: Array = []
	var keys: Array = tiles.keys()
	keys.sort()
	for key in keys:
		var parts: PackedStringArray = str(key).split(",")
		var tile: HexTile = tiles[key]
		hexes.append({
			"q": int(parts[0]),
			"r": int(parts[1]),
			"terrain_type": tile.terrain_type,
			"height_level": tile.height_level,
			"passability": tile.passability,
			"buildability": tile.buildability,
		})
	return {
		"width": width,
		"height": height,
		"hexes": hexes,
	}

class_name HexCoord
extends RefCounted

## Axial hex coordinate (q, r). All math uses integers.

var q: int = 0
var r: int = 0

const DIRECTIONS: Array[Vector2i] = [
	Vector2i(1, 0),
	Vector2i(1, -1),
	Vector2i(0, -1),
	Vector2i(-1, 0),
	Vector2i(-1, 1),
	Vector2i(0, 1),
]


func _init(coord_q: int = 0, coord_r: int = 0) -> void:
	q = coord_q
	r = coord_r


static func from_dict(data: Dictionary) -> HexCoord:
	return HexCoord.new(int(data.get("q", 0)), int(data.get("r", 0)))


func to_dict() -> Dictionary:
	return {"q": q, "r": r}


func add(other: HexCoord) -> HexCoord:
	return HexCoord.new(q + other.q, r + other.r)


func neighbor(direction_index: int) -> HexCoord:
	var dir: Vector2i = DIRECTIONS[direction_index % DIRECTIONS.size()]
	return HexCoord.new(q + dir.x, r + dir.y)


func neighbors() -> Array[HexCoord]:
	var result: Array[HexCoord] = []
	for i in range(DIRECTIONS.size()):
		result.append(neighbor(i))
	return result


func distance(other: HexCoord) -> int:
	var dq: int = q - other.q
	var dr: int = r - other.r
	var ds: int = -dq - dr
	return (absi(dq) + absi(dr) + absi(ds)) / 2


func key() -> String:
	return "%d,%d" % [q, r]

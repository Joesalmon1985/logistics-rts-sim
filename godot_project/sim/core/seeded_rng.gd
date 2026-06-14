class_name SeededRng
extends RefCounted

## Deterministic seeded RNG stored in simulation state. No global randi()/randf().

var state: int = 0


func _init(initial_state: int = 1) -> void:
	state = initial_state if initial_state != 0 else 1


static func from_seed(seed_value: int) -> SeededRng:
	var rng := SeededRng.new()
	rng.state = seed_value if seed_value != 0 else 1
	return rng


func next_int(min_value: int, max_value: int) -> int:
	if min_value > max_value:
		push_error("SeededRng.next_int: min > max")
		return min_value
	var span: int = max_value - min_value + 1
	if span <= 0:
		return min_value
	return min_value + (next_uint() % span)


func next_uint() -> int:
	# 32-bit xorshift — deterministic across platforms.
	state ^= state << 13
	state &= 0xFFFFFFFF
	state ^= state >> 17
	state &= 0xFFFFFFFF
	state ^= state << 5
	state &= 0xFFFFFFFF
	return state


func serialize() -> int:
	return state


func deserialize(value: int) -> void:
	state = value if value != 0 else 1

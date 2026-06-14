class_name FixedPoint
extends RefCounted

## Integer fixed-point helpers for authoritative state (DDR-027).
const SCALE: int = 1000


static func to_scaled(value: int) -> int:
	return value * SCALE


static func from_scaled(scaled: int) -> int:
	return scaled / SCALE

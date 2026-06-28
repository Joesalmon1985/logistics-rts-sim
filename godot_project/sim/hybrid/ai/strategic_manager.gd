class_name StrategicManager
extends RefCounted

## Low-frequency strategic intent for modular hybrid bots (PRD A.4.5).

const INTENTS: Array[String] = [
	"stabilise_economy",
	"expand",
	"tech_to_military",
	"defend",
	"harass",
	"attack_to_eliminate",
	"recover_from_damage",
]

var _last_intent: String = "stabilise_economy"
var _interval_ticks: int = 120


func reset() -> void:
	_last_intent = "stabilise_economy"


func tick_intent(state: GameState, player_id: String, snapshot: Dictionary, current_tick: int) -> String:
	if current_tick % _interval_ticks != 0:
		return _last_intent
	var enemies: int = HybridAiObservationExporter._visible_enemy_count(snapshot, player_id)
	var barracks: int = HybridAiActionSpace._building_id(snapshot, player_id, "barracks")
	if enemies > 0 and barracks > 0:
		_last_intent = "attack_to_eliminate"
	elif enemies > 0:
		_last_intent = "defend"
	elif barracks > 0:
		_last_intent = "tech_to_military"
	else:
		_last_intent = "expand"
	return _last_intent


func get_intent() -> String:
	return _last_intent

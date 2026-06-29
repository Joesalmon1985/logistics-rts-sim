class_name GodStrategyConstants
extends RefCounted

const BUCKET_MATCH: String = "god_strategy_match"
const BUCKET_DRAFT: String = "god_strategy_draft"
const BUCKET_UNLOCKS: String = "god_strategy_unlocks"

const AGES: Array[String] = ["prehistoric", "historic", "modern", "futuristic_collapse"]
const DRAFT_PICKS_PER_AGE: int = 8
const DRAFT_INTERVAL_TICKS: int = 500
const AGE3_VP_THRESHOLD: int = 10

const PROOF_TICK_CAP: int = 3000
const MICRO_TICK_CAP: int = 800
const SMOKE_TICK_CAP: int = 500
const LONG_FORM_TICK_CAP: int = 8000

const PHASE_ORDER: Array[String] = [
	"init",
	"worker_policy",
	"roads",
	"woodcutter",
	"sawmill",
	"barracks",
	"queue_unit",
	"unit_produced",
	"attack",
	"combat",
	"hold",
]

const SKIPPABLE_ROAD_ERRORS: Array[String] = [
	"ROAD_EXISTS",
	"NOT_OWNER",
	"TERRAIN_BLOCKED",
	"OUT_OF_BOUNDS",
	"SLOPE_TOO_STEEP",
]

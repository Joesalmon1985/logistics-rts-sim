class_name MilitaryConstants
extends RefCounted

## Stable identifiers for the Trial Military Game simulation core.
## All buckets live under GameState.entities with the "mil_" prefix to avoid
## collision with economy buckets (hybrid composition, RUN_07).

const BUCKET_META: String = "mil_meta"
const BUCKET_PLAYERS: String = "mil_players"
const BUCKET_RESOURCE_FIELDS: String = "mil_resource_fields"
const BUCKET_BUILDINGS: String = "mil_buildings"
const BUCKET_CONSTRUCTION_SITES: String = "mil_construction_sites"
const BUCKET_PRODUCTION_QUEUES: String = "mil_production_queues"
const BUCKET_UNITS: String = "mil_units"
const BUCKET_PROJECTILES: String = "mil_projectiles"
const BUCKET_VISION: String = "mil_vision"
const BUCKET_REPAIR_JOBS: String = "mil_repair_jobs"
const BUCKET_RESEARCH: String = "mil_research"
const BUCKET_SKIRMISH: String = "mil_skirmish"
const BUCKET_COMBAT_LOG: String = "mil_combat_log"

const ALL_BUCKETS: Array[String] = [
	BUCKET_META,
	BUCKET_PLAYERS,
	BUCKET_RESOURCE_FIELDS,
	BUCKET_BUILDINGS,
	BUCKET_CONSTRUCTION_SITES,
	BUCKET_PRODUCTION_QUEUES,
	BUCKET_UNITS,
	BUCKET_PROJECTILES,
	BUCKET_VISION,
	BUCKET_REPAIR_JOBS,
	BUCKET_RESEARCH,
	BUCKET_SKIRMISH,
	BUCKET_COMBAT_LOG,
]

# Building lifecycle states.
const BLD_PLANNED: String = "planned"
const BLD_CONSTRUCTING: String = "constructing"
const BLD_ACTIVE: String = "active"
const BLD_DISABLED: String = "disabled"
const BLD_DESTROYED: String = "destroyed"

# Construction site states.
const CONS_ACTIVE: String = "active"
const CONS_COMPLETED: String = "completed"
const CONS_CANCELLED: String = "cancelled"

# Unit states.
const UNIT_IDLE: String = "idle"
const UNIT_MOVING: String = "moving"
const UNIT_ATTACKING: String = "attacking"
const UNIT_HARVESTING: String = "harvesting"
const UNIT_RETURNING: String = "returning"
const UNIT_DEAD: String = "dead"

# Order types.
const ORDER_NONE: String = "none"
const ORDER_MOVE: String = "move"
const ORDER_ATTACK: String = "attack"
const ORDER_ATTACK_MOVE: String = "attack_move"
const ORDER_PATROL: String = "patrol"
const ORDER_GUARD: String = "guard"
const ORDER_HARVEST: String = "harvest"

# Intel / fog levels.
const INTEL_UNEXPLORED: int = 0
const INTEL_EXPLORED: int = 1
const INTEL_VISIBLE: int = 2

# Weapon firing modes.
const WEAPON_INSTANT: String = "instant"
const WEAPON_PROJECTILE: String = "projectile"

# Victory states.
const VICTORY_NONE: String = "none"
const VICTORY_WON: String = "won"
const VICTORY_DRAW: String = "draw"

# Common command error codes.
const ERR_NOT_OWNER: String = "NOT_OWNER"
const ERR_UNKNOWN_BUILDING: String = "UNKNOWN_BUILDING"
const ERR_UNKNOWN_UNIT: String = "UNKNOWN_UNIT"
const ERR_FOOTPRINT_OVERLAP: String = "FOOTPRINT_OVERLAP"
const ERR_OUT_OF_BOUNDS: String = "OUT_OF_BOUNDS"
const ERR_NOT_BUILDABLE: String = "NOT_BUILDABLE"
const ERR_OUTSIDE_BUILD_RADIUS: String = "OUTSIDE_BUILD_RADIUS"
const ERR_MISSING_PREREQUISITE: String = "MISSING_PREREQUISITE"
const ERR_INSUFFICIENT_CREDITS: String = "INSUFFICIENT_CREDITS"
const ERR_NO_SUCH_ENTITY: String = "NO_SUCH_ENTITY"
const ERR_NOT_PRODUCER: String = "NOT_PRODUCER"
const ERR_UNPOWERED: String = "UNPOWERED"
const ERR_DEAD_UNIT: String = "DEAD_UNIT"
const ERR_INVALID_TARGET: String = "INVALID_TARGET"
const ERR_TARGET_NOT_VISIBLE: String = "TARGET_NOT_VISIBLE"
const ERR_UNREACHABLE: String = "UNREACHABLE"
const ERR_INVALID_PAYLOAD: String = "INVALID_PAYLOAD"
const ERR_NO_RESEARCH: String = "NO_RESEARCH"
const ERR_ALREADY_RESEARCHED: String = "ALREADY_RESEARCHED"
const ERR_RESEARCH_BUSY: String = "RESEARCH_BUSY"
const ERR_CANNOT_REPAIR: String = "CANNOT_REPAIR"

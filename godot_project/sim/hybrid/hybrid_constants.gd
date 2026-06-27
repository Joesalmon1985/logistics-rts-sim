class_name HybridConstants
extends RefCounted

const BUCKET_META: String = "hyb_meta"
const BUCKET_DELEGATED_ORDERS: String = "hyb_delegated_orders"
const BUCKET_BUILDING_POLICIES: String = "hyb_building_policies"

const ALL_BUCKETS: Array[String] = [
	BUCKET_META,
	BUCKET_DELEGATED_ORDERS,
	BUCKET_BUILDING_POLICIES,
]

const STANCE_PASSIVE: String = "passive"
const STANCE_DEFENSIVE: String = "defensive"
const STANCE_AGGRESSIVE: String = "aggressive"
const STANCE_HOLD: String = "hold"

const ORDER_ATTACK: String = "delegated_attack"
const ORDER_PATROL: String = "delegated_patrol"
const ORDER_GUARD: String = "delegated_guard"

const ERR_FORBIDDEN_DIRECT: String = "HYB_FORBIDDEN_DIRECT"
const ERR_TARGET_NOT_VISIBLE: String = "HYB_TARGET_NOT_VISIBLE"
const ERR_INSUFFICIENT_WARES: String = "HYB_INSUFFICIENT_WARES"

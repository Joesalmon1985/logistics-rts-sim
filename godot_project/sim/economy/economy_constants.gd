class_name EconomyConstants
extends RefCounted

## Stable entity bucket keys in GameState.entities
const BUCKET_ROADS: String = "roads"
const BUCKET_ROAD_NODES: String = "road_nodes"
const BUCKET_BUILDINGS: String = "buildings"
const BUCKET_CONSTRUCTION_SITES: String = "construction_sites"
const BUCKET_CARRIERS: String = "carriers"
const BUCKET_WORKERS: String = "workers"
const BUCKET_DELIVERY_JOBS: String = "delivery_jobs"
const BUCKET_WARE_STORES: String = "ware_stores"
const BUCKET_RESERVATIONS: String = "reservations"
const BUCKET_ECONOMY_META: String = "economy_meta"

## Building states
const BLD_PLANNED: String = "planned"
const BLD_UNDER_CONSTRUCTION: String = "under_construction"
const BLD_ACTIVE: String = "active"
const BLD_PAUSED_MISSING_INPUT: String = "paused_missing_input"
const BLD_PAUSED_MISSING_WORKER: String = "paused_missing_worker"
const BLD_PAUSED_NO_OUTPUT: String = "paused_no_output_space"
const BLD_PAUSED_NO_ROUTE: String = "paused_no_route"
const BLD_DISABLED: String = "disabled"
const BLD_COMPLETED: String = "completed"
const BLD_BLOCKED: String = "blocked"

## Delivery job statuses
const JOB_CREATED: String = "created"
const JOB_WAITING_WARE: String = "waiting_for_ware"
const JOB_WAITING_ROUTE: String = "waiting_for_route"
const JOB_WAITING_CARRIER: String = "waiting_for_carrier"
const JOB_IN_TRANSIT: String = "in_transit"
const JOB_DELIVERED: String = "delivered"
const JOB_CANCELLED: String = "cancelled"
const JOB_FAILED: String = "failed"
const JOB_BLOCKED: String = "blocked"

## Worker states
const WORKER_IDLE: String = "idle"
const WORKER_ASSIGNED: String = "assigned"
const WORKER_CARRYING: String = "carrying"
const WORKER_BLOCKED: String = "blocked"

## Carrier states
const CARRIER_IDLE: String = "idle"
const CARRIER_ASSIGNED: String = "assigned"
const CARRIER_MOVING: String = "moving"
const CARRIER_BLOCKED: String = "blocked"

## Construction site states
const CONS_ACTIVE: String = "active"
const CONS_BLOCKED: String = "blocked"
const CONS_COMPLETED: String = "completed"
const CONS_CANCELLED: String = "cancelled"

# Glossary

Canonical terminology for this project. Use these terms consistently across design, architecture, testing, and run documents. Avoid interchangeable synonyms unless noted.

---

## Rule ID Convention

Each major requirement has a stable **rule ID** for test traceability.

| Prefix | Domain |
|--------|--------|
| `ECON-` | Trial Economy Game |
| `MIL-` | Trial Military Game |
| `HYB-` | Hybrid Game |
| `VIS-` | Godot visualisation |
| `BOT-` | Bots and agents |
| `HUM-` | Human play |
| `EXP-` | Expanded content |
| `NET-` | Wi-Fi multiplayer |

Format: `{PREFIX}-{SUBSYSTEM}-{NNN}` — e.g. `ECON-ROAD-001`, `MIL-COMBAT-003`.

Test naming: `test_{PREFIX}_{SUBSYSTEM}_{NNN}_{short_description}` — e.g. `test_ECON_ROAD_001_roads_connect_adjacent_hexes`.

---

## Core Simulation Terms

| Term | Definition |
|------|------------|
| **Simulation core (sim)** | The authoritative **Godot/GDScript** game engine (pure scripts/resources, headless-capable, separate from UI/rendering). Owns all rules and state. |
| **Tick** | One deterministic simulation step. All clients advance by submitting commands and stepping ticks. Decoupled from render frame rate; never uses frame delta or wall-clock. |
| **Command** | A validated, serializable player or agent intent (e.g. `PlaceRoadCommand`). Mutates state only after validation. |
| **Event** | A serializable record emitted when state changes (e.g. `ware_delivered`). Append-only log for replay and diagnostics. |
| **Snapshot** | JSON export of sim-core state at a point in time. The Godot visual client and bots consume snapshots; snapshots do not drive authority. |
| **Replay** | Ordered command log + initial seed/state. Re-executing must reproduce identical events and snapshot hashes. |
| **Variant** | A game mode (`economy_trial`, `military_trial`, `hybrid`). Variant registry selects rule sets and content. |
| **Entity ID** | Stable unique identifier for map objects, buildings, units, workers, carriers. |
| **Player ID** | Identifier for a human, bot, or agent controller. Distinct from entity ownership. |

---

## Map and Terrain

| Term | Definition |
|------|------------|
| **Hex map** | Game world on a hexagonal grid using **axial coordinates** `(q, r)`. |
| **Terrain** | Per-hex surface type (grass, water, rock, forest, etc.) affecting movement and buildability. |
| **Height** | Per-hex elevation value. Used with slope for movement cost and build rules. |
| **Slope** | Derived from height difference between adjacent hexes. Affects path cost and buildability. |
| **Buildability** | Whether a hex or footprint allows placement of roads, buildings, or deposits. |
| **Footprint** | Multi-hex area occupied by a building plan or completed building. |

---

## Economy Terms

| Term | Definition |
|------|------------|
| **Ware** | A transportable economic good (timber, planks, rations, tools, etc.). Canonical term — not "item", "good", or "material". |
| **Deposit** | A map resource node (forest, rock, coal, iron, fertile land, water source) subject to extraction or depletion rules. |
| **Tree density** | Per-hex forest measure (0–100). Woodcutters reduce it; foresters increase it. Affects timber production. |
| **Headquarters (HQ)** | Starting hub building. Holds available workers and initial wares. |
| **Storehouse** | Storage building that holds wares and available workers for provisioning. |
| **Worker** | A typed labour unit (woodcutter, miner, etc.) provisioned from HQ/Storehouse to buildings. Not freely created without supply. |
| **Tool** | Ware (generic `tools` in Run 02) required before some workers can be assigned. Schema supports tool categories later. |
| **Rations / meals** | Food support tiers for mines. Rations = basic; meals = upgraded/deep mines. |
| **Carrier** | An explicit logistics entity that transports wares along the road network. In v1, assigned **per delivery job** (not per road segment). |
| **Road** | A built connection on hex edges enabling carrier pathfinding between road nodes. |
| **Road node** | **Authoritative simulation entity** — a junction or building access point on the road network where carriers pick up, drop off, or change direction. |
| **Flag** | **Player-facing / Widelands-style alias** for a road node — not a separate simulation entity or graph type. Use **road node** in technical docs and architecture. |
| **Building port / entrance** | A configured access point (`ports[]` offset from building anchor) connecting a building footprint to the road/logistics network. Default port when omitted: anchor `[[0, 0]]`. |
| **Storage** | A building or site that holds wares with capacity limits. |
| **Production recipe** | Inputs, outputs, and duration for transforming wares at a building. |
| **Production chain** | Ordered recipes linking deposits → buildings → wares (see economy requirements). |
| **Construction site** | An in-progress building consuming delivered wares, then abstract builder work, until complete. |
| **Builder work** | Abstract construction progress ware (`builder_work`) consumed **after** required materials are delivered — not a worker entity assigned to the site in v1. |
| **Builder (worker type)** | A worker type in config (e.g. for production buildings). Distinct from **builder work** ware. Not assigned to construction sites in v1. |
| **Priority** | Player-set ordering for construction, production, or ware routing. |
| **Delivery job** | Explicit simulation entity: movement request with source, destination, ware type, quantity, status, and optional `block_reason` (e.g. `no_route`, `missing_node`). |
| **Economy stalled** | Diagnostic state when production/construction cannot progress (missing input, blocked output, no path). |

---

## Military Terms

| Term | Definition |
|------|------------|
| **Credits** | Military **trial-only** abstract currency for direct production. The Hybrid Game has **no credits** — hybrid costs are ware/logistics only (DDR-031). |
| **Power supply / demand** | Generator output vs building consumption. Unpowered buildings are non-functional. |
| **Production queue** | Ordered list of units or upgrades at a military building. |
| **Infantry / vehicle** | Unit categories with distinct movement, armour, and weapon profiles. |
| **Intel / visibility** | What each player can see based on radar, unit sight, and explored terrain. |
| **Defeat condition** | Loss trigger (e.g. command hub destroyed). |

---

## Hybrid Terms

| Term | Definition |
|------|------------|
| **Logistics-fed production** | Military units and buildings require wares from the economy chain, not instant sidebar spawn. |
| **Delegated order** | Player command targeting a building or sector, not an individual unit (e.g. `SetDefencePriorityCommand`). |
| **Building stance** | Aggressive, defensive, passive, or hold behaviour assigned to a military building. |
| **Garrison** | Preference for units to remain inside or near a assigned building. |
| **Rally point** | Preferred destination for newly produced units. |
| **Delegated attack order** | Attack command against a visible hostile target issued through a building or sector, not by selecting units. |

---

## Client and Tooling Terms

| Term | Definition |
|------|------------|
| **Simulation core** | Authoritative GDScript layer (`sim/`) owning all rules and state. |
| **Godot visual client / UI** | Non-authoritative GDScript layer (`client/`) that loads snapshots and submits commands; no rules. |
| **Godot adapter** | The part of the visual client that loads snapshots and submits commands; no authority. |
| **Bot** | Automated client using the same command API as humans. |
| **Agent** | Bot or neural-network controller. Python agents (RUN_06) drive the core via Godot headless; Python is trainer, not engine. |
| **Diagnostics** | Structured stall/block/health reports exportable in snapshots and events. |
| **Smoke test** | End-to-end scenario verifying a milestone's critical path. |
| **Scenario** | Deterministic test fixture: map + seed + command script + expected outcomes. |

---

## Placeholder Building Names (Economy)

| Placeholder | Role |
|-------------|------|
| Woodcutter | Extracts timber from forest (tree density) |
| Forester | Increases tree density on adjacent forest hexes |
| Sawmill | Timber → planks |
| Rations kitchen | Bread/grain → rations |
| Meal hall | Rations → meals |
| Headquarters | Worker pool and starting storage |
| Storehouse | Worker pool and ware storage |
| Quarry | Rock deposit → stone |
| Farm / Well | Fertile land / water → grain / water |
| Bakery | Grain + water → bread |
| Coal mine | Coal deposit → coal |
| Iron mine | Iron deposit → iron ore |
| Smelter | Iron ore + coal → iron ingots |
| Workshop | Iron ingots + planks → tools |

Names may change; do not use reference-game building names.

---

## Placeholder Building Names (Military)

| Placeholder | Role |
|-------------|------|
| Command hub | Primary base; loss triggers defeat |
| Power generator | Supplies power |
| Resource processor | Converts processed wares to credits (trial) or feeds hybrid logistics |
| Barracks | Produces infantry |
| Vehicle plant | Produces vehicles |
| Radar tower | Extends intel range |
| Research lab | Unlocks tech |
| Repair depot | Repairs units and buildings |
| Defence turret | Static defence |

Names may change; do not use reference-game building names.

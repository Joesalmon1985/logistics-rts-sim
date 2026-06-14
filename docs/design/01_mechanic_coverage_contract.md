# Mechanic Coverage Contract

## Definition of "All Functionality"

**All functionality** means:

> All major mechanic families represented by simplified, original, testable systems.

For the Trial Economy Game specifically:

> **Widelands-like mechanic coverage, not Widelands feature parity.**

The economy engine must include all major settlement-logistics mechanic families (roads, carriers, chains, worker provisioning, forest management, mine food support, upgrades, etc.) in original simplified form. It does **not** need Widelands tribes, campaigns, copied data, or full content breadth.

For the Trial Military Game specifically:

> **OpenRA-like RTS mechanic coverage, not OpenRA feature parity.**

The Trial Military Game must cover major OpenRA-like RTS mechanic families in simplified, original, deterministic, testable form — just as the Trial Economy Game covers major Widelands-like economy mechanic families in simplified original form. It does **not** need OpenRA factions, campaigns, copied YAML/traits, maps, UI, art, balance, or full content breadth.

This does **not** mean cloning Widelands, OpenRA, Command & Conquer, or Red Alert. Each family must exist in state, be tested deterministically, and appear in diagnostics where applicable — but rules, names, and balance are original.

## Traceability Requirement

Every mechanic family below must appear in:

1. A design requirement document
2. `docs/testing/01_rule_contract_test_matrix.md`
3. At least one run acceptance checklist in `docs/runs/`

---

## 1. Trial Economy Game

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Hex map (axial coords) | ECON-MAP | `02_trial_economy_game_requirements.md` |
| Terrain | ECON-TERR | `02_` |
| Height / slope | ECON-SLOPE | `02_` |
| Buildability | ECON-BUILD | `02_` |
| Resources / deposits | ECON-DEP | `02_` |
| Ownership | ECON-OWN | `02_` |
| Roads | ECON-ROAD | `02_` |
| Road nodes | ECON-NODE | `02_` |
| Pathfinding | ECON-PATH | `02_` |
| Wares | ECON-WARE | `02_` |
| HQ / Storehouse worker provisioning | ECON-WORKER | `02_` |
| Tool-based worker availability | ECON-TOOL | `02_` |
| Forest management (density, forester) | ECON-FOREST | `02_` |
| Food tiers / miner support | ECON-FOOD | `02_` |
| Mine depth / upgrade tiers | ECON-MINE | `02_` |
| Building upgrades | ECON-UPGRADE | `02_` |
| Road quality, slope, capacity | ECON-ROAD | `02_` |
| Carriers | ECON-CARR | `02_` |
| Storage (HQ, Storehouse) | ECON-STOR | `02_` |
| Production recipes | ECON-REC | `02_` |
| Production chains | ECON-CHAIN | `02_` |
| Construction sites | ECON-CONS | `02_` |
| Builder work | ECON-BUILDER | `02_` |
| Priorities | ECON-PRI | `02_` |
| Economy statistics / diagnostics | ECON-DIAG | `02_` |
| Data exports (CSV, JSONL) | ECON-EXPORT | `02_` |
| Godot-ready snapshot layers | ECON-SNAP | `02_` |
| Heuristic economy bot (smoke) | ECON-BOT | `02_` |
| Replay | ECON-REP | `02_` |
| Deterministic scenario maps | ECON-SCEN | `02_` |

**Explicitly not required for Trial Economy:** tribes, campaigns, map editor, multiplayer, ports/ships, military territory, copied Widelands content.

**Run:** RUN_02 (full engine), RUN_04 (formal bots/replay expansion)

---

## 2. Trial Military Game

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Resource fields / nodes | MIL-ECON | `03_trial_military_game_requirements.md` |
| Harvester / collector | MIL-ECON | `03_` |
| Resource processor / credits | MIL-ECON, MIL-CRED | `03_` |
| Base construction / buildability | MIL-BUILD | `03_` |
| Construction radius | MIL-BUILD | `03_` |
| Construction queue | MIL-QUEUE, MIL-BUILD | `03_` |
| Command hub | MIL-HUB | `03_` |
| Power generator / supply / demand | MIL-POWER | `03_` |
| Underpowered behaviour | MIL-POWER | `03_` |
| Barracks / vehicle plant | MIL-PROD | `03_` |
| Unit production queue | MIL-QUEUE | `03_` |
| Building prerequisites / tech tree | MIL-TECH, MIL-BUILD | `03_` |
| Radar tower | MIL-RADAR | `03_` |
| Research lab | MIL-TECH | `03_` |
| Repair depot / repair jobs | MIL-REPAIR | `03_` |
| Defence turret / static defence | MIL-TUR | `03_` |
| Infantry / vehicles / unit types | MIL-UNIT | `03_` |
| Target types | MIL-DAMAGE | `03_` |
| Movement / pathfinding / terrain | MIL-MOVE | `03_` |
| Direct unit orders (trial only) | MIL-ORDER | `03_` |
| Attack-move / patrol / guard | MIL-ORDER | `03_` |
| Combat (range, cooldown, projectiles) | MIL-COMBAT | `03_` |
| Damage / armour / type modifiers | MIL-DAMAGE | `03_` |
| Capture / sabotage (optional) | MIL-CAPTURE | `03_` |
| Veterancy (optional) | MIL-VET | `03_` |
| Fog / shroud / vision / scouting | MIL-FOG, MIL-VISION | `03_` |
| Skirmish setup | MIL-MAPGEN | `03_` |
| Deterministic map generation hooks | MIL-MAPGEN | `03_` |
| Defeat / victory conditions | MIL-DEFEAT | `03_` |
| Replay determinism | MIL-REPLAY | `03_` |
| Snapshot export | MIL-SNAP | `03_` |
| Diagnostics | MIL-DIAG | `03_` |
| Bots / smoke tests | MIL-BOT | `03_` |
| Mod / data-driven config hooks | MIL-CONFIG | `03_` |
| Scenario / campaign hooks | MIL-SCEN | `03_` |
| Superweapon / special power (optional future) | MIL-SPECIAL | `03_` |

**Run:** RUN_03, RUN_04

---

## 3. Hybrid Game

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Economy inheritance | HYB-ECON | `04_hybrid_game_requirements.md` |
| Military inheritance | HYB-MIL | `04_` |
| No instant sidebar production | HYB-NOSIDE | `04_` |
| Logistics-fed construction | HYB-LOGCON | `04_` |
| Logistics-fed unit production | HYB-LOGPROD | `04_` |
| Power integration | HYB-POWER | `04_` |
| Radar / intel integration | HYB-INTEL | `04_` |
| Research integration | HYB-TECH | `04_` |
| Delegated military control | HYB-CTRL | `04_` |
| Patrol / defence zone | HYB-CTRL | `04_` |
| Guard policy | HYB-CTRL | `04_` |
| Repair priority / logistics-fed repair | HYB-LOGREP, HYB-CTRL | `04_` |
| Defence priorities | HYB-DEF | `04_` |
| Garrison / rally | HYB-GARR | `04_` |
| Hybrid diagnostics / bots | HYB-DIAG | `04_` |

**Run:** RUN_07, RUN_08

---

## 4. Godot Visualisation

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Snapshot loading | VIS-SNAP | `06_mobile_2d_visual_design.md` |
| Layer rendering | VIS-LAYER | `06_` |
| Hex grid overlay | VIS-GRID | `06_` |
| Touch camera / selection | VIS-UX | `06_`, `07_human_play_design.md` |
| Command submission UI | VIS-CMD | `06_` |
| Diagnostics overlay | VIS-DIAG | `06_` |
| Replay timeline | VIS-REP | `06_` |

**Run:** RUN_05, RUN_09, RUN_10

---

## 5. Bots and Neural Training

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Bot command API | BOT-CMD | `docs/architecture/04_bot_and_training_architecture.md` |
| Observation from snapshot | BOT-OBS | `04_` |
| Deterministic rollouts | BOT-DET | `04_` |
| Replay verification | BOT-REP | `04_` |
| Training loop hooks | BOT-TRAIN | `04_`, `docs/testing/04_training_test_plan.md` |

**Run:** RUN_04, RUN_06, RUN_08

---

## 6. Human Play

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Touch controls | HUM-UX | `07_human_play_design.md` |
| Delegated military UI | HUM-CTRL | `07_`, `04_hybrid_game_requirements.md` |
| Economy priority UI | HUM-ECON | `07_` |
| Tutorial / onboarding | HUM-TUT | `07_` |
| In-game diagnostics | HUM-DIAG | `07_` |

**Run:** RUN_10

---

## 7. Expanded Content

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Campaign structure | EXP-CAMP | `08_expansion_design.md` |
| Progression / unlocks | EXP-PROG | `08_` |
| Additional maps | EXP-MAP | `08_` |
| Content packs | EXP-PACK | `08_` |

**Run:** RUN_11

---

## 8. Wi-Fi Multiplayer

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Command relay | NET-CMD | `09_wifi_multiplayer_design.md` |
| Deterministic sync | NET-SYNC | `09_` |
| Desync detection | NET-DESYNC | `09_` |
| Reconnect | NET-REC | `09_` |
| Packaging | NET-PKG | `09_` |

**Run:** RUN_12

---

## Completion Criteria

A mechanic family is **covered** when:

1. It is specified with at least one rule ID in a requirement doc
2. At least one test is listed in the rule contract test matrix
3. At least one run acceptance checklist item references it
4. Implementation run passes associated tests and smoke scenarios

Simplified rules are acceptable; missing families are not.

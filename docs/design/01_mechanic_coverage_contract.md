# Mechanic Coverage Contract

## Definition of "All Functionality"

**All functionality** means:

> All major mechanic families represented by simplified, original, testable systems.

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
| Workers | ECON-WORK | `02_` |
| Carriers | ECON-CARR | `02_` |
| Storage | ECON-STOR | `02_` |
| Production recipes | ECON-REC | `02_` |
| Production chains | ECON-CHAIN | `02_` |
| Construction sites | ECON-CONS | `02_` |
| Builder work | ECON-BUILDER | `02_` |
| Priorities | ECON-PRI | `02_` |
| Diagnostics | ECON-DIAG | `02_` |
| Bots | ECON-BOT | `02_` |
| Snapshot export | ECON-SNAP | `02_` |
| Replay | ECON-REP | `02_` |

**Run:** RUN_02, RUN_04 (bots/replay)

---

## 2. Trial Military Game

| Family | Rule ID prefix | Requirement doc |
|--------|----------------|-----------------|
| Command hub | MIL-HUB | `03_trial_military_game_requirements.md` |
| Power generator | MIL-POWER | `03_` |
| Resource processor | MIL-PROC | `03_` |
| Barracks / vehicle plant | MIL-PROD | `03_` |
| Radar tower | MIL-RADAR | `03_` |
| Research lab | MIL-TECH | `03_` |
| Repair depot | MIL-REP | `03_` |
| Defence turret | MIL-TUR | `03_` |
| Credits / stock | MIL-CRED | `03_` |
| Power supply / demand | MIL-POWER | `03_` |
| Production queues | MIL-QUEUE | `03_` |
| Infantry / vehicles | MIL-UNIT | `03_` |
| Movement / pathfinding | MIL-MOVE | `03_` |
| Combat (range, damage, armour) | MIL-COMBAT | `03_` |
| Target selection | MIL-TARGET | `03_` |
| Building / unit destruction | MIL-DEST | `03_` |
| Intel / visibility | MIL-INTEL | `03_` |
| Tech unlocks | MIL-TECH | `03_` |
| Defeat condition | MIL-DEFEAT | `03_` |
| Diagnostics / bots / replay | MIL-DIAG | `03_` |

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

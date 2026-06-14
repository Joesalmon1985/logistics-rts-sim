# RUN_02 — Trial Economy Game

## Purpose

Implement the **complete abstract economy engine** per `docs/design/02_trial_economy_game_requirements.md`.

Target: **Widelands-like mechanic coverage, not Widelands feature parity.** Run 02 must not become a small logistics demo.

## Branch Name

`feature/run-02-trial-economy-game`

## Prerequisite Documents

- `docs/design/02_trial_economy_game_requirements.md`
- `docs/design/01_mechanic_coverage_contract.md`
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/design/10_glossary.md`
- `docs/architecture/01_python_sim_authoritative_core.md`
- `docs/architecture/06_config_and_data_schema.md`
- `docs/testing/01_rule_contract_test_matrix.md` (ECON-* rows)
- `docs/runs/RUN_01_CORE_SIMULATION_KERNEL.md` (complete)

## In-Scope Work

All ECON-* mechanic families including:

- Hex map, terrain, height/slope, buildability, deposits, ownership
- Roads (terrain/slope legality, capacity/congestion), road nodes, pathfinding, carriers
- Wares, production chains, construction, priorities
- **HQ/Storehouse worker provisioning** (ECON-WORKER-*)
- **Tool-based worker availability** (ECON-TOOL-*)
- **Forest management** — tree density, forester, depletion/regrowth (ECON-FOREST-*)
- **Food tiers and miner support** — rations, meals (ECON-FOOD-*)
- **Mine tiers and depletion** (ECON-MINE-*)
- **Building upgrades** (ECON-UPGRADE-*)
- Economy statistics, diagnostics, CSV exports
- Godot-ready snapshot layers (headless export only)
- All 11+ production chains including rations/meals
- All required commands including `UpgradeBuildingCommand`
- All required event categories
- **Ten bundled deterministic scenarios** (§37 in requirements)
- **Heuristic economy bot** for ECON-SMOKE-005
- Replay hash verification

## Out-of-Scope Work

- Military, combat, territory conquest
- Multiplayer, campaigns, map editor
- Ports, ships, inter-player trade
- Multiple tribes
- Hybrid rules (RUN_07)
- Godot scenes (RUN_05)
- Neural training (RUN_06)
- Formal bot framework polish (RUN_04 expands heuristic bot)

## Test-First Requirements

- Implement tests from `01_rule_contract_test_matrix.md` for every ECON-WORKER, ECON-TOOL, ECON-FOREST, ECON-FOOD, ECON-MINE, ECON-UPGRADE, ECON-ROAD rule ID
- One scenario test per bundled map in §37
- ECON-DET-002 replay hash
- ECON-SMOKE-001 through ECON-SMOKE-005

## Implementation Requirements

- `economy_trial` variant fully functional headlessly
- Config-driven buildings, recipes, worker types, tool requirements, upgrades
- Generic `tools` ware in Run 02; schema supports future tool categories
- No placeholder mechanics — each family affects state, diagnostics, and tests

## Smoke-Test Requirements

| ID | Pass criteria |
|----|---------------|
| ECON-SMOKE-001 | Road network with terrain/slope; carriers path |
| ECON-SMOKE-002 | Timber → planks in storage |
| ECON-SMOKE-003 | Full building construction completed |
| ECON-SMOKE-004 | 500-tick replay hash match |
| ECON-SMOKE-005 | Heuristic bot builds **Workshop** via full logistics chain |

## Acceptance Checklist

- [ ] Coverage target met: Widelands-like families, not Widelands parity
- [ ] ECON-WORKER-001–005: HQ/Storehouse worker provisioning works
- [ ] ECON-TOOL-001–004: tool-gated worker assignment works
- [ ] ECON-FOREST-001–005: forest depletion/regrowth works
- [ ] ECON-FOOD-001–005: mine food support (rations/meals) works
- [ ] ECON-MINE-001–006: mine tiers and depletion work
- [ ] ECON-UPGRADE-001–005: at least one building upgrade works
- [ ] ECON-ROAD-001–007: roads not thin graph-only
- [ ] ECON-DIAG-*: statistics and CSV exports work
- [ ] ECON-SNAP-*: all Godot-ready snapshot layers populated
- [ ] All ten scenario maps pass
- [ ] ECON-SMOKE-001–005 pass
- [ ] Definition of done §43 in `02_` satisfied
- [ ] No military, multiplayer, campaigns, ports, or Godot in RUN_02

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

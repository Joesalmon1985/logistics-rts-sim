# RUN_11 — Expanded Content and Progression

## Purpose

Campaign structure, additional maps, content packs, and progression unlocks.

## Branch Name

`feature/run-11-expanded-content-progression`

## Prerequisite Documents

- `docs/design/08_expansion_design.md`
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/runs/RUN_10_HUMAN_PLAYABLE_HYBRID.md` (complete)

## In-Scope Work

- Campaign mission loader
- At least 3 additional maps
- Progression unlock gating (config-driven)
- EXP-SMOKE-001, EXP-SMOKE-002

## Out-of-Scope Work

- Wi-Fi multiplayer (RUN_12)
- Neural training content

## Test-First Requirements

EXP-CAMP-001, EXP-MAP-001 scenario tests with hash stability.

## Implementation Requirements

- Content data-driven; no sim fork per mission
- Determinism preserved per scenario

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_11 section.

## Acceptance Checklist

- [ ] Campaign loads mission 2 after mission 1 config
- [ ] New maps load and run with stable hash
- [ ] Unlock gating works without breaking replay
- [ ] EXP-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

# RUN_02.6 — Economy Foundation Audit and Implementation Plan

## Purpose

Audit the Trial Economy Game **logistics foundation** (workers, carriers, road nodes, building entrances, delivery jobs, ware movement, construction logistics, modular config) and produce a **decision-backed implementation plan** for RUN_02.7 — without implementing simulation changes in this run.

Target: Widelands-**style** foundation depth, not feature parity. This run is **documentation-only**.

## Branch

`feature/run-02-6-economy-foundation-audit-plan`

## Branch point

| Field | Value |
|-------|-------|
| Base branch | `main` |
| Base commit | `8b6c399953261642bbff867b7cf810ec166b4d69` |
| Rationale | RUN_02 economy and RUN_02.5 visual observer work are merged to `main`; stale `feature/run-02-*` branches superseded |

## Prerequisite documents

- `docs/design/02_trial_economy_game_requirements.md`
- `docs/design/04_hybrid_game_requirements.md` (economy logistics dependencies)
- `docs/design/05_units_resources_buildings_catalogue.md`
- `docs/design/10_glossary.md`
- `docs/architecture/02_command_event_snapshot_contract.md`
- `docs/architecture/06_config_and_data_schema.md`
- `docs/testing/01_rule_contract_test_matrix.md`
- `docs/testing/02_smoke_test_matrix.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md`
- `docs/audit/RUN_02_LESSONS_LEARNED.md`
- RUN_02.5 docs in `docs/runs/` and `docs/audit/` (visual only)

## Audit discipline

Audit hypotheses in planning notes are **not** accepted conclusions until re-verified against implementation, tests, exports, and documentation.

For every **positive claim**, identify:

- implementation evidence, or “not found”
- test evidence, or “no direct test found”
- documentation evidence, or “not documented”
- snapshot/export/viewer evidence where relevant

For every **absence claim**, record files/searches checked.

If audit evidence contradicts prior plans, **audit evidence wins**.

## Hard constraint (documentation-only)

Default is **documentation-only**. Do not modify simulation code, viewer code, exporter code, commands, or **tests** unless explicitly approved in a later run.

Do not add or alter tests in RUN_02.6 unless the audit cannot be completed without a small listing/index file (markdown inventory, not a new test harness). Document the reason before any such change.

## In-scope work

1. Read-only verification baseline (record metadata, do not fix failures).
2. Stage 1: Current-state map (logistics, modularity, export, tests).
3. Stage 2: Gap register with severity and recommendations.
4. Stage 3: Decision draft + RUN_02.7 implementation backlog.
5. Final report with decision control table.

## Out-of-scope work

- Implementing logistics foundation (RUN_02.7).
- Rewriting economy simulation.
- Civic, pandemic, power, wages, labour-market systems.
- Military hybrid integration (RUN_07).
- Viewer visual improvements.
- Snapshot-only fields without authoritative sim state.
- Neural training changes.

## Pre-flight verification (read-only)

Per `docs/verification/local_verification.md`:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
mkdir -p godot_project/.godot_user

timeout 400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
timeout 450s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
```

### Verification artefact safety

Commands may create gitignored artefacts (`godot_project/.godot_user/`, logs). **Do not commit generated outputs.**

After verification, record:

- working tree status before/after
- list of ignored/untracked artefacts produced

### Godot process hygiene

After Godot commands: `pgrep -af Godot` / `pgrep -af 'Godot.*godot_project'`. Document stale processes; kill only confirmed project headless instances per `.cursor/rules/10-run-verification-and-logging.mdc`.

### Metadata to record (per command)

git branch, commit hash, Godot path, exact command, exit code, timestamp, artefacts.

Store in [`RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md) §Verification baseline.

## Deliverables

| # | Path | Content |
|---|------|---------|
| 1 | This file | Run orchestration |
| 2 | [`docs/audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_CURRENT_STATE.md) | Stage 1 |
| 3 | [`docs/audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md`](../../audit/RUN_02_6_ECONOMY_FOUNDATION_GAP_REGISTER.md) | Stage 2 |
| 4 | [`docs/design/15_worker_carrier_flag_logistics_foundation_DECISION_DRAFT.md`](../../design/15_worker_carrier_flag_logistics_foundation_DECISION_DRAFT.md) | Stage 3 |

Optional: [`docs/audit/RUN_02_6_LESSONS_LEARNED.md`](../../audit/RUN_02_6_LESSONS_LEARNED.md)

## Next implementation run

**RUN_02.7** — economy logistics foundation hardening per decision draft backlog. **Do not start until user confirms DDR-LOG decisions.**

## Acceptance checklist (RUN_02.6)

- [x] Branch created from `main`; branch point documented
- [x] Read-only verification run; metadata recorded
- [x] Current-state doc (A–D) with evidence standard
- [x] Gap register with severity and categories
- [x] Decision draft with backlog and future acceptance criteria
- [x] No simulation/viewer/exporter/command/test code changes
- [x] Final report with decision control table

## Final report format

Per `.cursor/rules/07-final-report-format.mdc`.

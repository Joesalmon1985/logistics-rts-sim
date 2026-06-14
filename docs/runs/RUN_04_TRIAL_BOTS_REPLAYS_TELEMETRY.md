# RUN_04 — Trial Bots, Replays, and Telemetry

## Purpose

Add bots, replay export/import, and diagnostics telemetry for economy and military trials.

## Branch Name

`feature/run-04-trial-bots-replays-telemetry`

## Prerequisite Documents

- `docs/architecture/04_bot_and_training_architecture.md`
- `docs/architecture/05_determinism_replay_save_load.md`
- `docs/runs/RUN_02_TRIAL_ECONOMY_GAME.md` (complete)
- `docs/runs/RUN_03_TRIAL_MILITARY_GAME.md` (complete)
- `docs/audit/RUN_01_LESSONS_LEARNED.md`
- `docs/audit/RUN_02_LESSONS_LEARNED.md`

## Pre-flight (before any RUN_04 implementation)

Before implementing new mechanics (see `.cursor/rules/10-run-verification-and-logging.mdc`):

1. Read `docs/audit/RUN_01_LESSONS_LEARNED.md` and `docs/audit/RUN_02_LESSONS_LEARNED.md`.
2. Confirm RUN_01 kernel regression plus RUN_02 economy and RUN_03 military suites per `docs/verification/local_verification.md` — all exit 0.
3. Use `timeout` wrappers; export `GODOT_USER_DATA_DIR`; `--import` after fresh checkout or new `class_name` types.
4. Kill only confirmed stale project Godot processes; never broad `pkill -f Godot`.
5. Do not count aborted, timed-out, errored, or not-run commands as passes. If verification fails, **stop and report** before editing gameplay systems.

## In-Scope Work

- Formal bot interface (extends RUN_02 heuristic bot)
- Economy + military bots using observation/snapshot API
- Replay export/import JSON
- Telemetry/diagnostics export; bot_decision CSV
- BOT-SMOKE-*, REP-SMOKE-*
- **Per-rule-ID economy test hardening** (deferred from RUN_02) — add dedicated headless tests for each individual ECON-* rule ID listed in `docs/testing/01_rule_contract_test_matrix.md` (RUN_02 implemented coverage → Known coverage gap), cross-referenced with `docs/audit/RUN_02_LESSONS_LEARNED.md` ("Known remaining caveat"). Rule IDs include: ECON-WORKER-001/002/004/005, ECON-TOOL-001/003/004, ECON-FOREST-001/003/004/005, ECON-FOOD-001/003/004/005, ECON-MINE-002..006, ECON-UPGRADE-001/003/004/005, ECON-ROAD-003..006, ECON-PATH-002/003, ECON-PRI-002/003, ECON-CARR-*, ECON-CONS-001, ECON-DIAG-004, ECON-STALL-001, ECON-EVT-*.

Note: RUN_02 already requires a minimal heuristic economy bot for ECON-SMOKE-005. RUN_04 generalizes the bot framework and adds military bot.

## Out-of-Scope Work

- Neural training (RUN_06)
- Godot replay UI (RUN_05)
- Hybrid bots (RUN_08)

## Test-First Requirements

BOT-CMD-001, BOT-DET-001, REP-SMOKE-001.

## Implementation Requirements

- Bots are **in-engine GDScript** clients in the simulation core, using the same command validator as humans/tests (not Python; Python training is RUN_06)
- Replay files compatible with the kernel hash mechanism

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_04 section.

## Acceptance Checklist

- [ ] Economy bot completes tutorial scenario (builds on RUN_02 ECON-SMOKE-005 heuristic)
- [ ] Military bot wins skirmish scenario
- [ ] Replay export/import hash identical
- [ ] Diagnostics visible in snapshot JSON
- [ ] Deterministic bot rollouts
- [ ] Per-rule-ID economy test hardening complete for all rule IDs listed in In-Scope and in `docs/testing/01_rule_contract_test_matrix.md` (RUN_02 Known coverage gap subsection)

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

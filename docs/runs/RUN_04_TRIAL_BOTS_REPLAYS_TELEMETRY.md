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

## In-Scope Work

- Formal bot interface (extends RUN_02 heuristic bot)
- Economy + military bots using observation/snapshot API
- Replay export/import JSON
- Telemetry/diagnostics export; bot_decision CSV
- BOT-SMOKE-*, REP-SMOKE-*

Note: RUN_02 already requires a minimal heuristic economy bot for ECON-SMOKE-005. RUN_04 generalizes the bot framework and adds military bot.

## Out-of-Scope Work

- Neural training (RUN_06)
- Godot replay UI (RUN_05)
- Hybrid bots (RUN_08)

## Test-First Requirements

BOT-CMD-001, BOT-DET-001, REP-SMOKE-001.

## Implementation Requirements

- Bots use same command validator as tests
- Replay files compatible with kernel hash mechanism

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_04 section.

## Acceptance Checklist

- [ ] Economy bot completes tutorial scenario (builds on RUN_02 ECON-SMOKE-005 heuristic)
- [ ] Military bot wins skirmish scenario
- [ ] Replay export/import hash identical
- [ ] Diagnostics visible in snapshot JSON
- [ ] Deterministic bot rollouts

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

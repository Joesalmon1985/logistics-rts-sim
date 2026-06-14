# RUN_08 — Hybrid Bots, Telemetry, and Balance

## Purpose

Hybrid bots, extended telemetry, and balance smoke scenarios for headless hybrid.

## Branch Name

`feature/run-08-hybrid-bots-telemetry-balance`

## Prerequisite Documents

- `docs/design/04_hybrid_game_requirements.md`
- `docs/runs/RUN_07_HEADLESS_HYBRID_GAME.md` (complete)
- `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` (complete)

## In-Scope Work

- Hybrid bot using delegated + economy commands only
- Balance scenario fixtures (two-player)
- HYB-BOT-001, BAL-SMOKE-001
- Enhanced hybrid diagnostics

## Out-of-Scope Work

- Godot (RUN_09)
- Campaign content (RUN_11)

## Test-First Requirements

HYB-BOT-001 scenario; no stall deadlock in 200-tick run.

## Implementation Requirements

- Bot observes same snapshot as human would
- No privileged intel

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_08 section.

## Acceptance Checklist

- [ ] Hybrid bot completes 200 ticks without unrecoverable stall
- [ ] Balance scenario playable by two bots
- [ ] Delegated orders only from bot
- [ ] HYB-BOT-001, BAL-SMOKE-001 pass
- [ ] RUN_08 DoD items in `04_` §23 satisfied

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

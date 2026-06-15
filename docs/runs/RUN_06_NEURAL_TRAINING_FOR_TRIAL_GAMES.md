# RUN_06 — Neural Training for Trial Games

## Purpose

Training pipeline for trial games using the bot API and snapshots. **Python is the trainer, not the engine**: it drives the authoritative GDScript simulation core by running **Godot headless** and exchanging commands/snapshots/replays (development-time transport, DDR-033). There is no Python simulation package to import.

## Branch Name

`feature/run-06-neural-training-trial-games`

## Prerequisite Documents

- `docs/architecture/04_bot_and_training_architecture.md`
- `docs/testing/04_training_test_plan.md`
- `.cursor/rules/08-neural-training-gates.mdc`
- `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` (complete)

## Pre-RUN_06 entry checklist

Before starting `feature/run-06-neural-training-trial-games`:

- [ ] Re-run RUN_01–04 verification on `main` (per `.cursor/rules/10-run-verification-and-logging.mdc` and `docs/verification/local_verification.md`)
- [ ] RUN_05 closure report exists and all three tracks green — [`docs/audit/RUN_05_FINAL_REPORT.md`](../audit/RUN_05_FINAL_REPORT.md)
- [ ] Read `docs/architecture/04_bot_and_training_architecture.md` and `docs/testing/04_training_test_plan.md`
- [ ] Confirm: training observations from **snapshots**, not viewport pixels (RUN_05 pixel tests are viewer gates only)
- [ ] No ML dependencies added until RUN_06 scope approved

## In-Scope Work

- Python↔Godot-headless training transport (DDR-033): launch/control the core headlessly and exchange commands/snapshots/replays
- Observation encoder from exported snapshots
- Action space for economy and military trials
- Training loop skeleton in `python_tools/training/`
- Checkpoint output to gitignored dirs
- TRN-SMOKE-001, TRN-SMOKE-002

**RUN_02 carryover:** Observation encoders must consume exported economy snapshot and diagnostics fields from RUN_02 (`ECON-SNAP-002`, `ECON-DIAG-001` families) — not Godot scene or render state.

## Out-of-Scope Work

- Hybrid training (extend in RUN_08 if needed)
- Production model quality
- Godot integration

## Test-First Requirements

Per `docs/testing/04_training_test_plan.md`.

## Implementation Requirements

- Python never computes gameplay rules; it submits commands and reads snapshots/events from the GDScript core
- Deterministic rollouts mandatory (seed the policy's own sampling RNG)
- Replay equivalence after rollout (verified by re-running the core)

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_06 section.

## Acceptance Checklist

- [ ] Prerequisites from neural training gates met
- [ ] Python drives the GDScript core via Godot headless (no Python sim package; DDR-033)
- [ ] 10 rollouts identical command logs
- [ ] Replay hash matches after rollout
- [ ] Weights not committed to git
- [ ] TRN-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

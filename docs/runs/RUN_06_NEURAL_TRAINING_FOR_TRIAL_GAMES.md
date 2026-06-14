# RUN_06 — Neural Training for Trial Games

## Purpose

Training pipeline for trial games using bot API and snapshots.

## Branch Name

`feature/run-06-neural-training-trial-games`

## Prerequisite Documents

- `docs/architecture/04_bot_and_training_architecture.md`
- `docs/testing/04_training_test_plan.md`
- `.cursor/rules/08-neural-training-gates.mdc`
- `docs/runs/RUN_04_TRIAL_BOTS_REPLAYS_TELEMETRY.md` (complete)

## In-Scope Work

- Observation encoder from snapshot
- Action space for economy and military trials
- Training loop skeleton
- Checkpoint output to gitignored dirs
- TRN-SMOKE-001, TRN-SMOKE-002

## Out-of-Scope Work

- Hybrid training (extend in RUN_08 if needed)
- Production model quality
- Godot integration

## Test-First Requirements

Per `docs/testing/04_training_test_plan.md`.

## Implementation Requirements

- Deterministic rollouts mandatory
- Replay equivalence after rollout

## Smoke-Test Requirements

Per `docs/testing/02_smoke_test_matrix.md` RUN_06 section.

## Acceptance Checklist

- [ ] Prerequisites from neural training gates met
- [ ] 10 rollouts identical command logs
- [ ] Replay hash matches after rollout
- [ ] Weights not committed to git
- [ ] TRN-SMOKE-001–002 pass

## Final Report Format

Per `.cursor/rules/07-final-report-format.mdc`.

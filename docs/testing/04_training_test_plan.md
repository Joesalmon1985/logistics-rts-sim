# Training Test Plan

Scope: RUN_06 — neural training for trial games. Gates in `.cursor/rules/08-neural-training-gates.mdc`.

## Prerequisites

- RUN_01–04 complete with replay hash tests passing
- Bot API stable
- Observation encoder specified
- Python↔Godot-headless training transport chosen (DDR-033): Python drives the authoritative GDScript core via Godot headless; there is no Python simulation package to import

## Test Categories

### Deterministic Rollouts (BOT-DET-001)

- Same seed + same policy (rule-based stub) → identical command log
- 10 episodes × 100 ticks: all hashes match reference

### Replay Equivalence (TRN-SMOKE-002)

- Record rollout commands → replay in sim → hash equals live rollout final hash

### Observation Encoding

- Encoder uses exported snapshot fields only
- Same snapshot → same observation vector
- No hidden state leakage: the encoder consumes only exported snapshots (it has no access to internal core state, since the core runs in Godot headless behind the transport)

### Action Space

- Economy trial: only ECON commands in action space
- Military trial: MIL commands including direct unit control
- Hybrid (later RUN): excludes forbidden player commands

### Reward Signal (RUN_06 defines)

- Reward computed from exported events/snapshots post-tick, not from rendered scene state
- Deterministic given event log

### Training Smoke (TRN-SMOKE-001)

- Short training loop (1 epoch, tiny net) completes without crash
- Checkpoints written to gitignored path

## Non-Goals

- Production-quality model accuracy in RUN_06
- GPU-specific tests in CI

## Pass Criteria

- [x] TRN-SMOKE-001, TRN-SMOKE-002, TRN-SMOKE-003 pass
- [x] TRN-IMPROVE-ECON-001 / TRN-IMPROVE-MIL-001 pass via held-out BC accuracy (seeds 13 / 23)
- [x] No training before prerequisites documented in run report

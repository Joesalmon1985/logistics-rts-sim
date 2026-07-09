# RUN_NEURAL_HYBRID_4P_AI — Current State Audit (Phase A.5)

Branch at audit: `feature/neural-hybrid-prereq-audit`  
Date: 2026-06-28

## Prerequisite verification

| Check | Status | Notes |
|-------|--------|-------|
| RUN_01 kernel tests | **partial** | 330 tests; `test_ECON_BOT_002_dual_bot_expansion_metrics` failed in this session (pre-existing flaky metric) |
| Failure-mode integrity | **passed** | `verify_failure_modes.gd` exit 0 |
| RUN_06 trial neural scaffold | **complete** | `docs/audit/RUN_06_FINAL_REPORT.md` |
| RUN_07 hybrid sim | **exists** | `HYB-SMOKE-001`–`009` in harness |
| RUN_08 hybrid bots | **partial** | `TutorialHybridBot` + 2P spectator bots; formal HYB-BOT checklist open |
| RUN_10 mobile hybrid | **partial** | Desktop hybrid MVP; mobile profiles not yet implemented |

## PRD readiness questions

| Question | Answer |
|----------|--------|
| Can a scripted hybrid bot complete the hybrid objective? | **Yes** — `TutorialHybridBot` completes MVP objective via `run_tutorial_hybrid_bot_smoke.gd` |
| Can four scripted bots run in one match? | **After Run 0 spike** — `QuadBotRunner` + `hybrid_4p_training_ffa.json` added; `hybrid_init.gd` already supports N players |
| Can a four-player hybrid scenario load? | **Yes** — `hybrid_4p_training_ffa.json` |
| Can a bot observe only its own legal/intel state? | **Partial** — `SnapshotExporter.export_bot_observation_snapshot` per player; fog enabled in 4P config; hybrid legal enumeration added in `HybridCommandAvailability` |
| Can legal actions be enumerated or approximated? | **Yes (macro layer)** — `HybridAiActionSpace` macro mask; trial `CommandAvailabilityProvider` extended for hybrid variant |
| Can rollouts be saved? | **Yes** — RUN_04 `ReplayRecorder` + RUN_06 rollout artefacts |
| Can a match end with one remaining player? | **Yes** — `HybridFfaObjective` sole-remaining-player evaluator |

## Existing inventory

### Hybrid bots (scripted)
- `godot_project/sim/hybrid/bot/tutorial_hybrid_bot.gd`
- `godot_project/sim/hybrid/bot/hybrid_spectator_bot_a.gd`, `hybrid_spectator_bot_b.gd`

### Training (trial only)
- `python_tools/training/` — BC, policy eval, Godot headless transport
- **Missing before this run:** `python_tools/training/hybrid_ai/`

### Spectators
- **3D 2-bot MRP:** `hybrid_3d_bot_spectator.tscn` — full stack
- **2D 4P:** added in Run 1 — `hybrid_bot_spectator_2d.tscn`

### Scenarios
- All prior hybrid JSON scenarios: 2-player
- **Added:** `hybrid_4p_training_ffa.json`, `hybrid_4p_human_vs_neural_ffa.json`

## Go / no-go

**GO** for PRD implementation Runs 1–6 on this branch sequence.

Known blockers to monitor:
1. Pre-existing `ECON_BOT_002` failure — does not block hybrid 4P work; investigate separately.
2. Part D requires trained `ModularNeuralHybridBot` checkpoints — heuristic fallback unacceptable for PRD-complete claim.
3. Mobile real-window QA depends on `$DISPLAY`; SubViewport fallback documented when unavailable.

# Actual Training Current State (post RUN_ACTUAL_MODULAR_NN_TRAINING_AND_SPECTATOR_EVAL)

Date: 2026-06-28  
Branch: `feature/run-actual-modular-nn-training`

## Checkpoint files

| Path | Exists |
|------|--------|
| `python_tools/training/hybrid_ai/checkpoints/latest.pt` | **Yes** (57 KB) |
| `godot_project/sim/hybrid/ai/checkpoints/latest_modular_policy.json` | **Yes** (280 KB) |
| `docs/audit/neural_hybrid_ai/training/latest_checkpoint_manifest.json` | **Yes** |

## ModularNeuralHybridBot

- Loads exported JSON via `HybridNeuralInference`
- `use_trained_checkpoint = true` by default
- Telemetry fields: `inference_mode`, `checkpoint_loaded`, `heuristic_fallback`, `checkpoint_meta`
- Falls back to heuristic only when checkpoint missing or inference fails

## Python training

- `train_modular.py` collects 1440 teacher rows (3 seeds × 120 ticks × 4 players) then trains M1–M7
- Teacher source: heuristic modular experts (`use_heuristic_teacher = true` during collection)
- PyTorch runs on CPU (CUDA driver mismatch warning; training still completes)

## Dependencies

| Package | Status |
|---------|--------|
| torch | `.venv` 2.12.1+cu130 |
| numpy | available |
| pytest | available |

## Evaluation summary

| Eval | Trained neural used | Heuristic fallback |
|------|---------------------|--------------------|
| 1 neural vs 3 passive | p1 `trained_neural` | none on p1 |
| 1 neural vs 3 weak | p1 `trained_neural` | p2–p4 heuristic teacher (expected) |
| 4 neural FFA | all 4 `trained_neural` | 0 / 800 decisions |
| 4 neural spectator | all 4 `trained_neural` | 0 in sampled telemetry |

## Spectator screenshots

- Captured at 1920×1080 under `docs/audit/neural_hybrid_ai/visual/trained_neural_spectator/`
- Requires Godot **with DISPLAY** (non-`--headless`); pure headless SubViewport capture returns 1×1 placeholder

## Go / no-go

**GO for trained-neural inference path.** Policy quality is imitation-grade (early curriculum); matches did not reach elimination within 200 ticks.

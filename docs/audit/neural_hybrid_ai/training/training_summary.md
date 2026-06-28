# Modular Hybrid Training Summary

- Teacher rows: 1440 (`docs/audit/neural_hybrid_ai/datasets/modular_teacher_traces.jsonl`)
- PyTorch checkpoint: `python_tools/training/hybrid_ai/checkpoints/latest.pt`
- Godot export: `godot_project/sim/hybrid/ai/checkpoints/latest_modular_policy.json`
- Manifest: `docs/audit/neural_hybrid_ai/training/latest_checkpoint_manifest.json`
- Stages: M1–M7 imitation + tuning; M8/M9 eval stretch (see `m8_*` / `m9_*` results JSON)

## M5 arbiter imitation
- accuracy: 0.492

## Runtime verification
- `ModularNeuralHybridBot` reports `inference_mode=trained_neural` with `heuristic_fallback=false` when checkpoint present
- 4P FFA eval: 800/800 decisions trained neural, 0 fallback
- Spectator HUD shows trained-neural intent lines (tests `test_hybrid_spectator_013`, `test_neural_hybrid_052`)

## Capture note
Spectator PNGs require Godot with `DISPLAY` (non-headless). Pure `--headless` SubViewport capture is 1×1 on this platform.
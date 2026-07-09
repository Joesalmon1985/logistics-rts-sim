# RUN_ACTUAL_MODULAR_NN_TRAINING_AND_SPECTATOR_EVAL

Branch: `feature/run-actual-modular-nn-training`

## Purpose

Train hierarchical modular neural policies (M1–M7+), export Godot-loadable checkpoints, wire `ModularNeuralHybridBot` to use trained weights, and evaluate/watch 4P trained bots in the 2D spectator.

## Out of scope

- Additional mobile/FFA product scaffolding unless required for eval
- Claiming PRD Part D complete

## Acceptance

- [ ] `ACTUAL_TRAINING_CURRENT_STATE.md` audit
- [ ] Teacher datasets under `docs/audit/neural_hybrid_ai/datasets/`
- [ ] Real `.pt` checkpoints + Godot JSON weights
- [ ] M1–M5 imitation + M6–M7 tuning artefacts
- [ ] `ModularNeuralHybridBot.use_trained_checkpoint` loads weights
- [ ] 4 trained neural bots in 2D spectator with screenshots
- [ ] Tests fail on heuristic-only mode when checkpoint expected

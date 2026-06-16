# RUN_02.5b — Visual Output Quality Pass

Follow-up to RUN_02.5 focused on human-readable rollout review output (screenshots, panels, overlays, manifests). Does **not** redesign rollout architecture or add live neural inference.

## Issue 10 classification — `eval_build_obvious`

**Classification:** (a) intended scenario design + (d) misleading scenario name.

The scenario JSON declares `"victory": {"type": "worker_policy", "policy": "auto"}`. `PolicyEpisodeResult` awards win when the actor's worker policy matches — this is documented RUN_06b eval design, not a reward bug. The name `eval_build_obvious` suggests building completion but the eval actually tests legal worker-policy setup. Follow-up rename or a separate `complete_building` scenario belongs in RUN_06b/RUN_07 scoring hygiene, not this visual pass.

Test: `test_eval_build_obvious_worker_policy_victory.gd`

## Capture

```bash
# Single rollout
$GODOT --path godot_project --resolution 1920x1080 \
  --script res://sim/tools/run_rollout_review_capture.gd \
  -- --rollout-path=training_runs/visual_rollouts/economy_random_s11 \
     --output=training_runs/visual_rollouts/economy_random_s11/screenshots \
     --steps=0,10,30

# Batch (all comparable rollouts)
python3 -m python_tools.training.visual_review_batch --export-missing
```

Outputs (gitignored): per-rollout `capture_manifest.json`, `contact_sheet.html`, overall manifest under `training_runs/visual_rollouts/_review/`.

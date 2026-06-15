# RUN_02.5 Lessons Learned

## What worked

- Documentation-first Phase A gate prevented UI work before alignment/schema decisions were pinned
- N+1 snapshot/command alignment rule with explicit panel labels avoids neural review confusion
- Reusing `_op_snapshot` and RUN_05 `LayerRenderer` kept viewer non-authoritative
- Minimal committed fixture enables headless VIS-ROLL tests without gitignored rollouts

## What broke / lessons

- Godot observer must init `RolloutController` before `load_rollout_path` when loading from capture harness (pre-`_ready`)
- Export must create empty `events.jsonl` when no events occur
- Visual capture is slow (~40s per step×mode batch at 1080×720); use reduced step sets in CI
- Python export requires full permissions (Godot subprocess + session dir outside sandbox)

## Verification

- 164/164 Godot tests pass (includes VIS-ROLL-001–004)
- GOD-SMOKE-001–003 pass; VIS-ROLL-005 smoke pass
- GOD-ROLL-001 pass on fixture and economy/military rollouts
- pytest schema + TRN-SMOKE deterministic rollout pass

## Recommended next run

RUN_09 (hybrid observer) can reuse rollout timeline and mode presets.

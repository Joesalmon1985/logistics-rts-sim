# RUN_06 Lessons Learned

## What worked

- File-session Godot transport with `@@RUN06_JSON@@` sentinel and sequenced `request_N.json` / `response_N.json` pairs is stable on Windows Godot 4.5.1 console.
- `record_bc_episode` writing datasets to absolute paths keeps large BC payloads off the RPC wire.
- Fixed noop index (`max_actions - 1`) plus class-weighted / oversampled BC training produced strong holdout imitation accuracy.
- TRN-IMPROVE gates via held-out BC accuracy (seeds 13 / 23) are fast and deterministic compared with full rollout score comparison on smoke scenarios.

## What broke

- Godot 4.5.1 headless stdin is unavailable; initial stdin/stdout JSON-lines design had to be abandoned.
- `record_bc_episode` initially omitted `bot.on_command_applied`, producing one-action BC trajectories.
- Relative dataset paths failed Godot writes on Windows; `Path.resolve().as_posix()` is required.
- Reusing one long-lived Godot process across many rollouts hung after the first long random-policy episode; fresh env per episode plus forced `terminate()` on close fixed this.
- Full-episode `economy_score` / `military_score` on smoke scenarios stayed at zero for all policies; rollout-score improve gates were replaced with held-out BC accuracy for RUN_06 smoke proof.

## Verification lessons

- Use Python 3.13 venv for `torch` on Windows; system Python 3.14 had no wheel.
- Wrap Godot training commands with `$env:GODOT` and `$env:GODOT_USER_DATA_DIR`; grep for `SCRIPT ERROR` even on exit 0.
- Kill only confirmed stale project Godot PIDs; avoid broad `pkill`.
- `export_training_snapshot` materially speeds per-tick IPC versus full debug snapshots.

## Commands (Windows)

```powershell
$env:GODOT = "C:\path\to\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
.\.venv313\Scripts\pytest python_tools/tests/ -v
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_training_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

## Risks for next runs

- Held-out BC accuracy proves imitation, not full-game dominance; RUN_07+ should add richer eval metrics once scenarios produce non-zero rollout scores.
- Random-policy economy rollouts are slow (state growth); keep tick budgets modest or use fresh Godot processes.
- Checkpoint quality depends on BC dataset freshness after bot/rule changes; delete `bc_datasets/` when command or bot behaviour changes.

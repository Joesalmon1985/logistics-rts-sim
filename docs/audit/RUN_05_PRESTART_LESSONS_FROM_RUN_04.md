# RUN_05 Prestart — Lessons Carried Forward from RUN_04

Purpose: a short, actionable handoff so RUN_05 (Godot 2D trial viewers) starts on the lessons
already paid for in RUN_04. Read this together with `docs/audit/RUN_04_LESSONS_LEARNED.md` and
`docs/verification/local_verification.md` ("Verification environments").

## Environment and execution

- **Use the Godot console executable for headless/CLI on Windows.** The `_console.exe` variant
  returns proper stdout and exit codes; the plain `.exe` does not. Example:
  `C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe`.
- **Linux Godot 4.5.2 remains the reference environment.** Windows Godot 4.5.1 console is a
  supported verification environment; never claim Linux verification ran from Windows.
- **Check `$LASTEXITCODE` after every command.** Linux `timeout`/`pgrep`/`pkill`/`export` do not
  translate to PowerShell. Use `$env:GODOT`, `& $env:GODOT ...`.
- **Grep captured output for `SCRIPT ERROR` even when exit code is 0.** A clean exit code is not
  proof of a clean run; RUN_04 found non-fatal `SCRIPT ERROR` lines behind exit 0 smokes.
- **Set `GODOT_USER_DATA_DIR` to a project-local, gitignored path** before runs.

## Process safety (critical for RUN_05 windowed runs)

- Windowed Godot runs (needed for 1920x1080 visual review) can leave stale/orphaned processes.
- Inspect with `Get-Process | Where-Object { $_.ProcessName -like "*Godot*" }` before launching.
- Never broad-kill (`pkill -f Godot`) unrelated Godot processes; scope to this project and report
  matched PIDs first. Document any manual close/cleanup if a windowed run does not exit cleanly.

## Cross-platform paths

- **No hardcoded `/tmp`.** RUN_04 added `SimPaths` (`sim/core/sim_paths.gd`) which resolves an
  OS-safe export/temp dir (`OS.get_cache_dir()/wide_rts_export/`) and ensures parent dirs exist.
  RUN_05 demo/snapshot/screenshot exports must use `SimPaths` or `user://`, never `/tmp`.
- Screenshot convention for RUN_05: `user://screenshots/run_05_economy_demo.png`,
  `user://screenshots/run_05_military_demo.png`.

## GDScript pitfalls observed

- Avoid method/name collisions with Godot built-ins (RUN_04 hit `load()` shadowing → renamed to
  `load_from_path`). Do not name viewer adapter methods `load`.
- Nested-class `.new()` returned null in Godot 4.5 (RUN_04 `ReplayFile.ReplayRecorder`); prefer
  standalone `class_name` scripts for instantiable types.
- Guard nullable lookups: RUN_04 added a null-tile guard in `snapshot_exporter.gd` for sparse map
  files. The viewer's renderer must likewise tolerate missing/empty layers without crashing.

## Snapshot/adapter discipline (directly relevant to viewer work)

- **Render from snapshot/diagnostics only.** Verify field names against exporter output — RUN_04's
  economy bot bug came from reading `q`/`r` on buildings when the snapshot uses
  `anchor_q`/`anchor_r`. The viewer must read the real exported fields
  (`SnapshotExporter` / `MilitarySnapshotExporter`).
- **Never mutate replay/scenario objects on import.** Replay files embed full scenario objects;
  the viewer must treat loaded snapshots/replays as read-only.
- **The client must not recompute rules** (logistics, fog/vision, combat, pathfinding) or hold
  authoritative state. Legality is decided only by the authoritative `sim/` path.

## Non-authoritative data guardrail (new for RUN_05)

- RUN_05 introduces `available_commands[]` for the viewer. It is a **client/debug/export
  affordance, not authoritative state.** It must not enter replay-hashed state unless deliberately
  and explicitly approved. If `SnapshotExporter` output feeds `ReplayHash`, exclude
  `available_commands[]` from hashed snapshots or expose it via a separate client/debug export
  path. **Do not re-pin hashes** merely because command availability was added.

## Reporting and tracked artifacts

- Report test counts from `run_tests.gd` output only; use honest status categories (passed /
  failed / errored / timed out / not run) and never collapse them.
- `.gd.uid` files are tracked in this repo. After `--import` generates new ones for RUN_05 scripts,
  commit them with the change; do not leave the tree dirty.
- Final report must include exact commands, exit codes, and (for RUN_05) windowed-review launch
  commands plus screenshot paths or instructions.

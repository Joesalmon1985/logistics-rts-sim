# Local Verification — RUN_01 + RUN_02 + RUN_03 + RUN_04

Godot version used for verification: **4.5.2.stable.official** (`6ce3de25a`).

Godot binary path on this machine:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

If `godot` is on `PATH`, substitute `godot` for `$GODOT`.

## Verification environments (Linux vs Windows)

This project may be worked on from two kinds of machine. Know which one you are on
before claiming any result.

### Linux/Godot verification environment (authoritative full suite)

The usual full runtime verification environment is **Linux with Godot 4.5.2**, configured like:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

Linux verification uses the documented `timeout`-wrapped commands in this file and can run the
**full** suite: `run_tests.gd`, kernel smokes, economy smokes, military smokes, bot/replay smokes,
and failure-mode integrity checks. This is the only environment that may report a full-suite pass.

### Windows review environment (Godot 4.5.x console)

Windows may be used for: reading docs, reviewing branches, checking git status, checking
acceptance checklists, markdown/documentation cleanup, and running verification when a
compatible Windows Godot 4.5.x **console** executable is available (use the `_console.exe`
variant for correct stdout and exit codes in PowerShell).

**Verified on Windows (Godot 4.5.1 console, RUN_04 portability closure):** full suite passes —
148/148 tests, kernel/economy/military/bot smokes, and failure-mode integrity (subprocess checks
exit `1`, not vacuous `127`). No `SCRIPT ERROR` lines. Example console path:

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
```

**Linux Godot 4.5.2 remains the reference environment.** Windows 4.5.1 verification does not
claim Linux verification has passed, but a clean Windows pass with no blocking discrepancy is
merge-supporting.

If Godot is not available on Windows, report exactly:

```text
Full Godot runtime verification was not run on Windows because Godot 4.5.x was not available. This branch still needs Linux/Godot verification before merge.
```

### Windows command differences

Linux commands such as `timeout 120s ...`, `pgrep`, `pkill`, and `export GODOT=...` do **not**
directly translate to standard PowerShell. Where Windows verification is possible (a Godot 4.5.x
exe is present), use PowerShell-style environment variables and invocation, and check
`$LASTEXITCODE` after every command:

```powershell
$env:GODOT = "<full path to Godot executable>"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --version
& $env:GODOT --headless --path godot_project --import
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
$LASTEXITCODE
```

Notes:

- No shell-level GNU `timeout` wrapper exists in standard PowerShell; rely on the command
  runner's timeout and report that explicitly. A command that could not run on Windows is
  **not** a pass.
- For stale processes on Windows, inspect with `Get-Process | Where-Object { $_.ProcessName -like "*Godot*" }`;
  never broad-kill. Stop a process only if it is clearly stale and clearly belongs to this work.
- **No `SCRIPT ERROR` lines** are allowed even when exit code is `0`. Grep captured output
  (`2>&1 | Select-String "SCRIPT ERROR"`).
- **Cross-platform export paths:** tests and tools use `SimPaths` (`sim/core/sim_paths.gd`) —
  `OS.get_cache_dir()/wide_rts_export/`. Do not hardcode `/tmp/...`.
- **`verify_failure_modes.gd`** spawns Godot directly via `OS.execute` (no `bash`). Negative
  subprocess checks must exit `1` (intended failure), not `127` (executable not found / vacuous).

### Merge policy

A branch should not be merged to main on the basis of Windows documentation review alone. If full
runtime verification was not run successfully on Windows, rerun the full Godot verification on the
Linux/Godot machine before merging.

## Exit codes and timeout policy

| Exit code | Meaning |
|-----------|---------|
| `0` | Pass — command completed and reported success |
| `1` | Fail — tests/smokes/integrity checks reported failure |
| `124` | **Timed out** (`timeout` on Linux) — treat as **failure**, never as pass |
| (signal/killed) | Aborted or hung process — treat as **failure**, never as pass |

**Never report a pass** unless:

- the command **completed** (not aborted, not killed, not timed out)
- exit code was **`0`**
- the printed summary shows the expected pass counts
- you are reading output from **this run**, not stale terminal output

Import success (`--import`) is **not** test success.

## Stale Godot processes

Before rerunning verification, list Godot processes safely:

```bash
pgrep -af Godot || true
pgrep -af godot || true
```

Kill only stale headless Godot processes for this project if necessary (verify PID/command line first):

```bash
pkill -f 'Godot.*WideRTS' || true
pkill -f 'Godot.*godot_project' || true
```

**Warning:** never run a broad unscoped `pkill -f Godot` / `pkill -f godot`. It can kill the user's unrelated Godot editor or other projects. Always scope the pattern to this project and confirm the matched PIDs with `pgrep -af` first.

A Cursor "task aborted" or killed task is **not** a pass — it is a not-run/failed result. Discard its stdout; rely on a clean rerun's exit code and `EXIT:` line.

## First-time project import

Run once after cloning or when adding new GDScript `class_name` types:

```bash
mkdir -p godot_project/.godot_user
timeout 120s "$GODOT" --headless --path godot_project --import
```

Import registers `class_name` types. Without import, test scripts may fail to parse.

## Headless unit/integration tests (154 tests — RUN_01 through RUN_05)

```bash
timeout 400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
echo "exit=$?"
```

Expected output includes:

```text
Results: 154/154 ran, 154 passed, 0 failed, 0 errored
EXIT: 0 (pass)
```

Exit code must be `0`. A clean run must print **no** `SCRIPT ERROR` lines in stdout/stderr.

The test harness (`sim/tests/test_case.gd`) marks a test as incomplete when `_run_body()` aborts
before returning (e.g. a script error mid-test). Such tests report as **errored**, not passed.

## Headless smoke tests (KERN-SMOKE-001–004)

Economy trial (default scenario):

```bash
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
echo "exit=$?"
```

Military trial:

```bash
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
echo "exit=$?"
```

Hybrid:

```bash
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
echo "exit=$?"
```

Expected: all four `KERN-SMOKE-*` lines pass, `EXIT: 0 (pass)`, exit code `0`.

For `economy_trial`, KERN-SMOKE-002 checks `economy_initialized` and tick count (not `tick_advanced`).

Pinned economy hash in `scenarios/kernel_smoke.json` (RUN_02):

`sha256:1d69994a924390478e89397dc3e0ff9b5d65118eeb2995fe1de8c53a5898332b`

Pinned military hash in `scenarios/kernel_smoke_military.json` (RUN_03, re-pinned after
`spawn_building` began carrying the building `weapon` field; triple-run stable):

`sha256:5ce6102c91f7ad9191d6b25442d72170b82904ccc0be1b78be8183e5bfc3e08f`

## Military smoke tests (MIL-SMOKE-001–009)

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_military_smoke.gd -- --smoke=all --seed=1
echo "exit=$?"
```

Single smoke (e.g. the deterministic skirmish + export):

```bash
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_military_smoke.gd -- --smoke=009 --seed=1 --export-dir=/tmp/mil_export
echo "exit=$?"
```

Expected: `MIL SMOKE PASS`, `EXIT: 0 (pass)`. The whole suite runs in well under 30s on the
reference machine. MIL-SMOKE-009 asserts a deterministic replay hash (two runs from the
same seed/scenario must match) and writes snapshot/events/metrics/unit/building exports.

## Economy smoke tests (ECON-SMOKE-001–005)

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
echo "exit=$?"
```

Single smoke:

```bash
timeout 450s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd -- --smoke=005
echo "exit=$?"
```

Expected: `ECON SMOKE PASS`, `EXIT: 0 (pass)`.

Expected: `ECON SMOKE PASS`, `EXIT: 0 (pass)`.

## Bot and replay smoke tests (BOT-SMOKE-001/002, REP-SMOKE-001)

```bash
timeout 600s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd -- --smoke=all --seed=42
echo "exit=$?"
```

Single smoke:

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd -- --smoke=001 --seed=42
echo "exit=$?"
```

Expected: `BOT SMOKE PASS`, `EXIT: 0 (pass)`. BOT-SMOKE-001 completes `tiny_success_map` via
`HeuristicEconomyBot`; BOT-SMOKE-002 wins `skirmish_1v1` via `HeuristicMilitaryBot`;
REP-SMOKE-001 (smoke id `003`) export → import → identical hash.

## Replay CLI (export / verify)

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_replay.gd -- --mode=export-economy-bot --scenario=scenarios/economy/tiny_success_map.json --seed=42 --ticks=200 --export=/tmp/run04_replay.json
echo "exit=$?"

timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_replay.gd -- --mode=verify --replay=/tmp/run04_replay.json
echo "exit=$?"
```

Expected: export exit `0`, verify prints `REPLAY PASS` and exit `0`.

## Failure-mode integrity checks

Verifies invalid variant, hash mismatch, invalid ticks, invalid economy smoke id,
**invalid military smoke id**, **invalid military scenario**, **invalid bot smoke id**, and
**invalid replay verify** all **exit non-zero**:

```bash
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
echo "exit=$?"
```

Expected: `INTEGRITY PASS: all failure-mode checks behaved correctly`, exit code `0`.

### Manual negative checks (optional)

These must **fail** (non-zero exit):

```bash
# Invalid variant
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=not_a_variant --ticks=10 --seed=1
echo "exit=$?"   # expect non-zero

# Hash mismatch override
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1 --expected-hash=sha256:definitely_wrong
echo "exit=$?"   # expect non-zero

# Invalid ticks
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=0 --seed=1 --no-hash-check
echo "exit=$?"   # expect non-zero
```

## Repository hygiene

```bash
git diff --check
git status --short
```

## Test harness (DDR-032)

Gameplay-rule tests use a **lightweight custom Godot headless runner** (`sim/tools/run_tests.gd`). No GUT/gdUnit4 addon. Tests are pure GDScript `RefCounted` classes under `sim/tests/`.

The runner distinguishes **passed**, **failed** (assertion), and **errored** (load/instantiate/script error). It exits non-zero if any test failed or errored, or if the run count does not match the expected suite size.

## RUN_05 windowed visual review (Track 2 — human readability)

RUN_05 viewers need **three tracks** of verification (see `docs/testing/03_godot_viewer_test_plan.md`):

- **Track 1 — headless contract smokes** (GOD-SMOKE-001/002/003): run with `--headless`, exit `0`, no
  `SCRIPT ERROR`. These prove the snapshot/availability/command contracts, **not** that the viewport shows a map.
- **Track 1.5 — viewport pixel smokes** (GOD-SMOKE-004, GOD-CAPTURE-001): run **without** `--headless`;
  requires display. Proves SubViewport terrain and step/command pixel changes.
- **Track 2 — windowed visual review**: launch each demo scene in a real Godot **window** at
  1920x1080 and confirm the layout is human-readable. Manual review checklist below.

Launch the demo scenes in a window (do **not** pass `--headless`):

```powershell
# Windows (Godot 4.5.x console exe)
$env:GODOT = "<full path to Godot console executable>"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
& $env:GODOT --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
$LASTEXITCODE
```

```bash
# Linux (Godot 4.5.2 reference)
"$GODOT" --path godot_project --resolution 1920x1080 res://client/scenes/economy_viewer_demo.tscn
"$GODOT" --path godot_project --resolution 1920x1080 res://client/scenes/military_viewer_demo.tscn
```

(Exact scene paths finalized during RUN_05 implementation; use the demo launcher described in
`docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md`.)

**Implemented scenes:**
- `res://client/scenes/economy_viewer_demo.tscn`
- `res://client/scenes/military_viewer_demo.tscn`

### RUN_05 viewer smokes (Track 1)

```powershell
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
```

Expected: `GOD-SMOKE-001 PASS`, `GOD-SMOKE-002 PASS`, `GOD-SMOKE-003 PASS`, `VIEWER SMOKE PASS`, exit `0`.

### RUN_05 viewport pixel smokes (Track 1.5 — non-headless)

Proves the **2D view** renders hex terrain and visible changes on step/command. Requires display (do **not** pass `--headless`).

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_pixel_smoke.gd
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

Without `--assert`, the runner saves screenshots only (no automated color check).

Expected: `VIS-LAYOUT-001 PASS`, `GOD-CAPTURE-001 PASS`, `VIS-PIX-CHAIN-001 PASS`, `VIS-PIX-BUILD-001 PASS`, `GOD-SMOKE-004 PASS`, exit `0`.

Final report must state **three statuses separately**: headless pass/fail, pixel smoke pass/fail, human windowed review pending/done.

### RUN_05 closure verification (full ordered sequence)

Run on `main` after RUN_05 merge. Record exact commands, exit codes, and three-track statuses in
[`docs/audit/RUN_05_FINAL_REPORT.md`](../audit/RUN_05_FINAL_REPORT.md).

**Track 1 — headless (PowerShell example):**

```powershell
$env:GODOT = "<full path to Godot console executable>"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_military_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
& $env:GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

Expected: **154/154** tests; all smokes pass; GOD-SMOKE-001/002/003; INTEGRITY PASS; exit `0` each.

**Track 1.5 — pixel (no `--headless`):**

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_pixel_smoke.gd
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

**Track 2 — human:** windowed demo launch at 1920×1080 + checklist below; screenshots from capture run.

### Economy logistics visual review checklist (~5 min)

**Production chain (Load Production Demo)**

1. Load Production Demo → woodcutter on dark-green forest (D:80), sawmill on road network.
2. Step 10 → forest hex density drops OR woodcutter shows `T:n` store chip.
3. Step Until Event → dashed amber carrier route + moving dot; event log shows `production_completed` / `ware_moved` / `ware_delivered`.
4. Step more → sawmill shows timber input then `P:n` planks; summary wares update.

**Build tutorial (Load Build Tutorial)**

1. Load Build Tutorial → no pre-built site; command list includes road-adjacent storehouse placement.
2. Submit placement → dashed site hex appears; checklist step 1 ✓; site auto-selected in inspector.
3. Submit road segment(s) toward HQ if needed → yellow road extends; checklist step 2 ✓.
4. Step 10 / Until Event → site progress ring fills, carriers deliver, checklist step 3 ✓.
5. Site disappears, STORE building appears; checklist step 4 ✓.

Toggle **Logistics** / **Forest D** layers and **Logistics Events** filter to confirm overlays and log filtering.

### RUN_05 interactivity (human debug loop)

Economy viewer — after Load Production Demo:
1. Dashed hex + green progress ring = construction site (user-placed via Build Tutorial).
2. `D:n` on forest hexes, store chips (`T:`/`P:`), dashed amber carrier routes when Logistics layer on.
3. Submit **Roads:** or **Buildings:** command → **Step 10** or **Step Until Event** → watch carriers, delivered counts, or event log change.
4. Summary panel shows wares, forest density, route counts, site/job counts.

Military viewer:
1. Pre-spawned p1/p2 rifle squads nearby; attack commands appear prebuilt in the list.
2. Submit **Attack:** command → **Step 10** / **Step Until Event** → HP bars drop, combat/damage events in log.
3. Red ring highlights attack target hex after submit.

**Step Until Event** stops only on meaningful event substrings (`construction`, `ware`, `carrier`, `production`, `damage`, `combat`, `unit_moved`, `order`), capped at 50 steps.

### RUN_05 screenshot capture (SubViewport; non-headless)

Headless dummy renderer cannot capture viewport textures. Use the capture runner **without**
`--headless`:

```powershell
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

Omit `--assert` to save screenshots without automated color verification.

Saves to `user://screenshots/run_05_economy_demo.png` and `run_05_military_demo.png` (under
`GODOT_USER_DATA_DIR` when set, otherwise OS default app user data).

Confirm visually: map is large/readable; panels do not overlap the map; text is readable at desktop
scale; legend explains every colour/icon/overlay; layer toggles work; selection inspector is
understandable; command panel lists sim-provided commands and shows results/errors; event log shows
recent history; turn/tick/active player/phase/priority are visible; economy and military demos are
immediately understandable.

### Screenshots

Capture a screenshot of each demo. Preferred: an in-viewer screenshot button or debug hotkey
saving to:

- `user://screenshots/run_05_economy_demo.png`
- `user://screenshots/run_05_military_demo.png`

If the in-viewer capture is not implemented, take manual OS screenshots and record their paths. The
final report must state whether screenshots were automatic or manual and reference the paths.

### Process safety for windowed runs

Windowed Godot runs can leave stale/orphaned processes. Before launching, inspect:

```powershell
Get-Process | Where-Object { $_.ProcessName -like "*Godot*" }
```

Close the window normally when done. If a windowed run does not exit cleanly, stop only the clearly
stale Godot process for this project (confirm the PID first) and record the cleanup in the report.
Never broad-kill unrelated Godot processes.

## RUN_04+ pre-flight

Before adding bot/replay/telemetry mechanics in RUN_04, rerun all commands in this document
(tests, smokes, integrity checks) and confirm exit code `0` on each.

For RUN_05, rerun the RUN_01–04 tests, smokes, and failure-mode integrity checks (all exit `0`, no
`SCRIPT ERROR`) before writing viewer code, per `.cursor/rules/10-run-verification-and-logging.mdc`.

## RUN_06 neural training (Windows example)

Prerequisites: Python 3.13 venv with `torch`, `pytest`, `numpy` (`requirements.txt`); Godot 4.5.x
console; checkpoints under `checkpoints/run06_smoke/` (gitignored).

```powershell
$env:GODOT = "C:\path\to\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
.\.venv313\Scripts\pytest python_tools/tests/ -v
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_training_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_training_env.gd -- --self-test
```

Prior-kernel gate (before RUN_06 edits): `run_tests.gd` (160/160 on feature branch with RUN_06b tests), economy/military/bot smokes, `verify_failure_modes.gd` — all exit `0` on Linux Godot 4.5.2 during closure verification (2026-06-15).

## RUN_06b policy eval smoke (Linux)

After RUN_06b contract work, add to the closure gate:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_policy_eval_smoke.gd
```

Expect `POLICY EVAL SMOKE PASS` and exit `0`. Grep output for `SCRIPT ERROR`.

Subset policy-contract tests only (avoid full 160-test suite when iterating):

```bash
TEST_FILTER=policy_contract timeout 240s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

Full closure pytest (18 tests, includes RUN_06b):

```bash
timeout 1800s .venv/bin/python -m pytest python_tools/tests/ -v
```

Delete stale RUN_06 smoke artefacts before a clean pytest/improve run:

```bash
rm -rf training_runs/run06_smoke training_runs/run06_smoke_test \
       checkpoints/run06_smoke checkpoints/run06_smoke_test
```
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

## Headless unit/integration tests (188 tests — current suite)

```bash
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
echo "exit=$?"
```

Expected output includes timing summary, then:

```text
Results: 188/188 ran, 188 passed, 0 failed, 0 errored
EXIT: 0 (pass)
```

The 580s timeout in older run docs may be insufficient on the reference machine; use **900s** for full closure unless timing data shows headroom.

Exit code must be `0`. A clean run must print **no** `SCRIPT ERROR` lines in stdout/stderr.

The test harness (`sim/tests/test_case.gd`) marks a test as incomplete when `_run_body()` aborts
before returning (e.g. a script error mid-test). Such tests report as **errored**, not passed.

### Test runner selection and timing (RUN_02.8c)

CLI flags are passed **after `--`** (Godot user args). Env vars remain supported for existing workflows.

```bash
# List all manifest tests (no execution)
"$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd -- --list

# Manifest integrity preflight (no execution)
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd -- --manifest-check
echo "exit=$?"   # expect 0 on clean tree

# Single substring filter (must exit non-zero when zero tests match)
TEST_FILTER=econ_bot_b001 timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

# Multiple filters (OR)
TEST_FILTERS=econ_bot_b001,econ_bot_c001 timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

# Category slice: kernel, economy, military, bot, viewer, training, replay
TEST_CATEGORY=bot timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

# Timing export (creates parent dirs)
mkdir -p training_runs/test_timings
"$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd -- \
  --timing-json=training_runs/test_timings/serial_timing.json

# Zero-match filter must fail
TEST_FILTER=definitely_no_such_test timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
echo "exit=$?"   # expect non-zero
```

Output includes `TIMING: total=…s`, per-test slowest 20, and exact `Results:` counts.

## Tiered verification workflows (RUN_02.8c)

Use three tiers; only **Tier 2** is the authoritative merge closure gate.

### Tier 1 — Fast headless regression (iteration)

Targeted filters while editing a subsystem. Examples:

```bash
TEST_FILTERS=econ_bot_b001,econ_bot_c001,econ_bot_d001,econ_bot_e001 timeout 180s \
  "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

TEST_CATEGORY=viewer timeout 130s \
  "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

Filtered passes are **not** full closure.

### Tier 2 — Full headless closure gate (merge authority)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd
timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

All commands must complete with exit `0` and no `SCRIPT ERROR`.

### Tier 2b — Experimental parallel full suite (opt-in, not merge authority)

After serial runner changes are stable. Does **not** replace Tier 2.

```bash
mkdir -p training_runs/test_timings
python3 python_tools/testing/parallel_godot_tests.py \
  --godot "$GODOT" \
  --project godot_project \
  --parallel 2 \
  --timeout-per-worker 900 \
  --timing-json training_runs/test_timings/parallel_timing.json
echo "exit=$?"
```

Each worker uses a unique `godot_project/.godot_user_parallel/worker_XX` directory. Rerun Tier 2 serial closure after parallel experiments.

### Tier 3 — Visual/manual checks

See RUN_05 viewer demo launch, viewport pixel smokes, and RUN_02.8 bot-vs-bot visual review sections below. Non-headless; inspect Godot processes before/after; never broad-kill.

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

## Economy smoke tests (ECON-SMOKE-001–006)

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

Expected: `BOT SMOKE PASS`, `EXIT: 0 (pass)`. Default invocation does **not** export artefacts; pass `--export-dir=<path>` or `--artifacts` to opt in. BOT-SMOKE-001 completes `tiny_success_map` via `HeuristicEconomyBot`; BOT-SMOKE-002 wins `skirmish_1v1` via `HeuristicMilitaryBot`; REP-SMOKE-001 (smoke id `003`) export → import → identical hash (uses internal temp dir when no export flag is set).

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
- `res://client/scenes/economy_spectator_bot_demo.tscn` (RUN_05C visual groundwork)

### RUN_05C.3 economy spectator review polish (focused)

```bash
export GODOT=/path/to/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=vis_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_spectator_smoke.gd
timeout 240s "$GODOT" --path godot_project --script res://sim/tools/run_economy_spectator_capture.gd -- --assert
timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

Expected: VIS-SPECTATOR-001..006 pass; smoke `families=3/3` + sprite count; capture under `training_runs/visual_review/run_05c_economy_spectator/` with `contact_sheet.html`, `capture_manifest.json`, `REVIEW_NOTES.md`. Generated PNGs/HTML are **not committed**. Pixel delta is sanity only.

See `docs/audit/RUN_05C_3_VISUAL_POLISH_REPORT.md`.

### RUN_05C.2 economy spectator polish (focused)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=vis_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_spectator_smoke.gd
timeout 240s "$GODOT" --path godot_project --script res://sim/tools/run_economy_spectator_capture.gd -- --assert
```

Expected: VIS-SPECTATOR-001..005 pass; smoke `families=3/3`; capture under `training_runs/visual_review/run_05c_economy_spectator/`. Pixel delta is sanity only — authoritative gates are render/sprite deltas and grounding.

See `docs/audit/RUN_05C_2_VISUAL_POLISH_REPORT.md`.

### RUN_05C economy spectator (visual groundwork — focused)

Production-chain demo only for automated gates. Dual-bot is manual UI diagnostic. `ECON_BOT_002` / `BOT-SMOKE-004` / `ECON-SMOKE-007` are **not** RUN_05C blockers.

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

TEST_FILTER=vis_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_spectator_smoke.gd
timeout 180s "$GODOT" --path godot_project --script res://sim/tools/run_economy_spectator_capture.gd -- --assert
```

Expected: VIS-SPECTATOR-001..004 pass; `GOD-SMOKE-ECON-SPECTATOR-001 PASS`; capture saves `spectator_tick0.png` and `spectator_tick30.png` under project-local export `run_05c_economy_spectator/` with `pixel_delta > 5`. Grep for `SCRIPT ERROR` — must be zero.

See `docs/audit/RUN_05C_VISUAL_GROUNDWORK_CONTINUATION.md` for `meaningful_count` rules.

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

## RUN_02.8 bot-vs-bot economy (Linux verified 2026-06-16 on `feature/run-02-8-bot-vs-bot-economy`)

Authority: [`docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md`](../runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md).

**Pre-flight:** Rerun full RUN_02.7 baseline before RUN_02.8 gameplay edits (Phase 0 recorded pass on branch).

### Headless closure gate

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd
timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

Expect:

- Full test suite: **188/188 pass**
- ECON-SMOKE-001–007 exit `0`
- BOT-SMOKE-001–004 exit `0`
- GOD-SMOKE-001–005 exit `0`
- INTEGRITY PASS
- No `SCRIPT ERROR` in captured output

**Pinned ECON-SMOKE-007 / BOT-SMOKE-004 replay hash** (seed 28001, 30 ticks, `expansion_duel_2p`):

`sha256:628c0c82af0448747634376303a4a90ec4b009c25d6106560d586b2069cd3d9d`

Subset bot-vs-bot economy smoke:

```bash
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd -- --smoke=007
timeout 200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd -- --smoke=004
timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=005
```

### Non-headless visual review (Track 2)

```bash
"$GODOT" --path godot_project --script res://sim/tools/run_economy_bvb_visual_review.gd
```

**Contact sheet checklist (~5 min):**

1. **Start** — both HQs visible; ownership overlay distinguishes p1 (blue) vs p2 (red) on buildings, roads, nodes, carriers, sites.
2. **Early / mid / late / final** — both economies expanding; scoreboard shows rising `economy_scores`.
3. **Logistics overlay** — road nodes, ports, carriers, delivery jobs for both players.
4. Confirm `contact_sheet.html` and `capture_manifest.json` list panels: start, early, mid, late, final, logistics.

Output directory (planned): `training_runs/visual_review/run_02_8_economy_bvb/` (or path recorded in manifest).

Grep output for `SCRIPT ERROR`. Windowed runs: inspect stale Godot processes before/after (see RUN_05 process safety).

## RUN_02.8e human proof harness (Linux)

Authority: [`docs/runs/RUN_02_8E_HUMAN_PROOF_SCENARIO_VIABILITY.md`](../runs/RUN_02_8E_HUMAN_PROOF_SCENARIO_VIABILITY.md).

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

timeout 120s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_oracle.gd -- \
  --plan=res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json

TEST_FILTER=econ_human_proof timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

timeout 60s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_smoke.gd -- --smoke=all
```

Interactive scene: `godot_project/client/scenes/economy_human_proof_demo.tscn`

Expect: oracle `ORACLE PASS`, human_proof tests **4/4 pass**, smoke `HUMAN PROOF SMOKE PASS`, no `SCRIPT ERROR`. `ECON_BOT_002` may remain red (document only).

## MRP full-stack verification (Linux)

Authority: [`docs/runs/MRP_hybrid_3d_spectator_added.md`](../runs/MRP_hybrid_3d_spectator_added.md).

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"

# Bot suites
timeout 150s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_fast.gd
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_long.gd

# Filtered test slices
TEST_FILTER=military_mvp timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=hybrid_mvp timeout 130s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=hybrid_3d_spectator timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=HYB_SMOKE timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd

# Screen legibility gates
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_military_spectator_screen.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_military_mvp_screen.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_hybrid_observer_screen.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_hybrid_mvp_screen.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_hybrid_3d_spectator_screen.gd

# Orchestrator (full stack; long-running)
timeout 3600s "$GODOT" --headless --path godot_project --script res://sim/tools/run_mrp_full_verification.gd
```

## Manual playability QA (RUN_MANUAL_PLAYABILITY_QA)

Primary gate — menu-route screenshots, tutorial navigation, time controls, bot functional smokes:

```bash
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_manual_playability_qa.gd
TEST_FILTER=manual_qa timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_actual_playability_screenshots.gd -- --mode=all
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_bot_fast.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tutorial_bot_smoke.gd
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tutorial_military_bot_smoke.gd
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tutorial_hybrid_bot_smoke.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd
timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

If `$DISPLAY` is set, also run the real GUI checklist from `./scripts/run_game.sh` (Settler/Skirmish/Hybrid tutorial Next/Back/Play) and save captures under `docs/audit/screenshots/manual_playability/real_gui/`.

Launch scripts: `./scripts/run_game.sh`, `./scripts/run_skirmish.sh`, `./scripts/run_hybrid.sh`, `./scripts/run_hybrid_3d_spectator.sh`

Note: `ECON_BOT_002` is excluded from default `run_tests.gd`; run via `run_bot_long.gd` only.

## Real-time tutorials and 3D hybrid spectator QA

Product gate — all player-facing modes must start in **Playing** state (any mode starting paused = fail):

```bash
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_realtime_tutorials_and_3d_qa.gd
TEST_FILTER=realtime timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=tutorial_completable timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=3d_spectator timeout 500s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_hybrid_bot_vs_bot_3d_smoke.gd
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/capture_realtime_tutorials_and_3d_screenshots.gd
```

Screenshots: `docs/audit/screenshots/realtime_tutorials_and_3d/`. If `$DISPLAY` is set, manually verify modes launch running via `./scripts/run_game.sh`.

## PRD — Neural Hybrid / Spectator / Mobile (Linux)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
TEST_FILTER=neural_hybrid timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=hybrid_spectator timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=mobile_hex timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=mobile_ffa timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/run_neural_hybrid_4p_smoke.gd
timeout 30s "$GODOT" --headless --path godot_project --script res://sim/tools/run_mobile_hex_view_qa.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/run_hybrid_mobile_ffa_qa.gd
```

## God-Strategy MVP (Milestone A + B)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
TEST_FILTER=GOD_A timeout 7200s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_B timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=HYB_SMOKE timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
# Milestone A proof: domination_regression @ PROOF_TICK_CAP (3000) — allow ~25–40 min
timeout 2400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_god_strategy_eval.gd
# Long integration tests (barracks / unit production)
TEST_FILTER=GOD_A_010 timeout 2400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_A_011 timeout 2400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

Artefacts: `docs/audit/god_strategy/eval/`

## Final God-Strategy (Phases 2–8 filters)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
TEST_FILTER=GOD_C timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_D timeout 180s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_E timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_G timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=GOD_F timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
TEST_FILTER=god_strategy_mobile timeout 60s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 90s "$GODOT" --headless --path godot_project --script res://sim/tools/run_god_strategy_mobile_qa.gd
```

Artefacts: `docs/audit/god_strategy/final/phase2/` … `phase8/`, screenshots under `docs/audit/screenshots/god_strategy_final/`.

**Phase 6 neural gate (hard stop 2026-07-09):** see `docs/audit/god_strategy/final/phase6/GAP_REPORT.md`.  
**Next run:** `docs/runs/RUN_FINAL_GOD_STRATEGY_PHASE6_NEURAL.md`.  
**Lessons:** `docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md`.

### AI eval scenario rules

| Scenario | Use for |
|----------|---------|
| `god_strategy_domination_regression.json` | Combat-speed **proof** only (1-HP hubs + pre-placed squads) |
| `god_strategy_ai_eval.json` | Bot **win-rate comparison** (fair hubs) |

Do not report tick-1 domination from the regression scenario as a neural/heuristic win-rate result.

`TEST_FILTER=GOD_F` may need **2400s+** once real multi-seed fair evals land; 900s is a lower bound for scaffolding only.
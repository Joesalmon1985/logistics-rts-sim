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

### Windows review environment (review / docs only by default)

Windows may be used for: reading docs, reviewing branches, checking git status, checking
acceptance checklists, markdown/documentation cleanup, and running verification **only** if a
compatible Windows Godot 4.5.x executable is available.

**Windows must never claim Linux verification has passed.** If Godot is not available on Windows,
report exactly:

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

## Headless unit/integration tests (148 tests — RUN_01 + RUN_02 + RUN_03 + RUN_04)

```bash
timeout 400s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
echo "exit=$?"
```

Expected output includes:

```text
Results: 148/148 ran, 148 passed, 0 failed, 0 errored
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

## RUN_04+ pre-flight

Before adding bot/replay/telemetry mechanics in RUN_04, rerun all commands in this document
(tests, smokes, integrity checks) and confirm exit code `0` on each.

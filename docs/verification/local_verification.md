# Local Verification — RUN_01 + RUN_02

Godot version used for verification: **4.5.2.stable.official** (`6ce3de25a`).

Godot binary path on this machine:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

If `godot` is on `PATH`, substitute `godot` for `$GODOT`.

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

## Headless unit/integration tests (39 tests — RUN_01 + RUN_02)

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
echo "exit=$?"
```

Expected output includes:

```text
Results: 39/39 ran, 39 passed, 0 failed, 0 errored
EXIT: 0 (pass)
```

Exit code must be `0`.

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

## Failure-mode integrity checks

Verifies invalid variant, hash mismatch, and invalid ticks **exit non-zero**:

```bash
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
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

## RUN_02+ pre-flight

Before adding economy mechanics in RUN_02, rerun all commands in this document (tests, smokes, integrity checks) and confirm exit code `0` on each.

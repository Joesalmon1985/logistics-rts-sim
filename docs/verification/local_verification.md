# Local Verification — RUN_01

Godot version used for verification: **4.5.2.stable.official** (`6ce3de25a`).

Godot binary path on this machine:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
```

If `godot` is on `PATH`, substitute `godot` for `$GODOT`.

## First-time project import

Run once after cloning or when adding new GDScript `class_name` types:

```bash
mkdir -p godot_project/.godot_user
$GODOT --headless --path godot_project --import
```

## Headless unit/integration tests (13 tests)

```bash
$GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
```

Expected: `Results: 13 passed, 0 failed` and exit code `0`.

## Headless smoke tests (KERN-SMOKE-001–004)

Economy trial (default scenario):

```bash
$GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=economy_trial --ticks=10 --seed=1
```

Military trial:

```bash
$GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=military_trial --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_military.json
```

Hybrid:

```bash
$GODOT --headless --path godot_project --script res://sim/tools/run_smoke.gd -- --variant=hybrid --ticks=10 --seed=1 --scenario=scenarios/kernel_smoke_hybrid.json
```

Expected: all four `KERN-SMOKE-*` lines pass; exit code `0`.

## Repository hygiene

```bash
git diff --check
git status --short
```

## Test harness (DDR-032)

Gameplay-rule tests use a **lightweight custom Godot headless runner** (`sim/tools/run_tests.gd`). No GUT/gdUnit4 addon. Tests are pure GDScript `RefCounted` classes under `sim/tests/`.

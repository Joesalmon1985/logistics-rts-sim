# Python training tools (RUN_06)

Python drives the authoritative Godot/GDScript simulation core via headless transport (DDR-033). Python is the trainer, not the engine.

## Setup (Windows)

```powershell
py -3.13 -m venv .venv313
.\.venv313\Scripts\pip install -r requirements.txt
$env:GODOT = "C:\path\to\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"
```

## Run tests

```powershell
.\.venv313\Scripts\pytest python_tools/tests/ -v
```

## Artifacts (gitignored)

- `checkpoints/run06_smoke/` — trained policy weights
- `training_runs/run06_smoke/` — metrics, evaluation CSV, BC stats

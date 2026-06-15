# RUN_05 Final Report

Branch: `main` (`cce553f` — code); closure docs on `docs/run-05-closure-audit`

Summary: RUN_05 delivered Godot 2D debug viewers for economy and military trial games with sim-owned command availability, logistics visualization layers, and a three-track verification model. Headless contract tests (154/154) and viewer smokes (GOD-SMOKE-001–003) pass; non-headless pixel smokes (GOD-SMOKE-004, GOD-CAPTURE-001) gate against blank-viewport regressions. Human windowed review at 1920×1080 confirmed complete during closure audit.

Files changed (closure pass — docs only):

- `docs/audit/RUN_05_LESSONS_LEARNED.md` — authoritative handoff; "Avoiding a non-working visual product"
- `docs/audit/RUN_05_FINAL_REPORT.md` — this report
- `docs/audit/RUN_05_IMPLEMENTATION_PLAN.md` — archived header
- `docs/runs/RUN_05_GODOT_2D_VIEWERS_FOR_TRIAL_GAMES.md` — three tracks, acceptance complete
- `docs/runs/RUN_06_NEURAL_TRAINING_FOR_TRIAL_GAMES.md` — pre-RUN_06 entry checklist
- `docs/testing/03_godot_viewer_test_plan.md` — three tracks, Track 1.5/2 checked
- `docs/testing/02_smoke_test_matrix.md` — dual smoke runners noted
- `docs/verification/local_verification.md` — closure verification sequence, `--assert`
- `docs/audit/RUN_04_LESSONS_LEARNED.md` — RUN_05 outcome note

Tests run (closure verification, 2026-06-15, Windows Godot 4.5.1 console exe):

```powershell
$env:GODOT = "C:\Users\joesa\Downloads\Godot_v4.5.1-stable_win64.exe\Godot_v4.5.1-stable_win64_console.exe"
$env:GODOT_USER_DATA_DIR = "$(Resolve-Path .\godot_project)\.godot_user"

# Track 1 — headless
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_tests.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_economy_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_military_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_bot_smoke.gd
& $env:GODOT --headless --path godot_project --script res://sim/tools/run_viewer_smoke.gd -- --smoke=all
& $env:GODOT --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd

# Track 1.5 — non-headless (display required)
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_pixel_smoke.gd
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=economy --assert
& $env:GODOT --path godot_project --script res://sim/tools/run_viewer_capture.gd -- --variant=military --assert
```

Results:

| Command | Exit | Status |
|---------|------|--------|
| `run_tests.gd` | 0 | **passed** — 154/154 ran, 154 passed |
| `run_economy_smoke.gd` | 0 | **passed** — ECON SMOKE PASS |
| `run_military_smoke.gd` | 0 | **passed** — MIL SMOKE PASS |
| `run_bot_smoke.gd` | 0 | **passed** — BOT SMOKE PASS |
| `run_viewer_smoke.gd --smoke=all` | 0 | **passed** — GOD-SMOKE-001/002/003 |
| `verify_failure_modes.gd` | 0 | **passed** — INTEGRITY PASS |
| `run_viewer_pixel_smoke.gd` | 0 | **passed** — GOD-SMOKE-004 |
| `run_viewer_capture.gd --variant=economy --assert` | 0 | **passed** — GOD-CAPTURE-001 |
| `run_viewer_capture.gd --variant=military --assert` | 0 | **passed** — GOD-CAPTURE-001 |

No `SCRIPT ERROR` lines in any command output.

## Three-track status

| Track | Status | Evidence |
|-------|--------|----------|
| 1 Headless | **passed** | 154/154 tests; RUN_01–04 smokes; GOD-SMOKE-001/002/003; failure-mode integrity |
| 1.5 Pixel | **passed** | `run_viewer_pixel_smoke.gd`; capture `--assert` economy + military |
| 2 Human | **complete** | User confirmed satisfied at 1920×1080; checklist in `local_verification.md` |

### Screenshot paths (Track 1.5 capture, 2026-06-15)

Automatic capture via `run_viewer_capture.gd --assert`:

- `C:/Users/joesa/AppData/Roaming/Godot/app_userdata/WideRTS Simulation/screenshots/run_05_economy_demo.png`
- `C:/Users/joesa/AppData/Roaming/Godot/app_userdata/WideRTS Simulation/screenshots/run_05_military_demo.png`

Convention when `GODOT_USER_DATA_DIR` is set: `user://screenshots/run_05_economy_demo.png`, `user://screenshots/run_05_military_demo.png`.

Known issues:

- Control anchor warnings during pixel smokes (`Nodes with non-equal opposite anchors`) — cosmetic; smokes pass.
- Track 1.5 requires display; not wired for CI headless runs (manual/non-headless gate).
- Linux/Godot 4.5.2 re-verification not run in this closure pass; Windows 4.5.1 pass is merge-supporting per `local_verification.md`.
- Capture subprocess may use OS default app user data if `GODOT_USER_DATA_DIR` is not inherited.

Unimplemented requirements: none — all RUN_05 acceptance checklist items closed.

Uncommitted changes: (filled at commit time)

Suggested next run: **RUN_06** — Neural Training for Trial Games (`feature/run-06-neural-training-trial-games`). See pre-entry checklist in `docs/runs/RUN_06_NEURAL_TRAINING_FOR_TRIAL_GAMES.md`.

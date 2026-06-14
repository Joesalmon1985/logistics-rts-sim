# RUN_01 — Lessons Learned

For future high-context planning agents before RUN_02, RUN_03, RUN_07 and later.

Written during the RUN_01 closure pass after the kernel implementation and integrity audit.

---

## 1. Executive summary

RUN_01 successfully established a pure Godot/GDScript simulation kernel: deterministic tick loop, seeded RNG, integer/fixed-point policy, shared axial hex map, command/event/snapshot/replay foundations, variant registry with three stubs, and a custom headless test harness (DDR-032). All 13 kernel tests and three variant smokes passed after clean reruns with timeout wrappers.

Early implementation hit **recoverable tooling issues** (Godot not on PATH, sandbox userdata errors, missing `class_name` registration before import, wrong HashingContext API, hung processes from missing `quit()`). None of these invalidate the final kernel, but they show why verification must treat **aborted, timed-out, or partial runs as failures**.

---

## 2. What RUN_01 successfully established

- `godot_project/sim/` authoritative core (RefCounted-only, no scene tree dependency)
- Custom headless test runner: `sim/tools/run_tests.gd`
- Smoke runner: `sim/tools/run_smoke.gd`
- Failure-mode integrity verifier: `sim/tools/verify_failure_modes.gd`
- Shared hex primitives: `HexCoord`, `HexMap`, `MapGenerator`
- Core pipeline: `CommandValidator`, `EventLog`, `SnapshotExporter`, `ReplayHash`
- Variant stubs: `economy_trial`, `military_trial`, `hybrid`
- Config/scenario JSON under `sim/data/`
- Verification docs: `docs/verification/local_verification.md`
- DDR-032 Accepted (custom runner)

---

## 3. Architecture decisions confirmed by implementation

| Decision | Confirmed behaviour |
|----------|---------------------|
| DDR-026 | All rules in `sim/`; no UI/scene authority |
| DDR-027 | Hashed state is int-only; `CanonicalJson.contains_float` test guards this |
| DDR-029 | Single axial hex map shared by all variants |
| DDR-032 | Custom headless runner works; no GUT/gdUnit4 needed for kernel |
| DDR-004 | Seeded RNG in state; reproducible replay hash |

---

## 4. Godot/GDScript lessons

- Godot **4.5.2** was used; API differs from docs/examples (see HashingContext below).
- Pure `RefCounted` + `class_name` works for a headless sim core.
- `SceneTree` scripts (`extends SceneTree`) are appropriate for CLI entry points only (`sim/tools/`).
- Godot generates `.gd.uid` files on import; commit them for stable class registration.
- `call_deferred("quit", code)` is more reliable than immediate `quit()` from `_initialize()`.

---

## 5. Headless test harness lessons

- Explicit test manifest in `run_tests.gd` (`TEST_SCRIPTS` array) — no magic discovery.
- Tests extend `TestCase` with `_run_body()`; base `run()` sets `run_completed` flag.
- Runner must report: **ran / expected, passed, failed, errored** and `EXIT: N`.
- Deliberate fail fixture lives at `sim/tests/integrity/` — **not** in main suite.
- `verify_failure_modes.gd` subprocess checks prove smoke runner exit codes on bad input.

---

## 6. Determinism/replay/hash lessons

- Replay hash uses `CanonicalJson.stringify()` + SHA-256 via `HashingContext.finish().hex_encode()`.
- **Do not use** `retrieve_as_text()` — removed/not available in Godot 4.5.
- Prefix hashes with `sha256:` for clarity.
- Lock `expected_hash` in scenario JSON only after a verified run on the target Godot version.
- Hash excludes `event_log` (see `GameState.to_hash_dict()`).

---

## 7. Class_name/import/bootstrap lessons

- **`godot --headless --import` is required once** (or after adding new `class_name` types) before tests parse.
- Without import, tests fail with "Identifier X not declared in the current scope".
- `SimBootstrap.load_all()` preloads core scripts in dependency order — helps but does **not** replace import.
- After import, global `class_name` types resolve in test scripts.

---

## 8. Godot user-data / sandbox / local path lessons

- Default Godot userdata (`~/.local/share/godot/app_userdata/...`) may fail in **sandboxed** agent shells → segfault (exit 139).
- Set `GODOT_USER_DATA_DIR=godot_project/.godot_user` (gitignored) for headless runs.
- Godot may **not be on PATH**; use full binary path or `$GODOT` env var:
  `/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64`

---

## 9. Test timeout / aborted-task lessons

- Hung Godot processes occurred when `quit()` was not reached after script errors.
- **Aborted shell tasks are not passes.** Cursor may show "task aborted" for timed-out or killed runs — discard that output.
- Always use `timeout Ns` on Linux for Godot CLI commands.
- Exit code **124** = timeout → failure.
- Kill stale Godot PIDs (`pgrep -af Godot`) before reruns if a prior run hung.
- Check **exit code** and **this run's stdout**, not prior terminal files.

---

## 10. Verification command lessons

- Document exact Godot path, userdata dir, and timeout wrappers in `local_verification.md`.
- Distinguish: import pass vs test pass vs smoke pass vs integrity pass.
- Print `EXIT: 0 (pass)` / `EXIT: 1 (fail)` from runners for unambiguous logs.
- RUN_02 agents must rerun RUN_01 verification before adding mechanics.

---

## 11. Git/process lessons

- Work on `feature/run-01-core-simulation-kernel`; do not merge to `main` without instruction.
- Commit kernel first, closure docs second (this pass).
- `.godot/` and `.godot_user/` are gitignored; `.gd.uid` files are committed.

---

## 12. Risks for RUN_02

- **Scope size** (DDR-028): full economy is large; one run preferred, one A/B split max.
- Accidentally implementing rules in `client/` instead of `sim/`.
- Float creep in ware quantities, timers, or path costs — breaks replay hash.
- Parallel map/road model instead of extending RUN_01 hex + shared types.
- Skipping RUN_01 regression tests while adding ECON-* tests.
- Reporting pass from partial/aborted Godot runs.

---

## 13. Risks for RUN_03

- Creating a **square grid** or separate passability map (forbidden by DDR-029).
- Credits/combat floats in hashed state.
- Duplicating hex map code instead of importing RUN_01 primitives.

---

## 14. Risks for RUN_07

- Composing economy + military without shared map/event/snapshot contracts.
- Introducing credits in hybrid (forbidden by DDR-031).
- Weak replay tests after integrating two large rule sets.

---

## 15. Recommended changes to future run prompts

- Require **timeout-wrapped** Godot commands with explicit `echo exit=$?`.
- Require **integrity check** (`verify_failure_modes.gd`) on closure passes.
- Require reporting: passed / failed / errored / timed out / not run.
- Require stale-process cleanup before verification.
- Require `$GODOT` full path if not on PATH.
- State: "Import success ≠ test success."

---

## 16. Recommended changes to documentation or verification policy

- `local_verification.md`: timeout wrappers, exit codes, integrity checks (done in closure pass).
- `docs/testing/00_test_first_policy.md`: RUN_02+ must rerun kernel tests first (updated).
- `docs/runs/RUN_02_*.md`: pre-flight kernel regression step (updated).
- Future runs: add "closure pass" checklist when a run had aborted intermediate shells.

---

## 17. Things future agents must not assume

- Godot is on PATH.
- `class_name` works without `--import`.
- Sandbox allows default Godot userdata writes.
- HashingContext API matches Godot 3 or web docs.
- Aborted/hung terminal tasks imply tests passed.
- Import completing means tests passed.
- `SMOKE PASS` printed after an earlier `SCRIPT ERROR` means success (check exit code and `EXIT:` line).

---

## 18. Suggested pre-flight checklist for RUN_02

1. `git checkout feature/run-02-trial-economy-game` (from updated main or RUN_01 branch as policy dictates).
2. Confirm clean working tree.
3. `export GODOT=...` and `GODOT_USER_DATA_DIR=godot_project/.godot_user`.
4. `pgrep -af Godot` — kill stale project processes if any.
5. `timeout 120s $GODOT --headless --path godot_project --import` (if new class_names added).
6. `timeout 60s ... run_tests.gd` → expect 13/13, exit 0.
7. Three smoke commands → exit 0 each.
8. `timeout 120s ... verify_failure_modes.gd` → exit 0.
9. Read this document and `RUN_01_CORE_SIMULATION_KERNEL.md` acceptance checklist.
10. Only then begin ECON-* test-first implementation.

---

*Intermediate aborted shells during RUN_01 (sandbox userdata, pre-import class_name errors, HashingContext probe) were debugging steps. Final verification used clean reruns with timeouts and exit-code checks.*

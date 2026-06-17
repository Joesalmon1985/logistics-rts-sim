# RUN_02.8c — Lessons Learned

Date: 2026-06-16

## What worked

- Manifest integrity check catches orphan/missing tests without replacing stable manual ordering.
- `TEST_FILTER` / `TEST_FILTERS` / `TEST_CATEGORY` plus `--list` and `--manifest-check` give a clear iteration story.
- Per-test timing and slowest-20 output immediately surfaced dual-bot tests as the dominant cost.
- `SimTestLoop.run_bot_until` early exit cut the B–E bot slice from multi-minute tick burns to **~4s** without changing caps or assertions.
- External parallel wrapper with unique `GODOT_USER_DATA_DIR` per worker achieved **~353s vs ~556s** wall for 2 workers (37% faster), with correct 188/188 aggregate counts and no duplicates.
- Bot smoke export opt-in removes silent cache writes on every default smoke run.

## What broke / surprised

- **580s timeout is too low** for the full 188-test suite on the reference Linux machine (~555s observed). Closure docs now recommend **900s**.
- Phase A baseline timed out before completing; always record partial progress explicitly.
- Godot segfaulted in sandbox without `GODOT_USER_DATA_DIR` / full permissions — use documented env and non-sandboxed verification on Linux.
- Timing JSON write failed until `SimPaths.ensure_parent_dir` was added for relative paths under `training_runs/`.
- First parallel wrapper draft ran workers **sequentially**; fixed with `ThreadPoolExecutor`.
- Pre-existing failures on `main` block a green closure gate: `ECON_BOT_002`, `ECON-SMOKE-006`, `REP-SMOKE-001`. Infrastructure run cannot claim full gameplay closure pass.

## Process lessons

- Clean-tree gate with explicit allowed untracked debug file prevented accidental branch work on dirty state.
- Strict stale-Godot policy (report over kill) avoided collateral damage; stale debug Godot was noted but not killed.
- Preserve env-var filters (`TEST_FILTER=...`) alongside `--` CLI flags — existing docs and habits depend on env vars.
- Do not rewrite historical audit counts; update only active verification guidance.

## Risks for next runs

- Parallel mode remains experimental until serial closure is green and parallel flake rate is observed over multiple runs.
- Filter substring `econ_bot` includes heavy tests (002/003); document specific B–G filters for fast bot iteration.
- Full suite runtime will grow with new tests — watch slowest-20 output each run.

## Suggested next run

Address pre-existing `ECON_BOT_002` / smoke hash failures in a dedicated gameplay/recovery run before treating Tier 2 closure as merge-blocking on `main`. Continue using RUN_02.8c tiered workflows for day-to-day iteration.

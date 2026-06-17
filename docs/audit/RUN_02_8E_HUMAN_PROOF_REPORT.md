# RUN_02.8e — Human Proof Report

Date: 2026-06-17  
Branch: `feature/run-02-8c-test-runtime-efficiency`  
Starting commit: `be94538` (02.8d WIP stashed as `02.8d-wip-bot-chain`)  
**Status: MERGE-READY for harness work** — `ECON_BOT_002` still red (expected)

## Outcome classification

**Primary: C (partial)** — Scenario is passable via legal commands (oracle completes one building); bot shortlist/planning still fails full gates. Full **b≥5** human/oracle path not yet demonstrated.

| Class | Assessment |
|-------|------------|
| A | **Ruled out** for one-building path — oracle fixture passes |
| B | **Unknown** — interactive UI not manually playtested this run |
| C | **Likely** — bot b≈1–2 @800–3000 ticks; oracle b=1 with explicit JSON |
| D | **Not indicated** — one building completes within tick budget |
| E | **Not indicated** — contract metrics match `DualBotRunner` in tests |

## Deliverables

| Item | Path |
|------|------|
| Scenario contract | `docs/audit/RUN_02_8E_HUMAN_PROOF_SCENARIO_CONTRACT.md` |
| Run doc | `docs/runs/RUN_02_8E_HUMAN_PROOF_SCENARIO_VIABILITY.md` |
| Playtest guide | `docs/audit/RUN_02_8E_HUMAN_PROOF_PLAYTEST_GUIDE.md` |
| Oracle CLI | `godot_project/sim/tools/run_economy_human_proof_oracle.gd` |
| Replay CLI | `godot_project/sim/tools/run_economy_human_proof_replay.gd` |
| Smoke | `godot_project/sim/tools/run_economy_human_proof_smoke.gd` |
| Interactive scene | `godot_project/client/scenes/economy_human_proof_demo.tscn` |
| Oracle fixture | `godot_project/sim/tests/fixtures/human_proof/oracle_one_building_p1.json` |

## Verification (exact commands, Linux Godot 4.5.2)

| Command | Exit | Status |
|---------|------|--------|
| Oracle one-building fixture | 0 | passed |
| `TEST_FILTER=econ_human_proof` (4 tests) | 0 | 4/4 passed |
| `run_economy_human_proof_smoke.gd --smoke=all` | 0 | passed |
| `TEST_FILTER=econ_bot_002` | 1 | failed (buildings gate) |
| `TEST_FILTER=econ_bot_b001` | 0 | passed |
| `TEST_FILTER=econ_bot_c001` | 0 | passed |
| `TEST_FILTER=econ_bot_g001` | 0 | passed |
| `verify_failure_modes.gd` | 0 | INTEGRITY PASS |

## Oracle result (committed fixture)

After plan + 500 idle ticks: **p1 b=1, r=6, d=234** — one woodcutter completed from exact proof start.

## ECON_BOT_002

Still **exit 1** (p1/p2 buildings). Unchanged thresholds. Not a failure of this run.

## Known limitations

- Full five-building oracle script not yet authored
- No manual interactive playtest session recorded
- 02.8d bot WIP preserved in git stash, not on branch

## Next recommendation

1. Extend oracle JSON toward **b≥5** (both players) to prove or disprove full gate fairness (outcome A vs C).
2. Manual interactive playtest using playtest guide (check outcome B).
3. Resume bot chain fix on separate branch from stash with oracle golden script as reference.

# RUN_02.8 — Audit Final Report

Date: 2026-06-16

Type: documentation / audit + ratification — **no code changes**

## Audit conclusion

**The repository cannot yet support a full deterministic two-player economy-only bot-vs-bot expansion match end-to-end.**

RUN_02.7 on `main` (`3236685`) provides the required logistics foundation: ports, delivery jobs, carrier-held transit, construction via delivery + `builder_work`, diagnostics, snapshot/export logistics fields, and viewer logistics observability.

What remains for implementation:

- Dual-player economy bootstrap (`spawns[]`, per-player HQ, starters, ownership)
- Player-scoped economy safety with **isolated per-player road/logistics networks**
- Per-player economy scoring
- Rule-based `EconomyExpansionBot` + `DualBotRunner` (strict alternation)
- Symmetric two-player scenario/map with partitioned resources
- Headless smokes (BOT-SMOKE-004, ECON-SMOKE-007) and dual-bot replay hash
- Viewer ownership overlay + dual scoreboard + contact sheet

**Recommendation:** Proceed with RUN_02.8 implementation on `feature/run-02-8-bot-vs-bot-economy` using ratified design decisions below. Defer neural bots.

---

## Files created / updated (this audit + ratification pass)

### Audit (initial pass)

| File | Role |
|------|------|
| `docs/audit/RUN_02_8_BOT_VS_BOT_ECONOMY_CURRENT_STATE.md` | Current-state audit (6 question groups) |
| `docs/audit/RUN_02_8_BOT_VS_BOT_ECONOMY_GAP_REGISTER.md` | 33 gaps, risks, phase mapping |
| `docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION_PLAN_DRAFT.md` | Initial plan draft (now redirect stub) |

### Ratification (this pass)

| File | Role |
|------|------|
| `docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md` | **Authoritative implementation run document** |
| `docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION_PLAN_DRAFT.md` | Redirect stub → authoritative run doc |
| `docs/testing/01_rule_contract_test_matrix.md` | RUN_02.8 planned tests |
| `docs/testing/02_smoke_test_matrix.md` | ECON-SMOKE-007, BOT-SMOKE-004 |
| `docs/testing/03_godot_viewer_test_plan.md` | RUN_02.8 viewer review expectations |
| `docs/verification/local_verification.md` | Planned RUN_02.8 verification commands |
| `docs/audit/RUN_02_8_AUDIT_FINAL_REPORT.md` | This report |

---

## Ratified design decisions (locked for implementation)

| Decision | Answer |
|----------|--------|
| Bots | Deterministic rule-based expansion bots; **not** neural |
| Match | Two-player economy-only; no combat |
| Map | Symmetric mirrored spawns |
| Resources | Mirrored/partitioned pockets — not shared centre |
| Roads | **Isolated per-player logistics graphs** — no shared roads/nodes |
| Turns | p1 command → p2 command → simulation tick |
| Bot observation | Full-map via snapshot + `enumerate_legal_commands` only |
| Bot commands | Normal command API only; no raw `GameState` reads |
| Scoring | `10×completed + 5×active_prod + stock/10 + deliveries − 2×blocked_jobs` |
| Roads in score | **Not scored** — required for minimum-match gates only |
| Stopping | Fixed `max_ticks` + stall detection |
| Viewer | Ownership overlay + scoreboard **required** |
| Screenshots | Contact sheet: start / early / mid / late / final + logistics |

**Pathfinding compatibility:** Optional `player_id` on pathfinding; preserve single-player behaviour when omitted; dual-player code passes explicitly.

---

## Implementation phases (summary)

| Phase | Deliverable |
|-------|-------------|
| 0 | Audit + ratification (complete); baseline at branch start |
| 1 | Two-player bootstrap — ECON-MP-001 |
| 2 | Player-scoped safety + isolated networks — ECON-MP-002 |
| 3 | Per-player `economy_scores` |
| 4 | `EconomyExpansionBot` — ECON-BOT-004 |
| 5 | `DualBotRunner` + full symmetric scenario |
| 6 | BOT-SMOKE-004, ECON-SMOKE-007, ECON-BOT-002/003 |
| 7 | Viewer ownership overlay + scoreboard + contact sheet |
| 8 | Docs, regression, implementation final report |

Full detail: [`docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md`](RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md).

---

## Verification (this pass)

| Check | Result |
|-------|--------|
| Code changes | **None** — documentation only |
| Godot tests | **Not run** (not required for docs-only pass) |
| `git diff --check` | Run at commit time |

---

## Next implementation branch

```
feature/run-02-8-bot-vs-bot-economy
```

Branch from `main` after RUN_02.7 merge.

### Recommended implementation prompt

```
Implement RUN_02.8 per docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md on branch feature/run-02-8-bot-vs-bot-economy.

Start with pre-flight baseline (run_tests, ECON-SMOKE-001–006, GOD-SMOKE-001–003, verify_failure_modes).

Phase 1: economy spawns[] + dual-player init (ECON-MP-001).
Phase 2: player-scoped safety + isolated road networks (ECON-MP-002); pathfinding optional player_id with SP compat.
Then phases 3–8 per run doc.

Ratified: isolated per-player logistics, strict alternation, rule-based bots via snapshot/legal commands only, simple economy score, viewer ownership overlay + scoreboard required.
```

---

## Out of scope confirmation (audit passes)

- No GDScript changes
- No bot implementation
- No scenario/map implementation
- No tests added
- No viewer code
- No config changes
- No smoke runner changes

---

## Suggested next run

**RUN_02.8 implementation** — `feature/run-02-8-bot-vs-bot-economy`, Phase 0 baseline then Phase 1 two-player bootstrap.

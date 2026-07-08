# Final PRD Rationale — Why This Document Exists

**Date:** 2026-07-08  
**Companion PRD:** [`docs/prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`](../prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md)  
**Companion run:** [`docs/runs/RUN_FINAL_GOD_STRATEGY_PLAYABLE.md`](../runs/RUN_FINAL_GOD_STRATEGY_PLAYABLE.md)

---

## Problem

Long autonomous runs produced strong **infrastructure** (sim kernel, trial games, Settler legibility, hybrid MVP, neural scaffolds, God-Strategy A+B) but the project kept **redefining “done”** across overlapping PRDs. Agents claimed progress on scaffolds while the human-visible product — a finished 4P match against real AI on a mobile-shaped UI with reviewable screenshots — remained unproven.

Competing targets included:

- Formal RUN_07–10 hybrid/mobile sequence (checklists often stale vs code)
- Neural Hybrid umbrella PRD (Part D = true DoD, but hybrid-not-God-Strategy)
- God-Strategy MVP PRD (full fantasy A–H, but soft mobile/neural end-state)
- Ad-hoc MRP / overnight runs that advanced code without a single product sentence

---

## What worked (encode these)

| Pattern | Source |
|---------|--------|
| Test-first headless GDScript harness | RUN_01–04 |
| Micro-scenario ladder before long ticks | God-Strategy A hardening |
| Settler DISPLAY screenshots + node probes + review JSON | Tutorial legibility |
| Tiered `TEST_FILTER` + honest status vocabulary | Rule 10 / RUN_02.8c |
| Teachers that actually complete objectives before training | Settler/Hybrid tutorial bots; inverse lesson from weak neural imitation |
| One hard gate per phase; stop and gap-report | DDR-028 + current-state audit |

---

## What failed (forbid these)

| Failure | Consequence in Final PRD |
|---------|--------------------------|
| Over-scoped umbrella without kill switches | Phases 0–8 with fail-closed exits |
| Scaffold tests treated as product | Explicit “scaffold ≠ product” rule |
| Tick-cap wins reported as competitive loop | Phase 0 requires true domination |
| Neural training from non-winning teachers | Phase 5–6 teacher/win-rate gates |
| Headless 1×1 screenshots as UI proof | DISPLAY + committed PNG pack required |
| Multiple informal “almost done” PRDs | Single superseding product DoD |
| Mobile/neural polish before victory loop | Phase 8 last; Phase 0/6 blockers |

---

## Locked choices this PRD encodes

1. **Full God-Strategy** fantasy (ages, culture, workers, pollution) — not a retreat to Hybrid-MVP-only.
2. **4P neural required** for the human match — closest to Neural Part D discipline, applied to God-Strategy.
3. **Heuristic/scripted victory first** remains a **gate** (Phases 0 and 5), not the final claim.
4. **Incomplete is default** unless Phase 0 and Phase 6 pass; one DDR-028 split only after Phase 6.

---

## How to use this

- Approve the Final PRD text before any implementation long run.
- Treat prior God-Strategy and Neural Hybrid PRDs as **references**, not alternate DoDs.
- If Phase 0 or Phase 6 fails, ship a gap report — do not invent a fourth “final” PRD mid-run.

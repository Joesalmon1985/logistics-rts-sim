# PRD — Final God-Strategy Playable Product

**Status:** Ready for human review — **no implementation authorized until this PRD is approved**  
**Primary design source:** [`docs/design/HYBRID_GOD_STRATEGY_DESIGN_GOALS_FINAL.md`](../design/HYBRID_GOD_STRATEGY_DESIGN_GOALS_FINAL.md)  
**Systems reference (non-DoD):** [`docs/prd/HYBRID_GOD_STRATEGY_MVP_PRD.md`](HYBRID_GOD_STRATEGY_MVP_PRD.md)  
**Product-end-state pattern:** [`docs/runs/PRD — Neural Hybrid AI, Spectator Legibility, and Mobile Playability.md`](../runs/PRD%20—%20Neural%20Hybrid%20AI,%20Spectator%20Legibility,%20and%20Mobile%20Playability.md) Part D discipline  
**Current-state grounding:** [`docs/audit/CURRENT_STATE_OF_PLAY_HYBRID_GAME.md`](../audit/CURRENT_STATE_OF_PLAY_HYBRID_GAME.md)  
**Companion run:** [`docs/runs/RUN_FINAL_GOD_STRATEGY_PLAYABLE.md`](../runs/RUN_FINAL_GOD_STRATEGY_PLAYABLE.md)  
**Rationale:** [`docs/audit/FINAL_PRD_RATIONALE.md`](../audit/FINAL_PRD_RATIONALE.md)  
**Suggested branch prefix:** `feature/final-god-strategy-*`

---

## Document control

| Field | Value |
|-------|-------|
| Product name (working) | WideRTS — Final God-Strategy Playable |
| Target platform (MVP) | Godot 4.5; headless-first sim; mobile-shaped 2D client for product DoD |
| Authoritative runtime | Godot/GDScript simulation core (not Python, not scene tree) |
| **Sole product Definition of Done** | This document |
| Supersedes (product DoD) | God-Strategy MVP PRD completion claims; Neural Hybrid PRD Part D completion claims; informal “hybrid playable” claims |
| Does not supersede | RUN_01 kernel; RUN_02–04 trial contracts; HYB-SMOKE 001–009; Settler legibility verification patterns; design goals fantasy |
| Implementation run | Only after explicit human approval of this PRD text |

**Supersession statement:** This PRD is the **sole product Definition of Done** for the final long run. Prior God-Strategy and Neural Hybrid PRDs remain **design and infrastructure references**. Their scaffold, milestone, or Part A–C “complete” claims do **not** satisfy this DoD.

---

## 1. Product sentence (Definition of Done)

> A human can launch and finish a **4-player God-Strategy FFA** on a **mobile-shaped** screen (touch-first: pan/pinch/tap, draft, build/road, delegated attack, no unit micro), against **three neural modular god opponents** whose checkpoints **beat passive and heuristic baselines** in headless eval, with the match ending under **documented God-Strategy victory rules** (domination / Age 3 VP / Age 4 restoration or shared collapse — not disguised tick-cap stall), and with a **committed DISPLAY screenshot pack** proving HUD/inspector/age/draft/combat/end-screen legibility for manual review.

**This PRD is incomplete unless that sentence is literally true.**

Training scaffolds, layout profiles, checkpoint load tests, headless ticks-without-crash, and SubViewport 1×1 PNGs are **infrastructure only**. They never close the PRD.

---

## 2. Locked product decisions

| Decision | Choice |
|----------|--------|
| Game identity | **Full God-Strategy** — four ages, culture drafting, worker needs/society, pollution/collapse |
| Match format | **4P FFA** (2P hybrid MVP remains tutorial/regression only) |
| Human opponents | **Neural** modular god bots (`LearnedModularGodBot` / modular god nets) — no silent scripted fallback in product mode |
| Training bar | Checkpoints must **beat passive and heuristic** baselines in committed headless eval |
| Match endings | Documented victory rules; **tick-cap-only is not a product win** |
| Human UX | Mobile-shaped touch controls + understandable HUD/inspector |
| Screenshot bar | **Real DISPLAY** screenshots, automated checks, **committed under `docs/audit/screenshots/god_strategy_final/`** for manual review |

---

## 3. Risk acknowledgment

This is the **highest-risk** scope the project can pick.

- **Incomplete is the default outcome** unless Phase 0 (domination) and Phase 6 (neural vs heuristic) hard gates pass.
- UI, spectator, mobile, and neural work done **before** those gates is infrastructure only.
- The long run **must stop** at the first failed hard gate and ship a gap report rather than “almost complete” claims.
- A single DDR-028 A/B split is allowed **once**, only after Phase 6 (see §11). Silent multi-split is forbidden.

---

## 4. Explicit non-goals

Do **not** implement under this PRD unless a later amendment adds them:

- WiFi multiplayer / RUN_12 networking
- Commercial store packaging / final art pass
- Full 3D wizard embodiment or micro spell combat
- Deep industrial chains beyond the frozen catalogue (§8)
- Aircraft, naval, large unit rosters beyond one unit role per age
- One independent neural network per worker
- Obvious UI warnings (“this factory will cause collapse”)
- Claiming “strong AI” from imitation checkpoints without Phase 6 win-rate tables
- Claiming product complete with scripted opponents in the human match
- Claiming UI success from headless SubViewport 1×1 captures
- Replacing Godot sim authority with Python runtime

---

## 5. Current project reality (honest baseline)

*Grounded in audits as of mid-2026. Where docs conflict with code, code + recent audits win.*

### 5.1 What already exists (reuse — do not rewrite)

| Area | Status |
|------|--------|
| Deterministic sim kernel (RUN_01) | Strong |
| Trial economy + military + hybrid integration | Strong / HYB-SMOKE 001–009 |
| Settler MVP + tutorial legibility + screenshot gates | Strong product pattern to copy |
| 2P Hybrid MVP UI + `TutorialHybridBot` | Playable desktop slice |
| God-Strategy Milestone A+B tests | Green on overnight run; **domination unproven** |
| Modular neural hybrid scaffold | Load/inference path; **not competitive** (~49% imitation historically) |
| Mobile layout profiles / FFA shell | Scaffold |
| 2D/3D spectators | Partial; 3D 2-bot strongest |

### 5.2 What does not exist yet (required for this DoD)

| Area | Notes |
|------|-------|
| Proven scripted **domination** vs 3 passive | Tick-cap wins only at 3000-tick proof cap |
| Milestones C–H (workers, modular god AI, pollution, mobile product) | Not started or scaffold-only |
| Competitive neural god AI | No eval winner vs heuristic/passive at product bar |
| God-Strategy mobile human-vs-neural mode | Shells exist for hybrid FFA; not God-Strategy product |
| DISPLAY screenshot pack for God-Strategy 4P | Missing |
| Closed open questions from prior God-Strategy PRD §28 | Closed in this PRD §12 |

### 5.3 Prerequisite WIP note

Branch `feature/god-strategy-milestone-a-hardening` may hold uncommitted hardening work. The eventual implementation run must treat **Phase 0 domination** as the first hard gate and must not discard that WIP without an explicit merge/gap decision.

---

## 6. Systems contract (fantasy — do not redesign)

Full rules live in the design goals doc and the God-Strategy MVP PRD §§7–21. This section binds the final product to those systems without inventing a fourth vision.

### 6.1 Four ages

| Age | ID | Theme |
|-----|-----|-------|
| 1 | `prehistoric` | Survival, gathering, clan culture |
| 2 | `historic` | Settlement, law, professional war |
| 3 | `modern` | Industry, power, mechanised war — pollution tension |
| 4 | `futuristic_collapse` | Scarcity, restoration, catastrophe |

Age advances after **8 draft picks per player** in the current age, unless Age 3 victory ends the match early.

### 6.2 Cultural drafting

- 4 players; **8 picks per player per age**.
- Deal pattern: **dealt-hand** (8 cards), pick on `draft_interval_ticks` (default **500**).
- Cards are strategic technology: unlocks, modifiers, VP, worker pressure, pollution, mitigation.
- MVP pools: 12–16 cards/age; start with 4–6 mechanically distinct templates.

### 6.3 Victory priority (checked each tick)

1. Shared collapse loss (Age 4 only)
2. Domination — sole surviving Command Hub (hub rebuild site rule applies)
3. Age 3 civilisation victory — exactly one survivor with ≥10 VP
4. Age 4 restoration victory — collective restoration goals; highest score leads
5. Tick-cap survival score — **safety net only**; never counts as Phase 0/6 “domination proof”
6. Stalemate → **survival-adjusted score** (forced escalation deferred)

### 6.4 Economy and military

- Physical logistics: roads, carriers, storage; no ware teleportation.
- Ware-fed military construction/production; **no credits**.
- Delegated god control only; direct unit micro rejected.
- Logistics warfare: roads, carriers, storehouses are valid targets.
- One core military squad role per age (age-flavoured presentation).

### 6.5 Workers

- Needs (hunger, tiredness, happiness, safety) affect throughput — **cohort-level first**.
- Pluggable `WorkerPolicy` with deterministic fallback.
- Societal selection shifts behaviour prevalence from fitness + culture + environment.
- Optional small archetype nets only after deterministic selection proofs; not required for Phase 8 DoD.

### 6.6 Pollution and collapse

- Local + global pollution; non-linear harm in Ages 3–4.
- 2–3 disaster types; scarcity; restoration and shared-collapse endings.
- No blunt “this factory causes collapse” warning UX.

### 6.7 God AI architecture

1. `ScriptedGodStrategyBot` — Phase 0–1 teacher / regression
2. `HeuristicModularGodBot` — Phase 5 module proposals + coordinator
3. `LearnedModularGodBot` — Phase 6+ learned modules; **required** for Phase 8 opponents

Coordinator never bypasses `CommandValidator`. Spectator must show inspectable module proposals / intent.

---

## 7. Hard rules (non-negotiable)

1. **One product sentence** — PRD incomplete unless §1 is literally true.
2. **Scaffold ≠ product** — load/tick/checkpoint/layout tests never close a phase.
3. **Phase kill switches** — later phases may not start until prior eval artefacts prove endings.
4. **Micro ladder before long ticks** — isolate failures in short scenarios before 3000+ tick proofs.
5. **DISPLAY screenshots** — SubViewport 1×1 is failure; require `DISPLAY`, named PNGs + review JSON/MD, commit to git.
6. **Node probes + screenshots** — both required for UI phases.
7. **Tiered verification** — `TEST_FILTER` during work; full suite at phase boundaries; long-eval timeouts ≥2400–7200s.
8. **Teachers that win** — no neural promotion from bots that never eliminate; scripted/heuristic must beat passive before imitation/RL.
9. **Single DDR-028 A/B split** — once only, after Phase 6, with gap report (see §11).
10. **Freeze catalogue** until one end-to-end neural win (§8).

---

## 8. Frozen catalogue (until Phase 6 neural win)

Do not expand beyond:

- Command Hub + hub rebuild site rules
- Wood chain (woodcutter, forester, sawmill) + quarry/stone as already hybrid-tested
- One storehouse / logistics node type as needed for deliveries
- Barracks (or age-equivalent military producer) + one squad role per age
- Draft card **templates** (4–6 mechanical kinds), not full commercial decks
- Existing hybrid delegated attack / stance / guard / patrol / repair priority surface

Age flavour is presentation + unlock gating via roles — not a new deep industrial tree.

---

## 9. Phased implementation (0–8)

Each phase is a **merge boundary** with its own branch slug, tests, eval artefacts, and kill switch.  
Map to prior God-Strategy milestones: 0≈A harden, 1≈B, 2≈C, 3≈D, 4≈E, 5≈F0, 6≈F1, 7≈G, 8≈H productized.

```text
Phase0 → Phase1 → Phase2 → Phase3 → Phase4 → Phase5 → Phase6 → Phase7 → Phase8
```

### Phase 0 — Domination hardening (hard gate)

**Goal:** Scripted 4P logistics RTS reaches **true domination** (hub elimination), not disguised tick-cap.

| In scope | Out of scope |
|----------|--------------|
| Finish A hardening; micro ladder; domination regression | Ages, culture, workers, pollution, neural, mobile polish |
| Logistics warfare proofs | Catalogue expansion |
| `ScriptedGodStrategyBot` beats 3 passive | Claiming product playable |

**Hard exit (must fail closed):**

- [ ] Fixed-seed domination regression writes `winner` / match-end reason = domination (not tick-cap-as-domination).
- [ ] ≥30% of eval seeds reach domination within documented `T` ticks **or** explicit combat-speed variant proves elimination with committed artefacts (document which).
- [ ] Logistics warfare test: road damage blocks deliveries.
- [ ] HYB-SMOKE 001–009 still pass.
- [ ] Eval artefacts under `docs/audit/god_strategy/final/phase0/`.

**Evidence before Phase 1:** `winner.txt` (or equivalent), `match_summary.json`, `bot_phase_progression.json`, replay hash, test log.

---

### Phase 1 — Ages + culture (prior Milestone B)

**Goal:** Four ages, 8 picks/player/age, VP ledger, Age 3 VP check.

**Hard exit:**

- [ ] Deterministic draft schedule (`draft_interval_ticks = 500`).
- [ ] Card unlock enables building in snapshot.
- [ ] Age 3: sole ≥10 VP wins; multi ≥10 VP → Age 4.
- [ ] Headless match reaches Age 2+ in smoke.
- [ ] GOD_B (or successor) tests green; artefacts under `docs/audit/god_strategy/final/phase1/`.

---

### Phase 2 — Worker needs (prior Milestone C)

**Goal:** Cohort needs affect throughput; deterministic satisfaction.

**Hard exit:**

- [ ] Needs deterministic across replay hash.
- [ ] Measurable throughput delta when needs neglected.
- [ ] Snapshot/telemetry exposes cohort need averages.
- [ ] Artefacts under `docs/audit/god_strategy/final/phase2/`.

---

### Phase 3 — Worker policy interface (prior Milestone D)

**Goal:** Pluggable `WorkerPolicy`; legal masks; deterministic fallback.

**Hard exit:**

- [ ] Random/legal test policy runs 4P without crash.
- [ ] Invalid actions rejected/masked.
- [ ] Perf note documented (ticks/sec with X workers).
- [ ] Artefacts under `docs/audit/god_strategy/final/phase3/`.

---

### Phase 4 — Societal selection (prior Milestone E)

**Goal:** Behaviour prevalence shifts from fitness + culture + environment.

**Hard exit:**

- [ ] Two culture paths diverge behaviour distribution in fixed scenario.
- [ ] Replay-deterministic selection given seed.
- [ ] Telemetry JSONL committed under `docs/audit/god_strategy/final/phase4/`.

---

### Phase 5 — Heuristic modular god AI (F0)

**Goal:** Inspectable module proposals + coordinator; heuristic teachers that win.

**Hard exit:**

- [ ] Each module emits 0–3 inspectable proposals; coordinator never bypasses validator.
- [ ] Heuristic modular bot **beats** Phase 0 scripted baseline on a committed win-rate table (≥20% relative improvement or absolute win-rate bar documented in run doc — raise over time, never lower).
- [ ] Spectator/HUD can show module summaries (headless telemetry sufficient to unlock Phase 6; DISPLAY screenshots required by Phase 8).
- [ ] Artefacts under `docs/audit/god_strategy/final/phase5/`.

**Kill switch:** Do not start neural training (Phase 6) if heuristic teachers cannot beat passive and show non-zero elimination rate.

---

### Phase 6 — Neural god training (F1) — product AI bar

**Goal:** Train modular god nets from **winning** teachers; prove neural beats passive **and** heuristic.

| In scope | Out of scope |
|----------|--------------|
| BC (and optional light RL) via Python↔Godot headless (RUN_06 transport pattern) | Claiming Phase 8 complete |
| Eval harness with win/loss tables | Mobile polish |
| Checkpoints in gitignored `models/` / `checkpoints/` | Expanding catalogue |

**Hard exit:**

- [ ] Training run produces checkpoints + committed eval JSON (not weights in git).
- [ ] Neural bot **beats passive** baseline (win-rate table, ≥N seeds, documented).
- [ ] Neural bot **beats heuristic** baseline (win-rate table, ≥N seeds, documented).
- [ ] ≥1 documented 4P match ends with **neural winner** under domination or Age 3/4 rules (not tick-cap-only).
- [ ] Invalid action rate below documented threshold; fog-limited obs for eval bots.
- [ ] Artefacts under `docs/audit/god_strategy/final/phase6/`.

**Kill switch:** If Phase 6 fails, stop. Do not start Phase 7–8 product claims. Optional DDR-028 split only **after** this gate passes (§11).

---

### Phase 7 — Pollution, scarcity, collapse (prior Milestone G)

**Goal:** Ages 3–4 pollution and Age 4 endings exist before human productization.

**Hard exit:**

- [ ] Industrial Age 3 choices measurably degrade forests/farms/workers without warning pop-ups.
- [ ] Shared collapse loss testable at extreme pollution.
- [ ] Restoration victory path testable in scripted scenario.
- [ ] Artefacts under `docs/audit/god_strategy/final/phase7/`.

---

### Phase 8 — Mobile human vs three neural (sole product gate)

**Goal:** Make §1 true.

| In scope | Out of scope |
|----------|--------------|
| Touch-first God-Strategy 4P FFA; draft UX; build/road; delegated attack | Store release; WiFi MP |
| Victory/defeat UX | Full commercial art |
| DISPLAY screenshot pack + node probes | Silent scripted opponents |
| Mobile layout QA (priority profiles) | Claiming success without Phase 6 checkpoints |

**Hard exit:**

- [ ] Human P1 vs neural P2–P4 using Phase 6 checkpoints (report must not say fallback).
- [ ] Touch: pan, pinch or +/- zoom, tap select/inspect, draft pick, build/road, delegated attack — **no** required hover/right-click/keyboard.
- [ ] Touch targets ≥44px equivalent on priority mobile profiles (at least 390×844 and one landscape profile).
- [ ] Match can end with victory/defeat screen under documented rules.
- [ ] **DISPLAY** screenshot sequence committed (see §10); review JSON/MD pass automated probes.
- [ ] Manual review path: screenshots pushed to git for human inspection.
- [ ] Artefacts under `docs/audit/god_strategy/final/phase8/` and `docs/audit/screenshots/god_strategy_final/`.

**Only Phase 8 may claim this PRD complete.**

---

## 10. Screenshot and legibility contract

### 10.1 Method

- Require a real `DISPLAY` (Xvfb or desktop). Headless SubViewport 1×1 PNGs are **failures**.
- Reuse Settler patterns: capture scripts + `verify_human_understandable_*`-style probes + review JSON/MD.
- Commit PNGs + review artefacts to git under `docs/audit/screenshots/god_strategy_final/`.

### 10.2 Required named sequence (minimum)

| ID | Content |
|----|---------|
| `gs_final_00_main_menu.png` | Main menu entry visible |
| `gs_final_01_match_start.png` | 4P match HUD; player identity clear |
| `gs_final_02_age_draft.png` | Age + draft hand/pick UI readable |
| `gs_final_03_economy.png` | Buildings/roads/carriers + inspector text |
| `gs_final_04_delegated_attack.png` | Delegated military order UI + intent |
| `gs_final_05_welfare_pollution.png` | Worker welfare and/or pollution HUD (Phase 7+) |
| `gs_final_06_ai_intent.png` | Neural/heuristic intent or module summary readable |
| `gs_final_07_end_screen.png` | Victory or defeat screen |

### 10.3 Fail conditions

- Map-only screenshots with no HUD/inspector
- Clipped critical panels
- Touch targets &lt;44px on required profiles
- 1×1 or blank captures
- Tutorial/draft buttons hidden off-screen
- Claiming readability without review JSON/MD

---

## 11. DDR-028 single A/B split rule

Preferred plan: one dedicated long run completes Phases 0–8.

If the run cannot complete safely, **one** controlled split is allowed:

| Split | Contents |
|-------|----------|
| **Final-A** | Phases 0–6 inclusive (sim + heuristic + **neural win-rate gate**) |
| **Final-B** | Phases 7–8 (pollution + mobile human-vs-neural product) |

Rules:

- Split only **after** Phase 6 hard exit passes and is committed + pushed.
- Document exactly what passed, what remains, and produce a gap report.
- Do **not** weaken Definitions of Done.
- Do **not** split earlier (e.g. after Phase 0 only) under DDR-028 for this PRD.
- Do **not** silently create further splits.

---

## 12. Closed open questions (defaults for this PRD)

Prior God-Strategy PRD §28 items are **closed** here for implementation:

| # | Question | Decision |
|---|----------|----------|
| 1 | Draft deal pattern | **Dealt-hand**, 8 picks/player/age |
| 2 | `draft_interval_ticks` | **500** (age scaling deferred; constant OK for MVP) |
| 3 | `worker_policy_refresh_ticks` | **300** |
| 4 | Worker granularity | **Cohort-level first** |
| 5 | Global pollution curve | Superlinear; coefficients in `pollution.json`; tune in Phase 7 tests — start with documented defaults in run doc |
| 6 | Restoration thresholds | Scripted scenario constants in Phase 7; domination remains valid throughout |
| 7 | Stalemate | **Survival-adjusted score** (no forced escalation event in MVP) |
| 8 | Alliance logistics | **Owned-only** for MVP |
| 9 | Scout/hero marker | **Cosmetic** for MVP |
| 10 | Age 4 unit naming | Reuse squad code under age role IDs |
| 11 | Ware role migration | Role indirection in Phase 1; keep concrete IDs in regression tests until dual-read passes |
| 12 | 2P hybrid MVP | **Frozen regression/tutorial**; not the product DoD |
| 13 | Learned AI win-rate bar | Phase 5 heuristic ≥ documented bar vs scripted; Phase 6 neural **must beat passive and heuristic** on committed tables |

---

## 13. Training contract

### 13.1 Authority and transport

- Godot/GDScript sim is authoritative.
- Python is training/tooling only (`python_tools/training/`).
- Reuse RUN_06 **file-session** Godot headless transport patterns (stdin JSON-lines historically failed on Godot 4.5.x headless).
- Integer/fixed-point hashed state; seeded RNG; legal actions only.

### 13.2 Promotion gates

| Gate | Requirement |
|------|-------------|
| G0 | Kernel + HYB-SMOKE + failure-mode integrity pass |
| G1 | Phase 0 eval reaches documented **domination** endings (not tick-cap-only proof) |
| G2 | Fog-limited observations for eval bots |
| G3 | Invalid action rate &lt; threshold |
| G4 | Beats **passive** baseline |
| G5 | Beats **heuristic** baseline + ≥1 neural 4P ending artefact |
| G6 | Phase 8 human match loads Phase 6 checkpoints (no silent fallback) |

### 13.3 Forbidden claims

- “Training succeeded” from loss curves alone
- “Neural played a full game” when only scripted bots ran
- Part D / product complete with heuristic opponents
- Committing model weights unless explicitly requested (default: gitignore `models/`, `checkpoints/`)

---

## 14. Mobile and controls contract

### 14.1 Must support

- Tap select / inspect
- Drag pan
- Pinch and/or on-screen +/- zoom
- Bottom-sheet or equivalent inspector
- Draft pick UI
- Build / road / delegated order drawers
- Large Play/Pause/speed controls where time controls exist
- Victory / defeat screens

### 14.2 Must not require

- Right-click, hover tooltips, mouse wheel, or keyboard shortcuts for core play

### 14.3 Inference

- Neural inference on device/profile must not freeze play (tick-budget or async).
- No Python runtime embedded for gameplay (DDR-026).

### 14.4 Layout profiles

Reuse `mobile_layout_profiles.gd` priority set; Phase 8 must pass at least **390×844** and one landscape profile with node probes + screenshots.

---

## 15. Verification matrix

Status vocabulary (never collapse): **passed** | **failed** | **errored** | **timed out** | **not run**.

Authoritative command reference: [`docs/verification/local_verification.md`](../verification/local_verification.md) — update during the run when new runners land.

### 15.1 Preflight (every phase start)

```bash
export GODOT=...   # full path; Godot may not be on PATH
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s "$GODOT" --headless --path godot_project --import
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
TEST_FILTER=HYB_SMOKE timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
```

### 15.2 Phase filters (illustrative — finalize in run doc)

| Phase | Filter / runner |
|-------|-----------------|
| 0 | `TEST_FILTER=GOD_A` + `run_god_strategy_eval.gd` domination regression (timeout ≥2400–7200s) |
| 1 | `TEST_FILTER=GOD_B` |
| 2–4 | `TEST_FILTER=GOD_C` / `GOD_D` / `GOD_E` (as added) |
| 5–6 | God AI eval runners + Python training entrypoints; commit eval JSON |
| 7 | `TEST_FILTER=POLL` / `GOD_G` |
| 8 | Mobile QA + DISPLAY screenshot capture/verify; `docs/audit/screenshots/god_strategy_final/` |

### 15.3 Artefact roots

- Eval: `docs/audit/god_strategy/final/phaseN/`
- Screenshots: `docs/audit/screenshots/god_strategy_final/`
- Lessons: `docs/audit/RUN_FINAL_GOD_STRATEGY_PLAYABLE_LESSONS_LEARNED.md` (end of run)

---

## 16. Operational rules for the eventual long run

1. Read this PRD + companion run doc + `.cursor/rules/01`, `02`, `03`, `07`, `08`, `10`.
2. Report `git status`, branch, recent log; do not clobber unrelated WIP.
3. Work on `feature/final-god-strategy-<phase-slug>`; never on `main`.
4. Preflight kernel verification before Phase 0 edits.
5. Test-first per mechanic family; never weaken failure-mode integrity.
6. One phase at a time; commit + push at each hard exit.
7. Stop at first failed hard gate; write gap report.
8. Never claim PRD complete without Phase 8 sentence true.
9. End with final report format (`.cursor/rules/07-final-report-format.mdc`) and lessons-learned file.

**Do not start implementation until a human explicitly approves this PRD text.**

---

## 17. Relationship to prior PRDs

| Document | Role after this PRD |
|----------|---------------------|
| `HYBRID_GOD_STRATEGY_MVP_PRD.md` | Systems/milestone reference; **not** product DoD |
| Neural Hybrid AI / Mobile PRD | Infrastructure + Part D *discipline*; hybrid FFA product claims **superseded** |
| RUN_07–10 | Foundations / partial delivery; formal checklists may lag code |
| Settler Tutorial Legibility PRD | Screenshot/probe pattern to copy |
| `CURRENT_STATE_OF_PLAY_HYBRID_GAME.md` | Baseline honesty; heuristic-first advice becomes **Phase 0/5 gates**, not final DoD |

---

## 18. MVP completion checklist (all required)

- [ ] Phases 0–7 hard exits met with committed eval artefacts
- [ ] Phase 6 neural beats passive **and** heuristic; ≥1 neural 4P ending artefact
- [ ] Phase 8 human-vs-neural mobile match playable; victory/defeat UX works
- [ ] DISPLAY screenshot pack committed and review JSON/MD pass
- [ ] HYB-SMOKE + failure-mode integrity pass on Linux Godot 4.5.x
- [ ] Catalogue remained frozen until Phase 6 neural win (or explicit amendment)
- [ ] Honest audit successor updated
- [ ] §1 product sentence is literally true

---

## Appendix A — Suggested module layout (informational)

Reuse God-Strategy MVP PRD Appendix A (`godot_project/sim/god_strategy/…`). Prefer extending existing hybrid + god_strategy modules over greenfield rewrites.

---

## Appendix B — One-sentence reminder

Ship a human-playable, screenshot-proven, mobile-shaped 4P God-Strategy FFA against neural modular god opponents that actually win matches — or stop at the first hard gate and say so.

---

*End of Final PRD.*

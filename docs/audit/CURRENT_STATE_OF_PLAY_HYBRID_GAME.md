# Current State of Play — Hybrid Logistics + RTS Game

Date: 2026-06-28  
Branch at audit: `feature/run-actual-modular-nn-training` (commit `5368617` pushed)  
Auditor scope: read-only repository inspection; no code changes in this task.

---

## 1. Executive summary

WideRTS is an **original Godot 4.5 mobile-strategy project** built around a **headless, deterministic GDScript simulation core** with a non-authoritative visual client. The repo contains **three variant layers** registered in the sim kernel:

1. **Trial Economy** — settlement logistics on a hex map (most mature).
2. **Trial Military** — standalone RTS skirmish with credits and direct unit control.
3. **Hybrid** — economy + military composed with **ware-fed construction/production**, **delegated military control**, and **no credits**.

**What kind of game exists today:** The project is **closest to a hybrid logistics + RTS simulation with partial human UI**, not yet a polished mobile product and not yet a strong neural-AI opponent. The **Settler MVP** is the most complete human-playable slice. The **Hybrid MVP** (2-player) is playable in desktop UI with tutorial support and passes headless HYB-SMOKE integration tests. **4-player hybrid FFA** exists in sim + bot tooling but **does not reliably reach elimination/victory** within practical tick budgets with current bots.

| Lens | Assessment |
|------|------------|
| Logistics settlement sim | **Strong** — extensive economy tests, Settler MVP, bot-vs-bot economy |
| Combat skirmish sim | **Strong (standalone)** — military MVP, skirmish scenarios, heuristic military bot |
| Hybrid logistics + RTS | **Partial** — core integration implemented and tested; full competitive loop weak |
| Neural-training environment | **Partial** — trial-game BC/eval (RUN_06) + hybrid modular pipeline (recent); quality low |
| Mobile-playable product | **Scaffolded** — layout profiles, mobile FFA shell, SubViewport tests; not product-polished |

**Playable today (human):**

- Settler MVP tutorial + free play (`settler_mvp_game.tscn`) — documented play guide exists.
- Military MVP skirmish (`military_mvp_game.tscn`).
- Hybrid MVP tutorial + skirmish (`hybrid_mvp_game.tscn`) — delegated commands, ware-fed military.
- Hybrid mobile FFA menu/game shell (`hybrid_mobile_ffa_*.tscn`) — human P1 vs three modular bots; **scaffold-level UX**.
- Main menu routes to spectators and MVPs (`main_menu.tscn`).

**Simulation-only / headless-first:**

- Dual-bot economy duels, quad-bot 4P hybrid matches, teacher-trace collection, neural eval runners.
- Most rule proofs live in `godot_project/sim/tests/` (351 tests registered in `run_tests.gd`).

**Scaffolded but not fully working as product claims:**

- **Modular neural hybrid bots** — real checkpoints and inference path exist; policy is imitation-grade (~49% arbiter accuracy); **no eval reached winner within 200 ticks**.
- **4P FFA as a complete competitive mode** — scenario + elimination rules exist; bots tick but matches stall at tick cap.
- **Mobile playability PRD** — layout profiles + a few automated tests; not a verified touch-first product.
- **2D 4P spectator** — functional with `DISPLAY`; headless SubViewport capture is 1×1 without display.

**Honesty note on docs vs code:** `docs/audit/RUN_HYBRID_MVP_CURRENT_STATE.md` (2026-06-26) describes hybrid as **missing** — that is **stale**. Hybrid sim, HYB-SMOKE tests, hybrid MVP client, and 3D spectator were added in later MRP/neural-hybrid work. Treat that audit as pre-implementation baseline only.

---

## 2. Current game modes

### Settler / Economy mode

| Field | Detail |
|-------|--------|
| **Paths** | Scenario: `godot_project/sim/data/scenarios/economy/settler_mvp.json`; UI: `godot_project/client/scenes/settler_mvp_game.tscn`; controller: `godot_project/client/core/settler_mvp_controller.gd` |
| **Player actions** | Place building plans, roads, set worker policies, priorities, upgrades; pause/step/speed controls |
| **End state** | Composite objective (woodcutter, forester, sawmill, quarry, stored planks/stone) via `settler_objective.gd` — **reachable** |
| **UI** | **Complete** desktop MVP with tutorial flow, objective panel, build palette |
| **Bots** | `TutorialEconomyBot`, `HeuristicEconomyBot`, `EconomyExpansionBot` — tutorial bot completes settler MVP in tests |
| **Tests/smokes** | Extensive `test_econ_*`, `test_scenario_settler_mvp`, `run_settler_mvp_smoke.gd`, `run_tutorial_bot_smoke.gd` |
| **Status** | **Most complete human mode** |

### Skirmish / Military mode (trial)

| Field | Detail |
|-------|--------|
| **Paths** | `scenarios/military/military_mvp_skirmish.json`, `military_mvp_game.tscn`, `military_mvp_controller.gd` |
| **Player actions** | Direct unit move/attack, build command hub/fabricator, queue units, **credits-based** trial economy |
| **End state** | Command Hub destruction victory — **reachable** in MVP scenarios |
| **UI** | Desktop MVP with tutorial; manual QA noted as "paper-thin UX" in places |
| **Bots** | `HeuristicMilitaryBot`, `TutorialMilitaryBot` (via smoke runners) |
| **Tests/smokes** | `test_mil_*`, `run_military_smoke.gd`, `run_tutorial_military_bot_smoke.gd` |
| **Status** | **Complete as standalone trial**; intentionally **not** the hybrid control model |

### Hybrid MVP (2-player)

| Field | Detail |
|-------|--------|
| **Paths** | `scenarios/hybrid/hybrid_mvp.json`, `hybrid_mvp_game.tscn`, `hybrid_mvp_controller.gd`, sim: `godot_project/sim/hybrid/*` |
| **Player actions** | Economy building/roads + **delegated** military commands (`DelegatedAttackCommand`, stances, guard/patrol/repair priority) — **no direct unit micro** |
| **End state** | Config-driven objective (`config/hybrid_mvp_objective.json`) — build supplied barracks, produce rifle squad, optionally destroy enemy hub — **reachable by `TutorialHybridBot`** |
| **UI** | Desktop hybrid MVP + tutorial; hybrid observer demo |
| **Bots** | `TutorialHybridBot` (scripted plan), `HybridSpectatorBotA/B` (2-bot 3D spectator), `ModularNeuralHybridBot` (macro + neural/heuristic) |
| **Tests/smokes** | `test_HYB_SMOKE_001`–`009`, `run_tutorial_hybrid_bot_smoke.gd`, viewer hybrid MVP tests |
| **Status** | **Sim complete; 2P playable; competitive AI weak** |

### Hybrid 4-player FFA

| Field | Detail |
|-------|--------|
| **Paths** | `scenarios/hybrid/hybrid_4p_training_ffa.json`, `hybrid_4p_human_vs_neural_ffa.json`, `hybrid_ffa_objective.gd`, `quad_bot_runner.gd` |
| **Player/bot actions** | Same hybrid command surface; per-player fog in 4P config; elimination when Command Hub destroyed |
| **End state** | `HybridFfaObjective` — sole remaining player — **rule implemented**; **not observed in recent neural evals** (200-tick cap, no winner) |
| **UI** | 2D spectator (`hybrid_bot_spectator_2d.tscn`); mobile FFA shell; no dedicated human 4P desktop MVP |
| **Bots** | `ModularNeuralHybridBot` ×4, `PassiveBot`, heuristic teacher fallback |
| **Tests/smokes** | `test_neural_hybrid_020`–`022`, `048`, `run_neural_hybrid_4p_smoke.gd`, `run_trained_neural_eval.gd` |
| **Status** | **Partial** — loads, ticks, telemetry; **victory loop unproven at scale** |

### Bot-vs-bot / training modes

| Mode | Paths | Status |
|------|-------|--------|
| Economy dual-bot duel | `DualBotRunner`, `expansion_duel_2p.json`, `run_economy_bvb_*.gd` | **Runs**; `ECON_BOT_002` metrics test **fails** (buildings gate) |
| Hybrid 2-bot 3D spectator | `hybrid_3d_bot_spectator.tscn`, `run_hybrid_bot_vs_bot_3d_smoke.gd` | **Works** — authoritative snapshots, combat flow |
| Economy spectator bot demo | `economy_spectator_bot_demo.tscn` | **Debug/demo** |
| Rollout observer | `rollout_observer_demo.tscn`, RUN_06 training transport | **Trial-game neural scaffold** |

### Spectator modes

| Mode | Paths | Status |
|------|-------|--------|
| Economy spectator | `economy_spectator_bot_demo.tscn` | Partial — bot intent, logistics overlays |
| Military spectator | `military_spectator_demo.tscn` | Partial — unit selection, combat events |
| Hybrid observer | `hybrid_observer_demo.tscn` | Partial — merged economy+military layers |
| Hybrid 3D 2-bot spectator | `hybrid_3d_bot_spectator.tscn` | **Most complete spectator** — MRP stack |
| Hybrid 2D 4P spectator | `hybrid_bot_spectator_2d.tscn`, `hybrid_bot_spectator_controller.gd` | **Partial** — 4P trained-neural HUD works with DISPLAY; headless PNG capture unreliable |

### Mobile-shaped modes

| Mode | Paths | Status |
|------|-------|--------|
| Mobile layout profiles | `client/ui/mobile_layout_profiles.gd` | **Scaffold** — 8 profiles defined |
| Mobile hex view QA | `run_mobile_hex_view_qa.gd`, `test_mobile_hex_*` | **Partial** — 2 tests in harness; 18-test matrix not complete |
| Hybrid mobile FFA | `hybrid_mobile_ffa_menu.tscn`, `hybrid_mobile_ffa_game.tscn`, `hybrid_mobile_ffa_controller.gd` | **Scaffold** — launches, ticks, 3 neural opponents; real-device QA not evidenced |

### Debug / viewer demos (non-product)

`economy_viewer_demo.tscn`, `military_viewer_demo.tscn`, `economy_human_proof_demo.tscn` — rule visualization and QA, not shipped game modes.

---

## 3. Current implemented systems

| System | Status | Key files | What works | What is missing or unclear |
|--------|--------|-----------|------------|----------------------------|
| Hex map | **Implemented** | `sim/hex/*`, map in scenarios | Axial coords, neighbours, seeded generation, determinism tests | Map sizes/content for mobile performance not finalized |
| Terrain | **Implemented** | `economy_map_rules.gd`, terrain in map JSON | Grass, forest, water, mountain, desert; buildability rules | Some terrain sprites missing in asset registry (warnings: `hills`, `fertile`) |
| Roads | **Implemented** | `economy_road_system.gd`, `PlaceRoadCommand` | Adjacent hex roads, slope blocking, route diagnostics | Hybrid bot road planning is macro-compiled, not full player-grade path UX |
| Carriers/workers | **Implemented** | `economy_logistics_system.gd`, worker policies | Carriers per delivery job, worker types, tool requirements | Tool categories simplified to generic `tools` in Run 02 |
| Resources/wares | **Implemented** | `sim/data/config/wares.json`, catalogue `05_` | Timber→planks chain, mines, food support, iron, tools | **Bricks chain** config-present but not fully tested; future wares documented only |
| Buildings | **Implemented** | `buildings.json`, construction system | Footprints, ports, upgrades (partial) | Full catalogue not all proven in tests |
| Construction | **Implemented** | `economy_construction_system.gd`, `hybrid_construction_system.gd` | Sites, material delivery, builder_work abstract progress | Human UX for site placement polish incomplete |
| Production chains | **Implemented** | `economy_production_system.gd` | Recipes, missing-input jobs, priorities | Long-chain balance untuned for competitive play |
| Storage | **Implemented** | storehouse/HQ stores | Full-store blocking | Overflow strategy across multiple hubs in 4P unclear |
| Logistics bottlenecks | **Implemented** | diagnostics, blocked routes | Block reasons in snapshots/diagnostics | Bot reward metrics for bottlenecks not wired for hybrid RL |
| Military buildings | **Implemented** | `military_*`, hybrid config | Command hub, barracks, guard post, fabricator, etc. | Hybrid roster smaller than design catalogue ambition |
| Units | **Implemented** | military unit defs | Rifle squad, scout, etc.; squad model in military | Hybrid production limited to tested subset |
| Movement | **Implemented (military sim)** | `military_movement_system.gd` | Pathing, orders | **Human direct move rejected in hybrid** (`HYB-SMOKE-007`) |
| Combat | **Implemented** | combat systems, delegated attack | Damage, targeting, hub destruction events | Attack-roads/supply-line targeting **not evidenced** |
| Damage/destruction | **Implemented** | building/unit HP, destroyed state | Hub destruction triggers victory in military/hybrid | Repair loop exists (`hybrid_repair_system.gd`) but under-exercised in 4P |
| Repair | **Partial** | `SetRepairPriorityCommand`, hybrid repair | Priority command exists, HYB-SMOKE-006 | Player-facing repair UX thin |
| Fog/intel | **Partial** | fog in military/hybrid state | Enabled in 4P scenario; per-bot observation snapshots | Intel merge rules for hybrid observer vs bot obs **unclear in docs** |
| Ownership | **Implemented** | player_id on entities | Multi-player init tests | 4P starting fairness/balance not tuned |
| Victory/elimination | **Partial** | `HybridObjective`, `HybridFfaObjective`, military victory | 2P MVP objective + FFA elimination rule coded | **4P FFA victory not demonstrated** in recent eval artefacts |
| Commands/validation | **Implemented** | `CommandValidator`, variant registries | Invalid commands rejected; hybrid rejects direct micro | Macro compiler may emit commands that fail validation silently (bot noop) |
| Snapshots/export | **Implemented** | `SnapshotExporter`, `HybridSnapshotExporter` | JSON snapshots for UI and bots | Observation vector size/schema still evolving for hybrid AI |
| Metrics | **Partial** | economy diagnostics, bot summaries | Stall ticks, progress scores, bot duel metrics | Hybrid reward/trace metrics for RL mostly stubbed |
| Heuristic bots | **Implemented** | economy/military/hybrid/tutorial bots | Tutorial bots complete MVPs; economy heuristic targets workshop | **No strong hybrid FFA bot**; dual-bot expansion test failing |
| Neural (trial) | **Partial** | `python_tools/training/`, RUN_06 | BC training, policy eval smoke, Godot headless env | Trial-game policies; not hybrid macro policies |
| Neural (hybrid modular) | **Partial — real checkpoints, weak policy** | `train_modular.py`, `ModularNeuralHybridBot`, `HybridNeuralInference` | M1–M7 imitation, Godot JSON load, `trained_neural` telemetry | ~49% arbiter accuracy; no competitive wins; teacher is heuristic |
| Spectator | **Partial** | 3D 2-bot strongest; 2D 4P newer | Snapshot-driven rendering, intent HUD | Headless screenshot pipeline; mobile spectator readability gates incomplete |
| Mobile UI | **Scaffolded** | `mobile_layout_profiles.gd`, mobile FFA scenes | SubViewport tests, menu entry tests | Pinch-zoom, real-device QA, tutorial on mobile not done |

---

## 4. Current rules summary

*This section describes what the codebase currently supports, not a design target.*

### Match start

- Scenarios JSON define variant, seed, map size, players, spawns, starting wares/workers/roads (`godot_project/sim/data/scenarios/`).
- Hybrid MVP: 2 players, HQ-area spawns, starting planks/stone/tools/rations/iron, pre-built road segment, optional power generator.
- Hybrid 4P FFA: four corner spawns on 22×22 map, fog enabled, shared battle center metadata.

### Starting ownership

- Each player gets HQ/command hub footprint at spawn (via map setup helpers e.g. `HybridMapSetup.prepare_playable_map`).
- Starting military units may be placed in scenario (e.g. enemy scout in hybrid MVP).
- Workers assigned from HQ worker pool; carriers implicit in logistics model.

### Resources

- Economy wares per `wares.json` / design catalogue (`timber`, `planks`, `stone`, `tools`, `rations`, `meals`, ores, etc.).
- Hybrid: **no credits** (DDR-031). Military costs are wares + delivery.
- Trial military standalone still uses credits when not in hybrid variant.

### Resource movement

- Roads connect building **ports** (road nodes).
- **Delivery jobs** assign carriers to move wares along road network (`economy_logistics_system.gd`).
- Construction sites consume delivered materials + abstract `builder_work`.
- Hybrid military production pulls wares from HQ stores (`HybridProductionSystem`).

### Building placement and construction

- `PlaceBuildingPlanCommand` with footprint validation (`EconomyMapRules`, hybrid construction).
- Hybrid military buildings use same construction-site flow as economy (HYB-LOGCON-*).
- Build radius limits apply (`build_radius` in scenario).

### Production

- Economy buildings run recipes when inputs/workers/tools available; jobs created on missing input.
- Hybrid military queues consume ware costs upfront/at progress per `HybridProductionSystem`.
- Production stalls emit `input_missing` style events (design intent; see HYB-NOSIDE-*).

### Military unit production

- Queue at barracks/fabricator via commands (delegated/bot macro `MACRO_QUEUE_RIFLE`).
- Requires ware delivery to building — **not** sidebar instant spawn.

### Unit movement and combat

- Military sim handles movement/combat internally.
- **Hybrid human players** cannot issue direct move/attack commands (rejected — HYB-SMOKE-007).
- Delegated attack/posture commands route through `hybrid_order_system.gd`.
- Bots may set stances (`MACRO_DEFEND_BASE`, `MACRO_ATTACK_POSTURE`, etc.) via macro compiler.

### Damage and destruction

- Buildings/units have HP; destroyed state exists.
- Command Hub destruction triggers player defeat in military/hybrid contexts.

### Elimination and winner

- **2P hybrid MVP:** objective config may require military building + unit + optional hub kill (`HybridObjective`).
- **4P FFA:** player eliminated when Command Hub destroyed; winner when one player remains (`HybridFfaObjective`).
- **Recent 4P neural evals:** all ended at **tick cap without winner** (`no_winner_tick_cap.txt` in eval folders).

### Bot interaction

- All bots implement `Bot.decide(tick) -> SimCommand` through same validator as humans.
- Modular hybrid bot: observe snapshot → strategic intent → (neural or heuristic) macro choice → compile → validate.
- Tutorial bots use fixed build/road/attack scripts tied to scenario geometry.

**Unclear / not fully specified in code:**

- Stalemate rules beyond stall tick thresholds.
- Resignation / draw conditions in 4P FFA.
- Whether economy victory conditions exist in hybrid or only military elimination.
- Exact fog visibility rules per bot in 4P (partial intel vs omniscient teacher traces).

---

## 5. Rules gaps and design questions

### Economy / logistics

- Which wares/buildings are **MVP-final** vs placeholder? Catalogue lists more than tests prove (e.g. **bricks**, deep mines).
- Are **tool categories** staying generic `tools` or splitting into cutting/digging/mining tools?
- Must **all** deliveries use roads, or are there exceptions for adjacent/port-less flows?
- What is the intended **4P starting economy** — symmetric rush vs boom? Current spawns untuned.
- How should **storage overflow** and cross-base logistics work when players have multiple storehouses?
- What **stall/progress score** should trigger AI resignation or match end in long FFA?

### Military / RTS

- Final **unit roster** for hybrid — rifle squad only or broader?
- **Squads vs individuals** — military sim uses squad types; is that the long-term model?
- How should **delegated attack** choose targets (nearest hub, weakest army, economic targets)?
- Can units **attack roads or carriers** to cut supply? Not evidenced in current commands.
- **Repair** priority vs rebuild — when should bots/humans repair vs replace?
- **Power/radar/tech** (HYB-SMOKE-003) — how much belongs in next gameplay PRD vs later expansion?

### Victory / match structure

- Is **Command Hub destruction** the only elimination condition for hybrid FFA?
- Can a player **recover** after losing barracks but keeping hub? (Likely yes — unclear design intent.)
- **Tick cap / stalemate** — what should happen when no elimination by tick N?
- Should hybrid support **non-elimination objectives** (economic victory, wonder, score)?
- **2P vs 4P** — same rules or different game modes?

### Bots / neural training

- Is the **16-macro action space** (`HybridAiActionSpace`) the long-term API, or a temporary compiler layer?
- Which **heuristic teacher** is canonical — modular experts or `TutorialHybridBot` script?
- What **reward signal** defines success for hybrid RL (hub kill, ware throughput, survival)?
- Should training use **per-player fog-limited** observations or full snapshots (teacher traces currently rich)?
- What promotion gates matter before calling hybrid AI "playable" (win rate vs scripted bot, 4P elimination rate)?

### Interface / spectator

- Minimum **debug HUD** for hybrid (intent, blocked reason, ware shortages) vs polished mobile UI?
- Must spectators show **per-player fog** or omniscient 4P view?
- Is **2D or 3D** the primary watchable format going forward?
- What can wait until after sim+bots are solid?

---

## 6. Feature gaps

### Must resolve before full hybrid gameplay

| Gap | Why it matters | Evidence | Suggested next decision |
|-----|----------------|----------|-------------------------|
| 4P FFA victory loop unproven | Without eliminations, hybrid FFA is a sandbox | `docs/audit/neural_hybrid_ai/eval/4_neural_ffa/no_winner_tick_cap.txt` | Tune scenario + bot aggression or extend ticks until first documented elimination |
| No strong hybrid heuristic opponent | Neural teachers weak; humans lack challenge | Only `TutorialHybridBot` wins MVP; modular bot imitation ~49% | Invest in `TutorialHybridBot`-class scripted 4P bot before more neural work |
| Macro compiler → validation failures silent | Bots noop without clear failure mode | `ModularNeuralHybridBot` telemetry `validation_result` | Decide: expand macros, improve compiler, or penalize in training |
| 4P economy balance untuned | Corner spawns may not produce meaningful conflict | `hybrid_4p_training_ffa.json` | Playtest or headless oracle for time-to-first-contact |
| Stalemate/tick-cap policy undefined | Matches end arbitrarily | Eval cap 200; scenario max 50000 | Define official match length and tie-break |

### Useful before neural training

| Gap | Why it matters | Evidence | Suggested next decision |
|-----|----------------|----------|-------------------------|
| Reward/metrics for hybrid RL | Imitation alone plateaus | `reward.py` stub; teacher rows use flat 0.01 reward | Define sparse reward on hub damage, ware milestones, elimination |
| Teacher quality | Bad teacher → bad neural policy | M5 arbiter accuracy 0.492 | Require stronger heuristic before M6/M7 self-play |
| Fog-aligned observations | Training may cheat with full snapshot fields | Teacher collection uses full exporter | Align obs with bot legal/intel contract |
| Win/loss eval scenarios | RUN_06 eval exists for trial games only | `evaluate_4p.py` exists; no win gate | Add hybrid eval scenarios with known winnable scripted baseline |

### Useful before mobile polish

| Gap | Why it matters | Evidence | Suggested next decision |
|-----|----------------|----------|-------------------------|
| Mobile tutorial navigation | Prior QA: stranded after step 1 | `RUN_MANUAL_PLAYABILITY_QA_CURRENT_STATE.md` | Fix tutorial Next/Back before mobile layout pass |
| Real device / portrait QA | Only SubViewport tests | `RUN_MOBILE_HEX_VIEW_PLAYABILITY_CURRENT_STATE.md` | Defer device QA until hybrid loop fun |
| Touch build/road UX on hybrid | Hybrid MVP desktop-first | `hybrid_mvp_game.gd` | Decide if mobile hybrid is FFA-only or full builder |
| Asset gaps (terrain sprites) | Missing `hills`, `fertile` warnings | Godot capture logs | Fill placeholders or simplify terrain set |

### Optional / future

| Gap | Why it matters | Evidence | Suggested next decision |
|-----|----------------|----------|-------------------------|
| Bricks production chain | Catalogue/config ahead of tests | `05_units_resources_buildings_catalogue.md` | Defer until core hybrid loop stable |
| WiFi multiplayer | RUN_12 | `09_wifi_multiplayer_design.md` | Defer |
| Expanded content/progression | RUN_11 | `08_expansion_design.md` | Defer |
| 3D spectator for 4P | MRP was 2-bot | `hybrid_3d_bot_spectator.tscn` | Optional after 2D legibility |

---

## 7. Bot and AI state

### Heuristic bots (inventory)

| Bot | File | Can play | Wins by rules? | Notes |
|-----|------|----------|----------------|-------|
| `TutorialEconomyBot` | `sim/bot/` (tutorial) | Settler MVP | Objective complete | Test-proven |
| `HeuristicEconomyBot` | `heuristic_economy_bot.gd` | Economy chain | Workshop completion | Used in smokes |
| `EconomyExpansionBot` | `economy_expansion_bot.gd` | Dual duel | **Unclear** — duel metrics fail | Related to `ECON_BOT_002` failure |
| `HeuristicMilitaryBot` | `heuristic_military_bot.gd` | Skirmish | Military victory | Trial variant |
| `TutorialHybridBot` | `hybrid/bot/tutorial_hybrid_bot.gd` | Hybrid MVP | **Yes** — `run_tutorial_hybrid_bot_smoke.gd` | Scripted plan + delegated attack |
| `HybridSpectatorBotA/B` | `hybrid_spectator_bot_*.gd` | 2-bot spectator | Demo flow | Not FFA |
| `PassiveBot` | `passive_bot.gd` | Noop | Never | Eval baseline |
| `ModularNeuralHybridBot` | `modular_neural_hybrid_bot.gd` | 4P ticks | **No evidenced wins** | See neural section |

### Modular / neural hybrid bots — evidence-based status

**Claim:** Real trained checkpoints exist and Godot loads them.  
**Evidence:**

- `python_tools/training/hybrid_ai/checkpoints/latest.pt` (committed via force-add)
- `godot_project/sim/hybrid/ai/checkpoints/latest_modular_policy.json`
- `docs/audit/neural_hybrid_ai/training/latest_checkpoint_manifest.json`
- Tests: `test_neural_hybrid_050`–`053` pass when checkpoints present

**Claim:** Inference uses trained weights, not heuristic-only.  
**Evidence:**

- 4P FFA eval: 800/800 decisions `inference_mode=trained_neural`, 0 heuristic fallback (`player_metrics.csv`)
- Telemetry fields: `checkpoint_loaded`, `heuristic_fallback`, `checkpoint_meta`

**Claim:** Neural bots are **competitive**.  
**Evidence:** **NOT SUPPORTED.**

- M5 arbiter imitation accuracy **~49%**
- All eval runs: `no_winner_tick_cap.txt` at 200 ticks
- Policy trained from **heuristic modular experts**, not from winning play

**Fallback behaviour:**

- If checkpoint missing or inference fails → `heuristic_fallback=true`, uses `ExpertCandidateProposer` + `ModularArbiter`
- Teacher mode: `use_heuristic_teacher=true` for trace collection

### Trial-game neural (RUN_06) — separate from hybrid modular

- `python_tools/training/train_loop.py`, BC, policy eval smoke — targets **economy/military trial** flat policies
- Documented closure: 160/160 tests at RUN_06 time; hybrid macro policy is a **different stack** (`python_tools/training/hybrid_ai/`)

### What is missing for credible hybrid AI

- Heuristic that **wins 4P FFA** under documented rules
- Reward-shaped training beyond imitation
- Eval gates tied to **elimination rate**, not just "ticks without crash"
- Human vs neural mobile FFA **not verified** as fun/fair (`RUN_HUMAN_VS_NEURAL_MOBILE_4P_FFA_RESULTS.md` exists but product-ready claim would be premature)

---

## 8. Test and verification state

### Scale

- **351 tests** registered in `godot_project/sim/tools/run_tests.gd` (up from 160 at RUN_06 closure).
- Full suite runtime **>10 minutes** — timed out at 600s during this audit before completion (early tests passing; no final summary captured).

### Test families and what they prove

| Family | Count (approx) | Proves | Does not prove |
|--------|----------------|--------|----------------|
| Kernel / hex / determinism | ~15 | Sim foundation, replay hash, no float in hashed state | Fun gameplay |
| Economy `test_econ_*` | ~100+ | Logistics, recipes, roads, workers, scenarios | Competitive balance |
| Military `test_mil_*` | ~40+ | Combat, production, victory | Hybrid delegation alone |
| Hybrid `test_HYB_SMOKE_*` | 9 | Ware-fed build/produce, delegated attack, reject direct move, repair, replay | 4P FFA, neural quality |
| Bot `test_econ_bot_*`, tutorial bots | ~15 | Command API, tutorial completion, duel symmetry | Dual-bot expansion at scale |
| Human proof | 4 | Oracle parity with bot runner | UI playability |
| Viewer / mobile / spectator | ~30+ | Scene loads, SubViewport layout, menu entries | Real device UX |
| Neural hybrid | 13 | Obs shape, 4P tick, checkpoint load, trained inference mode | Winning play |
| Settler MVP / manual QA | several | Tutorial legibility gates | Complete mobile QA |

### Smoke runners (selected)

| Runner | Proves |
|--------|--------|
| `run_smoke.gd` | Variant kernel tick (economy/military/hybrid) |
| `run_economy_smoke.gd` | Economy milestone chains |
| `run_military_smoke.gd` | Military milestones |
| `run_bot_smoke.gd` | Bot command integration |
| `run_tutorial_hybrid_bot_smoke.gd` | Tutorial hybrid bot completes objective |
| `run_neural_hybrid_4p_smoke.gd` | 4P scenario ticks |
| `run_trained_neural_eval.gd` | Eval artefacts + trained telemetry |
| `run_training_smoke.gd` / `run_policy_eval_smoke.gd` | RUN_06 trial training |
| `verify_failure_modes.gd` | Negative tests still fail correctly |

### Known failures / flaky gates

| Test / gate | Status | Notes |
|-------------|--------|-------|
| `test_ECON_BOT_002_dual_bot_expansion_metrics` | **FAIL (documented)** | p1/p2 bot-built buildings below minimum (≥5); pre-existing on multiple audits |
| `REP-SMOKE-001` | **FAIL (historical)** | Replay import hash mismatch noted in RUN_02.8 closure |
| Full `run_tests.gd` in one 600s window | **Incomplete** | Suite grew to 351 tests; allow ≥15–20 min |
| Kernel + failure-mode integrity | **Passed** in recent neural-hybrid work per audits | Re-verify before next major run |

### What passing scaffold tests do **not** prove

- Neural hybrid PRD "complete" — only infrastructure + imitation policy
- Mobile playability PRD complete — mostly layout + scene load tests
- 4P hybrid esports-ready — ticks ≠ wins
- Economic dual-bot "expansion proof" — `ECON_BOT_002` still red

---

## 9. Recommended next decision points

### Decision 1: What is the minimum complete hybrid game loop?

**Options:**

- **A.** 2P hybrid MVP (current) — build economy, feed barracks, delegated attack, destroy hub — **proven** with tutorial bot.
- **B.** 4P FFA elimination — last hub standing — **rules exist**, **play loop not proven**.
- **C.** Human mobile FFA vs AI — product slice on top of B.

**Recommendation:** **B before C or more neural work.** The repo has strong 2P sim proof but 4P FFA is the stated hybrid AI target and has no documented winner.

---

### Decision 2: What bot should be "good enough" before neural training continues?

**Options:**

- **A.** Extend `TutorialHybridBot` to 4P with known win script.
- **B.** Strengthen modular heuristic experts + arbiter until they beat passive/weak opponents reliably.
- **C.** Continue imitation from current weak heuristics and hope RL fixes it.

**Recommendation:** **A or B.** Current neural policy imitates ~49% arbiter accuracy teachers; RL without a winning teacher or reward is low yield.

---

### Decision 3: Action surface for hybrid AI — macro vs legal commands?

**Options:**

- **A.** Keep 16 macro actions + compiler (current `HybridAiActionSpace`).
- **B.** Expand macros for finer economy/military (placement targets, explicit build sites).
- **C.** Flatten to legal primitive commands like RUN_06 trial policies.

**Recommendation:** **A short-term, B if compiler validation failures block wins.** Macros keep command validation centralized but hide placement intelligence in compiler heuristics.

---

### Decision 4: Victory and match length for hybrid FFA?

**Options:**

- **A.** Hub elimination only, no tick cap (may run forever).
- **B.** Tick cap + score/stalemate tie-break.
- **C.** Shorter tuned map/aggression so hub kills happen reliably within N ticks.

**Recommendation:** **C for training/eval**, with **B** as safety net. Current 200-tick eval cap produces only `no_winner_tick_cap.txt`.

---

### Decision 5: What is in scope for the next PRD vs deferred?

**Options:**

- **A.** Simulation + bots + 4P victory loop first; defer mobile polish and advanced neural.
- **B.** Neural training PRD continuation (M8/M9 self-play, more data).
- **C.** Mobile product PRD (touch UX, FFA menu, device QA).

**Recommendation:** **A.** Evidence shows UI/spectator/mobile scaffolds ahead of a proven 4P competitive loop.

---

### Decision 6: Economy catalogue breadth?

**Options:**

- **A.** Freeze hybrid roster to tested buildings/units (HQ, wood chain, quarry, barracks, rifle squad).
- **B.** Expand to bricks/deep mines/full catalogue before hybrid AI.

**Recommendation:** **A** until 4P loop wins once end-to-end.

---

## 10. Suggested next PRD shape (outline only)

**Working title:** *Hybrid 4P Competitive Loop — Heuristic Bots, Victory, and Eval Gates*

**Likely sections:**

1. **Goal** — Documented 4P FFA match that ends in elimination (not tick cap) with scripted bot; human-optional.
2. **In scope** — Scenario tuning, stronger heuristic 4P bot, stalemate policy, hybrid eval harness with win/loss assertions, fix or formally waive `ECON_BOT_002` if unrelated.
3. **Explicitly out of scope** — Mobile polish, new ware chains, WiFi MP, advanced neural RL, art finalization.
4. **Acceptance** — Headless eval: ≥X% matches reach winner within T ticks with bot Y; human can spectate in 2D with intent HUD; tests fail if no elimination in regression scenario.
5. **Follow-on PRD (later)** — Neural curriculum v2 (reward-shaped, winning teacher), then mobile FFA productization.

**Defer:**

- "Neural Hybrid AI PRD complete" claims until elimination-rate gates pass.
- Mobile hex playability full 18-test matrix until desktop hybrid loop is fun/debuggable.

---

## Appendix: Key file index

| Area | Paths |
|------|-------|
| Design contracts | `docs/design/02_`, `03_`, `04_`, `05_`, `10_glossary.md` |
| Hybrid sim | `godot_project/sim/hybrid/` |
| Hybrid AI | `godot_project/sim/hybrid/ai/`, `bot/modular_neural_hybrid_bot.gd` |
| Python hybrid training | `python_tools/training/hybrid_ai/` |
| Trial Python training | `python_tools/training/` (RUN_06) |
| Scenarios | `godot_project/sim/data/scenarios/` |
| Tests harness | `godot_project/sim/tools/run_tests.gd` |
| Verification commands | `docs/verification/local_verification.md` |
| Neural audit artefacts | `docs/audit/neural_hybrid_ai/` |
| Stale hybrid audit | `docs/audit/RUN_HYBRID_MVP_CURRENT_STATE.md` (pre-implementation) |

---

*End of audit.*

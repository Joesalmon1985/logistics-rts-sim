# PRD — Hybrid God-Strategy MVP

**Status:** Systems / milestone **reference** — **product Definition of Done superseded** by [`docs/prd/FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`](FINAL_GOD_STRATEGY_PLAYABLE_PRD.md)  
**Do not claim product complete from this file.** Use the Final PRD for the long-run DoD, phases 0–8, neural/mobile gates, and screenshot acceptance.  
**Primary design source:** [`docs/design/HYBRID_GOD_STRATEGY_DESIGN_GOALS_FINAL.md`](../design/HYBRID_GOD_STRATEGY_DESIGN_GOALS_FINAL.md)  
**Current-state grounding:** [`docs/audit/CURRENT_STATE_OF_PLAY_HYBRID_GAME.md`](../audit/CURRENT_STATE_OF_PLAY_HYBRID_GAME.md)  
**Suggested branch prefix:** `feature/god-strategy-mvp-<milestone-slug>` (historical); final run uses `feature/final-god-strategy-*`

---

## Document control

| Field | Value |
|-------|-------|
| Product name (working) | WideRTS — Hybrid God-Strategy |
| Target platform (MVP) | Godot 4.5, headless-first; 2D oblique top-down client |
| Authoritative runtime | Godot/GDScript simulation core (not Python, not scene tree) |
| Supersedes (scope) | Prior neural-hybrid/mobile PRD product claims as *complete* — those remain useful infrastructure, not the new MVP definition of done |
| Does not supersede | RUN_01 kernel, RUN_02 economy, RUN_03 military, RUN_07 hybrid integration contracts unless explicitly amended in a run doc |

---

## 1. Product summary

WideRTS’s next major MVP is a **4-player free-for-all god-strategy logistics RTS** in which each player guides a civilisation through **four ages**, shapes society through **infrastructure and cultural drafting**, commands war **indirectly**, and influences a population of **evolving worker agents** while managing **emergent pollution and collapse** in the modern/futuristic ages.

This PRD translates the design goals document into a **staged, testable implementation plan**. It is ambitious by design but **forbids a single undifferentiated “build everything” pass**. Each milestone must produce deterministic headless proofs before the next layer (culture, worker society, god AI, ecology) is allowed to depend on it.

**Milestone A is the hard gate.** Later milestones (culture, worker needs, societal selection, modular god AI, pollution/collapse) must not depend on sim layers they assume until Milestone A proves a **4P match can resolve cleanly** through domination or scored tick-cap/stalemate, with **at least one scripted domination regression** (fixed-seed bot vs passive opponents) and **working logistics warfare** (road/carrier/storehouse targeting affects deliveries). Milestones B–E establish ages, drafting, worker needs, and selection; **learned god-AI promotion** waits until those gates pass **and** Milestone F exposes inspectable heuristic modules—not before.

**What success looks like for this MVP (target, not day one):**

- A 4P FFA match can **start, progress through four ages, and end** under documented rules (domination, Age 3 VP, Age 4 restoration/survival/collapse).
- Logistics remain **physical** (wares move on roads via carriers).
- Player control remains **delegated god-level** (no unit/worker micro).
- Workers exhibit **needs + adaptive behaviour selection** (deterministic first, neural where proven).
- God AIs use a **modular network-of-networks** pattern with inspectable proposals.
- Pollution/collapse in Ages 3–4 are **emergent and observable**, not blunt warning pop-ups.
- Headless simulation, bots, tests, and training hooks remain **first-class**.

---

## 2. Core fantasy

The player is a **god** guiding a civilisation—not a foreman micromanaging every carrier, nor a general selecting individual squads.

People build, carry, farm, forge, rest, fight, pollute, restore, flourish, and collapse through **systems the god shapes**:

- roads and logistics networks;
- buildings and production priorities;
- cultural cards (institutions, technologies, beliefs);
- civic/divine edicts and incentives;
- military stances and delegated attack targets;
- welfare, leisure, safety, and restoration investment.

The civilisation should feel **alive**: worker behaviours that fit a peaceful agricultural society differ from those in a harsh industrial war economy or a collapsing polluted world. The god creates conditions; **societal selection** determines which worker behaviours spread.

---

## 3. Design pillars

| Pillar | Requirement |
|--------|-------------|
| **Delegated god control** | High-level intent only; simulation resolves detail. |
| **Physical logistics** | No ware teleportation; roads, jobs, carriers, storage matter; logistics warfare is valid. |
| **Culture as strategic technology** | Draft cards unlock/modify buildings, units, modifiers, VP, worker pressure, pollution, mitigation—not flavour text alone. |
| **Workers as society agents** | Needs, traits, policies, selection pressure; not anonymous conveyor belts at MVP target. |
| **Modular god minds** | Domain modules + coordinator; not one monolithic policy. |
| **Emergent collapse** | Age 3 industrial power vs Age 4 consequences; visible harm through forests, farms, workers, disasters—not “this factory causes collapse” warnings. |
| **Deterministic authority** | Godot/GDScript sim core is authoritative; integer/fixed-point hashed state; seeded RNG; commands validated centrally. |
| **Headless proof** | Every major mechanic family has deterministic tests before UI polish depends on it. |

---

## 4. Current project reality

*Grounded in repo inspection, June 2026. Where docs conflict with code, **code + recent audits win**.*

### 4.1 What already exists (reuse, extend—do not rewrite)

| Area | Status | Key evidence |
|------|--------|--------------|
| Deterministic sim kernel | **Strong** | RUN_01 tests, replay hash, `CommandValidator` |
| Trial economy logistics | **Strong** | Roads, carriers, jobs, construction, recipes, diagnostics |
| Trial military combat | **Strong (standalone)** | Squads, combat, credits-based trial mode |
| Hybrid variant integration | **Partial–strong sim** | `godot_project/sim/hybrid/*`, HYB-SMOKE 001–009 |
| Ware-fed hybrid military | **Implemented** | `HybridProductionSystem`, construction sites |
| Delegated hybrid commands | **Implemented** | `DelegatedAttackCommand`, stances; direct micro rejected (HYB-SMOKE-007) |
| 2P Hybrid MVP UI | **Playable** | `hybrid_mvp_game.tscn`, tutorial bot completes objective |
| 4P scenario + quad runner | **Partial** | `hybrid_4p_training_ffa.json`, `QuadBotRunner` |
| Command Hub elimination (simple) | **Implemented** | `HybridFfaObjective` — sole remaining player |
| Macro god action compiler | **Scaffold** | `HybridAiActionSpace` (16 macros) |
| Modular god bot (heuristic + imitation NN) | **Scaffold** | `ModularNeuralHybridBot`, checkpoints exist; **not competitive** |
| Spectator / mobile shells | **Scaffold** | 2D 4P spectator, mobile FFA layout profiles |
| Test harness | **Large** | ~351 tests in `run_tests.gd` |

### 4.2 What does **not** exist yet (greenfield for this PRD)

| Area | Notes |
|------|-------|
| Four-age match structure | No age state machine in sim |
| Cultural drafting | No draft decks, hands, rotation, card effects |
| Victory points + Age 3 check | No VP ledger |
| Age 4 collapse/restoration endings | No world-damage model |
| Functional ware/building roles with age flavour | Concrete wares only (`timber`, `planks`, …) |
| Worker needs (hunger, tiredness, happiness, …) | Workers are job-assignment entities |
| Worker policy interface / societal selection | Not in sim |
| Pollution (local/global) | **No code references** |
| Natural disasters tied to pollution | Not implemented |
| God AI pre-analytic/analytic module pattern | Heuristic experts + single arbiter only |
| 4P proven competitive loop | Evals end at tick cap; no documented elimination |
| Attack roads/carriers as first-class hybrid targets | Design intent; **not evidenced** in current hybrid combat tests |

### 4.3 Known conflicts to resolve via staging

| Current state | Design target | Staged path |
|---------------|---------------|-------------|
| `HybridFfaObjective` = hub elimination only | Multi-endpoint victories (VP, restoration, collapse) | Milestone A keeps hub rule; Milestone B adds VP/age; Milestone G adds collapse |
| 16-macro compiler | Richer god command surface | Keep macros as compiler in A–F; expand only where eval proves need |
| `ModularNeuralHybridBot` imitation policy | Modular god minds with pre/analytic stages | Milestone F replaces heuristic scaffold; **do not claim AI success before A–E + strong teachers** |
| Concrete ware IDs | Functional roles + age display names | Introduce role indirection layer in B without breaking economy tests |
| 2P hybrid MVP as main UI | 4P FFA target | 2P remains tutorial/regression; 4P scenario is acceptance target |
| `ECON_BOT_002` failing | Strong economy bots needed for teachers | Track separately; god-strategy bots must not depend on flaky duel metrics |

### 4.4 Pre-existing test debt (not blockers for Milestone A start, but must not be ignored)

- `test_ECON_BOT_002_dual_bot_expansion_metrics` — **known fail** (buildings gate).
- Full `run_tests.gd` runtime **>10 minutes** (351 tests).
- Prior neural eval: **0/800** heuristic fallback but **no winners** at 200 ticks.

---

## 5. Target MVP scope

### 5.1 Target MVP (this PRD’s definition of done)

When all Milestones **A through G** are complete and Milestone **H** meets minimum polish gates:

1. **4-player FFA** symmetric boom start, fog enabled, `max_ticks: 50000`, stall detection per design goals.
2. **Four ages** in one match cycle (Prehistoric → Historic → Modern → Futuristic/Collapse).
3. **8 cultural cards drafted per player per age** on fixed intervals; cards affect gameplay systems.
4. **Functional economy** with age-flavoured presentation for core ware/building roles (§12–13).
5. **Worker needs** affecting throughput; **worker policy interface** with deterministic fallback.
6. **Societal selection** changing behaviour prevalence based on success + culture + environment.
7. **One core military squad type per age**; delegated attack; logistics warfare (roads, carriers, storehouses targetable).
8. **Command Hub elimination** + **Age 3 VP check** + **Age 4 collapse endings**.
9. **Pollution/collapse** emergent in Ages 3–4 (local + global, non-linear harm).
10. **Modular god AI** (heuristic → learned) with pre-analytic/analytic module slots and coordinator.
11. **Headless eval harness** proving match endings, not just ticks-without-crash.
12. **2D debug HUD + omniscient spectator** with overlays (economy, combat, worker society, pollution, AI intent).

### 5.2 First playable slice (Milestone A only — intentionally narrower)

Proves the **4P logistics RTS loop** without ages, culture, worker neural society, or pollution:

- 4P, roads, storage, carriers, one production chain, one military unit type, delegated attack, hub elimination, tick-cap/stalemate resolution, debug HUD subset.

This is **not** the final MVP—it is the **foundation gate**.

---

## 6. Explicit non-goals (MVP)

Do **not** implement in this PRD scope unless a later amendment explicitly adds them:

- Full 3D wizard embodiment or micro spell combat
- WiFi multiplayer / RUN_12 networking
- Full diplomacy, espionage, religion systems
- Deep industrial chains (electronics, oil, full vehicle roster)
- Aircraft, naval, large unit rosters beyond one unit/age
- Polished final art and commercial mobile store packaging
- One independent neural network per worker (unless Milestone E perf proofs justify)
- Obvious UI warnings (“this factory will cause collapse”)
- Repeated civilisation cycles within one match (post–Age 4 return to Prehistoric)
- Replacing Godot sim authority with Python runtime
- Claiming “strong AI” from imitation checkpoints without eval win-rate gates

**Milestone H (mobile polish)** is explicitly **last**—after the loop is fun and testable headlessly.

---

## 7. Four-age structure

### 7.1 Ages

| Age | ID | Theme | Primary new tensions |
|-----|-----|-------|----------------------|
| 1 | `prehistoric` | Survival, gathering, clan culture | Food, mobility, low extraction |
| 2 | `historic` | Settlement, law, professional war | Surplus, defence, organised logistics |
| 3 | `modern` | Industry, power, mechanised war | **Pollution vs power**, VP race |
| 4 | `futuristic_collapse` | Scarcity, restoration, catastrophe | **Consequences**, joint loss, restoration |

### 7.2 Per-age contents

Each age defines (data-driven):

- draft deck ID and card pool;
- age-flavour names for functional building/ware roles;
- allowed building/unit unlocks baseline;
- military unit role for the age;
- pollution coefficients (minimal A1–2; meaningful A3–4);
- transition hooks to next age.

### 7.3 Age progression trigger

After **8 draft picks per player** in the current age (see §8), the match advances to the next age **unless** an Age 3 victory check (§9) ends the match early.

### 7.4 Implementation mapping

| Component | New sim module (proposed) |
|-----------|---------------------------|
| Age state | `godot_project/sim/god_strategy/age_system.gd` |
| Age config | `sim/data/config/ages/*.json` |
| Snapshot fields | `current_age`, `age_tick`, `draft_round`, `age_index` |

**Milestone:** B introduces age machine; A uses static “Age 1 equivalent” content only.

---

## 8. Cultural drafting system

### 8.1 Rules

- **4 players**, each receives an **8-card hand** per age (deal pattern TBD: draft-from-shared-pool vs dealt hands—default: **pass-and-pick** per design goals §7.1).
- Every **`draft_interval_ticks`**, each player picks **1 card**; hands rotate; after **8 picks**, age draft completes.
- Cards are **strategic technology**: unlocks, modifiers, VP, worker pressure, pollution, mitigation.

### 8.2 Card schema (authoritative)

```text
card_id
age
name
flavour_text
card_type
vp_value
unlock_buildings[]
unlock_units[]
unlock_ware_roles[]          # optional
global_modifiers[]
worker_society_modifiers[]
pollution_delta_local
pollution_delta_global
collapse_mitigation_delta
prerequisites[]
tags[]
rarity_or_weight
```

### 8.3 MVP card count (scope control)

| Age | MVP target cards in pool | Notes |
|-----|--------------------------|-------|
| Prehistoric | 12–16 pool, 8 picks | Simple unlocks + 1–2 VP cards |
| Historic | 12–16 | Irrigation, walls, markets |
| Modern | 12–16 | Factories, fossil power—**pollution tension** |
| Futuristic/Collapse | 12–16 | Restoration, grid, deterrence |

Start with **~4–6 mechanically distinct** card templates reused with variations; expand after Milestone B passes.

### 8.4 Draft AI (later)

- Milestone B: humans/bots pick via scripted priority (VP > unlock > modifier).
- Milestone F: `CultureDraftPolicy` module.

---

## 9. Victory and match-end rules

### 9.1 Ending types (priority order when checking each tick)

1. **Shared collapse loss** (Age 4 only) — world damage ≥ irreversible threshold → no winner.
2. **Domination victory** — only one player with surviving Command Hub (+ no active hub site saving elimination—see §10).
3. **Age 3 civilisation victory** — at Age 3 end: if exactly one surviving player has **≥10 VP**, that player wins.
4. **Age 4 restoration victory** — collective restoration goals met; highest score wins lead civilisation (joint win allowed).
5. **Tick-cap survival score** — `max_ticks` reached; survival-adjusted score + tie-breakers (design goals §15.4).
6. **Stalemate flag** — stall rule triggered (design goals §15.2); may invoke accelerated scoring or draw policy (TBD in open questions).

### 9.2 Match timing defaults

```text
max_ticks: 50000
stall_threshold_ticks: 1000
fog_enabled: true
draft_interval_ticks: TBD (Milestone B — start 500)
worker_policy_refresh_ticks: TBD (Milestone E)
```

### 9.3 Gap vs current code

`HybridFfaObjective` implements **domination only**. Milestones B and G must extend `GodStrategyObjective` (proposed) without breaking HYB-SMOKE regression tests (keep 2P MVP on legacy objective path until migrated).

---

## 10. Command Hub elimination and recovery rules

### 10.1 Elimination

A player is **eliminated** when:

- they have **no surviving Command Hub** (age-flavoured HQ role); **and**
- they have **no active Command Hub construction site** that could restore a hub.

Elimination is **immediate** when conditions met (check each tick).

### 10.2 Recovery

- Losing barracks, mines, or production **does not** eliminate.
- Rebuilding hub via construction site **prevents** elimination while site is active and legal.
- Repair rules (§19) apply to damaged but not destroyed hubs.

### 10.3 4P start (design default)

Per design goals §10.5:

- 1 Command Hub, integrated or adjacent storehouse, 3 carriers, 1 builder, optional scout marker;
- materials for early roads + 2–3 buildings;
- **no starting offensive army**;
- combat requires military building + weapons + supplies chains.

**Conflict:** Current `hybrid_4p_training_ffa.json` uses richer starting stocks but does not enforce delayed-conflict tuning. Milestone A must **retune scenario** to this spec and prove endings.

---

## 11. Economy and logistics rules

### 11.1 Inherited foundation (already in repo)

Reuse RUN_02 / RUN_07 systems:

- hex map, terrain buildability, footprints, ports;
- `PlaceBuildingPlanCommand`, construction sites, `builder_work`;
- delivery jobs, carrier assignment, blocked-route diagnostics;
- hybrid: **no credits**, ware costs for military build/produce/repair.

### 11.2 MVP logistics laws (must remain true)

1. **No ware teleportation** between buildings except: internal building inventory moves, starting stock in hub, explicit test/debug spawn flags.
2. **All inter-building delivery** uses road/logistics network paths.
3. **Delivery jobs reserve** wares at source before dispatch.
4. **Storage overflow:** producer tries nearest valid storehouse on connected network; then next; then **stall** with diagnostic.
5. **Multiple bases** share storage only through **connected owned** logistics routes (alliances deferred—owned only in MVP).

### 11.3 Production and military supply

- Economy buildings: recipe inputs, worker/tool gating, priority queues.
- Military: queue at barracks; ware costs from HQ/network per `HybridProductionSystem` pattern.
- **Combat requires supply chains**—no free sidebar spawn.

### 11.4 Tools

- MVP recipes use generic **`tools`** ware.
- Data model reserves optional `tool_class` for future cards/buildings.

---

## 12. Age-equivalent ware roles

Internal simulation uses **stable functional roles**; presentation uses age-flavoured names from config.

| Functional role | Purpose |
|-----------------|---------|
| `raw_wood` | Forest extraction |
| `processed_wood` | Construction/manufacturing input |
| `stone_material` | Construction |
| `generic_tools` | Worker/build gating |
| `food_raw` | Farm/forage output |
| `food_processed` | Worker consumption |
| `ore_raw` | Smelting input |
| `metal_processed` | Advanced tools/weapons |
| `basic_weapons` | Military production |
| `military_supplies` | Unit sustainment |
| `power_or_control` | Age-scaled authority/energy gate |

**Age flavour table:** see design goals §10.1 (timber→reclaimed biomass, etc.).

**Implementation:** Milestone B adds `WareRoleRegistry` mapping role → concrete ware ID per age without duplicating recipe logic four times.

**Conflict:** Current `wares.json` uses concrete IDs only. Migration must keep backward compatibility for economy tests (alias prehistoric roles to existing `timber`, `planks`, … initially).

---

## 13. Age-equivalent building roles

Stable functional roles (design goals §11):

| Role | MVP priority |
|------|--------------|
| Command Hub | A — required |
| Storehouse | A |
| Road/logistics node | A |
| Wood extraction + processing | A |
| Quarry/stone | A |
| Farm + food processing | B |
| Mine + smelter + workshop | B–C |
| Barracks | A |
| Power/control | B (Modern+) |
| Defence turret | C optional |
| Leisure | C |
| Restoration | G |
| Polluting factory | G |

Presentation names per age (Sacred Hearth → Arcology Core) are **config/display**, not separate sim classes, until proven necessary.

---

## 14. Worker needs model

### 14.1 Needs vector (MVP target)

```text
hunger
tiredness
happiness
safety
wealth
social_belonging   # optional v1 — can stub 0
work_suitability
leisure_desire
danger_avoidance
cultural_alignment
```

### 14.2 Effects on simulation

Needs affect **throughput and availability**, not direct player commands:

- high hunger/tiredness → reduced work ticks, missed assignments;
- low happiness → efficiency penalty, higher leisure-seeking;
- low safety → flee behaviour, refuse dangerous jobs;
- pollution proximity → health/tiredness penalties (Milestone G).

### 14.3 Satisfaction sources

Food consumption, rest (HQ/leisure), leisure buildings, safe regions, cultural cards modifying norms.

### 14.4 Current state

Workers exist as **economy assignments** (`economy_logistics_system.gd`, worker policies). Milestone C adds **`WorkerNeedsState`** per worker or per worker cohort (performance decision—start with **cohort aggregates** if individual tracking is too heavy).

---

## 15. Worker neural-policy model

### 15.1 Design target

Workers choose **broad action categories** via:

```text
WorkerObservation → WorkerLegalActionMask → WorkerPolicy → WorkerAction → WorkerStepResult
```

Example actions: `WORK_ASSIGNED_JOB`, `SEEK_FOOD`, `REST`, `SEEK_LEISURE`, `SEEK_SAFETY`, `JOIN_MILITARY`, `IMITATE_SUCCESSFUL_WORKER`, `IDLE`.

### 15.2 Staged implementation (mandatory order)

1. **Deterministic job assignment** (existing — baseline).
2. **Needs-driven deterministic overrides** (Milestone C).
3. **Shared policy by archetype** (carrier/builder/specialist) — table or small net (Milestone D).
4. **Trait/genome weights** per worker/cohort (Milestone E).
5. **Optional neural inference** for archetype policies—Godot JSON weights as with god bots (Milestone E/F).
6. Individual networks **only** if perf proofs allow.

### 15.3 Authority rule

Worker policies **never** bypass command validation or mutate state directly. They select from legal worker actions resolved by sim systems.

---

## 16. Worker policy refresh / societal selection model

### 16.1 Refresh interval

Every **`worker_policy_refresh_ticks`** (default TBD, suggest 200–500 after tuning):

1. Score worker/cohort outcomes over window: survival, hunger satisfaction, rest, happiness, wealth, useful work, cultural fit, safety.
2. Rank policy variants / genome weights.
3. Propagate successful profiles to struggling workers (copy/mutate/blend).
4. Reduce prevalence of failing profiles.

### 16.2 Selection pressure inputs

Player-created conditions:

- food availability, leisure buildings, safety, wages/wealth proxies;
- cultural cards (`worker_society_modifiers`);
- pollution and disaster harm;
- military mobilisation demands.

**Design rule:** High productivity alone is **not** sufficient fitness if culture does not reward exploitation at cost of welfare (configurable per card/civilisation).

### 16.3 Telemetry

Snapshots must expose:

- behaviour distribution histogram;
- top behaviour traits per player;
- selection events (for debug HUD and training).

---

## 17. God AI modular network-of-networks architecture

### 17.1 Modules (MVP)

```text
EconomyPolicy
LogisticsPolicy
MilitaryPolicy
CultureDraftPolicy      # Milestone F
WorkerSocietyPolicy     # Milestone F (incentives/edicts, not per-worker training)
CollapseResponsePolicy  # Milestone G
CoordinatorPolicy / Arbiter
```

Optional later: Diplomacy, Espionage, Wonder, Religion.

### 17.2 Pipeline (authoritative)

```text
PlayerStateSummary + fog-limited observations
  → Domain pre-analytic stage (filter/weight/compress)
  → Domain analytic stage (0–3 proposals each)
  → Coordinator selects/compiles proposals
  → Legal god command(s)
  → CommandValidator
  → Simulation
```

### 17.3 Relation to existing code

**Reuse:**

- `HybridAiActionSpace` as **command compiler** (expand macros as needed).
- `HybridAiObservationExporter`, `HybridCommandAvailability`.
- `ModularNeuralHybridBot` telemetry patterns.

**Replace/evolve:**

- `ExpertCandidateProposer` / `ModularArbiter` heuristics → module slots with identical proposal schema.
- Imitation-only training → stage-gated with **win-rate eval** before promotion.

### 17.4 Proposal schema (must match spectator HUD)

Each proposal includes: `module_id`, `pre_analytic_summary`, `action_macro_or_command`, `score`, `legal`, `plain_english_reason`.

---

## 18. Pre-analytic and analytic network pattern for AI modules

### 18.1 Pre-analytic stage

**Input:** broad domain snapshot (may be high-dimensional).  
**Output:** weighted feature vector + human-readable bottleneck summary (for HUD).

Examples:

- Logistics: blocked jobs, idle carriers, storehouse fullness, road disconnects.
- Military: threatened hubs, idle squads, delegated attack status, enemy logistics targets.
- Economy: ware shortages, stalled construction, production queue gaps.

**Milestone F:** interface + heuristic implementation first; learned pre-analytic net only after traces exist.

### 18.2 Analytic stage

**Input:** pre-analytic output.  
**Output:** **0–3** ranked proposals (not always 3—allows “no action”).

### 18.3 Coordinator

Combines module proposals; resolves conflicts; emits **one primary god action per decision tick** (or bounded batch if design later allows—MVP: **one**).

### 18.4 Training discipline

- Teacher traces may use full snapshots.
- Eval and shipped bots use **fog-limited** observations unless cheat flag explicitly enabled in debug.

---

## 19. Military and delegated combat rules

### 19.1 Squad model

- **Squads** are player-facing and AI-facing units—no individual micro.
- **One core unit role per age** (Warband → Guard Company → Rifle Squad → Survival Troopers).

### 19.2 Forbidden player commands (hybrid / god-strategy)

```text
SelectUnitCommand, MoveUnitCommand, AttackUnitCommand,
BoxSelectCommand, GroupControlCommand
```

Already rejected in hybrid—**preserve** (HYB-SMOKE-007).

### 19.3 Allowed military controls

Building stance, production priority, garrison, patrol, delegated attack target, defence priority, repair/rebuild priority.

### 19.4 Delegated attack flow

1. Player/bot selects visible hostile **building or infrastructure** target.
2. Sim assigns eligible squads by stance and distance.
3. Path, fight blockers, attack, return to stance/home.

### 19.5 Valid targets (MVP requirement Milestone A)

Command Hubs, military buildings, production buildings, **roads**, **carriers**, storehouses, construction sites, power/control, defences.

**Gap:** Current hybrid tests emphasise hub combat—not logistics targeting. Milestone A must add **HYB-GOD-001** tests for road/carrier/storehouse damage affecting delivery jobs.

### 19.6 Stances

- **Patrol/defensive:** protect territory, engage military threats; no deep civilian raiding by default.
- **Aggressive:** raid logistics, roads, carriers, storage, construction.

---

## 20. Pollution, local degradation, global pollution and emergent collapse

### 20.1 Design principles

- **No blunt warnings** in normal UI.
- Harm visible via: forest health visuals, yield decline, worker health/happiness, disaster frequency, supply instability.
- Age 3: prisoner’s dilemma—industry/power/weapons strong but polluting.
- Age 4: consequences dominate.

### 20.2 Local pollution

Sources: factories, fossil power, heavy industry, over-extraction, toxic waste, nuclear/catastrophic weapons (Age 4).

Effects on nearby hexes/buildings:

- forest biomass yield;
- farm reliability;
- worker health/tiredness/happiness;
- local disaster vulnerability.

### 20.3 Global pollution

Local emissions contribute to **global pollution** with **non-linear harm curve**:

```text
global_pollution_harm = f(global_pollution)  # superlinear, tunable
```

Affects: global worker health, food reliability, forest regrowth, production stability, disaster rate, collapse risk.

### 20.4 Collapse (Age 4)

If world damage exceeds thresholds:

- resource scarcity spikes;
- food/energy chains fail more often;
- disasters damage roads/buildings;
- restoration buildings/cards become critical;
- **shared loss** possible.

### 20.5 Information design

- **Player UI:** trends and world feedback, not “+10 pollution” tooltips on build buttons.
- **Debug HUD:** exact pollution maps, curves, culpability scores.

**Current state:** **Not implemented** — Milestone G only.

---

## 21. Natural disaster and scarcity systems

### 21.1 Disasters (MVP subset)

Flooding, storms, drought, wildfire (choose 2–3 for MVP implementation).

Triggered/strengthened by:

- global pollution level;
- local degradation;
- Age 4 collapse state.

### 21.2 Physical effects

Damage roads/buildings, interrupt jobs, injure/stress workers, reduce forest/farm output.

### 21.3 Scarcity

Age 4 collapse converts prior over-exploitation into **ware role shortages** (especially `raw_wood`/biomass, `food_processed`, `power_or_control`).

### 21.4 Restoration

Restoration buildings/cards consume resources to reduce local/global pollution and repair world damage—enabling restoration victory path.

---

## 22. UI, debug HUD and spectator requirements

### 22.1 Primary presentation

**2D oblique top-down** (existing `MapViewport`, sprite layers). 3D deferred.

### 22.2 Playable client (Milestone H minimum)

- 4P god-strategy scenario launch from menu (desktop first; mobile layout second).
- Build/road/delegated attack flows on touch/mouse.
- Draft UI for card picks (Milestone B).
- Victory/defeat/collapse screens with ending reason.

### 22.3 Debug HUD (required by Milestone A baseline, expand each milestone)

Must include (design goals §16.2):

- tick, speed, age, draft progress;
- hub health, VP, pollution/collapse status;
- ware inventories, shortages, jobs blocked;
- worker need summary, behaviour distribution;
- military stances, delegated targets;
- **AI module proposals and validation results**;
- event log.

### 22.4 Spectator

- Default **omniscient**; toggles for P1–P4 fog, economy, combat, worker society, pollution, AI intent overlays.
- Must consume **snapshots only** (non-authoritative client).
- 2D 4P spectator (`hybrid_bot_spectator_2d.tscn`) extended—not replaced.

### 22.5 Known gap

Headless SubViewport capture is 1×1 without `DISPLAY`—document in verification; use real display or synthesized layout diagrams for CI when needed.

---

## 23. Headless simulation and training requirements

### 23.1 Authority

- All rules in Godot/GDScript sim core.
- Python is **training/tooling only** (`python_tools/training/`).
- Integer/fixed-point hashed state; seeded RNG.

### 23.2 Required headless artefacts (each milestone)

- Scenario JSON fixtures.
- CLI runners: `run_god_strategy_smoke.gd`, `run_god_strategy_eval.gd` (proposed).
- JSONL decision telemetry, match summary, replay hash.
- Snapshot exports stable enough for observation encoders.

### 23.3 Training gates (no promotion without)

| Gate | Requirement |
|------|-------------|
| G0 | Kernel + prior variant smokes pass |
| G1 | Milestone A eval reaches documented ending in ≥X% of seeds |
| G2 | Fog-limited obs for eval bots |
| G3 | Invalid action rate < threshold |
| G4 | Beats passive + weak scripted baselines |
| G5 | Reach 4P endings (not tick-cap only) before “strong AI” claims |

### 23.4 Reuse RUN_06 infrastructure

Trial-game BC/eval (`run_training_smoke.gd`, `run_policy_eval_smoke.gd`) remains for economy/military trials—not god-strategy acceptance.

---

## 24. Bot and neural-training requirements

### 24.1 Bot hierarchy (build order)

1. **`ScriptedGodStrategyBot`** — extends `TutorialHybridBot` patterns to 4P + ages (Milestone A–B).
2. **`HeuristicModularGodBot`** — rule-based module proposals with inspectable pre-analytic/analytic stages (Milestone F).
3. **`LearnedModularGodBot`** — learned pre-analytic/analytic nets (Milestone F+, after heuristic modules and A–E gates).

**Do not** promote **learned god-AI** until: (a) Milestone A eval proves clean match endings; (b) Milestones B–E gates pass; (c) Milestone F heuristic modules emit inspectable proposals in eval/spectator telemetry. Worker archetype neural policies (Milestone E) are a **separate track** from god-AI learning.

### 24.2 Worker bots

Deterministic needs policy → shared archetype policy → optional neural archetype policy. **Separate** from god bots.

### 24.3 Teacher traces

- Full snapshots allowed in teacher collection.
- Rows must include: observation, legal mask, module proposals, chosen action, reward, tick, seed, player, scenario.

### 24.4 Current neural assets (honest status)

Existing `ModularNeuralHybridBot` checkpoints:

- prove **load/inference path**;
- **do not** prove competitive play;
- may be reused as Milestone F starting scaffold **after** new observation/action contracts stabilize.

### 24.5 Reward shaping (when RL begins)

Terminal win/loss plus shaped rewards for: logistics throughput, VP, hub defence, enemy infrastructure damage, collapse response, worker welfare (culture-weighted).

---

## 25. Implementation milestones

Each milestone is a **merge boundary** with its own run doc, branch, tests, and acceptance gate.

---

### Milestone A — Deterministic 4P logistics RTS loop

**Hard gate:** No Milestone B+ work may depend on 4P match resolution until this milestone’s acceptance criteria pass—including scripted domination regression and logistics-warfare proofs (see below).

**Goal:** 4P FFA reaches valid **domination or tick-cap/stalemate** ending with physical logistics and delegated combat—**no ages, culture, worker needs, pollution, or learned god AI**.

| In scope | Out of scope |
|----------|--------------|
| Retuned 4P scenario (symmetric boom, delayed conflict) | Cultural drafting |
| Command Hub elimination + hub rebuild site rule | Worker needs |
| Roads, storage, carriers, 1 production chain, 1 military unit | Pollution |
| Delegated attack; **logistics targets** (road/carrier/storehouse) | Neural training |
| Stalemate + tick-cap scoring (simple version) | Mobile polish |
| `ScriptedGodStrategyBot` (4P capable) | Age system |
| Headless 4P eval harness with winner/tick-cap artefacts | |
| Debug HUD subset (economy + military + AI intent) | |

**Implementation notes:**

- Extend `HybridFfaObjective` → `GodStrategyMatchEnd` (domination + tick-cap only for A).
- Add scenarios: `scenarios/god_strategy/god_strategy_4p_mvp_a.json`.
- Implement `ScriptedGodStrategyBot` by generalizing `TutorialHybridBot` with target priorities.
- Add logistics warfare tests.
- Tune until ≥**30%** of eval seeds reach domination **or** tick-cap with score spread within **T** ticks (T initially 8000–15000—tune in run doc).

**Tests:**

- `test_GOD_A_001` 4P scenario loads.
- `test_GOD_A_002` hub elimination eliminates player.
- `test_GOD_A_003` hub site prevents elimination.
- `test_GOD_A_004` road damage blocks deliveries.
- `test_GOD_A_005` delegated attack destroys hub in scripted scenario.
- `test_GOD_A_006` direct unit move rejected.
- `test_GOD_A_007` 4P headless eval produces ending artefact.
- `test_GOD_A_008` replay hash stable.

**Acceptance criteria:**

- [ ] 4P eval: ≥30% domination OR 100% valid tick-cap/stalemate resolution (no hangs).
- [ ] Scripted bot wins vs 3 passive bots in ≤8000 ticks in fixed seed regression.
- [ ] Logistics warfare test proves delivery failure after road destruction.
- [ ] Spectator shows 4P state + bot intent for eval replays.
- [ ] HYB-SMOKE 001–009 still pass (no regression).

**Evidence before B:** eval JSON + `winner.txt` or `no_winner_tick_cap.txt` + replay hash + test run log.

---

### Milestone B — Cultural draft skeleton and age progression

**Goal:** Four ages, 8 picks/player/age, cards apply modifiers/unlocks/VP; Age 3 VP check triggers win or Age 4 entry.

| In scope | Out of scope |
|----------|--------------|
| Age state machine | Worker needs |
| Draft pass-and-pick (simplified AI picker) | Pollution |
| Card schema + 4× small decks | Full 16-card pools |
| VP ledger + Age 3 check | Learned draft AI |
| `WareRoleRegistry` + age display names | |

**Tests:** draft determinism, card effect application, age transition, Age 3 VP win vs Age 4 branch.

**Acceptance:**

- [ ] 8 picks × 4 players completes in deterministic tick schedule.
- [ ] Card unlock visibly enables building in snapshot.
- [ ] Age 3 end: sole ≥10 VP player wins; multi ≥10 VP → Age 4.
- [ ] 2P tutorial path still works (legacy or simplified age-less scenario).

**Evidence before C:** headless match reaching Age 2 in smoke; Age 3 VP win scenario test.

---

### Milestone C — Worker needs prototype

**Goal:** Hunger/tiredness/happiness/safety affect throughput; deterministic satisfaction.

| In scope | Out of scope |
|----------|--------------|
| Needs state + update rules | Neural worker policies |
| Food/rest/leisure building hooks | Societal selection |
| HUD worker welfare summary | |

**Tests:** starving worker reduces output; rest recovers tiredness; leisure affects happiness.

**Acceptance:**

- [ ] Needs deterministic across replay hash.
- [ ] Measurable throughput delta when needs neglected.
- [ ] Debug HUD shows cohort need averages.

**Evidence before D:** needs telemetry in snapshots + tests green.

---

### Milestone D — Worker policy interface

**Goal:** Pluggable `WorkerPolicy` selects broad legal actions; deterministic fallback default.

| In scope | Out of scope |
|----------|--------------|
| WorkerObservation/LegalActionMask/Policy API | Societal selection |
| Table-driven test policy | Neural inference |
| Performance benchmarks (N workers) | |

**Acceptance:**

- [ ] Random/legal test policy runs 4P without crash.
- [ ] Invalid actions rejected/masked.
- [ ] Perf budget documented (ticks/sec with X workers).

**Evidence before E:** policy interface tests + perf note.

---

### Milestone E — Societal selection / evolving worker behaviour

**Goal:** Behaviour variants spread/decline based on multi-criteria fitness + culture modifiers.

| In scope | Out of scope |
|----------|--------------|
| Genome/trait weights per cohort | Per-worker large neural nets |
| Refresh tick selection | God AI learning |
| Behaviour histogram telemetry | |
| Optional: small neural archetype policy (1 net per archetype) | |

**Acceptance:**

- [ ] Two culture paths diverge behaviour distribution in fixed scenario.
- [ ] Pollution-free welfare culture vs industrial culture shows different prevalence (pre-G pollution via card modifiers if needed).
- [ ] Replay-deterministic selection given seed.

**Evidence before F:** telemetry JSONL showing distribution shift over match.

---

### Milestone F — Modular god AI architecture

**Goal:** God AIs use module proposals + coordinator; heuristic modules first; learned modules optional after traces.

| In scope | Out of scope |
|----------|--------------|
| Module interfaces (pre-analytic + analytic slots) | Collapse ecology |
| Heuristic Economy/Logistics/Military/Draft modules | Mobile polish |
| Coordinator + compiler integration | |
| Fog-limited eval | |
| Learned nets **if** A–E gates pass | |

**Acceptance:**

- [ ] Each module emits 0–3 inspectable proposals.
- [ ] Coordinator never bypasses validator.
- [ ] Eval bot beats Milestone A scripted bot on win-rate in ≥20% seeds (low bar—raise over time).
- [ ] Spectator HUD shows module summaries.

**Evidence before G:** win-rate table + proposal telemetry.

---

### Milestone G — Emergent pollution, scarcity and collapse

**Goal:** Ages 3–4 pollution, disasters, scarcity, restoration, collapse endings.

| In scope | Out of scope |
|----------|--------------|
| Local + global pollution | Full disaster catalogue |
| Non-linear harm curve | Mobile polish |
| 2–3 disaster types | |
| Restoration victory + shared loss | |
| Age 4 scarcity rules | |

**Acceptance:**

- [ ] Industrial Age 3 choices measurably degrade forests/farms/workers without warning pop-ups.
- [ ] Age 4 entry with high pollution → scarcity + increased disasters in tests.
- [ ] Restoration victory path testable in scripted scenario.
- [ ] Shared collapse loss testable at extreme pollution.

**Evidence before H:** Age 4 eval artefacts + visual review screenshots (DISPLAY).

---

### Milestone H — Mobile / spectator polish (last)

**Goal:** Human-can-watch and human-can-play minimal 4P god-strategy on mobile-shaped layout **after** loop is fun headlessly.

| In scope | Out of scope |
|----------|--------------|
| Touch build/road/attack/draft | Store release |
| 2D spectator overlays complete | 3D spectator |
| Mobile layout QA matrix (priority tests) | Full 18-test matrix if timeboxed |
| Victory/defeat UX | |

**Acceptance:**

- [ ] Human completes draft pick and delegated attack on 390×844 layout.
- [ ] Spectator overlays: economy, combat, worker society, pollution, AI intent.
- [ ] Manual playability QA checklist passes critical items.

---

## 26. Risks and scope controls

| Risk | Control |
|------|---------|
| Four ages × four decks × content explosion | Functional roles + config flavour; small pools; template cards |
| Worker neural cost | Cohort policies; archetype nets; perf gates |
| Culture drafting overwhelms logistics | Slow draft cadence; simple card effects first |
| Age 4 joint victory undermines FFA | Domination still valid; pollution culpability tie-break |
| Learned AI hides sim bugs | Legal command only; log validation failures; scripted teachers forever |
| Pre-analytic/analytic over-engineering | Heuristic stages first; learn only with traces |
| 4P never ends | Milestone A **blocks** later milestones until ending rate met |
| Scope creep from old neural/mobile PRD | Milestone H last; old PRD = infrastructure not DoD |

---

## 27. Testing and acceptance criteria

### 27.1 Global test principles

- Test-first for each new mechanic family (rule IDs: `GOD-*`, `CULT-*`, `WORK-*`, `POLL-*`).
- Headless Godot runner only for gameplay rules.
- Never weaken failure-mode integrity checks.
- Distinguish: **passes scaffold test** vs **passes gameplay proof**.

### 27.2 MVP completion checklist (all required)

- [ ] Milestones A–G acceptance criteria met with committed eval artefacts.
- [ ] 4P match endings documented (not 100% tick-cap stall).
- [ ] Four ages + draft function in headless smoke.
- [ ] Worker needs + selection change outcomes in telemetry.
- [ ] Pollution/collapse endings tested without warning-label UI.
- [ ] God AI modular proposals visible in spectator.
- [ ] Kernel + HYB-SMOKE + god-strategy smokes pass on Linux Godot 4.5.2.
- [ ] `verify_failure_modes.gd` pass.
- [ ] Honest audit doc updated (`CURRENT_STATE_OF_PLAY` successor).

### 27.3 Performance budgets (initial)

- 4P match: target ≥10 sim ticks/sec headless on dev machine (measure in Milestone A).
- Worker policy refresh: ≤5% tick time at 200 workers (adjust in D/E).

### 27.4 Verification commands (to be added to `local_verification.md` during Milestone A)

```bash
export GODOT=...
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s "$GODOT" --headless --path godot_project --import
timeout 900s "$GODOT" --headless --path godot_project --script res://sim/tools/run_tests.gd
timeout 300s "$GODOT" --headless --path godot_project --script res://sim/tools/run_god_strategy_eval.gd
timeout 120s "$GODOT" --headless --path godot_project --script res://sim/tools/verify_failure_modes.gd
```

---

## 28. Open questions that genuinely remain unresolved

These are **not** decided in the design goals doc with enough implementation detail for coding:

1. **Draft deal pattern:** pass-and-pick vs simultaneous reveal vs shared pool—confirm UX + determinism model.
2. **`draft_interval_ticks`:** initial value, scaling by age, and relation to age-transition schedule (Milestone B—culture/draft system).
3. **`worker_policy_refresh_ticks`:** initial value and scaling (Milestone E—societal selection); separate from draft cadence because it governs a different subsystem.
4. **Worker granularity:** individual worker needs vs cohort aggregation for MVP performance (Milestone C–D).
5. **Exact global pollution curve:** formula class and tunable coefficients (superlinear exponent, caps).
6. **Restoration victory thresholds:** quantitative joint goals vs domination-preferred meta.
7. **Stalemate resolution:** score-only vs forced escalation event when stall rule fires.
8. **Alliance/logistics sharing:** owned-only for MVP confirmed—future allied routes?
9. **Scout/hero marker role** in 4P start—is it cosmetic or mechanical?
10. **Age 4 unit naming vs reuse** of existing rifle squad code under new role ID.
11. **Migration strategy for economy regression tests** when introducing ware role indirection.
12. **Whether 2P hybrid MVP** migrates to god-strategy rules or remains frozen regression scenario.
13. **Minimum win-rate bar** for learned god AI promotion (proposed 20% → 50% staged—needs product sign-off).

---

## Appendix A — Suggested file/module layout (informational)

```text
godot_project/sim/god_strategy/
  age_system.gd
  culture_draft_system.gd
  victory_point_system.gd
  match_end_system.gd
  pollution_system.gd
  disaster_system.gd
  worker_needs_system.gd
  worker_policy_system.gd
  worker_selection_system.gd
  ware_role_registry.gd
  bot/scripted_god_strategy_bot.gd
  ai/god_module_*.gd
  ai/god_coordinator.gd

godot_project/sim/data/config/god_strategy/
  ages/*.json
  culture/*.json
  pollution.json

godot_project/sim/data/scenarios/god_strategy/
  god_strategy_4p_mvp_a.json
  ...

docs/runs/
  RUN_GOD_STRATEGY_MILESTONE_A.md
  ...
```

---

## Appendix B — Relationship to prior PRDs

| Prior work | Relationship |
|------------|--------------|
| **Final God-Strategy Playable PRD** | **Sole product DoD** for the final long run — see [`FINAL_GOD_STRATEGY_PLAYABLE_PRD.md`](FINAL_GOD_STRATEGY_PLAYABLE_PRD.md) |
| Neural Hybrid AI / Mobile PRD | Provides spectator shell, macro compiler, modular bot scaffold—**inputs** to Milestone F/H, not MVP DoD |
| RUN_07 hybrid integration | **Foundation**—extend, do not rewrite |
| RUN_06 trial neural | Parallel track for trial games only |
| Settler/Military MVPs | Tutorial/onboarding surfaces; god-strategy is the new primary competitive mode |

---

## Appendix C — One-sentence MVP target

A deterministic, testable, 4-player god-strategy logistics RTS where players guide civilisations through four ages by shaping infrastructure, culture, warfare, ecology, and worker society—while modular god minds and evolving worker policies adapt to the worlds those gods create, and industrial power in the modern age gives way to emergent collapse or restoration in the age to come.

---

*End of PRD.*

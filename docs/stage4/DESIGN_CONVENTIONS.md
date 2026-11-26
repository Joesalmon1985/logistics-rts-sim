# Stage 4 design conventions (read before writing or changing any design doc)

This is the shared contract for every document in `docs/stage4/`:
- the names, the time model, the layers, the policy levels, the events, the milestones, the document map and the open owner questions (section 13);
- where the documents disagree, this file wins until it is changed on purpose.

Owner decisions are recorded in the [plan](../UNITY_POC_PLAN.md#stage-4-a-living-population-under-a-state-planned-2627-nov-2026-not-started), and the full list is repeated below.

## 1. Vision in one paragraph
The game moves **from Darwin, through Dawkins, to Marx**: from how biological individuals evolve, through how cooperation and society emerge, to how production, ownership, labour, wealth and political institutions develop. Adam Smith (division of labour), Axelrod (the evolution of cooperation), Elinor Ostrom (governing common resources) and the *Creatures* games (drives, perception, lifetime learning) are equal influences. The player governs a state through institutions and incentives, never by controlling people. **Nothing is forced.** No economic or political system is built to win. Outcomes must arise from individuals, environments, institutions and the player's choices. See [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md).

## 2. Owner decisions (complete list, 25–26 Nov 2026)

**People and society**
- NPCs replace carriers, builders and harvesters. Soldiers and institution staff are the same persistent NPCs. Nothing is ever spawned to do a job.
- NPCs belong to a faction and can migrate, defect or be captured.
- **Full life cycle:** birth, childhood, adulthood, ageing, death, relationships, households, reproduction, child-rearing, care of relatives, passing on knowledge and possessions.
- **Four kinds of inheritance**, which interact but stay distinct in code and data:
  - **biological:** body and predispositions, through a genome;
  - **individual lifetime learning:** never inherited directly;
  - **cultural:** observation, communication, imitation, teaching;
  - **material:** property, goods, tools, productive assets.
- **Classes and strategies are not character types.**
  - Economic classes emerge from each NPC's relationship to property, productive assets, labour and surplus.
  - Strategies (Hawk, Grudger and so on) emerge from personality, circumstance, experience and decisions.
  - The named strategies exist only as **benchmark policies** for controlled experiments.

**Mind and communication**
- **Lifetime learning is a core workstream, not an optional extra.**
  - Innate instincts and drives give baseline behaviour, and individuals learn associations from their own experience.
  - Learning covers every domain: survival, gathering, production, gear, barter, cooperation, exploitation, relationships, communication, work, military service, faction membership.
  - Five mechanisms stay distinct: instincts and drives; individual associative learning; social learning (observation and imitation); cultural transmission (between individuals and generations); offline network training. Offline training never replaces lifetime learning. Trained policies later take learned associations and memories as inputs.
- **NPCs never read the true game state.** They act on what they perceive, remember, infer or are told.
- **Non-verbal compositional gesture language.** It is a real sim mechanic: NPCs decide whether, what and to whom to communicate, and how to respond. Lies, broken promises, misunderstandings, ignoring and changes of mind are all possible. Unity only animates the events; it never decides them. Headless works the same. The vocabulary is fixed at first, with learned and cultural meanings later.

**Economy**
- **Ownership:** state, private (one NPC), communal (a group), cooperative (worker-owned) and, later, other forms.
- NPCs can found workshops, employ others, accumulate wealth and pass assets to heirs. Others may come to depend on selling their labour.
- **Money:** barter only at first (goods for goods or labour). Currency comes in a later stage.
- **Construction:** the state designates building, road and HQ sites and commits treasury resources. NPC labour must do the building. Nothing is magicked into place.

**The state and its institutions**
- The player operates the **institutions of a state** (see [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)) through policies, taxes, investment and incentives.
- Institutions develop gradually through **civilisation ages**, starting from simple equivalents: elders, healers, communal mediation, guards, informal lending.
- **Ages advance by emergent unlocks.** An institution or technology becomes possible when the society can sustain it: population, surplus, knowledge spread by learning and culture, and the institutions that already exist. There is no fixed timeline, and factions can be in different ages.
- **Institutions change the strategic environment** (courts make promises credible, and so on). Institution staff keep their own motives, so management can be poor, corrupt, short-staffed or failing. No structure or ideology is assumed to be best.
- **Goodhart's law is built in.** Institutions respond to targets, and optimising a reported KPI need not improve the real outcome. Nothing rewards a reported KPI directly.

**Information and data**
- **Seven information layers** stay separate (section 6).
- **The player:** Stage 4 **builds the separation now**, and the HUD reads through it. The player still gets near-complete reports until statistics institutions arrive in a later age. Enforcing limited information comes later.
- **The analytical database:**
  - The sim emits structured domain events and periodic snapshots.
  - These can be stored in an analytical database: SQLite first, with Parquet and DuckDB for large runs.
  - It uses a dimensional model.
  - Recording can never change sim outcomes, and it can be configured down to nothing for training runs.

**Game modes and AI**
- **Victory:** keep 10 VP for now. Victory conditions are pluggable (prosperity, population, economic development and more later). **Sandbox first:** design and balance target the open-ended, multi-generation sandbox. The 10 VP race stays as a shorter competitive mode on the same rules.
- **Time scale is adjustable per game.** See section 4.
- **Four policy levels**, each replaceable on its own: civilian, military, institution, faction. They are scripted first, with networks later. Institution and faction policies use exactly the information, constraints, budgets and actions a human has at that level.
- **Training:** Python and PyTorch (the owner has a GPU). No training in stage 4. **Exact determinism**: deployed models run integer-only inference inside the sim and replay exactly.

**Experiments**
- **Lab vs island:** keep (a) controlled maths experiments with the book's payoffs separate from (b) island experiments where payoffs come from real consequences. The island is never forced towards a theoretical equilibrium.
- **Policy experiments** compare institutions and policies across seeds and generations. Keep causal experiments (randomised, controlled) separate from descriptive analysis. Never manufacture outcomes to support a theory.

## 3. Layers and determinism (unchanged from stages 1–3)
- **Two layers:**
  - `Assets/God/Sim`: pure C#, no UnityEngine. Integers or fixed point only, the seeded `Rng`, no wall clock, iteration in a fixed order (ascending id), no unordered enumeration. Also compiled by `DotNet/` for fast tests.
  - `Assets/God/Game`: Unity views. It reads state, sends commands and animates events. It never changes sim state.
- **Every random draw uses a forked stream:** `Rng.Fork("npc", id)`, `Rng.Fork("inst", id)` and so on. Nothing shares a stream across systems.
- **Tuning lives on `SimTuning`**, a per-sim object saved with the state. No mutable statics. The M0 audit removes them; see [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md):
  - `MilitaryTuning`'s 22 mutable static fields;
  - mutable arrays inside `EconomyDefs` building defs;
  - the lazily filled `TradeTuning` value table;
  - `static readonly` arrays whose contents can be changed.
- **`double` in rules:** a few rules use `double` today (board topology, lattice, faction rules, parts of the economy, the HQ circle). New code uses integers only. Converting the old code is decided in M0.
- **Saves:** all state is serialisable. Save version 3 arrives with M3. `StateHash` covers every module, and from M0 also the **pending command queue**. Adding the queue changes every stage 3 hash once; that is accepted. See [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md#44-hashing).
- **Recording is outside the sim:** recorders, the database and metrics only *consume* events and snapshots. A run with recording off has the same hash as a run with it on.
- **Institutional records are not recording.** Layer 4 in section 6 holds the records institutions keep inside the game, such as a census, a court register or a tax roll. These are game state: saved, hashed, and able to change what happens. The analytical warehouse is outside the sim; institutional records are inside it.
- **Random streams are keyed by entity and purpose, never by order of use.** Use `Rng.Fork("npc", id).Fork("forage")`, not one shared stream that each draw advances. Then adding an NPC or switching a policy doesn't shift every other random draw. Common random numbers in experiments depend on this.

## 4. Time model
- **Ticks:** the sim runs at 10 ticks per second at 1x speed, as now.
- **`SimCalendar`:** it maps ticks to **days** and **years**.
  - `GameSetup.TicksPerDay` defaults to 1200, or 2 minutes at 1x, with a day/night cycle.
  - `GameSetup.DaysPerYear` defaults to 10.
  - With the defaults, a year takes 20 real minutes at 1x.
- **`GameSetup.LifeScale`:** this is the adjustable time scale, a percentage, default 100. It scales every life-cycle duration (childhood, fertility, ageing, lifespan), so generations come faster or slower without changing work and walking speeds. **Higher is faster:** sim age advances at `LifeScale / 100` years per calendar year, so at 200 every life-cycle duration takes half the ticks ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#time-and-life-stages)).
- **What `LifeScale` does not scale:** work, walking, production, the think rate, and (by default) the per-day decay of social memory and learned associations. Whether decay should keep pace with lives is an open owner question (section 13).
- **Think rate:** an NPC decides every `ThinkEvery` ticks (default 10, about once a second at 1x), staggered by `(tick + id) % ThinkEvery`. Reflexes run every tick. Level of detail may lengthen it for distant NPCs ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md#49-level-of-detail-and-bounded-cost-per-tick)).
- **Default ages at LifeScale 100:**
  - child until **year 1**, about 20 real minutes, an "age of 16";
  - adult until year 3;
  - elder after year 3;
  - natural death around year 3.5–4.5.

  - Fertility from year 0.9.
  - Pregnancy lasts 0.25 years.

  So one generation (from birth to the birth of a first child) takes about **1.3–1.7 sim years**, or 25–35 real minutes. A 60-minute game sees about 2 generations. See [POPULATION_DESIGN.md](POPULATION_DESIGN.md).
- **`Rng` changes:** M0 adds `Rng.Fork(label, id)` overloads (today `Fork` takes only a label). It also moves the dice and other draws from the shared `s.Rng` onto keyed forks. That changes the results of stage 3 seeds once, and is accepted.
- **Ages in the docs:** write ages in sim years (`Year`), and show human-scale ages in the HUD (sim year × 16 = shown age).
- **Headless speed:** the headless runner runs at maximum speed, and hundreds of sim years are the aim for long experiments. See [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md) for performance targets.

## 5. Modules and names (`Assets/God/Sim/...`)

| Module | Holds |
|---|---|
| `Population` | `Npc`, body, drives, life stage, `Genome` (biological), households, kinship, births and deaths |
| `Cognition` | perception, attention, beliefs (`Mind.Beliefs`), situation features, the decider |
| `Learning` | associative learning: `AssociationStore`, eligibility traces, instincts as priors, social-learning hooks, **imitation of associations** (copied entries tagged `Imitated`), and the `MindView` it exposes to policies. See [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) |
| `Social` | relationship records, obligations, reputation, gossip, per-pair contest history; the civilian contest mechanic (`Social/Contests`, designed in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)) |
| `Comms` | gesture messages (`MsgTopic`), interpretation, lexicons |
| `Culture` | cultural traits and memes, teaching, **trait transmission records** (`TraitImitated` and others), dialects. Traits act on decisions through a separate Culture score term; imitation of associations belongs to `Learning` |
| `Economy` | items (with true and visible quality), ownership records, exchanges, production, jobs, enterprises, `WareRefValue` (existing module, extended) |
| `Estate` | material inheritance: wills or customs, estates, transfers on death |
| `State` | treasury, taxes, policies, contracts, sites, laws, citizenship, victory conditions |
| `Institutions` | the generic institution framework and each institution type. See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) |
| `Information` | institutional records, media publications, the faction knowledge view, and reports the player and AIs read |
| `Military` | soldier combat (existing), service, assignments, morale |
| `Lab` | maths-only game-theory experiments |
| `Policy` | policy interfaces, the registry, observation and intent types, `IntNet` (integer inference) |
| `Telemetry` | domain event definitions and the snapshot schema. Pure data, emitted by the sim. |

**Memory per NPC:** the combined budget (learning, social memory, beliefs, culture, kin beliefs, genome and the rest, with a total) is the table in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#memory-budget-per-npc-combined): about 17 KB per NPC at default caps, budget 20 KiB. A document that raises its cap updates that table.

Recorders that write SQLite, JSONL or Parquet live **outside** the sim, in `DotNet/God.Runner` and an optional Unity recorder. See [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).

**Ids** are `int`s, unique per kind for the whole game, and never reused: `NpcId`, `HouseholdId`, `BuildingId`, `InstitutionId`, `EnterpriseId`, `ContractId`, `FactionId`, `ItemLotId`. Surrogate keys in the warehouse are separate from sim ids.

## 6. Information layers (one vocabulary for every doc)
1. **True state:** `GodState`. Only the sim's systems read it.
2. **Observed events:** what one NPC perceived (`NpcObservation`, its perception memory).
3. **Communicated information:** messages and gossip between NPCs, which can be wrong or false.
4. **Institutional records:** what an institution wrote down, from its staff's observations and the reports it receives. Records can be incomplete, late, biased or falsified.
5. **Published information:** media output. It travels beyond personal contact and has an owner, a reach and a credibility.
6. **Individual beliefs:** each NPC's `Mind.Beliefs`, built from layers 2, 3 and 5.
7. **Player and faction knowledge:** what the state's institutions report: `FactionKnowledgeView`. It is the **only** thing the HUD and faction AIs read.

In stage 4 the view in layer 7 is filled almost completely (a `FullInformation` setting). The code paths still go through it, so later ages can limit it without rewriting the HUD or the AI.

## 7. Policy levels

| Interface | Decides for | Reads | Acts through |
|---|---|---|---|
| `INpcPolicy` | one civilian NPC | `NpcObservation` plus its own mind through `MindView` (beliefs, memory, associations) | `NpcIntent` |
| `IMilitaryPolicy` | one NPC on military duty (combat decisions) | `SoldierObservation` plus its own mind through `MindView` | `NpcIntent` (combat subset) |
| `IInstitutionPolicy` | one institution's management | `InstitutionView`: its own budget, staff, records and the faction's directives | `List<Command>`: institution commands, within budget and powers |
| `IFactionPolicy` | a faction's state (AI or human) | `FactionKnowledgeView` | `List<Command>`: state commands, the same ones the HUD sends |

**Signatures** (one pattern for all four levels):
```csharp
// individual levels: INpcPolicy (TObservation = NpcObservation), IMilitaryPolicy (SoldierObservation)
NpcIntent Decide(in TObservation obs, MindView mind, ref Rng rng);
// institution and faction levels: IInstitutionPolicy (TView = InstitutionView), IFactionPolicy (FactionKnowledgeView)
void Decide(in TView view, ref Rng rng, List<Command> outCommands);
```
- `MindView` is a read-only view of the NPC's own mind, defined by [NPC_LEARNING_DESIGN.md §3.6](NPC_LEARNING_DESIGN.md#36-mindview-the-read-only-mind-interface). [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md) encodes it and does not redefine it.
- The default civilian policy is **`AssociativeNpcPolicy`**: instincts plus learned associations, scored by the decider ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).

**Rules for policies:**
- No policy may reference `GodState`, and a reflection test enforces this. No policy may read an event's analytics-only truth fields (section 8).
- Every policy uses the same intents and commands, whether scripted, benchmark, trained or human.
- Lifetime learning lives in each NPC's mind, not in the policy object. A scripted policy and a trained policy both read the same learned associations.

## 8. Domain events and snapshots
- **Domain events:** the sim emits typed **domain events** (`Telemetry`), each stamped with a tick and the ids involved. The main families, with examples:
  - **life cycle:** `NpcBorn`, `NpcDied`, `PregnancyStarted`, `NpcImmigrated`, `NpcEmigrated`;
  - **household and kin:** `HouseholdFormed`, `HouseholdJoined`, `HouseholdLeft`, `PartnershipFormed`, `CareGiven`;
  - **economy:** `ExchangeStep`, `GiftGiven`, `JobTaken`, `WagePaid`, `OwnershipChanged`, `TheftCommitted`, `EnterpriseFounded`;
  - **estate:** `EstateOpened`, `EstateTransfer`, `EstateSettled`, `WillMade`;
  - **state:** `TaxCollected`, `ContractPosted`, `CitizenshipChanged`, `LawEnacted`, `CrimeCommitted`;
  - **social:** `PromiseMade`, `PromiseBroken`, `GossipPassed`, `TrustChanged`, `ContestEnded`;
  - **comms:** `GestureMade`, `MessageDecoded`;
  - **learning and culture:** `AssociationUpdated`, `AssociationsImitated`, `TeachingGiven`, `TraitImitated`;
  - **military:** `Enlisted`, `KitIssued`, `AttackResolved`, `Deserted`, `SurrenderAccepted`;
  - **institutions and politics:** `InstitutionFounded`, `CaseOpened`, `ReturnFiled`, `IncentivePaid`, `GrievanceFormed`;
  - **information, policy and KPIs:** `RecordWritten`, `RecordFalsified`, `PolicyChanged`, `KpiReported`.

  The **full catalogue**, with each event's target table, lives in [DATA_WAREHOUSE_DESIGN.md §3.7](DATA_WAREHOUSE_DESIGN.md#37-event-catalogue-event-to-table-map); each owning document defines its events' fields.
- **Truth fields:** an event may carry analytics-only truth fields (whether a message was a lie, a record falsified, a return inflated). Policies and sim rules never read them, and recorders store them only at the Full recording level.
- **`KpiReported` and returns:** `KpiReported` is the faction's measured value; under `FullInformation` it equals the actual value. Institution managers file separate returns (`FileReturn` command, `ReturnFiled` event) that can be gamed, and incentives are paid only on returns. In later ages, reported KPIs are built from records and returns ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **Snapshots:** the sim also produces periodic **snapshots**: a daily person snapshot, and daily institution and faction snapshots.
- **These are the warehouse's only sources.** The view's animation events (such as `GestureMade`) are the same events. The game, the runner and the warehouse all consume one stream.

## 9. Civilisation ages (emergent unlocks)

| Age | What it means | Institution equivalents | Stage that builds it |
|---|---|---|---|
| **Band** | Subsistence | Elders, healer, communal mediation, shared store, guards, informal lending between kin and friends | **Stage 4 foundations** |
| **Village** | Surplus and specialisation | Granary and welfare, headman's court, militia, workshops, cooperatives | Stage 5 |
| **Town** | Markets and employment at scale | Courts, police, prisons, schools, hospitals, money and banks, chartered companies | Stage 6 |
| **City** | Mass institutions | Media, statistics office, central bank, regulators, environmental agency, research | Stage 7 |

Each faction's age is measured, not chosen. An unlock condition is a rule over measured society, such as "a granary institution is possible when the faction's food surplus over the last 5 days exceeds X, and enough adults believe in storing food (a cultural trait)". See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#ages-and-unlocks).

## 10. Roadmap and stage 4 milestones (the official list)
Stage 4 builds the **foundations** for everything above. Later stages add institutions and depth. Each milestone leaves a working, tested game; acceptance criteria are in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

| # | Milestone | Needs |
|---|---|---|
| M0 | Headless runner, `SimTuning`, `SimCalendar` with LifeScale, `IVictoryCondition` (VP, sandbox), domain event stream plus JSONL recorder, replay and hash checks | - |
| M1 | Policy seams (all four interfaces; `IFactionPolicy` wraps `AiBrain`), `PolicyRegistry`, `FactionKnowledgeView` with `FullInformation`, the reflection test | M0 |
| M2 | Game-theory lab, maths only (benchmarks reproduced) | M0 |
| M3 | NPC core: body, drives, perception, attention, beliefs, **instincts plus the associative learning core**, foraging, huts, personal items | M1 |
| M4 | Life cycle and households: birth, childhood, ageing, death, partnership, reproduction, kinship, `Genome` (biological inheritance), basic estates (material inheritance by custom) | M3 |
| M5 | Social memory, gestures, honest barter as separate hand-overs, gifts | M3 |
| M6 | NPC labour replaces walkers; basic state (treasury, tax, wages, jobs, sites and contracts, purchase offers); ownership records (state, private, communal) | M5 |
| M7 | Deferred exchange, promises, reputation, lies, misunderstanding, **social learning by observation** | M5 |
| M8 | Enterprise: NPC workshops, NPCs employing NPCs, cooperatives, communal property, theft and witnesses, class *measures* | M6, M7 |
| M9 | Soldiers are NPCs (enlistment, kit, rations, assignments, morale, desertion, surrender) | M6 |
| M10 | Citizenship, loyalty, migration, defection; political-interest *measures* | M7, M9 |
| M11 | Cultural transmission: teaching, imitation of successful others, memes, dialects, traditions between generations | M4, M7 |
| M12 | Band-age institutions framework: elders and mediation, healer, guards, shared store and welfare, informal lending, through the generic institution model with NPC staff and `IInstitutionPolicy` | M6, M10 |
| M13 | Analytical recording: SQLite warehouse (core dimensions and facts), Band-age KPIs, reports through `FactionKnowledgeView`, the HUD reports panel | M0, M12 |
| M14 | Incentive faction AI, balance (sandbox first, then VP), HUD and tutorial rewrite, play guide | M6–M13 |
| M15 | Network readiness: encodings, masks, `IntNet` policy, dataset export, Python bridge and verification | M1, M14 |

**Workstreams across milestones:**

| Workstream | Milestones |
|---|---|
| Lifetime learning | M3 core, M5 social, M7 observational, M11 cultural, M15 network inputs |
| Information layers | M1 → M13 |
| Recording | M0 → M13 |

**After stage 4:**

| Stage | Adds |
|---|---|
| Stage 5 (Village age) | Collective bargaining and action, political movements, cooperatives at scale, the headman's court and militia, granaries, schools-lite, money pilot |
| Stage 6 (Town age) | Courts, police, prisons, healthcare, education, currency and banks, Goodhart-prone targets for institution managers, the policy-experiment suite |
| Stage 7 (City age) | Media and statistics institutions with limited information enforced, financial regulation, environmental agency, large-scale network training |

The owning documents give each system's split between stage 4 minimum and later.

## 11. Document map (one owner topic each; link, don't repeat)

| Document | Owns |
|---|---|
| [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md) | Darwin → Dawkins → Marx, plus Smith, Axelrod, Ostrom and *Creatures*; neutrality principles; how each influence becomes a mechanic |
| [POPULATION_DESIGN.md](POPULATION_DESIGN.md) | The NPC record, body, drives, life cycle, population dynamics (fertility, mortality, immigration), personality traits, skills, the combined per-NPC memory budget |
| [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md) | The genome, genes to traits, recombination, mutation, selection pressure, kin relatedness values |
| [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md) | Partnership, courtship, households, child-rearing, care of relatives, the kinship graph |
| [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) | Instincts and the associative learning core, delayed reward, generalisation, forgetting, the five learning mechanisms |
| [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) | Perception, attention, beliefs, situation features, the decider, the gesture language |
| [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) | Relationships, trust, obligations and promises, reputation, gossip, forgetting |
| [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) | Cultural traits and memes, teaching, imitation, conformity, dialects, traditions |
| [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md) | Estates, inheritance customs and laws, bequests, dowries and gifts, wealth across generations |
| [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) | Items, item quality (true and visible; fakes at M8), `WareRefValue` and the FE unit (a measurement index NPCs never read), production, subsistence → specialisation → surplus, barter, deferred exchange, jobs, property forms, enterprises, commons (Ostrom), division of labour (Smith) |
| [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md) | Class as a measured relation to property, labour and surplus; mobility; how inequality arises |
| [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) | Material interests, political beliefs, collective bargaining and action, disputes, alliances, movements, institutional change, competing ways of organising the economy |
| [STATE_DESIGN.md](STATE_DESIGN.md) | The stage 4 state: treasury, taxes, wages, purchases, sites and contracts, welfare basics, citizenship and loyalty, laws v1, victory conditions |
| [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) | The generic institution model, the catalogue by age, provision models (public, private, cooperative, communal), how each changes the strategic environment, failure modes, Goodhart's law, ages and unlocks |
| [MILITARY_DESIGN.md](MILITARY_DESIGN.md) | Soldier combat, service, kit and the quartermaster's store, rations, assignments, morale, retreat, desertion, surrender, defection, truces, the military policy |
| [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) | The seven information layers in detail, records, data collection (census, surveys, inspections), media, misinformation, measuring better can look worse |
| [KPI_CATALOGUE.md](KPI_CATALOGUE.md) | Every KPI: definition, numerator, denominator, period, eligibility, source table, breakdowns, the actual vs reported pair, gaming risks |
| [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) | Event and snapshot schema to warehouse; dimensions and facts (grain, keys, time semantics, sources); SQLite, Parquet, DuckDB; recording levels |
| [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md) | Each example from the book mapped onto the game; lab vs island; measuring strategies after the fact; how institutions change each game; **the shared civilian contest mechanic** (disputes over a resource: escalation turns, posture, use of `Duel` with the `Unarmed` profile). Per-pair contest history is SOCIAL_MEMORY's; soldier combat is MILITARY's |
| [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md) | Exact benchmark policies and lab experiments with expected results and tolerances |
| [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md) | The runner: commands (the CLI lists every command other documents need), scenarios, parallel runs, replays and hashes, save/load, performance and level of detail, metrics output |
| [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md) | Experiment design: treatments, controls, seeds, generations, outcome measures, causal vs descriptive, pre-registration, a neutrality checklist |
| [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md) | Observations and actions per policy level, `IntNet`, quantisation, Python bridge, imitation then reinforcement, verification, how trained policies use lifetime-learned state |
| [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md) | Automated tests and acceptance criteria per milestone (gathered from the docs' Tests sections) |
| [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md), [research/CREATURES_NOTES.md](research/CREATURES_NOTES.md) | Source research (read-only) |

## 12. How every design doc is written
- **Style:** plain English, short sentences, tables where they help. Integers and named fields where you mean code. British spelling.
- **Required sections, in this order:**
  1. Purpose and scope (which topics it owns and which it links to);
  2. Principles;
  3. Data structures (C#-like sketches; integer types; bounded sizes);
  4. Mechanics or algorithms (deterministic: say the order and the random stream);
  5. Interactions with other systems (links);
  6. Player and view (what the HUD shows; animations only show events);
  7. Telemetry (which domain events and snapshot fields it emits);
  8. Stage 4 minimum vs later stages (be explicit, using the milestone numbers above);
  9. Migration from the existing code where relevant;
  10. Tests and acceptance criteria (specific, automatable, with numbers or tolerances where possible);
  11. Open questions (only real ones).
- **Don't duplicate.** If another document owns a topic, give one line and a link.
- **Never present an outcome as designed in.** Say which mechanics *make it possible*, and how to *measure* whether it happened.
- **No quotes from copyrighted sources** beyond a few words. Paraphrase and cite page numbers from the research notes.

## 13. Open questions (owner)
Only the owner can answer these. Each has a **default**: implementation proceeds with it unless the owner says otherwise. Implementation details are settled in the owning documents, not here.

**Several documents**
1. Should the per-day decay of social memory and of learned associations scale with `LifeScale`, so forgetting keeps pace with lives? *Default: no; decay is per sim day, and `LifeScale` scales only life-cycle durations* ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#47-forgetting), [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).

**[VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md)**

2. Should the game name the theories (Darwin, Dawkins, Marx) to the player? *Default: only on an optional "about" page, never in reports or the tutorial.*

**[POPULATION_DESIGN.md](POPULATION_DESIGN.md)**

3. Is disease on in the default sandbox? *Default: a `Disease` setting (On, Mild, Off): On in the sandbox, Mild in the tutorial.*

**[BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md)**

4. The mutation rate (40 per 10,000 loci, about 0.26 new variants per gamete) is faster than nature so that change shows within 10 generations. Keep it? *Default: keep, tunable per scenario.*

**[HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)**

5. Concurrent partners? *Default: serial partnerships only in M4; concurrent partners later.*
6. Same-sex partnerships? *Default: allowed (they produce no child).*

**[NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)**

7. Should loyalty or belonging be a reward signal for learning? *Default: no. If political economy needs it, add a `Belonging` drive in Population instead.*

**[ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)**

8. Staffed workshops from M6 in both modes (sandbox and VP)? *Default: on in both; balanced in M14.*
9. May NPCs gather dice-produced piles by hand? *Default: yes; harvester buildings stay about 4× faster.*
10. Keep the stage 3 variety rule? *Default: keep it, and also test log and clay as complementary inputs.*
11. Does forest stock limit dice-produced log piles? *Default: off in VP mode, on in the sandbox.*

**[ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)**

12. Does care work count as work? *Default: yes for the "not in work" test, but not in the relation shares that define class.*

**[MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)**

13. Keep the fixed executor ladder (who settles an estate)? *Default: yes.*
14. Can executors skim estates? *Default: not in stage 4; considered at M12.*

**[GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)**

15. Is a shared nursery an informal household arrangement in stage 4? *Default: yes; a formal institution from Stage 5.*

**[STATE_DESIGN.md](STATE_DESIGN.md)**

16. Two automatic HQ rules remain in stage 4: paying wages with no paymaster on staff, and answering `AskJoin`. Keep them as rules? *Default: yes; a staffed paymaster or official comes in the Village age.*

**[INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)**

17. Can households or groups found communal institutions without the state? *Default: no in M12; a `FoundCommunal` intent comes in Stage 5.*

**[MILITARY_DESIGN.md](MILITARY_DESIGN.md)**

18. Is there a law of war in stage 4? *Default: no; killing a surrendering soldier costs only reputation.*

**[POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)**

19. Does a walk-out end the job contract or suspend it? *Default: suspend it; the employer may end it.*
20. Formal voting? *Default: a `DecisionRule` for associations in Stage 5; state-level voting only as a later institution.*

**[INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)**

21. What does a faction know about foreign factions under `FullInformation`? *Default: the current visibility rules, not open foreign books.*
22. Is the debug true-state overlay available in competitive play? *Default: off, set by the scenario's rules.*

**[POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)**

23. A project-wide composite wellbeing index? *Default: none. An experiment may use one (such as LIV-06) only with its weights written in its file.*

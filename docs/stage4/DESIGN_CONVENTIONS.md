# Stage 4 design conventions (read before writing or changing any design doc)

This is the shared contract for every document in `docs/stage4/`:
- the names, the time model, the layers, the policy levels, the events, the milestones, the document map, the resolved owner decisions (section 13) and the further owner requirements (section 14);
- where the documents disagree, this file wins until it is changed on purpose.

Owner decisions are recorded in the [plan](../UNITY_POC_PLAN.md#stage-4-a-living-population-under-a-state-planned-2627-nov-2026-not-started), and the full list is repeated below. The owner answered every open question on 27 Nov 2026 (section 13); no open owner question remains. Progress of the implementation run is in [DEV_LOG.md](DEV_LOG.md).

## 1. Vision in one paragraph
The game moves **from Darwin, through Dawkins, to Marx**: from how biological individuals evolve, through how cooperation and society emerge, to how production, ownership, labour, wealth and political institutions develop. Adam Smith (division of labour), Axelrod (the evolution of cooperation), Elinor Ostrom (governing common resources) and the *Creatures* games (drives, perception, lifetime learning) are equal influences. The player governs a state through institutions and incentives, never by controlling people. **Nothing is forced.** No economic or political system is built to win. Outcomes must arise from individuals, environments, institutions and the player's choices. See [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md).

## 2. Owner decisions (complete list, 25–27 Nov 2026)
Decisions added or changed by the owner's answers of 27 Nov 2026 are marked with the answer number from section 13, for example (Q6).

**People and society**
- NPCs replace carriers, builders and harvesters. Soldiers and institution staff are the same persistent NPCs. Nothing is ever spawned to do a job.
- NPCs belong to a faction and can migrate, defect or be captured.
- **Full life cycle:** birth, childhood, adulthood, ageing, death, relationships, households, reproduction, child-rearing, care of relatives, passing on knowledge and possessions.
- **Reproduction is for adults only.** Growth, decline and extinction must emerge; workers, soldiers and citizens are never auto-replenished (section 14).
- **Partnerships (Q5, Q6):** individuals enter and leave relationships voluntarily. Serial partnerships are enough for M4, but the relationship architecture must not permanently restrict anyone to one partner: concurrent partnerships and other household customs must be possible later. **Same-sex partnerships are explicitly supported**, with the same social, emotional, household, economic, caregiving and material-inheritance capabilities as any other partnership. Only biological reproduction is constrained, by the reproductive model. Adoption, fostering and caring for children are open to all suitable households; there are no arbitrary differences in value.
- **Childcare (Q15):** biological parents are not the only possible caregivers. Informal shared childcare between households is supported in Stage 4; a formal institution can emerge in Stage 5 or later.
- **Care work is real work (Q12).** Child-rearing, caring for relatives, preparing food, tending the injured and maintaining households cost time and resources and give genuine benefits. Unpaid care is reported separately so that it is never economically invisible ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md), [KPI_CATALOGUE.md](KPI_CATALOGUE.md)).
- **Disease is on by default (Q3):** setting `Disease` On, Mild or Off (sandbox On, tutorial Mild, controlled experiments configurable). Disease links nutrition, crowding, sanitation, healthcare, population growth and mortality, and is never an arbitrary event that play cannot understand or mitigate ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#disease-simple-model-in-stage-4-healthcare-later)).
- **Loyalty is earned (Q7).** Faction loyalty is never rewarded directly. It emerges from experiences, relationships, security, living standards, cultural beliefs and treatment by the state. A configurable `Belonging` drive is allowed only as social motivation, never as a reward for obeying a faction.
- **Four kinds of inheritance**, which interact but stay distinct in code and data:
  - **biological:** body and predispositions, through a genome;
  - **individual lifetime learning:** never inherited directly;
  - **cultural:** observation, communication, imitation, teaching;
  - **material:** property, goods, tools, productive assets.
- **Classes and strategies are not character types.**
  - Economic classes emerge from each NPC's relationship to property, productive assets, labour and surplus.
  - Strategies (Hawk, Grudger and so on) emerge from personality, circumstance, experience and decisions.
  - The named strategies exist only as **benchmark policies** for controlled experiments.
  - No permanent class labels, and no political beliefs assigned from class.

**Mind and communication**
- **Lifetime learning is a core workstream, not an optional extra.**
  - Innate instincts and drives give baseline behaviour, and individuals learn associations from their own experience.
  - Learning covers every domain: survival, gathering, production, gear, barter, cooperation, exploitation, relationships, communication, work, military service, faction membership.
  - Five mechanisms stay distinct: instincts and drives; individual associative learning; social learning (observation and imitation); cultural transmission (between individuals and generations); offline network training. Offline training never replaces lifetime learning. Trained policies later take learned associations and memories as inputs.
  - **Decay is independent of `LifeScale` (Q1).** The decay of social memory and of learned associations is per sim day and is independently configurable for experiments (section 4).
- **NPCs never read the true game state.** They act on what they perceive, remember, infer or are told.
- **Non-verbal compositional gesture language.** It is a real sim mechanic: NPCs decide whether, what and to whom to communicate, and how to respond. Lies, broken promises, misunderstandings, ignoring and changes of mind are all possible. Unity only animates the events; it never decides them. Headless works the same. The vocabulary is fixed at first, with learned and cultural meanings later.

**Economy**
- **Ownership:** state, private (one NPC), communal (a group), cooperative (worker-owned) and, later, other forms.
- NPCs can found workshops, employ others, accumulate wealth and pass assets to heirs. Others may come to depend on selling their labour.
- **Money:** barter only at first (goods for goods or labour). Currency comes in a later stage.
- **Construction:** the state designates building, road and HQ sites and commits treasury resources. NPC labour must do the building. Nothing is magicked into place.
- **Staffed workshops in both modes (Q8).** All productive work is ultimately done by persistent individuals, in the sandbox and in the VP mode. No anonymous or auto-generated workers are kept to preserve stage 3 balance; production, staffing and resources are rebalanced around the population system.
- **Hand gathering (Q9):** NPCs may gather dice-produced piles by hand. Specialist buildings, tools, skills and infrastructure make it much more efficient (about 4× as an initial tuning assumption, not a fixed rule).
- **Reasons to trade (Q10):** keep the stage 3 variety rule and also test complementary production inputs (logs and clay for shelter). Trade emerges because it benefits individuals, never because the game demands it.
- **Renewable resources (Q11):** forest stock limits are on by default in the sandbox and off by default in the VP mode, with a scenario option to enable them. Renewables regenerate by configurable ecological rules, so overexploitation, sustainable harvesting and resource-management institutions are all possible.
- **Labour disputes (Q19):** a walk-out suspends the job contract first; it does not end it.
- `WareRefValue` and FE are analytics and reporting only, never a hidden currency or a central barter price.

**The state and its institutions**
- The player operates the **institutions of a state** (see [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)) through policies, taxes, investment and incentives.
- Institutions develop gradually through **civilisation ages**, starting from simple equivalents: elders, healers, communal mediation, guards, informal lending.
- **Ages advance by emergent unlocks.** An institution or technology becomes possible when the society can sustain it: population, surplus, knowledge spread by learning and culture, and the institutions that already exist. There is no fixed timeline, and factions can be in different ages.
- **Institutions change the strategic environment** (courts make promises credible, and so on). Institution staff keep their own motives, so management can be poor, corrupt, short-staffed or failing. No structure or ideology is assumed to be best.
- **Goodhart's law is built in.** Institutions respond to targets, and optimising a reported KPI need not improve the real outcome. Nothing rewards a reported KPI directly.
- **Institutions do not all come from the state (Q17).** Communities must be able to organise themselves independently of the state. The state may support, regulate, compete with or replace community institutions.
- **Temporary automatic rules (Q16):** the automatic HQ paymaster and the immigration rules stay in Stage 4 as temporary rules. They respect available resources and state policy, emit events, are replaceable by staffed institutional processes, and never create resources or bypass economic constraints. Every temporary mechanism is listed with its removal milestone (section 10).
- **The state earns loyalty (Q7).** It is never handed loyalty for being the state.
- **Fixed executor ladder in Stage 4 (Q13, Q14),** with configurable rules; estate skimming is not in the first estate milestone. Actual assets are transferred, never duplicated.
- **Conflict (Q18):** no full law of war in Stage 4, but surrender, retreat, prisoners, treatment of the defeated and reciprocal restraint have real consequences.
- **Governance (Q20):** informal household and community decisions in Stage 4; a generic `DecisionRule` for associations in Stage 5; state-level democracy develops as an institution and is not assumed from the start.

**Information and data**
- **Seven information layers** stay separate (section 6).
- **The player:** Stage 4 **builds the separation now**, and the HUD reads through it. The player still gets near-complete reports on the faction's own affairs until statistics institutions arrive in a later age. Enforcing limited information comes later. Restricted NPC observations and read-only policy views are enforced from the start.
- **Foreign information (Q21):** even under `FullInformation` there is no unrestricted access to foreign factions' internal affairs. Existing visibility rules for foreign territory and organisations stay; foreign information comes through observation, trade, diplomacy, migration, communication, media, intelligence and official reporting. The architecture distinguishes what is true from what a faction knows or believes.
- **Debug true-state overlay (Q22):** off in normal competitive play; allowed in developer mode, lab experiments, debugging and dedicated observation scenarios, and optionally in sandbox scenarios. Ordinary faction AI and NPC policies never get debugging information.
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
- **No composite wellbeing score (Q23).** Reports show the underlying measures separately. An experiment may use an explicitly defined composite only with its components, weights and limitations documented. There is no hidden political or moral weighting of which societies succeed.
- **Naming the influences (Q2):** the intellectual influences appear only on an optional About/Philosophy page with references. Gameplay, tutorials, institutional reports and KPIs use neutral, descriptive terms.

**Saves and checkpoints**
- **Stage 3 save compatibility is not needed.** Instead the last stage 3 state is kept as the git tag **`stage3-final`** (at `d7eeb95`, created by the lead). Saves are versioned from Stage 4 on (section 14).

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
- **`double` in rules:** a few rules use `double` today (board topology, lattice, faction rules, parts of the economy, the HQ circle). New code uses integers or fixed point only. **Owner decision (27 Nov):** M0 audits the existing floating point and verifies it on more than one host; anything that fails parity, or whose parity cannot be guaranteed, is converted. Harmless code is not rewritten without reason ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md#11-open-questions)).
- **Saves:** all state is serialisable and saves are versioned. Save version 3 arrives with M3. Stage 3 saves need not load in Stage 4; the git tag `stage3-final` keeps the stage 3 game instead (section 2). `StateHash` covers every module, and from M0 also the **pending command queue**. Adding the queue changes every stage 3 hash once; that is accepted. See [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md#44-hashing).
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
- **What `LifeScale` does not scale (owner decision Q1):** `LifeScale` affects biological life-cycle durations only (ageing, fertility, childhood, lifespan). It does **not** scale ordinary work, movement, learning, the think rate, the decay of social memory or the decay of learned associations. Those decay rates are per sim day and are **independently configurable** on `SimTuning` for experiments ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#47-forgetting), [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#11-open-questions)).
- **Think rate:** an NPC decides every `ThinkEvery` ticks (default 10, about once a second at 1x), staggered by `(tick + id) % ThinkEvery`. Reflexes run every tick. Level of detail may lengthen it for distant NPCs ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md#49-level-of-detail-and-bounded-cost-per-tick)).
- **Default ages at LifeScale 100:**
  - child until **year 1**, about 20 real minutes, an "age of 16";
  - adult until year 3;
  - elder after year 3;
  - natural death around year 3.5–4.5.

  - Fertility from year 1.0, the start of adulthood (reproduction is for adults only; changed from 0.9 on 27 Nov).
  - Pregnancy lasts 0.25 years.

  So one generation (from birth to the birth of a first child) takes about **1.4–1.9 sim years**: adulthood at 1.0, plus courtship, plus a 0.25-year pregnancy. That is roughly 28–38 real minutes at 1x speed, so a 60-minute game sees about 1.5–2 generations. These are estimates to calibrate in M4.
- **Off-island immigration** is **off by default** (decided 27 Nov). Population changes only through births, deaths, and migration between factions. A scenario can switch immigration on as an environmental flow, but it must never react to deaths or vacancies. See [POPULATION_DESIGN.md](POPULATION_DESIGN.md).
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
Stage 4 builds the **foundations** for everything above. Later stages add institutions and depth. Each milestone leaves a working, tested game; acceptance criteria are in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md). The milestones M0–M15 are kept, and the owner (27 Nov) groups them into six **functional checkpoints**, 4A to 4F.

| Checkpoint | # | Milestone | Needs |
|---|---|---|---|
| **4A Simulation laboratory** | M0 | Headless runner, `SimTuning`, `SimCalendar` with LifeScale, `IVictoryCondition` (VP, sandbox), domain event stream plus JSONL recorder, replay and hash checks | - |
| 4A | M1 | Policy seams (all four interfaces; `IFactionPolicy` wraps `AiBrain`), `PolicyRegistry`, `FactionKnowledgeView` with `FullInformation`, the reflection test | M0 |
| 4A | M2 | Game-theory lab, maths only (benchmarks reproduced where the book gives the numbers; see [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#reproduction-status)) | M0 |
| **4B Living population** | M3 | NPC core: body, drives, perception, attention, beliefs, **instincts plus the associative learning core**, foraging, huts, personal items | M1 |
| 4B | M4 | Life cycle and households: birth, childhood, ageing, death, partnership, reproduction, kinship, `Genome` (biological inheritance), basic estates (material inheritance by custom) | M3 |
| **4C Social economy** | M5 | Social memory, gestures, honest barter as separate hand-overs, gifts | M3 |
| 4C | M6 | NPC labour replaces walkers; basic state (treasury, tax, wages, jobs, sites and contracts, purchase offers); ownership records (state, private, communal) | M5 |
| 4C | M7 | Deferred exchange, promises, reputation, lies, misunderstanding, **social learning by observation** | M5 |
| **4D Political economy** | M8 | Enterprise: NPC workshops, NPCs employing NPCs, cooperatives, communal property, theft and witnesses, class *measures* | M6, M7 |
| 4D | M9 | Soldiers are NPCs (enlistment, kit, rations, assignments, morale, desertion, surrender) | M6 |
| 4D | M10 | Citizenship, loyalty, migration, defection; political-interest *measures* | M7, M9 |
| 4D | M11 | Cultural transmission: teaching, imitation of successful others, memes, dialects, traditions between generations | M4, M7 |
| **4E Institutional state** | M12 | Band-age institutions framework: elders and mediation, healer, guards, shared store and welfare, informal lending, through the generic institution model with NPC staff and `IInstitutionPolicy`; simple communal institutions without the state where practical (Q17) | M6, M10 (so also M7, M9) |
| 4E (may be pulled forward) | M13a | Analytical recording core: SQLite warehouse core dimensions plus person, household, economy and citizenship facts, reconciled to events. Needs only M0 and the event types of the milestones already done, so it **may be pulled forward after M6** | M0 (earliest after M6) |
| 4E | M13b | Institution KPIs and reports: the KPI engine and Band-age KPIs (institution KPIs included), reported against actual values, reports through `FactionKnowledgeView`, the HUD reports panel, institution and record facts | M12, M13a |
| **4F Integrated gameplay and network readiness** | M14 | Incentive faction AI, balance (sandbox first, then VP), HUD and tutorial rewrite, play guide | M6–M13b |
| 4F | M15 | Network readiness: encodings, masks, `IntNet` policy, dataset export, Python bridge and verification. **No training** | M1, M14 |

**Bare "M13" in other documents.** Where a document says M13 without a letter, read **M13a** for the recorder, warehouse tables and facts, and **M13b** for KPIs, reports, the reports panel, records, returns and institution facts.

**Dependencies are respected, not guessed.** Two milestones may run in parallel only when each one's needs are met; touching different files is not enough to make parallel work safe.

**Functional releases (the honest "minimum viable Stage 4").** The earlier claim that M0–M6 plus M9 and M13 was a minimum was wrong: M13 needed M12, which needs M10, which needs M7 and M9. The releases are now:

| Release | Milestones | What it is |
|---|---|---|
| **First playable new game** | end of 4C plus M9: **M0–M7 and M9** | a living, learning population; households; a social economy with NPC labour and state incentives; persistent soldiers |
| **Analytical recording** | M0 gives JSONL events from the start; **M13a** (any time after M6) gives the SQLite warehouse core; **M13b** (after M12) gives institution KPIs and reports | |

**Progressive Unity visualisation.** Unity views arrive with the milestone that creates what they show, not all at M14. Each milestone delivers its own inspection or view (the "Manual" line of each milestone in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md#3-milestones)); M14 rewrites the HUD and tutorial as a whole.

| # | Inspection or view it delivers |
|---|---|
| M0 | The player build replays a runner replay (`Unity_Agrees_With_Runner`); a dev-mode run-info readout (seed, tick, hash) |
| M1 | The HUD reads only through `FactionKnowledgeView`; the true-state overlay as a dev-mode toggle (Q22) |
| M2 | Lab reports written by the runner (no Unity view needed) |
| M3 | NPC models and the NPC inspector: drives, attention, beliefs, top learned associations, last decision and why |
| M4 | Life-stage models, household panel, family tree, population pyramid |
| M5 | Gesture animations, relationship tab, barter hand-overs |
| M6 | "Designate a site and post a contract", state and jobs panels, ownership tags |
| M7 | Promises, gossip and reputation in the inspector (as the NPC believes them) |
| M8 | Workshop card with owner and staff; class-measures view (analysis only) |
| M9 | Soldiers as NPCs: kit, rations, morale, service record |
| M10 | Citizenship, loyalty and migration view |
| M11 | Cultural traits census view |
| M12 | Institution panel: staff, budget, records |
| M13a | NPC life history (family, education, employment, ownership, transactions, relationships, estate) from the warehouse, in the runner and in a dev-mode Unity view |
| M13b | HUD reports panel (each KPI with source, as-of date and quality) |
| M14 | Rewritten HUD and tutorial; play guide |
| M15 | None required |

**Temporary mechanisms.** Every temporary compatibility mechanism (a stage 3 rule kept alive, an automatic rule standing in for staff, an adapter, a feature switch) is listed in the "Temporary mechanisms" table in [DEV_LOG.md](DEV_LOG.md#temporary-mechanisms) with why it exists and the milestone that removes it, and it is removed at that milestone. A mechanism not in the table may not be added.

**Workstreams across milestones:**

| Workstream | Milestones |
|---|---|
| Lifetime learning | M3 core, M5 social, M7 observational, M9 military service, M11 cultural, M15 network inputs (the owner's required learning scenarios: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#417-required-lifetime-learning-scenarios-owner-27-nov)) |
| Information layers | M1 → M13b |
| Recording | M0 → M13a → M13b |
| Unity inspection | M0 → M15 (table above) |
| Performance benchmarks | from M0, every milestone ([DEV_LOG.md](DEV_LOG.md#performance-benchmarks)) |

**After stage 4:**

| Stage | Adds |
|---|---|
| Stage 5 (Village age) | Collective bargaining and action, political movements, cooperatives at scale, the headman's court and militia, granaries, schools-lite, money pilot, the generic association `DecisionRule` (Q20), formal shared nurseries if they emerge (Q15), `FoundCommunal` completed if it was not finished in M12 (Q17) |
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
| [DEV_LOG.md](DEV_LOG.md) | The persistent development log of the implementation run: current milestone and checkpoint, completed milestones, commits, test results, performance benchmarks, decisions taken during implementation, **temporary mechanisms**, known limitations, deviations, outstanding work, instructions for the next session |
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

## 13. Resolved owner decisions (27 Nov 2026)
The owner answered all 23 questions that this section used to hold. Each answer is binding and is recorded in the owning document, which replaces its open question with the decision. Where an answer goes beyond the old default, the owning document says so.

| # | Topic | Decision | Owning documents |
|---|---|---|---|
| 1 | `LifeScale` and decay | Keep the default. `LifeScale` affects biological life-cycle durations (ageing, fertility, childhood, lifespan) but **not** ordinary work, movement, learning, social-memory decay or association decay. Those decay rates are **independently configurable** for experiments. | [SOCIAL_MEMORY](SOCIAL_MEMORY_DESIGN.md#47-forgetting), [NPC_LEARNING](NPC_LEARNING_DESIGN.md#11-open-questions), [POPULATION](POPULATION_DESIGN.md#time-and-life-stages) |
| 2 | Naming the influences | Keep the default. The intellectual influences go on an optional About/Philosophy page with appropriate references. Ordinary gameplay, tutorials, institutional reports and KPIs use neutral, descriptive terms. The game explores the theories and does not instruct players to accept them. No predetermined political or economic conclusion. | [VISION](VISION_AND_PHILOSOPHY.md#12-open-questions) |
| 3 | Disease | **On by default.** Setting On, Mild or Off: Sandbox On, Tutorial Mild, controlled experiments configurable. Disease must link nutrition, crowding, sanitation, healthcare, population growth and mortality. No arbitrary disease events that cannot be understood or mitigated through play. | [POPULATION](POPULATION_DESIGN.md#disease-simple-model-in-stage-4-healthcare-later) |
| 4 | Mutation rate | Keep the default of 40 per 10,000 allele copies, configurable in scenario tuning. It is deliberately accelerated. Zero and lower rates are also supported for controlled experiments. Biological inheritance stays distinct from learning and culture. | [BIOLOGICAL_INHERITANCE](BIOLOGICAL_INHERITANCE_DESIGN.md#11-open-questions) |
| 5 | Concurrent partners | Serial partnerships are enough for M4, but the relationship architecture must not permanently restrict individuals to one partner. Concurrent partnerships, different household customs and other arrangements must be possible later. Individuals enter and leave relationships voluntarily. | [HOUSEHOLDS_AND_KINSHIP](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#partnership-and-courtship) |
| 6 | Same-sex partnerships | **Explicitly supported**, with the same social, emotional, household, economic, caregiving and material-inheritance capabilities as other partnerships. Biological reproduction stays constrained by the reproductive model, but adoption, fostering and caring for children are open to all suitable households. No arbitrary differences in value. | [HOUSEHOLDS_AND_KINSHIP](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#partnership-and-courtship) |
| 7 | Loyalty and belonging | Faction loyalty is **not rewarded directly**. Loyalty emerges from experiences, relationships, security, living standards, cultural beliefs and treatment by the state. A configurable `Belonging` drive is acceptable if it gives useful social motivation, but it must not be an artificial reward for obeying a faction. Individuals can learn that staying loyal, migrating, defecting or becoming independent pays under different circumstances. The state must earn loyalty. | [NPC_LEARNING](NPC_LEARNING_DESIGN.md#11-open-questions), [STATE](STATE_DESIGN.md#loyalty-migration-and-defection), [POPULATION](POPULATION_DESIGN.md#drives) |
| 8 | Staffed workshops | Yes, in both modes. All productive work is ultimately done by persistent individuals. No anonymous or auto-generated workers kept to preserve stage 3 balance. Production, staffing and resources are rebalanced around the population system. | [ECONOMY](ECONOMY_DESIGN.md#11-open-questions) |
| 9 | Gathering dice piles by hand | Yes. Specialist buildings, tools, skills and infrastructure make it much more efficient (about 4× as an initial tuning assumption, not a fixed rule). This supports independent subsistence, specialisation and mobility. | [ECONOMY](ECONOMY_DESIGN.md#11-open-questions) |
| 10 | Food variety | Keep the rule, and also test complementary production inputs (such as logs and clay for shelter). Several natural reasons to exchange: different nutritional benefits, local availability, complementary construction materials, productive capabilities, individual needs, and skills and equipment. Trade emerges because it benefits individuals, not because the game demands it. | [ECONOMY](ECONOMY_DESIGN.md#11-open-questions) |
| 11 | Forest depletion | Forest stock limits are **on by default in the sandbox**; non-depleting by default in the VP mode, with a scenario option to enable depletion. Renewable resources regenerate by configurable ecological rules. Support overexploitation, sustainable harvesting and eventual resource-management institutions (tragedy of the commons, Ostrom experiments). | [ECONOMY](ECONOMY_DESIGN.md#11-open-questions) |
| 12 | Care work | **Care work is real work.** Child-rearing, caring for relatives, preparing food, tending the injured and maintaining households cost time and resources and give genuine benefits. Keep the distinction between care work and relationships to productive assets in class calculations, but report unpaid care separately so it is not economically invisible. Reports include time on paid work, independent production, communal work, unpaid care, subsistence and leisure. | [ECONOMIC_CLASSES](ECONOMIC_CLASSES_DESIGN.md#11-open-questions), [KPI_CATALOGUE](KPI_CATALOGUE.md#14-open-questions) |
| 13 | Executor ladder | Keep it fixed in Stage 4. Make the rules configurable, and let the architecture support wills, household decisions, communal inheritance, state inheritance laws and estate tax later. Actual assets are transferred, never duplicated. | [MATERIAL_INHERITANCE](MATERIAL_INHERITANCE_DESIGN.md#11-open-questions) |
| 14 | Estate skimming | Not in the first Stage 4 estate milestone. Preserve the ability to add dishonesty, corruption, disputed inheritance and restitution later, as another case of exploiting an institutional position, with consequences from observation, reputation and enforcement. | [MATERIAL_INHERITANCE](MATERIAL_INHERITANCE_DESIGN.md#11-open-questions) |
| 15 | Shared nurseries | Yes. Informal shared childcare between households is supported in Stage 4; a formal institution can emerge in Stage 5 or later. Individuals can cooperate in raising children, share caregiving and potentially exploit such arrangements. Biological parents are not the only possible caregivers. | [HOUSEHOLDS_AND_KINSHIP](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#shared-childcare-between-households), [GAME_THEORY](GAME_THEORY_DESIGN.md#11-open-questions) |
| 16 | Automatic HQ paymaster and immigration rules | Keep these temporary rules in Stage 4. They must respect available resources and state policy, emit events, be replaceable by staffed institutional processes, and never create resources or bypass economic constraints. Eventually real NPC officials do these jobs, with capacity limits, motives and possible administrative failure. | [STATE](STATE_DESIGN.md#11-open-questions), [POPULATION](POPULATION_DESIGN.md#immigration-and-emigration), [DEV_LOG](DEV_LOG.md#temporary-mechanisms) |
| 17 | Communal institutions without the state | Yes. Communities must be able to organise themselves independently of the state; this is central to the Ostrom aspect. M12 supports simple communal institutions through the existing household, contribution, ownership and association mechanisms where practical. If autonomous founding would complicate M12 significantly, keep the explicit `FoundCommunal` interface and complete it in Stage 5. The architecture must not assume all institutions come from the state. Communities should eventually establish their own shelters and stores, childcare, mutual-aid groups, resource-management organisations, cooperatives, schools and healthcare, and dispute resolution. The state may support, regulate, compete with or replace them. | [INSTITUTIONAL_STATE](INSTITUTIONAL_STATE_DESIGN.md#11-open-questions), [POLITICAL_ECONOMY](POLITICAL_ECONOMY_DESIGN.md#11-open-questions) |
| 18 | Law of war | No full system in Stage 4, but surrender, retreat, prisoners, treatment of the defeated and reciprocal restraint have real consequences. Attacking someone who surrendered affects reputation, trust, morale and future willingness to surrender, where observed or communicated. Later institutions and treaties can establish and enforce formal rules. Individuals and factions do not all behave the same. | [MILITARY](MILITARY_DESIGN.md#11-open-questions) |
| 19 | Walk-outs | **Suspend the contract first.** The employee may resume, negotiate, resign or keep refusing; the employer may negotiate, improve pay, dismiss or seek alternatives. This is the basis for collective bargaining, strikes, lockouts and economic conflict, arising from material circumstances and incentives, not scripted events. | [POLITICAL_ECONOMY](POLITICAL_ECONOMY_DESIGN.md#11-open-questions), [ECONOMY](ECONOMY_DESIGN.md#11-open-questions) |
| 20 | Formal voting | Keep the staged approach: informal household and community decisions in Stage 4; a generic `DecisionRule` for associations in Stage 5, supporting alternative governance later (majority, consensus, appointment, rotation, election and others). State-level democracy develops as an institution, is not assumed from the start, and links to the owner's wider political plans. | [POLITICAL_ECONOMY](POLITICAL_ECONOMY_DESIGN.md#11-open-questions) |
| 21 | Foreign information under `FullInformation` | No unrestricted access to foreign factions' internal affairs. Keep the existing visibility rules for foreign territory and organisations. Foreign information comes through observation, trade, diplomacy, migration, communication, media, intelligence and official reporting. A faction's own treasury and records may be near-complete at first. The architecture distinguishes what is true from what a faction knows or believes. | [INFORMATION_AND_MEDIA](INFORMATION_AND_MEDIA_DESIGN.md#11-open-questions) |
| 22 | Debug true-state overlay | Off in normal competitive play. Allowed in developer mode, lab experiments, debugging and dedicated observation scenarios; sandbox scenarios may optionally enable it. Ordinary faction AI and NPC policies never get debugging information. | [INFORMATION_AND_MEDIA](INFORMATION_AND_MEDIA_DESIGN.md#11-open-questions) |
| 23 | Composite wellbeing | **No universal composite score.** Report the underlying measures separately (health, nutrition, life expectancy, security, comfort, leisure, education, autonomy, material living standards, inequality, social relationships, environmental sustainability). Experiments may use explicitly defined composites with documented components, weights and limitations. No hidden political or moral weighting of which societies succeed. | [KPI_CATALOGUE](KPI_CATALOGUE.md#14-open-questions), [POLICY_EXPERIMENTS](POLICY_EXPERIMENTS_DESIGN.md#10-open-questions) |

No open owner question remains. The owner has said not to ask questions that have reasonable defaults ([STAGE_4_HANDOVER.md](../STAGE_4_HANDOVER.md#7-owner-questions-resolved)); a choice made during implementation is recorded in [DEV_LOG.md](DEV_LOG.md#design-decisions-taken-during-implementation) and, if it changes a design, in the owning document.

## 14. Further owner requirements (27 Nov 2026)
The owner added these requirements with the answers above. Most reinforce the existing design; where a document already holds a requirement, the link is given instead of repeating it.

**Vision and inheritance**
- Keep the full intellectual vision: it must not shrink to an RTS with NPC stats, and all layers interact ([VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md)). The four inheritances stay distinct (section 2).

**Lifetime learning**
- *Creatures*-style lifetime learning is fundamental. It is never mere telemetry, a skill counter or machine-learning scaffolding. Eight owner examples must work, and each has an acceptance test: hungry near food; hungry with no food (producing goods enables a later food exchange); tired near shelter; gathering wood with an axe; helping someone (future reciprocity); lending to an unreliable person (loss); working for a reliable state (paid); joining a poorly managed military (injury, hunger, unpaid wages). See [NPC_LEARNING_DESIGN.md §4.17](NPC_LEARNING_DESIGN.md#417-required-lifetime-learning-scenarios-owner-27-nov).
- Learning stays active after networks are introduced: a shared trained network gives general competence, and each individual's associations give personal experience ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md); acceptance M15.7).

**Communication**
- Communication is functional: a promise is not a completed action, an offer is not a completed transaction, and a claim is not necessarily true. It works identically headless ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md), [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).

**Game theory**
- Where the book lacks the numbers to reproduce an experiment exactly, the experiment says so, and an experiment with invented payoffs is never called an exact reproduction ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#reproduction-status)). If the island differs from the maths, investigate why ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)).

**Economy**
- The economic progression the design must make possible, never force: subsistence → cooperation → barter → specialisation → surplus → productive ownership → employment → accumulation → classes → political organisation ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md), [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md), [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)). No permanent class labels; no political beliefs assigned from class. `WareRefValue` and FE are analytics and reporting only, never a hidden currency or a central barter price.

**Institutions**
- The player controls budgets, policies, resources, priorities, recruitment incentives and objectives, and eventually may operate individual institutions directly while leaving others under AI management. Healthcare and education should eventually affect the biological and cognitive simulation itself, never give abstract bonuses ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).

**Information and warehouse** ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md), [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md))
- Restricted NPC observations and read-only policy views are enforced from the start (sections 6 and 7).
- The warehouse keeps explicit fact grains, persistent identities, history for ownership, employment, citizenship and household membership, effective-dated policy records, half-open time boundaries, reconciliation from events to KPIs, versioned KPI definitions and configurable recording levels.
- **It must be possible to inspect an NPC's entire life:** family, education, employment, ownership, transactions, relationships, estate (the M13a view in section 10).
- Institutional returns stay distinct from independently calculated KPIs and from true outcomes (section 8).

**Determinism** ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md))
- Same version, configuration, seed, initial state and commands give identical results. New code uses integers or fixed point. Existing floating point is audited and verified on more than one host; anything that fails parity or cannot be guaranteed is converted; harmless code is not rewritten without reason (section 3).
- Required: versioned saves; serialisable random streams; stable processing order; deterministic tie-breaking; pending commands in the hash; persistent learning state; save/load continuation parity; replay verification; independent parallel instances; no mutable global tuning; identical results at any telemetry level; headless and Unity parity.
- Stage 3 save compatibility is **not** needed. The git tag `stage3-final` keeps a stable stage 3 checkpoint instead of complicating Stage 4.

**Population** ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#owner-population-requirements-27-nov))
- `SimCalendar` and `LifeScale` are kept.
- Configurable: starting population, faction count, founder age distribution, biological parameters, disease and resources.
- Defaults: **six factions**; about **20 individuals per faction** as the first scenario, with larger and smaller populations also tested; varied household configurations and age distributions.
- Reproduction is for adults only.
- Growth, decline and extinction must emerge; workers, soldiers and citizens are never auto-replenished.
- Demographic stability is tested across generations.
- Sandbox primary, VP optional.
- Genuine collapse, economic failure or extinction is not tuned away. Meaningful outcomes are distinguished from defective mechanics or bad baseline parameters.

**Roadmap** (section 10)
- Milestones M0–M15 are kept and grouped into checkpoints 4A–4F. Dependencies are respected rather than assuming parallel work is safe because files differ. Unity visualisation and inspection arrive progressively, not all at M14. Every temporary compatibility mechanism is documented and removed at the right milestone.

**Testing** ([ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md#43-owner-evidence-list-27-nov))
- Keep the acceptance-test coverage; it is never reduced. Evidence is wanted for: genuine individual learning; contextual and delayed reinforcement; different behaviour from different experience; multigenerational population stability; genealogical integrity; resource conservation; population conservation; estate and ownership conservation; knowledge isolation; meaningful non-verbal communication; cooperation, exploitation and retaliation; emergent specialisation; different ownership and employment relationships; deterministic replay; save/load continuation parity; parallel independence; headless and Unity parity; telemetry independence; KPI reconciliation.
- Distinguish deterministic unit tests, controlled model tests and statistical island experiments. Never weaken criteria to pass; if an expectation proves wrong, document the evidence and amend it transparently.

**Performance** ([HEADLESS_SIMULATION.md §4.10](HEADLESS_SIMULATION.md#410-performance-targets-and-profiling), [DEV_LOG.md](DEV_LOG.md#performance-benchmarks))
- Benchmark early. Measure: ticks/s; sim years per real second; NPC decisions/s; memory per NPC; pathfinding cost; learning cost; communication cost; telemetry and database overhead; save size and load time; parallel throughput.
- Keep per-NPC memory bounded with deterministic eviction. Avoid all-to-all comparisons; use spatial indexing, staggered decisions and bounded candidate sets.
- Level of detail is decided by sim state and scenario, never by camera or rendering. Document when an optimisation changes behaviour. Never sacrifice learning, social or economic capability for premature optimisation.

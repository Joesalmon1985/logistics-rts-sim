# Institutional state design (long-term framework)

The framework for every institution the player operates, from a Band age healer to a City age central bank. Stage 4 builds the generic model and the Band-age institutions (M12); later stages add the rest. The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); that file wins where this one disagrees.

## 1. Purpose and scope

**This document owns:**
- the generic institution model: resources, facilities, staff, leadership, policies, budgets, objectives and targets, constraints;
- the catalogue of institutions by age, and the rules that unlock them ([Ages and unlocks](#ages-and-unlocks));
- provision models: public, private, cooperative and communal;
- how each institution changes the strategic environment;
- failure modes, and Goodhart's law with how managers choose effort;
- `IInstitutionPolicy` and `InstitutionView`;
- the Stage 4 foundations (M12) versus later stages;
- institution telemetry.

**It links to, and does not repeat:**
- the treasury, taxes, contracts, laws v1 and the state command set: [STATE_DESIGN.md](STATE_DESIGN.md);
- the military institution's combat side: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- records, data collection, media and the knowledge view: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- each KPI's definition and its actual vs reported pair: [KPI_CATALOGUE.md](KPI_CATALOGUE.md);
- ownership forms, commons and enterprises: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- cultural traits used by unlock rules: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- experiment design for comparing institutions: [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md);
- how institutions change each game in the book: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md).

## 2. Principles
- **An institution is people, places, goods and records.** It has no power that its staff do not exercise. A court with no judge decides nothing; a guard post with no guard deters nothing.
- **Staff are real, persistent NPCs.** They are recruited and kept through incentives (contracts, pay, status, conditions). They are never spawned. An NPC who leaves an institution keeps its life, memory and skills.
- **Staff and managers keep their own motives.** The same decider that chooses a civilian's work chooses how hard a guard watches, whether a keeper favours kin, and what a manager reports.
- **Institutions change payoffs, never outcomes.** A court raises the expected cost of breaking a promise; it does not make anyone keep one. Every claim of effect is a hypothesis to measure.
- **Nothing rewards a reported KPI.** No sim rule pays, scores or reinforces anyone for a number in a report. Only explicit incentive terms that the player or the faction AI writes into a manager's or staff member's contract can link pay to a measure, and those are paid in real goods. That is how Goodhart's law enters, and the player chooses whether to risk it.
- **Two kinds of gap, kept apart.**
  - The **target gap**: a measure is recorded honestly, improves, and the outcome it stands for does not (cherry-picking easy cases improves the true mean wait while severe cases die).
  - The **report gap**: the manager's own return differs from what happened (falsification, unrecorded complaints).
  - Both are measured; the first exists even with perfect records.
- **No structure or ideology is assumed superior.** Public, private, cooperative and communal provision use the same model. Each has its own failure modes. Comparisons are experiments.
- **Same information as a human at that level.** An institution's policy reads only its `InstitutionView`; a faction's reads only `FactionKnowledgeView`.
- **Determinism.** Institutions update in ascending `InstitutionId`; cases in ascending case id; staff in ascending `NpcId`. Random draws use `Rng.Fork("inst", id)`.

## 3. Data structures (`Sim/Institutions`)

```text
Institution   (InstitutionId, unique for the game, never reused)
  Id, Type: InstitutionType, Age: Band | Village | Town | City
  Provision: Public | Private | Cooperative | Communal
  Owner: faction id | NpcId | group id (communal or cooperative members)
  Facilities: int[<= 4] BuildingIds          // e.g. a healer's hut, a guard post
  Store: per-ware counts held in its facilities (physical, can be stolen)
  Budget: BudgetLine[<= 8]                  // per-day allowance per purpose, in goods
  Staff: StaffPost[<= 32]
  ManagerNpcId (0 = none; the institution then runs on standing rules only)
  Policies: short[<= 16]                    // type-specific parameters (case order, eligibility, ration size)
  Directives: Directive[<= 8]               // from the owner: objectives, targets, incentive terms
  Constraints: InstitutionConstraints       // capacity, powers, required skills, jurisdiction area
  Cases: ring of Case[<= 256]               // open and recent cases
  Records: ring of RecordEntry[<= 512]      // what it wrote down (layer 4)
  Status: Founding | Operating | Unstaffed | Insolvent | Dissolved
  Founded (tick)

StaffPost
  NpcId, Role (type-specific: keeper, guard, healer, mediator, collector, inspector)
  ContractId                                 // a STATE_DESIGN contract (Kind = Staff or Service), or a private agreement
  Since (tick), Skill snapshot
  Effort: byte[<= 6]                         // share of its time per task, permille/4, chosen by the NPC each day

Directive
  Kind: Objective | Target | IncentiveTerm | Restriction
  Kpi: KpiId (from KPI_CATALOGUE), Threshold, Period (days)
  Incentive: Bundle bonus if the manager's *return* for the KPI meets the threshold, or Penalty (dismissal warning)
  // Objectives without incentives are just signals of what the owner wants.

Case
  Id, Kind, Subjects: NpcId[<= 3], Opened (tick)
  TrueDifficulty, TrueNeed (hidden: only the sim and telemetry know them)
  SeenDifficulty, SeenNeed                   // staff estimates from observation (noisy)
  Status: Waiting | Active | Closed | Dropped | Refused
  TrueOutcome, RecordedOutcome               // may differ: error, fraud
  HandledBy NpcId

RecordEntry
  Tick, Kind, CaseId or NpcId, Value, AuthorNpcId, Falsified (hidden flag, telemetry only)

InstitutionView        // what IInstitutionPolicy reads
  its own Budget, Store, Staff (ids, roles, skill, effort, contract terms), Cases as recorded,
  Records, Directives, Constraints, the KPIs computed from its own records (what it could truthfully return),
  the manager NPC's own mind view (beliefs, memory, traits) when the policy decides as the manager
```

**Bounded sizes** keep the daily cost predictable: at most 64 institutions per faction in stage 4, 32 staff each, 256 open cases each.

## 4. Mechanics

### The generic institution model
Each institution type supplies four things; the framework does the rest:
1. **Tasks.** A short list of what staff can spend effort on (watch, patrol, handle a case, record, keep the store, teach). Each task has a production rule: how effort and skill turn into results.
2. **Cases.** What arrives to be handled (a patient, a dispute, a theft report, a claim, a loan request), with true and seen difficulty and need.
3. **Powers.** What its staff may do that others may not (levy a fine, hand out from the store, hold a prisoner), backed by a law or an owner's rule.
4. **KPIs.** Which reported measures it produces from its records, each paired with an actual measure computed by telemetry from true events (KPI_CATALOGUE).

**Daily cycle** (first tick of each day, ascending id):
1. Pay staff from the budget (a physical hand-over at the facility or the pay point; a shortfall is a default, as in STATE_DESIGN).
2. Each staff NPC chooses its effort split for the day (see "How staff and managers choose effort").
3. The manager's `IInstitutionPolicy` runs: hiring, dismissals, case rules, purchases within budget, the day's report.
4. The manager files the day's **return** for each KPI with a directive (`ReturnFiled` event). The return is the manager's choice and may differ from the records. Incentive terms are settled on returns.
5. Separately, the records feed the faction view through the measurement pipeline of [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) (`KpiReported`). `KpiReported` is the faction's measured value; under `FullInformation` it equals the actual value. In later ages it is computed from records and returns, so it can drift from the truth.

**During the day,** staff act through their own `INpcPolicy` with the institution's tasks as candidate intents. An institution's effect happens only through these intents.

### Workforce and leadership
- **Recruiting.** A post is a contract (STATE_DESIGN) or, for private and communal provision, an agreement recorded by the owner. NPCs take posts by choice. Posts that pay too little stay empty.
- **Keeping staff.** Staff re-decide every day whether to stay, against their outside options (other jobs, foraging, migration). Unpaid staff, overloaded staff and staff whose manager they distrust leave more often.
- **Skills.** Staff skill grows with practice (POPULATION skills). Losing experienced staff costs capability, which is measurable.
- **The manager** is an NPC appointed by the owner (`AppointManager`) or, for cooperatives and communes, chosen by members' preference (each member names its most trusted eligible member; most names wins, ties by lower id). The manager has its own traits, memory, kin and pay.
- **No manager** means standing rules only: staff follow the last policies, nobody hires, and reports are not written.

### Budgets and resources
- **Public** institutions get budget lines from the treasury (`SetInstitutionBudget`), committed per day at a named HQ and hauled to the facility by contract.
- **Private** institutions fund themselves from fees in goods; **communal** ones from member contributions; **cooperatives** from members' shares of output.
- **Spending** is limited to the budget and the store. An institution that cannot pay staff becomes `Insolvent`; it keeps operating with whoever stays.
- **Goods in a facility can be stolen**, including by its own staff (embezzlement).

### Policies, objectives, targets and constraints
- **Policies** are the institution's operating rules (case order, eligibility, ration size, patrol route). Set by the manager within the owner's restrictions.
- **Objectives** are what the owner says it wants. Without an incentive they are signals: a manager with matching intrinsic motives may follow them.
- **Targets** are an objective with a threshold on the manager's return for a KPI.
- **Incentive terms** attach a bonus or a penalty to a target. These are the only links between a report and anyone's reward, and the owner writes them.
- **Constraints**: capacity (cases per staff per day), powers granted by law, required skills, the area it covers.

### Oversight
The owner has three tools, all of which cost goods or staff:
- **Balanced measures.** Several KPIs per institution, including at least one measure of quality or of what the target leaves out (for a healer: deaths after discharge as well as waiting time).
- **Inspection.** `OrderInspection(institution, sample)` sends an inspector NPC (a staff member of the faction's own oversight post, or a borrowed elder in the Band age) to re-examine a sample of closed cases: it sees the subjects and compares what it observes with the records. Detection depends on the inspector's skill, effort and honesty.
- **Investigation of complaints.** Citizens who believe they were badly treated can complain (an `Accuse` gesture at the HQ). Complaints are records too: an institution can discourage them, which is itself a failure mode.
- **Sanctions:** dismissal, a fine under a law, a budget cut, or public notice (later, through media).

### How staff and managers choose effort (Goodhart in the mechanics)
Each staff NPC, and the manager, splits its effort across the institution's tasks once per day by greedy integer allocation of 10 effort quanta.

```text
For each quantum q = 1..10, give it to the task t with the highest marginal value:
  value(t) = Wpay   * d(bonus expected from returned KPIs) / d(effort_t)     // only if an incentive term exists
           + Wkeep  * d(chance of keeping the post)       / d(effort_t)     // from its belief about what the owner watches
           + Wmot   * d(true outcome it perceives)        / d(effort_t)     // intrinsic: conscientiousness, generosity, kin weight
           - Wcost  * marginal effort cost (fatigue, boredom, danger)
Ties: lowest task index.
```
- **What it believes the owner watches** is a belief built from experience: which reports were praised or punished, which inspections happened.
- **Intrinsic motivation** comes from personality (Industry, Generosity, Honesty, Kin weight) and from the outcomes it can see for itself (a patient it treated recovered).
- **Falsification** is a separate choice: report `actual + m` when `bonus gain(m) - guilt(honesty) - P(caught) × penalty > 0`, where `P(caught)` is its belief about inspection.
- **Case selection.** Where the institution's policy allows it, the handler chooses which waiting case to take next. A handler optimising a waiting-time measure prefers cases with low `SeenDifficulty`.
- **This is the multitask problem.** When a measure covers only some of the tasks, effort moves towards the measured ones. Nothing in the rules makes this happen except the NPCs' own choices over their incentives.

### Goodhart's law: worked examples
Each example gives the target, how it can be gamed, the real outcome it can hurt, and the measures and oversight that limit it. All need an incentive term written by the owner; with none, the gaming motive is absent.

| Institution and target | How a manager can meet it | What it can hurt (actual) | Balancing measure and oversight |
|---|---|---|---|
| Healer: mean wait under 1 day | Take easy cases first; turn away or "close" hard cases early; record arrival late | Deaths and lasting injury among severe cases; hard cases waiting longest | Deaths within 5 days of discharge; share of severe cases treated; inspection of closed cases |
| Guards: recorded thefts down 30% | Discourage reports (turn complainants away, record as "lost"), patrol where reports come from, not where theft is | True theft unchanged or higher; trust in guards falls; victims stop reporting | Victim survey by an inspector; true-crime telemetry in experiments; complaints count |
| Workshop or quarry: output up | Cut quality (item quality 0–3) and safety (skip rests, so injuries rise) | Tools break sooner; injuries; staff quit | Quality share; injury rate; staff turnover |
| Elders' teaching: share of learners "passed" | Lower the bar for passing (the teacher sets the threshold); teach to the check | Skill actually transmitted is lower | A second, independent check by another elder; skill measured later on the job |

- **Measuring better can look worse.** When guards start recording honestly, recorded crime rises. See [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md).
- **Every KPI in [KPI_CATALOGUE.md](KPI_CATALOGUE.md) lists its gaming risks** and its actual partner, so these tables stay in one place there.

### How each institution changes the strategic environment
Each row names the payoff it changes, the mechanic, and what to measure. None of them guarantees the change in behaviour.

| Institution | Changes | Mechanic | Measure |
|---|---|---|---|
| Courts and mediation | Promises and contracts become credible | A promise or contract registered with the court gives the wronged side a case; a decision orders compensation, enforced by guards. The expected cost of breaking it becomes `P(case) × P(ruling) × award` | Promise-keeping rate for registered vs unregistered deals; deferred-exchange volume |
| Police and guards | Higher expected cost of aggression and theft | Watching raises detection; apprehension and fines; presence is visible, so beliefs about detection rise | Theft and assault per 100 adults per day; believed detection chance |
| Healthcare and healers | Survival and capability | Treatment speeds recovery from injury and lowers death from wounds and illness | Deaths from wounds; days of work lost to injury |
| Education and elders | Faster knowledge and cultural transmission | Teaching sessions multiply skill growth and the rate of cultural transmission (CULTURAL_TRANSMISSION) | Skill growth per year of age; trait spread speed |
| Banks and informal lending | Investment, lending, capital and debt | Recorded loans of goods with repayment terms; a lender can fund a workshop it does not run; default is a recorded event, enforced by courts later | Loans outstanding; workshop foundings financed by loans; default rate |
| Media | Information beyond personal contact | Publications reach NPCs who never met the source (layer 5), with an owner, a reach and a credibility | Belief accuracy about distant facts; rumour spread speed |
| Welfare and the shared store | Lower vulnerability | A floor on food; pooling of lucky and unlucky days (the vampire bat logic, SELFISH_GENE_NOTES E10) | Starvation deaths; variance of hunger; theft driven by hunger |
| Housing and infrastructure | Transport and shelter costs | Roads, bridges and huts lower walking and cold costs | Mean haul time; nights outdoors |
| Environmental management | Incentives on the commons | Harvest rules on wild food and forest, monitored by staff, with graduated sanctions and local rule-making (Ostrom's design principles) | Stock of wild sources over years; overharvest events |
| Statistics and research | Quality of the owner's information | Censuses, surveys and studies fill the faction view with measured, dated, possibly wrong figures | Gap between reported and actual figures |
| Government and treasury | Capacity to fund the rest | Collecting, storing, budgeting | Treasury flows; budget execution |
| Prisons and rehabilitation | Incapacitation and re-offending | Holding offenders; work and teaching inside | Re-offending within a year |
| Military | Defence and conquest | See [MILITARY_DESIGN.md](MILITARY_DESIGN.md) | Losses, captures, desertion |

### Ages and unlocks
- **An institution type becomes possible** when the faction can sustain it. "Possible" means it can be founded; it does not appear by itself.
- **Each unlock rule** is a conjunction over measured society, evaluated daily from snapshots (true state, since this is a rule of the game, not a policy):

```text
UnlockRule
  Type, Age
  MinAdults                                  // citizens of the faction
  MinFoodSurplusPermille, OverDays           // (food produced - eaten) / eaten, rolling
  Traits: (CulturalTraitId, MinShareOfAdults)[<= 3]
  Skills: (SkillId, MinLevel, MinCount)[<= 2]
  Requires: (InstitutionType, MinDaysOperating)[<= 3]
  HoldDays                                   // conditions must hold this long before unlocking
  LapseDays                                  // after failing this long, new foundings stop (existing ones continue)
```

- **A faction's age** is the highest age in which it operates at least `AgeCoreCount` (default 3) institution types of that age. Factions in the same game can be in different ages.
- **Example Band rules (defaults, tunable):**
  - Healer: 6 adults; one adult with healing skill at least 30 (skill grows by tending the injured).
  - Elders and mediation: 8 adults; at least 2 elders (life stage); at least 30% of adults hold the "consult elders" trait.
  - Guards: 10 adults; food surplus at least 50 permille over 5 days.
  - Shared store and welfare: 8 adults; food surplus at least 100 permille over 5 days; at least 40% of adults hold the "store food" trait.
  - Informal lending: always possible between kin and friends (it is a practice); the recorded lending circle needs elders and mediation operating for 10 days.
- **Example Village rule:** granary: 25 adults, surplus at least 200 permille over 10 days, the shared store operating for 20 days.
- **Until CULTURAL_TRANSMISSION (M11) supplies traits,** trait clauses are skipped in M12 test scenarios and marked so in the scenario file.

### Catalogue by age

| Area | Band (stage 4 foundations) | Village (stage 5) | Town (stage 6) | City (stage 7) |
|---|---|---|---|---|
| Government and treasury | HQ store, the faction's own commands | Headman and council, tally keeper | Treasury office, tax collectors | Ministries, budget office |
| Healthcare | Healer | Healers' house | Hospital | Health system, public health |
| Education | Elders teaching by example | Apprenticeship, schools-lite | Schools | Universities |
| Military | War band from volunteers | Militia | Army | Standing forces, staff |
| Police | Guards | Watch | Police | Police with investigators |
| Courts and judicial | Communal mediation by elders | Headman's court | Courts | Courts with appeals |
| Prisons and rehabilitation | none (exile or fines) | Stocks, holding hut | Prison | Rehabilitation services |
| Banks and finance | Informal lending between kin and friends | Lending circle, grain loans | Money and banks | Central bank, regulators |
| Media and information | Word of mouth (no institution) | Criers, notice keepers | Printed notices | Media institutions |
| Housing and infrastructure | Shared huts, paths | Public works crews | Builders' guilds, road authority | Planning authority |
| Social services and welfare | Shared store | Granary and welfare | Poor relief | Social services |
| Environmental management | Customary harvest rules | Commons wardens | Forest and fishery offices | Environmental agency |
| Statistics, research, analytics | none (the view is FullInformation in stage 4) | Head counts | Census office | Statistics office, research |

### Provision models

| Model | Owner and control | Funding | Typical fit | Its own failure risks |
|---|---|---|---|---|
| Public | Faction; manager appointed by the owner | Treasury budget | Guards, courts, welfare, statistics | Principal–agent problems, targets gamed, budget politics |
| Private | One NPC | Fees in goods | Healers, lending, workshops, later private guards | Excludes those who cannot pay; protection that turns into extortion |
| Cooperative | Worker-members | Shares of output | Workshops, healers' houses, lending circles | Hard to raise capital; insiders against newcomers |
| Communal | A group (household, kin, village) | Member contributions | Shared store, commons rules, mediation | Free-riding; exclusion of outsiders; capture by strong members |

- **Several can coexist.** A private healer and a public healer can serve the same village; NPCs choose.
- **None is the default winner.** Experiments compare them across seeds and generations (POLICY_EXPERIMENTS_DESIGN).

### Failure modes
| Failure | How it arises in the mechanics | How it shows |
|---|---|---|
| Poor management | A manager with low skill or low industry; bad case rules | Low throughput; long queues; staff quit |
| Corruption | Staff and managers keep their motives: embezzle from the store, take a bribe to drop a case, favour kin (kin weight), sell posts | Store losses; cases dropped for some subjects; gap between reports and inspections |
| Conflicting incentives | Two directives that pull effort apart (cut waiting time *and* treat every severe case) | Effort follows whichever has the incentive term |
| Staff shortages | Pay below outside options; danger; a distrusted manager | Unfilled posts; insolvency; `Unstaffed` status |
| Capture | Fees or gifts from a group buy the institution's attention (guards who patrol the rich quarter; a mediator who rules for its kin) | Outcomes that differ by group; measured by breakdowns in KPI_CATALOGUE |
| Gaming of measures | See "Goodhart's law: worked examples" | Reported vs actual gap |

### `IInstitutionPolicy`
```csharp
public interface IInstitutionPolicy
{
    string Name { get; }
    // view: only this institution's own budget, staff, records, directives and its manager's mind.
    // Appends at most MaxCommandsPerDay institution commands; each is validated like any command.
    // Same pattern as IFactionPolicy (DESIGN_CONVENTIONS section 7).
    void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands);
}
```
- **Commands:** `OfferPost(role, terms)`, `Dismiss(npc)`, `SetCaseRule(rule)`, `SetTaskWeights(role, weights)` (a request to staff, who still choose their own effort), `Buy(ware, count, price)` within budget, `RequestBudget(line, amount)` (a message to the owner), `FileReturn(kpi, value)` (the manager chooses the value; it may differ from the records), `InspectInternally(sample)`.
- **Scripted first:** `ManagerUtilityPolicy` decides as the manager NPC would, using the effort model above and the manager's traits and beliefs. A benchmark `RuleFollowerPolicy` reports honestly and follows directives exactly, for controls.
- **Networks later:** an `IntNet` policy with the same view and commands (AI_TRAINING_DESIGN).
- **A human could play an institution** in a later mode with the same view and commands.
- **Enforced:** the reflection test (`Policies_DoNotReferenceGodState`) covers this namespace.

## 5. Interactions with other systems
- **State:** budgets come from the treasury; posts are contracts; powers come from laws ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **Population and cognition:** staff are NPCs with drives and traits; tasks are candidate intents ([POPULATION_DESIGN.md](POPULATION_DESIGN.md), [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Learning:** staff learn which effort pays; citizens learn whether the institution helps ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
- **Social memory:** trust in an institution and in each staff member; complaints and accusations ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **Information:** records are layer 4; reports enter the faction view (layer 7) ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **Culture:** unlock traits; elders as teachers ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)).
- **Military:** the war band and militia use this model for management ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Political economy:** who controls institutions and whom they serve ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)).

## 6. Player and view
- **Institutions panel:** one card per institution: type, provision, manager (click to open the NPC inspector), staff with posts and pay, budget lines and spend, open cases, directives, and the reported KPIs over time.
- **Return vs view.** Each KPI with a directive shows two lines: the manager's return (what incentives are paid on) and the faction view's figure. Under `FullInformation` (stage 4) the view's figure equals the actual value, as [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) requires, so the report gap is visible to the player in stage 4. When later ages limit information, the view's figure comes from records and returns (checked by inspections), and the report gap is hidden unless measured.
- **The target gap is visible only if the player watches a balancing measure.** The panel suggests the balancing KPIs listed in [KPI_CATALOGUE.md](KPI_CATALOGUE.md) for each target.
- **After an inspection,** the inspected figure is shown with its sample size and date.
- **Unlocks:** a list of institution types the faction can found now, and for each locked one which conditions are unmet ("adults 7 of 10").
- **Commands:** found, appoint, set budget, set directive (with a warning when a target has an incentive term and no balancing measure), order inspection.
- **On the map:** facilities, staff on duty (a role badge), cases being handled (a healer tending a patient), all animated from events.

## 7. Telemetry
**Domain events:** `InstitutionFounded`, `InstitutionDissolved`, `InstitutionStatusChanged`, `UnlockGained`, `UnlockLapsed`, `PostOffered`, `StaffHired`, `StaffLeft` (reason: quit, dismissed, died, migrated), `ManagerAppointed`, `DirectiveSet`, `BudgetAllocated`, `BudgetSpent`, `CaseOpened`, `CaseClosed` (true outcome and recorded outcome), `CaseDropped`, `CaseRefused`, `RecordWritten`, `RecordFalsified` (telemetry only: the sim knows, the player does not), `ReturnFiled` (manager's return, with the record-based value and, in telemetry only, the actual), `IncentivePaid`, `Embezzled`, `BribeOffered`, `BribeTaken`, `InspectionDone` (sample, findings), `ComplaintMade`.

**Daily institution snapshot:** id, type, provision, status, manager, staff count by role, posts unfilled, mean staff tenure and skill, budget by line and spend, store per ware, cases waiting, opened, closed, dropped, mean true and recorded waiting time, true and recorded outcomes, each KPI's reported and actual value, effort shares by task, incentives paid.

**The actual vs reported pair** for every KPI is emitted so the warehouse can measure Goodhart gaps (DATA_WAREHOUSE_DESIGN). Recording never changes outcomes.

## 8. Stage 4 minimum vs later stages

| Milestone | Delivers |
|---|---|
| M1 | `IInstitutionPolicy` and `InstitutionView` types in `Sim/Policy`; the reflection test covers them |
| M6 | Posts as state contracts (STATE_DESIGN); the granary keeper as a plain shift |
| M12 | **The generic model** (institution record, staff posts, manager, budget, directives, cases, records, the daily cycle, effort allocation, falsification choice, inspection); unlock rules and the faction age; **Band-age institutions:** elders and mediation, healer, guards, shared store and welfare, informal lending (recorded lending circle); public and communal provision; `ManagerUtilityPolicy` and `RuleFollowerPolicy` |
| M13 | Institution snapshots and Band-age KPIs in the warehouse; the HUD institutions panel and reports |
| M14 | The faction AI founds, staffs and funds institutions through the same commands |
| Stage 5 | Village institutions (headman's court, militia, granary, schools-lite, lending circles); private and cooperative provision at scale; collectors |
| Stage 6 | Town institutions (courts, police, prisons, hospitals, schools, banks); the Goodhart-prone target sets for managers; the policy-experiment suite |
| Stage 7 | City institutions (media, statistics, central bank, regulators, environmental agency, research); limited information enforced |

**Band-age mechanics in M12:**
- **Elders and mediation:** a dispute (two NPCs with an `Accuse` between them) can be brought to an elder; the elder hears both (sees their gestures and goods) and recommends a settlement. Compliance is voluntary; refusing it costs reputation with those who saw.
- **Healer:** injured NPCs come or are carried; the healer's effort and skill speed recovery; the case queue is ordered by the healer's own rule.
- **Guards:** watch a post or walk a route; act on crimes they see or are told of; fines under STATE_DESIGN laws.
- **Shared store and welfare:** the granary and handouts of STATE_DESIGN, now with a keeper and manager who keep their motives.
- **Informal lending:** loans of goods between kin and friends are ordinary deferred exchange (SOCIAL_MEMORY); the lending circle adds a record kept by an elder, so a default becomes known beyond the two parties.

## 9. Migration from the existing code
There is no institution code today. Pieces that move in:
- **Defend flags** become guard assignments (MILITARY_DESIGN) in M9, and the law-enforcement side becomes the guards institution in M12.
- **The granary keeper** shift from M6 becomes a staff post of the shared store institution; the M6 scenario tests must still pass with `RuleFollowerPolicy`.
- **`AiBrain`** gains an institution step in M14 only.

## 10. Tests and acceptance criteria
"Significant" means a two-sided Mann–Whitney test at p < 0.05 over the stated seeds unless stated. Effect sizes are reported, never tuned to a target.

**Model invariants (every build):**
- **I-INV-1 Never spawned:** for every `StaffHired`, the NPC had an `NpcBorn` or immigration event earlier, and is alive and adult. 20 seeds × 20 days, zero violations.
- **I-INV-2 No staff, no effect:** an institution with zero staff produces zero case closures, handouts, fines or teaching sessions over 10 days, exactly.
- **I-INV-3 No KPI reward:** with no incentive terms, changing a manager's return (forced falsification in a test hook) changes no state outside the institution's own records and its `ReturnFiled` events (hash of every other module equal over 10 days).
- **I-INV-4 Budget:** spending never exceeds budget plus store; staff pay shortfalls emit defaults.
- **I-POL-1 Reflection:** no type in `Sim/Policy` or `Sim/Institutions/Policies` references `GodState`.
- **I-DET-1:** replay hashes match with recording on and off.

**Effects on the environment (M12 scenarios):**
- **I-GRD-1 Guards:** a guard institution with 3 staffed guards lowers measured (true) theft per 100 adults over days 10–30 by a significant margin vs no institution, over 20 seeds.
- **I-GRD-2 Unstaffed guards:** a guard institution whose posts pay zero has no staff after 5 days on 95% of 20 seeds, and theft does not differ significantly from no institution.
- **I-HEAL-1 Healer:** a staffed healer lowers deaths from wounds over 20 days by a significant margin vs none, in a scenario with raids, over 20 seeds.
- **I-MED-1 Mediation:** with elders and mediation operating, the share of `Accuse` events followed by a fight within 2 days is lower than without, over 20 seeds (reported with effect size).
- **I-WEL-1 Shared store:** the variance of daily hunger across adults is lower with a staffed shared store than without, over 20 seeds.
- **I-LEND-1 Lending circle:** the recorded circle makes a default known to at least 3 times as many NPCs within 5 days as an unrecorded loan, over 20 seeds.

**Goodhart (M12 scenarios with forced traits for controlled tests):**
- **I-GH-1 Cherry-picking:** a healer with a waiting-time target, a bonus incentive and a manager with low intrinsic motivation (Industry and Generosity at −60) shows a target gap: its mean wait over all cases improves by at least 30% against the no-target control, while the mean wait of severe cases and severe-case deaths within 5 days are not significantly better than the control, over 20 seeds. Records are honest here (falsification disabled), so the gap is from case selection alone.
- **I-GH-2 Balanced measures:** adding a severe-case deaths measure with equal incentive weight shrinks the target gap of I-GH-1 (severe-case deaths fall significantly against I-GH-1's treatment).
- **I-GH-3 Inspection:** with inspections of 20% of closed cases every 5 days and a dismissal penalty, the falsification rate (falsified returns per `ReturnFiled`, read from the analytics-only truth field) falls by a significant margin vs no inspections.
- **I-GH-4 Recorded crime:** guards with a recorded-thefts-down target and an incentive term show a report gap: the returned fall in thefts exceeds the true fall by a significant margin, and complaints turned away rise, over 20 seeds.
- **I-GH-6 FullInformation:** in the same runs, the faction view's theft figure equals the actual within rounding (INFORMATION_AND_MEDIA test I2 still holds).
- **I-GH-5 No incentive, no gaming motive:** with the same targets but no incentive terms, both the target gap and the report gap do not differ significantly from the no-target control.

**Unlocks:**
- **I-UNL-1:** `FoundInstitution` for a type whose rule fails is refused with `not_unlocked`, on every rule clause in turn (unit tests).
- **I-UNL-2:** in a scenario with one faction of 30 adults and one of 6, the larger reaches more Band unlocks within 20 days on at least 18 of 20 seeds; their measured ages can differ.

## 11. Open questions
The owner question (communal founding without the state) is in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#13-open-questions-owner). Settled here:
- **Band-age inspection.** There is no inspector post in the Band age; the faction borrows an elder for an inspection.
- **Returns vs `KpiReported`** (decided): `KpiReported` is the faction's measured value (equal to the actual under `FullInformation`); managers file separate returns (`FileReturn`, `ReturnFiled`) that can be gamed, and incentives are paid only on returns. KPI_CATALOGUE and INFORMATION_AND_MEDIA_DESIGN follow this split.
- **Managers chosen by members** use the "most trusted" vote in stage 4. Other rules (rotation, seniority, lot) are a [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md) extension.
- **Cultural trait ids** for unlock rules ("consult elders", "store food") are defined by [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md); until M11 those clauses are skipped.
- **Effort quanta and weights** (`Wpay`, `Wkeep`, `Wmot`, `Wcost`) are `SimTuning`. The Goodhart tests force traits to make a clear case; the natural rate of gaming on the island is a measurement, not a target.

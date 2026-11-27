# Economic classes design (stage 4 measures, later depth)

> **Superseded by owner D5 (26 Nov 2026), in part.** The design here already derives class only from relations and assigns no class identity; D5 confirms it (brief §17) and changes who reads the reports. The player is a god only (D5.1), and divine and institutional knowledge are separate (D5.28). See [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md). These earlier statements no longer hold:
>
> | Where | Earlier statement | Replaced by |
> |---|---|---|
> | [§6 Player and view](#6-player-and-view) | "**Economy report panel** (through `FactionKnowledgeView`, M13b)" as the player's report | The god's analysis reports read the divine analysis view under configured rules; `FactionKnowledgeView` is what the government organisation knows, shown separately and labelled as its records ([§12](#12-owner-d5-update-26-nov-2026)) |
> | [§4.9](#49-how-npcs-perceive-position) | `SeenEmployedBy`: "owner key … (NpcId, EnterpriseId or InstitutionId …)" | `NpcId` or `OrgId` (one id space for enterprises and institutions, [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#31-identity-and-ownership)) |
> | [§3.5](#35-derived-positions-analysis-only), [§4.5](#45-deriving-the-position) | `StateEmployee` defined by "hired by state" as the faction | "Hired by an organisation whose ownership model is State" (the government or a state body); hiring by any organisation is classified by that organisation's ownership model ([§12](#12-owner-d5-update-26-nov-2026)) |

## 1. Purpose and scope

This document owns **class as a measured relation** to property, productive assets, labour and the distribution of surplus:
- the per-NPC class *measures* and how each is computed from sim data;
- the derived class *positions*, which are analysis categories only;
- inequality and mobility measures;
- how NPCs perceive their own and others' economic position.

It links to, and does not repeat:
- items, production, ownership records, enterprises, jobs and wages: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- estates and inheritance: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md);
- political interests built on perceived position: [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md);
- KPI definitions and the warehouse tables: [KPI_CATALOGUE.md](KPI_CATALOGUE.md), [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- the philosophy behind it: [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md#46-marx-material-conditions-class-and-surplus).

## 2. Principles
1. **Classes are not character types.** No NPC has a class field. No NPC is created as a worker or an owner. Every NPC starts as a person with a body, a genome, a household and whatever its circumstances give it.
2. **A class position is a description of relations, computed after the fact.** It is derived from what an NPC owned, who it worked for, who worked for it and what it received, over a recent window.
3. **Analysis categories are never behaviour switches.** No decision code reads a class position. A static test enforces this (section 10).
4. **NPCs form their own, possibly wrong, view of position.** What an NPC believes about its own and others' standing is a *belief* built from its own observations and messages. It feeds decisions only as graded inputs, like any other belief.
5. **Measures before labels.** Every position is a rule over published measures with tunable thresholds. Reports always show the measures alongside the label, and sensitivity to the thresholds.
6. **No theory of value is assumed.** Where attributing a surplus needs a convention, two conventions are reported side by side (section 4.3).
7. **Measuring never changes the sim.** Measures are computed in `Telemetry` from state the sim already keeps, and computing them or not leaves `StateHash` unchanged.

## 3. Data structures

### 3.1 What the measures read (owned elsewhere)
These live in `Economy` and `Estate` and exist because mechanics need them. Listed here only so the measures can be defined precisely.

```text
Owner { OwnerKind Kind (None|Npc|Household|Group|Enterprise|State); int Id }   // ECONOMY_DESIGN.md 3.1
PropertyRecord { Asset, Owner, Basis, SinceTick, Rights: UseRight[<=8] }        // ECONOMY_DESIGN.md 3.5
Enterprise {                                                                    // ECONOMY_DESIGN.md 3.6
  Id, Form (Private|Cooperative|Communal|State),
  Shares: (Owner, permille)[<=16], Rule (DecisionRule), Buildings, StoreId, Postings,
  Books (Ledger: true income and outgoings by day) }
JobPosting { Id (ContractId), Poster (Owner), Kind, PayWare, PayCount, Per, ... }  // ECONOMY_DESIGN.md 3.6
```
- Work ticks come from the economy's daily snapshot field "work ticks by activity" plus `JobTaken` and `JobDone`; pay from `WagePaid` and `WageUnpaid`; surplus from the economy's appropriation measures ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#410-surplus)).
- An asset owned by the NPC's own **household** (`OwnerKind.Household`) counts as the NPC's own means; one owned by a `Group` it belongs to counts as communal.

### 3.2 Valuation: food-equivalents (`FE`)
- Barter has no prices, so values need a common unit. This document uses the **food-equivalent (`FE`)** at the fixed reference values `WareRefValue`, owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) (section 3.3) and stored in the warehouse as `dim_ware.ref_value`. It is a measurement index only: fixed for a run, so days and seeds compare, and no NPC or sim rule reads it. The warehouse also keeps wealth per ware, so no value theory is built in.
- A second, **observed** valuation is also reported: the median exchange ratio of each ware against food in `ExchangeStep` events over the last 10 days (falling back to the index with fewer than 5 trades). The gap between the two is a check that the index is not driving the conclusions.
- Both are integers.

### 3.3 Daily per-NPC accumulators (`Telemetry/Class/ClassLedger`)
Built by a consumer of the domain event stream, outside decision code. Bounded: one row per living adult NPC per day.

```text
ClassDay                           // one NPC, one day; all ints
  NpcId, Day, HouseholdId
  // labour, in work ticks, by relation to the means of production
  TicksOwnMeans          // working an asset the NPC privately owns (incl. foraging own plot)
  TicksCommunalMeans     // working a communal asset it holds rights in
  TicksCoopMeans         // working in a cooperative of which it is a member
  TicksHiredByPrivate    // employed by another NPC or a private enterprise
  TicksHiredByState      // employed by the state or an institution (incl. soldiers)
  TicksForagingWild      // unowned wild sources
  TicksHousehold         // unpaid care and household work, the sum of the four below (owner decision Q12: real work)
  TicksCareChildren      //   child-rearing: feeding, minding, carrying, teaching own or others' children unpaid
  TicksCareRelatives     //   caring for elders, the sick and the injured (Tend, Carry, Feed)
  TicksFoodPrep          //   preparing food for the household
  TicksHouseMaint        //   maintaining the household: hut repair, fetching for the store, fire
  // time use outside work (for the time-use report, 4.10)
  TicksLeisure           // awake, not working, not caring, not travelling for work: rest, play, social contact
  TicksOther             // sleep, eating, illness, travel to work; so that every category sums to the ticks of the day
  TicksHiringOthers      // labour ticks of others employed by enterprises it owns (pro rata to its share)
  // income, in FE, by source
  IncOwnProduct, IncCommunalShare, IncCoopShare, IncWage, IncStateWage,
  IncProfit, IncRent, IncTransferIn (gifts, welfare, inheritance), IncTheft, IncWild
  // flows out
  OutWagesPaid, OutTaxPaid, OutTransferOut
  // assets at end of day, in FE
  WealthPersonal (carried + stored goods), WealthProductive (share of productive assets),
  WealthHousing, Claims (loans owed to it), Debts (loans it owes)
```

### 3.4 Window measures (`ClassMeasures`)
Computed at each daily snapshot over the last `ClassWindowDays` days (default 5, half a sim year at defaults).

```text
ClassMeasures                      // all permille unless stated
  OwnsProductive : bool            // WealthProductive >= OwnThresholdFE (default 30 days of one adult's subsistence need, in FE)
  ProductiveWealthFE : int
  HiresLabour   : int              // hired-labour ticks / (one full-time worker-day of ticks)  (x1000)
  SellsLabour   : int              // (TicksHiredByPrivate + TicksHiredByState) / total work ticks
  WorksOwnMeans : int              // TicksOwnMeans / total work ticks
  WorksCommunal : int              // (TicksCommunalMeans + TicksCoopMeans) / total work ticks
  SurplusShare  : int              // share of the faction's surplus received (section 4.3), x1000000 (ppm)
  RetentionLabour : int            // received / labour-attributed product (1000 = receives exactly what its labour made)
  RetentionFactor : int            // received / factor-attributed product
  SubsistenceAccess : int          // section 4.4; 1000 = full need met without wages
  Dependent     : bool             // SubsistenceAccess < 500 and WageDependencePermille >= 500
  WealthFE      : int              // net worth: personal + productive + housing + claims - debts
  IncomeMix     : int[10]          // share of income from each source, sums to 1000
```

### 3.5 Derived positions (analysis only)

```text
// Telemetry/Class/ClassPosition.cs — never referenced from Policy, Cognition, Learning, Social, Comms
enum ClassPosition : byte {
  NotInWork,          // child, elder, sick or carer with no market or production work
  IndependentProducer,
  CommunalProducer,
  CooperativeMember,
  EmployerOwner,
  WageWorker,
  StateEmployee,
  DependentPoor,
  Mixed
}
struct ClassProfile { ClassPosition Primary; ClassPosition Secondary; short[9] Membership; }
```

`Membership` is a permille score for each position, so mixed cases are visible and analyses can use soft membership instead of the label.

## 4. Mechanics and algorithms

### 4.1 Order and determinism
- At the end of each sim day, after all systems have run, the snapshot step calls `ClassMeasures.Compute` for each living NPC **in ascending `NpcId`**, then the faction aggregates in ascending `FactionId`.
- No random draws are used. All arithmetic uses `long` intermediates and the `div_round` helper of [KPI_CATALOGUE.md](KPI_CATALOGUE.md).
- If `RecordingLevel` is `None`, the step is skipped. A test proves the hash is unchanged.

### 4.2 Labour attribution
- Each work action already belongs to a job, an asset or a wild source in the economy. The ledger classifies its ticks by the **ownership relation between the worker and the asset at the moment of work**, read from the `OwnershipRecord` and the NPC's employment contract.
- A member of a cooperative is counted under `TicksCoopMeans` even if the cooperative calls the payment a wage.
- An owner who works in its own enterprise counts under `TicksOwnMeans`.
- `TicksHiringOthers` for an owner of share `s` in an enterprise = `s × hired ticks / 1000`.

### 4.3 Surplus and its distribution
This builds on the economy's per-owner `appropriated` and per-worker `wages received − own output index` measures ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#410-surplus)), which are the daily inputs. For each enterprise or independently worked asset `e`, over the window:

```text
NetProduct(e) = Output(e) - Inputs(e) - Wear(e)              // FE
Surplus(e)    = NetProduct(e) - WagesPaid(e)                  // what is left for owners/members/state
```
- **Independent producers** have no wages: their whole net product is their own income, and `Surplus` is counted but goes to themselves.
- **Faction surplus** `S_f = Σ max(0, Surplus(e))` over all enterprises of the faction, plus taxes collected on wages.
- **Surplus share** of NPC *i* = (profit, rent, coop and communal shares *above its labour attribution*, plus its own-product surplus) ÷ `S_f`, in parts per million.
- **Two attribution conventions**, both reported:
  - *Labour convention*: `Attributed_L(i,e) = NetProduct(e) × ticks_i(e) / Σ ticks(e)`. Each worker is credited with the product in proportion to its labour time.
  - *Factor convention*: `Attributed_F(i,e)` splits `NetProduct(e)` between labour and assets in proportion to `LabourTicksValue` and `AssetWear + AssetHoldingCost`, then labour's part by ticks and the assets' part by ownership share.
  - `Retention_X(i) = 1000 × Received(i) / Σ_e Attributed_X(i,e)`, clamped to 0–10 000.
- Neither convention is the "true" one. Their gap is itself a reported measure.

### 4.4 Subsistence access and dependency
- **Subsistence access** asks: over the window, how much of its food need could this NPC have met without selling its labour? It counts:
  - food from its own productive assets at its own observed yield;
  - communal and cooperative rights it holds (shares of a household store, a communal farm, a coop's food);
  - wild food it actually gathered, plus the regrowth of wild sources within `WildReachNodes` of its home not already taken by others (a true-state estimate; this is analysis, not the NPC's belief);
  - transfers from kin it actually received (not welfare, which is counted separately as `WelfareAccess`).
- `SubsistenceAccess = min(1000, 1000 × (non-wage food capacity) / (food need))`.
- **Dependent** = `SubsistenceAccess < DependencyThreshold` (default 500) **and** the economy's `WageDependencePermille` (share of food and goods intake from pay over 7 days, [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)) ≥ 500.
- **Poor** = `WealthFE` below `PovertyLineFE` (default 3 days of one adult's subsistence need, in FE; KPI INQ-01's line is the published version) **and** `SubsistenceAccess < 500`.

### 4.5 Deriving the position
Thresholds live in `SimTuning.Analysis`, default 500 permille. Rules are checked in this order; the first full match is `Primary`, and the best partial match is `Secondary`.

| Order | Position | Rule |
|---|---|---|
| 1 | NotInWork | Child or elder, or total work ticks (**including unpaid care**, `TicksHousehold`) below 20% of a full-time day, and no profit or rent income above 100 permille. So a full-time carer is never "not in work" |
| 2 | EmployerOwner | `OwnsProductive`, `HiresLabour ≥ 1000` (one full-time worker or more) and profit plus rent ≥ 300 permille of income |
| 3 | DependentPoor | Poor, and no work share ≥ 500 |
| 4 | WageWorker | `TicksHiredByPrivate` ≥ 500 permille of work ticks |
| 5 | StateEmployee | `TicksHiredByState` ≥ 500 permille |
| 6 | CooperativeMember | `TicksCoopMeans` ≥ 500 permille |
| 7 | CommunalProducer | `TicksCommunalMeans + TicksForagingWild` ≥ 500 permille and `OwnsProductive` false |
| 8 | IndependentProducer | `TicksOwnMeans` ≥ 500 permille and `HiresLabour < 1000` |
| 9 | Mixed | none of the above; `Secondary` names the two largest components |

- **Membership scores**: for each position, the permille degree to which its main measure exceeds its threshold, so an NPC at 480 wage share and 470 own-means share reads as nearly half each.
- `Dependent` is a separate flag on every profile, because a wage worker may or may not be dependent.
- **Household position**: the same rules applied to the sum of a household's ledgers. Children and non-working elders take their household's position for mobility analysis.
- **Sensitivity**: reports recompute positions at thresholds 400, 500 and 600 and show how many NPCs change position. If more than 20% change, the report warns that the categories are fragile in that run.

### 4.5a Care work (owner decision Q12, 27 Nov)
**Care work is real work.** Child-rearing, caring for relatives, preparing food, tending the injured and maintaining households cost time and resources and give genuine benefits in the sim (children fed and protected, lower illness severity and faster recovery, food ready, huts kept: [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#care-of-elders-the-sick-and-the-injured), [POPULATION_DESIGN.md](POPULATION_DESIGN.md#disease-simple-model-in-stage-4-healthcare-later)).
- **In class calculations** the distinction is kept: care work counts as work for the `NotInWork` test, but it is not a relation to productive assets, so it does not enter `SellsLabour`, `WorksOwnMeans`, `WorksCommunal` or `HiresLabour`. Paid care (childminding for goods under a `CareArrangement`, or a care job) is counted where its pay comes from, like any paid work.
- **Unpaid care is reported separately, so it is never economically invisible.** Every economy report and the time-use KPIs ([KPI_CATALOGUE.md](KPI_CATALOGUE.md)) show unpaid care hours beside paid work, by sex, age group and household; its value is shown in hours, and optionally at a stated replacement valuation (FE of the same hours of paid care), always labelled as an analytical valuation.

### 4.6 Inequality
All per faction, per day, over living adults (and separately over households):
- **Gini** of `WealthFE` (KPI INQ-03) and of window income (INQ-04), in permille, with the integer formula in [KPI_CATALOGUE.md](KPI_CATALOGUE.md) section 6.5. Negative net wealth is clamped to 0 for the Gini, and the share of adults in net debt is reported separately.
- **Top 10% share**, **bottom 50% share** (INQ-05) and the Palma ratio (INQ-06).
- **Share of productive assets** held by the top 10% of owners, and the share of adults owning any productive asset.
- **Position shares**: the share of adults in each `Primary` position.

### 4.7 Mobility
- **Within a lifetime:** a transition matrix between `Primary` positions from the snapshot at the start of each sim year to the start of the next, over adults alive at both (INQ-09 is its off-diagonal share). Rows are permille and sum to 1000; rows with fewer than 5 NPCs are `Missing`, as in the KPI catalogue.
- **Between generations:**
  - Each child is compared with its parents at the **same life stage**: the average of their measures between year 2.0 and 2.5 (mid-adulthood at `LifeScale` 100).
  - **Rank-rank slope** of wealth (KPI INQ-07; formula in [KPI_CATALOGUE.md](KPI_CATALOGUE.md) section 6.6), with the parents' rank taken as the average of both parents where both are known.
  - A parent-to-child **position transition matrix** (INQ-08), using the parents' household position, beside the KPI's wealth-quintile matrix.
- **What makes mobility possible or unlikely** (all emergent, none designed to win): luck in foraging and harvest; skill growth; marriage and household pooling; inheritance customs and laws; returns on productive assets; debt; enclosure or exhaustion of commons; taxes and welfare; death and migration. Each run's report lists which of these mechanics were active.

### 4.7a Time use (owner decision Q12)
Reports include each adult's time in six categories per day and over the window, as shares of awake ticks and in hours (from the `ClassDay` ticks, so they reconcile to the events exactly):

| Category | Ticks |
|---|---|
| Paid work | `TicksHiredByPrivate + TicksHiredByState` (and paid care) |
| Independent production | `TicksOwnMeans` |
| Communal work | `TicksCommunalMeans + TicksCoopMeans` |
| Unpaid care | `TicksHousehold` (with its four parts) |
| Subsistence | `TicksForagingWild` (gathering from unowned sources for one's own or the household's use) |
| Leisure | `TicksLeisure` |

`TicksOther` (sleep, eating, illness, travel) completes the day so that the categories sum exactly to the ticks of the day. The breakdown is by sex, life stage and household position, so a division of labour inside households is visible as a measure, never assigned.

### 4.8 How inequality can arise (possibilities, not outcomes)
- **Concentrating forces**: assets that produce more than they wear; heirs who inherit undivided; skill compounding with tools; owners able to hire when others lose subsistence access; loans whose defaults transfer collateral.
- **Equalising forces**: food that spoils, so hoarding is limited; sharing norms and gifts; partible inheritance; communal rights; theft; taxes and welfare; mortality of the rich; migration of the poor to better states.
- The balance is not tuned to any target. Measures tell us which way each run went.

### 4.9 How NPCs perceive position
NPCs never read `ClassMeasures`. They build their own view in `Cognition` from layers 2, 3 and 5 of the information model ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#6-information-layers-one-vocabulary-for-every-doc)).

```text
PerceivedStanding                // in Mind, own view of self; ints
  OwnWorkMix   : short[6]        // own work by relation, from its own memory of what it did (near exact)
  OwnIncomeMix : short[6]        // from its own receipts (near exact)
  FoodSecurity : short           // own estimate: days it could eat without a job, from beliefs about stores and sources
  RankGuess    : sbyte           // -3..+3 vs the NPCs it knows, from visible goods, huts and tools
PerceivedOther                   // per known NPC: fields of the social-memory Relation record (M10)
  SeenEmploying : byte           // count of times seen paying or directing others (decays)
  SeenEmployedBy: int            // owner key last seen employing them (NpcId, EnterpriseId or InstitutionId, kind in 2 high bits)
  EconFlags     : byte           // MyEmployer, MyWorker, SameWorkplace, SameCoop, SameCommons
  WealthGuess   : sbyte          // -3..+3 vs self
```
- The fields sit in the `Relation` record of [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#3-data-structures-simsocial), which counts them in its memory budget. Creditor and debtor are already social-memory flags. Who owns which asset is a property belief in `Mind.Beliefs` ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#44-beliefs-and-knowledge-mindbeliefs)), not a field here.
- **These are beliefs and can be wrong.** An NPC may not know who owns the workshop it works at, or may overrate a neighbour's wealth.
- **What they feed** (graded inputs only, never switches):
  - situation features such as "my employer is near", "a fellow worker is in need", "this one is much richer than me";
  - the status drive, which rises on seeing richer neighbours ([POPULATION_DESIGN.md](POPULATION_DESIGN.md#drives));
  - the similarity of interests used to weight solidarity in [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md);
  - imitation targets in [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) (who counts as successful).
- **Measuring the gap**: reports compare each NPC's `PerceivedStanding` with its true `ClassMeasures` (for example, perceived vs true rank). Misperception is itself a finding.

## 5. Interactions with other systems
- **Economy**: supplies every input. If an economic mechanic creates a new relation to the means of production (for example, sharecropping or rent), it must add a ledger category here. See [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md).
- **Material inheritance**: estates move productive assets between generations. Mobility analysis joins `EstateSettled` events to parent and child measures. Inheritance customs are the main treatment variable for experiments on wealth persistence. See [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md).
- **Households and kinship**: household pooling and the kinship graph give household positions and parent-child links. See [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md).
- **Political economy**: uses `PerceivedStanding` and `PerceivedOther` to compute material interests and solidarity. Uses `ClassMeasures` only in analysis (does stance track position?). See [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md).
- **State and institutions**: welfare, taxes and wage policies change incomes and subsistence access. Statistics institutions (City age) publish *reported* versions of these measures that can differ from the true ones. See [STATE_DESIGN.md](STATE_DESIGN.md), [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md).
- **KPIs and warehouse**: section 7.

## 6. Player and view
- **Economy report panel** (through `FactionKnowledgeView`, M13b): Gini of wealth and income; top 10% and bottom 50% shares; share of adults owning productive assets; position shares with membership bars; dependency rate; a mobility matrix for the last year; **time use** (4.7a) with unpaid care shown beside paid work.
- **NPC inspector**: shows the NPC's *own* view (its `PerceivedStanding`), not the analysis label. A separate "analyst" tab, off by default and marked as true-state, shows the computed measures for debugging and teaching.
- **Wording**: the HUD uses the neutral position names in section 3.5 and always shows the underlying measures next to them.
- No animation exists for class; nothing on screen changes when a label changes.

## 7. Telemetry
- **Consumes**: `JobTaken`, `WagePaid`, `ExchangeStep`, `GiftGiven`, `TaxCollected`, `OwnershipChanged`, `EstateSettled`, `NpcBorn`, `NpcDied`, `HouseholdJoined`, and a new daily aggregate event.
- **Proposed new event** (for [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) and [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md) to accept or reshape):
  - `WorkDone { Day, NpcId, AssetId, EnterpriseId, Relation, Ticks, OutputFE }`, one per NPC per workplace per day, so labour attribution does not need per-tick events.
  - `SurplusDistributed { Day, EnterpriseId, RecipientKind, RecipientId, AmountFE, Basis (Profit|Dividend|CommunalShare|Rent|Tax) }`.
- **Snapshot fields** added to the daily person snapshot: every field of `ClassMeasures`, `ClassProfile.Primary`, `Secondary`, `Membership`, `Dependent`, `Poor`, and both valuations of `WealthFE`.
- **Faction snapshot fields**: the inequality measures in 4.6 and position shares.
- **Warehouse**: `class_key` on `fact_person_day` (with `dim_class` holding the positions in 3.5), `wealth_fe` and `income_fe` from these measures, and the `fact_person_day_class` side table (grain: NPC × day) for the full measure vector and membership scores. `dim_class` and `fact_person_day_class` are in the published list of [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#11-the-published-table-list) (M13). Faction aggregates and mobility come from the KPIs (INQ-03 to INQ-11) in [KPI_CATALOGUE.md](KPI_CATALOGUE.md); tables are owned by [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).

## 8. Stage 4 minimum vs later stages

| Milestone | Delivers |
|---|---|
| M6 | Ownership records (state, private, communal) and wages exist; the ledger categories can be counted |
| M4 | Care acts exist (`CareGiven`, `ChildFed`); unpaid care and time-use ticks (`TicksHousehold` and its parts, `TicksLeisure`, `TicksOther`) are counted from M4, so time use can be reported from M13a |
| **M8** | **Measures computed**: `ClassLedger`, `ClassMeasures`, `ClassProfile`, reference valuation, surplus with both attribution conventions, subsistence access, Gini and position shares, time use (4.7a), in the daily snapshot. Cooperatives and NPC employers exist, so every position can occur |
| M10 | `PerceivedStanding` and `PerceivedOther` in the mind, used by political-interest measures |
| M11 | Imitation uses perceived success, so it can depend on perceived standing |
| **M13a / M13b** | **Recorded** (M13a: ledger and time-use facts; M13b: KPIs and the report panel): warehouse fields and tables, Band-age KPIs (INQ-03 wealth Gini, INQ-05 top shares, INQ-06 Palma, INQ-11 class composition), position shares and dependency rate as report items, economy report panel |
| Stage 5 | Village-age KPIs (INQ-04, INQ-09, INQ-10); intergenerational measures once several generations exist (INQ-07 and INQ-08 are Town-age KPIs, but headless runs can compute them earlier); household positions |
| Stage 6 | Money: valuation switches to prices where they exist; rent, interest and shares as income sources; banks change debt measures |
| Stage 7 | Statistics institutions publish *reported* versions; the actual vs reported pair per KPI |

## 9. Migration from the existing code
There is no class logic in stages 1–3. Existing `EcoBuilding` recipes and stores become enterprises in M6 ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)); this document only adds a consumer and snapshot fields.

## 10. Tests and acceptance criteria
All run under `dotnet test DotNet` and become entries in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

**Unit tests on hand-built fixtures (M8)**
1. `Class_OwnerWithTwoWorkers`: NPC A owns a workshop and employs B and C full time for 5 days; A does no other work. Expect A `EmployerOwner`, `HiresLabour` = 2000 ± 10; B and C `WageWorker`, `SellsLabour` = 1000.
2. `Class_IndependentForager`: an NPC who only forages its own plot. Expect `IndependentProducer`, `WorksOwnMeans` = 1000, `SurplusShare` equals its own-product share.
3. `Class_CoopNotCountedAsWage`: three members of a cooperative paid "wages" by it. Expect `CooperativeMember`, `SellsLabour` = 0.
4. `Class_Dependency`: a worker with no plot, no communal rights and no wild food within reach. Expect `SubsistenceAccess` < 100 and `Dependent` true. Give it a communal farm share covering its need: expect `SubsistenceAccess` = 1000 and `Dependent` false, with its job unchanged.
5. `Class_MixedCase`: 450 permille wage work, 450 own plot, 100 communal. Expect `Mixed`, with `Secondary` `WageWorker` or `IndependentProducer` and both memberships within 50 of each other.
6. `Class_SurplusConventions`: an enterprise with net product 1000 FE, wages 600 FE, one owner who did no work, two workers with equal ticks. Labour convention: owner retention = 10 000 (clamped), workers 600. Factor convention matches a hand calculation within 1 FE.
7. `Gini_KnownValues` (shared with the KPI tests): [0,0,0,100] gives 750; [5,5,5,5] gives 0; [1..100] gives 330 ± 1.
8. `Mobility_RowsSum`: every transition-matrix row sums to 1000 ± 9 (rounding), on a 20-NPC synthetic history.
9. `Intergenerational_Identity`: synthetic data where child rank equals parent rank gives a slope of 1000; independent random ranks over 1000 pairs give |slope| < 100.

**Neutrality and determinism**
10. `ClassPosition_NotReadByDecisions`: reflection test; no type in `Policy`, `Cognition`, `Learning`, `Social`, `Comms`, `Economy`, `State` or `Institutions` has a field, parameter, local or generic argument of type `ClassPosition`, `ClassProfile` or `ClassMeasures`.
11. `ClassMeasures_DoNotChangeHash`: 20 seeds × 10 sim years with class measures on and off; identical `StateHash` at every checkpoint.
12. `ClassMeasures_Deterministic`: two runs of the same seed give byte-identical class snapshot rows.
13. `Perception_NotTruth`: an NPC that has never seen or been told who owns its workplace has `SeenEmployedBy` unknown, even though the true record exists.

**Scenario checks (M13, descriptive, over 50 seeds × 6 sim years)**
14. `Scenario_NoFixedOutcome_Enclosure`: a scenario where wild food is scarce and one NPC starts with a large farm. Report the share of seeds in which the dependency rate exceeds 300 permille by year 5. Pass if the measure is computed for every seed and the result is neither 0% nor 100% (see [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md#11-tests-and-acceptance-criteria)); no threshold on the rate itself.
15. `Scenario_PartibleVsUnitaryInheritance` (stage 5, causal): randomise inheritance custom across 100 seeds; report the difference in wealth Gini at year 20 with a 95% interval. Pass if the experiment runs to completion with its control; the sign is a finding, not a requirement.
16. Performance: computing class measures for 600 NPCs takes under 2 ms per sim day on one core.

**Care work and time use (owner decision Q12)**
17. `Care_CountsAsWork_NotAsRelation`: an adult who spends 80% of its awake ticks minding and feeding children and does no other work is not `NotInWork`; its `SellsLabour`, `WorksOwnMeans` and `WorksCommunal` are 0 and its unpaid-care share is 800 ± 10 permille.
18. `TimeUse_SumsToDay`: for every NPC and day over 10 seeds × 5 sim years, the six categories plus `TicksOther` sum exactly to the ticks of the day; each category equals its recomputation from events.
19. `UnpaidCare_Reported`: every economy report and the KPI set contain the unpaid-care series (hours and share) beside paid work, with no gap where care occurred (checked over 5 seeds).

## 11. Open questions

No open questions remain.

**Owner decision Q12 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)): care work is real work.** Child-rearing, caring for relatives, preparing food, tending the injured and maintaining households cost time and resources and give genuine benefits. The distinction between care work and relationships to productive assets is kept in class calculations, but unpaid care is reported separately so that it is not economically invisible. Reports include time on paid work, independent production, communal work, unpaid care, subsistence and leisure (sections 4.5a and 4.7a; tests 17–19; [KPI_CATALOGUE.md](KPI_CATALOGUE.md) TIM-01 to TIM-03).

Settled in review (26 Nov 2026): the FE index is `WareRefValue`, owned by ECONOMY_DESIGN, with the observed valuation always shown beside it (revisit if they differ by more than 30 percent on the main wares); soldiers count as `StateEmployee` with a `Military` sub-flag; reports show both 5-day and 10-day windows until a run shows which is more stable; wild foraging and communal work keep separate tick counts and merge only in the position rule.

1. **Household work.** Child-rearing and care are real work but produce no FE. Default: they count as work for the `NotInWork` test, but not in the relation shares.

## 12. Owner D5 update (26 Nov 2026)
**Classes are derived from relations to labour, ownership and productive assets only (brief §17).** D5 restates the rule this document already follows and extends it to organisations:
- **No permanent class identity.** No NPC, role, office or organisation carries a class field. `ClassPosition` stays in `Telemetry` (analysis only), recomputed over a window, so any NPC can move between positions in its lifetime. The static test of section 10 (no decision code reads a position) also covers `Sim/Orgs` and god policies.
- **No political or religious belief from class.** Political stances (POLITICAL_ECONOMY) and religious affiliation (COMMANDMENTS_AND_RELIGION) are never assigned from position; whether they track positions is a finding.
- **Roles are not classes.** Priest, judge, healer, teacher, soldier, manager and official are roles held in organisations ([ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md §4.3](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#43-roles-members-employees-managers-officers)). The class measures see only the relation each role puts the NPC in: who owns the asset it works, who pays it, what share of output it receives.
- **Organisations classified by ownership model.** Work for an organisation is attributed by that organisation's `OwnershipModel` and the NPC's relation to it: hired by a Private organisation → `TicksHiredByPrivate`; by a State organisation (the government or a state body) → `TicksHiredByState`; member-owner of a Cooperative → `TicksCoopMeans`; rights-holder of a Communal organisation → `TicksCommunalMeans`; paid staff of a Religious organisation → a new component `TicksHiredByReligious`, reported separately and treated as hired labour in the position rule. Unpaid volunteering for any organisation counts as communal work, reported as its own line. Soldiers stay `StateEmployee` with the `Military` sub-flag when the military organisation is state-owned.
- **Control without ownership.** A new analysis measure `ControlsProductivePermille` (share of the NPC's work ticks spent directing an organisation's productive assets or staff from an office it holds, without owning a share) lets reports distinguish owners from managers and officials. It is a measure, not a position, until evidence shows it is needed.
- **Who reads the reports.** The god's analysis view (Mode D, divine observation rules, D5.28) and the analytical warehouse. The government organisation may later publish its own, possibly wrong, versions (statistics institutions, Tier 3). Reports never feed back into the sim.

**Tier and package mapping:** class measures over organisation relations: Tier 2 with C3 (employment and enterprises); reporting: Q1. **Acceptance hooks:** scenario 3 (an employment relation appears in `TicksHiredBy*` for the worker and `TicksHiringOthers` for the owner), scenario 17 (class ledgers reconcile to `WorkDone`, `WagePaid` and organisation events).

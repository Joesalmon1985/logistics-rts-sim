# Information and media design (stage 4)

This document owns the seven information layers in detail: records, data collection, how information degrades, media, and the `FactionKnowledgeView`. The measures themselves are in [KPI_CATALOGUE.md](KPI_CATALOGUE.md). Shared names, ages and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

## 1. Purpose and scope

**Owns:**
- the seven layers and the rules that keep them apart;
- the record and publication data structures;
- how information flows and degrades: delay, noise, bias, loss, falsification;
- data collection as institutional activity: census, surveys, investigations, inspections, statistical reporting;
- media: outlets, ownership, reach, credibility, misinformation, and how publications feed NPC beliefs;
- `FactionKnowledgeView`: its interface, how it is filled per age, and the debug "true state" toggle.

**Links, does not repeat:**
- Beliefs, gossip and misunderstanding between NPCs: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) and [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md).
- The institution model, staff motives and audits: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md).
- Every measure and formula: [KPI_CATALOGUE.md](KPI_CATALOGUE.md).
- Tables and recording levels: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).
- The policy interfaces that read the view: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).

## 2. Principles

1. **Seven layers, never merged.** A value that crosses from one layer to the next goes through a named step that can add delay, noise, bias or loss. There is no shortcut from layer 1 to layers 2 to 7.
2. **Truth is for the sim, the warehouse and the debug overlay.** In normal play neither an NPC, an institution nor the player reads `GodState` directly. The only code that reads both truth and a knowledge layer is the sim step that *produces* that layer (a perception system, a recorder, a report builder, the view filler).
3. **Build the separation now, enforce it later.** Stage 4 routes the HUD and every faction policy through `FactionKnowledgeView`, then fills it almost completely. Later ages narrow the fill without rewriting a consumer.
4. **Knowing costs something.** Counting people, inspecting a market or running a survey uses staff time, goods and treasury. Better information is bought, and the player chooses how much to buy.
5. **Measurement is a mechanic, not a mirror.** Better counting can make a problem look worse. Targets change what gets recorded. Both effects are possible, and neither is scripted: they follow from the recording rules and the staff's motives.
6. **Recorded is not true.** Every record carries its source, observer, time, confidence and a possible error. Whether a record is false is known only to the audit sidecar (section 3), never to the record's readers.
7. **Recording output is not the sim's records.** Layer 4 records are sim state: saved, hashed, deterministic. The warehouse copies them through domain events, and switching it off changes no hash.

## 3. Data structures (`Sim/Information`)

### 3.1 The seven layers and where each lives

| # | Layer | Type and home | Who reads it | Written by |
|---|---|---|---|---|
| 1 | True state | `GodState` (all modules) | sim systems; warehouse; debug overlay | the sim |
| 2 | Observed events | `NpcObservation`, perception memory (`Cognition`) | that NPC | `Perception` |
| 3 | Communicated | `Message`, gossip items (`Comms`, `Social`) | the receiving NPC | senders (may lie or err) |
| 4 | Institutional records | `InfoRecord`, `Measurement` in `InstitutionRecords` (`Information`) | the institution and its management policy; report builders | staff (may err or falsify) |
| 5 | Published | `Publication` by `Outlet` (`Information`) | NPCs in reach | outlet owners and editors |
| 6 | Beliefs | `Mind.Beliefs` (`Cognition`) | that NPC | layers 2, 3, 5 |
| 7 | Player and faction | `FactionKnowledgeView` | the HUD, `IFactionPolicy` | `ViewFiller` from layer 4 (and in `FullInformation` from layer 1) |

### 3.2 Institutional records

```csharp
enum RecordKind : byte { Count, Event, Ledger, Roll, Register, Case, Inspection, SurveyReturn, Report }
enum SourceKind : byte { Observed, ToldByNpc, Reported, Measured, Inferred, Copied, Estimated }

struct InfoRecord                    // one line in an institution's books
{
    int  RecordId;                   // unique, never reused
    int  Institution;                // InstitutionId that holds it
    RecordKind Kind;
    SubjectRef Subject;              // (SubjectKind, id): NPC, household, hex, building, item lot, faction, case
    int  MeasureId;                  // what was recorded: a KpiId or an event type
    int  Value;                      // integer in the measure's unit (see KPI_CATALOGUE conventions)
    SourceKind Source;
    int  ObserverNpc;                // who saw or wrote it (0 = the institution itself, e.g. a ledger entry)
    int  ObservedTick;               // when the fact happened
    int  RecordedTick;               // when it was written down (>= ObservedTick: the delay)
    ushort Confidence;               // 0-1000: the recorder's own belief in the value
    ushort ErrorBound;               // 0-1000 permille half-width the recorder believes applies (0 = exact claim)
    byte Flags;                      // Estimated, Late, Revised, Contested, Superseded
}

struct RecordAudit                   // sidecar: sim and telemetry only. No policy or HUD type may read it.
{
    int  RecordId;
    int  TrueValue;                  // what the sim says the value was (0 if unknowable)
    sbyte Cause;                     // None, Noise, Bias, Loss, Late, Falsified
    int  Recorder;                   // NpcId who altered it, if Falsified
}
```

- A **bounded store** per institution: a ring of `RecordsCap` lines (default 256 for a Band-age institution), oldest summarised into `Measurement` rows when evicted.
- `RecordAudit` exists so tests and the warehouse can measure the gap between recorded and actual. It is saved with the state and counted in the hash, but no code in `Policy`, `Game` or the view may reference it (reflection test, section 10).

### 3.3 Measurements and reports

```csharp
struct Measurement                   // an aggregated, reportable figure
{
    int  KpiId; KpiScope Scope;      // scope: whole faction, a hex or region, a breakdown cell
    int  PeriodStart, PeriodEnd;     // ticks
    int  Value;                      // fixed point (permille, per-10000 or integer units, per KPI)
    int  Lo, Hi;                     // interval the producer claims (equal to Value if exact)
    int  SampleN;                    // persons or events behind it (0 = full count)
    ushort Coverage;                 // 0-1000: share of the target population actually seen
    MethodKind Method;               // Tally, AdminRecord, Census, Survey, Inspection, Estimate, Hearsay
    int  Producer;                   // InstitutionId
    int  PublishedTick;              // when the report became readable (>= PeriodEnd: the lag)
}
```

`Measurement` is the layer 4 to 7 hand-over. The `KpiReported` domain event carries one per publication: it is the faction's **measured** value. Under `FullInformation` it equals the actual value; in later ages it is computed from records and from managers' returns.

**Returns are separate.** An institution manager files a **return** for each KPI with a directive (`FileReturn` command, `ReturnFiled` event; [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#the-generic-institution-model)). A return is the manager's choice and can be gamed; incentive terms are paid only on returns, never on `KpiReported`. In `Reported` mode a return is one more source the report builders may use, tagged `SourceKind.Reported`.

### 3.4 Publications and outlets

```csharp
enum OutletKind : byte { ElderCall, NoticeBoard, Crier, Pamphlet, Press, Broadcast }
enum OwnerKind  : byte { State, Private, Cooperative, Communal, Foreign }

struct Outlet
{
    int  OutletId; OutletKind Kind;
    OwnerKind OwnerKind; int OwnerId;           // faction, NPC, enterprise or group
    int  Institution;                           // the institution that runs it (0 for a lone crier)
    ReachSpec Reach;                            // hex set (<= 64 hexes) or "whole faction"
    ushort ReachShare;                          // 0-1000: share of NPCs in the reach who see an item (access, literacy, habit)
    ushort Credibility;                         // 0-1000: its track record as *NPCs perceive it* (a running figure, see 4.6)
    ushort Accuracy;                            // 0-1000: hit rate of its claims vs truth. Measured by the sim, never shown to NPCs
    sbyte[] Bias;                               // per topic slant, -100..100 (pro-government, alarmist, pro-trade ...)
    ushort Sensationalism;                      // 0-1000: weight on dramatic items over typical ones
    ushort Independence;                        // 0-1000: how little the owner's wishes steer content
    int  CadenceTicks;                          // ticks between issues
    int  StaffNpcs; int Budget;                 // running costs in treasury goods or owner funds
}

struct Publication
{
    int  PubId; int Outlet; int Tick;
    PublicationKind Kind;                       // Fact, Opinion, Notice (job/price/contract), Warning, Scandal, Retraction
    SubjectRef Subject; int MeasureId; int Value;   // the claim
    ushort Stated;                              // 0-1000 confidence the outlet states ("certain" vs "rumoured")
    FalsityKind Falsity;                        // sim-side tag only (audit): None, HonestError, Slant, Fabricated
    int SourceRecord;                           // the InfoRecord or Measurement it came from, 0 if invented
}
```

`Falsity` and `Accuracy` follow the same rule as `RecordAudit`: sim and telemetry only.

## 4. Mechanics

### 4.1 How information flows and degrades

One flow, with named steps. Each step has parameters on `SimTuning.Info` and a random stream `Rng.Fork("inst", id).Fork("info", step)`.

```text
fact (layer 1)
 -> observe     NPC sees it with probability p_see (perception, attention)           [loss, noise]
 -> record      staff write it down; time passes; may round or shade it             [delay, bias, falsification]
 -> aggregate   counts, tallies, samples become a Measurement                       [coverage, sampling error]
 -> transmit    reports go up the chain to the faction centre                       [delay, loss, summarising]
 -> view        FactionKnowledgeView presents it                                    [staleness label]
 -> (publish)   an outlet may carry it to the public                                [selection, slant]
 -> believe     NPCs update Mind.Beliefs                                            [trust, confirmation]
```

**Degradations (all integer, all deterministic):**

| Effect | Rule | Typical cause |
|---|---|---|
| Delay | `RecordedTick = ObservedTick + d`, `d = baseDelay + queue / staffCapacity`. A report is unreadable before `PublishedTick`. | few staff, long walks to the centre, no messengers |
| Loss | Each item is dropped with probability `loss`. `Coverage` falls to match. | unobserved places, absent staff, no writing system |
| Noise | `Value += draw(-w, +w)`, `w = |Value| * noisePermille / 1000`, with the draw a sum of 4 uniform integers for a bell shape | tallying by eye, poor tools |
| Bias | `Value = Value * (1000 + biasPermille) / 1000` | systematic detection or definition (4.3) |
| Falsification | staff alter a figure (4.5) | self-interested staff, targets, fear |
| Selection | only a subset ever reports (the cases that reach the elders) | trust, cost of reporting, access |

**Default quality of a Band-age recorder:** loss 0–200‰ by distance from the recorder, noise ±100‰, delay 0–2 days, no bias. The `FullInformation` profile sets all of these to zero (section 4.8).

**Time labels.** Every figure the view shows carries `AsOfTick`. The HUD shows its age in days: "as of 3 days ago". A figure never updates faster than its institution reports.

### 4.2 Data collection as institutional activity

Collecting data is a **task** an institution's staff perform. It costs staff days and goods, produces `InfoRecord`s and `Measurement`s, and is steered by the institution's policy and the faction's directives (budget and frequency). The player improves information quality by funding more of it.

| Activity | What staff do | Output | Error source | Age |
|---|---|---|---|---|
| **Tally** | An elder or storekeeper counts what they can see, or is told | headcount, store stock | coverage, noise | Band |
| **Register** | Births, deaths, partnerships and arrivals are noted when someone tells the elder | rolls and ledgers | late and missed entries | Band |
| **Census** | Staff visit every household in a region and record members, goods and jobs | a full count of a region at a time | refusal, absent households, falsification by staff and householders | Village (simple), Town (regular) |
| **Survey** | Staff draw `n` persons from a frame and ask a question | estimate with an interval | sampling error, non-response, misreporting | Town |
| **Investigation** | Staff follow a specific suspicion (a missing store, a crime) | a `Case` record with findings | time, access, bribery | Band (elders), Town (police) |
| **Inspection** | Staff visit a workplace, market or institution and check records against what they see | an `Inspection` record and, as a side effect, an audit of the inspected institution's books | selection of what to visit, concealment | Village (headman), City (regulators) |
| **Statistical reporting** | A statistics office combines registers, surveys and inspections into KPI reports | `Measurement`s, published on a schedule | all of the above, plus definitions | City |

**Surveys** (the model for sampling with error):
- **Frame:** the ids the institution knows, sorted ascending. The sample is drawn with a partial Fisher-Yates shuffle on `Rng.Fork("inst", id).Fork("survey", seq)`. A survey of `n` persons costs `n × costPerResponse` staff ticks.
- **Response:** each sampled person answers with probability `respond = baseRespond × trust_in_institution / 1000`, so a distrusted office gets a poorer sample. Non-response is not random: persons with a grievance respond less (a selection bias the player can later correct by weighting).
- **Answer:** each responder gives the true value of the asked measure, except with probability `misreport` (a person with something to hide, such as a theft, a hidden store or an illegal job) when it answers falsely. Beliefs-based questions (fear of crime, trust) are answered from `Mind.Beliefs`, not from truth.
- **Estimator:** for a share, `p̂ = k / m` over `m` responders; the interval is `p̂ ± z · sqrt(p̂(1 − p̂) / m) · sqrt(1 − m/N)` with `z = 1.96` and integer square root. For a mean, the sample mean and `s/√m`. The `Measurement` carries `Lo`/`Hi` and `SampleN = m`.
- **Expected behaviour:** the standard error falls as `1/√m` (a quadrupled sample halves it), and the finite-population factor makes a survey of most of the frame nearly exact.

**Census vs survey:** a census has `Coverage` near 1000 but costs `N × costPerHousehold` and is rare (a few times in the game). A survey is cheap and frequent but has sampling error. The player chooses.

### 4.3 When better measurement makes things look worse

Reported value = true value × **detection rate** (and, for recorded events, × reporting rate). Detection rates are properties of institutions and of NPC behaviour:

`reported_events = actual_events × detect / 1000`, with `detect = coverage × report_rate × recognition / 1_000_000` (each in permille).

Raising `detect` (more guards, a register where there was none, a court people trust enough to bring cases to, a healer who can diagnose) raises the **reported** series, while the **actual** series does not move. Examples the mechanics make possible (whether they happen is measured):
- Reported theft doubles after guards are posted, because more theft is seen and more victims report, while actual theft is flat or falling.
- Recorded illness rises after a healer arrives, because illness previously went unrecognised.
- Recorded population rises after the first census, because births, deaths and arrivals were missed.

**Countermeasures built in:**
- Every crime, health and court KPI has a **detection-independent partner** (a survey, a death count, a stock check), see the scorecards in [KPI_CATALOGUE.md](KPI_CATALOGUE.md#balanced-scorecards-against-goodharts-law).
- The dashboard shows `Coverage` and `Method` beside each figure, and flags a **series break** when the method or coverage changed in the window ("counted differently from day 12").
- Nothing is scripted: the effect appears only because the recording rule and detection rates differ between periods. A test (section 10) holds actual constant and changes only `detect`.

### 4.4 Records feed management, and management feeds back

An `IInstitutionPolicy` reads only `InstitutionView`: its own records, budget, staff and the faction's directives. Its management therefore acts on **layer 4**. If a faction directive says "reduce reported theft" with an incentive term, the head of guards can shade its *return*, reduce the *recording* (4.5) or reduce the *thefts*. Which one is cheaper depends on the world; incentives are paid only on returns, and nothing rewards `KpiReported` directly (Goodhart, [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).

### 4.5 Falsification by self-interested staff

A recorder NPC has motives: pay or standing tied to a target, fear of punishment, a bribe, loyalty to a person, or plain laziness. Each time a record is written, the recorder's decider (an NPC decision, scored like any other) may choose among **Record honestly**, **Round or shade**, **Omit**, **Invent**.

```text
shade   = clamp(target_gap, 0, MaxShade)           // MaxShade per institution policy, default 150 permille
gain    = incentive(target_gap) + bribe
cost    = P(audit finds it) * penalty + guilt(honesty trait)
choose falsify when gain - cost > 0 (ties by lowest option index, small seeded noise term)
Value'  = Value * (1000 - shade) / 1000      (for "bad" measures: crime, waiting times, defaults)
```

- `P(audit finds it)` depends on the inspection intensity (4.2) and on how large the gap is relative to the series' usual noise.
- Falsification writes the altered `Value` to the record and the true one to `RecordAudit` with `Cause = Falsified`. Neither the record's readers nor the faction can tell the difference, except through an audit, a survey cross-check, or an NPC who saw the truth and speaks (gossip, layer 3).
- Consequences are other systems' business: sacking, fines, loss of legitimacy, a whistleblower. They arise only through those mechanics.

### 4.6 Media (City age; lighter forms earlier)

| Age | Forms |
|---|---|
| Band | none as media. Word of mouth and the elders' announcements (layer 3 and 4). |
| Village | **Notice board** at a hub (already used for jobs and prices in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)); **town crier** (an NPC paid to call announcements). Small reach, owner is the state or a headman. |
| Town | **Pamphlets** and a first **press**: private or cooperative outlets become possible once enough adults can read the symbol system and an enterprise can afford to print. |
| City | **Press** and **broadcast**: wide reach, many owners, a **statistics office** publishing KPI reports, and media institutions with limited-information rules enforced. |

**Ownership and funding.** Outlets are `State` (run as an institution, paid from the treasury), `Private` (one NPC or an enterprise, paid by sales and, later, advertising), `Cooperative` (readers or workers own it) or `Communal`/`Foreign`. Funding shapes behaviour: a state outlet follows directives unless its staff resist; a private outlet follows its owner's interests and its sales; a cooperative follows the members. The model assumes none of these is best.

**What an outlet publishes.** On each issue its editor policy (an `IInstitutionPolicy`) chooses items from: reports it received (records, `Measurement`s), notices, its own observation, rumours it picked up, and claims from interested parties. Choices are scored on audience interest (`Sensationalism`), the owner's agenda (`Bias`), cost, and risk (a false claim exposed costs credibility). Misinformation takes three forms, tagged for audit:
- `HonestError`: the outlet passes on a wrong record or misreads one;
- `Slant`: a true item chosen, framed or omitted to suit an agenda;
- `Fabricated`: invented, or a claim the editor knows to be false.

**Reach and exposure.** Once per day, for each NPC in an outlet's reach and each issue: the NPC is exposed with probability `ReachShare × habit / 1000` (habit is an NPC trait-derived 0–1000, and reading needs the symbol-system skill for written forms). Hearing-based forms (crier, broadcast) need no literacy. Exposure is drawn from `Rng.Fork("npc", id).Fork("media", day)`.

**How a publication becomes a belief.** On exposure, for each claim:

```text
trust_o   = the NPC's learned trust in this outlet (0-1000, starts at the tuning prior for the outlet kind)
weight    = trust_o * Stated / 1000
if the NPC holds a conflicting belief b:
      weight = weight * (1000 - stubbornness(b, conformity)) / 1000   // confirmation bias
new belief: value = claim.Value, source = Published(outlet), confidence = weight, tick = now
merge with an existing belief for the same subject by the belief store's rule (higher confidence * recency wins)
```

- **Outlet trust is learned.** When the NPC later observes the subject directly and the claim was right (within tolerance) or wrong, `trust_o` moves up or down (`+= (1000 - trust_o) >> 4` or `-= trust_o >> 3`; wrong costs more). Trust is stored for up to 8 outlets per NPC.
- `Outlet.Credibility` is the **mean of `trust_o` across NPCs in reach**, a perceived figure. `Outlet.Accuracy` is the true hit rate. The gap between them is the measure of a **credible liar** (high credibility, low accuracy) or an **ignored truth-teller**.

**What publications change.** Only through beliefs, so through the ordinary decider:
- **Loyalty** inputs (what NPCs believe about their state and other factions, see [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)).
- **Prices and stocks**: a published shortage belief makes NPCs hoard or migrate to sell. The belief, not the shortage, drives the behaviour.
- **Reputation**: a published accusation (`Scandal`) becomes a reputation item for the person or institution named, entering social memory as hearsay.
- **Politics**: beliefs about which class benefits from a policy, about wages and rents, which feed the interest *measures* in [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md).
- **Compliance and trust in institutions**: tax compliance, willingness to report crime, use of the courts.

**State levers over media** (they have costs; none is "right"): found a state outlet, subsidise or tax outlets, license them, restrict content by decree (enforced by guards, costing legitimacy and the credibility of state outlets when found out), and fund fact-checking inspections. Whether a free press arises, is captured or is closed down is an outcome to be measured (MED-* KPIs), not a design goal.

### 4.7 Order of work per day (deterministic)

1. Perception and NPC observation (existing, per tick).
2. Recorders write records for the day's events, institutions in ascending `InstitutionId`, staff in ascending `NpcId`.
3. Scheduled collection tasks complete (survey returns, census households, inspections), producing records.
4. Report builders emit `Measurement`s for periods that closed, with their lag.
5. Outlets issue publications, in ascending `OutletId`.
6. NPC exposure and belief updates, NPCs in ascending id.
7. `ViewFiller` refreshes each faction's `FactionKnowledgeView` (section 4.8).
8. `KpiReported` events and the daily snapshots go to telemetry.

### 4.8 `FactionKnowledgeView`

**Rule:** the HUD and every `IFactionPolicy` read this interface and nothing else about the world. The `ViewFiller` (in `Sim/Information`, the only reader of both truth and records on the player side) builds it.

```csharp
public interface IFactionKnowledgeView
{
    int  Faction { get; }
    KnowledgeProfile Profile { get; }             // per-domain mode (below)
    int  AsOfTick { get; }                        // the sim tick the view was last refreshed

    // 1. The faction's own books. Always exact: the state knows its own treasury and contracts.
    TreasuryView Treasury { get; }                // stock per ware, committed, owed, wage arrears
    ReadOnlySpan<SiteView>     Sites { get; }     // designated sites, contract state, labour assigned
    ReadOnlySpan<PostingView>  Postings { get; }  // jobs, purchase offers, tax rates and policy settings in force

    // 2. What the state can see of territory (own area, plus what its people have seen elsewhere).
    TerritoryView Territory { get; }              // owned hexes, buildings, roads, visible foreign assets with AsOfTick each

    // 3. What the institutions report: every KPI, any scope the breakdown allows.
    bool TryKpi(int kpiId, in KpiScope scope, out KpiValue value);
    int  Series(int kpiId, in KpiScope scope, Span<KpiValue> into);       // oldest first, up to into.Length periods
    ReadOnlySpan<CitizenView> Citizens { get; }   // register: id, age band, role, household. Empty when no register exists.

    // 4. Provenance, so the player can judge quality.
    ReportingSource Source(int kpiId);            // which institution, method, last period, coverage
}

struct KpiValue
{
    int  Value; int Lo, Hi;                       // fixed point per KPI; Lo == Hi == Value when exact
    int  PeriodEnd; int AsOfTick;                 // when it describes, and when it became readable
    ushort Coverage;                              // 0-1000
    KnowledgeQuality Quality;                     // Exact, Counted, Estimated, Hearsay, Stale, Missing
    bool SeriesBreak;                             // method or coverage changed in this period
}
```

**Profile:** `KnowledgeProfile` holds a mode per **domain** (treasury, population, health, crime, justice, military, economy, media, environment, politics). Modes:
- `FullInformation`: `ViewFiller` computes each KPI from layer 1 through the shared KPI functions, with `Coverage = 1000`, `Lo = Hi`, zero delay, `Quality = Exact` (rounded to the KPI's unit). This is the **Stage 4 default**. "Almost completely" means:
  - **included:** the faction's own books, own population and institutions, and everything the KPI catalogue defines for the own faction;
  - **excluded even here:** NPC minds (beliefs, memories, private intentions), other factions' internal books, and territory the faction's people have not seen. Foreign data follows the current visibility rules.
  - **Foreign information (owner decision Q21, 27 Nov).** `FullInformation` never gives unrestricted access to foreign factions' internal affairs. The existing visibility rules for foreign territory and organisations stay. What a faction knows about another comes only through **observation** (its people and soldiers see it), **trade** (envoys and traders), **diplomacy** (what the other side says, which may be false), **migration** (what immigrants and returning emigrants report), **communication** (gossip that crosses borders), **media**, **intelligence** (later: spies and scouts as staff) and **official reporting** (what the other faction publishes). Each foreign item in the view (`ForeignKnowledge`) carries its source channel, `AsOfTick` and confidence, and can be wrong or stale. A faction's own treasury and records may be near-complete at first.
  - **True, known and believed stay apart.** The architecture keeps three things distinct: what is **true** (layer 1), what a faction **knows** (records and measurements it holds, layers 4 and 7) and what it or its people **believe** (layers 3, 5 and 6). The view never fills a foreign figure from layer 1.
- `Reported`: the view shows only `Measurement`s that institutions have produced, with their lag, coverage and intervals; a KPI no institution measures is `Missing`. Later ages use this per domain, so a faction can have exact treasury books and a hazy crime picture.

The code path is identical in both modes. In `FullInformation` the same report builders run with the degradation parameters set to zero ("perfect recorder"), so reports and records still exist, are still emitted as events, and `Reported == Actual` to rounding (the test in section 10).

**Fill per age:**

| Age | Main sources of the view | Typical quality |
|---|---|---|
| Band | elders' tally and register, healer's log, store keeper's stock count, guards' observations, informal lending ledger | counted or hearsay, daily to weekly, patchy at the edges |
| Village | granary and welfare ledger, headman's court records, militia roll, tax roll, first notice boards | counted, consistent for own settlements |
| Town | census, courts, police, hospital registers, bank books, first surveys | counted and estimated, with intervals, some falsification |
| City | statistics office (censuses, surveys), regulators' inspections, central bank, media | estimated with explicit intervals, independent cross-checks |

**Debug "true state" toggle (owner decision Q22, 27 Nov).** Off in normal competitive play.
- `GameSetup.AllowTrueStateOverlay` is **false** in normal competitive play (VP mode against AI or humans). It is **allowed** (true by default) in developer mode, lab experiments, debugging and test runs, and in dedicated observation scenarios. **Sandbox scenarios may optionally enable it** (a scenario and settings option, default off). A HUD toggle shows it when allowed.
- When on, the HUD asks a **separate** `ITrueStateOverlay` (also built by the `ViewFiller`) for each figure's actual value and shows it beside the reported one, with the gap.
- The overlay is **view-side only.** `IFactionPolicy`, `IInstitutionPolicy`, `IMilitaryPolicy` and `INpcPolicy` implementations never receive it: **ordinary faction AI and NPC policies never get debugging information**, so toggling it can't change a decision. It carries no sim state, is not saved, and the state hash is the same with it on or off (tested).
- A teaching mode can also show the inspector card with an NPC's beliefs next to the true state. The NPC's own decisions are not affected.
- A test harness may run a **policy with truth** (`TruthCheatPolicy`) only in a named test scenario, to measure the value of information (policy experiments). It is registered separately and cannot be selected in a normal game.

## 5. Interactions with other systems

| System | Link |
|---|---|
| KPIs | Every KPI has an **actual** formula (over truth) and a **reported** formula (over records). One set of pure integer functions in `Sim/Information/Kpi` serves both, so they cannot drift. See [KPI_CATALOGUE.md](KPI_CATALOGUE.md). |
| Warehouse | Records, reports, audits and publications are emitted as domain events and snapshots. Tables (names from the warehouse's published list): `fact_record`, `fact_record_audit`, `fact_kpi` (actual and reported side by side), `fact_return` (managers' returns), `fact_belief_sample`, and from Stage 7 `fact_media_publication`, `fact_exposure`, `fact_outlet_day`. See [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md). |
| Institutions | Staff, budget and powers for data collection; recorders' motives; audits that change `RecordAudit`; the statistics office and media institutions in the City age. See [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md). |
| Cognition and social | Layer 6 is `Mind.Beliefs`; a published claim is a belief with source `Published`. Gossip (layer 3) and media (layer 5) are separate channels that can reinforce or contradict each other. |
| State | Policy levers (tax, wages, funding a census or outlet) are commands through the view's consumers; their effects are seen through the view with its delays. |
| Policy and training | The observation of `IFactionPolicy` is built from the view, so a trained policy trains on delayed, coarse figures once the view is narrowed. See [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md). |
| Experiments | A neutrality note: information quality is an experimental factor (treatment: census frequency; outcome: gap between reported and actual). See [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md). |

## 6. Player and view

- **Reports panel (M13b).** A list of KPI cards grouped by domain. Each shows the figure, its interval, `as of` age, `Quality` and a coverage mark; a series-break marker on the sparkline when the method changed. A card opens the drill-down described in [KPI_CATALOGUE.md](KPI_CATALOGUE.md#7-dashboards).
- **Provenance tooltip.** Which institution reported it, how (tally, census, survey), the period, the sample size.
- **Collection controls.** A panel to fund and schedule a census, a survey or an inspection, with its cost and the expected interval width.
- **Media panel (later).** Outlets, owners, reach, perceived credibility, and (if the overlay is on) accuracy.
- **True state overlay.** A toggle shown only where the overlay is allowed (developer mode, lab, debugging, observation scenarios, and sandbox scenarios that enable it; never normal competitive play, Q22). When on, each card shows an actual value and a coloured gap.
- **Animations** show events only: a crier calling, a survey taker visiting a hut, a clerk writing, a notice posted. They decide nothing.
- The HUD must show *uncertainty honestly*: a `Missing` card says "nobody measures this", never zero.

## 7. Telemetry

Domain events (see [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) section 8; additions are marked new):

| Event | Fields |
|---|---|
| `KpiReported` | kpi, scope, period, measured value, lo, hi, producer, coverage, method, published tick |
| `ReturnFiled` | institution, manager, kpi, period, returned value, record-based value; actual value (audit only). Owned by [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) |
| `RecordWritten` (new) | record id, institution, kind, measure, value, observer, observed and recorded ticks |
| `RecordFalsified` (new, audit stream only) | record id, true value, written value, recorder |
| `CollectionRun` (new) | institution, activity (tally, census, survey, inspection), scope, sample size, cost |
| `AuditFinding` (new) | inspected institution, records checked, discrepancies, penalty |
| `PublicationIssued` (new) | outlet, publication id, kind, subject, value, stated confidence, falsity tag (audit stream) |
| `MediaExposure` (new, sampled) | npc, outlet, publication, resulting belief confidence |
| `PolicyChanged` | existing; used as a marker on dashboards |

Snapshot additions: a daily `fact_institution_day` row per institution gets `records_held`, `collection_tasks_open`, `mean_report_lag`; a daily `fact_outlet_day` row (Stage 7) holds reach share, perceived credibility, accuracy, audience.

Audit-stream events and fields (`RecordFalsified`, true `Falsity` tags, the actual value on `ReturnFiled`) are analytics-only truth fields, marked `audit_only`. No policy or view ever reads them; recorders store them at the `Full` recording level only ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)).

## 8. Stage 4 minimum vs later stages

| Part | Stage 4 | Later |
|---|---|---|
| **M1: the seam** | `IFactionKnowledgeView` with `FullInformation`; `ViewFiller`; the HUD and `AiBrain` wrapper read only the view; reflection test; foreign items only through `ForeignKnowledge` with a source channel (Q21, I19); the overlay allowed only in developer, lab, debugging and observation scenarios and in sandboxes that enable it (Q22, I18) | per-domain `Reported` mode (Stage 6–7); a limited-information option for competitive play (Stage 7) |
| **M13b: records and reports** | `InfoRecord`, `RecordAudit`, `Measurement`; the elders' tally, healer's log, store count and guards' observations as recorders; the degradation pipeline wired with a "perfect recorder" default; Band-age KPI set (see [KPI_CATALOGUE.md](KPI_CATALOGUE.md#8-the-stage-4-minimum)); reports panel; `KpiReported` and `RecordWritten` events; debug overlay | census, surveys, inspections (Village–Town); statistics office (City) |
| Falsification | the mechanism and the audit sidecar exist and are tested, with a falsifying recorder available as a test scenario; default honest | staff motives tied to targets once institutions have KPI directives (Stage 6) |
| Detection effects | modelled for theft and violence observed (guards' detect rate) | health, courts, finance |
| Media | `Publication` and `Outlet` types, the notice-board postings (exist already) as the one outlet kind; not exercised beyond that | crier and notice boards (Village); press (Town); broadcast, media institutions, regulators (City) |
| Belief effects of media | none beyond postings | outlet trust, confirmation bias, misinformation (Stage 6–7) |

## 9. Migration from the existing code

- The current HUD reads `GodState` directly. M1 introduces the view and ports each HUD panel and `AiBrain` input to it. Where `AiBrain` reads state it will not be able to read through the view (internals of foreign factions), it keeps the current visibility rules.
- Existing metrics in `SmokeRun` and the JSONL runner become `KpiReported` events (M0 to M13).
- `Telemetry` stays the single source for the warehouse. Records are not reconstructed from the warehouse.

## 10. Tests and acceptance criteria

| # | Test | Pass criterion |
|---|---|---|
| I1 | `View_Only_Reads` (reflection) | No type in `Game.Hud*` or the `Policy` namespaces (all four interfaces and implementations, including `AiBrain` wrapper) has a field, parameter, local, generic argument or method call target of `GodState`, `EconomyState`, `PopulationState`, `RecordAudit` or `ITrueStateOverlay`. Only `ViewFiller` and report builders may. |
| I2 | `FullInformation_Equals_Truth` | For every KPI in the catalogue at every period end over 5 sim years and 5 seeds, the measured value in `KpiReported` (and the view) satisfies `abs(measured - actual) <= 1` unit of the KPI's last digit (rounding). `Lo == Hi`, `Coverage == 1000`, `Quality == Exact`. Managers' returns (`ReturnFiled`) are excluded: they may differ from the actual value, and that gap is measured separately. |
| I3 | `Survey_Error_Shrinks` | True share 300‰, `N = 2000`, 500 repeats for each of `n` = 50, 200, 800. The RMS error ratio `RMSE(50)/RMSE(800)` is within 4.0 ± 25% (≈ `√16` with finite-population correction); `RMSE(200)/RMSE(800)` is within 2.0 ± 20%. 95% intervals contain the truth in 93–97% of repeats. |
| I4 | `Survey_Full_Frame_Exact` | A survey with `n = N` and response 1000‰ returns the true value ± rounding, with interval width 0. |
| I5 | `Survey_Nonresponse_Bias` | With `respond` dropping to 400‰ among aggrieved persons and a true aggrieved share of 30%, the estimated share of "has a grievance" is biased low by at least 100‰ relative to a 1000‰ response run; it falls toward the truth as trust is raised. |
| I6 | `Falsifier_Creates_Gap` | One recorder with `MaxShade = 150‰`, falsifying 100% of records, on 400 theft records: `reported / actual` within 850‰ ± 30‰; `RecordAudit` shows 400 `Falsified` rows; readers cannot distinguish (no field of `InfoRecord` differs in type). With an inspection intensity of 500‰ the falsification share falls (the decision model reacts) and the mean gap shrinks monotonically over three intensities. |
| I7 | `Better_Counting_Looks_Worse` | Same seed, `actual` theft is forced identical (replayed events). Raising guard `detect` from 300‰ to 600‰ doubles the *reported* theft count ± 10%, while `actual` is unchanged. The view marks a series break at the change. |
| I8 | `Delay_Is_Respected` | No `Measurement` is readable through the view before its `PublishedTick`; with `d = 2` days every KPI's `AsOfTick` lags `PeriodEnd` by ≥ 2 days. |
| I9 | `Loss_Matches_Coverage` | With `loss = 200‰` over 1000 items, `Coverage` is 800‰ ± 40‰ and the count is within 800 ± 40. |
| I10 | `Noise_Is_Unbiased` | With symmetric noise ±100‰ the mean reported error over 500 records is within ±10‰ of zero. |
| I18 | `Overlay_Off_In_Competitive` | Loading a competitive VP scenario gives `AllowTrueStateOverlay = false` and the HUD toggle is absent; a sandbox scenario has it false unless the scenario or setting enables it; developer, lab and observation scenarios allow it (unit test on the setup) |
| I19 | `No_Foreign_Truth_In_View` | Under `FullInformation`, changing a foreign faction's treasury, population or institution records that no channel has reported leaves every field of the viewing faction's `FactionKnowledgeView` unchanged (the M15.5 leak test, extended to the view); each `ForeignKnowledge` item names a source channel |
| I11 | `Overlay_Does_Not_Change_Sim` | Two runs, same seed and commands, one with the true-state overlay toggled on and off every day: identical `StateHash` at every checkpoint. |
| I12 | `Recording_Off_Same_Hash` | Warehouse recording off vs on: identical `StateHash`. (Records are sim state and appear in both.) |
| I13 | `Records_Deterministic_And_Saved` | Save/load mid-run, continue: identical hash to the uninterrupted run. Two runs with the same seed draw identical survey samples. |
| I14 | `Media_Reach_Limits_Beliefs` | An outlet with reach on 10 hexes: no NPC outside them gets a `Published` belief from it; inside, the exposed share is `ReachShare` ± 5% over 20 days. |
| I15 | `Credibility_Is_Learned` | An outlet with `Accuracy` 300‰ sees mean `trust_o` fall below 400 after 30 verified claims per NPC; an outlet at 900‰ rises above 700. A zero-trust outlet moves a belief's confidence by at most 10‰. |
| I16 | `Dual_Channel_Beliefs` | A false published claim and a true gossip item about the same subject yield a belief with the confidence rule in 4.6 (golden values). |
| I17 | `Band_KPI_Pipeline` | The Band KPI set (KPI catalogue) is present in the reports panel data for a 20-year run, each with source and `AsOfTick`, in both `FullInformation` and a degraded `Reported` profile; `Missing` appears only for KPIs nobody measures. |

## 11. Open questions

No open owner question remains. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
- **Q21 Foreign information under `FullInformation`:** no unrestricted access to foreign factions' internal affairs. The existing visibility rules for foreign territory and organisations stay. Foreign information comes through observation, trade, diplomacy, migration, communication, media, intelligence and official reporting. A faction's own treasury and records may be near-complete at first. The architecture distinguishes what is true from what a faction knows or believes (4.8; test I19).
- **Q22 Debug true-state overlay:** off in normal competitive play. Allowed in developer mode, lab experiments, debugging and dedicated observation scenarios; sandbox scenarios may optionally enable it (changed from "on in sandbox by default"). Ordinary faction AI and NPC policies never get debugging information (4.8; tests I11, I18).
- **Further requirement (27 Nov):** restricted NPC observations and read-only policy views are enforced from the start (M1, I1), and institutional returns stay distinct from independently calculated KPIs and from true outcomes ([KPI_CATALOGUE.md](KPI_CATALOGUE.md#2-principles)).

Settled here:
1. **View refresh cost.** Each KPI refreshes on its own period; the faction's own books are live. Measured in M13.
2. **The audit sidecar is hashed.** `RecordAudit` is sim state, so falsification is replay-checked. Its rows reach the warehouse only at the `Full` recording level.
3. **Census in the Band age.** Band elders tally and register only; the first census is a Village-age activity (section 4.2).
4. **Reading ability.** Reading a written outlet is a skill in `Population` skills, spread by teaching through [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md). It matters only from the Town age.

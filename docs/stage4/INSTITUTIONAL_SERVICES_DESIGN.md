# Institutional services design (owner D5, 26 Nov 2026)

One service model for everything an individual or an organisation does for someone else on request: treating the injured, teaching, handing out from a store, minding children, lending goods, hearing a complaint. Each service changes real state (health, learned capability, goods, records), through real people, time and goods, at a real place. The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); owner decisions are in [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md), which wins over every design document.

**Owner requirements worked out here:** brief §19 (healthcare), §20 (education), §21 (banking and finance), §22 (courts, policing, prisons), §24 (other institutions); decisions D5.6 (no abstraction does the work), D5.16 and D5.17 (services by independent organisations of every ownership model), D5.28 (officials see only what they know).

## 1. Purpose and scope

**This document owns:**
- the common service model: request → queue → activity (staff time and resources at a place within reach) → outcome (a change in real state) → record;
- informal services (one NPC for another, no organisation) and organised services (through an organisation of [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md));
- capacity, unmet demand and how both are measured;
- the first concrete services, each with its tier and minimal first version: healthcare, education, communal stores and welfare, childcare, lending and deferred obligations, justice;
- pointers for infrastructure, environmental management, media, statistics and research, political organisations, trade associations and workers' organisations.

**It links to, and does not repeat:**
- the organisation record, roles, decisions, ledger and records: [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md);
- manager and staff effort, returns, Goodhart's law, inspection, failure modes, unlock rules by age: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- disease, injury, health and mortality: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- care of the sick, child-rearing and shared childcare: [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md);
- associative learning, observation and imitation: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md); teaching and traits: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- promises, trust, reputation, gossip: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- exchange, loans as deferred exchange, theft: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- laws v1, fines, guards, the granary and handouts: [STATE_DESIGN.md](STATE_DESIGN.md); prisoners of war: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- the physical actions and reach: [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md); goods API: [SHARED_CONTRACTS.md §2](SHARED_CONTRACTS.md#2-inventories-and-goods-movement);
- divine healing, revelation of evidence and other miracles: MYTHIC_AND_MIRACLES_DESIGN.md and DIVINE_PLAYER_DESIGN.md;
- KPI definitions: [KPI_CATALOGUE.md](KPI_CATALOGUE.md); event codes: [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md).

## 2. Principles
1. **A service is an action by a person.** Every outcome comes from a provider's physical action within reach of the recipient or the goods: tending beside the patient, demonstrating where the learner can see, handing over food. No organisation, building or policy produces an outcome on its own. A hospital with no healer heals no one.
2. **Outcomes change real state, never a multiplier.** Healthcare changes the patient's actual illness and injury progress; education changes the learner's actual skill and associations; welfare changes what the recipient actually holds and eats; justice changes goods, records, liberty and beliefs. There is no faction-wide health, education or order bonus (brief §19).
3. **Begin informal, grow formal.** Every service works first between two individuals with no organisation (a neighbour tends the sick; a parent teaches; a friend lends). An organisation adds a place, a queue, a store, records, specialised staff and rules. The same activity code serves both.
4. **Capacity is physical.** Staff ticks, places (beds, seats, cells), goods in the store and reach bound how many can be served. Excess demand waits, is refused or goes unmet, and all three are measured.
5. **Providers see only what they know.** A healer judges illness from what it perceives (visible signs, the patient's gestures); a judge decides from evidence it has perceived or been told; a lender judges credibility from its own memory and gossip. None reads true state (D5.28). Mistakes, misdiagnosis and wrongful accusation are possible and measured.
6. **Claims never create goods.** A loan, a fine, a fee owed or a restitution order is an obligation. Only hand-overs settle it (brief §21).
7. **Every ownership model can provide every service.** A private healer, a communal healers' house, a temple infirmary and a state clinic use the same activity, queue and record code. Which serves people better is measured.
8. **Determinism.** Requests are processed in ascending request id; activities run inside the provider's own intents in tick order; draws use `Rng.Fork("svc", orgId or providerNpcId).Fork(kind)`.

## 3. The common service model

### 3.1 Data structures

```text
public enum ServiceKind : byte {
    Treat = 1, Teach = 2, Apprentice = 3, Ration = 4, MindChild = 5, Lend = 6, Collect = 7,
    Mediate = 8, Hear = 9, Investigate = 10, Detain = 11, Rite = 12,         // Rite: COMMANDMENTS_AND_RELIGION
    // 13+ later: Build (infrastructure), Ward (commons), Publish, Census, Represent
}

ServiceRequest                              // in the provider organisation's ring (<= 256), or in the provider NPC's mind if informal (<= 4)
  Id: RequestId (new id space, never reused), Kind: ServiceKind
  Requester: NpcId (or OrgId with a flag), Subject: NpcId        // a carer may request for a patient; a parent for a child
  Provider: OrgId (0 = informal), ProviderNpc: NpcId (0 = whoever takes it)
  Place: node                                // where it must happen
  Opened (tick), Expires (tick)
  SeenNeed: short                            // the provider's estimate, from what it perceived (noisy)
  Topic: int                                 // what is asked: an illness or wound, a skill id, a ware and count, a complaint id
  Status: Waiting | Active | Done | Refused | Dropped | Expired
  QueuePos: short                            // by the organisation's queue policy
  Outcome: OutcomeRef                        // the state change event id (below)
  RecordedOutcome: short                     // what the recorder wrote; may differ from the truth

ServiceCapacity                             // per organisation per kind; recomputed daily
  StaffTicksAvailable, PlacesFree (beds, seats, cells), StoreCover (days of the needed goods)

ServiceActivity                             // a provider's ordinary intent, with the request id as purposeRef
  ProviderNpc, RequestId, Verb (Treat, Teach, Demonstrate, HandOut, Mind, HandOverLoan, Hear, Inspect, Guard ...)
  Since (tick), TicksDone, GoodsUsed: (ItemLotId, count)[<= 3]
```

The `Case` record of INSTITUTIONAL_STATE §3 **is** the `ServiceRequest`: its fields `SeenDifficulty`, `SeenNeed`, `TrueOutcome` and `RecordedOutcome` keep their meaning, and the true need stays analytics-only (it is the subject's actual body or holdings, which the sim knows and the provider does not).

### 3.2 The five steps
1. **Request.** The requester must be perceived by the provider: it walks to the provider or its place and gestures (`NeedHelp`, `NeedFood`, `Accuse`, `Want` with the topic), or a carer carries or leads the subject there. A provider may also open a request itself for someone it sees in need (a healer sees a wounded person). An organisation's request ring is written by whoever received the request (a role-holder); an unrecorded request is still a request in the provider's mind, but the organisation does not know of it.
2. **Queue.** Waiting requests are ordered by the organisation's queue policy (`OrgPolicy`: first come, most severe seen, kin first, fee paid first, members first), applied by the provider choosing its next case. Where the policy allows choice, the handler picks (INSTITUTIONAL_STATE case selection, where Goodhart's cherry-picking lives).
3. **Activity.** The provider walks to the place, is within the service's reach of the subject (`Reach.Touch` for treatment, the learner's sight for demonstration, `Reach.HandOver` for goods), spends ticks and uses goods (`Goods.Consume` with a service reason, or `Goods.Move` to the recipient). It can stop at any think: called away, tired, unpaid, afraid.
4. **Outcome.** The activity calls the owning system's ordinary state change (section 3.3). Its event carries the request id.
5. **Record.** A role-holder with `KeepRecords` may write it (`RecordWritten`). The organisation's view of its own performance comes only from these records and from returns.

### 3.3 Outcomes reuse existing state changes

| Service | State changed | Existing code it extends |
|---|---|---|
| Treat | illness severity and days left, injury recovery | `Disease.Daily` and `Life.TendedDay` (`Assets/God/Sim/Population/Households/Disease.cs`); `Tend` in `HouseholdActs.cs`; `Npc.Injury`, `Npc.Health` (`Assets/God/Sim/Population/PopulationState.cs`) |
| Teach, Apprentice | the learner's skill record and association store | `AssociationStore` with `Origin.Imitated` (`Assets/God/Sim/Learning/AssociationStore.cs`); `Learner` (`Learner.cs`); skills (POPULATION_DESIGN) |
| Ration, MindChild | goods held and eaten; care given | `Goods.Move(HandOver)`; `CareGiven`, `CareArrangement` (`Assets/God/Sim/Population/Households/HouseholdState.cs`) |
| Lend, Collect | lot owner or holder; formal obligations | `Goods.Move`, `Goods.SetOwner`, `OwnershipBasis` (`Assets/God/Sim/Population/Goods.cs`); `DealId` (`Assets/God/Sim/Core/GodState.Social.cs`) |
| Mediate, Hear, Investigate | beliefs (messages), records, goods (restitution, fines) | `GestureAct.Accuse`, `Apologise`, `Forgive` (`Assets/God/Sim/Comms/Message.cs`); `SocialView` (`Assets/God/Sim/Social/SocialView.cs`) |
| Detain | where a person may go; rations given | `PrisonerRecord` (MILITARY_DESIGN; `Assets/God/Sim/Military/MilitaryState.cs` when built) |

### 3.4 Unmet demand and capacity (measured, never rewarded)
- **Recorded unmet demand:** requests `Refused`, `Dropped` or `Expired`, per organisation per day, from its records.
- **True unmet demand (analytics only):** people whose actual state needed the service (ill or injured above a threshold; hungry with no food; a theft victim) and who were not served within `UnmetDays`, whether or not they asked. Computed by telemetry from true state. No policy reads it.
- **Capacity:** staff ticks available, places free and store cover, per kind, in the daily organisation snapshot. Utilisation and queue length are reported side by side.

### 3.5 Who provides, and how it is paid
- **Informal:** any NPC may provide to anyone it chooses, by its own scores (kin, trust, obligation owed, learned outcome, generosity), for nothing, a gift back later, or an agreed barter.
- **Organised:** a role-holder provides as part of its role. Funding follows the organisation's ownership model ([ORGANISATIONS §4.6](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#46-budget-and-obligations)): fees in goods handed over by the recipient, member contributions, a government budget line, offerings.
- **Fees** are offers by gesture valued by each side's own `Valuation` (`Assets/God/Sim/Social/Valuation.cs`); a recipient who cannot or will not pay may be refused, which is measured as exclusion.

## 4. Telemetry common to all services
- `ServiceRequested`, `ServiceQueued`, `ServiceStarted`, `ServiceProgress` (HV), `ServiceCompleted` (with the outcome event id), `ServiceRefused` (by whom, reason seen), `ServiceDropped`, `ServiceExpired`, `ServiceFeePaid`;
- analytics-only truth fields on `ServiceCompleted`: the subject's true need before and true state after;
- daily per organisation and kind: requests opened, served, refused, expired; capacity; true unmet demand (telemetry).
Warehouse (DATA_WAREHOUSE_DESIGN to adopt): `fact_service_request` (grain: request), `fact_service_day` (organisation × kind × day).

## 5. The services

### 5.1 Healthcare (brief §19)
**Tier 2 (C7).** Begins with functional healers and informal treatment; clinics and hospitals later.

**What already exists.** M4's `Tend` (`HouseholdActs.cs`): a carer within `Reach.Touch` of an ill or injured person for `TendTicks` sets `Life.TendedDay`; that day the illness loses `TendSeverityPermille` (default 700, so 30% less) of its severity, and a tended day counts twice towards recovery (`Disease.Daily`). Injury recovery doubles when tended (HOUSEHOLDS "Care of elders, the sick and the injured").

**Minimal first version (`Treat`).**
- **Request:** the patient, or a carer carrying or leading it, gestures `NeedHelp` (topic: self or the patient) at a healer it believes in, or the healer notices visible signs (the illness and injury display levels already shown, `HouseholdHooks`).
- **Medical knowledge:** a per-NPC healing skill (POPULATION's skill record, `Skill.Healing`, 0–1000), grown by treating (each completed treatment gives skill, more when the patient recovers) and by being taught (5.2). The healer's diagnosis `SeenNeed` is the display level it perceives plus noise that falls with skill.
- **Activity:** `Treat(patient)` is `Tend` with skill and goods: `TreatTicks` within `Reach.Touch`, optionally consuming one remedy lot (a new appended ware `Herbs`, gathered from sources like roots; proposed to ECONOMY 3.2).
- **Effect on actual biology:** severity loss that day is multiplied by `TendSeverityPermille × (1000 − Skill × SkillGain / 1000 − Remedy) / 1000`, floored at `TreatFloorPermille` (default 300); injury recovery multiplied by `(2000 + Skill × InjuryGain / 1000 + Remedy)` permille. With skill 0 and no remedy, `Treat` equals `Tend`, so M4 behaviour is the baseline. Effectiveness is never 100%: patients still die.
- **Capacity:** one patient per healer at a time; a healer's house adds beds (patients resting at the house can be treated without walking, and an infected patient there no longer counts as a hut contact at home, which is how isolation lowers spread through the existing contact term).
- **Unmet demand:** recorded (refused, expired) and true (ill or injured above `UnmetSeverity` for `UnmetDays` with no `Treat` or `Tend`).
- **Prevention where supported:** the disease model already uses crowding, fouling and wound terms. A communal or state body may adopt a "keep huts clean" policy whose task (an action at a hut) lowers that hut's fouling; isolation as above. Nothing else is prevention until the model supports it.
- **Ownership models:** a private healer charges fees; a communal healers' house is funded by members; a temple infirmary by offerings; a state clinic by budget.
- **Later:** several healers, specialisation by skill, remedies as a production chain, hospitals with wards, public health.
- **Divine healing** (MYTHIC_AND_MIRACLES) is a miracle, not this service; it acts on the same health fields and is recorded separately so healthcare outcomes and miracles are never mixed in KPIs.

**Events:** `TreatmentGiven` (healer, patient, request, skill, remedy lot, severity before and after as known to the sim: truth fields), `HealerSkillChanged`, plus the common service events. `CareGiven` stays for informal `Tend`.

### 5.2 Education (brief §20)
**Tier 2 (C7).** Begins with teaching, demonstration, apprenticeship and shared knowledge; schools later.

**Minimal first version.**
- **Learners and teachers** are any NPCs; a teacher role exists in an organisation only when a school or workshop names it.
- **Demonstration (`Demonstrate(activity)`):** the teacher performs an ordinary activity (gathering with an axe, treating, building) while the learner attends to it within sight. The learner gets an observation of the activity and its consequence, which feeds the existing social-learning hook (observational learning, NPC_LEARNING M7): vicarious association updates with `Origin.Vicarious`.
- **Teaching (`Teach(learner, topic)`):** the teacher and learner are within gesture range for `TeachTicks`; the teacher's top associations for the topic's situations are copied to the learner as `Origin.Imitated` entries (`AssociationStore.cs`), each with fidelity `(TeacherSkill × Attention) / 1000` and the teacher's value scaled by the learner's trust in the teacher; and the learner's skill in the topic rises by `TeachSkillGain × fidelity / 1000`, capped below the teacher's skill. What is taught may be wrong: a teacher passes on what it learned, including mistakes.
- **Apprenticeship:** an `Apprentice` role in an enterprise or service body. The apprentice works at the master's bench under a `Work` right, so practice gives its ordinary skill growth plus a supervision bonus while the master is in sight; output belongs to the owner, as for any employee.
- **Learning outcomes are measured on the learner itself:** skill before and after; association values for the topic before and after; later performance at the task (gather rate, treatment recoveries). No "educated" flag exists.
- **Costs:** teacher and learner ticks (neither works meanwhile), food for a teacher who is paid, the goods used in demonstration.
- **Capacity:** learners within sight or gesture range of one teacher per session, bounded by `MaxLearnersPerSession` (default 4).
- **Spread and loss:** knowledge spreads only by these acts and by ordinary imitation (CULTURAL_TRANSMISSION); it is lost when holders die untaught or forget (association decay is per sim day, independent of `LifeScale`, owner Q1).
- **Later:** schools as organisations with scheduled sessions and places, curricula as organisation policies, examinations (INSTITUTIONAL_STATE's Goodhart example for teaching is kept), advanced institutions.

**Events:** `DemonstrationSeen`, `TeachingGiven` (the existing conventions name; teacher, learner, topic, fidelity), `SkillChanged`, `AssociationsImitated` (existing name), `ApprenticeshipStarted`, `ApprenticeshipEnded`.

### 5.3 Communal stores and welfare (brief §24)
**Tier 2 (C8; treatment in F2).** "Communal assistance" in brief §42.
- **Store:** an organisation's `LotBin` (`OrgStore`) with a keeper office ([ORGANISATIONS §4.5](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md#45-buildings-productive-assets-and-stores)). Members deposit by `Store`; contributions are recorded if a recorder exists.
- **Welfare (`Ration`):** an eligible requester gestures `NeedFood` at the keeper at the store; the keeper judges eligibility from what it perceives and the organisation's policy (members only, children first, anyone hungry, contributors in proportion), withdraws and hands over up to the ration. STATE_DESIGN's `HandoutRule` is the government's instance of this policy; the granary keeper is the same role.
- **Effect:** the recipient's actual food holdings and hunger; spoilage is slower in a granary building.
- **Failure modes:** free-riding (members who never contribute), favouritism, theft by the keeper, exclusion of outsiders (INSTITUTIONAL_STATE failure table).
- **Minimal first version:** the shared food store of ORG-ACC-4, with a `MembersByRation` policy.

### 5.4 Childcare (brief §24)
**Tier 2 (C8; treatment in F2).** Informal shared childcare already exists (`CareArrangement` with `Given` and `Returned` ticks, owner Q15).
- **Formal childcare** is a `ServiceBody` organisation whose charter has purpose `Service(MindChild)`: a place (a hut), one or more minder roles, and member households. A parent brings the child (a request), a minder `Mind`s and `Feed`s it within reach, and the parent works meanwhile.
- **Founding** follows the opportunity path: households whose arrangement has run for `CareFoundDays` with both sides giving care recognise the need (ORGANISATIONS §4.1).
- **Effect:** actual care received by children (hunger, tending, supervision near hazards) and parents' work time, both measured; neglect by a minder is possible.

### 5.5 Lending and deferred obligations (brief §21)
**Tier 2 for primitive lending (C2, C8); Tier 3 for banking institutions (C8).**

**Two kinds of loan, both physical:**

| Loan of | Goods move | Ownership | Obligation |
|---|---|---|---|
| A thing to be returned (a tool, a hut) | `Move(Carry(lender) → Carry(borrower), HandOver, OwnerRule.Keep)` | stays the lender's; the borrower possesses (WORLD_ONTOLOGY 6.7) | return the same lot by the due day |
| Goods to be consumed (food, logs) | `Move(..., HandOver, OwnerRule.ToReceiver)` | becomes the borrower's | hand over `Count` of `Ware` (or the agreed other ware) by the due day |

- **Principal and terms** are agreed by gesture (`Offer` and `Promise`, Time: soon or later); the deal carries a `DealId`. Between two individuals the obligation is a social-memory obligation (M7). When it is made at a lending organisation or witnessed and recorded by one, it is also a **formal obligation** (`FormalObligation`, ORGANISATIONS §3.3) with debtor and creditor, which is what claims and defaults are counted from.
- **Repayment** is a later hand-over by the borrower; partial repayment reduces the obligation by exactly what was handed over.
- **Default:** nothing repaid by `DueDay + GraceDays` (default 1). `LoanDefaulted` (formal) or the M7 telemetry classification (informal). Nothing restores the lender's goods.
- **Credibility:** the lender's own belief about the borrower: trust (`SocialView.Trust`), its memory of the borrower's past repayments, gossip, and, for a lending organisation, its records of defaults (which make a default known beyond the two parties: INSTITUTIONAL_STATE's I-LEND-1 is kept).
- **Collateral (where appropriate):** the borrower hands a lot to the lender with `OwnerRule.Keep` (the lender possesses, the borrower still owns). On repayment the lender hands it back. On default the lender may take ownership with `SetOwner` basis **`Forfeited`** (proposed `OwnershipBasis` 14, to the contracts owner). Keeping collateral after repayment is theft.
- **Investment and productive asset acquisition:** a borrower may use borrowed goods as inputs to build or buy a productive asset (a workshop site, an axe). The asset is the borrower's; the obligation is unchanged. Measured: foundings financed by loans.
- **Savings:** an NPC may deposit goods in an organisation's store as a **deposit** (a formal obligation of the organisation to the depositor) instead of a contribution. Tier 3 (deposits and banks); the first version has contributions only.
- **Institutional lending:** a lending circle or a communal store lends from its own store by the decision of the office-holder with `Spend` power, under its lending policy (members only, maximum per loan, collateral required).
- **Insolvency:** an individual or organisation that cannot settle obligations due. For individuals it is a measured state (claims due exceed holdings); for organisations it is the `Insolvent` status. Creditors' remedies are whatever justice exists (5.6); with none, only social consequences.
- **Ledger rule (brief §21):** creating, defaulting or writing off an obligation changes no goods count; every claim has an equal liability in the registry; settlement only by hand-over (ORG-I2). Interest, monetary accounting, sophisticated credit, deposits and banks are Tier 3 and use the same registry.

**Events:** `LoanAgreed` (DealId, lender, borrower, ware, count, due, collateral lot), `LoanHandedOver`, `LoanRepaid`, `LoanPartlyRepaid`, `LoanDefaulted`, `CollateralPledged`, `CollateralReturned`, `CollateralForfeited`, `InsolvencyObserved`, plus `OrgObligation*` from ORGANISATIONS.

### 5.6 Justice: complaints, arbitration, restitution and detention (brief §22)
**Tier: informal mediation 2 (C9, low priority); formal courts, policing and detention 3 (C9).**

**Crime is relative to rules.** An act is a **crime** in the sim's truth only when it breaks an enacted rule of a body with jurisdiction over that place and person: a government law (STATE_DESIGN laws v1) or a rule adopted by a communal organisation for its members and property. It is a **norm violation** when it breaks a cultural norm the observer holds; that exists only in minds. `CrimeCommitted` (truth) names the rule broken; witnesses' observations name what each saw.

**The process, each step an NPC action:**
1. **Complaint:** the victim or a witness walks to a mediator, elder or judge (or the body's place) and gestures `Accuse` (topic: the accused, the act, a ware). `ComplaintMade`. Nothing happens without one, unless an enforcer witnessed the act itself.
2. **Witnesses and evidence:** the official asks known witnesses (gesture `Want` with the complaint topic); each witness answers from its own memory, may forget, misremember, lie or refuse. Physical evidence is what the official itself perceives by `Inspect` (a lot in the accused's carry whose owner tag is visible, a broken store). Evidence items are kept in the official's mind and in the case record: `EvidenceItem { Source: Saw | ToldBy(npc) | Inspected | Divine, Claim, Confidence }`. A divine revelation to a judge (brief §4 "reveal evidence to a judge") adds an item with `Source = Divine`, which the judge weighs by its own belief in and trust of the god; it may disbelieve it.
3. **Investigation** (Tier 3): an investigator role walks to places, inspects and asks, adding evidence.
4. **Arbitration and decision:** the official decides from the evidence it holds, with its own interests (kin weight, bribes, fear of the accused, trust in the parties) through `IInstitutionPolicy` as itself, or by the body's rule (a panel of elders). The ruling is a gesture message and a record: `Ruling { Complaint, Finding: Guilty | NotGuilty | NoFinding, Remedy }`. It never reads truth; **wrongful findings are possible**, and the analytics-only truth field `TrueOffender` lets reports measure them.
5. **Remedies (formal imprisonment is not the only response):** apology and forgiveness (gestures), restitution (return of a lot, or goods of the victim's choosing, as a formal obligation), a fine (a formal obligation to the body), compensation, exclusion from a commons or organisation (a role ended), exile (an order to leave, enforced only if someone enforces it), detention.
6. **Enforcement:** compliance is voluntary unless an enforcer acts. A guard (an organisation role) may take the owed goods from the offender's carry within reach (basis `Seized`), which the offender may resist (a contest, GAME_THEORY). Refusing a ruling costs reputation with those who know.
7. **Detention** (Tier 3) needs **real facilities, guards and food**: a building with `Cells` capacity owned or used by the body; a guard role on shifts within sight of the cells (otherwise the detained person may walk out, as MILITARY's prisoner escape rule); a ration per detainee per day handed over from the body's store (otherwise the detainee goes hungry like anyone). `PrisonerRecord` is reused with `HeldBy = OrgId`.
8. **Rehabilitation** (Tier 3): teaching or work inside (5.2 activities at the detention place).

**Officials make mistakes and can be dishonest:** misrecognition, missing witnesses, false testimony, bribes (`BribeOffered`, `BribeTaken`), favouring kin, discouraging complaints. The Goodhart and oversight machinery of INSTITUTIONAL_STATE applies unchanged.

**Minimal first version (Tier 2, low priority):** communal mediation by an elder (INSTITUTIONAL_STATE M12 Band-age mediation) through this model: complaint, the elder hears both parties and any witness present, a ruling message, voluntary restitution. No detention.

**Events:** `ComplaintMade`, `WitnessAsked`, `TestimonyGiven` (truth field: honest), `EvidenceObtained`, `RulingMade` (truth field: correct), `RestitutionOrdered`, `RestitutionMade`, `FineLevied`, `FineRefused`, `GoodsSeized`, `Detained`, `DetentionEnded`, `DetaineeEscaped`, `RationGiven` (detention).

### 5.7 Pointers to later services (mostly Tier 3)

| Service | Built on | Tier | Notes |
|---|---|---|---|
| Government and treasury | ORGANISATIONS §4.10, STATE_DESIGN | 2 (government org), 1 (existing treasury rules) | Not a service; listed for completeness |
| Infrastructure | STATE_DESIGN sites and contracts; ECONOMY 4.9 public goods | 3 beyond what exists | A public-works crew is a `ServiceBody` whose activity is building at designated sites; communal and private provision use the same sites |
| Environmental management | ECONOMY 4.8 `Commons` record and norms | 3 | A `Commons` organisation with warden roles who monitor takes (perception) and apply graduated sanctions (5.6 remedies); harvest rules as organisation policies |
| Media | INFORMATION_AND_MEDIA_DESIGN layer 5 | 3 | A crier or notice keeper role publishing messages with reach and credibility |
| Statistics and research | INFORMATION_AND_MEDIA_DESIGN; organisation records | 3 | A census is a service whose activity is visiting households and recording; reports can be wrong (D5.28) |
| Political organisations | POLITICAL_ECONOMY associations, petitions | 3 | `OrgType.Association` with `Represent(issue)` purpose; petitions are requests to the government organisation |
| Trade associations | POLITICAL_ECONOMY 4.9 (guild, cartel) | 3 | `RestrictEntry` and `SetPrices` purposes as organisation policies followed by members by their own scores |
| Workers' organisations | POLITICAL_ECONOMY 4.7, ECONOMY Q19 suspension | 3 | A strike fund is an organisation store; a walk-out is members' individual `RefuseWork` |
| Religious services | COMMANDMENTS_AND_RELIGION_DESIGN | 2 (basic religious beliefs) / 3 (advanced institutions) | `Rite` requests and activities at a temple through this model |
| Military service | MILITARY_DESIGN | 3 (expanded) | Not a request service; uses the organisation roles and pay |

## 6. Player and view
- **The god sees services in the world:** healers kneeling beside patients, teachers demonstrating, keepers handing out food, elders hearing disputes, each animated from service events (NPC_ANIMATION_DESIGN).
- **Organisation card** (ORGANISATIONS §6): queue, capacity, served and refused counts **as the organisation recorded them**, beside the divine-observation figures under the configured rules; never mixed.
- **NPC inspector:** requests the NPC made or handles, treatments received, what it was taught and by whom, loans as it believes them, complaints and rulings that concern it.
- **Divine actions related to services** (owned by the divinity documents): suggest that an NPC become a healer or teacher; command a healer to treat someone; reveal evidence to a judge; heal by miracle. Each works through the individuals; none fills a queue or changes a record directly.

## 7. Invariants
| # | Invariant |
|---|---|
| SVC-I1 A provider for every outcome | Every `ServiceCompleted` has a provider NPC alive and within the service's reach of the subject (or goods) during the activity ticks. |
| SVC-I2 Real state only | No service changes a faction-level or organisation-level modifier; every outcome event is one of the owning systems' state changes (table 3.3). A static test finds no health, skill or loyalty multiplier keyed by organisation or faction. |
| SVC-I3 Conservation | Goods used by services are `Consumed` or `ItemMoved`; C1–C6 hold. Loans, fines, fees and restitution orders create no goods (ORG-I2). |
| SVC-I4 Information separation | Diagnosis, eligibility, credibility and rulings read only the provider's own mind, the evidence it holds and the organisation records it may read (reflection test). `TrueOffender`, true need and true severity are analytics-only. |
| SVC-I5 No staff, no service | With no provider, zero services of that organisation complete (I-INV-2 kept). |
| SVC-I6 Detention needs resources | No `Detained` without a cell place free and a guard role filled; every detainee-day has a `RationGiven` or a recorded missed ration. |

## 8. Tier and package mapping

| Service or capability | Tier (brief §42) | Package | Minimal first version |
|---|---|---|---|
| Common service model (request, queue, activity, outcome, record) | 2 | C6 (framework), C7 (first users) | Requests in the organisation ring and informal requests in the mind; first-come and most-severe queues |
| Healthcare | 2 | C7 | `Treat` = `Tend` + healing skill + optional remedy; private and communal healer |
| Education | 2 | C7 | `Demonstrate` and `Teach` with imitated associations and skill gain; apprenticeship role |
| Communal stores and welfare | 2 | C8 | Shared food store with keeper and ration policy |
| Childcare | 2 | C8, F2 | Formal minding body grown from `CareArrangement` |
| Primitive lending and deferred obligations | 2 | C2, C8 | Formal obligations for recorded loans; repayment, default, credibility; tool loans with `Keep` |
| Collateral, insolvency | 2 (collateral), 3 (insolvency procedures) | C2, C8 | `Forfeited` basis; insolvency as a measured state |
| Deposits, interest, banks | 3 | C8 | Same registry |
| Mediation | 2 (low priority) | C9 | Elder hears complaint, ruling message, voluntary restitution |
| Courts, investigation, policing, detention, rehabilitation | 3 | C9 | As 5.6 |
| Infrastructure, environment, media, statistics, political, trade and workers' organisations | 3 | C5, C10 | Pointers only (5.7) |
| Effects of healthcare, welfare and childcare on survival and fertility | 2 | F2 | Used as treatments in viability experiments |

## 9. Stage 4 milestones and later
- **M4 (built):** `Tend`, `CareGiven`, `CareArrangement`, disease and injury that tending changes; the base all healthcare and childcare extend.
- **M7 (planned):** promises, loans as deferred exchange, observational learning; the base of lending and demonstration.
- **M12 (planned, re-based):** Band-age healer, elders and mediation, shared store, informal lending as services on this model.
- **D5 run:** the Tier 2 minimal versions above (C7, C8, C2), mediation if time (C9).
- **Stage 5 and later:** clinics, schools, granaries with welfare rules, lending circles at scale; Stage 6 courts, police, prisons, hospitals, banks; Stage 7 media, statistics, regulators.

## 10. Tests and acceptance hooks
"Significant" means a two-sided Mann–Whitney test at p < 0.05 over the stated seeds. Effect sizes are reported, never tuned to a target.

**Invariants (every build):** SVC-INV-1 to SVC-INV-6 check SVC-I1 to SVC-I6 over 20 seeds × 20 days (exact; SVC-I2 and SVC-I4 also as static or reflection tests).

**Unit tests:**
- **SVC-HEAL-U1** `Treat` with skill 0 and no remedy changes severity and recovery exactly as `Tend` does (M4 parity).
- **SVC-HEAL-U2** severity loss is monotone non-increasing in skill and floored at `TreatFloorPermille`.
- **SVC-EDU-U1** `Teach` copies only associations the teacher holds; copied entries carry `Origin.Imitated`; the learner's skill never exceeds the teacher's through teaching.
- **SVC-LEND-U1** creating, defaulting and writing off a formal loan leaves every goods count unchanged; repayment of n units reduces the obligation by exactly n.
- **SVC-JUS-U1** a ruling function given the same evidence items returns the same finding whatever the truth fields say (it cannot read them).

**Acceptance hooks (brief §47):**

| Scenario | Test |
|---|---|
| 1 Autonomous population | **SVC-ACC-1** with divine intervention disabled over 10 seeds × 5 sim years, informal `Tend`, `Teach` and loans occur on every seed and organised services on at least some; all invariants hold (reported, not targeted) |
| 2 Trade | **SVC-ACC-2** a fee for treatment is agreed by gesture and handed over as two separate `ExchangeStep`s (treatment then fee, or fee then treatment), either side able to defect |
| 3 Employment | **SVC-ACC-3** a communal store's keeper is paid by the organisation's treasurer, by hand-over at the store, for each shift worked |
| 4 Independent institution | ORGANISATIONS ORG-ACC-4 (shared store), and **SVC-ACC-4** a healer's practice founded by a skilled NPC after it treated at least 3 people informally, on at least 10 of 20 seeds in a raid scenario, with no player action |
| 5 Healthcare | **SVC-HEAL-1** an injured NPC gestures `NeedHelp`, a healer treats it within reach, and its recovery is faster than untreated controls (fixture, exact); **SVC-HEAL-2** a staffed healer lowers deaths from wounds over 20 days by a significant margin vs none, over 20 seeds (I-HEAL-1 kept); **SVC-HEAL-3** with one healer and 6 simultaneous patients, recorded unmet demand and queue length rise and at least one patient waits (capacity) |
| 6 Education | **SVC-EDU-1** a teacher with axe skill 600 teaches a learner with 0: the learner's skill and its learned value for "gather wood with axe" rise (exact deltas from the formula) and its later log gathering rate is higher than an untaught control over 20 seeds (significant); **SVC-EDU-2** a teacher who learned a wrong association passes it on (the learner's copied value has the teacher's sign) |
| 7 Lending | **SVC-LEND-1** A lends 4 berries to B for 2 days; B uses them (eats or trades); B either repays (obligation settled, `LoanRepaid`) or defaults (`LoanDefaulted`), both outcomes occurring across 20 seeds with varied trust; C1 holds throughout; **SVC-LEND-2** a recorded default lowers the lender's and listeners' credibility of B, and B's next request is refused more often than in the no-default control (significant over 20 seeds) |
| 15 Government autonomy | **SVC-ACC-15** a divine instruction to a government to open a clinic is relayed (ORG-ACC-15a); if adopted, the clinic still needs a healer to be hired and paid before any `TreatmentGiven` with that organisation's id occurs (exact) |
| 16 Religious and political separation | **SVC-ACC-16** a temple infirmary and a state clinic treat patients of any citizenship and any affiliation by their own policies; a patient's affiliation or citizenship is unchanged by being treated (exact) |
| 17 Analytical reconciliation | **SVC-ACC-17** `fact_service_request`, `fact_service_day` and obligation facts equal their rebuild from events for a 10-day run (exact) |

## 11. Open questions
Only consequential ones; each has the default the run uses.
1. **A healing skill record.** POPULATION_DESIGN owns the skill record, which is not in the code yet. Default: C7 adds `Skill.Healing` and the general skill slots there if no other package has; if none exists when C7 starts, healing skill is represented by the healer's learned association values for `Treat` (outcome-weighted), which is enough for SVC-HEAL-1 and SVC-HEAL-2.
2. **New ware `Herbs` and `OwnershipBasis.Forfeited`.** Both touch shared enums. Default: request both from the contracts owner in F1; if refused, `Treat` uses no remedy and collateral forfeiture uses `Seized` with a `Forfeit` reason on the event.
3. **Jurisdiction of communal rules.** Whether a commune's rule can make an act a crime in the sim's truth (default yes, for its members and its property only), or only a norm violation. This decides how justice KPIs count crime.

---

## 12. Owner D6: rights, commercial justice, universities and research (27 Nov 2026)
**Status: plan only.** Source: owner decisions **D6.9–D6.12, D6.15** ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). The knowledge, design, artefact and rights records are owned by [KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md](KNOWLEDGE_AND_TECHNOLOGY_DESIGN.md) (KNOW); fees and payments by [MONEY_AND_ACCOUNTING_DESIGN.md](MONEY_AND_ACCOUNTING_DESIGN.md) (MONEY). This section adds services on the common model of section 3, unchanged: request → queue → activity by a person within reach → outcome in real state → record. Principles 1–8 of section 2 hold, in particular **providers see only what they know** (an examiner knows its registry, a judge its evidence) and **claims never create goods** (fees, damages and licence fees are obligations settled by payment).

### 12.1 New service kinds (numbers by the SAC Industry addendum; proposed)
`RegisterRight = 20`, `ContractResearch = 21`, `CommercialDispute = 22`, `LibraryRead = 23`; `Teach` (2) gains design topics (`Topic = DesignId`).

### 12.2 Patent registration (`RegisterRight`, Tier 2, K1)
- **Provider:** a patent office organisation (KNOW §13) of a government that has a patent law (KNOW §8.2). No law: the office refuses every application (`no_patent_law`). No office: no patent can exist.
- **Request:** the applicant (a person, or an officer for an organisation) walks to the office, hands over a specification document of the design (the office now owns that copy, its evidence of priority) and pays the application fee (K2 `Fee` payment, or a `Fee` obligation if the office's policy allows deferral). Queue policy: first come by default.
- **Activity:** an examiner (office with the `Examine` power) at the office reads the deposit for `ExamineTicks × Complexity` and compares the design with its registry and the documents the office holds. **It cannot read truth**: prior art it has never seen does not count against the application.
- **Outcome:** a `Right` with status `Granted` (or `Refused`) in the registry (KNOW §8.1), written by the clerk (`RightGranted`/`RightRefused`). Assignment, licence recording and renewal are further requests of the same kind (`Topic` = right id and operation); each changes only the registry (and a K2 payment if a fee or price is involved), never goods, documents, knowledge or assets.
- **Capacity:** examiner ticks; recorded unmet demand = applications expired or dropped.
- **Failure and dishonesty:** an examiner may grant on a misread deposit, favour kin or take a bribe (INSTITUTIONAL_STATE oversight applies); wrongful grants are possible and measured (truth: an earlier holder of the design existed).

### 12.3 Commercial disputes (`CommercialDispute`; Tier 2 complaint, Tier 3 process; K1 with C9)
Covers **unauthorised use of a design** (infringement), **licence breach** (use beyond scope or term, unpaid licence fees or royalties), **unpaid bills and supply-agreement prices** and other contract disputes.
1. **Potential dispute (truth, analytics):** the sim classifies `UnauthorisedDesignUse` † when a design under a granted patent is built or run in the jurisdiction without a covering licence (KNOW §9). It changes nothing.
2. **Perception:** the holder or one of its members must perceive the asset or product (sight, inspection, gossip) to believe there is an infringement (`InfringementPerceived`).
3. **Complaint (Tier 2):** the believer walks to the commercial court (or, with none, a mediator) and accuses (`ComplaintMade`, kind `Infringement`, `LicenceBreach` or `UnpaidObligation`). The Tier 2 path ends here: the complaint is recorded and waits.
4. **Case (Tier 3):** the judge gathers evidence items (5.6 step 2): the registry entry (the clerk reads the patent office's records or a certified copy), the judge's own inspection of the asset (its design is an inspectable attribute), dated documents (an older document of the design is a prior-art defence), witness testimony, the obligation registry for unpaid amounts. It decides from what it holds; wrongful findings are possible (`true_infringement` †).
5. **Remedies:** injunction (an order to stop, obeyed by choice or by enforcers' acts; it never removes knowledge or documents), damages or arrears (a `Damages` or existing obligation, K2), a settlement or compulsory licence (if the law allows), seizure of infringing products (basis `Confiscated`, by an enforcer within reach), revocation of an invalid patent.
6. **Enforcement is physical** (5.6 step 6). Copying and building remain possible throughout (D6.11).

### 12.4 Licence enforcement (Tier 2 records, Tier 3 enforcement)
- A licence is a registry record (KNOW §8.4) with fee and royalty obligations (C2). **Royalties are billed by the licensor's clerk** from what it knows (the licensee's report, its own inspection), never accrued from truth (MONEY §7.1). Under-reporting is possible and measured.
- **Breach** (scope, term, exclusivity, non-payment) is classified in truth (†) and becomes a dispute only through 12.3 steps 2–6. A licensor may also refuse renewal or terminate by its own decision (`LicenceEnded`).
- **Inspection service** (Tier 3): an auditor role of the licensor visits the licensee's facility (if permitted) and counts output, adding evidence.

### 12.5 Universities and research organisations as education and research services
- **Teaching (Tier 2, C7 + K1):** a university's teachers teach design topics with the 5.2 `Teach` activity; a lecture hall raises the places per session; enrolment is a membership role (`fact_education_enrolment`); fees by K2. Outcomes are measured on the learner (level and fidelity in its `KnowledgeMind`, skill), never as an "educated" flag.
- **Library reading (`LibraryRead`, Tier 2):** a reader asks for access to the library store; the librarian grants it by the access policy; the reader `Study`s the document on site (documents leave only when lent, a loan with `OwnerRule.Keep`, 5.5). Reading never consumes or alters the document.
- **Research (`ContractResearch`, Tier 3, K1):** a sponsor commissions a research project (KNOW §10.2) from a research organisation or university for a fee; researchers spend real ticks and materials at a laboratory; the outcome is knowledge in the researchers' minds and, if formalised, documents owned per the contract and the employer's `Work` right. A discovery builds nothing.
- **Ownership models:** state universities, private laboratories, cooperative research bodies and religious schools use the same code (principle 7).

### 12.6 Invariants and tests
| # | Invariant or test | Checked |
|---|---|---|
| SVC-D6-I1 | No right is granted, assigned or licensed without an examiner or clerk present at the office and the parties' own acts there | events |
| SVC-D6-I2 | Registering, assigning or licensing a right changes no lot, artefact record, knowledge entry or asset (only the registry and K2 payments) | state diff |
| SVC-D6-I3 | No `UnauthorisedDesignUse` creates an obligation, a block or a ruling by itself; every ruling has a complaint and a judge | events |
| SVC-D6-I4 | Examination and rulings read only the provider's own mind, the office or court records and evidence items (reflection; extends SVC-I4) | reflection |
| SVC-D6-U1 | `RegisterRight` with a prior right on the same design in the office registry is refused; with prior art only in a document unknown to the office it is granted (a measurable wrongful grant) | EM |
| SVC-D6-U2 | `LibraryRead` raises the reader's level by the KNOW §5.2 rule and leaves the document byte-equal | EM |
| SVC-D6-ACC | acceptance rows D6.6, D6.7 and D6.8 of [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md) | RT |

### 12.7 Tier and package mapping
| Service | Tier | Package |
|---|---|---|
| Patent registration, assignment, licence recording, renewal | 2 | K1 (C6 framework, K2 fees, C10 or a scenario-seeded patent law) |
| Complaint of infringement, licence breach, unpaid obligation | 2 | K1 + C9 complaint (a recorded stub until C9) |
| Commercial court process, evidence, remedies, compulsory licences, revocation | 3 | C9 + K1 |
| University teaching of designs, library reading | 2 | C7 + K1 |
| Contract research, laboratories | 3 | K1 |

# Organisations and institutions design (owner D5, 26 Nov 2026)

The reusable framework for every organisation on the island: a household's shared store run as a commune, a healer's practice, a workshop, a cooperative, a lending circle, a temple, a war band and the government itself. It is the framework the owner asked for before any independent institution is built (brief §18). The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); owner decisions are in [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md), which wins over every design document.

**Owner decisions this document works out:** D5.1 (the player is a god only), D5.2 (governments are autonomous NPC organisations), D5.6 (NPC autonomy; no secret faction-level abstraction), D5.16 (institutions emerge independently; a threshold only makes an opportunity available), D5.17 (several ownership models, religious organisations on the same framework), D5.28 (divine and institutional knowledge are separate), D5.29 (autonomous civilian, military, institutional and faction policies), D5.33 (no predetermined outcome).

## 1. Purpose and scope

**This document owns:**
- the `Organisation` record and its persistent identity (`OrgId`);
- organisation types and purposes, the charter and its revisions;
- ownership models: state, private, communal, cooperative, religious;
- governance: offices, the generic `DecisionRule`, how a decision is proposed, ratified and carried out;
- roles held by NPCs: member, employee, manager, officer;
- the organisation's buildings, productive assets and stores (reusing goods bins and owners);
- the organisation ledger: budget and formal obligations, consistent with the goods ledger;
- institutional records kept by an organisation (information layer 4) and who may read them;
- organisation policies and relations between organisations;
- the lifecycle: opportunity, founding, growth, decline, failure, merger, split, dissolution;
- how a god can influence an organisation (only through its individuals);
- the government as one organisation type, with the treasury;
- religious organisations as one organisation type.

**It links to, and does not repeat:**
- the common service model and each service (healthcare, education, welfare, lending, justice): [INSTITUTIONAL_SERVICES_DESIGN.md](INSTITUTIONAL_SERVICES_DESIGN.md);
- manager effort, Goodhart's law, returns, inspection, failure modes, the catalogue by age and unlock rules: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md) (that model is kept and now sits on this framework);
- the treasury ledger, contracts, taxes, laws, citizenship: [STATE_DESIGN.md](STATE_DESIGN.md);
- production, jobs, enterprises, commons, property records: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- associations, collective action, interests and legitimacy: [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md);
- the military service model: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- goods, holders, owners and reach: [SHARED_CONTRACTS.md §2–3](SHARED_CONTRACTS.md#2-inventories-and-goods-movement), [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md);
- divine actions, their delivery to NPCs and refusal: DIVINE_PLAYER_DESIGN.md (owned by the divinity documents);
- doctrine, commandments, belief and affiliation: COMMANDMENTS_AND_RELIGION_DESIGN.md;
- records, publication and knowledge views: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- the D5 shared ids, enums and event code ranges: [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (this document names events and proposes contract additions; the contracts document allocates them).

## 2. Principles
1. **An organisation is people, places, goods and records.** It has no power that its role-holders do not exercise. With no living role-holder it decides nothing and does nothing (I-INV-2 of INSTITUTIONAL_STATE is kept and generalised).
2. **Every decision has a decider.** An organisation decision is taken by named NPCs (an owner, a manager, an officer, or the members present at a meeting) through the organisation's `DecisionRule`. Each of them decides from its own knowledge: its own mind (`MindView`) and the records it can actually read. No organisation-level or faction-level procedure reads true state, the warehouse or debug information (D5.6, D5.28).
3. **Deciding is not doing.** A decision changes only the organisation's records (a policy, a budget line, an appointment, a posting). Anything material (goods moved, a building built, a patient treated, a fine taken) happens only when an NPC performs an ordinary physical action within reach ([WORLD_ONTOLOGY.md §3](WORLD_ONTOLOGY.md#3-the-physical-action-set)). An NPC asked to carry out a decision may refuse.
4. **Organisations come from people, not from the government.** Individuals and communities found organisations when they recognise a need, know a way to meet it, have or can pool the resources, and can cooperate. A rule of the game (an unlock) may make a kind of organisation *available*; it never creates an organisation, staff, goods or members (D5.16).
5. **One framework, several ownership models.** State, private, communal, cooperative and religious organisations use the same record, the same physics, the same goods API and the same decision machinery. Only the owners, the decision rule and the distribution rule differ. Production, service and exchange code never branches on ownership model (D5.17; the static test of ECONOMY 4.7 is extended).
6. **Authorities stay distinct.** Religious, political, economic and institutional authority are separate roles in separate organisations. Holding an office in a temple gives no power in the government; owning a workshop gives no vote in a commune; being a god gives no office anywhere (brief §4).
7. **The god influences organisations only through individuals.** An instruction to an organisation is relayed to the individuals who could act on it. Each decides for itself whether to ignore, refuse, adopt, modify or carry it out. Compulsion and possession act on one individual's actions; they never bypass the organisation's rule or the other members' choices (brief §4, scenario 15).
8. **Claims never create goods.** A budget, a pledge, a wage owed or a loan outstanding is a record. Only `Goods.Produce` creates goods, only `Goods.Consume` destroys them, and only `Goods.Move` and `Goods.SetOwner` change where they are and who owns them ([SHARED_CONTRACTS §2.2](SHARED_CONTRACTS.md#22-the-one-sanctioned-api)).
9. **Nothing is favoured.** No organisation type, ownership model or governance rule is tuned to win. Which persist is measured (D5.33).
10. **Determinism.** Organisations update once a day in ascending `OrgId`, decisions in ascending decision id, members in ascending `NpcId`. Random draws use `Rng.Fork("org", orgId).Fork(purpose)`. Every list is bounded and sorted.

## 3. Data structures (`Sim/Orgs`, proposed)

Sizes are first bounds, to revisit with performance data. Integers only.

### 3.1 Identity and ownership

```csharp
// OrgId: int, from 1, never reused. Allocator: OrgModule.Found (only path). Counter: OrgState.NextOrgId (saved, hashed).
// Replaces InstitutionId and EnterpriseId as one id space (both were planned, neither is built: SHARED_CONTRACTS §1 lists neither).

public enum OrgType : byte {            // what kind of body it is; purposes say what it does
    Government = 1,                      // the state of a faction (section 4.10)
    Enterprise = 2,                      // production for its owners (ECONOMY 4.7)
    ServiceBody = 3,                     // provides a service (healer's house, school, shared store, court)
    Association = 4,                     // members pursuing a shared interest (POLITICAL_ECONOMY 3.6: union, guild, mutual aid, movement)
    Religious = 5,                       // a temple, a priesthood, a congregation (section 4.11)
    Military = 6,                        // a war band, militia or army (MILITARY_DESIGN)
    Commons = 7,                         // a resource-management body over a Commons record (ECONOMY 4.8)
}

public enum OwnershipModel : byte { State = 1, Private = 2, Communal = 3, Cooperative = 4, Religious = 5 }

public struct OrgShare { public Owner Holder; public short Permille; }   // Holder: Npc, Household, Org (another organisation), State
```

**Owner for organisation property.** Goods, bins and buildings owned by an organisation use the existing `Owner` struct (`Assets/God/Sim/Population/LotBin.cs`). This document asks the contracts owner to add **`OwnerKind.Org = 7`** (`Id = OrgId`). Mapping during migration:
- `OwnerKind.State` (`Id = FactionId`, used today for HQ stock through `EcoBuilding.Faction`, `Assets/God/Sim/Economy/EconomyState.cs`) keeps its meaning and is read as "the government organisation of that faction" (`OrgState.GovernmentOf(faction)`). New code writes `Org(govId)`; `State(f)` is a temporary alias, listed in the temporary mechanisms table, removed when C10 lands.
- `OwnerKind.Enterprise = 4` is never written (M8 is not built); enterprises are organisations with `Owner = Org(id)`.
- `OwnerKind.Group = 3` stays for communal ownership without an organisation (the M4 `BackToCommunity` escheat). A communal organisation owns as `Org(id)`; its members are recorded in `Shares`.

### 3.2 The organisation record

```text
Organisation                               // OrgState.Orgs, ascending Id
  Id: OrgId
  Type: OrgType
  Purposes: Purpose[<= 4]                  // { Kind: Produce | Service | MutualAid | ManageCommons | Worship | Defend | Govern | Represent | Lend,
                                           //   Ref: Ware | ServiceKind | CommonsId | IssueId | 0 }
  Charter: Charter                         // the current version; revisions are kept (below)
  Ownership: OwnershipModel
  Shares: OrgShare[<= 16]                  // sums to 1000; Religious: the organisation owns itself (one share, Holder = Org(Id))
  Faction: FactionId (0 = none)            // where it is based; an organisation need not be registered with any state
  Offices: Office[<= 8]
  Roles: RoleHolding[<= 64]                // ascending NpcId, then role
  Assets: AssetRef[<= 8]                   // buildings (BuildingId), huts, PropertyRecord asset ids it owns or holds a use right on
  Stores: BinId[<= 4]                      // LotBins with Owner = Org(Id), kind OrgStore (proposed BinKind 3)
  Ledger: OrgLedger
  Budget: BudgetLine[<= 8]
  Policies: OrgPolicy[<= 16]
  Records: ring RecordEntry[<= 512]        // INSTITUTIONAL_STATE RecordEntry, unchanged: layer 4
  Requests: ring ServiceRequest[<= 256]    // INSTITUTIONAL_SERVICES §3 (replaces INSTITUTIONAL_STATE Case)
  Decisions: ring OrgDecision[<= 64]
  Relations: OrgRelation[<= 16]
  Status: Forming | Operating | Dormant | Insolvent | Dissolved
  Founded (tick), Founders: NpcId[<= 8], OriginOpportunity: OpportunityKind
  MergedInto, SplitFrom: OrgId (0 = none)

Charter                                    // what the founders agreed; amended only through the decision rule
  Version: short, AdoptedTick, AdoptedByDecision
  Purposes (as above), MembershipRule: Open | ByAcceptance | KinOnly | HouseholdOnly | ByContribution | Hereditary
  Governance: DecisionRule
  Distribution: ToOwners | EqualToMembers | ByWorkTicks | ByContribution | ByNeed | Retained
  StoreAccess: Officers | Members | MembersByRation | Anyone
  DissolutionRule: ToMembersEqual | ToShares | ToAnotherOrg(OrgId) | Ownerless
  AmendRule: DecisionRule                  // usually the same as Governance
CharterRevision: { Version, Tick, DecisionId, Changed: bitmask }   // <= 16 kept per organisation

Office                                     // a seat with powers
  Kind: Head | Manager | Treasurer | Keeper | Recorder | Priest | Commander | Judge | Teacher | Healer | Councillor
  Powers: bitmask { Propose, DecideAlone, Spend, Hire, Dismiss, KeepRecords, KeepStore, Represent, Enforce }
  Holder: NpcId (0 = vacant), Since (tick), Filling: Appointed | Elected | Hereditary | Seniority | Founder

RoleHolding
  NpcId, Role: Member | Employee | Manager | Officer(officeIndex) | Apprentice | Client
  Since (tick), Basis: ContractId (a JobPosting or StateContract) | DealId (an agreement by gesture) | Charter (founder, hereditary)
  Terms: PayWare, PayCount, Per | ContributionWare, ContributionCount, PerDays    // what the NPC believes it agreed is in its own mind too
  Status: Active | Suspended (ECONOMY Q19) | Left

DecisionRule
  Kind: OwnerDecides | HeadDecides | MajorityPresent | Consensus | Council | (later) Election | Rotation | Lot | Seniority
  Quorum: byte (members present), MeetingPlace: node, ProposalWindowDays: byte

OrgDecision                                // the organisation's own record of a decision (also a domain event)
  Id, Tick, Proposer: NpcId, Source: Member | Request | Relation | DivineInstruction (with the instruction id)
  What: Hire | Dismiss | Appoint | SetPolicy | SetBudget | Spend | Post | FoundService | Join | Leave | Amend | Merge | Split | Dissolve | Respond
  Args: int[4], Votes: (Accept, Refuse, Absent) counts as the counter saw them
  Outcome: Adopted | Rejected | Lapsed | Modified (Args changed by the proposer before the vote)

OrgPolicy  { Key: OrgPolicyKey, Value: int, SinceTick, DecisionId }    // type-specific: queue order, eligibility, ration, fee, opening hours

OrgRelation { Other: OrgId, Kind: MemberOf | Funds | FundedBy | Regulates | RegulatedBy | Recognises | ContractsWith | Owns | OwnedBy | Allied | Rival,
              Since, Basis: DecisionId or ContractId }
```

### 3.3 The organisation ledger

```text
OrgLedger
  DayIn[ware], DayOut[ware] by cause       // contribution, fee, sale, grant, tax, wage, purchase, ration, treatment, loan out, repayment in ...
  Obligations: ObligationId[<= 32]         // formal claims on and by this organisation (below)
  ArrearsDays: short                       // days with pay owed and not handed over

FormalObligation                           // C2 owns the registry; listed here because organisations create most of them
  Id: ObligationId (new id space), Debtor: Owner, Creditor: Owner
  Ware, Count, DueDay, Collateral: ItemLotId (0 = none)
  Source: Wage | Loan | Pledge | Fee | Fine | Restitution | Tax
  WitnessedBy: NpcId[<= 3], RecordedBy: OrgId (0 = none)
  Status: Open | Settled | PartlySettled | Defaulted | WrittenOff | Orphaned
```

- **The goods are the lots.** An organisation's holdings are exactly the lots in its bins and carried for it (`Owner = Org(id)`), counted by the existing conservation check C1. The ledger adds accounts; it never holds goods. This is STATE_DESIGN's treasury rule ("there is no abstract pool"), applied to every organisation.
- **Formal vs informal obligations.** A promise made by gesture and remembered by the two parties stays a social-memory obligation (M7), as [MATERIAL_INHERITANCE §4.8](MATERIAL_INHERITANCE_DESIGN.md#48-debts-and-claims) says. A **formal obligation** exists only when an organisation is a party to it, or records it, or when a wage or service contract creates it (today's `Debt` in STATE_DESIGN's `TreasuryLedger.Arrears` is one). Formal obligations are true state (the institutional fact that the agreement was made), each held once in a registry with both sides, so claims always equal liabilities. The organisation's *record* of it (layer 4) is separate and may be wrong.

### 3.4 What a member knows (in the NPC's mind)

```text
OrgBelief                                  // NpcMind, <= 6 per NPC (about 6 x 24 B = 144 B; POPULATION's memory table to add the line)
  OrgId, MyRole (0 = none), BelievedHead: NpcId, BelievedPlace: node, BelievedStoreNode: node
  KnownPolicies: bitmask (policies it has heard or read), MyTermsAsUnderstood, Trust (in the organisation as a pseudo-agent, SOCIAL_MEMORY), Confidence, Tick
OpportunityBelief                          // <= 2 per NPC; section 4.1
  Template: OrgTemplateId, NeedEvidence: short, SupportersBelieved: NpcId[<= 4], Formed (tick)
```

The state as a pseudo-agent in social memory (STATE_DESIGN "Settled here") becomes the general rule: each NPC may keep a trust record for any organisation it knows, beside its records of the individuals in it.

### 3.5 Reuse of existing code

| Need | Reuse | Path |
|---|---|---|
| Owners, bins, holders | `Owner`, `OwnerKind`, `LotBin`, `BinKind` (add `Org`, `OrgStore`) | `Assets/God/Sim/Population/LotBin.cs` |
| Every goods change | `Goods.Move`, `Produce`, `Consume`, `SetOwner`, `NewBin`; `OwnershipBasis` (`Founded = 7`, `Seized = 6`, `StateGrant = 5` already exist) | `Assets/God/Sim/Population/Goods.cs` |
| Reach | `Reach.Touch`, `HandOver`, `Store` | `Assets/God/Sim/Population/Reach.cs` |
| Permission to use an organisation's store | extend the query `Permits.Believed` with basis `OrgRole` (from the mind's `OrgBelief` and the believed `StoreAccess`); still a query, never checked by `Goods` | `Assets/God/Sim/Cognition/Affordances.cs` |
| Affordances for new verbs | `Affordances.Of`, `Blocked` | `Assets/God/Sim/Cognition/Affordances.cs` |
| Gestures for proposing, joining, voting | `GestureAct.AskJoin`, `Accept`, `Refuse`, `Promise`, `Offer`, `NeedHelp`, `Accuse`, `Praise`; `TopicKind.Task` (add `TopicKind.Org`) | `Assets/God/Sim/Comms/Message.cs` |
| Agreements and their ids | `DealId` (`SocialState.NextDealId`), `DealMemory` | `Assets/God/Sim/Core/GodState.Social.cs`, `Assets/God/Sim/Social/SocialMind.cs` |
| Trust, familiarity | `SocialView.Trust`, `Familiarity`, `ClassOf` | `Assets/God/Sim/Social/SocialView.cs` |
| Subjective value of a contribution, fee or wage | `Valuation.Marginal`, `Gain`, `Loss` (own price beliefs, no global price) | `Assets/God/Sim/Social/Valuation.cs` |
| Households and their stores as founding cores | `Household`, the household `LotBin`, `CareArrangement` | `Assets/God/Sim/Population/Households/HouseholdState.cs`, `Households.cs` |
| Decision procedure seats | `IInstitutionPolicy`, `IFactionPolicy`; `InstitutionView` (a placeholder today) | `Assets/God/Sim/Policy/PolicyInterfaces.cs`, `Assets/God/Sim/Policy/Observations.cs` |
| The government's current decision procedure | `AiBrainFactionPolicy` wrapping `AiBrain` | `Assets/God/Sim/Policy/FactionPolicies.cs`, `Assets/God/Sim/Ai/AiBrain.cs` |
| Buildings and the HQ treasury | `EcoBuilding` (`Faction`, `IsHq`, stock) | `Assets/God/Sim/Economy/EconomyState.cs` |
| Learning from organisational outcomes | `Learner.AddReward`, `Learner.Outcome`, `EpisodeKey` (new kind for org roles, from the reserved 16–63 range) | `Assets/God/Sim/Learning/Learner.cs`, `EpisodeKey.cs` |
| Knowledge views | `FactionKnowledgeView` (the government's view only), `Knowledge` | `Assets/God/Sim/Information/FactionKnowledgeView.cs` |

## 4. Mechanics

### 4.1 Recognising an opportunity
An organisation begins as a belief in one NPC's mind, formed from that NPC's own experience. There is no world-level "institution spawner".

- **Templates.** An `OrgTemplate` is data: a type, purposes, a default charter, the roles it needs, the place and goods it needs, and its availability rule. Templates are generic ("shared food store", "healer's house", "lending circle", "workshop", "mutual-aid group", "temple", "war band", "government council"). They are rules of the game, like recipes.
- **Availability (the only threshold).** A template is *available* to an NPC when (a) the INSTITUTIONAL_STATE unlock rule for its type holds over the NPC's **community** (its household cluster, its existing organisations, or its faction, whichever the template names) and (b) the NPC **knows the practice**: it holds the matching cultural trait or skill ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)), has seen such an organisation operating, has been told of one, or received a divine suggestion or revelation naming it. Availability lets a `ProposeOrg` candidate exist. It creates nothing.
- **Need evidence** accumulates in `OpportunityBelief.NeedEvidence` from the NPC's own perceptions and memory, for the template's need signal:

| Template | Need signals (own experience only) |
|---|---|
| Shared food store | own hungry days while others it knows had spare food; spoilage of its own surplus; gifts given and received within its cluster |
| Healer's house | injuries and illness it suffered or saw untreated; tending it gave or received; deaths from wounds it saw |
| Lending circle | loans it made or took; defaults it suffered; refused requests for goods |
| Workshop | its own skill in a recipe; inputs it can get; requests it saw for the output |
| Mutual-aid group | shocks it suffered or saw (a hut lost, a carer died) and help it did or did not get |
| Communal childcare | care turns given and received (`CareArrangement.Given`, `Returned`); work time lost to minding |
| Mediation | `Accuse` events it made, suffered or saw; fights after accusations |
| Temple | prayers, worship and religious practice it shares with others (COMMANDMENTS_AND_RELIGION) |

- **Support.** The NPC believes others would join when it has seen them share the need or the practice and trusts them (`SocialView.Trust ≥ SupportTrust`, default 300). Up to 4 believed supporters are kept.
- **The `ProposeOrg` score** is an ordinary decider candidate: `need × believed benefit (learned and cultural) + support term − cost of time and pledged goods − believed risk`, plus learned bias from earlier attempts. It competes with every other intent. Personality (Industry, Generosity, Boldness) changes the score, never the rule.

### 4.2 Founding by individuals
1. **Proposal.** The proposer walks to a believed supporter (or a gathering) and gestures `AskJoin` with `TopicKind.Org` naming the template and, where relevant, a pledge (`Promise` of goods or work). A proposal is a message; it binds no one.
2. **Answers.** Each listener decides by its own scores: `Accept` (with or without a pledge of its own), `Refuse`, or ignore. A misunderstood proposal is possible (COMMUNICATION misunderstanding rules).
3. **Founding meeting.** When at least `Template.MinFounders` (1 for a private body; 2 or 3 for communal and cooperative ones) have accepted within `ProposalWindowDays`, the proposer calls a meeting at a place (a hut, a building it may use, a believed common spot). Founding happens on the first day the minimum are **present together within gesture range** and each signals `Accept` again. Then `OrgModule.Found` allocates the `OrgId`, writes the charter from the template (amended by the founders' answers: for example the ownership model the majority accepted), records the founders' roles and emits `OrgFounded`.
4. **What exists after founding.** An empty organisation: founders in their roles, an office or two filled by founders, no goods, no staff beyond founders, no building unless a founder contributes one (`SetOwner` with basis `Founded`) or the organisation later builds or acquires one. Pledges are formal obligations owed to the organisation, settled only by physical deposits (`Goods.Move(Carry → Bin(store), Deposit, ToBinOwner)`).
5. **Founding by another organisation.** The government, a temple or an enterprise may found a subsidiary body through its own decision rule (`OrgDecision.What = FoundService`). It then needs the same things: an office-holder who carries it out, a place, goods actually moved, staff actually hired. Being founded by the state confers nothing else.
6. **Communal founding without the state (Q17)** is this mechanism with a communal template. INSTITUTIONAL_STATE's `FoundCommunal` interface becomes `ProposeOrg` with a communal charter; I-COM-1 and I-COM-2 still apply.

### 4.3 Roles: members, employees, managers, officers
- **Joining.** Membership follows the charter's `MembershipRule`: an `AskJoin` answered by the deciding office or meeting; a hereditary rule (a child born into a member household); a contribution rule (a deposit makes one a member). Employment is a `JobPosting` with `Poster = Org(id)` ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#jobs-and-the-end-of-walkers)); taking it creates an `Employee` role. Offices are filled by the charter's filling rule.
- **Leaving.** Any role-holder may leave at any think (`LeaveOrg`); employees may suspend work (Q19); the organisation may dismiss by decision. Death ends every role (`OrgRoleLeft`, reason died). A resurrected NPC does **not** regain roles (D5.13); it may ask to rejoin.
- **Several roles.** An NPC may hold roles in up to 4 organisations. It allocates its own time; a conflict (a guard who is also a striker) is resolved by its own scores.
- **Powers come from the office and the charter.** A keeper may withdraw from the store; a treasurer may hand over pay; a head may decide alone where the rule says so. The power is checked when the NPC tries to act **as the organisation** (`OrgCommand.Actor` must hold an office with the power). The physical act still needs reach, possession and room.
- **Roles are relations, not types.** No NPC is created as a priest, a judge or a manager. Roles are recorded with start and end, so employment and office histories exist for the warehouse (effective-dated, half-open).

### 4.4 Decisions: who decides and how
Each organisation has one **decision slot** per day (tick `(dayStart + OrgId × 7) % TicksPerDay`, so organisations do not all decide on one tick).

1. **Proposals.** At the slot, each office-holder with `Propose` power who is alive and not asleep runs the organisation's decision policy **as itself**:
   ```csharp
   // Reuses IInstitutionPolicy (Sim/Policy/PolicyInterfaces.cs). InstitutionView (Sim/Policy/Observations.cs, a placeholder today)
   // is filled with: OrgId, DeciderNpcId, the decider's office and powers, the organisation records the decider may read,
   // the requests it knows of, the budget and store counts as last recorded, and the decider's own MindView.
   void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands);   // emits OrgProposal commands
   ```
   Members without office propose by gesture at a meeting (a `Demand` or `AskJoin` with an org topic), which the recorder writes into the proposal list.
2. **Ratification by the rule.**
   - `OwnerDecides`, `HeadDecides`: the proposer with `DecideAlone` decides; the decision is adopted at once.
   - `MajorityPresent`, `Consensus`, `Council`: the proposal is announced by gesture at the meeting place at the next meeting (members learn of meetings by being told, like postings). Each present member (or councillor) scores it with its own interests and beliefs (the political-interest model of [POLITICAL_ECONOMY §4.1](POLITICAL_ECONOMY_DESIGN.md#41-the-interest-model), its trust in the proposer, learned outcomes of similar decisions) and gestures `Accept` or `Refuse`. The counter (the recorder, or the proposer if none) counts **what it saw**; a miscount is possible and recorded as the counter saw it. Ties keep the old state. Below quorum, the proposal lapses.
3. **Recording.** An adopted decision becomes an `OrgDecision` record and changes only records: a policy, a budget line, an office, a posting placed on a board, a relation.
4. **Carrying out.** Material effects need an NPC with the power to act: the treasurer walks to the store and hands over the grant; the keeper posts the job; the head walks to the other organisation to deliver an answer. A role-holder may delay, refuse or do it badly (the effort model of [INSTITUTIONAL_STATE §4](INSTITUTIONAL_STATE_DESIGN.md#how-staff-and-managers-choose-effort-goodhart-in-the-mechanics)). The decision record keeps `CarriedOut` progress.
5. **Policies scripted first.** `MemberUtilityPolicy` (default; generalises INSTITUTIONAL_STATE's `ManagerUtilityPolicy`) proposes from the decider's own beliefs, interests and traits. `RuleFollowerPolicy` stays the benchmark control. Trained `IntNet` policies later (D5.31: no training in this run).

**No secret abstraction.** The reflection test (`Policies_DoNotReferenceGodState`) covers `Sim/Orgs/Policies`. A second test checks that every `OrgDecision` names a living decider who held the needed office at that tick, and that every material organisation effect has an NPC action event with that organisation's id (I-ORG-3, I-ORG-4).

### 4.5 Buildings, productive assets and stores
- **Stores** are `LotBin`s with `Owner = Org(id)` and kind `OrgStore`, at a building or hut node. Deposits and withdrawals are the existing `Store` and `Withdraw` actions within `Reach.Store`. Who may withdraw is the charter's `StoreAccess` and the organisation's ration policy, applied by the withdrawing NPC as a permission belief; taking against it is theft (M8 classification).
- **Buildings and productive assets** are `PropertyRecord`s ([ECONOMY 3.5](ECONOMY_DESIGN.md#35-property-records)) owned by `Org(id)`, or held under a use right granted by their owner. Work at an organisation's bench is under a `Work` use right, so output belongs to the organisation (the wage relation of ECONOMY 4.4, unchanged).
- **Acquiring assets** is always an ordinary transaction: a member contributes (`SetOwner`, basis `Founded` or `Gift`), the organisation builds (a site with contracts), buys (barter by an authorised member), receives a grant (a hand-over from another organisation), or seizes (basis `Seized`, by force, with every consequence of theft).

### 4.6 Budget and obligations
- **Budget lines** are decisions: "up to 3 berries a day for rations; 1 log a day for the healer's fire". Spending against a line is still a physical hand-over or consumption by an authorised role-holder. Spending never exceeds the store plus what the decision allows (I-INV-4 of INSTITUTIONAL_STATE, generalised).
- **Income** by ownership model: government taxes, fees and sales (STATE_DESIGN); private fees and sales; communal member contributions; cooperative shares of output; religious offerings and gifts. All are hand-overs at a place.
- **Wages** owed and not paid become formal obligations (`Source = Wage`) and arrears, exactly as STATE_DESIGN's `Debt`.
- **Insolvency.** An organisation whose open liabilities due exceed its holdings for `InsolventDays` (default 3) becomes `Insolvent`. It keeps operating with whoever stays. Creditors may pursue claims through whatever justice exists ([INSTITUTIONAL_SERVICES §5.6](INSTITUTIONAL_SERVICES_DESIGN.md#56-justice-complaints-arbitration-restitution-and-detention)).

### 4.7 Institutional records and information separation
- **Records are written by a role-holder's action** (`WriteRecord`, a short act at the organisation's place by the recorder or the office that holds `KeepRecords`). The writer records what it knows: what it saw, was told or counted. A record can be late, incomplete, wrong or falsified (the falsification choice of INSTITUTIONAL_STATE). With no recorder, nothing is written.
- **Who can read.** Office-holders read the organisation's records when at its place (or carried, later, as tallies). Other members know only what they were told. Another organisation learns only from a filed return, a report delivered by a person, or a publication (layer 5).
- **The government's records** feed its `FactionKnowledgeView` (layer 7). Under `FullInformation` (stage 4 default) that view is still near-complete for the government's own books; the D5.28 separation means the **god** does not read it as its own knowledge: divine observation is a separate view with its own rules (DIVINE_PLAYER_DESIGN, INFORMATION_AND_MEDIA_DESIGN).
- **Never read:** true state, analytics-only truth fields, the warehouse, debug overlays (D5.28; reflection test).

### 4.8 Policies
An organisation policy is a record set by decision: queue order, eligibility, ration size, fee, opening hours, who may draw from the store, what to produce. It binds nobody physically. Role-holders and clients learn it (told, read at a board, seen in practice) as a `KnownPolicies` belief and follow it through their own scores, weighed with their interests and norms. A rule that nobody follows is a measured gap, not an error. This is how D5.29's autonomous institutional policies work at the level of the people who carry them out.

### 4.9 Relations between organisations
- **Kinds:** membership (a federation of communes), funding (grants are hand-overs), regulation (a law of the government that names an organisation type, with an enforcer), recognition (a government decision that registers a commune or a temple), contracts (an organisation's posting taken by another organisation's member on its behalf), ownership (shares), alliance and rivalry.
- **Each relation is created by a decision of each side that has one** (recognition can be one-sided). A relation never transfers authority by itself: a regulating government still needs an NPC to inspect and an enforcer to act; a funded body still decides by its own rule.
- **The state's options towards independent bodies** (INSTITUTIONAL_STATE "Communal founding without the state"): support, regulate, compete, replace. Each is a government decision carried out by its office-holders with real consequences for members' trust and loyalty.

### 4.10 The government: the state as an organisation
- **One government per faction at the start** (D5.2): `OrgType.Government`, `OwnershipModel.State`, `Faction = f`, purposes `Govern` (and `Defend`, until a military organisation exists). Its stores are the HQ stocks: the treasury of [STATE_DESIGN.md](STATE_DESIGN.md#the-treasury), unchanged, owned by `Org(gov)` (alias `State(f)`).
- **Its offices and rule are data of the scenario.** Default: a `Council` of the faction's founder adults aged elder or the 3 oldest adults (`Councillor` offices), one `Head` (the most trusted councillor by the "most trusted" count of INSTITUTIONAL_STATE), a `Treasurer` (the HQ paymaster role, which in Stage 4 is the temporary automatic paymaster, STATE Q16), decision rule `HeadDecides` for routine commands and `Council` for laws, taxes and founding services. Scenarios may start with a headman, a commune or no government at all.
- **What it decides.** The STATE_DESIGN command set (sites, contracts, purchases, taxes, handouts, laws, immigration, enlistment terms, assignments, institution budgets, diplomacy). Each command is an organisation command with an `Actor` office-holder, validated by the existing `Check` and `Submit` gate.
- **How it decides in the first version (temporary mechanism "AiBrain government seat").** The existing `AiBrainFactionPolicy` (`Assets/God/Sim/Policy/FactionPolicies.cs`) is kept as the government's routine decision procedure, attributed to the Head office: it runs only while the government has a living Head, its commands carry the Head as `Actor`, and it reads only `FactionKnowledgeView`. Proposals that come from individuals (a councillor's own proposal, an adopted divine instruction, a petition answered) go through the council rule above and are applied as state commands when adopted. The seat is listed in the temporary mechanisms table and is replaced by `MemberUtilityPolicy` for the government (C10) when it can produce a working economy. If the Head dies, the office is vacant until filled by the rule; meanwhile only standing rules run (the automatic paymaster and immigration desk of STATE Q16 continue, as temporary rules).
- **Collapse.** A government with no office-holders for `DormantDays` (default 5) becomes `Dormant`: no new laws, contracts or budgets; standing rules continue; its stores remain its property and can be stolen or claimed. A new government can be founded by individuals from the "government council" template, which is available to any community with enough adults (INSTITUTIONAL_STATE unlocks), or the faction may simply have none. A captured HQ's stores pass to the captor's government (basis `Seized`), as today.
- **The god is not the government.** No god holds an office. Divine influence on the government goes through section 4.12.

### 4.11 Religious organisations
- Same framework, `OrgType.Religious`, `OwnershipModel.Religious` (the organisation owns itself; one share held by `Org(id)`). Typical offices: `Priest` (head), `Keeper` (offerings store), `Recorder`, `Teacher`.
- **Purposes:** `Worship` (rites, prayer gatherings), and any service (healing, teaching, welfare, mediation) through the same service model.
- **Income:** offerings and gifts (hand-overs at the temple), fees for rites, land and goods it owns.
- **Doctrine** (the commandments the organisation teaches, how it interprets them) is owned by COMMANDMENTS_AND_RELIGION_DESIGN; here it is a list of commandment ids in a `Doctrine` policy, set by the organisation's decision rule, so a priesthood can teach a version its god never revealed or reject a revealed one.
- **Interests.** Priests are ordinary NPCs: they can accumulate property, take fees, favour kin, side with or against the government, and disagree with their god (brief §13). Their influence is their trust and esteem in others' minds plus the organisation's goods.
- **Separation (scenario 16).** Religious membership is an `OrgBelief` and a role in a religious organisation. It never changes citizenship (`CitizenData.Faction`), government office or loyalty by rule. A government may adopt a commandment as law (a separate `LawEnacted`), after which religious and legal rules remain distinct records.

### 4.12 How a god influences an organisation
The divine action itself (cost, delivery, perception) is DIVINE_PLAYER_DESIGN's. This section says what happens inside the organisation.

1. **Target an individual.** A suggestion, command, compulsion or possession of a member, an officer or a leader works exactly as for any NPC. Only that NPC's own actions are affected.
2. **Target an organisation.** An instruction addressed to an organisation (for example "fund a healer", "free the prisoner", "share the store with strangers") is **relayed** to the individuals who could act on it: the office-holders whose powers cover it, in ascending `NpcId`, plus the members at its meeting place when the rule is a meeting rule (`InstructionRelayed`, one per recipient). Each recipient perceives it as a divine message through its own perception (asleep, absent or distant recipients do not).
3. **Each recipient decides**, through its own decider, with its belief, faith, trust and fear of the god (COMMANDMENTS_AND_RELIGION) set against its own interests:
   - **ignore** or **refuse** (a refusal can be a visible gesture, so others may learn of it);
   - **adopt** it as its own proposal, possibly **modified** (a smaller grant, a later date, a cheaper healer), entering the decision rule like any proposal (`OrgDecision.Source = DivineInstruction`);
   - **act on it personally** within its own powers (a keeper who hands out food without a decision is acting outside policy, which others may punish).
4. **The organisation's rule still applies.** The council may reject a proposal its head adopted; members may refuse to carry out an adopted decision. Scenario 15 is these outcomes.
5. **Compulsion of an official** compels that official's attempt at an available action: to propose, to vote, to hand over goods it can reach, to decide where it alone has `DecideAlone`. It does not compel any other member, and it cannot grant a power the official lacks.
6. **Possession of a leader** lets the player perform that leader's available actions, including proposals, votes and decisions within its powers. Each decision taken while possessed is recorded with `DivinelyControlled = true` (truth; analytics and the possessed NPC's own memory). Other members see only what they perceive.
7. **Miracles** change the organisation's physical state like anyone's: goods created in its store with ledger entries, a building smitten, a healer healed. They never change records, offices, votes or policies directly.
8. **No administrative authority from divinity.** A god gains no office, vote or power by being worshipped, and a religious organisation's claim to speak for its god is a claim other NPCs may or may not believe.

### 4.13 Lifecycle: growth, decline, failure, merger, split
- **Growth:** members join, contributions and income exceed spending, assets are acquired, services gain clients. Measured, never awarded.
- **Decline:** members leave (their own scores: unpaid, distrust of the head, better options), clients go elsewhere, stores run down.
- **Dormant:** no living office-holder with `Propose` for `DormantDays`; standing policies continue to be followed by whoever still holds them; nothing new is decided. Filling an office (a member proposes itself, per the rule) revives it.
- **Dissolution:** by decision under the charter's `AmendRule`, or when the last member leaves. Assets are distributed by `DissolutionRule`: `SetOwner` in place (basis `Divided`) to members, to shares, to another organisation, or to ownerless; goods are then collected physically. Open formal obligations become `Orphaned` (creditors keep their claim; nothing transfers it automatically, as for estates).
- **Merger:** both organisations adopt the same merger decision under their own rules. The survivor keeps its `OrgId`; the other's assets pass by `SetOwner` (basis `Founded`), its obligations are re-registered with the survivor as debtor or creditor (claims and liabilities unchanged in total), and each of its role-holders is **offered** an equivalent role, which it may refuse. `OrgMerged`.
- **Split:** members adopt a split decision, or a group leaves and founds a new organisation with an asset share the charter's rule gives (or contests it). The new body gets a new `OrgId` and `SplitFrom`. `OrgSplit`.
- **Takeover by force or law** is a seizure (basis `Seized`): real staff must take the goods; the former members remember it.

### 4.14 Order of a sim day and cost
Once per day, after households and before institutions' services run: (1) obligations falling due are checked and flagged (`OrgObligationDue`); (2) per organisation in ascending `OrgId`: status updates (insolvent, dormant), then its decision slot runs during the day at its own tick; (3) ratification at meeting times; (4) records written by role-holders' actions. Per organisation per day, the cost is O(offices + members present + open requests). Bounds: at most 256 organisations per game in the first version, 64 role-holdings each, 4 per NPC.

## 5. Interactions with other systems
- **Economy:** jobs, `Work` rights, enterprises, commons, public goods; every organisation production and payment uses the same rules ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)).
- **Services:** requests, queues, activities and outcomes ([INSTITUTIONAL_SERVICES_DESIGN.md](INSTITUTIONAL_SERVICES_DESIGN.md)).
- **State:** the government organisation's treasury, commands, laws and citizenship ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **Institutional management:** manager effort, returns, Goodhart, oversight ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Political economy:** associations are `OrgType.Association`; the Stage 5 `DecisionRule` of POLITICAL_ECONOMY §4.8a is this document's `DecisionRule`, now generic and available to every organisation ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)).
- **Military:** the war band, militia and army are `OrgType.Military` with a `Commander` office ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Households:** a household is **not** an organisation (it keeps its own M4 record), but a household or a group of households can found one; `CareArrangement` is the seed of communal childcare ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md)).
- **Estates:** a deceased member's shares and roles go to the estate or end, by the charter ([MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md)).
- **Learning:** members learn from organisational outcomes (paid on time, store fed them, a decision they backed worked) through `Learner.Outcome` with an organisation episode key ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).
- **Divinity and religion:** sections 4.11 and 4.12; DIVINE_PLAYER_DESIGN, COMMANDMENTS_AND_RELIGION_DESIGN.

## 6. Player and view
- **The player is a god.** Selecting an organisation (its building, a member, or the organisations list) opens its card in **two clearly separated parts**: what the organisation's own records say (its records, returns, decisions as recorded, with recorder and date), and what divine observation shows under the configured rules (D5.28). Debug true state is a developer overlay only.
- **Card contents:** type, purposes, ownership model and shares, charter version, governance rule, offices and holders (click to inspect the NPC), members and employees with terms, store contents, budget lines, open requests, recent decisions with their source (member, request, divine instruction) and outcome, relations.
- **Divine actions on an organisation** (from the contextual menu, PLAYER_UI_DESIGN): instruct the organisation (relayed to its office-holders), or act on one of its people. The result is shown as the recipients' responses and the organisation's decisions, never as an instant change.
- **Opportunities are invisible as such** (they are beliefs); the NPC inspector shows an NPC's `OpportunityBelief` in its own-view tab.
- **Animations only show events:** a founding meeting (gathered NPCs, `AskJoin` and `Accept` gestures), a vote by raised hands, deposits at a store, an official refusing a divine instruction.

## 7. Telemetry
**Domain events** (names; codes allocated by SHARED_ARCHITECTURAL_CONTRACTS):
- lifecycle: `OrgOpportunityFormed` (Full only), `OrgProposed`, `OrgFounded`, `OrgStatusChanged`, `OrgCharterAmended`, `OrgMerged`, `OrgSplit`, `OrgDissolved`;
- roles: `OrgRoleTaken`, `OrgRoleLeft` (reason: left, dismissed, died, merged, dissolved), `OfficeFilled`, `OfficeVacated`;
- decisions: `OrgProposalMade` (source), `OrgVoteCast` (HV, Full), `OrgDecisionTaken` (outcome, as counted; analytics-only true count), `OrgDecisionCarriedOut`, `OrgDecisionRefused` (by whom);
- goods and accounts: `OrgContribution`, `OrgSpend` (each is also an `ItemMoved` or `Consumed`), `OrgObligationCreated`, `OrgObligationSettled`, `OrgObligationDefaulted`, `OrgInsolvent`;
- records and policies: `RecordWritten`, `RecordFalsified` (truth field), `OrgPolicySet`, `OrgRelationChanged`;
- divine: `InstructionRelayed` (instruction id, recipient), with the response event owned by DIVINE_PLAYER_DESIGN.

**Daily organisation snapshot:** id, type, model, status, members, employees, offices filled and vacant, store per ware, obligations owed and owing, decisions adopted and rejected, requests open (services), income and spending by cause.

**Warehouse** (DATA_WAREHOUSE_DESIGN to adopt): `dim_org` (natural key `OrgId`), `fact_org_day`, `fact_org_role` (effective-dated, half-open `[from, to)`), `fact_org_decision`, `fact_obligation`, and `org_key` on existing goods and wage facts.

## 8. Invariants
| # | Invariant |
|---|---|
| ORG-I1 Conservation | An organisation's goods are lots; C1–C6 of SHARED_CONTRACTS hold unchanged with organisation owners. No organisation event changes a goods count except through `Produced`, `Consumed` or `ItemMoved`. |
| ORG-I2 Claims balance | For every formal obligation the debtor's liability equals the creditor's claim; settling one moves goods by hand-over and changes the obligation by exactly the goods moved. Creating, defaulting or writing off an obligation changes no goods count. |
| ORG-I3 A decider for every decision | Every `OrgDecisionTaken` names a living NPC who held an office with the needed power at that tick, or a meeting whose counted votes meet the rule. |
| ORG-I4 An actor for every effect | Every material organisation effect (spend, hire, handout, treatment, fine) has an NPC action event with the organisation's id and an actor within reach. |
| ORG-I5 No staff, no effect | An organisation with no living role-holder emits no service, spend, hire or decision events. |
| ORG-I6 Information separation | No organisation policy references `GodState`, the warehouse or truth fields (reflection test); `InstitutionView` is built only from the decider's mind and readable records. |
| ORG-I7 Ownership integrity | Every lot owned by `Org(x)` has x alive in `OrgState` or dissolved fewer than `DissolveCollectDays` ago; every role-holder is a living NPC; roles and `OrgBelief`s are consistent for the role-holder's own role. |
| ORG-I8 No spawned staff | Every role-holder had an origin event (founder, birth, immigration) before taking the role (I-INV-1 kept). |
| ORG-I9 Authority distinct | No code path changes citizenship, government office or loyalty because of a religious role, and no god id appears as a role-holder. |
| ORG-I10 Determinism | Replay and save/load continuation give identical hashes with organisations active, recording on or off. |

## 9. Tier and package mapping
Package ids are the brief §39 provisional ids; the package index may rename them.

| Capability | Tier | Package | Minimal first version |
|---|---|---|---|
| `OwnerKind.Org`, `BinKind.OrgStore`, org-owned lots and bins | 1 (needed by the Tier 1 labour economy as soon as any owner other than an NPC or household works) | C1 | Owner value, bin kind, `Permits.Believed` basis `OrgRole`; conservation tests pass with org owners |
| Formal obligation registry | 2 | C2 | Wage arrears and loans as double-entry records; ORG-I2 |
| Enterprises as organisations, employment | 2 | C3 | Private and cooperative workshop organisations posting jobs; output owned by the organisation |
| Military organisation | 3 (expanded military) | C4 | War band org with a Commander office over MILITARY's assignments |
| Associations, legitimacy links | 3 | C5 | Association type and `DecisionRule` reuse only |
| **Organisation record, roles, offices, `DecisionRule` (OwnerDecides, HeadDecides, MajorityPresent), records, policies, opportunity and founding, dissolution** | **2** | **C6** | One template (shared food store) founded by two households from need evidence; one private template (healer's practice); decisions by `HeadDecides` and `MajorityPresent` |
| Services on the framework | 2 | C7, C8 | See INSTITUTIONAL_SERVICES_DESIGN |
| Justice organisations | 3 | C9 | See INSTITUTIONAL_SERVICES_DESIGN |
| **Government as an organisation, offices, the AiBrain government seat, relayed divine instructions, religious organisations** | **2** (refusal by a government is scenario 15) | **C10** | Government org per faction with Head and Council; `InstructionRelayed`; adopt, modify or refuse; one temple template |
| Merger, split, Council and Consensus rules, inter-organisation regulation | 3 | C6, C10 | After the first version works end to end |
| Effects of welfare and childcare organisations on survival | 2 | F2 | Used as treatments in demographic viability experiments |

## 10. Stage 4 milestones and later
| When | Delivers |
|---|---|
| M6 (kept) | State-owned buildings and the treasury, now as the government organisation's assets (alias `State(f)`) |
| M8 (kept) | Enterprises, now `OrgType.Enterprise` on this framework |
| M12 (kept, re-based) | The generic institution model of INSTITUTIONAL_STATE runs on this record; Band-age bodies founded by individuals or by the government |
| D5 run (C6, C10) | Sections 3–4 minimal versions (table above) |
| Stage 5 | Council, Consensus, Election and Rotation rules; mergers and splits; federations; associations at scale |
| Later | Chartered companies, leases, regulators, multi-faction religious organisations, private and mercenary military provision |

## 11. Migration from the existing code
- **No organisation code exists today.** `InstitutionView` (`Assets/God/Sim/Policy/Observations.cs`) is a placeholder with `InstitutionId`, `Faction`, `Tick`; it gains `OrgId` (same value space), `DeciderNpcId` and a `MindView`, and keeps its name so `IInstitutionPolicy` is unchanged.
- **`InstitutionId` and `EnterpriseId`** in the older documents read as `OrgId`.
- **`StateRecord`** (STATE_DESIGN §3) becomes the government organisation's type-specific data; its fields are unchanged.
- **`AiBrainFactionPolicy`** stays as the government's routine decision procedure (temporary mechanism "AiBrain government seat", to be listed in [DEV_LOG.md](DEV_LOG.md#temporary-mechanisms)), with the Head as actor; the Stage 3 mode keeps the human faction seat (temporary mechanism "Stage 3 mode").
- **`EcoBuilding.Faction`** stays as the building's faction; ownership of state buildings is recorded as `Org(gov)` in the property registry from M6.

## 12. Tests and acceptance hooks
"Significant" means a two-sided Mann–Whitney test at p < 0.05 over the stated seeds. Effect sizes are reported, never tuned to a target.

**Invariant and unit tests (every build):**
- **ORG-INV-1** ORG-I1 and ORG-I2 hold over 20 seeds × 30 days with organisations founded, funded, merged and dissolved (exact).
- **ORG-INV-2** ORG-I3 and ORG-I4: every decision and every material effect is attributed (exact, from events).
- **ORG-INV-3** ORG-I5: a fixture organisation with all role-holders dead emits nothing for 10 days (exact).
- **ORG-INV-4** ORG-I6 reflection test over `Sim/Orgs` and `Sim/Policy`.
- **ORG-INV-5** Static test: production, service and exchange code does not branch on `OwnershipModel` or `OwnerKind.Org`.
- **ORG-INV-6** Threshold makes available, never builds: with a template's unlock rule satisfied and no NPC holding the practice or the need evidence, no `OrgFounded` occurs over 30 days on 20 seeds (exact).
- **ORG-INV-7** ORG-I10 replay and save/load continuation parity with organisations active.

**Acceptance hooks (brief §47):**

| Scenario | Hook | Test |
|---|---|---|
| 1 Autonomous population | Organisations are optional: with organisations disabled the population still runs; with them enabled and no god, organisations found, operate and fail by themselves | **ORG-ACC-1** 10 seeds × 5 sim years, divine intervention disabled: at least one `OrgFounded` by NPCs on at least 5 seeds; every invariant holds; reported, not targeted |
| 2 Trade | An authorised member barters on the organisation's behalf with the ordinary hand-overs | **ORG-ACC-2** a keeper trades organisation berries for logs; both `ExchangeStep`s carry `Org` as owner on one side; C1 holds |
| 3 Employment | An organisation posts a job, a non-member works its asset under a `Work` right, and a treasurer hands over pay | **ORG-ACC-3** fixture: output owned by `Org(x)`; `WagePaid` by an officer within reach; an unpaid wage creates one formal obligation |
| 4 Independent institution | Individuals recognise a recurring need and found an organisation with no player action | **ORG-ACC-4** two households with alternating food shortfalls and the "store food" practice: a shared-store organisation is founded by member agreement in at least 10 of 20 seeds within 20 days; with the practice absent (fixture), none; with the need absent (equal food), significantly fewer foundings (extends I-COM-2) |
| 5 Healthcare, 6 Education, 7 Lending | Delivered through organisations or informally | INSTITUTIONAL_SERVICES tests SVC-HEAL, SVC-EDU, SVC-LEND |
| 15 Government autonomy | A divine instruction to the government is relayed, and the government refuses or modifies it by its own decision | **ORG-ACC-15a** fixture: instruction "fund a healer with 5 berries a day" to a government whose Head has low trust in the god: `InstructionRelayed` to every office-holder present; on at least 18 of 20 seeds no adopted decision matches it unchanged (refused, ignored or modified), and the reason terms are logged. **ORG-ACC-15b** same, high trust Head, council rule: the Head adopts it; the council's counted votes decide; both outcomes occur across 20 seeds with varied councillor interests. **ORG-ACC-15c** compulsion of the Head makes it propose, but no councillor's vote changes because of it |
| 16 Religious and political separation | Changing religious organisation leaves citizenship and government roles unchanged | **ORG-ACC-16** an NPC leaves one temple and joins another: `CitizenData.Faction`, its government offices and loyalty are unchanged by that change (exact); ORG-I9 static check |
| 17 Analytical reconciliation | Warehouse organisation facts reconcile to events | **ORG-ACC-17** from a 10-day run: members, role spells, store totals and obligations in `fact_org_*` equal those rebuilt from the event stream (exact) |

## 13. Open questions
Genuinely consequential ones only; each has the default the run uses.
1. **`OwnerKind.Org` and the `State` alias.** Adding `OwnerKind.Org = 7` touches the frozen goods contract (save, hash, the static goods test). Default: add it in F1 or C1 under the contracts owner, keep `State(f)` as an alias resolved to the faction's government. Decision for SHARED_ARCHITECTURAL_CONTRACTS.
2. **The AiBrain government seat.** Keeping `AiBrain` as the government's routine procedure is the only practical way to keep the economy running in the run, but it is a faction-level script that does real work. Default: keep it as a declared temporary mechanism attributed to the Head office, require a living Head, and route every individual or divine proposal through the council rule. The owner may prefer that the first god-only builds start with no government and let one be founded; that is a scenario option either way.
3. **Initial governments.** Whether every faction starts with a government organisation (default yes: council of the oldest adults, D5.2) or some scenarios start without one (supported).

# Knowledge and technology design (owner D6, 27 Nov 2026)

The explicit ontology of technological knowledge (owner decisions **D6.9–D6.12**): what a person knows, what a design is, what a document is, who owns a document, what a patent and a licence are, what a trade secret is, what it takes to actually build and operate something, how research and invention work, how a god's revelation fits, and the one auction mechanism for designs, rights and assets. It also defines four new organisation templates on the generic framework: universities, research organisations, patent offices and commercial courts (D6.15).

**Status: plan only. Nothing in this document is built.** Owner decisions: [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md) D6.9, D6.10, D6.11, D6.12, D6.15, D6.16 (and D5.6, D5.16, D5.17, D5.28, D5.33); brief: [OWNER_BRIEF_2026-11-27_INDUSTRY.md](../civdiv/OWNER_BRIEF_2026-11-27_INDUSTRY.md) "additional owner decisions" §§5–8, 11. Cross-package numbers are fixed by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (SAC); this document names and requests them (section 14). "Owner D6" means the decision set; "package D6" is SAC's commandments package.

**Packages.** **K1** (knowledge, designs, artefacts, licences, patents minimal) owns this document. Its Tier 2 minimal part is on the industrial vertical slice (D6.18 steps 7 and 8 need a design, construction and operating ability) and covers the D6 acceptance tests D6.5–D6.8. Patent litigation in full, challenges and revocation by court, compulsory licensing, research projects, invented variants and auctions are Tier 3 (D6.18: "patent litigation, advanced research ... are later extensions"). Payments are K2's ([MONEY_AND_ACCOUNTING_DESIGN.md](MONEY_AND_ACCOUNTING_DESIGN.md)).

---

## 1. Purpose and scope

**This document owns:**
- the eight distinct things of D6.9: individual knowledge, formal designs, knowledge artefacts, ownership of artefacts, patents, licences, trade secrets and practical ability (section 3);
- the `DesignDef` schema: the data-driven definition of a productive capability and its construction and operating requirements, used by every industrial document for its technology and recipe data (section 4);
- how knowledge is copied (teaching, observation, reading, revelation) without ever being removed from its holder, how it is forgotten and lost, concealed, sold and stolen (sections 5, 7);
- knowledge artefacts as goods: writing, copying, reading, owning, destroying (section 6);
- rights: patents and licences as legal claims recognised per government, their registration, transfer, licensing, challenge, revocation, expiry, enforcement, and the alternatives (open knowledge, public ownership, cooperative research, secrecy, compulsory licensing) (sections 8, 9);
- research and invention (section 10), divine revelation of technology (section 10.4);
- the general auction mechanism (section 11);
- the distinction owning a design / permission to use / possessing knowledge / constructing (section 12);
- the four organisation templates (section 13).

**It links to, and does not repeat:**
- associations and their origins (`Own`, `Vicarious`, `Imitated`), observation and imitation: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) §4.13–4.14, `Assets/God/Sim/Learning/AssociationStore.cs`;
- cultural traits (Technique, Norm, Belief ...), innovation by practice, the adoption test: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) §3, §4.3, §4.6;
- teaching, demonstration and apprenticeship as services; justice: [INSTITUTIONAL_SERVICES_DESIGN.md](INSTITUTIONAL_SERVICES_DESIGN.md) §5.2, §5.6 and its owner D6 section;
- commandment knowledge (a separate store): [COMMANDMENTS_AND_RELIGION_DESIGN.md](COMMANDMENTS_AND_RELIGION_DESIGN.md);
- the `RevealKnowledge`/`RevealTechnology` miracles: [MYTHIC_AND_MIRACLES_DESIGN.md](MYTHIC_AND_MIRACLES_DESIGN.md) §6.10–6.11;
- organisations, offices, powers, records: [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md); laws: [STATE_DESIGN.md](STATE_DESIGN.md) and C10;
- the actual designs (mines, refineries, generators, factories, vehicles, weapons), their recipes and construction: INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md, VEHICLES_AND_HARVESTING_DESIGN.md, ELECTRICITY_DESIGN.md, INDUSTRIAL_MILITARY_DESIGN.md, WASTE_AND_CIRCULAR_DESIGN.md (they fill the design table with this schema);
- payments, fees, the books: [MONEY_AND_ACCOUNTING_DESIGN.md](MONEY_AND_ACCOUNTING_DESIGN.md);
- the nine ontological categories: [WORLD_ONTOLOGY.md §11](WORLD_ONTOLOGY.md); warehouse tables: [DATA_WAREHOUSE_DESIGN.md §13](DATA_WAREHOUSE_DESIGN.md); KPIs: [KPI_CATALOGUE.md §16](KPI_CATALOGUE.md); tests: [ACCEPTANCE_TESTS.md D6](ACCEPTANCE_TESTS.md).

## 2. Principles
1. **Knowledge lives in minds.** Only a person knows. An organisation "knows" a design only in the sense that some of its members know it or it owns documents that describe it. When the last holder dies untaught and no document survives, the design is lost (it may be rediscovered).
2. **Copying knowledge never removes it** (D6.9). Teaching, being watched, being read and being revealed add knowledge to the learner; the source's knowledge is unchanged by the act (KN-I1). Knowledge is non-rival; documents are rival goods.
3. **A design never builds anything** (D6.10, D6.12). Knowing, owning a document of, holding a patent on or winning an auction for a design produces no asset. Construction needs materials at the site, workers with the practical ability, time, tools and, where required, electricity.
4. **Rights are legal claims, not properties of knowledge** (D6.11). A patent exists only where a government's law recognises it and a patent office has registered it. It never stops anyone physically copying, building or using: it gives its holder a claim that courts may enforce through evidence and remedies. There is no universal patent system; a world with no patent law is normal.
5. **Concealment is who knows.** A trade secret is nothing but the fact that few minds hold a design and its documents are kept where others cannot reach them. Secrets leak through teaching, observation, theft of documents and departing workers.
6. **Divine revelation gives knowledge, not rights** (D6.10). A revealed design enters a mind; it confers no ownership, no patent and no asset.
7. **No technology is favoured and none is scheduled.** Designs are discovered, spread, lost or ignored by the mechanics; no age or threshold hands out technologies (CULTURAL_TRANSMISSION 4.6 "never by design" kept; D5.33).
8. **Information separation** (D5.28). A patent examiner decides novelty from its own registry and the documents it has; a judge decides infringement from evidence it holds; neither reads truth. The sim's truth classification of infringement is analytics only.
9. **Determinism.** Per-NPC knowledge updates inside that NPC's acts in tick order; day-start decay in ascending NpcId; registries in ascending id; copy errors and discovery draws from `Rng.Fork("know", npcId).Fork(purpose)` and `Rng.Fork("research", projectId)`.

## 3. The eight things (D6.9), kept distinct

| Thing | What it is | Where it is (truth) | How it changes | Example |
|---|---|---|---|---|
| **Individual knowledge** | A person's understanding of a design, at a level from "heard of it" to "can teach it" | `KnownDesign` entries in the person's `KnowledgeMind` (layer 6, section 5) | rises by research, practice, teaching, observation, reading, revelation; falls by forgetting; ends at death (kept in the mind archive, SAC 4.4) | Ada understands the coal-fired boiler at level 180 |
| **Formal design (blueprint)** | A reproducible, data-driven definition of a productive capability and its construction and operating requirements | `DesignDef` content rows (`SimTuning.Civ.Knowledge.Designs`), id `DesignId`; discovery state in `DesignRegistry` (section 4) | content is fixed for a run; "formalised" when the first document of it exists | design 210 "Steam harvester mk1" |
| **Knowledge artefact** | A physical document (drawing, manual, tablet) that describes one design at some fidelity | a lot of ware `Document` (count 1, never merges) plus its `ArtefactRecord` (design, fidelity, origin) (section 6) | written or copied by labour and materials; read; moved; stolen; burned | the refinery's only copy of the cracking manual, in its office store |
| **Ownership of an artefact** | Who owns that document lot | the lot's `Owner` (goods API), as for any good | by hand-over, sale, gift, theft, inheritance (goods rules) | the refinery organisation owns the manual |
| **Patent** | A government-recognised, registered claim to exclusive rights over a design in a jurisdiction, for a term | a `Right` of kind `Patent` in `RightsState`, registered at a patent office organisation (section 8) | applied for, granted, assigned, licensed, challenged, revoked, expired, lapsed | Patent 7: design 210 in the Northern government's jurisdiction, holder Harbour Works, until day 400 |
| **Licence** | Permission from a right holder (or a secret's owner, by contract) to manufacture, use or sell under a design, exclusive or not, for a term and fee | a `Right` of kind `Licence` with a parent right or a contract (section 8.4) | granted, paid for, expired, terminated, enforced | an exclusive manufacture licence of patent 7 to the Coop Foundry for 100 days |
| **Trade secret** | A design known only to some, protected only by concealment and contract | not a record: the set of minds that hold it and the places its documents are kept (section 7) | leaks by teaching, observation, document theft, departure | the refinery keeps its catalyst design secret |
| **Practical ability** | The skill to construct or operate equipment of a design: knowing *how*, in the hands | the person's skill in the design's skill class (`SkillClass`, section 5.3) plus its knowledge level | grows by practice, apprenticeship and teaching; decays with disuse | Bo can drive harvesters (driving 420) but has never built one |

**Owning a design** (D6.12) is therefore never one field: it is shorthand for holding a patent (or, under a law that allows it, a registered design title) on it in some jurisdiction. Owning a *document* of a design is ownership of goods. Neither is knowing it, and neither is having built it (section 12).

## 4. Formal designs: the data-driven definition (used by the industrial documents)

### 4.1 `DesignDef`: the knowledge envelope around an industrial payload
A design has two halves. The **payload** is the productive capability and its physical requirements, owned by the industrial document of its kind and already keyed by `DesignId` there: a recipe row (`RecipeBook`, INDUSTRIAL_RESOURCES §4), a facility definition (INDUSTRIAL_RESOURCES §3.5), a `VehicleDesign` (VEHICLES §3.1), a `GeneratorDesign` (ELECTRICITY §3), military equipment (INDUSTRIAL_MILITARY), waste processes (WASTE). Construction materials, labour ticks, crew, inputs, power and wear live **only** in the payload. The **envelope** (`DesignDef`, owned by K1) holds what is about knowledge:

```text
DesignDef                             // content, read-only during a run, saved with SimTuning.Civ.Knowledge
  Id: DesignId (ushort; 0 = common knowledge: no knowledge check at all, as the industrial documents use it;
                content ids 1..0x7FFF stable across versions; 0x8000+ reserved for invented variants, Tier 3)
  Key: string (stable content key, e.g. "vehicle.harvester.mk1")
  Kind: Recipe | Facility | Vehicle | Generator | GridElement | Equipment | Weapon | Process | Improvement
  Payload: { Table: PayloadTable, Row: int }   // the industrial row that this design unlocks (one design may unlock
                                               // a facility and its recipes: Payload[<= 4])
  MinBuildKnowledge: byte, BuildSkill: SkillClass, MinBuildSkill: short    // for whoever leads construction
  MinOperateKnowledge: byte, OperateSkill: SkillClass, MinOperateSkill: short  // for operators and workers
  Prerequisites: DesignId[<= 4]       // designs a researcher or learner must already understand
  Parent: DesignId                    // the design this one improves (0 = none)
  Complexity: short                   // drives learning, copying and research time
  DocumentPages: byte                 // material and time to write or copy a document of it
```
- **One envelope table, one owner.** K1 owns the envelope schema and loader; each industrial package adds the envelope row for every payload row whose `DesignId ≠ 0`. No industrial document defines a second knowledge or licensing table, and K1 never duplicates a payload field.
- **Existing recipes stay common.** The stage 3 buildings and processed wares (`Assets/God/Sim/Economy/BuildingDefs.cs`, `Wares.cs`), which the industrial recipe adapter imports (INDUSTRIAL_RESOURCES IND-3), and the M3–M5 activities keep `DesignId = 0`, so nothing already playable needs a learner. A CULTURAL_TRANSMISSION Technique trait that is a productive recipe sets `Hook = DesignId` (one route, not two).
- **A design is not an instance.** Each built asset records the `DesignId` it was built from (facilities, vehicles and stations already carry it in the industrial documents); many assets share one design.

### 4.2 `DesignRegistry` (truth state, `CivState.K1`)
Per `DesignId` that has ever been known: `FirstKnownTick`, `FirstHolder` (NpcId), `How` (Seeded | Practice | Research | Revealed), `Formalised` (any document ever written), `Lost` (no living holder and no document) and `LostTick`. Counts of holders are analytics (computed by the warehouse), never read by a rule.

### 4.3 What gates a facility, a vehicle or a recipe (and what never does)
The industrial runners check **physical and knowledge conditions only**: materials, workers present, power, and that a present worker (or operator) meets the envelope's knowledge and skill minima (5.3) or follows a document at the site (6.3). **A right is never a truth gate.** Whether a patent is in force and whether the owner holds a licence is classified after the fact (`UnauthorisedDesignUse`, section 9) and may lead to a dispute; it never stops a cycle, a construction or a drive (D6.11: "Knowledge can physically be copied even when that copying is legally prohibited").

An owner that wants to respect rights does so by **its own decision**: an organisation policy `RespectRights` (default on), which its manager follows by its own scores when placing orders, exactly as staff follow any policy (ORGANISATIONS §4.8), using what it *believes* about rights (its records, `Permits.Believed`). A law-abiding firm therefore rarely infringes; a reckless one can.

**Conflict flagged for the lead (amendment row needed).** INDUSTRIAL_RESOURCES §4.4 step 1 ("usable by the facility owner: the owner holds a licence or the design is open, and at least one present worker knows the design", stall reason `NoDesign`) and ELECTRICITY §3 ("its owner must hold the design's licence or open access") make the licence a truth gate. This document's rule is: the truth gate is the knowledge half only (`NoDesign` means no present worker knows it and no usable document is at the site); the licence half becomes the owner's `RespectRights` decision. Without this change, test D6.8 (unauthorised use creating a potential legal dispute) cannot occur in industrial production. The industrial documents are not edited by this package.

## 5. Individual knowledge and practical ability

### 5.1 The per-person store
```text
KnowledgeMind                             // NpcMind.Know (new mind slot, section 14), null unless Civ.Knowledge.Enabled; hashed when present
  Known: KnownDesign[<= 24], Count         // eviction when full: lowest Level x recency, never one in use today; ties by DesignId
  Skills: short[SkillClassCount]           // practical ability, permille (only if C7 has not landed shared skill slots; see 5.3)

KnownDesign
  Design: DesignId
  Level: byte          // 0-63 heard of; 64-127 understands (can follow a document, can be taught further); 128-191 can build
                       // and operate to the design's knowledge minimum; 192-255 can teach, write it down and research from it
  Fidelity: short      // permille: how complete and correct the person's version is (errors from copying, misheard teaching)
  Source: Seeded | Practice | Research | Taught(npc) | Observed(npc) | Read(artefact lot) | Revealed(intervention)
  SinceTick, LastUseDay
```
Levels use the same 0..255 strength scale as culture traits and commandments (CULTURAL_TRANSMISSION §3.2, COMMANDMENTS §12), so the adoption maths can be shared.

### 5.2 How knowledge is gained, and why the source keeps it

| Channel | Act (each an ordinary action of the people involved) | Gain for the learner | The source |
|---|---|---|---|
| **Teaching** | C7's `Teach` with topic = DesignId: teacher (level ≥ 192) and learner within gesture range for `TeachTicks × Complexity / 100` | `Level += TeachGain × teacherFidelity × attention / 10^6`, capped at the teacher's level − 16; `Fidelity` = teacher's × (1000 − `MisteachPermille` × draw) | unchanged (it spent ticks) |
| **Demonstration and observation** | the learner attends to someone building or operating equipment of the design within sight (`Demonstrate`, or ordinary watching) | small gain (`ObserveGain`), capped at 127 (understanding, not building); repeated watching needed | unchanged; it need not consent (observation of a guarded site is classified in 7.2) |
| **Reading** | `Study` (new verb, section 14): the reader holds the document in its carry or within `Reach.Touch` of it in a bin it may open, for `ReadTicks × DocumentPages`; literacy: skill `Reading` ≥ `MinReading` (tuning; 0 in the industrial scenario's adults) | `Level` rises towards a cap set by the document's fidelity (≤ 191; teaching or practice is needed for teaching level) | the document is unchanged (no wear by default); the writer is unaffected |
| **Practice** | building or operating it | +`PracticeGain` per cycle up to 255; skill rises (5.3) | — |
| **Research** | section 10 | discovery at level 128 | — |
| **Revelation** | the `RevealTechnology` miracle (10.4) | level 128 (or 64 without the prerequisites) | — (the god "knows" nothing in this sense) |

**KN-I1:** no act lowers another person's `Level` or `Fidelity`; the only decreases are the holder's own forgetting and death. A static test lists the only writers of `KnownDesign` (K1's `Knowledge` class, called from those acts and the day-start decay).

**Forgetting:** at day start, an entry not used (built, operated, taught, read) for `ForgetAfterDays` loses `ForgetPerDay`; at 0 it is removed (`KnowledgeForgotten`). Death removes the store with the mind (archived for resurrection by D7 if configured, SAC 4.4; the archive gains the `Know` field).

### 5.3 Practical ability (skills)
- **Distinct from knowing.** To build a unit a worker needs `KnownDesign.Level ≥ MinBuildKnowledge` (or a document at the site, 6.3) **and** `Skill(BuildSkill) ≥ MinBuildSkill`; to operate it, `MinOperateKnowledge` and `Skill(OperateSkill) ≥ MinOperateSkill`. Below the skill minimum the act is refused with `NoSkill` (the existing `Affordances` flag `NoSkill = 64`, `Assets/God/Sim/Cognition/Affordances.cs`); between the minimum and mastery, work is slower and wears equipment faster (the industrial documents' rule).
- **Skill classes** (`SkillClass`, small enum): `Construction, Mechanics, Electrical, Driving, Mining, Refining, Chemistry, Metalwork, Reading, Research, Medicine (C7's Healing)`. Skills grow by practice (`SkillGainPerCycle`, scaled by the LearnRate phenotype as POPULATION's skills say) and apprenticeship (INSTITUTIONAL_SERVICES §5.2), decay slowly with disuse.
- **One skill store.** POPULATION_DESIGN owns the skill record in principle but none exists in code (no `Skills` field on `Npc`). INSTITUTIONAL_SERVICES open question 1 lets C7 add skill slots. Rule: **whichever of C7 and K1 merges first lands the slots** (`NpcMind.Know.Skills` by K1, or C7's), and the other reuses them; the lead records which in the addendum. The slice needs `Driving`, `Mechanics`, `Construction` and `Electrical`.

## 6. Knowledge artefacts (documents)

### 6.1 A document is a good with content
- Ware **`Document`** (section 14), always **count 1**, never merges (the goods merge rule's "tools never merge" generalised to `Wares.IsUnique(w)`, section 14). Its lot id is its identity for its whole life; it is owned, carried, stored, handed over, stolen, inherited and destroyed by the goods API exactly as any good (C1–C6 hold).
- **`ArtefactRecord`** (`CivState.K1.Artefacts`, ascending lot id): `{ Lot, Design, Fidelity, Origin: Written(npc) | Copied(npc, fromLot) | Seeded, MadeTick, Pages }`. The record follows the lot; when the lot is consumed (burned, rotted, a miracle) the record closes (`ArtefactClosed`). **AR-I1:** every live `Document` lot has exactly one open record and every open record a live lot.

### 6.2 Writing and copying (labour and materials)
- **Writing (formalising):** a person with level ≥ 192 spends `WriteTicks × Pages` at a desk (a workplace asset) with `Pages` units of a writing material (`Paper` in the industrial ware block, or clay or hide in earlier content), consumed as production inputs (`ConsumeReason.ProcessInput` 8 of the widened ledger, INDUSTRIAL_RESOURCES §3.2); produces a `Document` lot (`ProduceReason.Crafted`, F1) owned by whoever the work is for (the writer, or its employer under a `Work` right). `Fidelity` = the writer's fidelity × `WriteAccuracy`. First writing of a design sets `DesignRegistry.Formalised` (`DesignFormalised`).
- **Copying:** a person holds the source document (or it is within reach) and has `Reading` skill, spends `CopyTicks × Pages` with the materials; produces a new `Document` lot; `Fidelity` = source × `CopyAccuracy` (a deterministic draw may drop a further amount: copying errors accumulate). The source is unchanged. Copying from memory is writing.
- **Authorisation is a classification, not a block.** The sim classifies a copy as *unauthorised* (analytics truth, `ArtefactCopied.Authorised = false` †) when the copier is not the document's owner and has no permission belief from it, or when a right in force in the jurisdiction reserves copying to its holder. Nothing prevents the copy (D6.11).

### 6.3 Building from a document
A builder whose level is below `MinBuildKnowledge` but ≥ 64 may still build if a document of the design with `Fidelity ≥ MinDocFidelity` is at the site (in a bin there or in someone's carry), at `DocBuildSlowdownPermille` (default 1500 = 50% slower), and gains knowledge by practice. This is what makes manuals worth owning, stealing and selling.

## 7. Selling, sharing, concealing and stealing knowledge
- **Selling:** a document is sold like any good (K2 `Sale`); teaching is sold as a service with a fee (C7 + K2 `Fee`); a licence is sold as a right (8.4). Buying a document gives the buyer a document, not knowledge: someone must read it.
- **Sharing and open knowledge:** a holder may teach anyone for nothing, or write copies into a store with `StoreAccess = Anyone` (a library), or record an `OpenDedication` right (8.1) that announces the design is free in a jurisdiction (prior art against later patents).
- **Concealing (trade secrets):** keep the documents in a store whose access policy excludes outsiders; teach only members; add a confidentiality term to employment contracts (`ContractTerm.Confidential`, C3, Tier 3); guard the site (a guard role prevents uninvited presence, MILITARY/C9 guard rules).
- **Stealing:** (a) taking a document (theft of a lot, C1 `TheftCommitted`); (b) copying a document one can reach without permission (unauthorised copy, 6.2); (c) watching a guarded process from a place the owner forbids (`KnowledgeAcquired` with channel `Observed` and `Trespass = true` †, classified by `Permits.Truth`); (d) a departing worker who knows it (lawful unless a confidentiality term or a right forbids use).
- **Secrets are measured, never protected by the sim.** `KNW-02`/`KNW-06` (KPI §16) count holders and leaks.

## 8. Legal rights: patents and licences

### 8.1 The right record (truth)
```text
Right                                     // CivState.K1.Rights, ascending Id
  Id: RightId (from 1, never reused; allocator Rights.Grant only; counter CivState.K1.NextRightId)
  Kind: Patent | Licence | OpenDedication | DesignTitle      // DesignTitle only where a law provides it
  Design: DesignId
  Holder: Owner                           // Npc, Household or Org; never a god
  Jurisdiction: OrgId                     // the government organisation whose law recognises it (0 = none: a contract-only licence)
  Office: OrgId                           // the patent office that registered it (0 for contract-only licences)
  Parent: RightId                         // a licence's patent (0 for a patent or a licence of an unpatented secret)
  Scope: Manufacture | Use | Sell | License (bitmask)
  Exclusive: bool                         // licences: excludes other licences of the same scope in the jurisdiction
  PriorityTick, GrantedTick, ExpiresTick
  Deposit: lot id                         // the specification document handed to the office at application (its evidence of priority)
  Terms: FeeObligation, RoyaltyPerUnit, RoyaltyUnit (UnitRef)   // K2/C2
  Status: Applied | Granted | Refused | Challenged | Revoked | Expired | Lapsed | Surrendered | Ended
```
- **Not goods, not knowledge.** A right has no lot and no location. Holding it teaches nothing; losing it forgets nothing.
- **RT-I1** every right has exactly one holder; **RT-I2** a licence's scope and term never exceed its parent's; **RT-I3** an exclusive licence and any other licence of an overlapping scope on the same parent never overlap in time; **RT-I4** no right exists in a jurisdiction whose government has no patent law in force (except contract-only licences with `Jurisdiction = 0`); **RT-I5** no right has a god as holder or is created by a miracle.

### 8.2 Patent law is per government (no universal system)
A patent exists only under a **patent law**: a C10 `Law { OrgId (government), Kind = PatentLaw, Rules }` adopted by that government's own decision, with rules: `TermDays`, `Novelty: AgainstRegistry | AgainstKnownPriorArt`, `ApplicationFee`, `RenewalFee` and period, `CompulsoryLicensing: None | ByCourt | ByOffice`, `PublicDisclosure: bool` (granted specifications are copied into the office's open library), `RecognisesForeign: bool`. Different governments may have different laws or none; a design can be patented by different holders in different jurisdictions. Until C10 lands, a scenario may seed a patent law on the faction's government alias (`OwnerKind.State` temporary mechanism of SAC 7.1), listed in the DEV_LOG.

### 8.3 Lifecycle (each step an act by a person)

| Step | Who does what | Record and events |
|---|---|---|
| **Apply** | the applicant (or its officer) walks to the patent office, hands over a specification document of the design (`Goods.Move`, the office now owns that copy) and pays the application fee (K2 `Fee`) | `Right` with `Applied`; a service request of kind `RegisterRight` (INSTITUTIONAL_SERVICES owner D6 section); `RightApplied` |
| **Examine** | an examiner (office with the `Examine` power) spends `ExamineTicks × Complexity` reading the deposit, compares with the office's registry and the documents it holds | decides from what it holds: novelty fails if the office knows an earlier right, open dedication or dated document of the design; the examiner may miss prior art it has never seen |
| **Grant or refuse** | the examiner's decision, written by the office recorder | `RightGranted` (`GrantedTick`, `ExpiresTick = Priority + TermDays`) or `RightRefused` |
| **Assign** | holder and assignee (or their officers) appear at the office; the clerk records the new holder; the price, if any, is a K2 payment between them | `RightAssigned`; **no goods, documents or assets move** (D6 test 6) |
| **License** | the holder grants a licence (8.4), recorded at the office if the law requires | `LicenceGranted` |
| **Renew or lapse** | the holder pays the renewal fee at the office by the due day, or the right lapses | `RightRenewed` / `RightLapsed` |
| **Challenge** (Tier 3) | anyone files at the commercial court or the office with evidence of prior art (an earlier dated document, an earlier right, testimony) | `RightChallenged`; the decision may revoke (`RightRevoked`) |
| **Expire** | day start after `ExpiresTick` | `RightExpired` |
| **Surrender, dedicate** | the holder declares it at the office | `RightSurrendered`; optionally `OpenDedication` |

Divine revelation of a design (10.4) gives the person knowledge only. If that person applies for a patent, the office treats the application like any other; the god's involvement is visible only to analysis (`Source = Revealed` in the mind).

### 8.4 Licences
- **Patent licences:** granted by the patent holder (scope ⊆ the patent's), exclusive or non-exclusive, for a term, with a fee (`LicenceFee` obligation, paid via K2) and optional royalty per unit produced (`Royalty`, accrued at day start from the truth production count under the licence, MONEY §7.1).
- **Contract licences of a secret:** a holder of unpatented knowledge may sell access (documents plus teaching) under a contract with use restrictions; `Jurisdiction = 0`; enforceable only as a contract dispute.
- **Compulsory licences** (Tier 3): where the law provides, a court or office grants a licence against the holder's will at a set royalty (`CompulsoryLicenceGranted`).
- **A licence is permission, not ability.** A licensee still needs knowledge (documents, teaching from the licensor, or its own) and the practical ability, materials and labour to build (D6 test 7).

### 8.5 Alternatives to patents (all supported, none favoured)
Open knowledge (teach freely, open libraries, `OpenDedication`); public ownership (a government organisation holds the patent and licenses freely or to its own enterprises); cooperative research (a research cooperative's charter distributes rights to members or dedicates them); trade secrecy (section 7); compulsory licensing (8.4); no law at all. Which arrangement spreads technology faster, or concentrates productive ownership, is measured (KPI §16), never assumed (D5.33).

## 9. Unauthorised use and enforcement
- **Truth classification (analytics only).** When an asset of design D is completed, when a production cycle of a recipe design D completes, or when a product of D is sold, in the jurisdiction of a government where a `Granted` patent on D is in force, by a party that is not the holder and holds no licence covering that use, the sim emits **`UnauthorisedDesignUse`** † (its evidence for production is the `Licensed = false` flag that I1 puts on `ProductionCycleStarted`, INDUSTRIAL_RESOURCES §11; for construction, the asset record's `DesignId` at `AssetBuilt`; K1 owns the rights query `Rights.Permits` that sets the flag) (user party, design, right, asset or cycle, jurisdiction, tick). It is like `TheftCommitted`: recorded, never read by a rule or policy, and **it changes nothing** (no block, no obligation, no fine).
- **A potential legal dispute** (D6 test 8) exists from that moment: the holder has a claim it *could* pursue. It becomes an actual dispute only through people:
  1. **Perception:** the holder, an officer or a member of the holding organisation sees the asset or product (sense seam, assets carry their `DesignId` as an inspectable attribute), is told by someone (M5 gossip topic `Infringement`), or inspects it. The perceiver's mind gains an `InfringementBelief { Right, Suspect, Asset, Confidence }` (`InfringementPerceived`). Without perception nothing happens.
  2. **Complaint:** the believer may (by its own score, through C9's `ComplaintMade` with kind `Infringement`) walk to the commercial court of the jurisdiction and accuse. Tier 2 stops here: the complaint exists, is recorded, and the case waits for a court (C9 is Tier 3).
  3. **Case** (Tier 3, C9 framework, INSTITUTIONAL_SERVICES §5.6 and its owner D6 section): evidence items are the registry entry (the court's clerk reads the office's records), the judge's own inspection of the asset, the parties' documents (a dated document proving earlier knowledge is a defence), witnesses.
  4. **Remedies:** a finding of infringement may order an injunction (an order to stop; obeyed only by people's choice and enforcers), damages (a `Damages` obligation to the holder, K2), a compulsory or settlement licence, seizure of products (basis `Confiscated`) — or the court may find the patent invalid and revoke it. Wrongful findings are possible and measured (`TrueInfringement` truth field).
- **Copying remains physically possible throughout.** An injunction does not remove knowledge or documents; destroying documents or dismantling assets is an ordinary physical act someone must perform.

## 10. Research, invention and revelation

### 10.1 Discovery by practice (informal, Tier 2 seam, Tier 3 content)
CULTURAL_TRANSMISSION §4.6's unlock rule becomes the general discovery roll: a person who understands every prerequisite of an undiscovered (or locally unknown) design D at level ≥ 128, and has practised related work (`Attempts ≥ Y`), rolls once a day at `DiscoveryPermille × f(skill, Complexity)`; success gives `KnownDesign(D, 128, Source = Practice)` and `DesignDiscovered`. One function `Discovery.Roll`, used by practice and by research.

### 10.2 Research projects (Tier 3)
```text
ResearchProject                           // CivState.K1.Projects, ascending Id
  Id, Sponsor: Owner (an NPC or an organisation), Target: DesignId (undiscovered design whose prerequisites the team knows) | Improve(DesignId)
  Site: asset id (a laboratory or workshop asset with the design's research equipment)
  Team: NpcId[<= 6], Budget: WareCount[<= 4] (materials allowed per day), Progress: permille, Started, Status
```
- **Research consumes real resources** (D6.10): each researcher's `Research` act (new verb) at the site spends ticks (it does not do other work), consumes experiment materials (`Goods.Consume`, `ConsumeReason.Research`, proposed 13 in the widened ledger, section 14), and may need electricity (the site's demand, ELECTRICITY). Progress per act = `ResearchRate × Skill(Research) × prerequisites understood / Complexity`; the discovery roll's chance rises with progress. Failure is normal; materials are spent either way.
- **Discovery → formalisation.** Success gives the researcher knowledge (`DesignDiscovered`); the design becomes reproducible for others only when someone writes it down (`DesignFormalised`), teaches it, or is observed using it.
- **Improvements** (`Improve(D)`) produce an invented variant: a runtime `DesignId ≥ 0x8000` with `Parent = D` and bounded parameter deltas (efficiency, capacity, input reductions) drawn deterministically; allocated by `Designs.Formalise` only (Tier 3).
- **Who owns results** is the sponsor's charter and the researchers' contracts: an employer owns documents written at work (the `Work` right rule); knowledge stays in the researchers' heads either way.
- **Discovery is not construction.** A discovered generator design is not a generator.

### 10.3 Universities and research organisations
See section 13: they are organisations whose staff teach and research using these same acts; they add places, materials, stores of documents (libraries) and records.

### 10.4 Divine revelation (`RevealTechnology`, MiracleKind 11)
- **What it does:** the target person gains `KnownDesign(D, 128, Fidelity 1000, Source = Revealed(intervention))` if it understands D's prerequisites, else level 64 (an insight it cannot yet use), tuning `RevealRequiresPrerequisites` (default 1). The miracle emits D3's `MiracleEffectApplied` and K1's `KnowledgeAcquired` with the intervention id. Mythic cost: set in MYTHIC_AND_MIRACLES' table (proposed 20 + Complexity / 10), Tier 3 there; K1 supplies the handler body D3 calls (MYTHIC §6.11 "reserved until a technique store exists": this store is it).
- **What it does not do:** no document, no right, no asset, no materials, no skill. The person may then write it down, teach it, build it with real inputs, or apply for a patent like anyone (D6.10). It goes into no institution's records (D5.28).
- **Revealing to a group** (a `Location` scope) gives each recipient its own entry with its own fidelity draw (`RevealBlurPermille`, as COMMANDMENTS 4.4), so revealed knowledge can be misunderstood.

## 11. Auctions (D6.12): one mechanism for designs, rights and assets
```text
Auction                                    // CivState.K1.Auctions (Tier 3), ascending Id
  Id: AuctionId, Auctioneer: NpcId (and its OrgId, if it acts for an auction house or office)
  Seller: Owner
  Item: AuctionItem { Kind: Document(lot) | Right(RightId, assignment) | NewLicence(parent RightId, Exclusive, Scope, TermDays)
                      | Asset(asset id: a building, a power station, a vehicle) | Goods(lot) ; Bundle: AuctionItem[<= 3] }
  Format: EnglishOpen | SealedFirstPrice | DescendingDutch
  Unit: UnitRef (K2), Reserve: int, MinStep: int
  Place: node, Opens, Closes (ticks)
  Bids: Bid[<= 16] { Bidder: Owner, Agent: NpcId, Amount, Tick }   // as the auctioneer perceived them
  Status: Announced | Open | Closed | Settled | Voided; Winner, Price, Obligation (C2 AuctionPrice)
```
- **Bidding is an act at a place.** A bidder (or its agent) must be at the auction place within gesture range of the auctioneer and gesture `Offer` with a quantity (M5 comms); sealed bids are documents handed to the auctioneer. The auctioneer records only bids it perceived (it may misread). Bids are bounded by the bidder's own valuation (`Valuation` for goods; for rights and designs, the bidder's own expected-use score), never by a hidden price.
- **Closing** creates an `AuctionPrice` obligation of the winner to the seller; **settlement** is a K2 `AuctionSettlement` payment; then the item is transferred by its own rule: a document by hand-over (`Goods.Move`, basis `Purchase`), a right by assignment at the office (8.3), a new licence by the holder's grant, an asset by a title change of its property record (C1 `PropertyRecord`, the owner's act at the asset or the registry), goods by hand-over. Non-payment by the due day voids the sale (`AuctionVoided`); the seller keeps the item.
- **Winning is not building.** Winning a generator *design* gives a document and/or a right; winning an *existing* power station gives its ownership, not fuel, staff or a grid connection (D6.12).
- **One mechanism.** Estate sales, confiscated goods, surplus vehicles and Power Grid-style generator rounds all use this record. No industrial package adds a second auction type.

## 12. Four distinctions, as predicates

| Predicate | True when | Truth query (analytics, validation) | What a person believes |
|---|---|---|---|
| **Owns the design** | holds a `Granted` patent (or a `DesignTitle`) on D in jurisdiction J | `Rights.HolderOf(D, J)` | its record of the registry (layer 4) or hearsay |
| **May use the design** | no right in force in J reserves the use, or it is the holder, or holds a licence whose scope covers the use, or a compulsory licence | `Rights.Permits(party, D, use, J)` | `Permits.Believed` extended with rights beliefs (C1's permission query, one place) |
| **Knows the design** | some person (for an org: some member) has `KnownDesign(D).Level ≥` the act's minimum, or a document of sufficient fidelity is at hand | `Knowledge.LevelOf(npc, D)` | its own store: a person knows which designs it holds, but not how faulty its version is (`Fidelity` is truth) |
| **Has constructed it** | an asset with `DesignId = D` exists, built by real construction | the asset records | what it has seen |

Each can be true without the others: a patent holder who has forgotten the design and built nothing; a licensee who knows nothing yet; a skilled engineer with no permission who builds anyway (unauthorised use, section 9); a factory that exists but whose staff cannot operate it. Tests D6.5–D6.8 check these separations.

## 13. New organisation templates (D6.15, on the generic framework)
All are ordinary organisations (`Organisation` record, ORGANISATIONS §3.2) of any ownership model, founded by people who see the need (D5.16), staffed by NPCs with actual workers, places, goods, documents, budgets and records. The god influences them only through divine mechanics on individuals.

| Template | `OrgType` and purposes | Offices (new kinds marked *) | Places and stores | Services (INSTITUTIONAL_SERVICES owner D6 section) |
|---|---|---|---|---|
| **University** | `ServiceBody`; `Service(Teach)`, `Research` | Head, Teacher, Researcher*, Librarian* (KeepStore of documents), Treasurer, Recorder | lecture hall (places = learners per session), library (`OrgStore` of `Document` lots, access by policy), laboratory asset | Teach (DesignIds and skills), Library reading, contract research |
| **Research organisation** | `Enterprise` (private, cooperative) or `ServiceBody` (public); `Research` | Head, Researcher*, Treasurer, Recorder | laboratory or workshop asset, document store | contract research; it may apply for patents, sell documents, license |
| **Patent office** | `ServiceBody` funded by or part of a government (`OrgRelation FundedBy/RegulatedBy`); `RegisterRights` | Head, Examiner* (`Examine`), Clerk* (`KeepRecords`, `Register`), Librarian* | office node with the registry desk; deposit store (the specification documents it holds); open library if the law requires disclosure | RegisterRight (apply, examine, grant), assignment and licence recording, renewal |
| **Commercial court** | `ServiceBody` of a government or a merchants' association (arbitration); `Adjudicate(Commercial)` | Judge, Clerk*, Enforcer (Tier 3) | courtroom node; evidence store | CommercialDispute: infringement, licence breach, unpaid bills and contract disputes (Tier 3 process; Tier 2 accepts complaints) |

New office kinds (`Researcher`, `Examiner`, `Clerk`, `Librarian`), powers (`Examine`, `Register`), purposes (`Research`, `RegisterRights`, `Adjudicate`) and service kinds are requested from C6 (section 14).

## 14. Contract requests to the lead (for the SAC Industry addendum)

| # | Request | Neutral? |
|---|---|---|
| K1-R1 | `CivState.K1` sub-object (partial `Civ/CivState.K1.cs`: `DesignRegistry`, `Artefacts`, `Rights`, `Projects`, `Auctions`), hashed after C5 (and before K2) when non-null; switch `Civ.Knowledge.Enabled` (default 0); tuning `Civ.Knowledge` with the `Designs` table | yes |
| K1-R2 | Mind slot `NpcMind.Know` (`KnowledgeMind`), null unless the switch is on, hashed only when present (the `Kin`/`Social` pattern); `ArchivedLife.Know` (D7) | yes |
| K1-R3 | Ids `RightId`, `ResearchProjectId`, `AuctionId` (counters in `CivState.K1`); `DesignId` as content id; episode-key kinds 19 right, 20 auction, 21 research project | yes |
| K1-R4 | Wares `Document` (proposed 63) and `Paper` (proposed 64) from the spare 63–71 of the industrial ware block (INDUSTRIAL_RESOURCES §3.1); `Wares.IsUnique(w)` (Axe and Document) replacing `w != Ware.Axe` in `Goods.Mergeable` and the ground merge (`Assets/God/Sim/Population/Goods.cs`, a frozen file: a one-line, behaviour-neutral lead amendment) | yes (no Document lot exists until K1 writes one) |
| K1-R5 | `ConsumeReason.Research = 13` in the 16-slot ledger that the I1a foundation lands (INDUSTRIAL_RESOURCES §3.2; 13–15 reserved there); writing and copying materials use `ProcessInput` (8); documents are produced with `ProduceReason.Crafted` (5, F1) | yes (slots hashed only when non-zero) |
| K1-R6 | Learning verb rows for `Study`, `CopyDocument`, `WriteDesign`, `Research`, `ApplyForRight`, `Bid` from the free rows of 96–127 (proposed 114–119), and an `NpcVerb` block **210–219** for Knowledge and Money (200–209 is Industry, INDUSTRIAL_RESOURCES §9.2); K1 and K2 candidates use the Civ candidate quota (`CivMindHooks.K1`/`.K2` partials), no new `MaxCand` quota | yes |
| K1-R7 | C6: purposes `Research`, `RegisterRights`, `Adjudicate`; offices `Researcher`, `Examiner`, `Clerk`, `Librarian`; powers `Examine`, `Register`; templates of section 13 | yes |
| K1-R8 | `ServiceKind` values (proposed) `RegisterRight = 20`, `ContractResearch = 21`, `CommercialDispute = 22`, `LibraryRead = 23`; C7's `Teach` topic extended to `DesignId`; C9 complaint kind `Infringement` | yes |
| K1-R9 | C10 `Law.Kind = PatentLaw` and its rules (8.2) | yes |
| K1-R10 | Asset records of C1 and the industrial packages carry `DesignId`; the sense seam exposes it to inspection | yes |
| K1-R11 | Domains `Knowledge = 76`, `Rights = 77`, `Auction = 78` (and K2's `Money = 79`; Industry has 70–75), all with `RangeOf` → **1100–1199** (the block INDUSTRIAL_RESOURCES §12 suggests; Industry has 1000–1099); event codes (proposal) **1100–1169**: knowledge 1100 `DesignDiscovered`, 1101 `KnowledgeAcquired`, 1102 `KnowledgeForgotten`, 1103 `DesignFormalised`, 1104 `ArtefactCopied` (truth field `Authorised` †), 1105 `ArtefactClosed`, 1106 `DesignLost`, 1107 `SkillChanged` (if C7 does not own it), 1110 `ResearchProjectStarted`, 1111 `ResearchProgress` (HV), 1112 `ResearchProjectEnded`; rights 1130 `RightApplied`, 1131 `RightGranted`, 1132 `RightRefused`, 1133 `RightAssigned`, 1134 `LicenceGranted`, 1135 `LicenceEnded`, 1136 `RightRenewed`, 1137 `RightLapsed`, 1138 `RightExpired`, 1139 `RightChallenged`, 1140 `RightRevoked`, 1141 `RightSurrendered`, 1142 `UnauthorisedDesignUse` †, 1143 `InfringementPerceived`, 1144 `CompulsoryLicenceGranted`, 1145 `InjunctionOrdered`; auctions 1160 `AuctionAnnounced`, 1161 `BidMade` (HV), 1162 `AuctionClosed`, 1163 `AuctionSettled`, 1164 `AuctionVoided` | catalogue rows only |
| K1-R12 | Migrations `0039_knowledge_designs.sql`, `0040_rights_licences.sql`, `0043_auctions.sql` (DATA_WAREHOUSE §13.4; the I packages take 0031–0037) | recording only |

## 15. Tier and package mapping

| Capability | Tier | Package | Minimal first version |
|---|---|---|---|
| `DesignDef` schema and loader; founders' known set; `DesignId` on assets | 2 (slice) | **K1** (rows by I1–I5) | the slice's designs: harvester, refinery, coal generator, factory, transport vehicle |
| `KnowledgeMind`, levels, knowledge and skill requirements for building and operating | 2 (slice) | **K1** (skills shared with C7) | operator and builder checks in the slice |
| Teaching by C7 with design topics; observation gain; forgetting | 2 | K1 + C7 | teach one design; KN-I1 |
| Documents: write, copy, read, build from a document; `ArtefactRecord` | 2 | **K1** | test D6.5 |
| Rights: patent law seed, application, examination against registry, grant, assignment, non-exclusive and exclusive licences, fees through K2, expiry and lapse | 2 | **K1** (+ K2, C10 or scenario seed) | tests D6.6, D6.7 |
| `UnauthorisedDesignUse` classification, `InfringementPerceived`, complaint candidate | 2 | **K1** (+ C9 complaint, or a recorded complaint stub until C9) | test D6.8 |
| University, research organisation, patent office, commercial court templates | 2 (patent office, university teaching), 3 (research, courts) | K1 + C6 | the patent office for D6.6/D6.7 |
| Court process for infringement, challenges, revocation, remedies, compulsory licensing | 3 | C9 + K1 | later |
| Discovery by practice (roll), research projects, invented variants | 3 | K1 | later |
| `RevealTechnology` handler | 3 | D3 + K1 | later (MYTHIC §6.11) |
| Auctions | 3 | K1 + K2 | later |

## 16. Tests and acceptance hooks

**Invariants (EM/RT, every build):** KN-INV-1 (KN-I1: no act lowers another's knowledge; static writer list), AR-INV-1 (AR-I1), RT-INV-1..5 (RT-I1..I5), KN-INV-2 (no asset completes without a builder meeting the knowledge and skill rule or a document at the site), KN-INV-3 (no `Right` or `KnownDesign` writer in `Sim/Divinity` except the K1 handler called by D3).

**Unit tests (EM):**
- **KN-U1** teaching a design: learner level and fidelity rise by the formula; teacher's entry byte-equal before and after.
- **KN-U2** copying a document: a second `Document` lot exists with fidelity ≤ the source's; the source lot, its record and its owner are unchanged; materials consumed exactly; C1 holds.
- **KN-U3** reading a document raises the reader to at most the fidelity cap; the document is unchanged.
- **KN-U4** assignment of a patent: holder changes; `NextLotId`, every lot, every asset record and every `KnownDesign` byte-equal.
- **KN-U5** a licensee with no knowledge cannot build (refused `NoSkill`/knowledge) until taught or holding a document; then builds with real materials.
- **KN-U6** unauthorised construction emits exactly one `UnauthorisedDesignUse` and creates no obligation, no block and no change to the builder's state; without any perceiver, no `InfringementPerceived` follows.
- **KN-U7** no patent law in a jurisdiction: an application is refused at validation (`no_patent_law`); a contract licence is still possible.
- **KN-U8** (Tier 3) a revealed design gives knowledge with `Source = Revealed` and no right, document or asset.

**Acceptance hooks** ([ACCEPTANCE_TESTS.md D6](ACCEPTANCE_TESTS.md)): D6.5 (copy without removing), D6.6 (patent transfer without duplication), D6.7 (licence purchase and construction), D6.8 (unauthorised use → potential dispute), slice steps VS7–VS8 (factory workers' and operator's ability) and VS10.

## 17. Open questions (each with the default used)
1. **Literacy.** Should reading need a learned `Reading` skill everywhere? Default: yes, with the industrial scenario seeding adult literacy; prehistoric scenarios have no documents anyway.
2. **Can an organisation be a holder of knowledge?** Default: no; only its members and documents. An org's "capability" is a view over its members.
3. **Novelty standard.** Default `AgainstRegistry` (the office knows only its own registry and deposits); `AgainstKnownPriorArt` is a law option.
4. **Do royalties need the licensor to observe output?** Default: truth count by contract terms (MONEY §15 q4).
5. **Design table versioning across saves.** Default: content ids are stable; a save records the content version and refuses a mismatch (as other tuning).

# Political economy design (stage 4 foundations, stage 5 onwards for movements)

The shared names, layers, policy levels and milestones are in [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md); that file wins where this one disagrees.

## 1. Purpose and scope

This document owns how NPCs come to want things from the economy and the state, and what they can do together to get them:
- the **interest model** (computed and learned, never assigned);
- **political beliefs** as cultural traits;
- **grievances**;
- **collective action**, **collective bargaining** and **industrial disputes** (strikes, lockouts);
- **economic alliances**: guilds, unions, cartels, cooperatives, mutual-aid groups;
- **political movements** and **petitions**;
- **legitimacy** and **unrest**;
- how game theory and gestures scale from pairs to groups;
- **competing forms of economic organisation** and how they compete without favour;
- how the player's state can **concede, repress, co-opt or negotiate**.

It links to, and does not repeat:
- class measures and perceived position: [ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md);
- ownership forms, enterprises, jobs, commons: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- the gesture grammar and the decider: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- promises, reputation and gossip: [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- cultural traits and their spread: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- learning: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- loyalty, migration and state commands: [STATE_DESIGN.md](STATE_DESIGN.md);
- institutions, their staff and ages: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- the two-player games and the lab: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md), [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md);
- the philosophy: [VISION_AND_PHILOSOPHY.md](VISION_AND_PHILOSOPHY.md).

## 2. Principles
1. **Interests are computed and learned, not assigned.** No NPC is given a political side. Each NPC's stance on an issue is the sum of its material stake (from its own perceived position), its experience (learned associations), its relationships (people it trusts and cares about) and its culture (beliefs it has picked up). Each part is recorded separately so analysis can tell them apart.
2. **There is no collective actor in the decider.** A strike, a movement or a union exists only as the sum of individual choices and the records individuals keep. Every participant decides for itself, every think tick, through the ordinary decider.
3. **Disputes are detected, not started.** The sim never runs a "strike event". Analysis detects a dispute episode from individual acts (section 4.7).
4. **Saying is not doing, at scale too.** A pledge to strike, a demand, a threat and a promise of concession are messages. They bind no one in the rules.
5. **No form of organisation is favoured.** Independent, communal, cooperative, private and state production run on the same physics. They differ only in rules for deciding, joining and dividing. See section 4.10.
6. **No political belief comes from class.** Class is a measured relation ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)); no NPC carries a permanent class label, and no political belief is assigned from class. Whether stances track positions is a finding.
7. **Communities organise themselves (owner decision Q17).** Groups can create their own institutions without the state, from households, contributions, shared ownership and associations ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#communal-founding-without-the-state)). The state may support, regulate, compete with or replace them.
8. **Governance is not assumed (owner decision Q20).** Informal household and community decisions come first; formal rules for associations come later; state-level democracy develops as an institution and is not assumed from the start.
6. **The state's responses have direct costs only.** Conceding costs what was conceded; repression costs staff time, wages and risk. The *consequences* (resentment, emboldenment, fear, emigration) arise through NPC learning, memory and beliefs, never through a scripted penalty.
7. **Collective action must be able to fail.** Free riding, mistrust, misunderstanding, lack of reserves and replacement workers are all real. A scenario in which collective action cannot fail, or cannot succeed, is a bug unless the circumstances explain it.

## 3. Data structures
All values are integers; stances and strengths run −1000 to 1000 unless stated. Sizes are bounded.

### 3.1 Issues
```text
enum IssueKind : byte {
  TaxRate, StateWage, WelfareRation, CommonsAccess, EnclosureAllowed, PropertySecurity,
  InheritanceRule, WageTerms /*with one employer*/, WorkHours, PriceOfWare,
  WarAndService, Immigration, InstitutionFunding, Other }
struct Issue { IssueKind Kind; int Subject; }  // Subject: ware, enterprise id, institution id, faction id, 0
```
Issues map to **real levers** the sim already has: a tax, a ration, an employer's contract terms, a law. An issue that has no lever is not an issue.

### 3.2 Interests (`Mind.Politics`, M10)
```text
InterestEntry                        // one issue, for one NPC; up to 16 per NPC
  Issue
  Material   : short   // first-order own stake (4.1)
  Learned    : short   // from the Learning store, key (Issue, Direction)
  Social     : short   // weighted stances of trusted and cared-for others it has seen or been told
  Cultural   : short   // from political-belief traits (3.3)
  Stance     : short   // combined (4.1)
  Salience   : short   // 0..1000; how much it matters now
  UpdatedDay : int
```

### 3.3 Political beliefs (cultural traits, M11)
Political beliefs are ordinary cultural traits ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)) with a bias table onto issues. Examples, all neutral in the sim:

| Trait | Biases |
|---|---|
| `FruitsToTheMaker` | the person who works a thing should keep what it yields |
| `OwnersEarnTheirShare` | those who provide tools and land deserve the surplus |
| `CommonsForAll` | wild land and water belong to everyone |
| `ElderAuthority` | decisions belong to elders and officials |
| `StateFeedsTheHungry` | the faction should prevent hunger |
| `EachProvidesForOwn` | households should not rely on others |
| `StrangersAreRisk` | outsiders should be kept out |
| `SolidarityWithFellows` | support those in the same position as me |

```text
// The NPC holds an ordinary TraitEntry { Id, Strength 0..255, Flags, Param } (CULTURAL_TRANSMISSION_DESIGN.md 3.2).
// This document owns the content definitions of the political Belief traits:
PoliticalTraitDef { ushort TraitId; ushort Locus; IssueBias[<=6] Biases; }
IssueBias { IssueKind Kind; sbyte DirectionWeight; }   // -100..100
```
- Rival positions share a **locus**, so an NPC's active variant is its strongest one there: for example `FruitsToTheMaker` and `OwnersEarnTheirShare` at the locus "who should keep the yield"; `CommonsForAll` and `EachProvidesForOwn` at "who provides".
- Only held traits (`Strength ≥ 128`) count fully; 64–127 count at half weight; below 64 not at all, matching the culture module's thresholds.
- Traits are copied, taught and forgotten like any meme. They can agree or disagree with material interest; the gap is measured (section 7).

### 3.4 Grievances (`Social`, M10)
```text
Grievance                            // up to 12 per NPC; weakest forgotten first
  TargetKind : Npc | Enterprise | Institution | Faction | Association
  TargetId
  Issue
  Severity   : short   // 0..1000, from the loss relative to the reference point (4.2)
  Since, LastReinforced : int (day)
  Evidence   : Seen | Told(NpcId) | Published
  Shared     : byte    // count of others who told it the same grievance
```

### 3.5 Legitimacy (`Mind.Politics`, M10)
```text
LegitimacyBelief                     // one per faction the NPC knows; <= 4
  FactionId
  Procedural : short   // were rules applied as I expected: contracts paid, cases decided, promises kept
  Outcome    : short   // did life go as I expected under this state
  Voice      : short   // does the state answer people like me (petitions answered, concessions seen)
  Repression : short   // force seen used against people like me (negative)
  Total      : short   // weighted sum; weights from traits such as ElderAuthority
```
Loyalty (owned by [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)) takes `Total` as one input. Legitimacy is "is this rule rightful", loyalty is "do I stay and serve". They can diverge.

### 3.6 Associations (stage 5)
```text
Association
  Id (AssociationId, new id kind), FoundedDay, Founder NpcId, Faction
  Purposes : flags { BargainWages, SetPrices, RestrictEntry, MutualAid, PoolFund,
                     PetitionState, OwnAssets, ResolveDisputes }
  Members  : (NpcId, JoinedDay, DuesOwed)[<=64]
  Leaders  : NpcId[<=3]        // whoever members endorse most (4.5); recomputed weekly
  Rules    : Dues (goods per day), EntryRule (open | invited | trade-only),
             Sanction (shun | expel | fine), DecisionRule (generic, Q20: Majority | Consensus | Leader |
             Appointment | Rotation | Election | Lot | Seniority; data with parameters, more kinds later)
  Fund     : ItemLotId[<=8]    // pooled goods: lots owned by the association's Group (Owner{Group, id})
  Record   : (Day, Act, Outcome)[<=32]   // its own memory of past actions, readable by members
```
- An association is created by an NPC intent `FoundAssociation(purposes, rules)` and joined by `Join(a)`. It has no will of its own.
- **Its "kind" is derived for analysis only** (section 4.9): a union, a guild, a cartel or a mutual-aid society is a label computed from purposes and the members' class measures.
- A cooperative *enterprise* is an ownership form in [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md). An association with `OwnAssets` can found one.

### 3.7 Petitions and demands (stage 5)
```text
Petition
  Id, Day, Faction, Issue, Direction, Presenter NpcId, AssociationId?
  SignerCountClaimed : int     // as presented
  SignerCountSeen    : int     // as counted by the receiving official (layer 4; can differ)
  ReceivedBy InstitutionId, Status: Pending | Answered(Concede|Refuse|Partial|Ignored)
```
A `Demand` to an employer is a gesture message, not a record; the employer remembers it like any message.

### 3.8 New gesture acts
Listed as "later acts" beside the vocabulary table in [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#vocabulary-fixed-and-shared-at-first), same grammar; the message `Topic` there can name an issue or a posting.

| Act | Meaning | Gestures | Stage |
|---|---|---|---|
| `Complain` | I am harmed by this (Topic: issue subject) | Point at the thing, then both palms down, shake head | M10 |
| `Approve` | This is good for me | Point at the thing, nod, hand on chest | M10 |
| `Rally` | Gather at this place, at this time | Raise arm high, sweep to the group, point at the place, then at the sun's position | 5 |
| `Pledge` | I will join if Quantity others do (conditional promise) | Hand on chest, show fingers, then sweep arm across the group | 5 |
| `Endorse` | I follow this person | Point at them, then fist to own chest | 5 |
| `Demand` | Give us X, or else Y (Counter is the threat) | Group stands shoulder to shoulder; leader points at target, then at X, then shows Y | 5 |
| `Withhold` | We will not work | Sit or stand with arms folded at the workplace; tools on the ground | 5 |
| `Shun` | You let us down | Turn back on the target, refuse eye contact | 5 |
| `Concede` | I accept your demand | Open palms, bow head slightly, present X if held | 5 |
| `Disperse` | We are going home | Arms down, turn away together | 5 |

`Petition` is not a gesture: it is `Demand` addressed to a state official at a notice board or HQ, which the official writes into a record.

## 4. Mechanics and algorithms

### 4.1 The interest model
Each adult NPC updates its interests at its sleep step, in ascending `NpcId`, using its own random stream `Rng.Fork("npc", id)` only for tie-breaks.
1. **Material.** For each issue with a known lever, estimate the first-order change in own drive relief over the planning horizon from a one-step change in the lever, using the NPC's own `PerceivedStanding` ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md#49-how-npcs-perceive-position)):
   - `TaxRate` on ware W: −(own taxed volume of W × step);
   - `StateWage`: + own state-wage share × step; − (if it employs others) wage pressure × own hired labour;
   - `CommonsAccess`: + own communal and wild food share × step;
   - `WageTerms` at enterprise e: + if it works there for wages; − if it owns a share of e;
   - and so on: one first-order formula per issue, written next to the lever it reads (open question 1).
   - Scaled up by precarity: `Material × (1000 + (1000 − FoodSecurity)) / 1000`. People near hunger feel stakes more.
   - The NPC estimates only first-order effects. It does not model the whole economy. Second-order effects reach it through learning.
2. **Learned.** When an NPC perceives a `PolicyChanged` posting or an employer's change of terms, it marks an eligibility trace on the association key `(Issue, Direction)`. Later drive changes reward or punish that key through the ordinary learning core ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)). `Learned` is the key's current weight. An NPC that went hungry after the last tax rise can learn to oppose tax rises, whatever its formula said.
3. **Social.** Weighted mean of the stances of others it has heard `Complain` or `Approve` on the issue, weighted by trust × (care, from kin weight and friendship) × similarity of perceived position (`PerceivedOther.RelationToMe`), divided by the total weight.
4. **Cultural.** Sum over its active political variants of `effectiveStrength × DirectionWeight × 1000 / (255 × 100)` on the issue's kind, clamped to ±1000.
5. **Combine.** `Stance = clamp((wM·Material + wL·Learned + wS·Social + wC·Cultural) / 1000, ±1000)`. The weights come from personality: conformity raises `wS` and `wC`; patience lowers the precarity scaling. Defaults in `SimTuning.Politics`, all four equal.
6. **Salience** rises with recent change on the issue, recent grievances about it, and how often it was discussed; it decays daily (`s -= s >> 3`).

### 4.2 Grievances
- A grievance is created when the NPC experiences, sees or is told about a loss relative to a **reference point**:
  - its own recent past (wage cut, ration cut, eviction);
  - peers in the same perceived position (others at the same workplace paid more);
  - a cultural norm (a `FruitsToTheMaker` NPC sees the owner take most of what it made);
  - a broken promise from an employer, official or the state ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#obligations-and-promises)).
- `Severity = 1000 × (loss relative to the reference) / (own daily food need)`, both valued in the NPC's own believed food-equivalent of the wares involved (from its own consumption experience; NPCs never read `WareRefValue`, which is a measurement index owned by [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)), raised by up to half again by the strength of any cultural trait whose norm was broken, clamped to 0..1000.
- Grievances are reinforced by repetition and by others telling the same story (`Shared` rises), and decay otherwise. They are told in gossip like any fact, so they can be exaggerated or false.
- A grievance against a target lowers trust in that target and raises the salience of the issue.

### 4.3 From pairs to groups: the games
The two-player games in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md) already exist in the island. Collective action is the same machinery with more players:

| Pair game (stage 4) | Group version | What decides it |
|---|---|---|
| Prisoner's Dilemma (help, deferred exchange) | **N-person public goods game**: each participant pays a cost, the gain goes to everyone affected | Marginal effect of own participation is small, so material terms alone favour free riding |
| Assurance (hunting together) | **Threshold public goods**: the gain arrives only if enough join | Beliefs about how many will join; pledges and rallies change them |
| War of attrition (PDF 99–100) | **Strike vs lockout**: both sides pay time costs until one gives up | Reserves (food, strike fund, stock) and patience; unreadable resolve |
| Resident/intruder (PDF 100–103) | **Picket line**: strikers "own" the gate by convention | Which convention the community holds; costly fights |
| Grudger memory (PDF 206–209) | **Shunning strike-breakers and free riders** | Social memory and gossip make defection costly in future dealings |
| Clustering (PDF 239–243) | **Solidarity in dense networks** | Kin, household and workplace ties let a cluster cross the threshold |

The lab adds maths-only N-person benchmarks so the island can be compared with known results (section 10).

### 4.4 Deciding to join
`JoinAction(target, issue)` is an ordinary candidate intent, as are `Pledge`, `Rally`, `Withhold`, `Demand` and `Disperse`. The default `AssociativeNpcPolicy` scores joining on each think tick:

```text
score(Join) =   Δp(n̂) × Gain × Stake            // material: own effect on the chance of success
              + Excludable × Gain × p(n̂)         // selective benefit for participants only
              + Solidarity                       // Σ care × trust over friends/kin seen joining
              + ReputationGain − ShameAvoided    // expected social memory of joining or not
              + Learned(JoinAction, situation)   // from past disputes it joined or watched
              − LostIncome × days_expected       // wages forgone
              − Fear × p(repression) × Harm      // from beliefs about guards and past repression
              − HungerRisk                       // own reserves vs expected length
```
- `n̂` is the NPC's **belief** about how many will take part: from pledges it has seen or been told, the number at the last rally it saw, and the association's recorded past turnout. Never the true number.
- `p(n̂)` is its belief about the chance of success given `n̂`, learned from past disputes it saw or heard about. Starting prior: low.
- `Δp(n̂) = p(n̂ + 1) − p(n̂)`. Near the believed threshold it is large (the NPC feels pivotal); far from it, small. This is what makes free riding possible.
- `Excludable` is 1000 when the gain goes only to participants (a guild's protected market, a mutual-aid fund, a closed shop) and 0 when everyone gets it. Selective incentives change the game, as Olson argued; nothing forces them to exist.
- Personality enters only as biases on these terms (boldness on fear, generosity and kin weight on solidarity, patience on the horizon).

### 4.5 Assurance, signals and leaders
- **Conditional pledges** solve assurance problems: "I will if 5 others do". A pledge is a promise in the listener's memory; breaking it after the threshold is met is a betrayal, with the usual reputation cost.
- **Rallies** make numbers visible. Everyone at a rally perceives the crowd directly (layer 2), which updates `n̂` far better than rumours.
- **Leaders** are NPCs who are endorsed (`Endorse`) by many others. Endorsement is scored like any act: trust in the person, their record of kept promises, their visible success, shared position. A leader is a focal point: its `Rally` and `Demand` carry weight because others believe others will follow it. Leaders get no rule-based powers in an informal group; in an association they get what its `DecisionRule` gives.
- **Leaders have their own interests.** A leader can be offered a post or goods (co-optation, 4.11), can lie about pledge counts, and can lose endorsement when caught.

### 4.6 Credible threats
A target (employer, official, the state's institution policy) receiving `Demand(X, else Y)` estimates:

```text
P(carry out Y) = f( pledges seen and crowd size,
                    the group's or leader's record of following through,
                    the group's reserves vs the expected length of the dispute,
                    the cost to the group of carrying out Y )
```
- The target compares `P × cost of Y` with the cost of conceding X. Bluffs work until they are called. Calling a bluff that was real is costly.
- Following a war-of-attrition logic, a readable sign of when one side will give up gets exploited, so neither side has a reason to reveal its reserves honestly. Lying about reserves is possible.
- Institutions change credibility. A court that enforces collective contracts (Town age) makes a *concession* credible; a strike fund makes a *threat* credible.

### 4.7 Collective bargaining and industrial disputes
- **Bargaining.** An association (or an informal group with a leader) sends `Demand` to an employer. The employer's decider scores `Concede`, `Refuse`, counter-`Offer` or `Ignore`. A concession becomes new contract terms ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)). Until courts exist, the employer can quietly break them later, and the workers remember.
- **Strike.** Workers choose `Withhold`: they do not go to their jobs, and may stand at the workplace. **A walk-out suspends the job contract first; it does not end it (owner decision Q19).** The contract mechanics are in [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#jobs-and-the-end-of-walkers): while suspended there is no work and no pay. Each employee may then resume, negotiate, resign or keep refusing; the employer may negotiate, improve pay, dismiss or seek alternatives. Dismissal is the employer's choice, so losing the job is a possible cost, never an automatic one.
- **Lockout.** An owner chooses not to open the workplace or pay, to press its own terms: the same suspension from the employer's side.
- **From material circumstances, not scripts.** Collective bargaining, strikes, lockouts and economic conflict arise from these individual choices under material circumstances and incentives (reserves, dependence, replacement labour, ties); no event is scripted (principle 3).
- **Strike-breaking.** The owner can offer the jobs to others. Whether anyone takes them depends on their own decider: how dependent they are, how they view the strikers, whether they fear being shunned.
- **Picketing.** Strikers can `Defend` the gate. Crossing it is a contest under the resident/intruder logic, and violence becomes a crime with witnesses.
- **Resolution** comes from individuals: strikers drift back as hunger rises, the owner concedes as stock spoils and state purchase orders go unmet, or a mediator (elders in the Band age, a court later) proposes terms.
- **Detection (analysis only).** `Telemetry` opens a `DisputeEpisode` when at least `DisputeMinParticipants` (default 3) NPCs with contracts at the same employer `Withhold` on the same day after a `Demand`, and closes it when fewer than that remain for 2 days. It records start, end, peak participants, the demand, concessions and outcome.

### 4.8 Movements, petitions and institutional development
- **Movements** are associations with `PetitionState`, or informal groups whose members repeatedly `Complain` and `Rally` about the same state issue.
- **Petitions** are `Demand`s presented to an official. The official (an NPC with its own interests) writes `SignerCountSeen` into the institution's records, possibly wrong. The player sees the record through `FactionKnowledgeView`, not the truth.
- **Institutional development** can arise when an association's practice works and persists: a mutual-aid fund that acts like welfare, a guild that settles members' disputes like a court. The player may recognise, charter, absorb, tax or ban such bodies; the catalogue and unlock rules are in [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#ages-and-unlocks). An association's existence and practice can count towards an unlock condition, the same as any measured feature of society.

### 4.8a Decisions and governance (owner decision Q20)
- **Stage 4: informal decisions only.** Households decide norms by the agreement of their adults ([HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md#forming-and-breaking-households)); cooperatives decide at a meeting of present members ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#47-enterprises-m8)); communities and communal institutions decide by the agreement of those present, with the "most trusted" member as manager ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#11-open-questions)). Each participant decides for itself by gesture.
- **Stage 5: a generic `DecisionRule` for associations,** shared by associations, cooperatives and communal institutions, supporting alternative governance: majority, consensus, appointment, rotation, election, lot, seniority and others later. It is data, so a group can change its own rule by its current rule. Which rules persist is a finding.
- **State-level democracy develops as an institution,** not assumed from the start: an assembly, elections or a council is an institution type with unlock rules, staff and failure modes ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)), in a later age. Its design links to the owner's wider political plans and is decided with the owner when that stage is planned.

### 4.9 Economic alliances (derived kinds)
Kinds are computed each day from an association's purposes and its members' class measures, for analysis only:

| Derived kind | Rule (analysis) |
|---|---|
| Union | `BargainWages`, and ≥ 600 permille of members are `WageWorker` |
| Guild | `RestrictEntry` or `SetPrices`, and ≥ 600 permille `IndependentProducer` |
| Cartel | `SetPrices` or wage-setting, and ≥ 600 permille `EmployerOwner` |
| Mutual-aid society | `MutualAid` and `PoolFund`, without bargaining purposes |
| Cooperative founders | `OwnAssets`, and the association owns a cooperative enterprise |
| Movement | `PetitionState` |
| Mixed | anything else |

### 4.10 Competing forms of economic organisation
All five forms use the same items, recipes, tools, skills, wear and work speed, as [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#47-enterprises-m8) requires. A static test checks that production code never branches on `EnterpriseForm` or `OwnerKind` (section 10).

| Form | Represented as | Who decides production | Who may work | How net product is divided |
|---|---|---|---|---|
| Independent production | an asset with `Owner{Npc}` or `Owner{Household}`, worked by the owner and household, no postings | The owner | Owner and household | All to the owner's household |
| Communal ownership | `EnterpriseForm.Communal` or an asset with `Owner{Group}`, use rights per member | Members, by custom or `DecisionRule` | Rights holders | By rights, custom or need |
| Cooperative | `EnterpriseForm.Cooperative`, worker-members hold the shares | Members by their rule | Members, and hired non-members if their rule allows | By members' rule: equal, by labour, by shares |
| Private enterprise | `EnterpriseForm.Private`, with job postings for hired workers | The owner | Hired workers on terms | Wages, then the rest to the owner |
| State-owned production | `EnterpriseForm.State` (`Owner{State}`), run by an institution | Institution staff under `IInstitutionPolicy` and the player's directives | State employees | Wages, then to the treasury |

**How they compete:**
- **For labour**: every NPC chooses where to work through its decider (pay, share, conditions, relationships, beliefs, culture). No form can conscript.
- **For inputs and customers**: barter, state purchase offers and contracts are open to all forms unless the player's policy says otherwise.
- **Through internal incentives**: cooperatives can suffer free riding and slow decisions; private firms can suffer monitoring costs and strikes; state firms can suffer principal–agent problems and corruption; communal land can suffer overuse or thrive under good rules. All of these come from NPC decisions under the form's rules, not from modifiers.
- **Through the player**: the state may favour any form by policy. That is a player choice, recorded as a treatment.
- **Measured** per form: output per work hour, survival rate of enterprises, member welfare (food security, wealth), inequality within the form, investment, dispute rate. Reported side by side, never ranked by the HUD.

### 4.11 The state's responses
The player (or an AI faction policy) responds through ordinary state and institution commands. Only direct costs are mechanical.

| Response | How (commands; owned by [STATE_DESIGN.md](STATE_DESIGN.md)) | Direct cost | What NPCs may learn or believe (not scripted) |
|---|---|---|---|
| **Concede** | Change the policy, contract terms or ration; `AnswerPetition(Concede)` | The goods or revenue conceded | That demanding works (`Learned` on `JoinAction` rises); `Voice` legitimacy rises among those helped; opponents may form grievances |
| **Repress** | Post a guard or military assignment to disperse a gathering or protect strike-breakers, with pay and bounty ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) | Wages and bounties; risk to staff | Fear of joining rises; `Repression` legitimacy falls; grievances against the state; migration; guards who are kin of protesters may refuse or desert |
| **Co-opt** | Offer leaders a state post or goods (a contract) | The post or goods | If seen, trust in the leader falls; if unseen, the movement may weaken; corruption risk in the post |
| **Negotiate** | Open talks with an association: exchange offers through the contract system, possibly through a mediator | Staff time; any concession agreed | Credibility of the state depends on whether it later keeps the terms (`Procedural` legitimacy) |
| **Ignore** | No action; `AnswerPetition(Ignored)` | None | `Voice` falls; salience may rise or fade |

Proposed new commands for [STATE_DESIGN.md](STATE_DESIGN.md): `AnswerPetition(petition, answer)`, `OpenTalks(association)`, `PostDispersal(node, until, pay)`, `OfferPost(npc, institution, terms)`. Guards and soldiers given a dispersal assignment still decide individually whether and how to carry it out.

### 4.12 Unrest
Unrest is **not a state variable** and triggers nothing. It is a faction-level measure of behaviour over the last 5 days:
- non-compliance: tax evasion, contract walk-outs, ignored laws;
- `Complain` gestures per adult;
- rally attendance; dispute-episode participants;
- crimes against state property and officials;
- emigration of citizens.

Each component is reported separately, plus an equal-weight index for the HUD. The weights are a reporting convention and never feed back into the sim.

## 5. Interactions with other systems
- **Classes**: perceived position feeds `Material` and solidarity; true measures are used only to analyse stance vs position ([ECONOMIC_CLASSES_DESIGN.md](ECONOMIC_CLASSES_DESIGN.md)).
- **Culture**: political traits spread, can be taught by parents, and can be published by media (City age) ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md), [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).
- **Social memory**: grievances, pledges, endorsements and shunning are memory records; gossip spreads them ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)).
- **State**: legitimacy feeds loyalty; petitions and responses are state commands ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **Institutions**: mediators settle disputes; courts enforce collective contracts later; police and prisons change the cost of repression and of crime; institution staff can themselves organise ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).
- **Military**: soldiers are NPCs with families and interests, so their reliability in repression is emergent ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Experiments**: free-rider payoffs, reserves, replacement labour and network density are the treatment variables ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).

## 6. Player and view
- **Politics report** (M13 foundations; full in stage 5): stance distribution per issue; the share of each stance explained by material, learned, social and cultural parts; top grievances by target; legitimacy components; unrest components; petitions received (as recorded, with the official who recorded them).
- **Petition card** (stage 5): the issue, claimed and seen signers, presenter, and the response buttons (concede, refuse, partial, open talks, ignore).
- **Dispute card** (stage 5): the workplace, participants seen, the demand, days so far, each side's visible reserves.
- **NPC inspector**: the NPC's own top stances with their four parts, and its grievances.
- **Animations**: the gestures in 3.8, gatherings at rally points, folded-arm posture at workplaces. They show events only.

## 7. Telemetry
- **New domain events**: `InterestUpdated` (daily per NPC, only when a stance changes by ≥ 50), `GrievanceFormed`, `GrievanceShared`, `PledgeMade`, `RallyCalled`, `EndorsementGiven`, `DemandMade`, `WithholdStarted`, `WithholdEnded`, `Conceded`, `PetitionReceived`, `PetitionAnswered`, `AssociationFounded`, `AssociationJoined`, `AssociationLeft`, `DispersalPosted`. All also feed animations where they have a gesture.
- **Person snapshot fields**: top 4 interests (issue, stance, the four parts, salience), grievance count and total severity, legitimacy components per known faction.
- **Faction snapshot fields**: unrest components, petitions open, associations by derived kind, dispute episodes open.
- **Analysis-only episodes**: `DisputeEpisode` and association kinds (4.7, 4.9), computed in `Telemetry`.
- **Key analyses** ([KPI_CATALOGUE.md](KPI_CATALOGUE.md)):
  - *Materialism share*: across adults, the share of variance in `Stance` on each issue explained by class measures alone, vs by social ties and culture (descriptive).
  - *Interest–belief gap*: correlation between `Material` and `Cultural` parts per issue. The share of adults whose stance sign matches their material sign is KPI TRU-08 (class interest alignment).
  - *Grievances*: each `GrievanceFormed` becomes a row of `fact_grievance`, which KPI CRT-05 (access to justice) uses.
  - *Collective action rate*: dispute episodes per 100 wage workers per year; mean participation; success rate.

## 8. Stage 4 minimum vs later stages

| When | Delivers |
|---|---|
| M7 | Promises and reputation, the basis for pledges and credibility |
| **M10** | **Political-interest measures (foundations only)**: `InterestEntry` with Material, Learned and Social parts on the stage 4 issues (tax, state wage, welfare ration, commons access, immigration); grievances; legitimacy components feeding loyalty; `Complain` and `Approve` gestures. No groups, no collective acts |
| M11 | Political beliefs as cultural traits (the `Cultural` part) |
| M6, M8 | Contract suspension for lasting jobs (an individual `RefuseWork` suspends, Q19), the basis for later collective walk-outs ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md)) |
| M12 | Band-age elders and mediation can receive complaints; communal mediation of disputes between two parties; simple communal institutions founded by groups without the state where practical (Q17) |
| M13a, M13b | Snapshot fields recorded (M13a); politics report (stances, parts, grievances, legitimacy, unrest components) (M13b) |
| **Stage 5** | Associations with the generic `DecisionRule` (Q20), `Rally`, `Pledge`, `Endorse`, `Demand`, `Withhold` (a collective walk-out, suspending contracts), `Shun`, `Concede`, `Disperse`; strikes, lockouts and dispute detection; petitions; the state's responses; derived alliance kinds; cooperatives at scale; the headman's court as mediator; `FoundCommunal` completed if M12 deferred it |
| Later | State-level democracy as an institution (Q20) |
| Stage 6 | Courts enforcing collective contracts; police and prisons; chartered companies; money in strike funds and dues; institution staff organising |
| Stage 7 | Media spreading political traits and grievances at a distance; statistics on unrest that can be gamed; mass movements across settlements |

## 9. Migration from the existing code
No political systems exist in stages 1–3. `AiBrain`'s diplomacy is between factions and stays in the faction policy ([STATE_DESIGN.md](STATE_DESIGN.md)).

## 10. Tests and acceptance criteria
All run under `dotnet test DotNet` or the headless runner, and become entries in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

**Foundations (M10–M13)**
1. `Interest_MaterialFromOwnPosition`: two NPCs identical except that one is mostly state-paid and the other mostly independent. After a perceived `StateWage` posting, the first has `Material` > 0 and the second ≈ 0 (|x| ≤ 20).
2. `Interest_LearnedFromExperience`: an NPC whose hunger rose for 3 days after each of two perceived tax rises has `Learned` on `(TaxRate, Up)` below −100, while a control NPC who saw the same postings with no drive change stays within ±20.
3. `Interest_NotAssigned`: no constructor, scenario loader or setup command can set `InterestEntry` or `PoliticalTrait` values directly (static test; scenarios can only seed cultural traits through the culture module's documented seeding).
4. `Grievance_FromBrokenWagePromise`: an employer who fails to pay an agreed wage creates a grievance in the worker with `TargetId` = employer and `Severity` > 0; the worker's trust in the employer falls.
5. `Legitimacy_Components`: a state that pays every contract for 5 days raises `Procedural` in its contractors; one that defaults on half lowers it. Hash unchanged with politics telemetry off.
6. `Politics_DoesNotReadTruth`: an NPC that never perceived a policy change has no `Learned` trace for it (reflection and behaviour test).

**Neutrality**
7. `Forms_SameProduction` (shared with the economy's equal-rules test): the same recipe, inputs, tools, skill and labour ticks yield identical output for `Private`, `Communal`, `Cooperative` and `State` enterprises and for an independent `Npc` owner, to the unit, across 1000 random fixtures.
8. `Forms_NoKindInProduction`: static test; no method in the production, job, exchange or theft code paths branches on `EnterpriseForm` or `OwnerKind`.
9. `Forms_LabelSwap`: two enterprises with identical decision and distribution rules but different `EnterpriseForm` labels, swapped between runs, give identical `StateHash` after relabelling the ids in the comparison (the label alone has no effect).

**Lab benchmarks (stage 5, maths only)**
10. `Lab_PublicGoods`: N = 10, linear public goods with marginal per-capita return 0.3 and self-interested benchmark agents: contribution converges to 0 within 50 rounds. With return 1.2 (> 1): full contribution.
11. `Lab_Threshold`: threshold public goods with threshold 6 of 10 and a conditional-pledge benchmark policy: success rate matches the analytic value within 3 percentage points over 10 000 games.

**Island scenarios (stage 5, over 200 seeds × 4 sim years, nothing forced)**
Both scenarios have one private enterprise employing 12 NPCs at a wage that falls by 20% at year 1. Only circumstances differ.

| Variable | `Scenario_CollectiveActionCanArise` | `Scenario_CollectiveActionFails` |
|---|---|---|
| Benefit of a concession | goes only to the 12 workers (excludable) | spread over 60 households (non-excludable), so each worker's share is small |
| Reserves | a communal food store covers 6 days per worker | none; workers' households have 1 day of food |
| Replacement labour | none: closed borders, no unemployed | 30 unemployed, dependent NPCs nearby |
| Social ties | workers drawn from 3 households, long acquainted | workers are recent immigrants, strangers to each other |
| Dialect | shared | three dialects (raised misunderstanding) |

12. **Measure**: share of seeds with at least one `DisputeEpisode` at the enterprise with peak participation ≥ 6, and the share of those that end with a concession.
13. **Pass conditions** (set before any run, so nothing is tuned to them):
    - the `CanArise` rate is strictly between 5% and 95%, and so is its concession share (it is possible, not inevitable);
    - the `Fails` rate is below the `CanArise` rate, with a one-sided test at p < 0.01 over 200 seeds;
    - the `Fails` rate is above 0% *or* the report shows at least 10 seeds in which a `Pledge` or `Rally` occurred but participation stayed below threshold (failure happens through individual choices, not because the acts are impossible).
14. **Ablations** (descriptive, reported, no pass threshold): flip each variable in the table one at a time and report how the rate moves.
15. **Repression consequence check** (stage 5): in `CanArise`, randomise whether the state posts dispersal. Report emigration and later dispute rates in each arm with 95% intervals. Pass if the experiment completes with its control; the sign is a finding.

**Owner decisions (Q17, Q19, Q20)**
17. `Walkout_Suspends` (M6, shared with ECONOMY E5): a `Withhold` or `RefuseWork` leaves the contract `Suspended`, not ended; only `Resign` or `Dismiss` ends it.
18. `DecisionRule_Generic` (stage 5): one association fixture decided under each of majority, consensus, appointment, rotation and election gives the outcome that rule defines, through the same code path; a group can change its own rule only by its current rule.
19. `NoClassBelief`: no constructor, scenario loader or rule sets a political trait or interest from a class measure (static test, beside test 3).

**Performance**
16. Interest updates for 600 NPCs at sleep take under 3 ms per sim day; joining scores add under 5% to think-tick time.

## 11. Open questions
No open owner question remains. Owner decisions of 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):
- **Q19 Walk-outs:** suspend the contract first. The employee may resume, negotiate, resign or keep refusing; the employer may negotiate, improve pay, dismiss or seek alternatives. This is the basis for collective bargaining, strikes, lockouts and economic conflict, arising from material circumstances and incentives, not scripted events (4.7; test 17).
- **Q20 Formal voting:** the staged approach. Informal household and community decisions in Stage 4; a generic `DecisionRule` for associations in Stage 5, supporting alternative governance later (majority, consensus, appointment, rotation, election and others). State-level democracy develops as an institution, is not assumed from the start, and links to the owner's wider political plans (4.8a; test 18).
- **Q17 Communal institutions without the state:** yes; communities organise themselves independently of the state ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md#communal-founding-without-the-state); principle 7).

Settled here:
- **Issue formulas.** This document owns the table of first-order material formulas, one line per lever, added when the lever is added.
- **Legitimacy vs loyalty.** Legitimacy is one input to loyalty, weight 2 of 18 by default ([STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection)).
- **Topic field.** The message `Topic` can name an issue or a posting ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md)).
- **Scale.** 64 members per association and 16 interests per NPC are starting bounds; revisit with M10 performance data. Political memory per NPC is about 0.8 KB (16 interests × 24 B, 12 grievances × 28 B, 4 legitimacy beliefs × 14 B).

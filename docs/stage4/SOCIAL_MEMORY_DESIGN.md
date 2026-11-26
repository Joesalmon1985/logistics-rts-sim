# Social memory design (stage 4)

## 1. Purpose and scope

**This document owns** what each NPC remembers about other individuals and groups, and how that memory changes:
- the bounded relationship table (familiarity, trust, affection, fear, perceived kinship, believed faction, help and exchange ledgers, the per-pair contest history, economic standing cues);
- recognising individuals, imperfectly;
- obligations and promises as memories, and how a failed promise becomes a betrayal;
- betrayal, apology and forgiveness records;
- second-hand reputation, gossip and lies in gossip, and reputation of groups;
- forgetting: decay and eviction;
- how this memory feeds the decider and the learning core, and how the game-theory strategies become *possible* (never assigned);
- the memory budget in bytes, telemetry, milestone split and tests.

**It links to, and does not repeat:**
- the gesture vocabulary, the message grammar and decoding errors: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- personality traits (Trustfulness, Vengefulness, Forgiveness, Honesty, Generosity): [POPULATION_DESIGN.md](POPULATION_DESIGN.md#personality);
- associative learning keyed by situation and intent: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- the kinship graph (the truth about relatedness): [HOUSEHOLDS_AND_KINSHIP_DESIGN.md](HOUSEHOLDS_AND_KINSHIP_DESIGN.md), and kin relatedness values: [BIOLOGICAL_INHERITANCE_DESIGN.md](BIOLOGICAL_INHERITANCE_DESIGN.md);
- items, ownership records, deferred exchange mechanics and the worth units NPCs value goods in: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md);
- the civilian contest mechanic (escalation turns, posture, when a contest becomes a `Duel`): [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record); soldier combat: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- institutions that make promises enforceable: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md);
- what happens to estates and debts at death: [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md);
- norms, gossip-borne stories and imitation of prestigious others: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- the book's strategies and how they are measured: [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md), [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md), and the source notes in [research/SELFISH_GENE_NOTES.md](research/SELFISH_GENE_NOTES.md).

Information layers: a relationship record is layer 6 (individual belief), built from layer 2 (observed), layer 3 (communicated) and later layer 5 (published). Nothing here reads true state.

## 2. Principles
1. **A memory, not a rule.** A promise, a debt or a friendship exists only as records in NPC minds. The sim never moves goods or changes a mind because a promise fell due. Two parties can hold different records of the same event.
2. **Bounded and lossy.** Every table has a fixed size. The least important entry is forgotten first. Forgetting is a feature: it lets cheats start again, grudges fade, and strangers stay strangers.
3. **Imperfect recognition.** Individuals are recognised with a probability that grows with familiarity. A cheat can hide, and a stranger can be mistaken for someone known.
4. **Personality sets rates, never outcomes.** Vengefulness changes how fast a grudge decays. It does not decide whether the NPC retaliates; the decider does.
5. **No named strategies in the data.** The record stores facts (what happened, when, how much). "Grudger" and "Tit for Tat" are patterns an observer can measure afterwards.
6. **Cues are not truths.** Perceived kinship and believed faction are guesses that can be wrong or out of date. The true kinship graph is used only by the genetics and estate code, never by the decider.
7. **Integers, fixed order, forked streams.** All values are integers. Every pass runs in ascending NPC id, then ascending slot index. Random draws use `Rng.Fork("npc", id)`.

## 3. Data structures (`Sim/Social`)

```csharp
struct SocialMind {                        // one per NPC, fixed size, inside Mind
  sbyte RecentWins;                        // -8..8, cricket-style general memory of contest outcomes
  byte  RelCount, OblCount, HearCount, GroupCount;
  int   NextObligationSeq;                 // local counter, not a global id
  Relation[]    Rel;      // RelSlots   (default 40, SimTuning; range 32-48)
  Obligation[]  Obl;      // OblSlots   (default 16)
  Hearsay[]     Hear;     // HearSlots  (default 32)
  GroupRep[]    Group;    // GroupSlots (default 16)
  SocialEvent[] Log;      // ring, 16: recent betrayals, apologies, forgiveness, accusations
  SeenStranger[] Seen;    // ring, 8: sightings of individuals with no record yet
}

struct Relation {                          // 56 bytes
  int   OtherId;                           // NpcId
  int   FirstTick, LastTick;               // first and last interaction (not mere sighting)
  short Familiarity;                       // 0..1000
  short Trust;                             // -1000..1000: expectation they will cooperate
  short Affection;                         // -1000..1000: liking
  short Fear;                              // 0..1000
  short Grudge;                            // 0..1000: hostility memory, blocks trust recovery
  short BelievedFaction;                   // FactionId or -1 unknown; refreshed only by seeing faction colours, a message or an AskJoin
  byte  BelievedFactionConf;               // 0..255
  byte  KinCue;                            // cached believed relatedness r̂ from Mind.Kin (HOUSEHOLDS_AND_KINSHIP), r*500 (0 = none, 250 = r of 0.5); refreshed daily
  byte  Esteem;                            // 0..255: believed skill and success (read by Culture)
  ushort Flags;                            // household mate, partner, creditor, debtor, enemy, ...
  short HelpGiven, HelpReceived;           // value units (see 4.2), saturating, halve at 30000
  short OwedToMe, IOwe;                    // exchange ledger, value units each way
  byte  Wins, Losses; sbyte Streak;        // per-pair contest history, for pecking orders
  byte  Betrayals, Apologies, Forgiven;    // counts of records in the Log
  byte  LastMoveMine, LastMoveTheirs;      // 0 none, 1 cooperated, 2 defected (last exchange or help)
  short ImportanceCache;                   // recomputed daily
  // economic standing cues (M10), read by ECONOMIC_CLASSES_DESIGN 4.9 and POLITICAL_ECONOMY_DESIGN
  byte  SeenEmploying; sbyte WealthGuess; byte EconFlags;
  int   SeenEmployedBy;
}

struct Obligation {                        // 32 bytes. This NPC's own copy.
  int   Seq, OtherId, MadeTick, DueTick;   // DueTick = NoDate for "when I die" (bequests)
  int   ContractId;                        // 0 normally; set if registered with an institution
  ushort Ware; ushort Qty; short Value;    // what, how much, in value units (my belief)
  byte  Kind;     // Repay, Reciprocate, Deliver, HelpAt, Return, Bequest, Other
  byte  Status;   // Open, Kept, Broken, Forgiven, Cancelled, Disputed, Orphaned
  byte  Dir;      // 0 they owe me, 1 I owe them
  byte  Confidence; // 0..255: how sure I am about the terms (misheard, vague, faded)
}

struct Hearsay {                           // 20 bytes: second-hand reputation
  byte  SubjectKind;                       // Npc, Faction, Household, Enterprise
  int   SubjectId;
  short Rep;                               // -1000..1000
  short Conf;                              // 0..1000
  int   SourceId, Tick;                    // who told me, when
  byte  Hops;                              // 0 = source saw it; 1..3 = passed on
  byte  Tag;                               // Cheat, Thief, Violent, Reliable, Generous
}

struct GroupRep { byte Kind; int Id; short Rep; byte Conf; }   // 8 bytes: from my own contacts
struct SocialEvent { int Tick, Other; byte Kind; byte Severity; short Value; }  // 16 with padding
```

**The state as a pseudo-agent.** One `Relation` slot per NPC is reserved for its own faction's state (`Other = −1 − FactionId`), never evicted. State defaults on wages, unjust fines and handouts update it through the same evidence table (4.2) as a person would; records of individual state agents (keepers, guards) are ordinary relations. It uses one of the 40 slots, so the budget is unchanged ([STATE_DESIGN.md](STATE_DESIGN.md)).

### Memory budget per NPC (defaults)

| Table | Slots | Bytes each | Total |
|---|---|---|---|
| Header | 1 | 16 | 16 |
| Relations | 40 | 56 | 2240 |
| Obligations | 16 | 32 | 512 |
| Hearsay | 32 | 20 | 640 |
| Group reputation | 16 | 8 | 128 |
| Event log (ring) | 16 | 16 | 256 |
| Strangers seen (ring) | 8 | 12 | 96 |
| **Total** | | | **3888 B** |

Budget: at most 4096 B per NPC. At 600 NPCs that is about 2.3 MB for all of social memory. The save file stores it as is.

## 4. Mechanics and algorithms

### 4.1 Recognition (imperfect)
When NPC A perceives NPC B (ascending B id), A makes one recognition roll per think tick in which B is attended, using A's own stream:
- **Miss chance** `miss = FailBase × (1000 − Familiarity) / 1000`, with `FailBase` 350 (per mille). Add 100 in darkness and 150 beyond half sight radius. Subtract 100 if B is carrying goods A has just traded with B. Clamp 0..900.
- **On a miss** B is treated as a stranger for this tick: relation unread, and only a `Seen` entry is made. This is how a cheat hides.
- **False match** (about one quarter of `miss`, and only if some known C has the same faction colour and gear class): A treats B as C. A acts on C's record. If B then wrongs or helps A, the *update goes to C's record*, until A notices the mismatch (B does something clearly out of character, or A hears B's name used). On noticing, the update is reversed on C at half strength and a new record is made for B.
- **Familiarity** rises by 100 per completed interaction and 40 per distinct day of contact, up to 1000, and falls by 2 per day without contact.
- **A stranger seen once** gets a `Seen` entry only. The second sighting within 5 days, or the first real interaction, creates a `Relation` (slot rules below).

### 4.2 Updating a relation (the evidence table)
**Value units** are the NPC's own worth units ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#45-exchange): 1000 = a day's hunger relief for this NPC) divided by 10, so 100 is about one day's food as this NPC values it. They are the NPC's belief, never the reference index: reports convert the goods behind a ledger to `FE` with `WareRefValue` ([ECONOMY_DESIGN.md](ECONOMY_DESIGN.md#33-reference-value-per-ware-warerefvalue-and-the-fe-unit)), and no NPC reads that. All deltas are applied once, on the tick the NPC observes the event. `sev` is severity 0..255.

| Evidence (first-hand) | Trust | Affection | Other |
|---|---|---|---|
| Received a gift or help of value v | `+min(150, v/2) × (1000 − Trust)/1000` | `+delta/2` | `HelpReceived += v`, `IOwe += v`, `LastMoveTheirs = 1` |
| Gave a gift or help of value v | none | `+v/10` (liking those we help) | `HelpGiven += v`, `OwedToMe += v`, `LastMoveMine = 1` |
| Barter completed (both hand-overs seen) | `+30` | `+10` | `LastMoveTheirs = 1` |
| Partner took but did not hand over within the timeout | `−4 × sev`, `sev = clamp(v/2, 20, 255)` | `−sev` | betrayal record, `LastMoveTheirs = 2` |
| Promise kept | `+40 + min(80, Value/10)` | `+10` | `Kept` |
| Promise broken | `−4 × sev`, `sev = clamp(20 + Value/2, 20, 255)` | `−sev` | betrayal record |
| Caught in a lie | `−4 × 80` | `−60` | betrayal record |
| Theft from me | `−4 × 200` | `−200` | betrayal record; `Fear += 100` |
| Theft from a third party, witnessed | `−100` | `−50` | hearsay-style record on the thief |
| Attacked | `−1000` | `−300` | `Fear += 300`, `Grudge += 255 × 3` |
| Refused my request when I was in need | `−60` | `−30` | `LastMoveTheirs = 2` (not a betrayal: no promise) |

Trust is clamped to −1000..1000. **Trust gain after a betrayal** is multiplied by `(1000 − Grudge)/1000`, so a strong grudge makes forgiveness slow even when the other side behaves well. A betrayal sets `Grudge += sev × 3` (cap 1000) and raises the Anger drive in `Population`.

**Starting trust for a stranger:** `Trustfulness × 3` (so −300..300), plus 100 if the believed faction matches mine, minus 100 if it is hostile.

### 4.3 Class of a relationship (the learning feature)
Each think tick, for each attended NPC, the record is reduced to a **relationship class** (4 bits), in this priority order:
1. `Partner` or `HouseholdMate` (flags);
2. `Enemy`: `Grudge > 600`, or `Fear > 500` with trust below 0;
3. `Rival`: `Grudge > 300`, or `Losses > Wins + 2`;
4. `Debtor` (they owe me, open obligation) / `Creditor` (I owe them);
5. `KinCue` ≥ 60 (believed relatedness ≥ about 0.12): `Kin`;
6. `Friend`: `Trust > 300` and `Affection > 300`;
7. `Acquaintance`: `Familiarity ≥ 100`;
8. `Stranger` (no record, or recognition missed).

The decider also gets the raw numbers (trust, ledger and so on) for its social terms.

### Obligations and promises
**A promise is a pair of memories.**
1. Speaker S makes a `Promise` gesture (aimed at listener L, Topic the ware or help, Quantity, Time later). The sim does nothing but emit `GestureMade`.
2. L decodes it (possibly wrongly, see the misunderstanding rules in the gesture language). L makes an `Obligation` (Dir 0, they owe me) with `Confidence` set by decode quality and whether real goods were presented.
3. S makes its own `Obligation` (Dir 1) **only if it intends to keep it**. A liar writes nothing. The telemetry event carries an `Intended` flag for analysis only; no NPC can read it.
4. A deferred exchange (M7) is the same, except that one hand-over has already happened. L's ledger updates (`OwedToMe`), and L's obligation is created from the observed hand-over, not just a gesture.

**Settlement is a check by the creditor.**
- On or after `DueTick`, at the creditor's next think tick, it checks whether it observed a matching hand-over (or accepts that it did not).
- If it did, `Kept`: `PromiseKept`, trust and ledger updated, `LastMoveTheirs = 1`.
- If not, after a grace period (`GraceTicks`, default `TicksPerDay / 8` = 150 ticks) it marks `Broken`, writes a betrayal record, and emits `PromiseBroken`. It may then `Accuse`, retaliate, or let it go (all decider choices).
- The debtor's own record makes the debtor *want* to pay: the score has a term `Value × honesty weight × creditor importance`. It may instead `Apologise` before the due date, ask for time (`Promise` again, new `DueTick`), or forget (see forgetting).
- **Disagreement is possible.** The debtor may think it paid, or may think the terms were different. The creditor's `Disputed` status keeps both records until one side stops caring.
- **Orphaned obligations:** if the counterparty dies, the record becomes `Orphaned`. An heir may re-open it by making a claim (a `Want` or `Accuse` at the debtor). Whether the debtor then honours it is its own decision. See [MATERIAL_INHERITANCE_DESIGN.md](MATERIAL_INHERITANCE_DESIGN.md#48-debts-and-claims).

**Courts and institutions later.** An obligation can carry a `ContractId` when a headman's court, a court or a registrar writes it down (institutional record, layer 4; see [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)). The memory rules do not change. What changes is the debtor's expected cost of default (a believed penalty enters the score), and a staff NPC may act on the record. Staff can be slow or corrupt, so enforcement is an outcome of the institution, not a property of the promise.

### 4.5 Betrayal, apology and forgiveness
- **Betrayal record** (`SocialEvent` Kind Betrayal): tick, other, severity, value. `Betrayals` on the relation counts them; the log is a ring, so old events fall off, but the counter and `Grudge` remain.
- **Apology:** the `Apologise` gesture. The receiver's decider scores `Forgive` using `Forgiveness × 3 + Affection/4 + apology gift value/2 − Grudge/2`, plus seeded noise of ±50. Choosing `Forgive` is the act; it sends a `Forgive` gesture and applies: `Grudge −= 200 + gift value`, `Trust += 100`, `Forgiven += 1`. An apology that arrives with restitution (the goods) also clears the matching `IOwe` or `OwedToMe`.
- **Cheap talk:** an apology costs nothing, so a repeat cheat can apologise every time. Whether it works depends on the receiver's `Forgiven` count and `Betrayals`: the term `−(Betrayals × 40)` is added to the forgiveness score, so repeat apologisers meet resistance. This is a score term, not a ban.
- **Retaliation** is not stored here. The decider reads `Grudge`, `Fear`, the contest record and the situation.

### 4.6 Reputation and gossip

**Gossip is a gesture message.** `Accuse` and `Praise` use the existing grammar:
- `Act = Accuse` or `Praise`;
- `Topic` = an NPC (the subject), or a building for groups (a faction HQ for a faction, a hut for a household, a workshop for an enterprise);
- `Quantity` (1..9) = intensity, mapped to severity `Quantity × 28`;
- `Target` = the listener (one NPC, or anyone watching).

`Praise` is in the gesture vocabulary ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#vocabulary-fixed-and-shared-at-first)). Direct `Accuse` at the offender (the existing meaning) is unchanged.

**Receiving gossip about subject X from source T:**
1. Source weight `w = Trust(T) clamped to 0..1000 / 1000 × Confidence of the message`. The message confidence is 800 for something T says it saw and `700 ×` the hearsay confidence per hop it passes on.
2. If A has a relation with X and `Familiarity(X) ≥ 400`, A's own experience wins: the relation's trust moves by only `1/8` of `w × severity`. Otherwise the hearsay table takes it fully.
3. **Hearsay entry** `Rep = ±severity × 4`, `Conf = w`, `Hops = hops + 1`, `SourceId = T`. If the same subject and source exist, replace. If a different source, the two blend by confidence.
4. **Effective trust used by the decider:**
   `eff = (Familiarity × Trust + (1000 − Familiarity) × HearsayRep × Conf / 1000) / 1000`.
   So a stranger is judged almost wholly by hearsay, a friend almost wholly by experience.
5. **Echo guard:** A never passes a hearsay item back to the source it came from, and does not repeat a subject to the same listener within 2 days. Hops are capped at 3, and confidence falls by 30 percent per hop (800 to 560 to 392 to 274).

**Deciding to gossip.** `Accuse` or `Praise` about a third party is just another candidate intent. Benefits are scored from memory: warn a friend (their affection), harm an enemy, or raise one's own status by being the source of useful news. Costs: time, and the risk of being caught.

**Lying in gossip.** A false accusation scores like a lie: expected gain (hurting a rival, helping a friend), minus guilt (`Honesty`), minus the expected cost of being caught (belief about who knows the subject well). The listener can check: if it knows X well (`Familiarity ≥ 600`) and its own trust in X is high (`Trust ≥ 400`) while the gossip is hard on X, then the **source's trust drops by 100 and an `Accused` false-flag event is recorded** (visible only to the telemetry as `Truthful = false`). The listener may then reply with `Accuse` at the source. The sim never knows who lied, except for analysis.

**Reputation for groups.** `GroupRep` is the NPC's stereotype from its own contacts. Each interaction with a believed member of group G adds one quarter of the individual delta to `GroupRep[G]`. Hearsay at group level uses a building as the Topic. Group reputation feeds:
- initial trust for strangers (believed faction);
- loyalty and migration beliefs (see [STATE_DESIGN.md](STATE_DESIGN.md#loyalty-migration-and-defection));
- the "known cheat is near" situation feature, which can fire for a whole faction.

Faction reputation can be wrong and sticky. An NPC that sees a member of a stereotyped group behave well updates only a quarter as far.

### 4.7 Forgetting
**Daily pass** (each NPC on its own day slot, `(tick + id × 7) % TicksPerDay == 0`, ascending id, so the work is spread).

**Life scaling (owner decision Q1, 27 Nov).** Per-day decay is per sim day and is **not** scaled by `LifeScale`, which affects biological life-cycle durations only ([conventions §4](DESIGN_CONVENTIONS.md#4-time-model)). The same rule applies to the learning store's decay ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#11-open-questions)). So at `LifeScale` 200 a grudge lasts the same number of days but a larger share of a life.

**Decay rates are independently configurable for experiments.** Every per-day rate in the list below is a field of `SimTuning.Social.Decay` (saved with the state and hashed), with the default shown: `FamiliarityPerDay` 2, `GrudgeBase` 60 and `GrudgeFloor`/`GrudgeCeil` 10/110 per mille, `TrustDriftPermille` 10, `FearShift` 4 (that is, `Fear/16`), `HearsayConfPerDay` 20, `RepShrinkPermille` 30, `ObligationConfPerDay` 3, `GroupRepShrinkPermille` 10. A single `ScalePermille` (default 1000) multiplies all of them together (integer, rounded half up, at least 1 where the base is non-zero), so an experiment can make social memory fade faster or slower, or switch one kind of forgetting off (rate 0), without touching `LifeScale` or the learning store's own rates. Changes caused by events are never scaled.

- `Familiarity −2` if no contact today.
- `Grudge −= Grudge × GrudgeDecay / 1000`, where `GrudgeDecay = clamp(60 − Vengefulness/2, 10, 110)` per mille per day. At Vengefulness +100: 1 percent per day (half-life about 69 days, which outlasts a lifetime of about 40 days). At −100: 11 percent per day (half-life about 6 days).
- `Trust` moves 1 percent of the gap per day towards the stranger prior, only when `Familiarity < 300` (and never while `Grudge > 300`). A negative trust therefore fades for people we barely know, but not for someone who wronged us and whom we still remember.
- `Fear −= Fear/16`.
- Hearsay confidence `−20` per day; delete at 0. `Rep` shrinks 3 percent per day.
- Obligation `Confidence −3` per day after `DueTick + 5 days`; delete when 0. Open obligations that have been forgotten are the honest kind of non-payment: the debtor simply no longer remembers.
- `GroupRep` shrinks 1 percent per day.

**Eviction** (a new relation needs a slot and none is free):
`Importance = |Trust|/2 + |Affection|/2 + Fear + Grudge/2 + Familiarity/4 + 150 × (open obligation) + 150 × (non-zero ledger) + 100 × (Betrayals>0)`, divided by `1 + daysSinceLastTick/10`.
- **Protected, never evicted:** partner, household mates, anyone with an open obligation.
- Pick the lowest `Importance` among unprotected slots, ties by lowest slot index. Replace it only if the newcomer's starting importance (from the interaction that created it) is higher. Otherwise the newcomer stays only in the `Seen` ring.
- Eviction emits `RelationEvicted`, sampled, with the old values, so it can be measured how often a strong relation was lost.
- Cheats can exploit eviction. A cheat who wrongs many strangers is forgotten by most, which is exactly the situation in which reputation by gossip matters.

## 5. Interactions with other systems

**The decider** ([COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md#47-the-decider)) reads: `eff` trust, affection (the "welfare counts" term), the ledger, open obligations, grudge, fear, kin cue and the class. Situation features that come from social memory: stranger is offering; known cheat is near; a friend is in need; the partner owes me; I owe them; they beat me last time.

**Learning** ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)). Two layers, kept distinct:
- *Per individual:* the `Relation` record. It learns from one meeting.
- *Per class:* the learned associations use the **relationship class** (4.3) as a context feature next to the situation features. "Offer from a stranger, then cheated" teaches wariness towards strangers in general, while the cheat's record teaches wariness towards that cheat. After a bad experience with a stranger the NPC is wary of strangers through the class feature, and the long-term weight is gentler (the two timescales in the learning doc).
- Vicarious learning (watching others) updates the association store, not the relation, unless the NPC can identify the individuals.

**How the game-theory strategies become possible** (nothing here is a strategy; these are the ingredients):

| Book example | What the record supplies | What else it needs |
|---|---|---|
| Sucker, Cheat, Grudger (SELFISH_GENE_NOTES E2) | Recognition; `Betrayals` and `Grudge`; low `Forgiven` count | Personality: a low Forgiveness and high Vengefulness make a **Grudger-like refusal**: the situation feature "partner betrayed me, grudge high" scores refusal highly for a long time. A Cheat is a decision to take without giving; the record is what lets Grudgers punish it. |
| Tit for Tat (E4-E6) | `LastMoveMine` and `LastMoveTheirs` per partner | A **Tit-for-Tat-like** NPC scores "do what they did last time". The benchmark `TitForTatNpcPolicy` reads exactly these two bytes, so benchmark and island use one memory. Forgiving variants use `Forgiven`; suspicious ones start with a low trust prior. |
| Vampire bats (E10) | `HelpGiven`, `HelpReceived`, `OwedToMe`, `IOwe`, familiarity, kin cue | A feature "friend in need" plus a surplus (cost to me low, value to them high). Sharing with those who shared with me is a score term from the ledger. Recognising roostmates is the familiarity and household flag. |
| Pecking orders, hens (A6) | `Wins`, `Losses`, `Streak`; `RecentWins` in the header (cricket version) | Outcomes of the civilian contests of [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#31-the-contest-record) (`ContestEnded`) and of soldier combat in [MILITARY_DESIGN.md](MILITARY_DESIGN.md). Dovish behaviour towards whoever beat me is a decider term. |
| Resident/intruder, ownership (A4) | Believed faction and "I am owner here" are separate features | The ownership convention is a cultural trait. |
| Live and let live, truces (E8) | Per-pair history of who held fire; `LastMove` pairs between opposing groups | Stable local pairs; see [MILITARY_DESIGN.md](MILITARY_DESIGN.md). Apologies for accidents are the `Apologise` gesture. |
| Deception in signals (B) | Caught-lie records; trust loss | Liars can still gain until caught. |

None of these is selected. A scenario can *measure* after the fact how Grudger-like or Tit-for-Tat-like an NPC was ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md#410-measuring-strategies-in-the-island)).

**Other links:**
- `Population`: the Anger and Loneliness drives read betrayal and affection. A death removes records on others' tables slowly (the dead person's relation decays and is eventually evicted; obligations become `Orphaned`).
- `Culture`: `Esteem` and the candidate list of recent contacts are the models for imitation. Norms such as "repay debts" change the guilt term for broken promises. Cultural transmission: [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md).
- `Economy`: ledger values use the NPC's own worth units and price beliefs (a different NPC may value the same thing differently), never `WareRefValue`. Deferred exchange: [ECONOMY_DESIGN.md](ECONOMY_DESIGN.md).
- `State` and loyalty: group reputation of factions, and hearsay about how citizens are treated.
- `Institutions`: elders and mediation read `Disputed` obligations as cases. A records office is a separate layer-4 store, not a change to this memory.

## 6. Player and view
- **NPC inspector card** shows the NPC's own view only: its strongest few relations (class, trust and affection as bars), open obligations ("owes me 2 fish, due tomorrow"), and what it has heard about named others, with the source. Never the truth about whether someone lied.
- **World animations:** the gesture animations of `Accuse`, `Praise`, `Apologise`, `Forgive` and `Promise` already exist. A small mood icon near an NPC (friend, enemy, debtor) is optional decoration from the class.
- **Debug overlay (off by default):** a relationship graph of one selected NPC, and a "promise ledger" of the island showing both parties' records with disagreement marked. This reads the sim, never changes it.

## 7. Telemetry

**Events** (all stamped with tick and ids; all pure data):

| Event | Fields |
|---|---|
| `PromiseMade` | maker, receiver, kind, ware, qty, due tick, `Intended` (analysis only), decoded-correctly flag |
| `PromiseKept` | maker, receiver, obligation key, ticks late |
| `PromiseBroken` | maker, receiver, value, ticks past due, `Forgot` flag (analysis only) |
| `Accused` | accuser, subject, listener, severity, `Truthful` (analysis only), first-hand or hearsay |
| `Praised` | praiser, subject, listener, severity |
| `Apologised`, `Forgave` | from, to, grudge before and after, gift value |
| `BetrayalRecorded` | victim, offender, kind, severity, value |
| `RecognitionError` | observer, observed, kind (miss or false match), familiarity |
| `TrustChanged` | a, b, before, after, cause. Always emitted when `|delta| ≥ 100`; otherwise sampled by the pure function `(tick × 31 + a × 17 + b) % 16 == 0` (no random draw, so sampling cannot affect the sim) |
| `RelationEvicted` | owner, evicted, importance, trust, grudge (sampled 1 in 4) |
| `GossipPassed` | from, to, subject, hops, confidence (sampled 1 in 8) |

Existing event names from the conventions (`GiftGiven`, `PromiseMade`, `PromiseBroken`) are used as they are.

**Snapshot fields** (daily person snapshot): `RelCount`, `MeanTrust`, `FriendCount`, `EnemyCount`, `OpenObligationsOwed`, `OpenObligationsDue`, `GrudgeSum`, `HearsayCount`. An optional **edge snapshot** (daily, level of detail "full" only) lists relations with `|Trust| ≥ 400` or an open ledger above 100, for the relationship graph facts in [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md).

**Analysis-only fields** (`Intended`, `Truthful`, `Forgot`) come from layer 1 and are for experiments. They never enter any observation, no policy reads them, and recorders store them only at the `Full` recording level ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#41-recording-levels)).

## 8. Stage 4 minimum versus later

| Milestone | Included |
|---|---|
| **M5** (social memory, gestures, honest barter) | `Relation` table with familiarity, trust, affection, fear, help ledger, exchange ledger, last-move bytes, believed faction, kin cue from household flags, importance eviction, imperfect recognition, evidence updates for gifts and barter, the daily pass, relationship class, telemetry for trust and recognition. Wins, losses and `RecentWins` are filled from the civilian contests that start at M5 (unarmed). |
| **M7** (promises, reputation, lies) | Obligations and promises, deferred exchange, betrayal records, apologies, forgiveness, grudge, hearsay, gossip through `Accuse` and `Praise`, lies in gossip, group reputation, `Orphaned` status, the promise and gossip events. Observational learning hooks. |
| **M8** | Theft and witness records (third-party). Cooperative and enterprise reputation as group topics. |
| **M9-M10** | Contest history from soldier combat, surrender, truces, defection; economic standing cues (M10); faction reputation shapes loyalty. |
| **M11** | `Esteem` and candidate models consumed by cultural transmission. Norm-based guilt for broken promises. |
| **M12** | Elders' mediation reads `Disputed` obligations and `Accuse` histories. |
| **Later (Stage 5-6)** | Obligations with a `ContractId` enforced by courts; written records and credit registers (layer 4); media reputation (layer 5); learned lexicon for gossip; richer group topics; reputation institutions. |

### 8.1 As built in M5 (branch `unity/m5`, 26 Nov 2026)

The M5 row above is implemented in `Sim/Social` (`SocialMind`, `SocialMemory`, `SocialSim`, `SocialSystem`), with these details decided by the implementation:

| Topic | As built | Why |
|---|---|---|
| Relation record | 60 B: the fields of section 3 that M5 needs, plus `LastSeenTick`, `LastNode` (where to look for someone), `LastContactDay`, `GrudgeRem` (the carried fraction of the daily grudge decay, so 20 days at 1% a day is exact), `Holds` (wares it saw the person show or hand over, with `HoldsTick`) and own counts for the card (`DealsDone`, `Cheated`, `GiftsIn`, `GiftsOut`). Contest history, esteem, believed faction and economic cues are not yet built (M7 and later). Tables per mind: 40 relations, inbox 6, deals 6, prices 32, seen ring 8: **3136 B** | Bounded tables; the save packs relations into a blob, as the belief store does |
| Recognition | At each think tick the observer's people (the nearest 8 in its sight, by distance then id) are recognised once, with the keyed stream `recognise(observer, tick, person)`; decoding a message recognises its speaker with `decode(listener, tick, message)`. A miss leaves the person unplaced (`As = 0`): no relation is read or written. Someone with no record is a new face, and its id becomes the key. **False matches are not built** (M7) | Draws do not depend on who else is seen (G2.10) |
| Familiarity | +180 per completed interaction (a hand-over given or received), +20 per addressed message, +40 per day of contact, -2 per day without | Section 4.1 says +100 per interaction; with 100, test 5 ("under 3% after 5 interactions") cannot pass (5 x 100 + 40 = 540, a 16% miss). 180 gives 940, a 2.1% miss |
| New relations | The second sighting on another day within 5 days, or the first interaction; eviction as 4.7 (ties to the lowest slot; a newcomer must be more important) | |
| Evidence | Gift received or given, barter completed, betrayal (a deal partner took and did not hand over within `DealTimeoutTicks`, 360; severity `clamp(v/2, 20, 255)` of the value given), refused request; trust gains after a betrayal x (1000 - Grudge)/1000 | 4.2 |
| Forgetting | `SimTuning.Social.Decay` (`ScalePermille`, `FamiliarityPerDay`, `GrudgeBase`/`GrudgeFloor`/`GrudgeCeil`, `TrustDriftPermille`, `FearShift`), never scaled by `LifeScale` | Q1 |
| Class | The 4.3 priority; `Debtor`/`Creditor` from a deal half done; household mate = shares the household (M4) or, with no households, the hut's sleeping rights | SHARED_CONTRACTS 5 |
| Use in decisions | The relationship class (`NpcRel`) and the person (`NpcRef`) are target features of every social candidate, so what was learned about a class or a person changes choices (L16 relationships: 49 of 50 seeds). The **raw-number social term** (trust, grudge, affection and a deal's surplus, `Social.Term`) is built but **off by default**: the frozen inspector option row (`OpportunitySlot`) has no Social column, so with the term on the shown terms would not add up (M3.28). Proposed amendment: add `Social` to `OpportunitySlot`, then default `Social.Term` to 1 | Contract freeze |
| Apology and forgiveness | The scores and effects of 4.5 (`ForgiveScore`, `Forgive`) are tested as units (M5.3); the `Apologise` and `Forgive` acts are not candidates until M7 | M7 scope |
| Not in M5 | Obligations and promises, hearsay, gossip, group reputation, the event log ring (M7, as in the table above) | |

## 9. Migration from the existing code
There is no social memory in stage 3. `AiTrader` keeps its own simple faction-to-faction trade history. That history stays with the faction level (`IFactionPolicy`). NPC social memory is new in `Sim/Social`, added at M5. Save version 3 includes it. `StateHash` covers every table, in index order.

## 10. Tests and acceptance criteria

All tests run in `dotnet test DotNet` and in Unity EditMode. A fixed seed is used unless a range is given.

**Bounds and eviction**
1. After one NPC has met 200 distinct NPCs, `RelCount ≤ RelSlots` (40). Partners, household mates and anyone with an open obligation are still present.
2. Eviction picks the lowest importance, with ties to the lowest slot index. Same seed, same evictions, 3 runs.
3. `sizeof(SocialMind)` for default slots is at most 4096 B (expected 3888 B). 600 NPCs use at most 2.5 MB.

**Recognition**
4. Over 10,000 draws, at `Familiarity 50` in daylight at short range the miss rate is 30-36 percent. At `Familiarity 800` it is 5-9 percent. At 1000 it is 0.
5. A cheat who defrauds a stranger and is met again after one sighting is unrecognised in 30-36 percent of meetings (hides). After 5 interactions it is unrecognised in under 3 percent.

**Obligations and promises**
6. A `Promise` with due tick T where the debtor hands over nothing: the creditor's record is `Broken` by `T + GraceTicks` (150), `PromiseBroken` is emitted exactly once, `Betrayals` is 1, and trust falls by at least 200 for a value-100 promise.
7. **No enforcement:** across a run, no inventory or ownership record changes because a promise was made or broken (the only transfers are NPC hand-over intents). A static check on the `Social` module finds no call into `Economy` mutation.
8. A debtor with `Honesty +100` and a creditor with importance above 500 keeps at least 90 percent of promises when it can afford to. A debtor with `Honesty −100` keeps under 60 percent. The seeded scenario uses 200 promises each.
9. A liar's promise (no debtor record) shows `Intended = false`, and the debtor's `Obl` table contains no entry. The creditor's does.
10. An obligation whose creditor dies becomes `Orphaned` within 1 tick and is not paid unless an heir claims it.
11. A registered obligation (`ContractId ≠ 0`) changes the *expected cost* term only. Memory records and statuses behave as before. (Contract exists from Stage 5.)

**Trust and forgiveness**
12. At `LifeScale` 100, after a value-100 betrayal, `Grudge` is 300 at `sev` 100. With `Vengefulness +100`, grudge after 20 days is at least 245 (about 82 percent remaining). With `−100` it is at most 30 (about 10 percent). At `LifeScale` 200 the grudge after 20 days equals its `LifeScale` 100 value (decay is never scaled by `LifeScale`). With `SimTuning.Social.Decay.ScalePermille` 2000 the grudge is strictly lower than in the 1000 run on every day it is non-zero, and the learning store's weights are identical to the 1000 run (the two decay settings are independent); with `GrudgeBase`, `GrudgeFloor` and `GrudgeCeil` all 0 the grudge is unchanged after 20 days.
13. After a betrayal with grudge 600, a helpful act raises trust by `delta × 0.4`, not `delta`.
14. An `Apologise` with a gift raises the forgiveness score by `giftValue/2`. A repeat apology after three betrayals scores `120` lower than after none.

**Gossip**
15. A 3-hop chain of one accusation starting at confidence 800 arrives at 560, 392 and 274. Hops 4 and above are not passed on.
16. A listener with `Familiarity ≥ 600` and `Trust ≥ 400` about X who hears a false accusation of X moves its trust in X by at most 20, and its trust in the source drops by 100.
17. Echo guard: in a 10-NPC fully connected scenario with one accusation, no NPC receives the same subject from the same source twice within 2 days.
18. A stranger's effective trust is within 5 percent of its hearsay rep; a friend's is within 5 percent of its own trust.

**Mechanism checks for the strategy ingredients** (these test the *memory*, not emergence)
19. `TitForTatNpcPolicy` returns cooperate after `LastMoveTheirs = 1` and defect after `2`, 100 percent over 1000 trials, from the same `Relation` bytes that the island NPCs read.
20. A Grudger-like benchmark reading `Betrayals > 0 && Grudge > 300` refuses 100 percent when so, and 0 percent when not.

**Scenario measures (reported, not forced):** in a 12-NPC shared-hut scenario with differing luck, the fraction of food gifts going to NPCs who gave in the previous 5 days versus those who did not (expected higher for the former, tolerance and baseline set at first M7 run).

**Determinism and recording**
21. Same seed, 30 sim days: identical `StateHash` at every day boundary. A run with recording on has the same hash as one with it off. Save and load with open obligations mid-run gives an identical hash and event stream afterwards.

## 11. Open questions

**Owner decision Q1 (27 Nov 2026, [conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)):** keep the default. `LifeScale` affects biological life-cycle durations but not social-memory decay; the decay rates are independently configurable for experiments (`SimTuning.Social.Decay`, section 4.7).

Settled in review (26 Nov 2026): `Praise` is in the gesture vocabulary, with `Topic = building` for group topics; ledger values are the NPC's own worth units, and reports use `WareRefValue`; `BelievedFaction` is refreshed only by faction colours, a message or an `AskJoin`. No open questions remain.

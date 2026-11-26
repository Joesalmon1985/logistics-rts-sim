# Communication and cognition design (stage 4)

## 1. Purpose and scope
This document covers how an NPC perceives, attends, knows and decides, and the gesture language it communicates with. It follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md).

**It owns:**
- perception, attention and beliefs (`Cognition` module);
- situation features and the context each candidate is judged in;
- the decider: candidates, the score, and the default NPC policy;
- the gesture language: grammar, vocabulary, honesty, misunderstanding and responses (`Comms` module).

**It links to, and does not repeat:**
- body, drives and traits: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- instincts, associative learning, delayed reward, generalisation, forgetting and the five learning mechanisms: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- memories of other NPCs (trust, obligations, reputation, gossip): [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md);
- dialects, imitation, teaching and memes (`Culture` module): [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md);
- the seven information layers in detail: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- the military combat policy: [MILITARY_DESIGN.md](MILITARY_DESIGN.md);
- network policies: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md).

The background research is in [research/CREATURES_NOTES.md](research/CREATURES_NOTES.md).

## 2. Principles
- **No NPC reads the true game state.**
  - An NPC policy receives only an `NpcObservation`, built by perception, plus a read-only view of that NPC's own mind (beliefs, social memory, learned associations). It returns an `NpcIntent`.
  - `INpcPolicy` lives in `Sim/Policy` and has no access to `GodState`. Observations are plain value types: ids, classes, counts and relative positions.
  - The test `Policies_DoNotReferenceGodState` checks by reflection that no type in the policy namespaces has a field, parameter or local of type `GodState`, `EconomyState` or similar.
  - Scripted, benchmark and trained policies use the same observation and intent types, so none can cheat.
- **Decisions come from instincts plus learned associations.** Hand-written estimates are innate priors only. Each NPC's own experience adds learned values that can outweigh them, so two NPCs with the same genome and personality come to act differently. Nothing is a fixed utility formula or a behaviour tree. See [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md).
- **Personality changes scores, never rules.** An aggressive, bold NPC scores "fight for this" higher, but still backs off when the visible opponent is much stronger and its own fear is high.
- **Communication is a sim action.** NPCs decide whether, what and to whom to communicate, and how to respond. Unity animates the events and decides nothing. Headless works the same.
- **Determinism.** Integers only; NPCs in ascending id; one forked stream per NPC (`Rng.Fork("npc", id)`); fixed candidate order; ties go to the lowest index.

## 3. Data structures

```csharp
public struct NpcObservation {           // built by Perception; value types only
    public BodyView Self;                // own body and drives
    public ItemStack[] Carry;            // what it carries
    public SeenThing[] Things;           // up to N = 12: class, relative position, visible state
    public SeenNpc[] Npcs;               // up to M = 8: id (as recognised), faction colour, gear, action, gesture
    public DecodedMessage[] Messages;    // up to 4: addressed to it or broadcast nearby, as decoded
    public Posting[] Postings;           // read at a notice board: jobs, prices, contracts
}

public struct Belief {                   // Mind.Beliefs: 64 slots × 24 B = 1,536 B per NPC
    public BeliefKind Kind;              // Place, Posting, Price, Faction, Danger
    public int Subject, Value;
    public short Confidence;             // 0..1000
    public int Source;                   // seen, posted, or the NpcId that told it
    public int TickLearned;
}

public struct Message {                  // Comms
    public GestureAct Act; public MsgTopic Topic; public byte Quantity;
    public MsgTopic Counter; public int Target; public MsgTime Time;
}

public struct MsgTopic {                 // what a message is about
    public TopicKind Kind;               // None, Ware, Place, Npc, Issue (M10), Posting (M6)
    public int Id;                       // ware id, node or BuildingId, NpcId, IssueKind, posting id
    public int Sub;                      // Issue only: the issue's Subject (POLITICAL_ECONOMY_DESIGN.md); else 0
}

public struct Candidate {                // up to 16 per think
    public byte Verb; public int Target; // object class, NpcId, node or building
    public FeatureSet TargetFeatures;    // up to 6 FeatureIds (encoding in NPC_LEARNING_DESIGN.md)
}

public interface INpcPolicy {
    string Name { get; }
    // obs: what the NPC perceives now. mind: its own drives, beliefs, social memory and learned
    // associations (read-only MindView, defined in NPC_LEARNING_DESIGN.md section 3.6).
    // rng: the NPC's own stream. Returns one intent (possibly Idle). Must be deterministic.
    NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng);
}
```

The same pattern holds for soldiers (conventions, section 7): `IMilitaryPolicy` is `NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng)`.

`FeatureId`, `AssociationStore`, `InstinctTable` and `MindView` are defined in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#3-data-structures-assetsgodsimlearning).

## 4. Mechanics

### 4.1 The thinking loop (each NPC, on its think tick)
An NPC thinks every `ThinkEvery` ticks (default 10, about once a second at 1x), staggered by id ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)); level of detail can lengthen the interval for distant NPCs ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)). A soldier in contact thinks faster ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).

```text
1 Perceive  -> NpcObservation (what is in sight and hearing now)
2 Update beliefs (Mind.Beliefs) from the observation and from messages received
3 Attend    -> pick up to K salient things (objects, NPCs, messages)
4 Recognise -> the base context: drive bands, self, place, role, messages, situation features
5 Candidates-> up to 16 (verb, target), each with its target features and matching conjunctions
6 Decide    -> score = instinct + learned + social + culture + personality - cost + explore; winner takes all
7 Act       -> the intent is validated and applied by the sim (it may fail)
8 Learn     -> rewards since the last think credit recent decisions (Learning)
```

**Reflexes** run every tick and skip the loop. Examples: flinching from a blow, stopping at a blocked node, and continuing a walk or a job already chosen.

### 4.2 Perception (`Sim/Cognition/Perception`)
- **Sight:**
  - A radius in nodes, with a daylight and night factor.
  - Hills and forest hexes block line of sight beyond one hex. A cheap test is enough: no line through more than one forest or mountain node.
- **Hearing:**
  - A larger radius, but only for loud events: fights, alarms, building collapses.
  - Anything heard has its direction blurred.
- **What is seen:**
  - **Objects as classes** (*Creatures*): "berry bush, stock high", "log pile 4", "enemy soldier with spear". Not object ids.
  - **NPCs as individuals.** Unlike *Creatures*, NPCs recognise other NPCs. The game-theory examples need this (Grudger, Tit for Tat, vampire bats).
  - **Recognition is not perfect.** Recognising a stranger seen only once fails with a probability that falls as familiarity grows. A failure makes them look like a stranger, so cheats can sometimes hide.
- **Postings:** state jobs, prices and contracts are posted at HQs. Only NPCs who visit or are told know about them.

### 4.3 Attention
- **Salience:** each perceived item gets a salience made of:
  - novelty;
  - relevance to the strongest drive;
  - threat;
  - being addressed by name;
  - plus a **learned salience** from the NPC's own associations with the item's category ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#49-learned-values-and-attention)).

  The top K items (K = 3 by default) are attended.
- **Goals drive attention too.** Unlike *Creatures*, a goal in progress (such as "find food") adds salience to items of the goal's class, and the NPC can *search*: walk to remembered or likely places. This fixes the weakness the *Creatures* sources note.
- **The cost of attention is real.** An NPC may miss a gesture or a theft it didn't attend to, which is a source of misunderstanding and of undetected cheating. Outcomes it didn't attend to also teach it nothing.

### 4.4 Beliefs and knowledge (`Mind.Beliefs`)
- **Kinds of belief:**
  - places: food sources, piles, huts with room, workplaces, enemies seen;
  - postings: jobs, state prices, contracts;
  - prices: exchange ratios seen in recent trades;
  - other factions: how they treat citizens, from rumour;
  - danger: where it was attacked.
- **Limits:**
  - Capacity is fixed at about 64 facts. The entry with the lowest confidence × importance is forgotten first.
  - Confidence decays with age. An NPC that goes back and sees a change updates it.
- **Beliefs can be wrong.** A food source may have run out, and a liar may have described a place falsely. Acting on a false belief costs walking time, which is the price of being misled.
- **Beliefs are not habits.** A belief says what is true ("berries at node 412"). An association says what has worked ("foraging at node 412 when hungry pays"). Both decay, separately.

### 4.5 Situation features and the context
Each think computes a **base context**, a sorted set of `FeatureId`s (encoding in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#31-feature-ids)). It is built from:
- drive bands;
- self state (carrying food, has bed, owes someone);
- location type;
- role;
- received message acts;
- time of day;
- the hand-written **situation features**, for example:
  - hungry and food remembered;
  - a stranger is offering;
  - a known cheat is near;
  - I am owner here;
  - the opponent looks stronger;
  - the household has a surplus;
  - a friend is in need;
  - a job is posted with good pay;
  - an enemy is in sight;
  - outnumbered;
  - unwatched.

Each candidate adds its **target features**: the object's class, category and state, or the target NPC's relationship class, group and identity.

**Learned conjunctions** of 2–3 features (*Creatures* concept cells) are recruited per NPC from a bounded pool at sleep, from M3. They are owned by Learning.

### 4.6 Candidate intents
The decider builds up to 16 candidates from the attended items and beliefs. Each is a verb plus a target:
- eat this;
- forage at a remembered place;
- go home and sleep;
- take this job;
- offer X for Y to NPC n;
- answer this offer;
- give to n;
- ask n for help;
- fight for this;
- back off;
- flee;
- gesture "warning" at n;
- build the hut;
- migrate to faction f.

The verb list is the `VerbTable` in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#32-verbs).

### 4.7 The decider
The default policy is **`AssociativeNpcPolicy`**. For each candidate `c`, in build order:

```text
score(c) = Instinct(c) + Learned(c) + Social(c) + Culture(c) + Personality(c) - Cost(c) + Explore(c)
```

| Term | Meaning | Owner |
|---|---|---|
| `Instinct(c)` | Innate priors from the genome: "food relieves hunger", "flee enemies when afraid". The old utility estimate of expected drive relief lives here, as a starting bias only | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#34-instincts) |
| `Learned(c)` | The sum of the NPC's own association weights for the context's keys and this verb. It comes from its own outcomes, from outcomes it watched others get, and from habits it copied from a model | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#42-the-decision-score) |
| `Social(c)` | Trust in the partner, obligations owed, how much the partner's welfare counts (friend, kin) | [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md) |
| `Culture(c)` | Held cultural traits (norms, techniques, taboos); 0 before M11 | [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) |
| `Personality(c)` | Trait biases per verb | [POPULATION_DESIGN.md](POPULATION_DESIGN.md) |
| `Cost(c)` | Perceived walking time, visible risk × fear, goods given up | This document |
| `Explore(c)` | One seeded draw per candidate, larger with curiosity and boredom, smaller with fear, plus a bonus for untried actions | [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#42-the-decision-score) |

- **Choosing:** integers only. The winner takes all, and a tie goes to the lowest index. Exploration makes mixed strategies possible.
- **Instincts are only a starting point.** Learned weights (up to ±4000 vp per entry) can outweigh any instinct (±2000 at most). A newborn acts mostly on instinct. An adult acts mostly on what its life has taught it, and the share is measured (`LearnedShareDay`).
- **Learning modes:** with learning `Off`, the same policy runs on instincts, social, personality and cost alone. That is the baseline. `Frozen` uses the learned values but stops updating them.
- **Benchmarks:** benchmark policies such as `TitForTatNpcPolicy` act only in exchange and help situations, and fall back to `AssociativeNpcPolicy` otherwise.
- **Soldiers:** soldiers on duty use `IMilitaryPolicy` for combat decisions, which reads the same learned associations ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)).
- **Network policies:** a later `IntNetNpcPolicy` takes the same terms and the learned state as inputs. It never replaces lifetime learning.

### 4.8 Learning (summary)
Learning is owned by [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md). In short:
- **What an NPC associates:** its drives, perceived circumstances, recognised objects and categories, other individuals and factions, received messages, its actions, and their immediate and delayed outcomes.
- **Reward and punishment** come from drives falling or rising (the *Creatures* rule), plus goods gained or lost, injury, obligations kept or broken, and opportunities gained or lost.
- **Delayed credit:** eligibility traces credit recent decisions. Long delays (wages, loans, promises) are tracked as episodes that close on a later event.
- **Fast and slow weights** per entry. A sleep-time consolidation pass recruits conjunctions and forgets through decay, capacity eviction and recency.
- **Social learning** (owned by `Learning`, inside the learning store): observation at a lower gain (M7), and imitation, which copies selected association entries from a successful model into the learner's own store with origin tag `Imitated` (M11). Copied entries are not culture.
- **Cultural transmission** is separate (owned by `Culture`): named traits in `CultureStore` and their transmission ([CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md)). Held traits act only through the separate `Culture(c)` score term and never write association weights.
- **The state can't install habits directly.** It can only change consequences. Its laws and incentives change what gets rewarded, and so what is learned and spreads.

### 4.9 Gesture language (`Sim/Comms`)

#### Rules
- **Communication is a sim action.** A message is an intent (`Gesture`). It costs time (1–3 s), is visible only to those who perceive it, and becomes a `GestureMade` event.
- **The view only animates it.** The Unity view plays the matching animation from the event and decides nothing.
- **It works headless.** It runs the same with no graphics.
- **Saying is not doing.** "I will help later" creates no obligation in the rules. Only the listener's memory of the promise exists. The speaker can lie, change its mind or forget. A listener can ignore it, misread it, or not see it.

#### Grammar
A message is a small structure:

```text
Message { Act, Topic?, Quantity?, Counter?, Target?, Time? }
```

| Field | What it means |
|---|---|
| Act | The communicative act |
| Topic | A ware, a place (node or building), an NPC, an **issue** (an `Issue` from [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md), from M10) or a **posting** (a job, contract or purchase offer, from M6) |
| Quantity | 1–9, shown by raised fingers or by presenting the goods |
| Counter | The thing offered in exchange; for `Demand` (Stage 5), the threat |
| Target | Who it is aimed at: one NPC, the group, or anyone watching |
| Time | Now, soon or later (later for promises) |

Each field maps to a separate gesture element, so messages are compositional. The animation chains them: the act posture, then pointing at or presenting the topic, then the fingers, then the counter presented. Presenting a real object is stronger evidence than pointing at a remembered one.

#### Vocabulary (fixed and shared at first)

| Meaning | Act | Gestures and physical actions |
|---|---|---|
| I want this resource | `Want` | Reach towards the item or point at it, then palm to own chest |
| I offer this in exchange | `Offer` | Hold the counter-item out on open palms; with nothing in hand, point at own pile |
| I accept your offer | `Accept` | Nod, step forward, put out both hands |
| I refuse your offer | `Refuse` | Head shake, palm out, step back |
| I need food | `NeedFood` | Hand to mouth, then hand to belly, slumped posture |
| I need assistance | `NeedHelp` | Beckon with both arms, then point at the task (load, site, wound) |
| I will cooperate | `Cooperate` | Stand shoulder to shoulder, then mirror the other's pose |
| I promise to help later | `Promise` | Point at the other, hand on own chest, then sweep the arm towards the horizon ("later") |
| Follow me | `Follow` | Beckon, turn, walk a step, look back |
| I am warning you | `Warn` | Raise weapon or fist, stamp, stare, lean forward |
| I surrender | `Surrender` | Drop weapon, kneel, raise open hands |
| I will defend this resource | `Defend` | Stand over or before the resource, arms wide, weapon held across the body |
| You betrayed me | `Accuse` | Point hard at the other, then at the broken thing or empty hands, angry stance |
| I apologise | `Apologise` | Head bowed, hands open, offer a small gift if carrying one |
| I forgive you | `Forgive` | Open arms, then touch the other's shoulder |
| I would like to join your faction | `AskJoin` | Kneel to a faction HQ or official, present own goods, point at their banner |
| This person (or group) did well by me | `Praise` | Point at the subject (or the group's building), thumbs up, then hand on chest. Used for gossip ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)); Topic is the subject |

The acts arrive honest in M5; lies, promises, accusations and `Praise` (for gossip) in M7.

**Later acts** (owned by the documents named, same grammar, added to this table in their milestone):
- political acts from [POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md#38-new-gesture-acts): `Complain` and `Approve` (M10); `Rally`, `Pledge`, `Endorse`, `Demand`, `Withhold`, `Shun`, `Concede` and `Disperse` (Stage 5). They use the issue and posting topics;
- `WarningShot`, a combat intent rather than a gesture, owned by [MILITARY_DESIGN.md](MILITARY_DESIGN.md) (M9). Like a gesture it is a cheap signal: it shows capability without harm;
- candidates with no owner yet: `Thank`, `Greet`, `Threaten(if X)` (conditional threat), `Alarm` (danger there), `Point-out` (food is there) and `Ask` (where is X).

#### Deciding to communicate
Communicating is just another candidate intent, scored by the same decider. Each act is a verb (`SayWant`, `SayNeedFood`, ...), so an NPC **learns which messages work for it, with whom**: a `NeedFood` to a generous friend that brought food is reinforced, and an `Offer` to strangers that was always refused is weakened. Instinct priors and social memory give the starting point. Examples:
- A hungry NPC with a generous friend in sight scores `NeedFood` to that friend highly.
- A trader scores `Offer`.
- An owner who sees an intruder scores `Defend` or `Warn` (the resident/intruder convention).
- An NPC who failed to repay scores `Apologise` if its honesty is high and the creditor matters to it.

**Received messages are context too.** A decoded act becomes a `MsgAct` feature, so a listener learns from its own outcomes what a `Promise` from a stranger or a `Warn` from kin tends to mean.

#### Honesty and lies
- **Messages can be false:** "need food" when not hungry, a `Promise` not meant, a `Defend` bluff, an `Offer` of goods not owned.
- **The choice to lie** is in the score:
  - the instinct and learned value of the lying verb in this context (what lying has brought this NPC before);
  - minus guilt (the honesty trait);
  - minus the expected loss of reputation if caught (beliefs about who is watching).
- **Getting caught:** the listener checks what it can see. "Needs food" but carrying food is a visible contradiction, which lowers trust and can lead to an `Accuse`.
- **Promises:** a promise is recorded in the *listener's* memory as an expectation. When it falls due, failure becomes a betrayal record ([SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md)), and it closes the listener's learning episode with a punishment.

#### Misunderstanding
- **The chance:** the listener decodes each field with a chance of error. It rises with distance, darkness, low attention and low familiarity with the speaker's dialect, and falls when real goods are presented. The draw uses the listener's stream.
- **What an error looks like:** an act read as a similar act (`Offer` read as `Want`), a quantity off by one, a topic read as a neighbouring item.
- **What follows:** the listener acts on what it *believes* it saw, so a misreading can lead to a real dispute. How often that happens is measured (`MisreadDay`), not targeted.

#### Responding
- The listener adds the decoded message to its observation. Its own decider then scores the responses: accept, refuse, counter-offer, ignore, comply, flee, fight.
- Nothing forces a response, but ignoring someone has social effects: the speaker remembers a refusal or an insult.

#### Learned meanings and dialects (later)
- **Learned lexicons (`Comms`):** each NPC holds a lexicon mapping gesture elements to meanings, with weights. At first everyone shares one lexicon.
- **Dialects (`Culture`):**
  - A faction can have a *dialect*: small changes in which gesture stands for which act.
  - Children learn it from their household, and a migrant brings theirs.
  - Mismatched dialects raise misunderstanding between factions and over time.
  - [CULTURAL_TRANSMISSION_DESIGN.md](CULTURAL_TRANSMISSION_DESIGN.md) owns how dialects spread.
- **Emergent signals (research option):** lexicon weights are updated when a message leads to the outcome the speaker wanted. That allows conventions to emerge (Lewis signalling games).

## 5. Interactions with other systems

| System | What flows |
|---|---|
| `Population` | Drives, body, traits and life stage feed the observation, the context and the score |
| `Learning` | Context encoding, `Instinct(c)`, `Learned(c)`, `Explore(c)`, learned salience; Cognition calls `OnChosen` after the decision |
| `Social` | Relationship classes for features; trust and obligations for `Social(c)`; promises and accusations recorded |
| `Culture` | Dialects (decoding errors); held traits as the `Culture(c)` term. Imitation of associations (origin `Imitated`) is Learning's, not Culture's |
| `Economy`, `State` | Ware classes and categories; postings at notice boards; exchanges as separate hand-overs |
| `Military` | `IMilitaryPolicy` for combat; soldiers keep the same mind |
| `Information` | Layers 2, 3 and 6: observations, messages and beliefs ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)) |
| `Policy` | `INpcPolicy`, `MindView`, the registry; the reflection test |

## 6. Player and view
- **Animations:**
  - each gesture element as a short procedural animation of the NPC model: pose, arm, head and facing;
  - presenting goods (the item mesh in hand);
  - pointing (a line of arm and gaze);
  - a small floating icon for the topic ware, as an option for players. The icon is decoration only and can be turned off; the sim never needs it.
- **The "interpret" overlay** is an optional accessibility and debugging aid, off by default. It shows the NPC's **intended** message and the listener's **decoded** message, with a mismatch marker.
- **The NPC inspector** shows the NPC's own view: what it attends to, its top beliefs, and its last decision with the term breakdown. The "Habits" panel is in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#6-player-and-view).

## 7. Telemetry
- **`GestureMade`:** tick, speaker, act, topic (kind, id, sub), quantity, counter, target, and an `Honest` truth flag.
- **`MessageDecoded`:** tick, listener, speaker, the decoded fields, and a `Mismatch` truth flag.
- **Truth flags are analytics-only.** `Honest` and `Mismatch` describe the true state. No policy, perception or mind code reads them, and recorders store them only at the Full recording level ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)).
- **`DecisionMade`** (sampled): owned by [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#7-telemetry).
- **Daily person snapshot:**
  - `BeliefCount`;
  - `MessagesSentDay`, `MessagesReceivedDay`;
  - `LiesToldDay`;
  - `MisreadDay`.

## 8. Stage 4 minimum vs later stages

| Milestone | Delivers |
|---|---|
| M1 | `INpcPolicy`, `MindView` (skeleton), the reflection test |
| **M3** | Perception by radius; goal-driven attention to the top K; the belief store; hand-written situation features and the context; `AssociativeNpcPolicy` with instincts and the learning core ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)) |
| **M5** | The gesture vocabulary above, honest only, with no misunderstanding; `MsgAct` features; `Say*` verbs learned from responses; the view's animations |
| M6 | Posting topics in messages |
| **M7** | Lies, promises and accusations; `Praise` for gossip; misunderstanding; learning from observation |
| M9 | `IMilitaryPolicy` on the same mind; `WarningShot` ([MILITARY_DESIGN.md](MILITARY_DESIGN.md)) |
| M10 | Issue topics; `Complain` and `Approve` ([POLITICAL_ECONOMY_DESIGN.md](POLITICAL_ECONOMY_DESIGN.md)) |
| **M11** | Dialects and the `Culture(c)` term (`Culture`); imitation of associations, origin `Imitated` (`Learning`) |
| M15 | `IntNetNpcPolicy` reading the observation and the learned state |
| Later | Emergent signalling, learned lexicons, trained networks ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) |

## 9. Migration from the existing code
- The stage 3 code has no NPCs, so nothing is removed.
- The earlier draft's hand-made utility score with a small "learned bias" is replaced by `AssociativeNpcPolicy`:
  - the expected-relief estimate became instinct priors;
  - costs stayed;
  - learning became a full term owned by `Learning`.

## 10. Tests and acceptance criteria
- **No true state:** `Policies_DoNotReferenceGodState` (reflection) passes for every type in `Sim/Policy` and `Sim/Cognition` policy code (M1).
- **Perception only:** an NPC never chooses an intent whose target it has not perceived, remembered or been told about. This is checked on every decision in a 20-NPC, 10-day run (M3).
- **Determinism:** the same seed gives the same decision sequence in 10 seeds, and recording on or off gives the same hash (M3).
- **Instincts plus learning:** the learning tests L1, L7 and L8 in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#10-tests-and-acceptance-criteria) also cover the decider (M3).
- **Honest gestures** (M5):
  - every `GestureMade` has a `MessageDecoded` for each listener who attended it;
  - with misunderstanding off, the decoded fields equal the sent fields in 100% of cases.
- **Communication learning** (M5): an NPC whose `NeedFood` to a friend is always answered with food uses it ≥ 2× more often by day 5 than an NPC whose friend always ignores it, in ≥ 90% of 50 seeds.
- **Misunderstanding rates** (M7): in a calibration scenario, the decode error rate is within ±2 percentage points of the configured rate at each distance band, over 10,000 messages.
- **Lies get caught** (M7): a `NeedFood` from a speaker visibly carrying food lowers the listener's trust in ≥ 95% of cases where the listener attended the speaker.

## 11. Open questions
None for the owner. Settled here:
- **A failed recognition hides both records.** The context uses the *recognised* id, so a failed recognition emits no `NpcRef` key and finds no social-memory record. The cheat's learned entries and its trust record are both hidden.
- **No inference of others' drives in stage 4.** Observers use only what they see: gestures, carried goods, actions. A visible `NeedFood` gesture is already a `MsgAct` feature. Revisit only if test L15 in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#10-tests-and-acceptance-criteria) needs it.

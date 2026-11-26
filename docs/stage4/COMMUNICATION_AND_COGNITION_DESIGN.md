# Communication and cognition design (stage 4)

This document covers how an NPC perceives, attends, knows, decides, learns and communicates. Body and drives are in [POPULATION_DESIGN.md](POPULATION_DESIGN.md), and memories of other NPCs are in [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md). The background research is in [research/CREATURES_NOTES.md](research/CREATURES_NOTES.md).

## The rule above all: no NPC reads the true game state
- **What a policy gets:** an NPC policy receives only an `NpcObservation`, built by the perception system, plus a read-only view of that NPC's own mind (beliefs, social memory, learned weights).
- **What it returns:** an `NpcIntent`.
- **How this is enforced:**
  - `INpcPolicy` lives in `Sim/Policy` and has no access to `GodState`. Observations are plain value types: ids, classes, counts and relative positions.
  - The test `Policies_DoNotReferenceGodState` checks by reflection that no type in the policy namespaces has a field, parameter or local of type `GodState`, `EconomyState` or similar.
  - Scripted and trained policies use the same observation and intent types, so neither can cheat.

## The thinking loop (each NPC, on its think tick)

```text
1 Perceive  -> NpcObservation (what is in sight and hearing now)
2 Update beliefs (Mind.Beliefs) from the observation and from messages received
3 Attend    -> pick up to K salient things (objects, NPCs, messages)
4 Recognise situations -> active situation features (concepts)
5 Decide    -> score candidate intents, pick one (winner takes all, ties by lowest index)
6 Act       -> the intent is validated and applied by the sim (may fail)
7 Learn     -> later drive changes reward or punish recent (situation, intent) pairs
```

Reflexes run every tick and skip the loop. Examples: flinching from a blow, stopping at a blocked node, and continuing a walk or a job already chosen.

## Perception (`Sim/Cognition/Perception`)
- **Sight:**
  - A radius in nodes, with a daylight and night factor.
  - Hills and forest hexes block line of sight beyond one hex. A cheap test is enough: no line through more than one forest or mountain node.
- **Hearing:**
  - A larger radius, but only for loud events: fights, alarms, building collapses.
  - Anything heard has its direction blurred.
- **What is seen:**
  - **Objects as classes** (*Creatures*): "berry bush, stock high", "log pile 4", "enemy soldier with spear". Not object ids.
  - **NPCs as individuals.** Unlike *Creatures*, NPCs recognise other NPCs. The game-theory examples need this (Grudger, Tit for Tat, vampire bats).
  - Recognition is not perfect: recognising a stranger seen only once fails with a probability that falls as familiarity grows. A failure makes them look like a stranger, so cheats can sometimes hide.
- **What the observation holds:**
  - the NPC's own body and drives;
  - what it carries;
  - up to N visible things (classes, positions relative to the NPC, visible state);
  - up to M visible NPCs (id, faction colour, visible gear, visible action, visible gesture);
  - messages addressed to it or broadcast nearby;
  - postings it can read at a notice board: state jobs, prices and contracts are posted at HQs and are known only to NPCs who visit or are told.

## Attention
- **Choosing what to attend to:** each perceived item gets a salience: novelty, plus how relevant it is to the NPC's strongest drive, plus threat, plus being addressed by name. The top K items (K = 3 by default) are attended.
- **Goals drive attention too.** Unlike *Creatures*, a goal in progress (such as "find food") adds salience to items of the goal's class, and the NPC can *search*: walk to remembered or likely places. This fixes the weakness the *Creatures* sources note.
- **The cost of attention is real.** An NPC may miss a gesture or a theft it didn't attend to. That is a source of misunderstanding and of undetected cheating.

## Beliefs and knowledge (`Mind.Beliefs`)
- **What a belief is:** a small table of facts, each with a value, a confidence (0–1000), a source (seen, told by an NPC id, posted) and the tick learned.
- **Kinds of belief:**
  - places: food sources, piles, huts with room, workplaces, enemies seen;
  - postings: jobs, state prices, contracts;
  - prices: exchange ratios seen in recent trades;
  - other factions: how they treat citizens, from rumour;
  - danger: where it was attacked.
- **Limits:**
  - Capacity is fixed, around 64 facts. The weakest entry is forgotten first: lowest confidence × importance.
  - Confidence decays with age, and an NPC that goes back and sees a change updates it.
- **Beliefs can be wrong.** A food source may have run out, and a liar may have described a place falsely. Acting on a false belief costs walking time, which is the price of being misled.

## Situations and decisions

### Situation features
Every think tick computes a fixed list of yes/no features from attention, beliefs, drives and social memory. For example:
- hungry and food in sight;
- hungry and food remembered;
- a stranger is offering;
- a known cheat is near;
- I am owner here;
- the opponent looks stronger;
- the household has a surplus;
- a friend is in need;
- a job is posted with good pay;
- an enemy is in sight;
- outnumbered.

Like *Creatures* concepts, but hand-written at first. **Later** (M10+), sparse AND-concepts of 2–3 features are taken from a pool and freed when unused, as in *Creatures*.

### Candidate intents
The decider builds up to about 16 candidates from the attended items. Each one is a verb plus an object:
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

### Scoring (the scripted `UtilityNpcPolicy`)
`score = expected drive relief over the planning horizon × personality biases + social terms − cost (walking time, risk × fear, loss of goods) + learned bias(situation, intent)`.

- **Integers only.** The horizon depends on patience.
- **Social terms come from social memory:** trust in the partner, obligations owed, how much the partner's welfare counts (friend, kin).
- **The learned bias** comes from experience (below).
- **Choosing:** a small random term (seeded) breaks ties and makes mixed strategies possible. The winner takes all.

**Personality changes scores, never rules.** An aggressive, bold NPC scores "fight for this" higher, but still backs off when the visible opponent is much stronger and its own fear is high.

## Learning

### From consequences (borrowed from *Creatures*)
- **The memory:** each NPC keeps a small table of learned biases keyed by (situation feature, intent verb). Up to a few hundred entries, with an integer short-term weight and long-term weight each.
- **Susceptibility:**
  - When an intent is chosen, the active features get a *susceptibility* for that verb.
  - Susceptibility decays each tick: `s -= s >> 4`.
- **Reward and punishment:**
  - Reward is any later fall in drives, plus gains in goods and status. Punishment is any rise in drives, plus injury, plus losses.
  - Both are summed over the next few think ticks.
  - The total updates the short-term weight, in proportion to susceptibility.
- **Two timescales:** the long-term weight moves slowly towards the short-term one (`ltw += (stw - ltw) >> 6`), and the short-term weight relaxes back towards the long-term one.
- **The result:** an NPC cheated once by a stranger becomes wary at once, but its long-term view is gentler.
- **When it runs:** learning runs at sleep (a replay of the day's pairs) and lightly when awake. It is deterministic and in a fixed order.

### From observation and imitation
- **Watching others:** an NPC that sees another do something (trade, fight, give) and sees how it turned out ("got food", "was beaten") records a *vicarious* reward on the same table, at a lower gain.
- **Copying successful neighbours:**
  - Every few days, an NPC with high conformity compares itself with a better-off neighbour it knows: more goods, better hut, higher status.
  - It copies some of that neighbour's learned biases, with noise.
  - These are cultural memes: habits spread without genes. This is the transmission channel in [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md).
- **The state can't install habits directly.** It can only change consequences. Its laws and incentives change what gets rewarded, and so what spreads.

## Gesture language

### Rules
- **Communication is a sim action.** A message is an intent (`Gesture`). It costs time (1–3 s), is visible only to those who perceive it, and becomes a `GestureMade` event.
- **The view only animates it.** The Unity view plays the matching animation from the event and decides nothing.
- **It works headless.** It runs the same with no graphics.
- **Saying is not doing.** "I will help later" creates no obligation in the rules. Only the listener's memory of the promise exists. The speaker can lie, change its mind or forget. A listener can ignore it, misread it, or not see it.

### Grammar
A message is a small structure:

```text
Message { Act, Topic?, Quantity?, Counter?, Target?, Time? }
```

| Field | What it means |
|---|---|
| Act | The communicative act |
| Topic | A ware, a place (node or building), or an NPC |
| Quantity | 1–9, shown by raised fingers or by presenting the goods |
| Counter | The thing offered in exchange |
| Target | Who it is aimed at: one NPC, the group, or anyone watching |
| Time | Now, soon or later (later for promises) |

Each field maps to a separate gesture element, so messages are compositional. The animation chains them: the act posture, then pointing at or presenting the topic, then the fingers, then the counter presented. Presenting a real object is stronger evidence than pointing at a remembered one.

### Vocabulary (fixed and shared at first)

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

Further acts can be added in the same table: `Thank`, `Greet`, `Threaten(if X)` (conditional threat), `Alarm` (danger there), `Point-out` (food is there) and `Ask` (where is X).

### Deciding to communicate
Communicating is just another candidate intent, scored by the same decider. The expected effect on the listener comes from social memory: does this NPC usually respond to a request? Examples:
- A hungry NPC with a generous friend in sight scores `NeedFood` to that friend highly.
- A trader scores `Offer`.
- An owner who sees an intruder scores `Defend` or `Warn` (the resident/intruder convention).
- An NPC who failed to repay scores `Apologise` if its honesty is high and the creditor matters to it.

### Honesty and lies
- **Messages can be false:** "need food" when not hungry, a `Promise` not meant, a `Defend` bluff, an `Offer` of goods not owned.
- **The choice to lie** is in the score: expected gain, minus guilt (honesty trait), minus the expected loss of reputation if caught (beliefs about who is watching).
- **Getting caught:** the listener checks what it can see. "Needs food" but carrying food is a visible contradiction, which lowers trust and can lead to an `Accuse`.
- **Promises:** a promise is recorded in the *listener's* memory as an expectation. When it falls due, failure becomes a betrayal record. See [SOCIAL_MEMORY_DESIGN.md](SOCIAL_MEMORY_DESIGN.md#obligations-and-promises).

### Misunderstanding
- **The chance:** the listener decodes each field with a chance of error. It rises with distance, darkness, low attention and low familiarity with the speaker's dialect (below), and falls when real goods are presented.
- **What an error looks like:** an act read as a similar act (`Offer` read as `Want`), a quantity off by one, a topic read as a neighbouring item.
- **What follows:** the listener acts on what it *believes* it saw. Mistakes cause real disputes, and that is wanted.

### Responding
- The listener adds the decoded message to its observation. Its own decider then scores the responses: accept, refuse, counter-offer, ignore, comply, flee, fight.
- Nothing forces a response, but ignoring someone has social effects: the speaker remembers a refusal or an insult.

### Learned meanings and cultures (later)
- **Learned lexicons:** each NPC holds a lexicon mapping gesture elements to meanings, with weights. At first everyone shares one lexicon.
- **Culture by faction:**
  - A faction can have a *dialect*: small changes in which gesture stands for which act.
  - Children learn it from their household, and a migrant brings theirs.
  - Mismatched dialects raise misunderstanding between factions and over time.
- **Emergent signals (research option):** learned weights are updated when a message leads to the outcome the speaker wanted. That allows conventions to emerge (Lewis signalling games), and fits the meme layer.

### What the view must show
- **Animations:**
  - each gesture element as a short procedural animation of the NPC model: pose, arm, head and facing;
  - presenting goods (the item mesh in hand);
  - pointing (a line of arm and gaze);
  - a small floating icon for the topic ware, as an option for players. The icon is decoration only and can be turned off; the sim never needs it.
- **Accessibility:** an optional "interpret" overlay for the player shows the NPC's **intended** message and the listener's **decoded** message, with a mismatch marker. It is a debugging aid, off by default.

## Policy interface

```csharp
public interface INpcPolicy
{
    string Name { get; }
    // obs: what the NPC perceives now. mind: its own beliefs, memory and weights (read-only).
    // rng: the NPC's own stream. Returns one intent (possibly Idle). Must be deterministic.
    NpcIntent Decide(in NpcObservation obs, NpcMindView mind, ref Rng rng);
}
```

Other policies use the same interface:
- `UtilityNpcPolicy`, the scripted default;
- benchmark policies such as `TitForTatNpcPolicy`, which act only in exchange and help situations and fall back to the utility policy otherwise;
- a later `IntNetNpcPolicy`.

Soldiers on duty use `IMilitaryPolicy` for combat decisions; see [MILITARY_DESIGN.md](MILITARY_DESIGN.md).

## Minimum viable version vs later
- **Minimum viable (M3–M4):**
  - perception by radius;
  - attention to the top K items, goal-driven;
  - a belief store;
  - hand-written situation features;
  - the scripted utility policy;
  - reward from drive changes, with short-term and long-term weights;
  - the gesture vocabulary as above, honest only, no misunderstanding;
  - the view's animations.
- **M6:**
  - lies, promises and accusations;
  - misunderstanding;
  - learning from observation.
- **M10:**
  - imitation and memes;
  - dialects;
  - concepts taken from a pool.
- **Research later:** emergent signalling, learned lexicons, trained networks (see [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).

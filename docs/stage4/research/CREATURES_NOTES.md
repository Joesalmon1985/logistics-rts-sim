# Research notes: the AI of *Creatures* (Steve Grand, Cyberlife, 1996)

These notes were made on 26 Nov 2026 from the sources below. Everything is paraphrased. [COMMUNICATION_AND_COGNITION_DESIGN.md](../COMMUNICATION_AND_COGNITION_DESIGN.md) explains how we use it.

## Sources

| Key | Source |
|---|---|
| **[A]** | Alan Zucconi, "The AI of Creatures" (2020), <https://www.alanzucconi.com/2020/07/27/the-ai-of-creatures/>. Secondary source. |
| **[P]** | Grand, Cliff & Malhotra, "Creatures: Artificial Life Autonomous Software Agents for Home Entertainment", Agents '97. Primary source, read from an archive mirror: <https://eem.foo/ccarchive/Sites/fp.cyberlifersrch.plus.com/articles/agents_%2797_paper.htm> |
| **[D]** | Creatures Developer Resource, community reverse-engineering of Creatures 1, with some Creatures 2: <https://double.nz/creatures/brainlobes/lobes.htm>, plus its perception, SVRule and emitter pages. Treat its numbers as observed, not official. |

**Not read:**
- [D]'s concept-lobe page (unfinished);
- its decision-learning page, receptor page and stimulus page;
- the Creatures 2 differences page;
- Grand's 1997 PDF.

The sources disagree on brain size: 952 neurones in [A], about 1,000 in [P]. Both say about 5,000 connections.

## Architecture
- **The brain is split into lobes.** Each lobe has its own neurone settings, and draws from at most two source lobes, through two classes of dendrite. [P][A][D]
- **Lobes in Creatures 1:**
  0. perception
  1. drive
  2. stimulus source
  3. verb
  4. noun
  5. general sense
  6. decision
  7. attention
  8. concept
  9. regulator

  Lobes 0–8 are fixed in the program; lobe 9 is built from the genome. [D]
- **Cost:** about 1,000 neurones and 5,000 synapses per creature. Ten creatures (20,000 neurones and 100,000 synapses a second) were the practical limit on a 486 PC. [P]
- **Biochemistry** is a separate layer that feeds the brain. [P]

## Neurones and dendrites
- **A neurone** has:
  - inputs;
  - an internal state, updated by a rule;
  - a threshold: its output is the state above the threshold, else 0;
  - a resting state it relaxes back towards, faster the further away it is. This damps it.
  - an input gain. [P]
- **A dendrite** has:
  - a short-term weight (STW), which multiplies the input;
  - a long-term weight (LTW), the resting value of the STW;
  - rates at which each relaxes, slower for the LTW;
  - susceptibility, with a half-life;
  - a strength, which drives migration;
  - rules for reinforcement, susceptibility, and gaining or losing strength. [P]
- **Observed STW update:** STW = LTW + (susceptibility / 255) × reinforcement, with decay applied. This is unverified. [D]
- **The rules are a small interpreted language ("SVRules")** with operations such as:
  - add, subtract;
  - "all type-0 inputs firing";
  - multiply by a fraction of 256;
  - step up, step down;
  - read a chemical;
  - move towards a target. [P][D]

  It cannot fail: any mutation still gives a valid rule.

## Biochemistry
- **Chemicals** are numbered 0–255, each with a concentration. A chemical's effects come only from the reactions its genes define. [P]
- **Emitters** watch a byte, such as an organ's state, and release a chemical. [P][D]
  - Analogue form: (signal − threshold) × gain / 255.
  - Digital form: a fixed amount when the signal passes the threshold.
  - Each has a sampling rate.
- **Receptors** watch a chemical and write a byte. [P]
- **Reactions** take the form iA + jB → kC + lD, at a rate that depends on concentration. Fusion, decay and catalysis are allowed; nothing may be made from nothing. [P]
- **Metabolism:** starch → glucose ↔ glycogen; glucose → energy. Toxins and a simple immune system are modelled too. [P]

## Drives
- **The drive lobe** has 16 cells. Creatures 1 uses 13: [A][D]
  - pain
  - need for pleasure
  - hunger
  - cold
  - heat
  - tiredness
  - sleepiness
  - loneliness
  - crowdedness
  - fear
  - boredom
  - anger
  - sex drive

  Creatures 2 adds injury, suffocation, thirst and stress.
- **Each drive is a chemical level.** Things in the world raise or lower drives through chemicals. A shower, for example, lowers heat and tiredness. [P]
- **Reward and punishment come from changes in drives, not from events directly.** [P]
  - Raising a drive releases a punishment chemical.
  - Lowering a drive that is present releases a reward chemical.
  - Lowering a drive that isn't present does nothing, so eating when full isn't rewarded.
- **Drives can't be set directly by script.** Their receptors overwrite them every tick. [D]

## Perception and object classes
- **The senses are semi-symbolic.** If an object of a class is in view (the eyes must point at it), that class's neurone fires. Sound gets quieter with distance and is muffled by objects in the way. [P]
- **The stimulus-source lobe** has 40 cells, one per object class. The most stimulating object in view wins. [D][A]
  - About 26 classes are used: food, drink, toy, another creature, the hand, a lift, and so on.
- **A creature perceives an object's class, not the individual object.**
- **The general-sense lobe** has 32 cells: [A][D]
  - patted, slapped, hit a wall;
  - the user spoke, a creature spoke, its own kind spoke;
  - something approaching or leaving;
  - is a sibling, parent or child;
  - is of the opposite sex.
- **The perception lobe** is a summary that copies the drive, verb, general-sense and attention lobes: 104 of its 112 cells. A concept can then combine four sources, despite the two-source limit. Drive and verb are copied so that a concept holds at most one drive. [D][A]

## Attention
- **The attention lobe** has 40 cells, winner-takes-all. Each class adds the stimulus-source cell and the noun cell for that class. The winner is what the creature looks at. [D]
- **It isn't learned.** A creature can't choose to look for food when hungry unless food is already the most stimulating thing in view. [D] This is a known weakness; our design avoids it.
- **The decision is a verb plus the object attended to.** There is no subject. [P]

## Concept space
- **Concept cells are AND-detectors** with 1–4 perception inputs. A cell fires only when all its inputs fire. [P][A]
- **Cells take on new patterns as they happen**, and stay committed until every dendrite's strength falls to zero. A feedback loop keeps a pool of free cells. [P]
- **Partial matches generalise.** A new situation ABCD can recall memories of ABD or D. [P]

## Dendrite migration
- **Initial wiring:** genes wire neighbouring cells spatially. [P]
- **Migration after birth:**
  - Strength rises with reinforcement and falls otherwise.
  - At zero, a dendrite lets go and looks for a new source cell. [P][A]
- **A dropped feature:** a version where dendrites competed between cells was built, but switched off because it caused problems. [P]

## Decision
- **One cell per action.** [P] says 16; [A] lists 11:
  - quiet
  - push / activate
  - pull
  - stop
  - come
  - run
  - get
  - drop
  - think / say
  - sleep
  - left / right

  Creatures 2 adds eat and hit.
- **Each cell adds its exciting inputs and subtracts its inhibiting ones**, relaxing slowly so it builds up evidence. The winner takes all, and its action script runs when the winner changes, about once a second. [P][A]
- **The action is the verb applied to the attended class.** Push on food means eat. [A]

## Learning
- **Reinforcement:** a reward strengthens the exciting links into the chosen action; a punishment strengthens the inhibiting ones. [P]
- **Delayed credit:** a link's susceptibility rises when it helps fire a cell, then fades. A later reward still credits recent pairings of situation and action. [P]
- **Two timescales:** the short-term weight reacts to single events, and the long-term weight is its moving average. The creature avoids a bad action at once, but its long-term view is milder. [P]
- **Forgetting:** links lose strength when they aren't reinforced, and drop at zero. [P]
- **The result:** creatures learn simple things, such as eating when hungry but not when full. [P] calls the model "not an ambitious one".

## Language
- **Verb-object language.** Creatures learn from the keyboard, from a teaching machine, or from other creatures. [P]
  - Typing a learned word fires the matching verb cell or noun cell.
  - "Look" over an object fires its noun cell. [D][A]
- **Words steer only attention and the verb.** The creature still decides what to do. [A]
- **Speaking** is a decision cell ("think/say"). [A][D]

## Instincts
- **Creatures 1 has 19 instincts** (Creatures 2 has 44): [A]
  - rewards for doing the action the user asked for;
  - courtship;
  - eating when hungry;
  - sleeping when tired;
  - playing when bored;
  - avoiding crowds.
- **They are applied during sleep, as replayed rewards and punishments.** [A only; [P] doesn't describe this]

## Genetics
- **The genome** is a string of bytes divided into genes. Any byte can mutate and nothing crashes. [P]
- **Inheritance:**
  - one haploid chromosome;
  - crossover at gene boundaries;
  - occasional genes dropped or duplicated;
  - point mutations. [P]
- **Gene contents:**
  - Each gene says which operations are allowed on it, and when it switches on (for example at puberty).
  - Genes encode structure (lobes, receptors, reactions), not traits.
  - There are about 320 genes. [P]

## Social behaviour and imitation
- **[P] saw occasional social-looking behaviour:**
  - creatures playing ball together;
  - "chase" scenes from "unrequited love".

  The authors warn this may be people reading too much into it.
- **[P] speculates about culture and dialects spreading** as creatures were emailed between computers. It is speculation, not a measured result.
- **Creatures learn from each other only indirectly**, through general-sense cells (another creature spoke, kinship) and reinforcement. None of the sources describes imitation as a mechanism.

## Limits and lessons
- **Scale:** all combinations of up to four of ~128 inputs is far too many. Creatures keeps about 5,000 useful links out of a possible million. [P][A]
- **What worked:** stable, varied behaviour; real learning; no collapse into loops; a workable genome of about 320 genes. [P]
- **What didn't:**
  - the model is not ambitious [P];
  - attention can't learn [D];
  - there's no "subject verb object" [P];
  - the competition between dendrites was switched off [P];
  - early bugs [D].

## What to borrow for a deterministic multi-agent sim
1. **Integer chemicals** (0–255) with reactions stored as data, and integer reaction rates each tick, so there's no floating-point drift.
2. **Drives as chemicals.** Reward and punishment come from drives falling or rising, not from raw events, so eating when full isn't rewarded.
3. **Two timescales per link:** a fast short-term weight and a slow long-term weight, updated with integer shifts (STW += (LTW − STW) >> 2).
4. **Susceptibility per link**, so a delayed reward still credits the recent pairing of situation and action.
5. **Sparse AND-detector concept cells** (1–3 inputs), taken from a pool and freed when their strength reaches zero, always updated in a fixed order.
6. **Winner-takes-all decisions** over action cells (exciting minus inhibiting inputs), with ties broken by lowest index.
7. **Perceive classes, not individuals**, for objects. One salience cell per class, winner-takes-all. (We extend this: NPCs also recognise individual NPCs, which the game-theory examples need.)
8. **Limit concepts to one drive and one verb each**, to shrink the space of patterns.
9. **Do learning in batches** during an offline or sleep phase per agent, at a fixed tick. It's cheaper and reproducible.
10. **A byte-string genome read by a fail-safe interpreter**, so any mutation gives a valid brain. Each agent's random-number stream is seeded from the genome and the tick.
11. **Budget compute by sampling.** Sensors and emitters have sampling rates, NPCs far from the action update less often, and decisions happen about once a second.
12. **Don't make attention purely reactive.** The decision layer must be able to choose a target to look for, or agents ignore needs they can't currently see.

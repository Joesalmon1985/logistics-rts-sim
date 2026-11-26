# Possession and avatar design (owner decisions D5, 26 Nov 2026)

Two ways for the player to act inside the world with a body: **possession** (Mode C) borrows an ordinary person's body for a while; the **avatar** (Mode B) is the god's own distinct body. Both use the existing movement, reach and goods actions; neither has a second implementation of any ordinary action. This document follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) and the owner decision register [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md); the owner's words are in [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §6 (Modes B and C). **The control seam (`ControlEntry`, precedence, end reasons, `DivineControlInput`, the order within a tick) and the avatar body (`BodyKind.Avatar`) are frozen by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("SAC") §4.2 and §11; this document owns the behaviour of D4 (possession) and D5 (avatar) on top of them.**

Written against `unity/poc` at `8617eaa`.

---

## 1. Purpose and scope

**This document owns:**
- possession: the swap of control authority, deterministic input to intents each tick, body continuity (needs, injury, death ends it), consciousness and memory, the `DivinelyControlled` record and how learning treats it (configurable credit), Mythic upkeep, ending, one at a time, foreign citizens and animals later;
- the avatar: a distinct body with the god's identity, existing movement, reach and goods actions, no hunger or ageing, damage and destruction returning the god to divine existence, upkeep, gestures and demonstrations that NPC learning and imitation can observe, and the hand-off to the third-person camera.

**It links to, and does not repeat:**
- gods, commands, modes, divine learning and the causal record: [DIVINE_PLAYER_DESIGN.md](DIVINE_PLAYER_DESIGN.md);
- costs and upkeep values: [MYTHIC_AND_MIRACLES_DESIGN.md](MYTHIC_AND_MIRACLES_DESIGN.md) §4.5;
- physical actions, reach and goods: [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md); the decider, perception and gestures: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md); the learner: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- cameras and the mapping from stick and click to `DivineControlInput`: [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md); on-screen controls: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md); the look of possession and manifestation: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md); body animation: [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md).

---

## 2. Owner decisions and principles

**Owner decisions:** D5.8 (possession permitted; one at a time; body, identity, memory and learning continue; actions recorded as divinely controlled), D5.9 (avatars permitted; distinct identity; existing movement and interaction; no hunger or ageing; can be attacked), D5.10 (possession consumes Mythic continuously), D5.14 (avatar destruction does not destroy the god).

**Principles:**
1. **One implementation of every action.** A possessed person and an avatar act through candidates that go `Match` → `Start` → `Act`, with the same `WalkTo`, `NodePather`, `Reach` (`Sim/Population/Reach.cs`) and `Goods` API as everyone (SAC §6, §11.3).
2. **Inputs are commands.** Every player input in these modes is a `DivineControlInput` in the pending queue, applied at a tick boundary, logged and replayed. The camera, frame rate and time scale never reach the sim (SAC §11.3).
3. **The body is real.** A possessed body keeps every physical process. An avatar has a physical presence others can perceive, approach, give to, take from and attack.
4. **Same world, same person.** Possession never copies a person; the possessed person is the same `Npc` record and id throughout.

---

## 3. Data structures

Frozen by SAC: `ControlKind { Self, Compelled, Possessed, AvatarBody }`, `ControlEntry`, `ControlTable` (`DivinityState.Control`), `Control.Of/TryAcquire/Release/Sweep`, `ControlEndReason` (`Completed, Timeout, Cancelled, Death, MythicExhausted, Superseded, Destroyed`), `DivineControlInput { God, Body, Kind (Heading, MoveTo, Stop, Action), Heading, Gait, Node, Act, Target }`, `BodyKind { Person, Avatar }`, `Npc.Kind`, `Npc.God`, `Bodies`, `SeenPerson.Body`, `AvatarBodyId = 0x4000_0000 + GodId`, `MoveReason.AvatarDrop = 11`.

**D4's record** (`DivinityState`, partial `D4`):

```csharp
public struct PossessionSpell {             // one per possession, keyed by its InterventionId; at most Divinity.Possession.MaxPerGod (1) per god
    public int  Intervention, God, Npc;
    public long StartTick, EndTick;
    public ControlEndReason EndReason;
    public int  InputsApplied, InputsRefused, ActionsStarted;
}
```

**D5's record** (`DivinityState.Avatars`, partial `D5`):

```csharp
public struct AvatarSpell {                 // one per manifestation (warehouse fact_avatar_manifestation), keyed by InterventionId
    public int  Intervention, God, Body;    // Body = AvatarBodyId of the god (one body record per god for the whole game)
    public long StartTick, EndTick;
    public ControlEndReason EndReason;      // Cancelled (DismissAvatar), MythicExhausted, Destroyed, Superseded
    public int  DamageTaken, ActionsStarted, Observers;   // Observers: distinct people who attended it (demonstration reach)
}
```

The avatar body itself is an `Npc` record with `Kind = Avatar` (SAC §4.2), so no new holder or movement code exists.

---

## 4. Possession (Mode C)

### 4.1 Beginning and the authority swap

- **`BeginPossession { God, Npc }`.** Preconditions: a living person (`Bodies.IsLivingPerson`) of any faction (foreign citizens allowed, SAC §11.1); not possessed by another god (`already_controlled`); the god holds no other possession (`Divinity.Possession.MaxPerGod = 1`, `control_limit`); biological age at least `Possession.MinAgeMilli` (default 0.5 sim years; a scenario may lower it); Mythic for the cost (MYTHIC §4.5). It opens a `ControlEntry { Kind = Possessed }` and a `PossessionSpell`, and emits `PossessionStarted`.
- **What it supersedes.** A compulsion on that body ends `Superseded`; the god's own avatar is dismissed `Superseded` (one embodied focus per god, SAC §11.1). Unread divine messages in the body's inbox wait; they lapse if they expire meanwhile.
- **What swaps and what does not.** Only *who chooses the next intent* changes. The mind, body, carry, household, relationships, community, citizenship, religion and ownership stay the person's. The policy seat is not rewritten: `Npc.Policy` keeps its name; the override seam skips the policy while the entry exists (SAC §11.3).

### 4.2 Deterministic input to intents, every tick

- **One input command.** The controls (CAMERA_AND_CONTROLS_DESIGN) produce `DivineControlInput`s:
  - `Heading` (a sector `0 … HeadingSectors − 1`, default 16, with `Gait`): valid for `Divinity.Control.LeaseTicks` (default 3) ticks; each tick while the lease holds, `Control` picks the passable lattice neighbour whose direction is nearest the sector's centre (integer angle table, ties by lower node id) and makes it the body's `MoveTo` candidate; the step is `WalkTo` at the body's own speed;
  - `MoveTo` (a node): the sim paths there with `NodePather` (click-to-move);
  - `Action` (an `Act` and a `Target`): pick up, drop, give, eat, gather, sleep, build, gesture and so on, through `DivineActs.Map` (DIVINE_PLAYER §4.4);
  - `Stop`: the body finishes its current edge and halts (bodies never stop mid-edge).
- **Validation.** The act must be one the person's level may name; the target must exist; reach, room, possession and paths are checked by `Act` as for anyone, and a failed act fails exactly as it would for the person (a frustration signal to its learner). The god's sight never extends the body's reach: the player may point at anything the god sees, and the body walks there.
- **Rate.** At most one input per body per tick; the last within a tick wins. The host sends an input only when the kind, sector or gait changes, or when the lease would expire next tick (SAC §11.3).
- **No input.** The body continues its running action; with none, it idles in place. A possessed person never makes a policy decision, so `RngPolicy` is not advanced (test `Possession_NoPolicyDraws`).
- **Capabilities are the person's own** (D5.8): carry room, child competence (POPULATION M4 "Children"), skills, tools, walking speed, fatigue. A separate miracle may change them (healing, blessing); possession itself grants nothing.

### 4.3 The body continues

`Body()` runs unchanged for possessed persons (SAC §11.3): hunger, fatigue, cold, pain and fear; nutrition and starvation; disease (infection, severity, recovery); injury and wound infection; ageing and life stage; pregnancy; reflexes. Being busy (a running meal) and sleep collapse at the fatigue limit interrupt inputs, as they would the person's own choices. The person can be attacked, injured and killed. **Death ends the possession**: `Kill` releases control first (`DivinityHooks.OnDeath`, SAC §11.4), the spell ends `Death`, and the death goes through `PopulationModule.Kill` with its real cause.

### 4.4 Consciousness, memory and the "divinely controlled" record

- **The person remains conscious.** `Thinker.Think` still runs every think tick (perception, beliefs, attention, recognition, candidates), so beliefs, social recognition and the inspector's options stay live. Interactions it takes part in are its own in every memory: a gift its body gave is a gift *from it* in the receiver's and its own social memory.
- **The record says the acts were controlled.** Every action started under possession carries `Control = 2` (possessed) on its action events (`ItemMoved`, `ExchangeStep`, `GiftGiven`, `GestureMade`, ...; SAC §15.1), the decision record carries `DecisionRecord.Control = Possessed` and the `InterventionId`, and `PossessedActionStarted` (847, HV) is emitted. Analytics, institutional records (a court may later ask who acted) and the warehouse can distinguish voluntary from controlled acts.
- **What other people know** is a matter of perception: a possession shows a visible sign (the god's colour in the body, VISUAL_WORLD_DESIGN) that onlookers perceive as `SituationKind.DivineSign`. A victim who perceived the possession may blame the god (its divine beliefs change: fear up, trust down) instead of the person (`Possession.BlameShiftPermille`, default 700, applied to the social-memory evidence of that act); one who did not perceive it blames the person it saw. That is the victim's attribution, not a rule that cleans the record.
- **Afterwards** the person keeps the base feature `SelfFeature.Possessed` (51) for `MemoryDays` (default 2), and a `Target` exposure in its divine mind (DIVINE_PLAYER §3): it remembers having been taken.

### 4.5 Learning from possessed actions

- **Traces.** Each action the possessed body starts opens a learning trace as a chosen action would (NPC_LEARNING §4.1 step 6), with the context built from the person's own base features plus the action's target features as the person perceives them, plus `Feature.Divine(Possessed 4)`. The trace is flagged as divinely controlled through that key and the decision record's `Control` field.
- **Credit.** Rewards and punishments reach these traces like any others, scaled by `Divinity.Possession.LearnPermille` (default 1000, range 0–1000; SAC §11.3). At 0 the person learns nothing from what its body did; at 1000 it learns as if it had chosen. Episodes opened by possessed deferred acts (a `Give` that opens reciprocity, a forage lot) close normally, at the same scale.
- **What can be learned.** Through `Feature.Divine(Possessed)` the person can learn that being possessed goes with good or bad outcomes (its fear or trust of the god also moves by the belief evidence, COMMANDMENTS_AND_RELIGION §4.2). Through the action keys it can learn the action itself: a possessed person walked to a fish spot it never knew and ate there now values `ForageAt` there (the brief's "the NPC may learn from actions performed during possession").
- **Surprise is its own.** The prediction for a possessed trace is the person's own learned value for that action in that context (perhaps very negative: a starving body carrying food to someone else), so the update measures surprise against its own expectations.

### 4.6 Upkeep and ending

- **Upkeep** per tick (MYTHIC §4.3; default 0.05 Mythic). When the god cannot pay, the possession ends that tick, `MythicExhausted`.
- **Ends:** `EndPossession { God }` at any time (`Cancelled`), `MythicExhausted`, `Death`, `Superseded` (the god manifests its avatar or a seat change the new controller cannot drive). On end: the control entry is released; at the person's next think (forced by `ForceThink`) its own policy decides afresh, so it is never left walking where the god sent it unless it chooses to continue. `PossessionEnded` is emitted exactly once (invariant CT1). The person is the same individual, with the same id and record (scenario 10).

### 4.7 Foreign citizens and animals

- **Foreign citizens** are allowed by the seam; a scenario may forbid them (`Possession.AllowForeign`, default on in sandbox). A foreign person possessed among its own people acts, in their eyes, as itself.
- **Animals** are reserved: `BodyKind` 2+ (SAC §4.1). When an animal body exists, it uses the same `ControlEntry` and an intent subset for its kind; nothing in possession assumes a human beyond `IntentRules` for the body's level.
- **Several possessions at once** are an architecture parameter (`MaxPerGod`), kept at 1 in this run (SAC 21.2).

---

## 5. The avatar (Mode B)

### 5.1 The body and its divine identity

- **One body per god, for the whole game.** The avatar is an `Npc` record with `Kind = Avatar`, `God = GodId` and id `AvatarBodyId = 0x4000_0000 + GodId`, in `PopulationState.Npcs` (SAC §4.2). It is created at the god's first manifestation and reused by every later one, so the identity people remember is persistent (D5.9). It has an `NpcMind` with `LearningMode.Off`, no M4 `Life`, no household, no hut rights, no genome of a person, and `Policy = PolicyRegistry.None`: it **never thinks for itself** and acts only on control inputs (`ControlKind.AvatarBody`).
- **Not a person.** It is never counted as a person (every person loop uses `Bodies.IsLivingPerson`, SAC §4.3), never has a `dim_person` row, never owns anything and is never a citizen or member.
- **Manifested** means `Alive = true` at a node; **not manifested** means `Alive = false`, its last node, an empty carry, so every `if (!n.Alive)` loop skips it for free.
- **`ManifestAvatar { God, Node }`:** the node is land and in the god's view; any possession of the god ends `Superseded`; cost and upkeep per MYTHIC §4.5; an `AvatarSpell` and `AvatarManifested` (850). **`DismissAvatar { God }`:** carried lots go to the ground at its node (`MoveReason.AvatarDrop`, ownership unchanged), `Alive = false`, `AvatarDismissed` (851).

### 5.2 Movement, reach and goods actions

The avatar is driven by `DivineControlInput` exactly like a possessed body (4.2) and uses the NPC machinery unchanged:
- **Movement:** `WalkTo` and `NodePather` with the same step costs (6 ticks a node, 4 on road).
- **Goods:** take (`Holder.Carry(AvatarBodyId)` with `OwnerRule.Keep`: an ownerless lot stays ownerless in its hands, an owned lot stays owned: taking someone's goods is the same physical take as anyone's and C1 classifies it as theft); drop; give to a person (`ToReceiver`, basis `Gift`; the receiver's social and divine memory record a gift from the god's body); gather from a source; use a tool; deliver logs and work to a hut site (`Build`, a demonstration of building).
- **Limits:** an adult's carry room and work rate, skill 0 by default (`Avatar.Skill`), no supernatural ability; miracles are separate commands with their own cost.

### 5.3 No hunger or ageing; can be attacked

- **No biology** (D5.9): `Body()` skips drive updates for `Kind != Person` (SAC §4.2): no hunger, fatigue, cold, ageing or illness, no sleep.
- **Damage.** The avatar has `Health` and `Injury`. Blows from civilian contests (`Social/Contests`), combat (`Sim/Military`) and other gods' smites go through `Bodies.Damage`, which dispatches to D5. It does not heal by itself; its own god's `HealInjury` heals it through `Bodies.Heal` (MYTHIC §6.1). `AvatarDamaged` (853) records each blow.
- **People choose to attack.** Others' candidate builders may offer an attack on the avatar like on anyone once the attack verb exists (C4); their instincts, learned values and beliefs about the god decide. Nothing forbids it.

### 5.4 Gestures and demonstrations

- **Gestures.** The avatar performs any M5 gesture act (`GestureAct`, `Sim/Comms/Message.cs`) through `DivineControlInput { Kind = Action, Act = Communicate }`, including `Demonstrate` (21, C7, "also avatar", SAC §14). Delivery uses the M5 perceiving-listener rule; listeners decode with the ordinary misunderstanding chance and choose their own responses. The avatar is a speaker like any other, never a channel that forces understanding.
- **Recognition.** People see it as `SeenPerson.Body = Avatar`, in the group `NpcGroup.Avatar` (10), with `SituationKind.AvatarInSight` (81). Whether a person *recognises it as its god* is that person's belief (D6, SAC 21.3): a believer of the god recognises it; others see a strange glowing figure.
- **Demonstrations.** Doing something where people watch is a demonstration: the avatar equips the axe and gathers logs, builds a hut wall, eats a new food, feeds a child. Attending observers learn from it like from anyone:
  - **vicarious learning** (NPC_LEARNING §4.13, M7): the visible outcome at the observer's vicarious gain, and the avatar in its `SeenVerbs` for later imitation (NPC_LEARNING §4.14, M11), with the model's esteem taken from the observer's beliefs about the god;
  - **teaching** (C7's `Teach`, 108, and CULTURAL_TRANSMISSION §4.5): a deliberate teaching act by the avatar uses the same adoption test, with the god's teaching strength `Avatar.TeachStrength` (default 200); a god can teach only what exists in the content.
- **Recorded.** Every avatar action carries `Control = 3` (avatar) and `AvatarActionStarted` (854, HV); observers record a `Witness` exposure; the spell counts distinct observers.

### 5.5 Destruction returns the god to divine existence

At `Health ≤ 0` the avatar is **destroyed, not killed** (SAC §4.2): no `NpcDied`, no estate, no `Kill` (which throws on a non-person body). Its carried lots go to the ground at its node (`MoveReason.AvatarDrop`, ownership unchanged); `Alive = false`; a loud divine sign is left; `AvatarDestroyed` (852) with the attacker and cause; the spell ends `Destroyed`; the camera returns to Mode A. **The god is unaffected** (D5.14): its Mythic, commandments, religion and records stay. It may manifest again after `Divinity.Cooldown.Avatar` (default 600 ticks after a destruction). The body record keeps its history; at the next manifestation its `Health` is restored to `Avatar.Health` (default full). People who destroyed it remember what they did; their beliefs about the god change by the evidence rules.

### 5.6 Upkeep and camera hand-off

- **Upkeep** per tick while manifested (MYTHIC §4.3; default 0.02 Mythic); when the god cannot pay, the avatar is dismissed that tick, `MythicExhausted`, dropping what it carries.
- **Camera.** Manifesting or possessing switches the view to a third-person camera on the body; ending returns to the overhead camera at the body's position. The hand-off is driven by `AvatarManifested`/`PossessionStarted` and the end events; the sim never reads the camera. CAMERA_AND_CONTROLS_DESIGN owns the cameras and input mapping; PLAYER_UI_DESIGN owns the controls and the upkeep readout.

---

## 6. Interactions with other systems

| System | What flows |
|---|---|
| `Divinity/Control.cs` (F1) | Possession and avatar spells as `ControlEntry`s; inputs; precedence; sweep |
| `Population` | `Body()` unchanged for persons and skipped for avatars; `WalkTo`, `Goods`, `Act`; `Bodies.Damage`/`Heal` dispatch; `MoveReason.AvatarDrop` |
| `Cognition` | Possessed persons think (steps up to candidates); `SeenPerson.Body`, `NpcGroup.Avatar`, `SituationKind` 80, 81 |
| `Learning` | `Feature.Divine(Possessed 4)`, `Possession.LearnPermille`, `SelfFeature.Possessed` |
| `Social`, `Comms` | Interactions under possession are the person's own; avatar gestures use M5 delivery and decoding |
| C7, C10 | Avatar `Demonstrate` and `Teach` |
| `Military`, contests | Avatar as a target; damage to its health |
| `Divinity` | Mythic upkeep, signs, exposures, causal ids |

---

## 7. Player and view

- **Possession:** the body glows in the god's colour, the camera moves behind it, the HUD shows the person's needs, carry and the upkeep rate, and "end possession" is always visible. The person's inspector keeps working (beliefs, "why not" options), so the player sees what the person would have done.
- **Avatar:** a distinct stylised figure in the god's form, its health, carried goods, a gesture wheel and a "demonstrating" hint showing how many people are watching.
- Animations follow the same events as ordinary actions (NPC_ANIMATION_DESIGN); transitions follow `PossessionStarted`, `AvatarManifested` and their end events (VISUAL_WORLD_DESIGN).

---

## 8. Telemetry

Codes and names are SAC §15.1.

| Event | Fields |
|---|---|
| `PossessionStarted` (845) | intervention, god, npc, community, superseded compulsion |
| `PossessionEnded` (846) | intervention, god, npc, reason, ticks, inputs applied and refused, actions |
| `PossessedActionStarted` (847, HV) | intervention, npc, act, target |
| `AvatarManifested` (850), `AvatarDismissed` (851) | intervention, god, body, node, reason |
| `AvatarDestroyed` (852) | intervention, god, by, cause, node, lots dropped |
| `AvatarDamaged` (853) | intervention, by, amount, health after, source kind |
| `AvatarActionStarted` (854, HV) | intervention, act, target, observers |
| Action events (extended by F1) | `Control` (0 own, 1 compelled, 2 possessed, 3 avatar) |

**Person-day row:** ticks possessed. **God-day row:** possession ticks, avatar ticks, avatar damage. Warehouse: `fact_possession_spell`, `fact_avatar_manifestation` (Q1, DATA_WAREHOUSE §12).

---

## 9. Invariants

| Id | Invariant | Checked by |
|---|---|---|
| CT1 | Every spell's start and end events pair exactly once | Event check (SAC §11.2) |
| I-DIV-6 | At most `MaxPerGod` possessions per god; one controller per body; one embodied focus per god | `SimInvariants`; `Control_Precedence_Table` |
| I-PA-1 | Possession changes only the control table and the spell; every body process runs for the possessed person as for others | `Possession_BodyContinues` |
| I-PA-2 | Every possessed and avatar act passes the same physical checks (C8) | Event-driven check |
| I-PA-3 | An avatar owns nothing; lots it carries keep their owner; nothing it carries vanishes on dismissal or destruction | `SimInvariants`; C1 |
| I-PA-4 | An avatar is never counted as a person | `Static_PersonLoopsUseIsPerson` (SAC §4.3) |
| I-PA-5 | A possessed person makes no policy draw | `Possession_NoPolicyDraws` |

---

## 10. Tier and package mapping

| Capability | Package | Tier |
|---|---|---|
| `BeginPossession`/`EndPossession` handlers, `PossessionSpell`, upkeep, end reasons, one at a time, minimum age | D4 | 2 |
| Possessed think and the `Control` record on events; `PossessedActionStarted` | D4 (seam: F1) | 2 |
| Learning under possession: `Feature.Divine(Possessed)`, `LearnPermille`, `SelfFeature.Possessed`; blame shift for perceiving victims | D4 | 2 |
| Stick and click to `DivineControlInput`; over-the-shoulder camera | U1 | 2 |
| Foreign citizens (flag) | D4 | 2 |
| Animals, several possessions | — | later |
| Avatar body (F1 type), `ManifestAvatar`/`DismissAvatar`, upkeep, `AvatarSpell` | D5 | 2 |
| Avatar perception and recognition (recognition belief-based, D6) | D5 + D6 | 2 |
| Avatar damage, destruction and return to divine existence | D5 (+ C4 for combat damage) | 2 |
| Demonstrations observed by vicarious learning | D5 | 2 |
| Avatar `Demonstrate` and `Teach` | D5 + C7 | 3 |
| Third-person camera hand-off | U1 | 2 |

Both are Tier 2 in the brief (§42). F1 lands their seams (body kind, control table, `Control` codes, `AvatarDrop`), so Tier 1 code never changes when they arrive.

---

## 11. Migration from the existing code

All structural changes are F1's (SAC §0.1, §4, §11): `BodyKind`, `Npc.Kind/God`, the person-loop sweep, the control seam and its `PopulationSystem` sites, `DivineControlInput`. D4 and D5 add only handlers, spells and their partial hook methods. Behaviour with no spell open is identical to the base (pins P1, P2).

---

## 12. Tests and acceptance hooks

| Id | Test | Pass | Scenario |
|---|---|---|---|
| PA1 | **Possess, act, return.** Possess person n, walk it 12 nodes by heading inputs, take a log, give it to a neighbour, end | Same `NpcId` and record before and after; `ItemMoved(HandOver)` with `Control = 2`; the neighbour's social memory records a gift from n; after the end n's next decision comes from its own policy (`Possession_ReturnsSameIdentity`) | 10 |
| PA2 | **The body continues.** Possess with no inputs for 2 days | Drives, nutrition, illness and ageing follow the same rules as a resting control; it starves to death if unfed, and the possession ends `Death` with `NpcDied(Starvation)` (`Possession_BodyContinues`) | 10 |
| PA3 | **Capabilities are the person's.** Possess a child of 0.6 years and gather logs | Work at the child's competence; a carry beyond its room is refused by `Goods` | 10 |
| PA4 | **Learning flagged and scaled.** A possessed person is walked to an unknown fish spot and eats there, at `LearnPermille` 0, 500 and 1000 | Its `ForageAt` entry for that place is 0, about half and the full voluntary value, in 100% of 20 seeds; contexts contain `Feature.Divine(Possessed)` (`Possession_LearnsWithControlTag`) | 10 |
| PA5 | **No policy draws.** A 300-tick possession | The person's `RngPolicy` state is unchanged (`Possession_NoPolicyDraws`) | 10, 18 |
| PA6 | **One at a time; Mythic ends it.** A second `BeginPossession` while one is active; upkeep runs out | Second refused `control_limit`; first ends `MythicExhausted` on the computed tick (`Exhaustion_EndsSameTick`) | 10 |
| PA7 | **Avatar walks and handles goods.** Manifest, walk 20 nodes, pick up an ownerless fish, give it to a hungry person, dismiss | Same `WalkTo` step counts as a person on the same path; the fish moves ground → avatar carry → person's carry; C1 holds; the avatar is in no person count | 11 |
| PA8 | **No hunger or ageing; can be attacked.** Manifested for 3 days, then struck in a contest until health 0 | No drive or age change; `AvatarDamaged` per blow; at 0 `AvatarDestroyed`, lots on the ground, no `NpcDied`, the god's records unchanged except the upkeep paid; it can manifest again after the cooldown | 11 |
| PA9 | **Demonstration is observed.** The avatar gathers with an axe in sight of 4 people who never used one | Each attending observer gets a vicarious update and the avatar in its `SeenVerbs`; their axe use over the next 5 days exceeds unobserved controls in ≥ 70% of 50 seeds | 11 |
| PA10 | **Replay and save/load.** *runner*: a recorded heading sequence for a possession and a manifestation, saved mid-spell and loaded | Identical hashes at every checkpoint; a lapsed lease halts at the edge end (`ControlInput_LeaseAndReplay`, `Control_SaveLoadMidSpell`) | 18 |
| PA11 | **Same action, three choosers.** The same 6 actions done by a person's own choice (a scripted bench policy), under possession and by the avatar | Identical goods and position changes, apart from holders and `Control` codes | 10, 11 |

---

## 13. Open questions

None blocking. Settled with configurable defaults: one possession and one embodied focus per god; `LearnPermille` 1000 (SAC default); minimum possession age 0.5 years; foreign possession on in sandbox; avatar skill 0 and teaching strength 200; a victim's blame shifts to the god only if it perceived the possession.

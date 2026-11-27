# NPC animation design (stage 4, divine player)

Status: design, 26 Nov 2026. Owner decisions: D5.24 (non-verbal NPC communication in normal play), D5.25 (optional accessibility and developer decoding), D5.21 (stylised low-poly characters), D5.6 (NPC autonomy). Brief section 28; acceptance rows M5.18 and M5.19 in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md). Package: U4 (NPC animations, gestures, carried objects), with F1 and the social, care, education and religion packages supplying events. Grammar owner: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md) section 4.9. Sister documents: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md), [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md), [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md), [GAMEPLAY_SCENARIOS.md](GAMEPLAY_SCENARIOS.md).

## 1. Purpose and scope

A reusable, procedural, non-verbal animation vocabulary that lets the player watch two people negotiate, share, warn, threaten, pray, teach or submit without any written sentence, and read it correctly. Every animation plays because the sim emitted an event for it. Unity never decides whether communication succeeded, was understood, or was accepted.

Note on the count: the brief's list in section 28 holds twenty entries (attention, pointing, requesting, offering, displaying objects, giving, receiving, accepting, refusing, agreeing, cooperating, warning, threatening, apologising, expressing gratitude, begging, praying, teaching, fear, submission). All twenty are covered here.

## 2. Current state (read from the code, 26 Nov 2026)

| Area | What exists | Where |
|---|---|---|
| Rig | `NpcLayer.Fig`: root, `Body`, `LegL`, `LegR`, `ArmL`, `ArmR` pivots, a `Cargo` mesh on the head and a `Tool` mesh in the right hand; body, arms and legs are cached meshes (`PeopleModels`), the head is part of the torso mesh (no head pivot, no hands or fingers). Walk cycle, eat, work, rest and sleep poses are coded in `NpcLayer.LateUpdate` | `Models/NpcLayer.cs`, `Models/PeopleModels.cs` |
| Gesture drawing | `GestureLayer.OnEvent` stores the latest `GestureMade` per speaker; `Apply` for 3.5 s draws (a) a billboard `TextMesh` over the head with the words (`Words(g)`: "offer: wants 1 fish, gives 3 berries", "need food", "warn! danger there"), coloured by act, (b) the right arm raised and swung, (c) a `LineRenderer` to the addressee | `Models/GestureLayer.cs` |
| Life-stage poses | child scale, elder stoop, expecting width, begging child bob (`DisplayKind.Beg`, `DisplayLevel`) | `Models/LifeStageLayer.cs` |
| Sim side (M5) | `GestureMade` (MessageId, Speaker, Act, Topic kind, id, sub, Quantity, Counter, Target, Time, DealId, Node, `Presented`, Listeners, `Honest` truth flag), `MessageDecoded` (per listener, with a `Mismatch` truth flag), `ResponseChosen`, deal steps (`ExchangeOpened`, `ExchangeStep` with Giver, Receiver, Lot, Ware, Count, `ExchangeClosed`), `ItemMoved` (reason `HandOver` among pick up, drop, deposit and so on), `TrustChanged`, `BetrayalRecorded`, M4 care events (`BeggingMade`, `ChildFed`, `CareGiven`, `ChildOrphaned`) | `Sim/Social/SocialEvents.cs`, `Sim/Population/*Events.cs` |
| Gesture acts | `GestureAct`: Want, Offer, Accept, Refuse, NeedFood, NeedHelp, Cooperate, Promise, Follow, Warn, Surrender, Defend, Accuse, Apologise, Forgive, AskJoin, Praise. M5 composes NeedFood, NeedHelp (with ware topic), Offer (trade, gift, counter), Accept, Refuse, Cooperate, Warn. The others are understood or arrive in M7 | `Sim/Comms/Message.cs`, COMMUNICATION 8.1 |
| Truth flags | `Honest` and `Mismatch` are analytics only; no policy, perception or mind code may read them | COMMUNICATION section 7 |

**Gaps against the brief and the acceptance rows:**
1. **M5.18 is not met today.** The text label and addressee line are drawn unconditionally by `GestureLayer.Apply`. Decoded text must be dev overlay or accessibility only, default off.
2. **M5.19 is only partly met.** One animation (an arm raise) is used for every act; there is no per-act vocabulary, no pointing at a person or object, no presenting of a held object, no facing or approach.
3. No head, no hands, no fingers (for quantities), no kneel, bow, nod, shake, point, beckon.
4. No animations for the non-M5 entries: praying, teaching, threatening, gratitude, submission; no listener-side reaction.

## 3. Decisions

| Id | Decision |
|---|---|
| NA-1 | **Animations are produced by a pure planner from sim events.** `GestureAnimationPlanner` takes events (and the sim state needed for positions and held items) and emits `AnimationCue` records. It has no Unity types, so it runs in EditMode and under `dotnet test`. |
| NA-2 | **No cue without a cause.** Every communicative cue carries the id of its source event; a cue with none is a bug (section 12). |
| NA-3 | **No sentence in normal play.** The label and the addressee line become the developer decode overlay (section 11), default off; "captions" for accessibility are a separate switch, also off by default. |
| NA-4 | **Gesture = a sequence of phases built from the composed message:** attention, act posture, topic (point or present), quantity (fingers or stack), counter (present), and a holding pose. Each phase is a short clip. |
| NA-5 | **Pose blending on the existing rig.** A `PoseLayer` over the walk and work base layers drives arms, head, torso lean, knees and root offset; no skinned meshes; clips are C# data (keyframes of bone rotations), deterministic and unit-testable. |
| NA-6 | **Approaching and facing are done by the sim where they matter** (physical interaction needs reach: hand-overs, care, teaching). The view adds only cosmetic facing. Walking to a recipient is the NPC's own movement, seen as ordinary walking. |
| NA-7 | **Symbols only where animation alone is insufficient,** off or automatic by default, never text (section 8). |
| NA-8 | **LOD changes what is drawn, never what is computed or what happens** (section 13). |
| NA-9 | **The view does not read `Honest` or `Mismatch`** to choose an animation. A lie looks exactly like the truth; the listener's decoded meaning is the NPC's own belief. |
| NA-10 | **All three kinds of actor use one vocabulary:** NPCs, the possessed NPC (tinted as divinely controlled) and the divine avatar (a distinct figure, the same clips). |

## 4. Architecture

```text
SimEvent stream (GestureMade, MessageDecoded, ResponseChosen, ExchangeStep, CareGiven, BeggingMade,
                 PrayerMade, TeachingStep, divine events ...)
        -> GestureAnimationPlanner (pure; reads positions, held items)
        -> AnimationCue { Id, SourceEventId, Actor, Kind, Target?, Object?, StartTick, Phases, Intensity }
        -> CueQueue per actor (ring buffer, 10 s of view time)
        -> PoseSequencer (Unity; evaluates phases to bone targets with easing)
        -> PoseLayer blend over walk/work -> bones (Head, Body, ArmL, ArmR, LegL, LegR) + props (Carried, Held, Symbol)
```

- **Timing.** Cue start is the event's tick converted to view time. The sim gesture costs 1 to 3 seconds of sim time (COMMUNICATION 4.9); the cue's duration is that cost (the event carries or the planner knows it from `CommsTuning`; a `DurationTicks` field is proposed on `GestureMade`, section 11). At 1x it plays at natural speed; at higher speeds the clip scales to the available view time but never shorter than 0.15 s, and at 10x or more only the posture and the held pose are shown.
- **Queues.** An actor may have several cues. Priority: divine control and reactions to a perceived miracle, then gesture phases, then care and teaching, then work poses; a lower cue is deferred (not dropped) until the actor's current cue ends unless it is older than 10 s of view time.
- **Pure clips.** A clip is `Clip { Name, Duration, Tracks: Bone -> Key[] }`, where a key is `(t, Euler, Ease)`. Evaluation `Pose Evaluate(Clip, t)` is a pure function of time, so an EditMode test can compare sampled values, and the same clips run headless for a `PoseLog` (for example in screenshot checks).
- **Phases.** `Phase { Clip, Duration, Facing (None, Target, Object), Prop (None, HeldWare, Gesture Symbol), Weight }`.

**Rig additions** (`PeopleModels`, `NpcLayer.Fig`): a `Head` pivot (neck at 1.5), simple `Hand` boxes at each wrist with an optional 1 to 5 finger-pip child set for quantities, a `Held` mesh slot in each hand (for displayed goods; the head-load `Cargo` stays for work), a `Symbol` slot above the head, and an `Aura` slot for divine effects (VISUAL_WORLD_DESIGN section 9). Part names of the existing bones are unchanged.

## 5. The vocabulary of twenty

Each row is a reusable clip family. "Directed" means the actor faces and, when the sim has them within reach, steps toward the target.

| # | Animation | Pose and motion | Directed | Object shown | Phases |
|---|---|---|---|---|---|
| 1 | **Attention** | step forward half a pace, raise one hand with an open palm, small wave, head tilted towards the recipient; the recipient turns its head | yes | none | Wave (0.5 s), Hold (0.3 s) |
| 2 | **Pointing** | arm fully extended along the line to the topic (a place, a person, a ware); head turns to the same line; finger box extended | yes, towards topic | the lot or the building named, if visible | Reach, Hold (0.7 s), Return |
| 3 | **Requesting** | both hands out, palms up, small forward lean, head up; for a ware: reach then a palm on the chest; for help: beckon with both arms | yes | optional symbol of the ware | Reach, Palm-to-chest, Hold |
| 4 | **Offering** | the item held on both open palms in front, a short step toward the recipient, a small bow | yes | the real ware (`Held`) | Raise, Present (0.8 s), Hold |
| 5 | **Displaying objects** | lift the item high with one hand, turn it left and right so a watcher sees it; for a distant building, point then sweep | optional | the real ware | Lift, Turn (0.8 s), Lower |
| 6 | **Giving** | extend both arms, pass the item, release (the ware leaves `Held` and the lot moves in the sim at the same tick), a step back | yes (requires reach) | the real ware | Extend, Release, Withdraw |
| 7 | **Receiving** | both hands cupped, take the item, draw it to the chest (the ware appears in `Carry` as the sim moves it), a nod | yes | the real ware | Cup, Take, Draw-in |
| 8 | **Accepting** | a nod, a step forward and both hands put out to the offered goods | yes | none | Nod, Extend |
| 9 | **Refusing** | head shake, palm out in a stop gesture, step back | yes | none | Shake, Palm-out, Step-back |
| 10 | **Agreeing** | a double nod and a hand to the chest; a thumb up when close | yes | none | Nod x2, Chest |
| 11 | **Cooperating** | walk to stand shoulder to shoulder, then mirror the partner's work or carry pose in step with them | yes | a shared load if hauling | Approach, Align, Mirror |
| 12 | **Warning** | eyes wide, one arm raised high and flapped or a fist shaken, stamp, then a point at the danger place or the dangerous person | yes (audience), then topic | none | Raise, Stamp, Point |
| 13 | **Threatening** | weapon or fist raised and held; chest forward, head low; step toward the target; a slower, heavier version of warning aimed at a person | yes | the weapon held | Raise, Advance, Hold |
| 14 | **Apologising** | head bowed, hands open or clasped; if carrying a gift, held out low | yes | the gift, if any | Bow, Hold, Offer |
| 15 | **Expressing gratitude** | a small bow with a hand on the chest, then both hands lifted open | yes | none | Bow, Chest, Lift |
| 16 | **Begging** | kneel or crouch, hands cupped and held out, head up, a tremble; a child bobs (existing `DisplayKind.Beg`) | yes | none; belly pat for hunger | Kneel, Hold-out |
| 17 | **Praying** | kneel, head bowed, hands joined or raised to the sky; a thread of motes (VISUAL_WORLD_DESIGN 9) | no | none | Kneel, Raise, Hold |
| 18 | **Teaching** | demonstrate: perform the skill slowly with exaggerated motion while pointing at the learner and the object; repeat; then hand the tool over for the learner's turn | yes | the tool or material | Show, Point, Hand-over |
| 19 | **Fear** | shrink: shoulders up, arms over the head or chest, knees bent, head turned away; trembling; flee gait when the sim says flee | no | none | Cower or Flee-run |
| 20 | **Submission** | drop any weapon, kneel, hands open and raised or crossed, eyes down; before a god or avatar, prostrate | yes | the dropped weapon on the ground | Drop, Kneel, Bow |

Variations: a **quantity** (1 to 9) raises finger pips on one or both hands (1 to 5 on the right, 6 to 9 add fingers on the left), or shows a stack of the real ware; **intensity** (0 to 1) scales amplitude and duration (a strong request is wider and held longer); **distance** adds a bigger arm swing so a far watcher sees it; **age** changes proportions (a child's reach is shorter); **possession** adds a stiff, over-careful quality only if the player's input timing shows it, not sim data (cosmetic).

## 6. Mapping from the M5 gesture parts and events to animations

### 6.1 Composed message to a sequence
A `GestureMade` composes `Act, Topic (kind, id, sub), Quantity, Counter, Target, Time, Presented`. The planner builds the sequence by fixed rules, mirroring the grammar ("act posture, then pointing or presenting the topic, then the fingers, then the counter").

| Part | Rule | Clip |
|---|---|---|
| Target set | an attention prelude; both turn to face (cosmetic) | Attention (1) |
| Act | the act posture of the table of 6.2 | Requesting, Offering, Accepting, Refusing, ... |
| Topic kind Ware | if `Presented`, show the real ware in `Held`; otherwise point at the remembered ware's place or hold a ware symbol (section 8) | Displaying (5) or Pointing (2) |
| Topic kind Place | point at the place node; the arm direction is computed from the node position (view only) | Pointing (2) |
| Topic kind Npc | point at the person (a stare, a chin lift) | Pointing (2) |
| Quantity | fingers or stack | finger pips |
| Counter | present the counter item in the other hand | Offering (4) |
| Time (later) | a sweep towards the horizon for `Time = Later` | a sweep clip |
| Hold | a short hold pose so the listener can be seen to respond | hold |

### 6.2 Act to animation family

| `GestureAct` (as built or planned) | Animation family | Notes |
|---|---|---|
| `Want` (understood in M5, composed later) | Requesting (3) with a reach at the ware | |
| `Offer` with a ware topic and a counter | Offering (4) presenting the counter, Pointing (2) or Displaying (5) of the wanted ware, finger quantity | the full trade proposal |
| `Offer` gift (no topic) | Offering (4) low, a little bow | |
| `Accept` | Accepting (8); Agreeing (10) when the topic is not a ware deal | |
| `Refuse` | Refusing (9) | |
| `NeedFood` | hand to mouth, then to belly, slumped (Begging family 16 for an adult) | |
| `NeedHelp` | Requesting (3) beckoning, then Pointing (2) at the task | |
| `Cooperate` | Cooperating (11) after Agreeing (10) | |
| `Promise` (M7) | Pointing (2) at the other, hand on chest, sweep to the horizon | |
| `Follow` (M7) | beckon, turn, walk a step, look back (Attention 1 plus a walk) | |
| `Warn` | Warning (12) with Pointing (2) at the place topic | |
| `Surrender` (M9) | Submission (20) | |
| `Defend` | Threatening (13) stance over the resource, arms wide | |
| `Accuse` (M7) | Pointing (2) hard at the person, then at the empty hands or broken thing, angry stance (Threatening 13 lite) | |
| `Apologise` (M7) | Apologising (14) | |
| `Forgive` (M7) | open arms, shoulder touch (Agreeing 10 plus a contact pose) | |
| `AskJoin` (M7) | Submission (20) to an official, present goods, point at the banner | |
| `Praise` (M7) | Pointing (2) at the subject, thumb up, hand on chest | |
| `Threaten` (candidate, no owner yet) | Threatening (13) | amendment, section 11 |
| `Thank` (candidate, no owner yet) | Expressing gratitude (15) | amendment, section 11 |
| `Greet` or `Alarm` (candidates) | Attention (1); Warning (12) | amendment |

### 6.3 Events other than gestures

| Sim event or state | Animation | Rule |
|---|---|---|
| `ExchangeStep` (Giver, Receiver, Lot, Ware, Count, Node) | Giving (6) on the giver, Receiving (7) on the receiver, in lockstep on that tick | only when the step's `DealId` has a `GestureMade` earlier in the log (M5.19) |
| `ItemMoved` with reason `HandOver` and a care `PurposeRef` | Giving and Receiving in a carer variant (kneel to a child) | the care event, not a gesture, is the cause; classified functional (section 12) |
| `BeggingMade` (children, M4) | Begging (16), existing child bob kept | |
| `ChildFed`, `CareGiven` | a feeding or tending pose (functional, not communicative) | |
| `ResponseChosen` with Ignored | no animation (the sim chose nothing); optional head turn away if the sim provides a "turn away" flag later | do not invent a reaction |
| `MessageDecoded` | listener head and body turn towards the speaker while attending (Attention reaction); the decoded meaning is not shown | this is cosmetic attention only |
| Drive Fear above a threshold, `Flee` intent, `InjuryTaken` | Fear (19) as a posture layer | outward expression of a body state; no gesture needed |
| Prayer (`PrayerMade`, D6) | Praying (17) | |
| Teaching activity (education events, C7: `TeachingStarted`, `LessonStep`) | Teaching (18) on the teacher, an observing pose on the learner | |
| Divine events | Fear (19), Praying (17), Submission (20), Gratitude (15), shielding the eyes, turning to look up, according to the sim's response event and what the person perceived | section 10 |
| Compulsion, possession | a driven gait and posture layer; no communication cue | VISUAL_WORLD_DESIGN 9 |

## 7. Approaching, facing, sequences and conversations

- **Approach is the sim's.** An NPC that wants to give, trade or care chooses a `Seek` or walking intent; the hand-over requires reach (invariant C6 from the `ItemMoved` actor and holder nodes). The view shows the walk as the NPC's normal gait with a slightly more purposeful stride (cosmetic) and ends it facing the recipient. When the speaker is beyond reach but within the gesture range (`CommsRange.Gesture`), the gesture is made at a distance with a bigger swing.
- **Facing.** While a cue with a target runs, the actor yaw turns towards the target's drawn position at 360 degrees per second, and back to its movement yaw after the hold. Listeners that the sim says attended turn the head (not the body, if they are working) to the speaker.
- **Sequences and conversation.** A conversation is a sequence of `GestureMade` events between two people, linked by `MessageId`, `DealId` and `Target`; the planner renders them in order, with a hold between turns. The deal arc "request, offer, counter, accept, give, receive, gratitude" is a chain of cues built from events as they arrive, so a trade reads as a dance of gestures. If the sim reorders or drops a step (misunderstanding, refusal, ignore), the sequence shows exactly that, because the planner draws only events that happened.
- **Group scenes.** Several listeners react independently from their own events; a crowd around an avatar shows the Fear, Praying or Submission posture per person according to its own state.
- **Cue cancel.** If the speaker dies, is stunned or is compelled, its remaining phases stop at the next frame from the matching event.

## 8. Optional small symbols

- **Purpose:** only where animation alone is insufficient, mainly to identify the topic ware or place at a distance. Never text, never a number string (quantity is fingers or a stack).
- **Form:** a small procedural icon above the head for 1.5 s (a ware silhouette from the same meshes as `LoadMesh`, a house outline for a place, a crossed bar for danger, a heart-like shape for gratitude is avoided in favour of an open-hand glyph).
- **Setting `Symbols`:** Off, Auto, On. Default Auto: show a symbol only when the camera is above a configurable height (default 40 m) and the topic ware is not being presented as a held object. Both the Auto rule and the glyph set are view-only.
- **Captions (accessibility)** are separate: a one-line plain-language caption of the visible gesture (for example "offers 3 berries for 1 fish") built from the gesture's observable fields, off by default, with no truth flags. They must be derivable from what an observer could have seen, so they never reveal a lie.

## 9. Carried-object presentation

| Situation | Presentation |
|---|---|
| Work load (carrying a ware to a store) | head load as now, the mesh from `LoadMesh(ware)`; stack grows to four with `Count` |
| Displaying or offering | the ware moves from the head or the belt to the hands (`Held`), a short lift animation; the lot id of the `Held` item is the sim lot named in the event, so the item shown is real |
| Giving | `Held` hand extends to the receiver; at the `ExchangeStep` tick the item is detached and attached to the receiver's `Held` or head; the sim lot moves on the same tick, so the picture and the state agree |
| Fragile or infant | carried in both arms, head up |
| Equipment | tools stay in the right hand when working, on the back or belt when not, from `Carry` and equipped state; weapons held for threatening |
| Dropped by submission | the weapon lies on the ground at the actor's feet, a ground prop tied to the lot the sim moved |
| Ownership | no outward marker in normal play; the ownership overlay is a dev overlay |

## 10. Divine events, possession and the avatar in the vocabulary

- **NPC reactions to perceived divinity** (from the sim's response events, never guessed): Fear (19), Praying (17), Submission (20), Gratitude (15), a look-up and shield-the-eyes beat, and flee. Whether a witness reacts is the sim's decision from its perception and beliefs; the view animates `DivineResponse`.
- **Divine suggestion, command, compulsion:** the receiver's response animation is the sim's own result: accepted (agreeing nod and turn to the task), refused (shake, turn away), ignored (none), compelled (driven gait). The animation is the NPC's body language for its decision, not a claim about the god's effect.
- **Possession:** the body's actions are the NPC's own action primitives driven by `DivineControlInput`; gestures made through possession are player-chosen gestures sent as commands and appear with the same clips, tagged "divinely controlled" on the actor (a subtle rim), as the sim records.
- **Avatar:** uses the same clips, with a distinct model and larger, calmer motion (an easing profile `Divine`), and can demonstrate activities (Teaching 18) and give or take objects like any body. The avatar's gestures are `GestureMade` events with the avatar entity as Speaker.

## 11. Sim-interface rules and requested sim amendments

**The view reads:** the event stream, positions of drawn figures (`NpcLayer.TryPosition`), the `Carry` list and `Display` of an NPC (`Life.Display`, `DisplayLevel`), the drives, and the node positions of topics. It sends nothing for animation.

**Rules**
1. Cue = f(event, state at the event). Same events give the same cues; a test replays a log and compares cue lists.
2. The planner and the sequencer do not read `Honest`, `Mismatch` or any analytics-only field (checked by a reflection test).
3. No animation sets, moves or asks for any sim value; it may fail to draw (culling) without any effect on the sim.
4. Cosmetic facing, leans, tremble and amplitude scaling are allowed because they leave sim positions unchanged.
5. Dev overlay and captions are the only places text appears for gestures (section 12).

**Amendments requested from the owning packages (not blockers for Tier 1):**

| Request | Owner | Why |
|---|---|---|
| `GestureMade.DurationTicks` (or a documented constant per act) | social, M5 | cue length equals the sim cost |
| `GestureAct.Thank`, `Threaten`, `Greet`, `Alarm` (the candidates with no owner in COMMUNICATION 4.9) | social | gratitude, threatening and attention are listed by the owner and should be sim acts, not view inventions |
| `PrayerMade`, `TeachingStarted` and `LessonStep` events | D6, C7 | praying and teaching |
| `DivineResponse` (or the existing `ResponseChosen` extended with a divine source) | D2 | reaction animations |
| a short `AttentionDirected { Listener, Speaker }` or reuse `MessageDecoded` | social | listener turn |
| `GestureMade.Presented` carries the lot id shown | social | the displayed item is real |

## 12. Acceptance alignment (M5.18 and M5.19)

**M5.18 (decoded text only in the developer overlay, default off; hash unchanged either way).**
- `GestureLayer.Apply` is changed so that the `GestureLabel` text and the addressee line are created only when `DevOverlays.DecodedMessages` (developer) is on; the arm raise is replaced by the vocabulary. With it off, no `TextMesh` exists under any figure.
- Two switches: **Decode overlay** (developer, shows intended and decoded text with the mismatch marker, may use truth flags) and **Captions** (accessibility, plain-language, no truth). Both default off, both persist in local preferences, neither enters the save or the hash.
- Test: 5 days of a Living island run with both off find no gesture `TextMesh` or text; on, they appear; `StateHash` is equal in all three runs (H-NA-1).

**M5.19 (the nine named gestures map to reusable body animations; every animation follows a gesture event; none plays for a transfer with no preceding gesture and decision).**
- The nine named acts (request, offer, accept, refuse, give, threaten, warn, cooperate, gratitude) map to the rows 3, 4, 8, 9, 6, 13, 12, 11 and 15 above, each a reusable clip family.
- Every communicative cue carries `SourceEventId`; the planner refuses to emit Giving or Receiving unless the `ExchangeStep`'s deal has an earlier `GestureMade` and `ResponseChosen` (the receiver's own decision). A hand-over without them, which includes functional care transfers (`ChildFed`, `CareGiven`), is classified functional and drawn with the functional feeding pose, not the communicative Giving clip.
- **Threaten and gratitude have no sim act yet** (section 11); until D-package or social work adds them, the clips exist and are tested with synthetic events, and M5.19 is satisfied for the acts the sim produces. When the acts exist the mapping is already in place.
- Test: for every `GestureMade` in a 10-day run, at least one cue with its `SourceEventId` exists and its clip family equals the table; for every `ExchangeStep`, a Giving cue exists on the giver; no communicative cue exists without a source event (H-NA-2, H-NA-3).

## 13. Level of detail (rendering only)

| Camera distance | What is drawn for a cue |
|---|---|
| Close (within 25 m) | full vocabulary: head, arms, hands, finger pips, held items, symbol options |
| Mid (25 to 90 m) | simplified clips: arm raise or point, bow, kneel, held item; no fingers |
| Far (beyond 90 m) | a small coloured pulse above the actor by gesture family (offer, request, warn, refuse) when `Symbols` is Auto, and conversation lines are skipped |
| Whole island | aggregated markers (a trade marker where a deal closes) |

All cues are always planned from events (cheap, no per-frame cost when nothing happens); only the sequencer's evaluation is skipped when the actor is not drawn, and its current phase is reconstructed from the cue time when it is drawn again. The sim never reads the camera, LOD, or cue state. A test runs the same seed at three zoom levels and compares `StateHash` and the event log (H-NA-6).

## 14. Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.

| Capability | Package | Tier |
|---|---|---|
| Remove unconditional gesture text and line; developer decode overlay; hash independence (M5.18) | U3a | 1 |
| Planner, cue queue and pose layer; the clips for request, offer, give, receive, accept, refuse, warn, cooperate, begging | U3b | 2 |
| Head pivot, hands, finger pips, held items | U3b | 2 |
| Pointing, displaying, attention, agreeing, apologising, gratitude, threatening (with the new acts) | U3b with social | 2 |
| Fear and submission reactions to divine events; praying; teaching | U3b with D2, D3, D6, C7 | 2 for fear and submission and praying, 3 for teaching |
| Optional symbols and captions | U3b | 3 |
| Conversation chains with holds, group scenes, LOD pulses, divine tint | U3b | 3 |

## 15. PlayMode-testable acceptance hooks

| Hook | Test |
|---|---|
| H-NA-1 | M5.18: no gesture text exists with overlays off; on, labels appear; `StateHash` equal in both |
| H-NA-2 | M5.19: each `GestureMade` has a cue with its `SourceEventId` and the expected clip family |
| H-NA-3 | Giving and Receiving cues exist only for `ExchangeStep`s whose deal has a gesture and a response; a synthetic hand-over without them produces none |
| H-NA-4 | Clip purity: `Evaluate(clip, t)` is deterministic and bounded (joint angles within limits) for 20 clips at 100 sampled times |
| H-NA-5 | Facing: during a Pointing cue the head yaw error to the topic is under 15 degrees at the hold phase |
| H-NA-6 | LOD invariance: same seed, three camera distances, identical hash and event log; cue lists identical |
| H-NA-7 | Reflection: the planner and sequencer do not reference `Honest`, `Mismatch` or `GodState` write paths |
| H-NA-8 | Held object truth: during Giving the `Held` ware equals the event's ware and count; after the tick the receiver's `Carry` contains it (read from the sim) |
| H-NA-9 | Quantity: a message with quantity 3 shows 3 finger pips; 7 shows 5 plus 2 |
| H-NA-10 | Misunderstanding: when the sim decodes an `Offer` as a `Want`, the listener's response cues follow its decoded decision, and the speaker's cue is unchanged |

## 16. Risks

| Risk | Mitigation |
|---|---|
| Procedural poses look wooden or unreadable | few strong silhouettes per clip (arm line, head, kneel), easing, reviewed by screenshot sheets of all 20 clips; amplitude and hold times are configuration |
| Animations contradict the sim (an item appears before it moves) | the Giving release is bound to the `ExchangeStep` tick; test H-NA-8 |
| Players cannot read trades without text | optional symbols and captions, fingers, held goods, holds between turns; developer decode for diagnosis |
| Animation load with many actors | event-driven cues, LOD tiers, pooling, a cap on simultaneous detailed sequences (default 40) |
| Missing sim acts (threaten, thank) delay the vocabulary | clips exist and are tested with synthetic events; mapping added when the acts arrive |
| Head and hands change the cached meshes and break tests | add parts, keep names; update the `ModelKit` cache keys |
| Misreading a lie as honesty | by design: gestures show what the speaker did; the player learns of lies through consequences or through the dev overlay |

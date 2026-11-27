# Divine player design (owner decisions D5, 26 Nov 2026)

The player is a god, never a government. This document owns what a god is to the game: its identity and seat, the four ways of experiencing the one world, the six distinct kinds of divine intervention and what each changes, the divine action vocabulary and its mapping onto the existing NPC actions, how instructions reach individuals, groups, households, organisations and places, how NPCs and organisations decide to follow, modify or refuse, and how divine interventions enter each NPC's own learning.

It follows [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) (sections 12 and 15) and the owner decision register [OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md) (D5.1–D5.33); the owner's words are in [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §§4–9 and §14. **Types, ids, enum values, command and event names are frozen by [SHARED_ARCHITECTURAL_CONTRACTS.md](../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) (cited as "SAC §n"); this document uses them and owns the behaviour behind them. Where the two ever differ, SAC wins.**

Written against `unity/poc` at `8617eaa` (M3, M3b, F0, M4 and M5 merged). "Exists" means in that tree; "F1" means the behaviour-neutral foundation commit of SAC §0.

---

## 1. Purpose and scope

**This document owns:**
- the behaviour of gods: seats (human, AI, none), switching in sandbox, persistence, recovery without regeneration (types: SAC §2);
- the four interaction modes A–D against one authoritative sim;
- the taxonomy Suggestion / Command / Compulsion / Possession / Commandment / Miracle: what each changes and how the NPC decides (command families: SAC §10.2);
- the divine action vocabulary table: divine act → existing verb or primitive → status (`DivineAct` values: SAC §6.1);
- instruction targets and their resolution, expiry and responses;
- how organisations and governments refuse, modify or comply (scenario 15);
- the separation of religious, political, economic and institutional authority;
- divine learning (brief §14): attribution by the NPC's own learner, witnesses and communication, and the causal record linking intervention → perception → learning updates.

**It links to, and does not repeat:**
- Mythic and every miracle: [MYTHIC_AND_MIRACLES_DESIGN.md](MYTHIC_AND_MIRACLES_DESIGN.md);
- possession and the avatar body: [POSSESSION_AND_AVATAR_DESIGN.md](POSSESSION_AND_AVATAR_DESIGN.md); the control seam itself: SAC §11;
- commandments, belief, faith, affiliation, prayer and religious organisations: [COMMANDMENTS_AND_RELIGION_DESIGN.md](COMMANDMENTS_AND_RELIGION_DESIGN.md);
- resurrection and the archive of the dead: [RESURRECTION_DESIGN.md](RESURRECTION_DESIGN.md);
- the organisation framework (governance, office-holders, relay recipients): [ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md](ORGANISATIONS_AND_INSTITUTIONS_DESIGN.md);
- physical actions, reach, ownership and affordances: [WORLD_ONTOLOGY.md](WORLD_ONTOLOGY.md); the decider, perception and gestures: [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md); the learner: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- the information layers and divine observation (D5.28): [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md) §12 and SAC §9;
- cameras and input: [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md); toolbar, radial menu and panels: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md); the divine visual identity: [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md); NPC reactions: [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md); scenarios and objectives: [GAMEPLAY_SCENARIOS.md](GAMEPLAY_SCENARIOS.md).

---

## 2. Owner decisions and principles

**Owner decisions applied:** D5.1 (god only), D5.2 (governments autonomous; authorities distinct), D5.3 (a god per initial faction; replaceable AI policies; sandbox switching), D5.4 (gods persist independently), D5.5 (a god cannot lose; no auto-regeneration), D5.6 (NPC autonomy), D5.7 (compulsion), D5.10 (compulsion and possession cost Mythic), D5.28 (divine and institutional knowledge separate), D5.29 (faction policy always autonomous), D5.30 (AI god policies), D5.33 (no predetermined outcomes).

**Principles:**
1. **A god acts only through validated commands.** Every divine act is a `DivineCommand : Command { God }` (SAC §10.1) through `GodSim.Submit`, applied at the next tick boundary, re-validated in `Apply`, which spends Mythic first, opens an intervention (`InterventionId`) and then applies. Commands are saved in the pending queue and hashed. Unity sends commands and animates events; it never decides an outcome. A replay of the command log reproduces every divine intervention (scenario 18); AI gods' commands are recomputed in replay because their seats are sim state (SAC §2.1, §12).
2. **Human and AI gods use the same rules:** the same command types, Mythic, cooldowns, validation and divine view; no charging code reads the seat (SAC §5).
3. **Six intervention types, never one generic instruction.** Each is its own command family, record and events (SAC §10.2). Section 4.3 says what each changes.
4. **Influence goes through minds; only miracles touch the world directly.** Suggestions, commands and commandments change only what an NPC perceives and believes; the NPC's own policy decides. Compulsion and possession take over the choice for a while but never the physical checks. Miracles change authoritative state through the owning modules' functions (`Goods`, `Bodies.Damage`/`Heal`, source and hut APIs, `PopulationModule.Kill`).
5. **No authority by status.** Being a god gives no administrative, political, economic or institutional power: a god owns nothing (no `OwnerKind.God` exists, SAC §2 G3), holds no office, enacts no law, posts no contract and appoints no one. It influences the people who can.
6. **The NPC interprets.** The player's intention is never written into a mind. The NPC attributes outcomes with its own learner and beliefs, and may be wrong (section 4.9).
7. **Exact determinism.** Integers only, ascending ids, keyed draws with the SAC labels (`divine-perceive(npc, intervention)`, `divine-decode(npc, intervention)`, `god-policy(god, tick)`, `miracle-effect(intervention, step)`), no unordered enumeration.
8. **The civilisation runs without gods.** With `Divinity.Enabled = 0` there is no divinity state and every part hash equals the base (pin P1); with an idle god for every faction only the `divine` part differs (pin P2).

---

## 3. Data structures

The frozen shapes are in SAC: `GodRecord` and `GodRegistry` (§2), `DivineCommand` and the command families (§10.2), `DivineAct` (§6.1), `DivineTarget` and `DivineScope` (§6, §10.2), `ControlEntry` (§11), `DecodedDivine` and `DivineMind` (§10.3, §13.1), `IDivineView` and `DivineObservationProfile` (§9), `IGodPolicy` (§12). In brief:

```csharp
public enum GodSeat : byte { None = 0, Human = 1, Ai = 2 }
public sealed class GodRecord { int Id; string Name; Faction OriginFaction; GodSeat Seat; string PolicyName;
                                long Mythic, MythicMax; long[] CooldownUntil; DivineObservationProfile Profile; ... }   // GodId = (int)faction + 1
public abstract class DivineCommand : Command { public int God; }
public enum InterventionKind : byte { Suggestion = 1, Command = 2, Compulsion = 3, Possession = 4, Commandment = 5, Miracle = 6, Avatar = 7 }
public interface IGodPolicy : IPolicy { void Decide(in IDivineView view, ref Rng rng, List<Command> outCommands); }   // PolicyLevel.God = 4
```

**What this document adds (D2's internal records, in `DivinityState.Messages`):**

```csharp
public struct Instruction {                // one per accepted suggestion, command or compulsion
    public int  Intervention, God; public InterventionKind Kind;
    public DivineAct Act; public DivineTarget Target; public DivineScope Scope;
    public long IssuedTick, ExpiresTick;
    public ushort Recipients, Perceived, Followed, Refused, Lapsed;   // counters for reports
    public byte DeclaredReason;            // optional verb the god "meant" (analytics truth only; no rule or policy reads it)
}

public struct DivineExposure {             // DivineMind ring, 4 (D2): interventions this NPC experienced, witnessed or was told of
    public int Intervention, God; public long Tick; public byte Role;   // Target, Witness, Told
}
```

`DivineExposure` is the one field this document asks D2 to add to `DivineMind` (an additive field in its own partial file, SAC §13.1). It is the NPC's own memory ("I saw a power strike Bo yesterday"), used by the attribution step (4.9) and by telemetry to fill causal fields.

**Per-NPC memory:** inbox 4 × `DecodedDivine` (about 32 B each) and exposures 4 × 16 B: about 192 B; beliefs and commandments are in COMMANDMENTS_AND_RELIGION §3. The [POPULATION_DESIGN.md](POPULATION_DESIGN.md#memory-budget-per-npc-combined) table gains a "Divinity" row (D1 updates it).

---

## 4. Mechanics

### 4.1 Gods, seats and persistence

- **Creation.** `DivinityModule.StartGame` creates one `GodRecord` per initial faction, `GodId = (int)faction + 1` (SAC §1), before the first tick; `GodCreated` is emitted on the first divinity tick. The pairing (`OriginFaction`, `GodAssociated`) is history: no god field depends on the faction, state, territory, population or religion existing (SAC §2 G2). The existing `FactionEliminated` event never touches a god.
- **Seats.** `GameSetup.HumanGod` names the god whose seat is `Human`; every other god is `Ai` with the `policies.god` assignment, default `IdleGod` (never acts). `StewardGod` (scripted, D1, Tier 2) is described in 4.11.
- **God-only mode.** With `GameSetup.Control = GodOnly`, every faction is seated by its faction policy and `GodSim.Submit` refuses an external `FactionCommand` (`god_only`, "Governments decide for themselves."). The Stage 3 mode keeps the human faction seat as a temporary mechanism (SAC §2.1).
- **Switching gods (sandbox).** `SwitchGod { From, To }` moves the `Human` seat; `SetGodPolicy { God, Policy }` changes an AI god's policy. Both are logged commands refused outside sandbox (`not_sandbox`). Switching changes who commands the god, never its state; control spells that the new seat cannot drive are ended `Superseded`.
- **A god cannot lose (D5.5).** No rule removes a god or disables its commands because its followers, faction or religion are gone; fixed regeneration continues (MYTHIC §4.2). Victory conditions evaluate factions; god objectives are optional scenario rules (GAMEPLAY_SCENARIOS).
- **Recovery routes, never regeneration.** A god whose people died out may instruct surviving or foreign believers to migrate (`MoveTo`), show itself by miracles to foreign people (raising their belief: conversion, COMMANDMENTS_AND_RELIGION §4.6), resurrect archived dead followers (RESURRECTION_DESIGN), manifest and demonstrate. **The sim never regenerates a population**: persons enter only by birth, configured immigration and resurrection of an existing `NpcId` (population conservation P-C, SAC §4.4).

### 4.2 Delivery, perception and decoding of an instruction

Follows SAC §10.3; the behaviour is:
1. **Recipients.** `Apply` resolves the target into recipients in ascending `NpcId` (4.5), capped by `Divinity.Instruction.MaxRecipients` (default 32; more is refused `bad_target`).
2. **Perception.** Each recipient must be a living person (`Bodies.IsLivingPerson`). An asleep recipient perceives it on waking (tuning `Instruction.WakeDelivery`, default on: a dream or vision). Possessed recipients cannot be instructed (`already_controlled`). The draw `divine-perceive(npc, intervention)` against `Instruction.PerceiveChance` decides; defaults: suggestion 800 per mille, command 1000 (a command is unmistakable). `CommandPerceived` is emitted for each perceiver.
3. **Decoding.** A perceived instruction is decoded into `DivineMind.Inbox` (cap 4, oldest evicted) with M5's decode-noise model (`divine-decode`): a suggestion is decoded with the M5 field-error chance raised by `Instruction.SuggestionNoisePermille` (default 100), so an act or topic can be misread (the `Mismatch` truth flag is analytics only); a command uses `Clarity` 255 (no noise by default). The decoded topic is communicated knowledge, so `Thinker.Known` accepts its target (the god "shows" the place or thing).
4. **Who sent it.** The decoded entry carries the true god id, but the **mind uses it only through its beliefs**: the divine term (4.3.1) weights it by the recipient's `GodBelief` for that god. An NPC with no belief in that god gives it little weight: in effect "an unknown power".
5. **Witnesses.** A command is visible: people within `CommsRange.Gesture` of the recipient who attend it perceive `SituationKind.DivineSign` (80) and record a `Witness` exposure (4.8). A suggestion has no witnesses.
6. **Reading.** The inbox is read at the recipient's first think after the tick of delivery (M5 rule 3), through `DivinityMindHooks.FillDivine` into `ThinkScratch.Divine[2]`.

### 4.3 The six intervention types

| Type | Commands (SAC §10.2) | What changes in state | How the NPC decides | Ends |
|---|---|---|---|---|
| **Suggestion** | `IssueSuggestion { God, Act, Target, Scope, ExpireTicks }` | Recipients' inbox and exposures; a communicated belief about the target | Its own policy scores a candidate for the suggested act with a small divine term (4.3.1) | Expiry (default 1 day), followed, evicted |
| **Command** | `IssueCommand { same }` | As suggestion, plus witnesses' exposures | Same decider, a larger authority weight; still refusable | As suggestion |
| **Compulsion** | `IssueCompulsion { God, Act, Target, ExpireTicks }`, `CancelCompulsion` | A `ControlEntry` of kind `Compelled` plus an `Instruction`; Mythic | The mind still runs and scores; the override puts the compelled candidate in place of the policy's choice while it exists (4.3.3) | `Completed`, `Timeout`, `Cancelled`, `Death`, `MythicExhausted`, `Superseded` |
| **Possession** | `BeginPossession`, `EndPossession`, `DivineControlInput` | A `ControlEntry` of kind `Possessed`; Mythic upkeep | The player's inputs replace the choice; perception, memory and the body continue | POSSESSION_AND_AVATAR_DESIGN |
| **Commandment** | `RevealCommandment`, `ReviseCommandment`, `RevokeCommandment` | A commandment record; recipients' commandment knowledge | Held interpretations add to the divine term and a feature key; adopted, followed, ignored or forgotten per NPC | COMMANDMENTS_AND_RELIGION_DESIGN |
| **Miracle** | `CastMiracle { God, Kind, Target, Magnitude }`, `Resurrect` | Authoritative physical state (body, goods with the miracle ledger, sources, huts, life) | No decision: it happens. Perceivers interpret it (4.8, 4.9) | Applied once, or a timed modifier that lapses |

(`ManifestAvatar`/`DismissAvatar` form a seventh family, `InterventionKind.Avatar`, for the god's own body: POSSESSION_AND_AVATAR_DESIGN.)

#### 4.3.1 The divine decision term

SAC §10.3 adds one score term, `CandidateScore.Divine`, to `AssociativeNpcPolicy`'s total (0 by default, so choices are unchanged), with an `OpportunitySlot.Divine` column so the inspector's "why" line adds up:

```text
score(c) = Instinct + Learned + Social + Culture + Divine + Personality - Cost + Explore
Divine(c) = clamp( authority(kind) x Belief x max(Trust, Fear) / 1e6 , -2000, 2000 )     // D2: an instruction matching c
          + clamp( commandment term , -2000, 2000 )                                     // D6: COMMANDMENTS_AND_RELIGION §4.4
```

- `Belief`, `Trust`, `Fear` are the recipient's own `GodBelief` fields for the sending god (COMMANDMENTS_AND_RELIGION §3). `authority(kind)` is `Instruction.Authority.Suggestion` (default 1500) and `.Command` (default 3000), in vp at full belief and full trust or fear; so a convinced, trusting or frightened believer feels a command at up to 3000 × 1000 × 1000 / 1e6 = 3000 vp, capped at 2000. An NPC with belief 200 and trust 300 feels it at 180 vp.
- **Always refusable.** 2000 vp is the instinct bound: learned values (±4000 per entry, summed over a context) and strong drives can outweigh it, so a starving, frightened or sceptical NPC refuses. Negative trust with no fear makes `max(Trust, Fear)` negative, and the instructed candidate becomes *less* attractive: deep distrust breeds defiance.
- **The candidate.** `DivinityMindHooks.AddCandidates` (quota 4, after Civ and Orgs, SAC §14) adds the walk-then-act candidate that `DivineActs.Map` names (4.4), built through `Affordances.Of` (`Sim/Cognition/Affordances.cs`). A blocked affordance (`NoTool`, `NotHeld`, `Empty`, `Unreachable`) gives no candidate; the instruction then lapses unless the situation changes.
- **Feature key.** The instructed candidate carries `Feature.Divine(Suggested 1 | Commanded 2)` (`FeatureKind.Divine = 17`), so the learner learns, per verb, whether doing what a god says has paid (4.9). D2 adds no reward of its own; an innate prior for `Commanded` exists as tuning (default 0).
- **Responses.** `CommandResponded { Intervention, Npc, Response }` with `Followed` (it started the matching intent), `Refused` (the instruction was its own candidate and another won at the first think after reading), `Ignored` (perceived but never a candidate, e.g. blocked), `Lapsed` (expired unread or unfollowed), `Misread` (it followed a misdecoded act). Exactly one per perceiver.

#### 4.3.2 Suggestion versus command

They share delivery and the term, and differ in perception chance, decode noise, authority weight, witnesses, cost (MYTHIC §4.5) and visual cue: a faint glow on the recipient that only the player sees, versus a visible beam that witnesses also see (VISUAL_WORLD_DESIGN). A command refused in public is itself something witnesses may talk about (4.8).

#### 4.3.3 Compulsion

Behaviour behind SAC §11:
- **The mind still runs.** The compelled NPC perceives, updates beliefs, attends and builds candidates; `DivinityMindHooks.AddCandidates` places the compelled act as a candidate (first divine slot). After the policy step, `OverrideIntent` replaces the policy's choice with that candidate while it exists. The decision record carries `Control = Compelled`.
- **What it overrides:** drives, instincts, learned values, culture, social memory and permission queries. A compelled NPC may walk past food while starving, or take from a store it believes it may not use (a physical take that C1 classifies as theft from the owner's view; the record shows compulsion).
- **What it never overrides:** the physical checks of `Match`/`Start`/`Act` and `Goods` (location, reach, possession, room, skill or tool), death, being asleep and collapsed, being busy (a running meal finishes). A compelled act that is physically blocked sets `Status = Blocked` (`CompulsionBlocked`), retries at each think, never teleports, never creates goods, and runs on until expiry or cancellation.
- **"Still needs to move, hold the materials and act"** (brief §8): a compelled `Give(fish, child)` while holding no fish stays blocked; the player first compels `Take(fish lot)`, which walks there and picks it up, then issues the `Give` (the same god's new compulsion supersedes the finished one). Multi-step compulsions in one command are a possible later amendment.
- **Ends:** `Completed` (the compelled intent started and its action ended `Done`), `Timeout` (default `ExpireTicks` 600), `Cancelled`, `Death`, `MythicExhausted`, `Superseded` (the same god's new compulsion, or a possession). One per body; another god's is refused `already_controlled` (SAC §11.1).
- **Learning:** the trace carries `Feature.Divine(Compelled 3)`; rewards scale by `Divinity.Instruction.CompelLearnPermille` (default 1000: the NPC learns from what its body did); afterwards the base feature `SelfFeature.Compelled` (52) stays for `MemoryDays` (default 2), so the NPC can associate what followed with having been compelled.

### 4.4 The divine action vocabulary (brief §9)

There is **one** implementation of each action: the NPC's. `DivineActs.Map(act, target)` (D2) maps a `DivineAct` (SAC §6.1) to an existing candidate shape (learning verb, `NpcVerb`, `IntentTarget`), and the candidate goes through `Match` → `Start` → `Act` like any intent. An act whose verb is not yet on the branch is refused at `Validate` with `act_unavailable` (the HUD greys it out), never faked.

| `DivineAct` | Existing verb / primitive (WORLD_ONTOLOGY §3) | Status at `8617eaa` | Completed by |
|---|---|---|---|
| MoveTo 1 | `MoveTo` inside every verb (`PopulationSystem.WalkTo`, `NodePather`); mapped to `Verb.Approach` (7) on `IntentTarget.Node` | Walking exists; `Approach` has no `Act` case yet | D2 (adds it through `TryAct`) |
| Approach 2 | `Seek` (54, M5) / `Approach` (7) on a person | Exists (M5 `Seek`) | — |
| Inspect 3 | Attention (`Thinker.Attend`); `Inspect` (WORLD_ONTOLOGY, M8) | Attention exists; `Inspect` planned. Tier 1 "look at": the target joins the recipient's attended set for `Instruction.LookTicks` | D2 (look), C1 (inspect) |
| Gather 4 | `Gather` (9), `ForageAt` (10) | Exists | — |
| Take 5 | `PickUp` (15), `TakeFromStore` (18, M4 `UseStore`), `Claim` | Exists | — |
| Drop 6 | `Goods.Move(... MoveReason.Drop)`; verb `Drop` (16) reserved | Primitive exists (set-down, death spill); no intent row | D2 (intent row) |
| Give 7 | `Give` (48, M5), `Feed` (M4), `HandOver` | Exists | — |
| Eat 8 | `Eat` (1) | Exists (no drinking need) | — |
| UseTool 9 | `Equip` (19), then `Gather` with the axe | Exists (implicit equip, WORLD_ONTOLOGY O6) | — |
| Sleep 10 | `Sleep` (2), `SeekShelter` (6) | Exists | — |
| Work 11 | `Work` (96, C3), `WorkShift` (14) | Planned | C3 |
| Construct 12 | `Build` (12) for huts; sites and contracts later | Huts exist | C1 (buildings) |
| Trade 13 | `Say(Offer)` (66), deals, `HandOver` (49) | Exists (M5 honest barter) | — |
| Cooperate 14 | `Say(Cooperate)` (71); `Help` (C2) | Partial | C2 |
| Communicate 15 | `Say(act)` for the M5 acts (`GestureAct`, `Sim/Comms/Message.cs`) | Exists for M5 acts | — |
| Protect 16 | `Defend` act (no candidate yet); guard roles | Planned | C4, C9 |
| Retreat 17 | `Flee` (4); military `Retreat` | Exists (`Flee`); military subset exists for soldiers | — |
| Attack 18 | civilian contest (GAME_THEORY, `Social/Contests`); `NpcVerb.Attack` for soldiers | Planned for civilians; exists for soldiers | C4 |
| Teach 19 | `Teach` (108, C7) | Planned | C7 |
| Treat 20 | `Treat` (107, C7); `Tend` (M4 household care) | `Tend` exists; `Treat` planned | C7 |
| InstitutionalAction 21 | the organisation's own task verbs (C6) | Planned | C6 |

**Tier 1 subset** (enough for scenarios 8 and 9): MoveTo, Approach, Inspect (look), Gather, Take, Drop, Give, Eat, UseTool, Sleep, Trade, Communicate, Retreat, and Treat as `Tend`. Rows become available as the C packages land their verbs; the mapping is data, so a new row needs no new divine code.

### 4.5 Targets, scope and expiry

Every instruction has an **act, a target, a scope and an expiry**. `DivineScope` (SAC §10.2) says how the target becomes recipients, resolved once at `Apply`, ascending `NpcId`:

| `DivineScope` | Recipients | Notes |
|---|---|---|
| `Individual` | the target person | Must be a living person, not possessed |
| `Household` | its members at apply time | A child too young for the act has no affordance, so it never becomes a candidate for it |
| `Group` | the explicit id list the HUD sends (≤ 32) | Never a live predicate, so replay is exact |
| `Org` | relayed to office-holders and members (4.6) | The organisation decides through its people |
| `Location` | living persons within `Target.Radius` of `Target.Node` at apply time | A standing location instruction (people who enter later) is a possible later amendment |

The act's object is the `DivineTarget` (kinds `Npc, Household, Org, Location, Lot, Bin, Source, Hut, Building, Commandment, Self`). **Expiry** is `ExpireTicks` (default 1200 = one day; max 10 days). Each recipient decides alone; "any one of you" is the recipients' own business (once someone has fetched the water, the others' candidates lose their affordance or their reason).

### 4.6 Organisations and governments: comply, modify, refuse (scenario 15)

A god has no administrative authority (D5.1, D5.2). An instruction to an organisation is **communication with its people**, decided by the organisation's own process.

1. **Relay.** For `Scope = Org`, D2 asks `Orgs.RelayRecipients(org, act)` (C6) for the office-holders whose powers cover the act, plus members present at the meeting place under a meeting rule, and delivers an individual instruction to each, emitting one `InstructionRelayed` per recipient (SAC §10.2).
2. **Perception is individual.** Each recipient perceives and decodes by 4.2. What the organisation "knows" of the god's wish is what its office-holders perceived and bring to its decisions: their minds, never the god's command record.
3. **Deciding.** The organisation's policy sees the request only through its view, which C6 builds from its own records and its member-manager's own mind (SAC §9). The manager or council member weighs the request with the divine term above, inside the organisation's ordinary decision (its budget, its other objectives, the decision-maker's own interests: pay, standing, kin, material position). The outcome is one of:
   - **Comply:** the organisation issues its own ordinary commands that carry out the request (fund a post, found a service, post work);
   - **Modify:** it issues different or smaller commands (half the budget, another site, later);
   - **Refuse:** nothing;
   - **Defer:** it keeps the request until expiry and decides again.
   Under a council `DecisionRule` (C6) each member decides individually and the rule aggregates.
4. **Record.** The organisation's own decision event (C6's organisation events; for a government, C10's `GovernmentDecision`, 592) carries the `Intervention` id and the outcome, so the warehouse links request and answer. The player sees the answer in the world (the clinic is or is not funded) and in the organisation panel.
5. **Compelling or possessing a leader is individual.** A compelled or possessed leader's *own* acts are controlled (it signs the order), but governance, records and other members carry on: a council may overrule, a successor may reverse, members remember who did it.
6. **Until C10.** The stage 3 faction AI (`AiBrain`) remains the temporary government of every faction until C10's government organisations take its decisions over (SAC §2.1). `AiBrain` has no mind and receives no divine instructions; scenario 15 is therefore first demonstrated with a C6 organisation (an elders' council, a communal store) and then with a C10 government.

### 4.7 Separation of authorities (D5.2, D5.15)

| Authority | Held by | Changed by | A god's relation to it |
|---|---|---|---|
| **Religious** | gods (revelation), religious organisations and their priests (interpretation, ritual), believers (adherence) | revelation, preaching, the religious organisation's own process | A god reveals and works miracles; priests and believers may disagree |
| **Political** | government organisations and their office-holders; citizenship spells (`CivState.Citizenship`, C5) | their own processes | None directly; only through the people who hold it |
| **Economic** | owners (`ItemLot.Owner`; organisations through `OwnerKind.Org`) | `Goods` transfers, contracts, estates | None: a god owns nothing; a miracle's created goods are ownerless on the ground or the recipient's (MYTHIC §6.8) |
| **Institutional** | an organisation's charter, governance and roles | its own decisions | None directly; requests through 4.6 |

Community of origin (`Npc.Faction`), citizenship (C5), religious affiliation (D6) and cultural membership (C10) are four separate records (SAC §3); a change of one never changes another automatically (scenario 16). **No divine command writes citizenship, `Npc.Faction`, an organisation's roles or budget, a law, or an ownership record** other than the miracle transfers of MYTHIC §6 with basis `Miracle`.

### 4.8 Divine signs, witnesses and communication

- **Signs.** Every intervention a person could see leaves a perceptible sign at its node for a short time (a command's beam, a miracle's light, a strike's flash). Population's sense seam (`PopulationExtensions.SenseExtra`, SAC §6) offers it to people within sight (loud ones, such as a smite, within hearing) as `SituationKind.DivineSign` (80), and a smite of another as `SmiteWitnessed` (83). An NPC that attends it records a `Witness` exposure and updates its beliefs (COMMANDMENTS_AND_RELIGION §4.2). D3 keeps the transient signs in `DivinityState.Miracles` (a 64-slot ring).
- **Telling others.** Witnesses communicate through ordinary M5 gestures with `TopicKind.God` (8): `Praise` (the god did well by someone: a healing, a gift), `Warn` (danger: a smiting, an angry power), and, for commandments, `Say(Teach)` with `TopicKind.Commandment` (7) (COMMANDMENTS_AND_RELIGION §4.5). Speaking is the speaker's own decision; listeners decode with M5's misunderstanding, so stories change in the telling. A listener records a `Told` exposure; its beliefs change at hearsay gain, its associations do not.

### 4.9 Divine learning (brief §14)

**Rule: the NPC attributes outcomes itself.** No path writes the lesson the player intended. Divine interventions reach learning only through the existing channels of [NPC_LEARNING_DESIGN.md §4.4](NPC_LEARNING_DESIGN.md#44-reward-and-punishment-signals) and the context keys reserved by SAC §14:

| Intervention | What the NPC receives | Context it learns in |
|---|---|---|
| Smite | Injury and the pain and fear drive rises (`Learning.Injury`) | Its live traces (what it did in roughly the last 12 thinks) and open episodes; then `SelfFeature.Smitten` (48) for `Miracles.MemoryDays` |
| Heal, bless | Pain and fear relief; illness ended | Live traces; `SelfFeature.Healed` (49), `Blessed` (50) |
| Create resources | Goods it later picks up and uses (realised value) | The decisions that took them |
| Destroy resources | Goods lost (booked against the owner) | Live traces |
| Suggestion or command followed | Whatever the act brought | Keys include `Feature.Divine(Suggested 1 / Commanded 2)` |
| Compulsion, possession | Whatever the body's acts brought, at `CompelLearnPermille` / `Possession.LearnPermille` | Keys include `Feature.Divine(Compelled 3 / Possessed 4)`; then `SelfFeature.Compelled` (52) / `Possessed` (51) |
| Commandment held | Whatever following or breaking it brought | Keys include `Feature.Divine(CommandmentRequired 5 / Prohibited 6 / Encouraged 7)` |
| Prayer answered | The relief the miracle brings closes the `Pray` episode | `Pray` (116) in the context it prayed in (COMMANDMENTS_AND_RELIGION §4.6) |

**"The sim never tells the victim why"** (SAC §8): the trace credit puts a smite's punishment on whatever the NPC did recently. If the theft was two minutes before and the gathering one minute before, the gathering takes more of the blame: the NPC may associate punishment with the wrong action (brief §14).

**Attribution step (D2, optional, `Instruction.Attribution`, default on).** A believer also tries to *explain* a divine reward or punishment aimed at itself, from its own mind:
1. Among its own recent decisions (`Mind.Experiences`, the 8-slot ring, and the trace ring) within `AttributionWindowTicks` (default 1200), newest first, it looks for one that an interpretation it holds covers with the matching sign (a punishment after an act its version of a commandment prohibits, a blessing after one it encourages), else a divine instruction it refused, else nothing.
2. If found, the reward is also applied to that decision's keys as a closed outcome (`Learner.Outcome` with episode key `EpisodeKey.Make(36, intervention)`, kind 36 from Divinity's spare range 32–39), at gain `AttributionGainPermille` (default 600), scaled by its belief in the god.
3. If not, only the ordinary traces carry it.

Because the explanation comes from the NPC's *own* interpretation and memory, it can be wrong: a misheard variant ("do not gather" for "do not steal") blames the gathering; an NPC that knows no commandment learns only what its traces held; a sceptic (low belief) barely attributes at all. The event (`DivineAttribution`, a D2 row in its range 818–824) records which decision it chose, the basis, and the truth field `MatchesDeclared` against the god's optional `DeclaredReason`, so experiments can measure how often attribution matches intent.

**Witnesses learn vicariously** (NPC_LEARNING §4.13, M7): seeing someone struck is the visible outcome `Struck`; the observer's context uses what it saw the victim do (`SeenVerbs`), so it learns "taking from that store is dangerous" only if it saw the taking. Its fear drive rises and its beliefs change. Told NPCs change beliefs but not associations.

**Culture.** Lessons become cultural only through the defined routes: a commandment (COMMANDMENTS_AND_RELIGION) or a norm or story (C10, CULTURAL_TRANSMISSION). Many NPCs separately learning "theft near the shrine hurts" is not culture (CULTURAL_TRANSMISSION §4.9 rule 4).

**The causal record.** One `InterventionId` per accepted divine action (SAC §1) is carried by: `DivineActionAccepted` (807); the issue event (`DivineSuggestionIssued`, `DivineCommandIssued`, `CompulsionStarted`, `MiracleCast`, ...); every state change it causes (`MiracleEffectApplied`, `ResourceCreated`/`ResourceDestroyed`, `InjuryTaken`, `NpcDied` with its attribution in `MiracleEffectApplied`); each perception (`CommandPerceived`, `InstructionRelayed`; for witnesses `DivineSignPerceived`, a D2 row 819, sampled at Standard, all at Full); each attribution (`DivineAttribution`); each belief change (`BeliefChanged` with cause refs, SAC §13.1); each response (`CommandResponded`, the organisation's decision); each action started under control (`Control` code on the action events, SAC §15.1); and learning updates (`AssociationUpdated` gains `CauseIntervention`, filled at Full recording from the NPC's exposure ring: the most recent exposure before the trace's decision or the reward). The warehouse joins them into one chain: intervention → perception → attribution → learning updates → communication → later behaviour ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md) §12, Q1). Reading the exposure ring for telemetry changes nothing (pin P7).

### 4.10 The four interaction modes against one sim

| Mode | What the player does | Sim state involved | View |
|---|---|---|---|
| **A Overhead** | Free camera from the island to one person; select people, objects, places; issue instructions and miracles | Commands only | Overhead camera (CAMERA_AND_CONTROLS); toolbar and radial menu (PLAYER_UI) |
| **B Avatar** | Manifest a body, walk, carry, give, gesture, demonstrate | The god's avatar body and its `ControlEntry` (`AvatarBody`) | Third-person camera; back to A on dismissal or destruction |
| **C Possession** | Control one person's available actions | A `ControlEntry` (`Possessed`) | Over-the-shoulder camera; back to A at the end |
| **D Analysis** | Reports, statistics, history, inspection | None (reads `IDivineView` and, labelled separately, institutional records) | Reports (PLAYER_UI) |

**One world.** Modes are presentation states plus at most one embodied focus per god (SAC §11.1: manifesting ends a possession and the reverse). No mode copies a person, forks the world or runs a second sim.

**Divine observation (D5.28, SAC §9).** `IDivineView` is filled by `DivineViewFiller` from the god's `DivineObservationProfile`: per domain `Omniscient`, `Perceived`, `Reported` or `Blind`, through the channels `Avatar, Possessed, Prayer, ConsecratedSite, Followers, Records, Sight`. Sandbox default: `Omniscient` for physical state and minds; competitive defaults are a scenario choice. Mode D shows divine observation and, in a separate labelled column, what the faction's institutions record (layer 7), which may be incomplete or wrong. Divine knowledge never becomes NPC or institutional knowledge except through a physical in-sim step (a miracle writing a belief, an instruction perceived). No institution or faction policy reads `IDivineView`; no god policy reads `GodState`, the warehouse or the debug overlay (I-D5-1, I-D5-2).

### 4.11 AI gods

- **Same rules** (D5.26, D5.30): `GodDirector` (system order 910) seats every god whose seat is `Ai` with its policy, thinks every `Divinity.AiThinkTicks` staggered by god id, draws from `god-policy(god, tick)`, and submits inside `Step`. A dropped inadmissible command is counted, never applied (SAC §12).
- **`IdleGod`:** never acts (Tier 1 default; the civilisation must run without gods).
- **`StewardGod`** (D1, Tier 2, scripted): answers prayers from its believers when Mythic allows (heals the injured or ill who prayed, creates food near a hungry household that prayed), reveals one template commandment every `RevealEveryDays` (default 10), and smites at most once a day an act it observes that breaks one of its commandments. It is a benchmark-style opponent, not a balanced one.
- **Later:** trained god policies with the same view and commands ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md) §14). No training in this run (D5.31).

---

## 5. Interactions with other systems

| System | What flows |
|---|---|
| `Core` | `DivineCommand` handlers through `GodSim.Submit`; pending queue and hash; `GameSetup.Control`, `HumanGod`; the god-only gate |
| `Cognition` | Inbox read into `ThinkScratch.Divine`; divine candidates through `Affordances.Of`; `CandidateScore.Divine`; `SituationKind` 80–83 from the sense seam |
| `Learning` | `FeatureKind.Divine` (17) values 1–7; `SelfFeature` 48–52; episode kinds 32–39 (32 instruction, 33 commandment, 34 compulsion, 35 possession; 36 attribution and 37 prayer proposed here from the spares) |
| `Comms` | Witnesses' `Praise`/`Warn` with `TopicKind.God`; `Say(Teach)` with `TopicKind.Commandment`; M5 decode noise for instructions |
| `Divinity/Control.cs` | Compulsion, possession and avatar spells (SAC §11) |
| Organisations (C6, C10) | `Orgs.RelayRecipients`, `InstructionRelayed`, the organisation's own decision events with the intervention id |
| `Population` | Recipients, bodies (`Bodies`), deaths, resurrection |
| `Information` | `IDivineView`, `DivineObservationProfile` (D1) |
| `Telemetry` | Section 7 |

---

## 6. Player and view

- **Distinguishable at a glance** (brief §29): suggestion (a faint glow on the recipient, seen only by the player), command (a visible beam that witnesses also see), compulsion (a binding aura while active), possession (the god's colour inside the body; camera change), miracle (its own effect). VISUAL_WORLD_DESIGN owns the look; NPC_ANIMATION_DESIGN owns reactions (fear, submission, praying, refusing).
- **Feedback is the NPC's choice, shown.** After a radial-menu instruction the player sees each recipient's response as it happens: followed, considering, refused (the head-shake of the `Refuse` vocabulary), unable (a small icon of the `Blocked` reason). The inspector's "why" line shows the `Divine` term.
- **Organisations answer in the world.** The organisation panel lists divine requests and decisions, with who decided.
- **Mode D labels its sources:** divine sight versus the institutions' records.
- Unity animates events only; it never decides perception, responses or outcomes.

---

## 7. Telemetry

Codes and names are SAC §15.1 (D1 800–809, D2 810–824). This document defines the behaviour of:

| Event | When | Key fields |
|---|---|---|
| `GodCreated` (800), `GodAssociated` (801), `GodPolicyChanged` (802) | first divinity tick; seat or policy changes | god, origin faction, seat, policy |
| `DivineActionAccepted` (807) | one per intervention | intervention, god, kind, act, target, scope, cost |
| `DivineSuggestionIssued` (810), `DivineCommandIssued` (811) | at apply | intervention, recipients; truth: `DeclaredReason` |
| `InstructionRelayed` (812) | one per organisation recipient | intervention, org, npc, office |
| `CommandPerceived` (813, HV) | per perceiver | intervention, npc, clarity; truth: `Mismatch` |
| `CommandResponded` (814) | once per perceiver | intervention, npc, response, chosen verb, divine term |
| `CompulsionStarted` (815), `CompulsionEnded` (816), `CompulsionBlocked` (817) | spell start, end, block | intervention, npc, act, reason, blocked why |
| `DivineAttribution` (818, proposed D2 row) | attribution step | intervention, npc, attributed verb and tick, basis; truth: `MatchesDeclared` |
| `DivineSignPerceived` (819, proposed D2 row, HV) | a witness attends a sign | intervention, npc, role, distance |
| `AssociationUpdated` (extended) | as now | `CauseIntervention` (Full only) |

**Snapshots:** daily god row (`MythicBalanceDay`, 809; interventions by kind; responses by kind); the person-day row gains divine messages perceived, followed, refused, and ticks under control.

---

## 8. Invariants

| Id | Invariant | Checked by |
|---|---|---|
| I-DIV-1 | Every divine state change happens in a divine command's `Apply` or in `DivinitySystem` (order 230) processing an open record, in ascending id; all divine inputs are in the pending queue | Static test; replay test |
| I-DIV-2 | Suggestions and commands change only recipients' inboxes, exposures, communicated beliefs and divine beliefs; the choice is the NPC's policy | Unit test: with the recipient's policy forced to `Rest`, nothing else changes |
| I-DIV-3 | No compelled, possessed or avatar act bypasses `Match`/`Start`/`Act`/`Goods`; every `ItemMoved` passes the C6 reach check (SAC §7.2 C8) | Event-driven check |
| I-DIV-4 | Gods are never removed; `Gods.Count == FactionCount` throughout (G1) | `SimInvariants` |
| I-DIV-5 | A god holds no ownership, office, citizenship or membership (G3) | `SimInvariants` over owners and rosters |
| I-DIV-7 | Persons enter only by birth, immigration and resurrection of an existing id (P-C) | `SimInvariants` |
| I-DIV-8 | No `IGodPolicy` references `GodState`; no NPC, institution or faction policy reads `IDivineView` (I-D5-1, I-D5-2) | Reflection tests |
| I-DIV-10 | No divine command and no affiliation change writes citizenship or `Npc.Faction` | Static test; scenario 16 |

---

## 9. Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.

Package ids are the brief's (§39), as used by SAC; tiers are §42's.

| Capability | Package | Tier |
|---|---|---|
| Gods, seats, `IdleGod`, god-only gate (F1 types), `IDivineView` with the `Omniscient` profile, `SwitchGod`, `SetGodPolicy` | D1 | 1 |
| Suggestions and commands: perception, decoding, divine term, `Feature.Divine` 1–2, responses, witnesses' signs | D2 | 1 |
| Compulsion through the control seam, `CompelLearnPermille`, `SelfFeature.Compelled` | D2 | 1 |
| `DivineActs.Map` for the Tier 1 subset; `Approach` act case; `Drop` intent row | D2 | 1 |
| Scopes `Individual`, `Household`, `Group`, `Location` | D2 | 1 |
| Scope `Org` with relay and organisation decisions | D2 + C6 (C10 for governments) | 2 |
| Attribution step and exposure ring; causal fields | D2 | 1 |
| Witnesses' `Praise`/`Warn` about gods (`TopicKind.God`) | D2 + D6 + U3b | 2 |
| `StewardGod` | D1 | 2 |
| `Perceived`, `Reported`, `Blind` observation profiles | D1 + Q1 | 3 |
| Trained god policies | — | later (D5.31) |

---

## 10. Migration from the existing code

All structural changes are F1's (SAC §0.1): the `Divinity` module and state slot, `DivineCommand` types, the policy level `God`, the control seam, the id ranges and the extension sites. On top of F1:
- `AssociativeNpcPolicy` (`Sim/Policy/AssociativeNpcPolicy.cs`) sums `CandidateScore.Divine` (0 unless a `Divine` mind part exists, so every existing scenario is unchanged).
- D2 fills `DivinityMindHooks` (`FillDivine`, `AddCandidates`, `DivineTerm`) and `DivineActs.Map`, adds the `Approach` act case and the `Drop` intent row, and the exposure ring in `DivineMind`.
- The human seat moves in god-only mode; the Stage 3 mode and its tests stay as the listed temporary mechanism.

---

## 11. Tests and acceptance hooks

Unit tests run under `dotnet test DotNet`; *runner* marks headless scenarios. Each names the brief §47 scenario it feeds.

| Id | Test | Pass | Scenario |
|---|---|---|---|
| DP1 | **Command perceived and decided.** A hungry NPC with food in sight receives `IssueCommand(Gather logs)` from a god it believes in at 800 with trust 600 | One `CommandPerceived` and one `CommandResponded`; with its food values and hunger instinct zeroed it follows in ≥ 90% of 100 seeds; with normal values and hunger in the pain band it refuses in ≥ 60%; the decision event shows the `Divine` term | 8 |
| DP2 | **Defiance from distrust.** Same NPC with trust −800, fear 0 | Follows in ≤ 10% of seeds; the divine term is negative | 8 |
| DP3 | **A suggestion weighs less than a command** | Follow rate of a suggestion ≤ that of a command in every paired seed, 100 pairs | 8 |
| DP4 | **Compulsion overrides hunger, not physics.** A starving NPC compelled to take a log 10 nodes away | It walks past food and takes it (`CompulsionEnded(Completed)`); Mythic falls by the configured cost; a compelled `Give` of a fish it does not hold stays `Blocked` to expiry with no `ItemMoved`; every compelled move passes C6 | 9 |
| DP5 | **Compulsion ends on death and on Mythic exhaustion** | Both reasons emitted exactly once; no further compelled intents | 9 |
| DP6 | **An organisation refuses or modifies.** A request to fund a healer post, to a C6 organisation whose manager has belief 0, and to one whose manager has belief 900, trust 700 and a tight budget | First: no command (`Refuse`); second: a smaller budget line (`Modify`) or `Comply` within budget; no budget changes except by the organisation's own command | 15 |
| DP7 | **Authorities separate.** 1,000 random divine commands of every kind | No change to citizenship, `Npc.Faction`, organisation rosters, laws or non-miracle ownership | 15, 16 |
| DP8 | **Wrong attribution is possible.** An NPC steals food, then gathers wood, and is smitten 300 ticks after the theft; arm A holds "do not steal", arm B a misheard "do not gather", arm C none | A attributes to the theft, B to the gathering, C has no attribution and only its live traces are credited, in 100% of seeds; ten days later A's theft rate in that context is below C's in ≥ 80% of 50 seeds | 12, 13 |
| DP9 | **Witnesses learn and tell.** A smite seen by 3 NPCs | 3 `DivineSignPerceived`; each witness's fear rises; with M5 gestures on, at least one `Warn` or `Praise` with `TopicKind.God` within 2 days in ≥ 80% of seeds; the warehouse joins the chain | 12 |
| DP10 | **Same rules for AI and human.** The command types an `IGodPolicy` may emit equal the HUD's; an AI god with 0 Mythic has every paid command refused `mythic_short` | 8, 9, 18 |
| DP11 | **God persistence.** A faction goes extinct | Its god remains, Mythic is credited daily, commands to foreign people are accepted; no person is created | — (D5.5) |
| DP12 | **Neutrality.** Pins P1, P2, P3 (SAC §16) | As stated there | 18 |
| DP13 | **Replay.** *runner*: a 5-day scenario with ordinary play and one of every Tier 1 divine command, replayed from the log; also saved and loaded mid-compulsion with an unread inbox | Identical hashes at every checkpoint | 18 |
| DP14 | **One world across modes.** Overhead → avatar → possession → overhead in one run | Person ids and counts unchanged except by the commands' recorded effects | 10, 11 |

---

## 12. Open questions

Minor questions are settled with configurable defaults above (authority weights, perception chances, decode noise, expiry, attribution window and gain). One consequential question remains for the owner:

- **Default divine observation in competitive play.** Sandbox uses `Omniscient`. Should the VP mode and AI-versus-human play default to `Followers`/`Perceived` (what the god's believers perceive), which makes believers strategically valuable, or stay `Omniscient`? Default until answered: `Omniscient` everywhere, the others available as scenario profiles once D1 builds them.

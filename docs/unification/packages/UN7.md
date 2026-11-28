# UN7: Exchange and communication (offer, promise, hand-over, claims as beliefs)

Package spec for the Unification milestone (owner D9, 28 Nov 2026; owner brief §40 field set). Self-contained. Binding sources, in order: [LATEST_OWNER_AMENDMENT.md](../LATEST_OWNER_AMENDMENT.md), owner decisions D9.1–D9.14 and D10 in [OWNER_DECISION_REGISTER.md](../../civdiv/OWNER_DECISION_REGISTER.md), [LEAD_RULINGS.md](../../civdiv/LEAD_RULINGS.md) R72–R84, [UNIFICATION_DESIGN.md §D9](../UNIFICATION_DESIGN.md) ("§D9", controlling), [SHARED_ARCHITECTURAL_CONTRACTS.md §23](../../civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md) ("§23", binding numbers and ownership), [UNIFICATION_PACKAGE_INDEX.md](../UNIFICATION_PACKAGE_INDEX.md) ("the index"; where this spec and the index disagree, the index wins), this file. Supporting: [CATALOGUE_MANIFEST.md](../CATALOGUE_MANIFEST.md) and [D9_CODE_CONFLICT_AUDIT.md](../D9_CODE_CONFLICT_AUDIT.md) ("the audit"). **D9 in one line:** hard-code physics and biology, never behaviour or social convention: no title, permission, automatic reciprocity or scripted strategy; the twelve primitive controls and twelve needs only. Paths are under `Assets/God/Sim/` unless they start with `Assets/`, `DotNet/` or `docs/`; new code lives under `Unified/` (namespace `God.Sim.Unified`).

| Field | Value |
|---|---|
| Package identifier | **UN7** (Tier UA, Wave 2, Checkpoint UA-mech; training stage S7 of UN6t-b) |
| Branch / worktree | `unity/un7` in `C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs-un7`, from `unity/poc` after **UN0** merged |
| Effort | 3.0 agent-hours (planned H8.6–H11.6; merge H12.3 after UN2 and UN6) |
| Status | Not started. Replaces the D8 "UN7 catalogue and recipes" spec (catalogue mapping is UN3a, recipes and furnishings UN3b, breadth UB2) |
| Recommended agent | **Opus** (communication seam, perception and belief rules). Never Fable |

## 1. Objective
Make exchange exactly what D9.5 says it is: **communication plus separate voluntary hand-overs**. `GESTURE` carries an act (`Offer`, `Promise`, `Want`, `Accept`, `Refuse`, `Claim`, …) with referents (ware or lot, quantity, person, place) and an optional correlation id; a transfer is the generic physical hand-over (giver `USE` lot on person, slot *hold out*, then the receiver's `TAKE`); nothing is atomic, locked, enforced, owed or titled. Claims are beliefs held by people and communicated by gesture; observers update belief and trust from what they perceive, never from a ledger.

## 2. User-facing outcome
Two persons can signal an offer, hand goods across and (or not) hand something back, each leg a separate physical event the player can watch; a person can refuse, delay, short-change or keep both. The person card (UN10a) later shows the person's own beliefs, never "who owns what". The world has no trade object, deal ledger or debt.

## 3. Design anchors
- Amendment §4 (trade is not a primitive or atomic transaction; receiver's cooperation; optional protocol id only for memory and analytics; agreements subjective; no auto enforcement), §3 (claims as local beliefs or communicated propositions; contradictory claims valid; observers cannot query truth), §5 (a theft is someone's interpretation of a `TAKE`, not an engine offence).
- §D9.1 b item 5, §D9.5 rows A–D (no rights; offer is a gesture; hand-over is hold-out plus `TAKE`; no consent flag), §D9.2 d.
- §23.4 (`GestureAct` 28 `Claim`; `MoveReason.HandOver` 5, `Grab` 18), §23.5 (`TAKE` of a held-out lot, `Resisted`, `Grab`), §23.9 (`MsgExt`, `HoldOut`, `Corr`, claims), §23.12 (events 1280–1299), R81, R82.
- Audit §1.5, §2, §4.5 (call sites), §8 "Exchange".

## 4. Relevant existing source files (verified)
- Communication: `Comms/Messages.cs` (`Composed` at `:14`, `Emit` at `:64`, perceive/noise path from `:137`; **quantity is clamped to 0–9 at `:48`**, which is why unified quantity lives in `MsgExt`; `DealId` at `:23`, `:48`), `Comms/Message.cs` (`GestureAct` incl. `Offer 2`, `Promise 8`, `Accept 3`, `Refuse 4`, `Want 1`, `Surrender 11`, `Accuse 13`, `Teach 18`, `Threaten 26`; `DealId` at `:47`), `Comms/CommsRange.cs`, `Comms/CommsTuning.cs`.
- Social memory and deal memory (to feed, never to enforce): `Social/SocialMemory.cs` (`HasOpenDeal` at `:39`), `Social/SocialSim.cs` (`CheckDeals` at `:431`, `OpenTrace` `:518`, `CloseTrace` `:536`, the hand-over act around `:704`; **own-lot filter `l.Owner.Kind == OwnerKind.Npc …` at `:708`** is a legacy title path to avoid), `Social/SocialCandidates.cs` (`Held` at `:42`, `InDealWith` `:320`: legacy candidates, not used in unified mode), `Social/SocialMind.cs`, `Social/SocialSystem.cs`, `Social/Valuation.cs` (own valuation, read-only).
- Goods: `Population/Goods.cs` (`Move` with `MoveReason.HandOver`, `Fits`, `InReach`; unified branch landed by UN0), `Population/Reach.cs` (`Reach.HandOver = 1` at `:18`, mapping at `:31`).
- Tests to model on: `DotNet/God.Runner.Tests/M5BarterTests.cs` (defection already supported: `:25`), `M5NeutralityTests.cs`, `M5NaturalTests.cs`, `Assets/God/Tests/EditMode/CommsTests.cs`.
- Landed by UN0/UN2/UN6 (read): `Unified/Schema/*` (`Referent`, `PrimitiveControl`, `EffectKind.HoldOut 15`, `MsgExt` shape), `Unified/Motor/*` (`GESTURE` path through `Messages.Emit`, `TAKE` of held-out lots), `Brain.AddClaim` (UN6).

## 5. Allowed scope
1. **GESTURE referents and correlation:** the `MsgExt { Message; R1; R2; Quantity (0–999); Corr }` side table (`UnifiedState.MessageExt`, sorted by message id) written when UN2's `GESTURE` primitive emits; `Corr` allocated by the first gesture's speaker from `UnifiedState.NextCorr++`; **nothing ever reads `Corr` to lock, settle, enforce or reward**; the M5 `Message` layout and `DealId` are untouched (`DealId` unused in unified mode).
2. **Hold-out and hand-over** (`EffectKind.HoldOut` handler in `EffectHandlers.UN7.cs`): `USE` a carried lot on a reachable person (slot *hold out*, optional intended receiver `To`, 0 = anyone) creates `HoldOut { Npc; Lot; To; Corr; Expires }`; the receiver's `TAKE` of that held-out lot completes `Goods.Move(MoveReason.HandOver)` (the same generic move any gift uses); expiry or the giver moving away leaves the lot in the giver's carry (`HoldOutLapsed` 1282); each completed transfer emits `HandedOver` 1281 (giver, receiver, lot, count, corr) independently. The receiver may decline simply by not taking; there is no consent flag and no automatic completion.
3. **`TAKE` from a person:** only a held-out lot, or any carried lot of a person who is asleep, unconscious or dead (`MoveReason.Grab`, `Grabbed` 1284); from an awake person's ordinary carry `TAKE` returns `Resisted` and the victim perceives it (`TakeResisted` 1283). (The motor rule is UN2's; UN7 owns the events, the hold-out table and the perception.)
4. **Claims as beliefs:** `GestureAct.Claim` 28 with referents communicates a claim (`ClaimCommunicated` 1285); a listener that perceives it may form a `Claim` record through `Brain.AddClaim` (Source = Told), a witness of a physical act forms one (Source = Observed); `ClaimFormed` 1286 (Full, for analytics of beliefs). Contradictory claims coexist; there is no resolution, no veto and no function returning the "true" owner.
5. **Observer belief, trust and reputation from perception only:** extend `SocialSystem` fed inputs so that who handed what to whom and who did not return anything, **as perceived**, updates `SocialMemory` trust and grudges (reuse its subjective deal memory; remove the `Held`/own-lot basis and any title filter from the unified path); an unseen act reveals nothing to anyone (EP-04); the victim of an unwitnessed removal learns the loss, not the actor; two witnesses may interpret the same act differently (their beliefs are separate records).
6. **Misunderstanding is allowed:** perception noise from `CommsRange`/`Messages` may garble a referent; the listener acts on what it perceived. No correction channel exists.
7. **Optional protocol correlation for memory and analytics:** `Corr` is stored on hold-outs, hand-overs, claims and gestures so UN11a can reconstruct "an offer followed by a hand-over followed by a return (or not)" and measure fulfil, partial, delayed, defect and keep-both patterns; these are measured patterns, never policy classes.
8. **Mechanical fixtures:** scripted primitive streams (`ControlOrigin.Fixture`, UN2 harness) for the exchange scenarios of EP-03 and EP-04, labelled mechanical evidence.
9. Events 1280–1286, mapper stub `UN7Mappings.cs`.

## 6. Explicitly excluded scope
An atomic trade, deal object, escrow, lock on goods, debt, obligation, enforced promise, automatic second leg, change of title, price list, market, currency; legacy M5 `Exchanged`/`Gift` title basis and own-goods filters in the unified path; M7/C2 loans and C3 wages as engine mechanisms (they are later social interpretations of separate transfers and labour, UB); a "theft" or "crime" event; reputation as a global score; group agreements with power over goods (UN8b, UB); new primitive verbs; edits to `Comms/Message.cs` or `Messages.cs` layouts (frozen; use the side table).

## 7. Dependencies
- **Hard (merged first):** UN0.
- **Merge-after:** UN2 (executor, `GESTURE`, `TAKE`), UN6 (claims API); may start earlier on the frozen interfaces with stubs.
- **Downstream:** UN6t-b stage S7 (needs UN7 merged: H12.3), UN8b, UN11a (EP-03, EP-04, QA-02), UN10a.

## 8. Shared contracts
- **R:** §23.4, §23.5, §23.9, `Goods` (unified branch; never write title), `Messages.Emit`, `SocialMemory` (feed through public update calls only), `Brain.AddClaim` (UN6).
- **M (own partials only):** `UnifiedState.UN7` (hold-outs, `MessageExt`, `NextCorr`), `EffectHandlers.UN7.cs`, events 1280–1299, `UN7Mappings.cs`. **Never** edit `Goods.cs`, `Message.cs`, `Messages.cs`, `GodSim`, `SaveFile`.

## 9. Files and modules owned
`Unified/Exchange/*`, `Unified/Interactions/EffectHandlers.UN7.cs`, `Unified/UnifiedState.UN7.cs`, `Unified/Exchange/Events.UN7.cs`, `DotNet/God.Runner/Warehouse/UN7Mappings.cs` (stub; UN11a completes), tests `DotNet/God.Runner.Tests/UN7*.cs`, `EP03*.cs`, `EP04*.cs`, scenario `DotNet/God.Runner/Scenarios/unif_exchange.scenario.json`.

## 10. Required domain events
`HeldOut` 1280, `HandedOver` 1281 (giver, receiver, lot, count, corr, origin), `HoldOutLapsed` 1282, `TakeResisted` 1283, `Grabbed` 1284, `ClaimCommunicated` 1285, `ClaimFormed` 1286 (Full). Every action event carries `Origin`. No event names a debt, owner, theft or settlement.

## 11. Save/load implications
Saved and hashed in the `unification` part: open hold-outs (with expiry), `MessageExt` rows (bounded: oldest pruned after a fixed window (L) while keeping rows referenced by open hold-outs), `NextCorr`. Test `UN7_SaveLoadParity` (U-P6): save with a hold-out open and a half-heard gesture, load, continue: equal to the straight run. The save version is the lead's.

## 12. Warehouse implications
Mapper stub for 1280–1286; UN11a's `0045_unification_social.sql` keeps **separate tables for events (moves, hand-overs), observations (who perceived what) and claims or promises (beliefs and communications)**; no legitimate-owner or theft fact. Reconciliation (UN11a): every `HandedOver` row has exactly one `ItemMoved` and no ownership row.

## 13. Acceptance tests
| Id | Test (behavioural) | Maps to | Pass |
|---|---|---|---|
| UN7-T1 | **EP-03** `Offer_Promise_OneHandover`: A gestures `Offer` (wood, 3) and `Promise` (stone, 2), then hands 3 wood to B (hold-out, `TAKE`); one `ItemMoved(HandOver)` and one `HandedOver`; no second move happens by itself; B may then fulfil (2 stone), partially fulfil (1), delay, refuse or keep both: five fixture variants, each leaving goods exactly where the physical moves put them; ledger U-C1 holds | EP-03 (mech), U7 (D9 wording) | exact |
| UN7-T2 | `HoldOut_Lapse_NoDelete`: hold-out expires or giver walks away: the lot stays in the giver's carry; if the giver's carry is full at lapse nothing is lost (U-C4) | EP-03, EP-05 | exact |
| UN7-T3 | `Receiver_Cooperation`: with no receiver `TAKE` the lot never moves; a third party's `TAKE` of a hold-out addressed to `To = B` is allowed or not by the same physics (reach), never by a title (an unaddressed hold-out may be taken by anyone in reach) | amendment §4 | exact |
| UN7-T4 | **EP-04** `Unwitnessed_NoActor`: a lot removed from a container while no person perceives the actor: the removal is an objective event; no person's memory names the actor; the container's usual user learns only that the lot is gone | EP-04 | exact |
| UN7-T5 | `Differing_Interpretations`: two witnesses of one `TAKE` form different claim records (one "she stole it", one "she was given it"); both persist; no engine verdict; a communicated claim reaches a listener as `Told` with garbling per `CommsRange` | EP-04, EP-02, QA-02 | exact |
| UN7-T6 | `Grab_AsleepDead_Resisted_Awake`: `TAKE` from a sleeping, unconscious or dead person succeeds (`Grab`); from an awake carrier it returns `Resisted` and is perceived by the carrier; a held-out lot is takeable | EP-01 support | exact |
| UN7-T7 | `Corr_NeverEnforces`: static and run-time: `Corr` is read only by analytics and memory code; changing or dropping every `Corr` leaves every physical hash unchanged | amendment §4 | exact |
| UN7-T8 | `NoTitle_NoReward`: hand-overs produce no `OwnershipChanged`, no ownership field, no learner reward for the transfer; U-P3 static clean for `Unified/Exchange` | D9.1 d, U-P3 | exact |
| UN7-T9 | `Trust_FromPerception`: a witness who sees A give and B not return updates trust in B; one who sees nothing does not; reputation differs between persons | EP-03, EP-04 | exact |
| UN7-T10 | `UN7_SaveLoadParity`, `UN7_EventsRoundTrip`, `Exchange_NoSecondLeg` (over 20 seeded fixture runs no automatic transfer follows any hand-over) | QA-01, U-P6 | exact |
| UN7-P | pins: U-P1, P1, P4, P8, Stage 3 hashes (M5 barter tests unchanged with the switch off) | all | exact |

D8 ids: **U7** (two individuals physically exchange) is covered by UN7-T1 with D9 wording (no ownership change); EP-03 learned is reported by UN6t-b (T7).

## 14. Headless demonstration
```
dotnet test DotNet -c Release --filter "FullyQualifiedName~UN7|FullyQualifiedName~EP03|FullyQualifiedName~EP04|FullyQualifiedName~Neutrality"
dotnet run --project DotNet/God.Runner -c Release -- run --scenario unif_exchange --seed 1 --days 2 --record full --sqlite --out <scratch>\un7
```
Then `SELECT code, COUNT(*) FROM event WHERE code BETWEEN 1280 AND 1286 GROUP BY 1;` (table names per UN11a's mapping) and a fixture trace showing offer, hand-over and the three kinds of return.

## 15. Unity demonstration
Compile check only (events and types `God.Game` reads): `AgentScripts/check-game-compile.ps1` 0 errors. UN10a draws gestures and carried items; the lead's checkpoint shows a hold-out and hand-over in the player.

## 16. Performance considerations (D10)
Witnesses and listeners are found through `ISpatialIndex` (no all-pairs scan); hold-outs live in a small sorted list processed per tick only when non-empty; `MessageExt` pruned by a rolling window; per-person belief records bounded by UN6's ring of 8; cost O(active gestures + open hold-outs) per tick.

## 17. Integration procedure
1. `git -C C:\Users\joesa\Documents\Cursor\RhytmDrama\wt-lrs worktree add ..\wt-lrs-un7 -b unity/un7 <unity/poc SHA at which the hard prerequisites are merged>`. Copy `Library` only with `robocopy ...\wt-lrs\Library ...\wt-lrs-un7\Library /E /MT:16` (0–7 = success; source editor closed) and only if this package holds the Unity token (index §4).
2. Sim code and its EditMode and `DotNet/God.Runner.Tests` tests first, then runner scenarios, then (only if the package has views) `Assets/God/Game`. Run the package's focused filters after every commit.
3. Before hand-back: `git merge unity/poc` (also after every prerequisite or merge-after package lands); resolve conflicts only in your own files; a conflict in or need to edit a frozen file (§23.0, §23.15) means stop and send the lead an amendment request; never work around it.
4. **Neutrality with the switch off (byte-identical):** with `Unification.Enabled = 0` pin **U-P1** (every part hash of `band_6x20`, `m4_families`, `ibs_families`, `watch6`, `demog_*`, the god-only Living Island scenario, seeds 1–3, every checkpoint, equals the base head), **P1, P4, P8** (and P2, P3, P5, P9 where present) and the three Stage 3 hashes `12be0f64cefc59f1`, `4bd54b271857a803`, `0a66415b389d7a87` must be unchanged; U-P2 and U-P3 (static) re-run as the index §9 says. No hash moves except at a re-pin the lead declares.
5. Full `dotnet test DotNet -c Release` green on the branch after merging `unity/poc`, including the package's `*_SaveLoadParity` (U-P6 stub or full) and `*_EventsRoundTrip`.
6. **Compile check before any Unity step:** `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors (it compiles `God.Game`, Editor and every test assembly without Unity). Required whenever a type `God.Game` reads changed; only then may the editor be opened in batch mode (and only by the package that holds the Unity token).
7. Write "un7 record" in `docs/stage4/DEV_LOG.md` on the branch (what ran with counts, what did not, hashes, design decisions taken as lead defaults) and hand back to the lead. **Never push; never merge into `unity/poc`; never touch `main`.** The lead merges with `--no-ff` and runs the gates of index §6.
Package-specific: re-run `M5BarterTests` and `M5NeutralityTests` with the switch off (unchanged); EP-03 and EP-04 are re-run by the lead (index §6 merge 9); no new `Message` layout field.

## 18. Risks
- Legacy `SocialSim` title paths (`Held`, own-lot filter at `:708`) leaking into unified exchange: static test over `Unified/Exchange` and review of every call into `Social/*`.
- Quantity clamp 0–9 in `Messages.cs:48` silently capping referents: all unified quantities go through `MsgExt` (0–999); a test sends 500.
- Accidental automatic reciprocity through the M5 deal checker: it runs only as subjective memory in unified mode; test `Exchange_NoSecondLeg`.
- Perception leaks the actor of an unseen act: EP-04 test plus a review of every event that names an actor.
- `Corr` growing into an enforcement tool: the read-only test.

## 19. Definition of done
UN7-T1…T10 and UN7-P green under `dotnet test`; EP-03 (mech) and EP-04 shown in fixture traces; compile check 0 errors; mapper stub present; "UN7 record" in DEV_LOG lists what ran, the defaults taken (hold-out expiry ticks, `MessageExt` window) and any amendment requests; handed back.

## 20. Smaller checkpoints
- **UN7a** (≈ 1.5 h): `MsgExt`, hold-out, hand-over, lapse, `Grab`/`Resisted`, events, T1–T3, T6, T7, T10 (enough for S7 training to start).
- **UN7b** (≈ 1.5 h): claims as beliefs, observer trust, unwitnessed and differing interpretations, T4, T5, T8, T9. Cut-plan fallback (index §8 H9.5): UN7 merges hold-out only; claims communication is dropped and recorded.

## 21. Recommended agent
**Opus**. Never Fable.

## 22. Agent working rules
- Follow `CLAUDE.md`: `Assets/God/Sim` is pure deterministic C# (integers, seeded or keyed `Rng` with a label unique per call site, no wall clock, no unordered enumeration, no allocation in per-tick hot paths; bots, people, possessed bodies and test fixtures use the same primitives); `Assets/God/Game` only reads state, sends commands and animates events; sim code and EditMode tests before views; tests pass under `dotnet test DotNet`; never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, `Assets/InitTestScene*` or anything Sims-2-derived; no third-party art or audio; never copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty`; one Unity editor per folder, batch mode, close it when done.
- **D9 rules, always:** no owner, title, permission, use right, theft or legitimate-owner fact in unified code (static guard U-P3); no scripted build order, profession, strategy, retaliation or moral judgement; no new primitive verb (the twelve controls only); every game-visible fact is physical (holder, location, quantity, condition, occupancy, barrier) or a belief held by a person; origin (policy, possessed, fixture, teacher) never changes a physical result; no hidden oracle (`NodePather`, truth queries) on the unified decision path. Every scale-sensitive structure is configurable (tribes, radius, settlements per tribe) and every per-tick cost is O(active entities), never all-pairs (D10).
- **Before any Unity open or test run** (CLAUDE.md): `powershell -NoProfile -ExecutionPolicy Bypass -File AgentScripts/check-game-compile.ps1` must show 0 errors. Only then open the editor (batch mode) and confirm a clean console.
- Commit messages: write the message to an ASCII file in your scratch directory and run `git commit -F <file>`; end it with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Run `git status` before every commit; add paths explicitly, never `git add -A`.
- Commit as you go so work survives a cut-off; never bare `git stash` (shared stash stack), use a WIP commit.
- Never push; never merge into `unity/poc`; never touch `main`. Never use or suggest Fable.
- Report exactly which tests and runs happened and plainly what did not run. A fixture-driven result is labelled mechanical; it is never presented as autonomous behaviour.
- Keep context lean: read only the cited sections; grep for symbols; send long output to files and read the failures.
- Contradiction with §23 or the code: stop and ask the lead for an amendment; never edit a frozen file. Minor reversible choices: take a default, make it configurable, and record it in "UN7 record" under "Design decisions taken during implementation".

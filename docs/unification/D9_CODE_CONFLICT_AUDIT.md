# D9 code conflict audit (unified mode)

Read-only audit of `Assets/God/Sim/**` and `DotNet/God.Runner/**` at worktree head 07b69b6 (plus uncommitted planning docs). Controlling text: [LATEST_OWNER_AMENDMENT.md](LATEST_OWNER_AMENDMENT.md) and D9 in [../civdiv/OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md). It does not repeat [UNIFICATION_BASELINE.md](UNIFICATION_BASELINE.md) (sections 15-16 are the planning delta; this file supplies the call-site evidence for R24, R26-R31).

**Method and status.** Evidence is Grep/Read of the files cited (file:line). Nothing was built, run or tested. Items marked **[U]** are unverified (inferred from names/comments, or not read in full). Line numbers are for this head and may drift. Not read in full: `Orgs/Health.cs`, `Orgs/Education.cs`, `Orgs/Services.cs`, `Lab/**` (exact-game lab, kept separate by the amendment), `Ai/**` and `Economy/**` (faction level; baseline section 3 covers them), `DotNet/God.Runner/Kpi|Demog` (only grepped for ownership symbols).

**Actions legend.** N = neutralise in unified mode behind the switch (legacy path unchanged, hashes preserved). K = keep for legacy only. P = replace with a physical rule. D = delete later (after legacy pins are retired).

---

## 0. The shape of the problem (read first)

1. **`Goods` itself enforces no permission.** `Goods.Move` checks holder validity, reach (`Reach.Of(why)` at `Population/Goods.cs:169-170`), count, and room (`Fits`, `:175`). It does not read `Owner` to refuse. The class comment on `Permits` says the same (`Cognition/Affordances.cs:57-64`: "Not enforced by Goods or by any M3, M4 or M5 behaviour"). So the physical core is already close to D9. The conflict is in (a) what `Goods` *does* with the owner field as a side effect, and (b) the many callers that filter, gate or classify on it.
2. **Carry is pseudo-ownership.** Almost every transfer into a carry uses `OwnerRule.ToReceiver` (`Goods.cs:464-470`), so a carried lot's `Owner` becomes `Npc(holder)`; a drop forces `Owner.None` (`Goods.cs:459`, `:534`); withdraw from a bin uses `OwnerRule.Keep`, so goods carried out of a household/org/estate bin keep the bin owner. Downstream filters (`Held`, `OwnerRelation`) then treat "my carried lot" as "my own lot". In unified mode a holder-is-owner mirror would recreate title silently. The audit target is therefore *every* `l.Owner` read.
3. **Switches that exist today.** `Civ.Enabled`, `Orgs.Enabled`, `Orgs.Emergence`, `Divinity.Enabled` are tuning flags (`Civ/CivTuning.cs:15`, `Orgs/OrgTuning.cs:15-16`, `Divinity/DivinityTuning.cs:15`); a module's state is created only when on (`Core/GodSim.cs:114`, `:48-51`). **M3 population, M4 households/estates and M5 social have no off switch short of `GameSetup.Population`** (`Core/GameSetup.cs:52`; `Households.Install` returns only if state absent, `HouseholdHooks.cs:21-28`). The living-population game sets `Divinity/Civ/Orgs = 1` and attaches `AiBrain` factions (baseline 2.1). So **all of the code below runs in the living-population game**, except where noted. UN0 needs a new unified switch (one flag read by the sites below), not just the existing module flags.
4. **Telemetry/warehouse readers** (`DotNet/God.Runner/Warehouse/C1Mappings.cs:19-22,54-89`, `PopulationMapping.cs:36,95-99`, `HouseholdMappings.cs:26,111`, `WarehouseRecorder.cs:226-235`) record `OwnershipChanged`, `UseRightGranted/Ended`, `TheftCommitted`, `EstateTransfer`, owner party keys. They only read; but they define `fact_theft`, `fact_use_right`, `fact_inheritance` as ownership truth. QA-02 requires analytics not to publish a universal theft/legitimate-owner fact: these tables must stay legacy-only or be re-labelled (objective moves only). Registry tests on event ids (`CivEvents.C1.cs:16-17`, `DomainEvent.cs`) are a pin risk if events are removed; emit nothing instead of deleting.

---

## 1. Ownership and permission

### 1.1 The ownership data (writers)

| Site | What it does | Writers / readers | Runs in living game | Action |
|---|---|---|---|---|
| `Population/Goods.cs:86-90` `Produce(..., owner, ...)` sets `lot.Owner` | New goods are born owned by the producer (foragers/gatherers pass `Owner.Npc(npc.Id)`: `PopulationSystem.cs:958`, `:983`); asset output is owned by `a.Owner` (`Civ/Work.cs:164`) | Writers: M3 foraging/gathering, C1 `Operate`, `Miracles` | Yes | N (unified: produce unowned/holder-only; keep `Owner` field as `None` or legacy-neutral) |
| `Goods.cs:173,188` `Move` computes `OwnerAt` and writes `moved.Owner` | Every move can re-own: `ToReceiver`, `ToBinOwner`, `Set`, `Keep` (`:10`, `:462-474`); ground always `None` (`:459`, `:534`) | All ~30 `Goods.Move` callers (list in 1.5 and 2) | Yes | N: in unified mode force `OwnerRule.Keep`/`None` and no `OwnershipChanged`; P: provenance goes to a separate history record |
| `Goods.cs:202-208` `OwnershipChanged` emission and `:209-221` `HandOver.OwnerChanged`/`FromOwner`/`ToOwner` | Emits ownership change event; hand-over event carries owner change flag read by M5 (`:209` comment "M5 filters on OwnerChanged") | `SocialHooks.OnHandOver` [U for exact readers]; warehouse `fact_ownership` (`PopulationMapping.cs:95`) | Yes | N (do not emit; objective `ItemMoved` remains) |
| `Goods.cs:227-245` `SetOwner(...)` | Changes owner in place (no movement) | Estates (`Estate/Estates.cs:115,361`), Orgs dissolve (`Orgs/Orgs.cs:324`), lost property (`Civ/Ownership.cs:294`), asset transfer | Yes (estates always when M4 on) | N; D later |
| `Goods.cs:477` `Mergeable(.., owner)` and `:519,556` | Lots merge only when owners are equal. **Hidden effect:** differently-owned identical wares do not merge, changing lot ids/stack counts | `Insert` | Yes | N: in unified mode owner is constant so merge becomes ware+spoil only. **Hash-sensitive: confirm legacy-off path unchanged.** |
| `Goods.cs:250-256` `NewBin(kind, owner, ...)`, `LotBin.Owner` | Bins carry an owner (household store, org store, estate, asset store) | `Civ/Assets.cs:232`, `Estates.cs:70`, `Orgs.Found` | Yes | N (bin owner is routing data in legacy only); P: a bin has location/capacity/occupancy/barrier, not owner |
| `Goods.cs:276-320` `PayProduce`, `PayOwnerChange`, `PaySpoiled` | **Learning reward reads owner.** Production rewards the producer-owner (`:281`); an owner change pays the gainer and charges the loser (`:289-305`) except NPC<->own household; spoilage charges the owner (`:312`). `GoodsWorth.Of` (`:579`) is the valuation | `Learner.AddReward`; every owner change | Yes | **N critical for training**: unified rewards must come from experienced needs, not from title changes (amendment section 6 "Feedback comes from experienced needs"). Reward by title swap would teach "possess = good". Replace with need-driven credit (the `CreditedLater` / `GoodsCredit.Realised` path at `:276` already exists for food and axes) |
| `Population/PopulationModule.cs:98` `Kill` -> `DeathSpill` | Carry drops to ground ownerless | M3 death | Yes (but see 2.1: M4 estate runs first) | K (already physical) |
| `Civ/Assets.cs:140` `Own(owner, basis, tick)`, `:244-250` `Transfer`, `Create(..., owner, ...)` (`Work.cs:52`) | Productive asset carries `Owner`, `Basis`, `Rights` (list of `UseRight`, `Assets.cs:95-121`), `MaxAssetsPerOwner` (`CivTuning.C1.cs:15`) | `Work.FoundSite`, `Employment.Activate`, `Assets.GrantUse` | Yes if `Civ.Enabled` (it is) | N (see 4.4); D later |
| `Population/PopulationState.cs:61-77` `Hut.Rights[]`, `Contributors`, `HasRight`; `Population/Households/HutRights` | **Bed ownership.** Finishing a hut gives the first contributors the bed rights (`PopulationSystem.cs:1052-1074` `FinishHut`); sleeping in a hut requires `hut.HasRight(npc.Id)` (`:925`); hut candidates are offered only if `mine` (`:464`) or open site (`:465`); household grants (`Households.cs:121-122`), revoke (`HouseholdActs.cs:483,589`; `PopulationMiracles.cs:61`) | Legacy shelter model | Yes | **N critical** (this is the builder-privilege D9.6 forbids): in unified mode the shelter is a physical building with occupancy/capacity; no right list. P |
| `Civ/Ownership.cs:311-323` `Property.Claim` (`Source`) | First arrival at a wild source records a claim (`Classify` `:213-217`). No exclusion follows (comment) | `Classify` | Yes | N (delete later; a claim is a belief, not world state) |
| `Estate/Estates.cs` (`BinKind.Estate`, `Owner.Estate(d)`, `EstateRecord`) | See 2.1 | | Yes | N |
| `Core/Reasons.cs:17` `NotOwner` | Reason code | | | K |

### 1.2 Readers: permission and truth queries

| Site | What it does | Who calls | Living game | Action |
|---|---|---|---|---|
| `Civ/Ownership.cs:58-65` `OwnerRelation(actor, owner)` | RightOwner / RightHousehold / None from `owner`; **the single most used owner predicate** | `Work.cs:86,112,191,229,254,284,311,316`; `Employment.cs:91,208,222,578,585`; `PeopleCards.Civ.cs:46,51` | Yes | N: return "allowed" constant in unified mode (neutral), but see the warning in 1.4 |
| `Ownership.cs:67-75` `RightOn(actor, asset, kind)` | True use right: owner, household or `UseRight` row | `Work.Operate` (`Work.cs:174`), `Ownership.Truth(bin..)` (`:94-96`) | Yes | N |
| `Ownership.cs:78-87` `Truth(actor, owner)` and `:90-107` `Truth(actor, bin, lotOwner)`; asset store rights, `StoreAccess.MayTake`, `Orgs.Truth` fallback (`:85`) | The canonical **truth** permission. Used by theft classification only (`Ownership.cs:256,273`) | `TakeFromGround`, `TakeFromBin` | Yes | N (neutralise: never classify). QA-02: no truth query of entitlement |
| `Orgs/Orgs.cs:388-397` `Orgs.Truth` | Org store access by charter (`Anyone/Members/Officers/MembersByRation`) | `Ownership.Truth` | Yes (Orgs on) | N |
| `Population/Households/Households.cs:203-212` `StoreAccess.MayTake` | Household store: only own household; children allowed if `h.StoreAccess != 0` | `HouseholdActs.Take` (`:275`), `Ownership.Truth` (`:104`) | Yes | N |
| **`Population/Households/HouseholdActs.cs:262,275` `IsOwnStore` + `MayTake` gate inside the act** | **This is an enforced permission check in the act executor** (Frustrate if not own store). Also `:164-166` (feed only own goods), `:300-301`, `:325`, `:342`, `:405` own-goods filters | M4 `Feed`/`Store`/`TakeFromStore`/`Provision` | Yes | N critical: EP-01 requires withdrawal from any accessible container regardless of legacy owner/group fields |
| `Civ/Work.cs:112` `Build`: `if (OwnerRelation(npc, a.Owner) == RightNone) { Frustrate }` "only the owners build their site"; `:86,191,229,254,284,311,316` material, input, haul, take-out filters on `OwnerRelation` | Enforced in the executors: only owner/household can build, haul or take from an asset store | C1 | Yes | N critical; P (D9.6: any physically able NPC may contribute) |
| `Civ/Work.cs:174-181` `Operate` | After the act, emits `TheftCommitted{Kind=Use}` and increments `Thefts` when `RightOn == RightNone` ("A worker without any true right still works physically; the sim classifies it as a use theft") | C1 | Yes | N (the physical part is already right; drop the classification) |
| `Civ/Employment.cs:84-97` `Activate` | Needs `OwnerRelation(agent, a.Owner)`; grants a `Work` `UseRight` on accepting a job | C3 | Yes | N |
| `Employment.cs:208,222` `Pay`/`Carried`; `:578,585` `PayAct` | Pays only from lots whose owner relation is own/household (a wage cannot be paid from a withdrawn communal lot) | C3 | Yes | N (physical carry only) |
| `Employment.cs:803` invariant CIV3 "active contract without the worker's Work right" | Invariant couples contracts to UseRight | `Check` | Yes | K legacy only |

### 1.3 Mind side: believed permission (`Permits.*`)

`Permits.Believed` (`Cognition/Affordances.cs:69-76`), `BelievedOrg` (`:82-94`), `BelievedAsset` (`:101-111`), `BelievedOwnerOf` (`:115-120`) are queries over the mind's own beliefs ("a query, never enforced", `:99`). They are **candidate filters in disguise**:

| Call site | Effect on what the NPC can think of | Action |
|---|---|---|
| `Civ/CivMindHooks.cs:76-77` (owner flag), `:98` (remembered asset needs `BelievedAsset(Work).Allowed`), `:158` `MayTakeBelieved` (only stores whose asset it believes it may Store-use are offered), `:176` | **No build/work/take-out candidate is generated unless the NPC believes it may.** Also `:78-80`: building an unfinished site is offered only `if (owner && ...)` | N: in unified mode generate candidates from physical reachability/legality only (amendment section 6 "physical legality masks only") |
| `Civ/CivMindHooks.C3.cs:168`, `:386` | Employment offers/answers filter by believed owner | N |
| `Population/Households/HouseholdHooks.cs:364` `UsableFood` | Only food it believes it may give (own/household) | N |
| `HouseholdHooks.cs:120-153` `SenseBins` | **The Sense seam for bins omits other households' stores** ("Other households' stores are not offered to the mind (no permission; theft is M8)", `:123-125`) and counts only "mine" units (`:142-143`); estates only appear if it has own lots (`:152`) | N critical: an NPC cannot even perceive a communal or another's store. Replace with physical sight. |
| `HouseholdHooks.cs:659,676` (`BinCand TakeFromStore`) | Estate/Store candidates, gated by the SenseBins result | N |
| **`Social/SocialCandidates.cs:41-52` `Held(sc, w)`** | **Owner-based candidate filter**: units counted only if `l.Owner.Kind == Npc && Id == Self`; M3-M5 candidates "offer only its own goods" (comment). So any carried lot that is not own-owned (withdrawn from a household/org/estate bin with `Keep`) is invisible to every give/offer/deal | N critical |
| `Social/SocialSim.cs:708-711` hand-over executor picks lot by `Owner == Npc(self)` | Same filter in the act | N |
| `Divinity/DivineActs.cs:403-408` `OwnLotOf`; `DivinityHooks.D2.cs:73`; `DivinityHooks.D4.cs:61` ("a person gives only its own: never conjured"), `DivinityMindHooks.D4.cs:183` | Divine Give/possession give only own-owned lots | N |
| `Orgs/OrgMindHooks.C6.cs:377`, `Orgs/CommunalStore.cs:216-218` `Mine` | Deposit candidates only own/household goods | N |
| `Orgs/CommunalStore.cs:129,153` `l.Owner.Same(Orgs.OwnerOf(org.Id))` | Serve takes only org-owned lots and re-checks org ownership in the act | N (needs P: "ration" is an NPC choosing to give) |
| `Information/PeopleCards.Civ.cs:41-59` | Inspector cards show owner/`AssetOwned`/`BelievedOwner` ("Truth" layer) | N (analytics display only; but shows the "nobody's" truth, QA-02) |
| `Civ/Ownership.cs:115-181` `Observe`, `Witness`, `CivMind.Own` beliefs (`OwnBelief`) | The perception path that forms *believed owner* from a builder/user. Builder -> believed owner (`:180`) | N, but this **is** a usable base for D9 claim beliefs (claimant, subject, source, confidence, time, expiry) if rewired; see section 7 |
| Valuation: `GoodsWorth.Of` (`Goods.cs:576-587`) reads need (`HomeHut`, `LogWorthNeeded`) not owner; **but** `Social/Valuation.cs` [U] and `SocialSim.cs:541-548` `BelievedGain` price goods; `PayOwnerChange` makes owner change the reward carrier (see 1.1) | Valuation per se is not ownership-gated; reward on title swap is | N for reward; K for price |

### 1.4 Neutralisation warning

Making `OwnerRelation` return "allowed" is not enough on its own: `Held` and the M4/C6 filters compare `l.Owner` to the actor (not through `OwnerRelation`), so each family above needs its own unified branch. Also `Goods.Insert` merge-by-owner interacts: if unified mode leaves `ToReceiver` on, lots will become `Npc(holder)` owned and the filters will silently work as "holder owns" - the exact "silent adapter that recreates title" the amendment forbids. The cleanest approach is a single switch read inside `Goods.OwnerAt`/`Insert` (owner always `None`) *and* the filters returning physical-only candidates, with a test that grep-asserts no `Owner` read in the unified candidate path.

### 1.5 `Goods.Move` call sites (all re-own on move unless `Keep`)

`Employment.cs:210,588`; `Work.cs:255,289`; `DivinityHooks.D2.cs:55,75`; `DivinityHooks.D4.cs:65`; `Miracles.cs:487`; `Estates.cs:99,182,323,368`; `CommunalStore.cs:131,158,206`; `HouseholdActs.cs:177,284,305,326,407`; `Households.cs:80`; `PopulationModule.cs:98`; `PopulationSystem.cs:396,1013`; `SocialSim.cs:714`. The `ToReceiver` ones (`Employment.cs:210`, `D2:75`, `D4:65`, `CommunalStore.cs:158`, `HouseholdActs.cs:177`, `PopulationSystem.cs:1013`, `SocialSim.cs:714`) include the **pick-up of ground goods** (`PopulationSystem.cs:1013`), i.e. ordinary PickUp makes the picker the owner today.

### 1.6 Invariants that encode ownership (replace in unified mode)

`PopulationInvariants.cs:12,102-103` C4 ("ground lots ownerless; no lot owned by a person dead more than 3 days") - the deceased-owner invariant the amendment says to replace with location/conservation; `HouseholdInvariants.cs:41,57,101-107` (store owner is household; estate open past deadline; lot owned by deceased only if estate open); `OrgModule.cs:96-107` ORG4 (bin owned by known org; dissolved org owns nothing); `CivModule.cs:50-55` CIV1 (lost lot without owner is a violation); `Employment.cs:803` CIV3; `DivinityModule.cs:69-70` G3 `OwnerKind.God` must not exist (keep: no god ownership). Action: gate each on "legacy ownership on"; add unified C1-C7 conservation by location (no deletion, no teleport, one holder per unit).

---

## 2. Automatic transfers

| # | Mechanism | Evidence | Who triggers | Living game | Action |
|---|---|---|---|---|---|
| 2.1 | **Estates / executor / inheritance** | Death hook `PopulationModule.Kill` -> `PopulationExtensions.OnDeath` (`PopulationModule.cs:92`) -> `HouseholdHooks.OnDeath` (`HouseholdHooks.cs:46`) -> `Estates.Open` (`Estates.cs:57-105`): carry moved into an estate bin `MoveReason.EstateIn` with `Owner.Estate(d)` (`:99-100`); heirs fixed from the deceased's believed kin (`:75-88`); `EstateSystem` order 255 (`HouseholdHooks.cs:27`) runs `Estates.Tick` (`Estates.cs:123`): executor ladder (`ExecutorRung` `:13`), `CustomSettlement` (`:203`), `SetOwner(... Inherited)` (`:115,361`), `Transfer` events (`:190`), share bins (`:323`), deadline -> ownerless on ground `Escheat` (`:368`), `EstateOut` to ground (`:182`). Households' goods: `Households.cs:80` drops store contents to ground on household end. Heirs collect via `HVerb.TakeFromStore` + `Collect` (`HouseholdActs.cs:274`) driven by instinct `HClass.Estate` (`HouseholdHooks.cs:303`). `DeathSpill` then only fires for what is left (nothing when M4 is on) | Death (any cause) | Yes | **N critical.** EP-05: goods stay where they physically are on death (the carry drop at `PopulationModule.cs:95-99` is already the right behaviour). Gate `Estates.Open`/`EstateSystem`/`SenseBins` Estate branch/instinct/`HClass.Estate` candidates. D later |
| 2.2 | `ItemMoved` with ownership side effects | `Goods.Move` (see 1.1); rewards `PayOwnerChange` (`Goods.cs:289-305`) | every move | Yes | N |
| 2.3 | **C3 `Employment.Pay`, wage arrears** | `Employment.Pay` `:197-217` (decision driven `PayAct` `:560`); `Tick` `:642-690` counts earned units and, past `PayGraceTicks`, marks `Unpaid` (`:703-720`, counter and trust loss only: "no rule forces payment", `:688`). Contract `Activate` grants a `Work` `UseRight` (`:84-97`); `End` removes it (`:172-176`) | contract state | Yes | **Already mostly voluntary**: pay is a decision, arrears is a counter. Conflicts: the `UseRight` grant/arrears bookkeeping as authoritative state, `Pay` from own-owned lots only, offers gated by believed ownership. N (neutralise rights); K the social memory ("they didn't pay") as belief only; the ledger must not be truth |
| 2.4 | **C6 rations / communal store** | `OrgTemplates.SharedFoodStore` (`Orgs.cs:62-66`, `MembersByRation`); `CommunalStore.Serve` (`CommunalStore.cs:111-175`): keeper withdraws from org store then hands over (two `Goods.Move`, rations booked in an org ledger `org.Ledger.Book`, `WelfareGranted`, `OrgSpent`, `s.Organisations.Rations++`); `Deposit` `ToBinOwner` (`:206`); `Orgs.Dissolve` -> `SetOwner(... Divided)` (`Orgs.cs:302-325`, `DissolutionRule.ToMembersEqual`) | NPC decisions (`OrgVerb.RequestService`, `OrgWork`) | Yes (Orgs on, Emergence 1) | N (org has no magical power over goods, D9.4); K as an NPC-to-NPC give if the title/ledger/`Dissolve` re-owning is removed |
| 2.5 | **C1c take-out** | `Work.TakeOut` (`Work.cs:262-300`) withdraws `IsOutput` lots from an asset store if `storeRight || OwnerRelation != None` (`:284`) | NPC decision `VerbHaul` + bin target | Yes | N (permission); K physical |
| 2.6 | **I-BS household sharing** | `HouseholdActs.Feed` (`:164-177`: hands own/household lots to a dependant, `rule = lot.Owner.Kind==Household ? Keep : ToReceiver`), `Provision` (deposit into the household store `:326,407`), `TakeFromStore` (`:275-305`), `HVerb.Beg/Feed` instincts (`HouseholdHooks.cs:290-295`) | NPC decisions | Yes | N filters; K as voluntary give. The instincts are prescriptive (see 4.2) |
| 2.7 | Gifts that change title | `Goods.cs:202` basis `Gift`; D2/D4 `Give` `OwnershipBasis.Gift` (`DivinityHooks.D2.cs:75`, `D4.cs:65`); `SocialSim.cs:713` | NPC/possession | Yes | N (the hand-over is the physical act; no `OwnershipChanged`) |
| 2.8 | **M5 deal settlement** | Not atomic and not automatic: each side performs its own `Goods.Move` (`SocialSim.cs:706-715`) at its own decision; `DealId` is carried by messages (`Comms/Message.cs:47`, `Messages.cs:23,48`) and a trace (`Core/GodState.Social.cs:24-66`); `CheckDeals` (`SocialSim.cs:431-465`) marks a deal `Failed` and records `Betrayal`/trust loss when "I handed over and they have not ... they took and left" and a `Done` state when both sides gave (`:460-462`). **Tests already prove defection is possible**: `DotNet/God.Runner.Tests/M5BarterTests.cs:25` `M5_7_DefectionIsPossible_TakeAndLeaveKeepsBothGoods_BsTrustFalls`; `Social/BenchSocialPolicy.cs` [U on its TakeAndLeave mode]. Conflicts only: (a) owner filters `Held` and `SocialSim.cs:710`; (b) the `Exchanged` vs `Gift` `OwnershipBasis` (`:713`); (c) `Completed`/price learning keyed to settled deals (`:737-760`); (d) `OwnerChanged` flag on `HandOver` (`Goods.cs:218`); (e) messages cap `Quantity` to 0-9 (`Messages.cs:48`) so offers are coarse | Decisions | Yes | **Good reuse base** for EP-03. N the owner filters/title basis; K DealId as the optional correlation id (amendment 4); K betrayal/trust as subjective memory. No lock or atomic leg exists to remove. |
| 2.9 | Atomic/paired transfers | None found in M5 or C3. Closest: `Orgs.Dissolve` (divides all lots in one call), `Estates.Split` (all heirs), `AiTrader` / Stage 3 trade (baseline section 3, faction-level goods ledger) | | | Stage 3 trade: baseline only |
| 2.10 | Miracles `Confiscate`/divine ownership edit | `Miracles.cs:487` (`Drop`, `OwnerRule.Set None`), `:514-516` emits `OwnershipChanged` (`FromKind None`), `:529-531` reads `lot.Owner` for reward; `Miracles.cs:207,212,224,257` read `Owner`/`estate` flag | god acts | Yes | N (the god may stay; its goods edit should not write title); [U] full read |

---

## 3. Automatic theft / crime classification

| Site | What | Readers | Living game | Action |
|---|---|---|---|---|
| `Civ/Ownership.cs:202-234` `Classify` (called every tick from `CivSystem.cs:17`) | Reads this tick's `ItemMoved`: drops of owned lots are recorded as `Lost` overlay (`:222-229`); take from ground / withdraw from bin without `Truth.Allowed` -> `TheftCommitted` (`:236-278`) | emits; counters `Thefts`/`Abandoned`; warehouse `fact_theft` (`C1Mappings.cs:22,85`) | Yes | **N critical** (QA-02: no universal theft). EP-04 needs *subjective* interpretation from observation. D later |
| `Ownership.cs:187-197` `LostOwner`, `Lost` list (`LostLot`; hashed `CivState.C1.cs:67`), `DayStart` (`:281-300`) after `LostPropertyDays` -> `SetOwner(None, Abandoned)` | Lost-property windows: dropped owned goods remain "owned" for N days | `CivSystem.cs:23` | Yes | N |
| `Civ/Work.cs:174-181` use-theft | See 1.2 | | Yes | N |
| `Civ/CivModule.cs:55` CIV1 "lost lot without owner" | invariant | | | K legacy |
| `Civ/CivEvents.C1.cs:74` `TheftCommitted` doc: "Classified by the sim after the act; nothing ever reacts to it" | In legacy nothing reads it, so removing it changes no decision. Reputation/`Betrayal` in M5 are separate and subjective | | | N (cheap win) |
| `Information/PeopleCards.Civ.cs` and `PopulationKnowledge` | **Truth-layer rows** of owner for the inspector | analytics only | Yes | N / relabel |
| Any query that reveals a perpetrator to a victim | Not found: `TheftCommitted.Taker` is only in events/warehouse. Perception-based beliefs (`SocialMemory`, `Ownership.Witness`) are local. **[U]** I did not read `SocialMemory.cs`/`SocialHooks` for victim-side inference from `OwnerChanged`. | | | verify |

---

## 4. Prescriptive behaviours

### 4.1 Verb and intent inventory (high-level vs primitive)

The existing action vocabulary is **learning `Verb` ids** (byte, `Learning/Verbs.cs`, `Max=128`) mapped to `NpcVerb` intents (`Policy/PolicyTypes.cs:10-`). None of them is a 12-primitive control. Every one of these is a **single-step high-level verb** (target + implicit pathing + effect):

| Family | Ids / names | File | High-level? |
|---|---|---|---|
| M3 core | `Eat 1, Sleep 2, Rest 3, Flee 4, Hide 5, SeekShelter 6, Approach 7, Avoid 8, Gather 9, ForageAt 10, Haul 11, Build 12, Craft 13, WorkShift 14, PickUp 15, Drop 16, Store 17, TakeFromStore 18, Equip 19, Explore 20`; lab 60-62 | `Learning/Verbs.cs:16-32` | Yes. Only `PickUp/Drop/Equip` and `Approach` are near a primitive |
| Intents | `Idle, Move, Work, Eat, Rest, Gesture` (`PolicyTypes.cs:14`), combat subset `HoldPost 20 ... Assist 30` (faction military, `:16-17`), M3 `Sleep 40 ... SeekShelter 47`, `LabAct 48`, plus F0 generic intents (e.g. `Interact`, `HandOver`, referenced `DivineActs.cs:70-76`) | `PolicyTypes.cs` | Yes |
| M4 | `Feed 32, Beg 33, Propose 34, Accept 35, TryForChild 36, Leave 37, Tend 38, Adopt 39, AskCare 40, AcceptCare 41, EndCare 42, SeekChild 43, AcceptChild 44, RangeFar 45, Provision 46` (+`Store`/`TakeFromStore`) | `Households/HouseholdVerbs.cs:12-19` | Yes, social/economic (`Provision` = forage-and-store-for-household, `RangeFar` = long walk) |
| M5 | `Give 48, HandOver 49, Seek 54`, `Say(act)=64+act` (`SayWant 65, SayOffer 66, SayAccept 67, SayRefuse 68, SayNeedFood 69, SayNeedHelp 70, SayCooperate 71, SayWarn 74`) | `Social/SocialVerbs.cs:14-25` | `Say*` are gestures (close to the GESTURE control); `Give/HandOver` are a pair of generic physical handover; the rest high-level |
| C1 | `VerbWork 96, VerbBuild 102, VerbHaul = Verb.Haul 11 (take-out via bin target; `IntentTakeOut`)` | `Civ/Work.cs:18-24` | **Hidden high-level economic actions**: `Build` = found-site-or-contribute-or-finish, `Work` = produce a recipe cycle, `Haul` = deliver or take-out |
| C3 | `VerbHire, VerbWorkFor, VerbPay (101)`; intents offer/accept etc. | `Civ/Employment.cs:23-30` | Yes (employment) |
| C6 | `Join 104 (sub-acts Propose, Answer, CallMeeting, AnswerMeeting, AskJoin, AcceptJoin), Leave 105, RequestService 106, WriteRecord 114, OrgWork 115 (Deposit 11, Serve 12)`; intents 180-184 | `Orgs/OrgMindHooks.C6.cs:11-26` | Yes, institutional |
| C7 | `Treat 107, Teach 108, Attend 109`; intents 185-187 | `Orgs/OrgMindHooks.C7.cs:12-18` | Yes |
| Divinity | `DivineVerb.Give 119`; `EmbodiedVerb.AvatarTake 121, Give 122, Gesture 123` (`Possession.cs:42-51`) ; `DivineAct` 21 acts (`DivineTypes.cs:10-14`: `MoveTo ... Attack 18, Teach, Treat, InstitutionalAction`) mapped by `DivineActs.Map` (`DivineActs.cs:60-81`) onto the M3 verbs | | Yes. `DivineAct.Attack`, `Construct`, `Trade`, `Cooperate`, `InstitutionalAction` have **no `Map` row** (default) so they do nothing at the NPC level [U: possession `CheckAction` may reject]. |
| Faction/military | `Charge, Attack, Retreat, Surrender ...` (`NpcVerb` 20-30) | Stage 3 `MilitarySystem` | Faction-level |

**Where hidden high-level economic actions are issued:** the *policy* layer does not (see 4.5), but the **act executors** expand one verb into many physical steps with built-in economics: `Work.Build` (`Work.cs:107-134`: walks, checks owner, picks the material slot, consumes one unit per trip, advances `BuildWork`, calls `Assets.Finish`), `Work.Operate` (recipe cycle with inputs/outputs in one tick, `:153-185`), `Work.Haul`/`TakeOut`, `PopulationSystem` hut `Build` (`:862-1043`), `Gather`/`ForageAt` (`:958,983` produce goods directly into carry, owner = self), `Eat`. Under D9.6 these become sequences of primitive `USE`/`TAKE`/`DROP` by the brain.

### 4.2 Instinct tables (innate economic strategies)

`Learning/Instincts.cs` `InstinctTable.Core` (`:35-45`) is added into every default NPC's score ("instincts are never written into the store"). Rows that encode behaviour/strategy:

| Rows | Content | Verdict |
|---|---|---|
| Hunger -> `Eat` 600, `ForageAt` 250, `Explore` 300 (`:36-39`); Fatigue -> `Sleep` 500; Pain -> `Rest` 400; Fear+EnemyInSight -> `Flee` 500 | Survival reflexes | Prescriptive (hard-coded `Eat/Flee`): in unified mode these are not allowed to be the controller; the trained policy has to produce them (TR-01). `Flee` must become "step away", not a verb |
| NoBed -> `Gather` 150, CarryingLog + Shelter -> `Build` 250, NoBed + HutSite -> `Build` 100 (`:43-45`) | **A scripted shelter goal**: "so an instinct-only NPC still builds a hut slowly" (comment `:30-33`) | Prohibited by D9.6 ("no scripted shelter goal"). N |
| M4 table `HouseholdHooks.cs:290-321`: child Hunger -> `Beg`, `Begging` -> `Feed` 300-600, Sick -> `Feed/Tend`, NeedsHome -> `Adopt`, hunger -> `TakeFromStore`, carrying food -> `Store`, `Estate` -> `TakeFromStore` 450, `Unpartnered -> Propose`, `TryForChild`, `Provision`, `RangeFar` | Care/provisioning/family reflexes | N. These are "learned expectations" candidates but not hard-coded reflexes |
| M5 table `SocialVerbs.cs:65-82`: good offer -> `SayAccept` 350; `Accept/Cooperate` -> `HandOver` 350; `NeedFood` -> `Give` 200; `Warn` -> `Flee`; hunger -> `SayNeedFood`; HasSite -> `SayNeedHelp` | Reciprocity reflexes ("instinct is to hand over after an accept") | N: nothing in D9 forbids a trained default, but an instinct that automatically performs the second leg is prescriptive reciprocity (D9.5) |
| C1 `CivMindHooks.cs:31-44`: log + Plot -> `Build` 200, log + Unfinished -> `Build` 300, Field -> `Work`, Workshop+stock -> `Work`, food hunger -> `Haul` | Build/work/haul strategy | N |
| C3 `CivMindHooks.C3.cs:49-71`: pay-band conditioned accept/refuse/offer/pay (e.g. unpaid -> refuse 700) | Employment strategy | N (legacy) |
| C6 `OrgMindHooks.C6.cs:74-89`: needs -> `Join` (found an org); `MeetingCall -> Join 400`; `RationRequest + Hunger -> RequestService 350`; `Serve -> OrgWork 400`; C7 `TreatNeed -> Treat`, `Teach` | **Organisational strategy as instinct**: C6 founding meetings with fixed templates (`OrgTemplates` `Orgs.cs:62-77`: `SharedFoodStore`, `HealerPractice`, `HealersHouse`: min founders/households, offices, charter fields) and `TryFound` (`OrgHooks.C6.cs:215-238`): the *template menu* is the prescription | N (Orgs.Enabled = 0 or Emergence = 0 in unified mode; see 7). K only if re-expressed as NPC agreements |
| C3 offer pacing `CivTuning.C3.cs:19-23` (`OfferTicks 120`, `OfferCooldownTicks 600`, `OfferRange 4`, `OfferAssetRange 8`) and `CivMindHooks.C3.cs:393-426` (offers made face to face within range, one candidate at a time) | Fixed pacing/range for contract offers | N (legacy) |
| M4 care reflexes: `HVerb.Tend/Feed` on `HState.Sick`, `C7 Treat` on seen need | Care as an instinct | N; K as a trained default |

### 4.3 C1 build-site/plot logic (R28)

`Work.CanFound` (`Work.cs:62-73`): omniscient truth check (land, no building/flag/road, outside every HQ circle, no hut/asset/source within one node, `MaxAssetsPerOwner`), `FoundSite` (`:38-58`) creates `Assets.Create(owner = household or npc, Basis Built)`; `Ownership.Witness` publishes a believed owner to everyone in sight; `CivMindHooks.cs:60-70` offers at most two asset kinds per plot (`offered >= 2`, skipping kinds already believed owned: `OwnsKindBelief`). M3 hut logic similar: `PopulationSystem.cs:851-860` `CanFound`, `HutSiteFounded`, `FinishHut` (`:1052`). Both: owner = founder, builder gets beds/ownership, one unit of material per trip, work counted in ticks (`BuildTicks`), no cooperation conservation (`MaterialsIn`). Action: N behind the switch; P: new site/known-design/`USE` placement with the shared controller. Reuse: `ProductiveAsset.MaterialsIn`, `BuildWork`, `Assets.Finish`, `Goods.Consume(ConsumeReason.Built)` (the material part *is* physical and conserving).

### 4.4 M4 care reflexes / executor delivery

`HouseholdActs.Collect` (`:274`) is the heir "collection" executor; `Estates.Tick` is the "executor delivery": sim-side `CustomSettlement` (`ChooseExecutor`, `RuleOf`, `Split`; `Estates.cs:50-60,203`). See 2.1. Care: `HouseholdActs.Feed` (`:164-177`), `LifeCycle`, `Orgs/Health.cs` (`Treat`; [U] herbs from `SourceKind.Roots`: baseline R23).

### 4.5 Where policy issues actions

`Policy/*`: `AssociativeNpcPolicy` is the default `INpcPolicy` ([U]: read only the OpportunitySlot reference); `Cognition/Thinker.cs` builds candidates (`BuildCandidates`, `Sensed` classes, `AddBase`) from perception; selection = instinct + learned + personality - cost + explore + social + divine (`OpportunitySlot`, `Mind.cs:194-205`). The policy chooses a *candidate verb on a perceived target*, and `Start/Act` executors do all the economics. So prescriptive logic lives in (i) candidate generation (what is on the menu: 1.3), (ii) instinct priors (4.2), (iii) act executors (4.1). The god director (`Divinity/GodDirector.cs`, order 910) and faction `AiDirector` (order 900) are separate faction/god-level controllers: the former is god-only (retain), the latter must be off in unified mode (baseline 2).

### 4.6 Reflexes in the sim loop

- Hunger/sleep/cold/fear drives and the reward from drive change in `PopulationSystem.Think` ([U] line-level: baseline section 15 cites `PopulationSystem.cs`, `Features.cs`): five `Drive`s (`Features.cs` `Drive.Count`), no Thirst/Hygiene/Comfort/Fun/Bladder (R19).
- `StartSleep` with default on-the-spot sleep (instinct comment `Instincts.cs:28`). K physical.
- Flee: `Verb.Flee` is a candidate + `NpcVerb.Flee` and `SocialVerb.SayWarn`; fear drive raised by `Miracles.cs:432-454` (smite) only. No fear source from NPC conflict exists yet.

---

## 5. Faction-level economy

The baseline (sections 2-3, 6-8, 12-13) already inventories orders 10/200/300/350/400/900 (Dice, Economy, Military, Trade, Factions, AiDirector), HQ stock, carriers/builders, `Work.cs` constraints and the AiBrain. This audit only adds what it missed:

1. `Information/FactionKnowledgeView.cs` (`Own` books: "exact (the state knows its treasury, sites, buildings and orders)", `:13`) feeds the faction AI an omniscient own-books view; **in unified mode the faction view must not feed NPC candidates**. Not an NPC reader today [U].
2. `OwnerKind` already has `State`/`Group` kinds that map to faction parties in the warehouse (`WarehouseRecorder.cs:235`): the faction store/ledger is an ownership kind inside `Goods`; Stage 3 `EconomySystem` goods are *not* `Goods` lots [U]. If the unified island ever routes HQ stock through `Goods` the owner kind would become a faction title.
3. `Trade/TradeSystem.cs`, `AiTrader.cs`: envoy/convoy atomic settlement is the only genuinely atomic paired transfer in the code [U: not read]. Faction-level, outside D9's NPC scope, but must not run beside NPC exchange.
4. `Divinity/DivinityModule.cs:69-70` G3 forbids god ownership; consistent with D9; keep.

---

## 6. Military (CF-01 reuse)

Existing combat is **faction/soldier-level**, not persistent-NPC:

| Piece | Evidence | Reusable? |
|---|---|---|
| `Duel` pure attack math | `Military/Duel.cs:34-80`: `HitChance` (weapon hit% minus dodge, close-quarters penalty), `BlockChance`, `HitDamage` (`:62`), `AttackResult{Miss,Blocked,Hit,Crit,Building}`, rolls in fixed RNG order | Yes: pure function of `SoldierStats` + `Rng`; give it NPC stats from gear (`Wares.Gear.Attack/Defence`: `Economy/Wares.cs:152`; `AiBrain.Gear.cs:166-174` already calls it for a "sword" brain action) |
| `MilitaryState`/`MilUnit`/`GroupBattle`, `MilitarySystem` order 300 | Abstract units, formations, sieges (`MilitarySystem.cs:303`) | No for CF-01 ("no faction-level military AI or spawned abstract soldiers"); keep for the stage 3 game |
| `Bodies.Damage(s, ev, body, injuryMicro, DamageCause, actor, intervention)` | `Population/Bodies.cs:36-50`: injury and health loss, death -> `Kill` with `DeathCause.Violence`; the divine hook handles protection/avatar bodies. `DamageCause.Violence` exists (`:9`) but **only `Miracles.cs:427` (smite) calls it** ("No caller in F1") | **Yes, the key seam**: a persistent-NPC strike applies `Bodies.Damage(..., DamageCause.Violence, actor)` |
| `Bodies.Heal(.. HealKind.Injury ..)` | `:52-58` | Yes: remedy `USE` on an injured person |
| Injury/pain drive | `Npc.Injury`, `Drive.Pain = max(Injury, PainExtra)` (`Miracles.cs:352`); `SelfFeature.Injured` (`Thinker.cs:339`); instinct pain -> `Rest` | Yes (pain is an existing drive) |
| Fear | `Drive.Fear` (`Features.cs`); raised only by smite (`Miracles.cs:432-454`) | Partial: needs a witness-local fear source on seeing a strike |
| Death | `PopulationModule.Kill` (`:84`) with carry spill | Yes |
| Perception of `EnemyInSight` | `SituationKind.EnemyInSight` [U]: how it is set (faction hostility?) | Must become identity-neutral (same/cross tribe equal) |
| `NpcVerb.Attack/Flee/Surrender` | faction-soldier verbs; `AiBrain.Military.cs` | No (policy classes: forbidden) |
| `Lab/Experiments/L01HawkDove.cs, L02Retaliator.cs, Contests.cs, ContestStudy.cs` | exact game-theory lab (kept separate by the amendment) | K separate. Provides the *measurement* vocabulary (Hawk/Dove/Retaliator) for CF-02 analytics; must not become island policy classes |

Gap list: an NPC-level strike resolver (reach check = `Reach.Touch` style, fatigue/work time, hit roll via `Duel` with NPC stats, `Bodies.Damage`), witnessed-injury perception, fear from observed violence, protective equipment (`Wares.Gear.Defence`, `Equip`).

---

## 7. Seams to reuse

| Seam | Evidence | Use |
|---|---|---|
| **F1 control seam** | `Divinity/Control.cs`: `ControlTable`/`ControlEntry` (`:20-86`), `Control.TryAcquire/Release/Sweep` (`:118-172`), `ForceThink`, `SkipsPolicy`, `ControlIntent`, `OverrideIntent` (`:242-300`) (the decision-override seam), heading/lease resolution `ResolveMovement` (`:359`) and `DivineControlInput` (executed in the D4 slot, `Possession.cs:327-`); `Embodiment.CheckAction/Accepts` (`:90,162`) | Already routes a possessed body through the *same* act executors as an autonomous one (`EmbodiedVerb`, `DivineActs.Map`). A single primitive executor can be a new `ControlInputKind` set (step, turn, take, drop, use, gesture ...) resolved in the same slot, so learned policy, possession and tests share it (D9.2, QA-01). Risk: `Possession.CheckAction` restricts acts; the controller currently emits *verbs*, not primitives |
| `Goods` physical API | `Goods.Move` reach checks (`Reach.Of`, `Goods.cs:169-170`; `Reach.cs` table: PickUp/Claim Touch, Drop/DeathSpill Enter, HandOver 1, Deposit/Withdraw Store 1), `InReach` (`:350`), `Produce/Consume/Wear/Fits/Insert`, `NewBin`, `WeightCap`, carry slots and `CarryWeight`, `MoveResult` statuses (`OutOfReach, NoRoom, NotEnough`) | Physical core; reuse as is. Remove owner effects (section 1) |
| Conservation invariants C1-C7 | `Population/PopulationInvariants.cs` (C4 ground ownerless / dead-owner, C5 no bin over cap, no carry over weight ...); `Core/SimInvariants.cs:70` | C1-C3, C5-C7 are physical and reusable; C4 must be replaced (1.6) |
| `OpportunitySlot` / `ThinkScratch` | `Cognition/Mind.cs:194-205`, `Thinker.cs` (Sensed, candidates, `AddBase`), `Mind.Remember` experience slots | Trace/explanation per decision; learner features. Training can read slot terms (instinct vs learned). Needs a primitive-action candidate set and legality-mask-only filter |
| Learner / episodes | `Learning/Learner.cs`, `AssociationStore.cs`, `EpisodeKey.cs`, `Imitation.cs` (Origin Own/Vicarious/Imitated), `Learner.AwaitOutcome/Outcome` | Delayed credit (deferred verbs: `Build`, `ForageAt`, `PickUp`, `Equip`: `Verbs.cs:75-77`), lot episodes (`GoodsCredit.Realised`), vicarious/imitated origins: a base for sequence memory and child observation. **Currently associative table over verb x feature keys, not a motor policy** (baseline R21). Train/inference parity needs a new versioned artifact |
| Perception | `Thinker.cs` sensing, `Cognition/People.cs`, `Sensed` (`HouseholdHooks.SenseBins`, `Sense` seams), sight radii `SightDay/SightNight` (`Ownership.Witness`, `:162-181`), `Comms/CommsRange.cs`, `Comms/Messages.cs` (GESTURE: `Emit`, `Perceive`, mismatch, `Honest`) | Observation records; witnesses (the `Witness` loop is the template for witness-local fear/claims) |
| Messages / gestures | `Comms/Messages.cs:23-155`: speaker, act, topic, quantity 0-9, counter, target, time, `DealId`; `GestureAct` set includes Want/Offer/Accept/Refuse/NeedFood/NeedHelp/Cooperate/Warn | The offer/promise channel (amendment 4). Needs threat/submission/protest acts and a claim topic [U: which `GestureAct` values exist] |
| Beliefs about ownership | `CivMind.Own` (`OwnBelief{BelievedOwner, Confidence, MyRights, Tick}`) | A claim record: claimant, subject, source, confidence, time, expiry (add expiry and subject kinds) |
| Hash/save | `StateHasher`, `GodSim.StateHash` per-module parts (`GodSim.cs:198-202`) | QA-01: unified mode adds its own part; legacy parts untouched |

---

## 8. Prioritised changes by package

Priority P0 = blocks everything, P1 = needed for the first mechanical gates, P2 = later.

### UN0 foundation switch (P0)
1. One `unified` flag (setup/tuning, saved) read by every site below; with it off, the legacy hash is bit-identical. **Pin risk: all legacy hashes** (`StateHash` parts for Population/Households/Social/Civ/Orgs).
2. Make M3/M4/M5 optional parts of the island: unified mode disables `Estate.EstateSystem` (`HouseholdHooks.cs:27`), `LifeCycleSystem` ownership side, `Ownership.Classify/DayStart` (`CivSystem.cs:17,23`), Org/Civ systems where they imply title (or leave on with neutralised readers).
3. Detach `AiDirector`/`AiBrain` Stage 3 factions and `Economy/Trade` from the unified island (baseline), keep `GodDirector`.
4. A grep-based guard test: no `.Owner`/`OwnerRelation`/`Permits.*` read on the unified candidate or act path.

### Physical goods / no-title (P0-P1)
1. `Goods.OwnerAt`, `Insert`, `Mergeable`, `Produce`, `SetOwner`, `PayOwnerChange/PayProduce/PaySpoiled`: unified branch (owner constant `None`; no `OwnershipChanged`; rewards from need credit). Keep `ItemMoved` as objective history; add provenance (producer, mover, tick) as separate records.
2. Replace `Held` (`SocialCandidates.cs:41-52`), `SocialSim.cs:708-711`, `HouseholdActs.cs` own-filters (`:164-166,262,275,300-301,325,342,405`), `HouseholdHooks.SenseBins` (`:120-153`) and `UsableFood` (`:364`), `CommunalStore.cs:129,153,216`, `OrgMindHooks.C6.cs:377`, D2/D4 own-lot filters (`DivineActs.cs:403-408`, `DivinityHooks.D2.cs:73`, `D4.cs:61`, `DivinityMindHooks.D4.cs:183`), `Work.cs` (`:86,112,191,229,254,284,311,316`), `Employment.cs` (`:91,208,222,578,585`) with physical-only (reach, weight, capacity) variants.
3. Neutralise `Permits.*` (`CivMindHooks.cs:76-77,98,158,176`; `.C3.cs:168,386`).
4. Replace estates: no `Estates.Open`; death leaves goods where they are (carry drop exists); new location/conservation invariant replacing C4 and `HouseholdInvariants` estate rules; hut `Rights/Contributors` neutral.
5. Theft/crime: stop `Ownership.Classify`/`LostLot`/`Property.Claim` in unified mode; relabel warehouse `fact_theft/fact_ownership/fact_use_right/fact_inheritance` as legacy-only (QA-02).

### Primitive executor (P0-P1)
1. A single executor for the 12 controls over the existing F1 seam (`Control.cs`, `DivineControlInput`), reusing `Goods` (take, drop, store, hand over, consume), `Reach`, `Bodies`, `Messages.Emit`; same code for autonomous, possessed, test actors (QA-01).
2. Unified candidate generation emits primitives with legality masks only (reach, capacity, occupancy, barrier), not verbs. Keep the old verb table for legacy.
3. Disable instinct tables in unified mode (`InstinctSet.None` exists: `Instincts.cs:25-29,92`); the trained policy replaces them. Remove the shelter instinct (`:43-45`) from the unified set.

### Construction (P1)
1. Known designs as brain knowledge (stable catalogue ids); `USE` on a valid site places a site; `USE` with material contributes (`Goods.Consume(ConsumeReason.Built)` path in `Work.Build` is reusable); labour via `BuildWork`/`WorkProgress` style counters; interruption/abandonment/resumption. Replace `Work.CanFound` (omniscient truth checks) and the two-kind-per-plot offer logic (`CivMindHooks.cs:60-70`).
2. Remove builder privilege: `PopulationSystem.FinishHut` bed rights (`:1052-1074`), `Work.Build` owner gate (`Work.cs:112`), `Assets.Create(owner)`; record founders/contributors as provenance.
3. Level-1 shelters labour-only; raw extraction requires a completed collection building plus deposit capacity; temporary capacity reservation with expiry (new; bin `Fits` is the base).
4. Roads connect existing settlements only (baseline section 6).

### Exchange (P1)
1. Offer/promise via GESTURE and referents (`Comms/Messages.cs`); handover through the one generic `Goods.Move(HandOver)`; DealId as optional correlation only. M5 already supports defection (`M5BarterTests.cs:25`): keep `CheckDeals` as subjective memory; remove owner filters and the `Exchanged/Gift` title basis. Quantity range 0-9 (`Messages.cs:48`) may need widening [U].
2. Receiver cooperation to receive (EP-03): `Reach.HandOver` check exists; "refuses to receive" is new.
3. C3 wages, C6 rations, loans: treat as later interpretations; neutralise `Employment.Activate` UseRight, `Orgs.Dissolve` re-owning, org ledger as authority.

### Conflict (P1-P2)
1. NPC strike via `USE` (reach, work time, fatigue) -> `Duel` math (`Military/Duel.cs`) -> `Bodies.Damage(DamageCause.Violence, actor)` (`Bodies.cs:36`); protective equipment equip; remedy via `Bodies.Heal`; retreat is movement.
2. Witness-local fear and observed-injury memory (template: `Ownership.Witness` loop); `SocialMemory` grudges stay subjective; no `TheftCommitted`.
3. Make `SituationKind.EnemyInSight` and hostility identity-neutral; faction `Attack/Charge` verbs stay Stage 3 only.

### Training (P1, critical path)
1. Trainer against the real simulator with the same controls; versioned deterministic C# inference; `Learner` associative store is not a motor policy (R21): new artifact, hash, seed split, thresholds before held-out results.
2. Reward from needs (drives), not owner change: remove `PayOwnerChange` reward in unified mode (`Goods.cs:289-305`) and goods-worth shaping (`GoodsWorth`) as a profession reward.
3. Child inheritance of biology/temperament only: `Genetics/Genome.cs`; verify `Imitation.cs` does not copy adult memory [U].

### Legacy pins at risk
- All `StateHash` parts if any `Goods` field/event changes; `Mergeable` change alters lot ids and stack merges (`Goods.cs:477,519,556`).
- Event registry/telemetry ids (`CivEvents.C1.cs:16-17`; `OwnershipChanged`, `TheftCommitted`, `EstateTransfer`) and warehouse table ids (`Tables.C1.cs`, `PopulationMapping.cs:95`, `HouseholdMappings.cs:111`): keep definitions, just do not emit in unified mode.
- Tests that assert ownership behaviour (known: `M5BarterTests.cs:25` is *compatible*; other `DotNet/God.Runner.Tests/**` ownership/estate/theft/C4 tests not enumerated [U]) and invariants C4, `HouseholdInvariants`, ORG4, CIV1, CIV3.
- `Learner` reward totals (owner-change rewards) feeding any scenario hash with learning on.
- Save format: legacy `Owner/Rights/Lost/Claims` serialisation (`CivState.C1.cs:67`, `PopulationModule.cs:393-394`, `Estates` `AddTo`): keep fields, neutral.

## 9. Unverified list

- Exact readers of `HandOver.OwnerChanged` in `Social/SocialHooks.cs` and victim-side inference in `SocialMemory.cs`.
- `Social/Valuation.cs` and `AssociativeNpcPolicy` contents; `Policy/*` selection path.
- Which `GestureAct` values exist (threat/submission/protest) and whether `Quantity` 0-9 fits.
- How `SituationKind.EnemyInSight` is derived and what sets `Drive.Fear` besides smite.
- `Orgs/Health.cs`, `Education.cs`, `Services.cs`, `Imitation.cs` internals; `Stage 3` `Trade/Economy` ledgers' relation to `Goods`.
- Whether every `Goods.Move` call from `Lab/**` and `Ai/**` is irrelevant to the island (excluded from grep).
- Full list of DotNet tests pinned to ownership behaviour.

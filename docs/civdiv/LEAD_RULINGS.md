# Lead rulings on planning inconsistencies (27 Nov 2026)

The lead's binding rulings on conflicts found between the package index, the shared contracts and the design documents while the package specs were written. Where a ruling and a design document disagree, the ruling wins until the document is corrected; the contracts still govern everything not listed here. These are reversible implementation decisions, not owner decisions.

| # | Conflict | Ruling |
|---|---|---|
| R1 | Scripted test gods: contracts §10.1 last sentence vs the index §0 | A scripted test god is an `Ai`-seated `ScheduleGod` acting from inside the sim through the god director; it never takes the `Human` seat. Contracts §20 gets the amendment row. |
| R2 | U4 is folded into U3, but contracts F1.18, §19.1 and several design docs still name U4 | Read "U4" as **U3** everywhere (U3b owns animation, names display and divine effects). F1.18's names table goes to U3. |
| R3 | `PopulationModule` is a non-partial static class; D3 planned a partial file | D3 adds a new static class `PopulationMiracles.cs`; F1 does not make `PopulationModule` partial. |
| R4 | RevealKnowledge (`MiracleKind` 10) not placed in the index | In D3, Tier 2 (after heal, smite, create, destroy). It calls the K1 seam when K1 exists; until then it reveals only knowledge kinds that already exist in minds. |
| R5 | Tier 1 commandment templates: index lists T1, T3, T4, T6, T7, T12; COMMANDMENTS §9 adds T11 | Index set is Tier 1; T11 is Tier 2. |
| R6 | `CommandmentText.Render` inside the sim (index) vs outside (design) | **Outside the sim**, in U2 (Game layer), with the static test that no sim code uses it. The shared adoption function in `Sim/Culture` is owned by **D6**. |
| R7 | Warehouse table names differ between design docs and contracts | **Contracts govern:** `fact_possession`, `fact_life_spell`, the contracts' archive model. Design docs are corrected in the review. |
| R8 | Pause rule: code steps one tick on a command while paused; contracts and UI want queueing | **Queue while paused** (contracts §11.3) in the god-only mode; Stage 3 mode keeps its behaviour. U1 owns the flagged `GodBootstrap.cs` edit. |
| R9 | Shared Game files without one owner | `GodHud.cs` and `GodHud.Gui.cs`: **U1 owns**, U2 adds only through a registration hook U1 provides (or a flagged additive edit merged after U1). People-panel tabs become data-driven registrations (U2 Divine, U5 Economy/Life/Family, I6 industrial). `TerritoryView.cs` and `HexDressingView.cs`: U3 may make flagged edits for overlay gating. |
| R10 | Divine effects Tier 1 at Checkpoint B (design) vs all effects in U3b Tier 2 (index) | **Minimal distinct cues** for suggestion, command, compulsion, heal, smite and create are **Tier 1 in U2a** (simple procedural markers driven by events); the rich effects stay in U3b. |
| R11 | Commandment builder Tier 1 (design) vs U2b (index) | **Template picker** (choose a template, fill its slots, reveal) is **Tier 1 in U2a**; the free structured builder stays U2b. |
| R12 | Mythic regeneration per day (contracts, MYTHIC) vs "+1 per second" (UI docs) | **Per sim day** is canonical (contracts §5); the UI shows the per-day rate and the time to full. |
| R13 | Provisional names in PLAYER_UI, VISUAL_WORLD, CAMERA | Use the contracts' names. U2 builds its catalogue from `Gate.Check`; there is no sim-side `IDivineCatalog`. |
| R14 | Animation planner tests under `dotnet test` vs Game assembly | Its tests run in Unity EditMode; the compile check covers compilation. |
| R15 | Industrial drafts claim `DivineAct` 22/23, `ObjClass.Vehicle = 117`, verbs 21/22 | Settled only by the contracts' **Industry addendum** (lead/planner). Until then these numbers are provisional. |
| R16 | Documents cited but not yet written: dependency graph, Industry addendum | Being written by the planner; nothing implements against them until they exist. |
| R17 | Branch names `unity/q1`, `unity/q3` taken by stage 3 | Use `unity/q1kpi` / `wt-lrs-q1kpi` and `unity/q3int` / `wt-lrs-q3int`. F0 is `unity/int0`. |
| R18 | KPI views in `0030` vs merge order | `0026` is allocated to **early views** (population, divine core, Tier 1 KPIs), landed by Q1 after F1b and D1. `0030` lands **late** (after C6/C10/D6), holding the org, service and religion views. |
| R19 | `Reproduction.cs` does not exist (class inside `LifeCycle.cs`) | F2 rebases after F1b and puts new code in a new `Population/Households/Conception.cs`; edits to `LifeCycle.cs` are flagged and minimal. |
| R20 | No `AssetId` row in contracts §1 | **C1 owns `AssetId`** (one id space for every productive asset: buildings, workshops, vehicles, power stations); contracts §1 gets the row. Industry uses it. |
| R21 | No `OrgState.C3.cs` in F1.2 | F1 adds `OrgState.C3.cs` as an empty partial alongside C6–C10. |
| R22 | No Herbs source kind | C7 gathers herbs at `Roots` sources (default). A dedicated `SourceKind` waits for an amendment. |
| R23 | Org event block 500–519 is tight | C6 may use **500–549**; welfare uses 551–559; 550 stays reserved for banking. |
| R24 | Event names differ between design docs and contracts §15.1 | **Contracts names govern** (for example `LoanMade`); design docs are corrected in the review. |
| R25 | `OwnerKind.Org`, `OrgStore` owner; healer template owner | Enums land in **F1**; the private healer template is **C7**'s. |
| R26 | When is a two-person loan formal | Every completed `Lend` is registered in C2's obligation registry; "formal" (recognised by an organisation) is a flag on the record. |
| R27 | Seeded governments vs pin P5 | `Orgs.C10.SeedGovernments`, default **0** in pinned scenarios, **1** in the god-only Living Island and the industrial scenario. |
| R28 | `InstitutionView` fields for C10 | C6 adds them additively on C10's request (perceived instructions, god beliefs). |
| R29 | No god-only boot flag | **U1a** adds `-godonly` (with `-godpopulation`) for the god-mode smoke before Checkpoint B. |
| R30 | KPI Tier 1 subset | Q1's Tier 1 subset includes HLT-10, LIV-07 and TIM-04. |
| R31 | `SimBuilder.cs` director attachment shared by C6 and D1 | **D1 owns** the edit (lands first); C6 adds its director through the same registration point. |
| R32 | Industrial and knowledge numbers | Settled by the contracts' Industry addendum: industry events **1000–1099** (domains 70–75), migrations **0031–0037**; knowledge and money events **1100–1199** (domains from 76), migrations **0038+**. |
| R33 | Industrial docs refuse production without a licence; knowledge doc says only knowledge gates it | **Only knowledge (and physical inputs) gates production.** Respecting rights is the owner organisation's own policy (`RespectRights`); unlicensed use is classified as `UnauthorisedDesignUse` and can become a dispute (owner D6.8/D6.11 acceptance test). INDUSTRIAL_RESOURCES §4.4 and ELECTRICITY §4.1 are corrected accordingly. |
| R34 | Knowledge and money shared numbers | `OwnershipBasis.Paid = 26`; `NpcVerb` block 210–219 for K1/K2 (200–209 Industry); wares Document 63, Paper 64, Coin 65 (optional); `Wares.IsUnique` replaces the axe-only check in `Goods.cs` (behaviour-neutral, landed by I1a); state slots `CivState.K1`, `CivState.K2`; switches `Civ.Knowledge.Enabled`, `Civ.Money.Enabled`; service kinds 20–23. All recorded in the contracts' Industry addendum. |
| R35 | "D6" means both owner decision set D6 and package D6 (commandments) | Owner decisions are always written **D6.n** (with a number); the commandments package is written **package D6** or **D6 (commandments)**. |
| R36 | R23 overlaps C7 520–539 and C2 540–549 | C6 uses only codes in 500–549 that no other package holds (the index's reading). |
| R37 | Run length | The owner asked to keep working until a finished playable game: the 'continuation' after H18 is part of this run, not a later session. Checkpoint G (about H25) is in scope. |
| R38 | No contract for seeding scenario content (deposits, facilities, vehicles, orgs, agreements, knowledge, coin) | Accepted as **I1a item 17**: a deterministic scenario-content loader in the runner/`SimBuilder` that issues ordinary setup commands at tick 0 (no hidden state injection); contracts §20 gets the row. |
| R39 | Knowledge and rights checks needed before K1 exists | Accepted as **I1a item 18**: `KnowledgeQuery` and `RightsQuery` stubs (everyone knows `DesignId 0` designs; no rights recorded), replaced by K1. |
| R40 | Industry card data for the HUD | Per-package `Information/IndustryCards.*` partials (sim-side, read-only views), consumed by I6. |
| R41 | Royalties: automatic accrual (KNOW §8.4) vs none (MONEY §7.1) | **MONEY governs:** no automatic accrual; royalties are billed by an office-holder's act and paid by actual payment. |
| R42 | Shared skill store lives in `OrgState.C7` (needs Orgs on) | The skill store lives in **`CivState`** (exists when any Civ/Industry switch is on), created lazily; C7 and K1 both use it. |
| R43 | Agreement record kinds | I3's agreement record carries `Kind` (Supply, Electricity, Procurement, Haulage); I4's billing depends softly on I3 (fallback: C2 obligations only). |
| R44 | Smoke flag, tag and scenario names | `-godonly -godpopulation`; tag `civdiv-g` for Checkpoint G; scenario `ind_slice`. WST incineration uses `Disposed`. |
| R45 | Numbers on unity/poc differ from what the contracts assume (`MaxCand` 24, `PersonalCount` 32, `MoveReason` to 9, no `BinKind.AssetStore`) | F1 raises them to the contract values (36, 40, and the listed enums); I1a asserts the post-F1 values before its own changes. |
| R46 | Rulings vs contracts text not yet amended (U4 names, skill-store owner) | Where a ruling and the contracts disagree, **the ruling wins**; the contracts are amended by the package that next touches the section. |
| R47 | `Information/CommandmentText` owner | R6 governs: U2 (Game layer). The index entry under package D6 is superseded. |
| R48 | C7 and K1 specs say `OrgState.C7.Skills` | R42 governs: the skill store is in `CivState`. |
| R49 | `fact_org_decision` without a migration; migration 0020 unused | C6 adds `fact_org_decision` to its own migration 0018. 0020 (C8's slot) stays reserved and unused. |
| R50 | Draft org and service events without contract codes | A package allocates a code from its own block only if the warehouse must reconcile the event; otherwise it stays state. |
| R51 | Tier 1 scenario page, generated islands and tutorial rows naming deferred U6 | The scenario menu entries (Living island, god-only, unlimited Mythic, Iron Coast) are added by **U2b**; generated islands already exist; the tutorial is Tier 3, after Checkpoint G. |
| R52 | RESURRECTION §4.2–4.6 still describe the old archive model | Left under the notice; D7 rewrites them when it merges. |
| R53 | No deconsecration event | D3 may add `LocationDeconsecrated` at code 830 if it implements deconsecration. |

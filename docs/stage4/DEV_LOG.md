# Stage 4 development log

The persistent log of the autonomous Stage 4 implementation run. Every session reads it first and updates it before it stops. It records what is true now; the design itself lives in the documents listed in [DESIGN_CONVENTIONS.md §11](DESIGN_CONVENTIONS.md#11-document-map-one-owner-topic-each-link-dont-repeat), and the gates in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

Rules for this file:
- Newest entries go at the top of each section. Dates are real dates; commits are short hashes.
- Say plainly what ran and what did not ([STAGE_4_HANDOVER.md §8](../STAGE_4_HANDOVER.md#8-working-rules-from-the-owner-and-earlier-stages)).
- A design change made during implementation is recorded here **and** in its owning document in the same commit.

## Current status

| Item | Value |
|---|---|
| Current milestone | **M3** (NPC core with instincts and lifetime learning) in worktree `wt-lrs-m3`, plus **M13a infrastructure** (SQLite warehouse core) in `wt-lrs-m13a`, in parallel. M0, M1 and M2 are done and merged, which completes checkpoint 4A. |
| Current checkpoint | **4A Simulation laboratory** (M0–M2) |
| Next functional release | **First playable new game**: M0–M7 and M9 ([conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list)) |
| Owner questions | None open: all 23 answered on 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)) |
| Last updated | 27 Nov 2026 (docs update for the owner's answers) |

## Completed milestones

| Milestone | Checkpoint | Completed | Commit | Notes |
|---|---|---|---|---|
| M2 | 4A | 27 Nov 2026 | merge into `unity/poc` (`unity/m2`: `2b2a972`, `c9d2813`, `0d86851`, `bd42cb9`) | `Sim/Lab`: Q30 fixed point; benchmark state machines; replicator (including two populations with RK4); agent-based engine; tournaments; presets L1–L13 with status labels. Runner `lab` list/run/all --check/report/hash. `lab all --check`: 70 runs, 70 pass, 33.8 s on one core. Nine expectations were **amended with evidence** (see [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)). One is a finding for the owner: L2's Hawk/Bully claim doesn't hold under default drift (Retaliator stays dominant; the system is bistable). |
| M1 | 4A | 27 Nov 2026 | merge into `unity/poc` (`unity/m1`: `a112a21`, `324c96b`) | Four policy interfaces; `PolicyRegistry`; `AiBrainFactionPolicy`; `Stage3MilitaryPolicy` (the military seam also done); `FactionKnowledgeView` (live and filtered: own books exact, foreign stores never, Q21); `KnowledgeProfile`; true-state overlay as a host setting (Q22). The HUD reads only the view, and the reflection test's exception list is empty. Pause rule replaces `StepHeld`. Seeds 1–12 replay the M0 hashes exactly. |
| M0 | 4A | 27 Nov 2026 | merge into `unity/poc` (branch `unity/m0`: `46d9fad`, `d44aa05`, `1dfb80f`, `acae4c1`) | Delivered: `SimTuning` (no mutable statics; audit test), keyed `Rng` streams, `SimCalendar`/`LifeScale`, `VictorySpec` list (VP10 + LastStanding default; Sandbox), `StateHashParts` including pending commands, domain events and `EventCatalog`, the `God.Recording` JSONL recorder (Off/Metrics/Summary/Full), `God.Runner` (run, batch, verify, replay, bench), save v3, `SimBuilder` shared with Unity's SmokeRun, and the HUD calendar. The Unity player and the runner give identical hashes. |

## Commits and branches

| Date | Branch | Commit | What |
|---|---|---|---|
| 27 Nov 2026 | `unity/poc` | `10a7142` | M1 and M2 merged (pushed) |
| 27 Nov 2026 | `unity/poc` | merge of `unity/m0` | M0 merged (4 commits on `unity/m0`, listed above) |
| 27 Nov 2026 | `unity/poc` | `c584818` | Owner's 23 answers recorded, roadmap 4A–4F, DEV_LOG created |
| 27 Nov 2026 | `unity/poc` | `e4bc98d` | Stage 4 design: 24 design docs, conventions, acceptance tests, handover |
| 27 Nov 2026 | tag | `stage3-final` → `d7eeb95` | Stable stage 3 checkpoint (replaces Stage 3 save compatibility) |
| 27 Nov 2026 | `unity/poc` | `d7eeb95` | Last stage 3 code commit |

Branch rules for this run: commit and push tested changes to `unity/poc` and to feature branches (`unity/<name>`); **never push to `main`** ([handover §8](../STAGE_4_HANDOVER.md#8-working-rules-from-the-owner-and-earlier-stages)).

## Test results

| Date | Commit | Suite | Result | Notes |
|---|---|---|---|---|
| 27 Nov 2026 | `3463d30` (**checkpoint 4A**, merged tree) | Unity EditMode / PlayMode; Windows build; player smoke vs runner | EditMode 363/363 and PlayMode 77/77. Build OK (87 MB, 0 errors). Player smoke seed 2: Crimson at tick 20161, `4bd54b271857a803`, invariants ok, 0 exceptions. The runner gives the same result. | Run by the lead; closes 4A. |
| 27 Nov 2026 | `10a7142` (M0+M1+M2) | `dotnet test DotNet -c Release`; `lab all --check` | God.Sim 359/359, God.Runner 90/90; lab 70/70 in 33.8 s | Rerun by the lead after merging. |
| 27 Nov 2026 | `324c96b` (unity/m1) | Unity EditMode / PlayMode; build; player smoke | 336/336 and 77/77; build OK. Smoke seed 2: tick 20161, `4bd54b271857a803`; seed 1: 17534, `12be0f64cefc59f1`. Both equal the runner. | Run by the M1 agent. Background Unity runs took 45–100 minutes each because of editor throttling. |
| 27 Nov 2026 | `bd42cb9` (unity/m2) | Unity EditMode | 340/340 | Run by the M2 agent. |
| 27 Nov 2026 | M0 merge | `dotnet test DotNet -c Release` | God.Sim.Tests 313/313, God.Runner.Tests 48/48 | Rerun by the lead after merging. |
| 27 Nov 2026 | `acae4c1` (unity/m0) | Unity EditMode / PlayMode | 313/313 and 74/74 | Run by the M0 agent in its worktree. A golden hash for the double-based rules matched under Mono. |
| 27 Nov 2026 | `acae4c1` (unity/m0) | Windows build and player smoke | Build OK (87 MB). Seed 2: Crimson at tick 20161, hash `4bd54b271857a803`, the same as the runner; `verify --against-unity` matched 16 checkpoints. Seed 1: matched 14 checkpoints. | M0.2 ran 4 seeds × 2 years in CI; the 200-year nightly run has not been done. |
| 27 Nov 2026 | `e4bc98d` | `dotnet test DotNet -c Release` | 282 / 282 passed | Baseline for Stage 4 (docs-only commit on top of `d7eeb95`) |
| 27 Nov 2026 | `d7eeb95` | Unity EditMode | 282 / 282 passed | Baseline |
| 27 Nov 2026 | `d7eeb95` | Unity PlayMode | 73 / 73 passed | Baseline |

## Performance benchmarks
The owner's metrics (27 Nov), measured by `God.Runner bench` ([HEADLESS_SIMULATION.md §4.10](HEADLESS_SIMULATION.md#410-performance-targets-and-profiling)) on the reference machine unless stated. A column that does not exist yet at a milestone stays blank.

| Date | Commit | Case | Ticks/s | Sim years per real second | NPC decisions/s | Memory per NPC | Pathfinding cost | Learning cost | Communication cost | Telemetry and database overhead | Save size | Load time | Parallel throughput |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 27 Nov 2026 | `46d9fad`+`d44aa05` | Stage 3 watch6 (sandbox), no NPCs, recording Off, Ryzen 7 5800X | 9,758 (976× real time) | 0.81 (12,000-tick year) | - | - | - | - | - | - | 467 KB (saved in 89 ms) | 109 ms | 1 worker 16,493 ticks/s; 4 workers 59,546 (scaling 0.90); 8 workers 97,529 (0.74, below the 0.8 target); 16 logical 103,982. Per tick: 0.103 ms mean, 0.380 ms at p99; 47 KB allocated. |

## Design decisions taken during implementation
Choices the docs left open, or that implementation forced, with the reason and the owning document updated. The owner has said not to ask questions that have reasonable defaults: take the default, record it here.

| Date | Milestone | Decision | Why | Owning doc updated |
|---|---|---|---|---|
| 27 Nov 2026 | M1 | `FactionKnowledgeView` answers live (no copies). Own books are exact. The map, units, building health and envoys are by observation (stage 3 has no fog). Wars are public; peace offers are seen by the two sides; trade only where involved; foreign stores never. | Q21; cheap in stage 3 | INFORMATION_AND_MEDIA (the lead records it) |
| 27 Nov 2026 | M1 | `Purpose` and `AllowTrueStateOverlay` are host settings, neither hashed nor saved. Competitive play has no overlay; watch mode is observation (allowed); `-goddev` is developer mode. | Q22; the overlay can't change the sim | INFORMATION_AND_MEDIA |
| 27 Nov 2026 | M1 | Pause rule: a paused game takes one ordinary tick (AI included) when a person's command is accepted. Nothing else moves a paused game. | Replaces `StepHeld`. About 40 PlayMode tests and placing while paused need commands to show at once | HEADLESS_SIMULATION |
| 27 Nov 2026 | M1 | The military policy seam is done in M1 (`Stage3MilitaryPolicy`, per-soldier forked stream) | MILITARY_DESIGN and M1.4 | MILITARY_DESIGN |
| 27 Nov 2026 | M1 | The HUD reflection tests live in a Unity-only EditMode assembly `God.Game.EditTests` | The HUD needs UnityEngine, so it can't run under `dotnet test` | ACCEPTANCE M1.2/M1.3 |
| 27 Nov 2026 | M2 | Lab numbers: Q30 fixed point; exact 128-bit MulDiv; integer Ln/Exp. Contest rules settled: a display block ends a contest; withdrawal with no mutual exchange means no prize and no cost. | Determinism, and gaps in the spec | STRATEGY_BENCHMARKS |
| 27 Nov 2026 | M0 | Tuning in `GodState.Tuning` (`MilitaryTuning` is an instance; a `Decay` block), set through integer dotted paths. Static arrays are `ReadOnlyArray<T>`; `BuildingDef` is readonly. | Removes all mutable statics so parallel runs can use different tuning | HEADLESS_SIMULATION (the lead records it) |
| 27 Nov 2026 | M0 | Dice draw from `Rng.Fork("dice", roll)` and duels from `Rng.Fork("duel", attacker, tick)`. The root `GodState.Rng` never advances. | Keyed streams (conventions §3) | conventions §3 |
| 27 Nov 2026 | M0 | Victory is a `VictorySpec` list. The default [VP10, LastStanding] is exactly the old rule; Sandbox has no limit. | Pluggable victory; old tests keep their meaning | STATE_DESIGN |
| 27 Nov 2026 | M0 | The hash parts are territory, economy, military, factions, trade, core and pending. New modules append after "pending". | Lets a mismatch be bisected; pending commands are in the hash | HEADLESS_SIMULATION |
| 27 Nov 2026 | M0 | Save version 3 arrived at M0 (planned for M3) | It needed to carry tuning and setup; stage 3 save compatibility isn't required | conventions §3 |
| 27 Nov 2026 | M0 | The tutorial island moved from seed 40 to 111, and `TutorialIsland.Qualifies` now refuses a 2 or 12 in the tutorial area | With keyed dice, seed 40's only stone/ore hex rolled a 2, so the learner stalled | PLAY_GUIDE |
| 27 Nov 2026 | M0 | Re-pinned tests: `ChainSeed` 8→39; `ChainRun_WorksOnSeveralSeeds` S16≥9 → S50≥22 (the old dice scored 25/50 on the same 50 seeds) | Stage 3 seed outcomes changed once, as the owner accepted. This is evidence, not weakening. | ACCEPTANCE G6.5 (M0 baseline in `GameRulesRunTests`) |
| 27 Nov 2026 | docs | Fertility starts at 1.0 (adulthood), not 0.9 | Owner requirement: reproduction for adults only | [POPULATION_DESIGN.md](POPULATION_DESIGN.md#time-and-life-stages), conventions §4, ACCEPTANCE M4.2 |
| 27 Nov 2026 | docs | LIV-06 is five separate drive levels, not an equal-weight composite | Owner decision Q23 | [KPI_CATALOGUE.md](KPI_CATALOGUE.md#54-work-productivity-and-living-standards-emp-liv) |
| 27 Nov 2026 | docs | The debug overlay is off by default in the sandbox (was on), and allowed in developer, lab, debugging and observation scenarios | Owner decision Q22 | [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md#48-factionknowledgeview) |
| 27 Nov 2026 | docs | M13 split into M13a (warehouse core, may follow M6) and M13b (institution KPIs and reports, after M12) | Dependency error in the old "minimum viable Stage 4" | conventions §10, [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#11-the-published-table-list), ACCEPTANCE §3.0 |

## Temporary mechanisms
Every temporary compatibility mechanism is listed here with its removal milestone ([conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list), gate G6.6). Nothing temporary may be added without a row. Remove the row in the commit that removes the mechanism.

| Mechanism | Why it exists | Removal milestone |
|---|---|---|
| Stage 3 mode (`GameSetup.Population` off) and its tests | Keeps the stage 3 game playable and its tests meaningful while NPC systems arrive (G6.1, G6.4) | M14 (owner decides whether to retire it) |
| Walkers (carriers, builders, harvesters) beside NPCs | The stage 3 economy runs until NPC labour passes the same tests | M6 |
| `MilUnit` spawning | Soldiers are spawned units until they are NPCs | M9 |
| `AiBrain` behind the `IFactionPolicy` adapter, unchanged. It still reads true state internally, **including a Q21 breach**: `AiTrader` reads other factions' stock and needs. `AiTradeJudge` answers offers inside the trade system, outside the seam. It is the only temporary entry in the policy reflection test. | Seam that preserves behaviour (M1.4) | M14 (rewritten for incentives, reading only the view) |
| Saves don't record the policy assignment; a loaded game gets its policies from its host | Not built in M1 | M3 (when NPC policies are added) |
| Non-HUD Unity code (world views, models, boot, menu, tutorial cards) still reads the sim directly | Outside the M1 gate | M14 |
| `Stage3MilitaryPolicy` as the default military policy | Combat equivalence while soldiers become NPCs | M9 (stays as a benchmark policy only) |
| Legacy `double` code on the allow-list | Kept only where M0's multi-host parity audit passes | Shrinks from M0; any item that fails parity is converted at once |
| Automatic HQ paymaster (`IPayPoint` automatic implementation) | Owner decision Q16: temporary rule; must respect resources and policy, emit events, create nothing | Stage 5 (Village age: a staffed paymaster) |
| Automatic immigration rules (HQ answer to `AskJoin`; off-island arrival rule) | Owner decision Q16: temporary rules, never replenishment | Stage 5 (Village age: a staffed official; arrival rule reviewed then) |
| Granary keeper as a plain state shift | Welfare tests at M6 before institutions exist (conflict C9) | M12 (becomes a staff post) |
| `StaticCultureProvider` (fixed per-faction customs and norms) | Customs are needed before cultural transmission exists | M11 |
| Trait clauses skipped in unlock rules | No cultural traits before M11 | M11 |
| `FoundCommunal` explicit interface (only if M12 does not implement autonomous communal founding) | Owner decision Q17 fallback | Stage 5 |
| `FullInformation` knowledge profile | Stage 4 builds the separation; limited information comes later | Later ages (Stage 6–7), per domain |
| `TutorialSteps.All` on the static-audit allow-list | Stage 3 tutorial data | M14 (tutorial rewrite) |
| Runner exit code 9, "not implemented until Mx", for learn-dump (M3), export (M13a), experiment (M14), serve, dataset and verify-model (M15). `lab` was implemented in M2. | Explicit stubs; nothing is faked | The milestone named |
| Scenario keys `population`, `startNpcsPerFaction` and `populationBelow` are refused | No NPCs yet | M3 |
| `MaxLivePartnerships` = 1 | Owner decision Q5: serial partnerships are enough for M4 | Not a removal: raised or set by custom when concurrent partnerships are added |

## Known limitations
- M0's parallel scaling at 8 workers is 0.74, below the 0.8 target; part of the gap is the all-core clock drop. Allocation is 47 KB per tick (budget 64 KB; goal 8 KB).
- The bench machine (Ryzen 7 5800X) hasn't been declared the reference machine, so performance gates only warn unless `GOD_REFERENCE_MACHINE=1` or `--strict` is set.
- Not built in M0: the SQLite batch output, `--grid`/`--resume`, the profiler hooks and the `TelemetryMask` guarded emission (they move to M13a, or to when needed).
- With keyed dice, the stage 3 seeds play differently: all 12 now end between minute 26 and 51.
- Replay-file verify names the first bad checkpoint and module. Bisecting to the exact tick only works for two fresh runs.
- The stage 3 balance targets were missed (see [UNITY_POC_PLAN.md](../UNITY_POC_PLAN.md), "Q4 result"); Stage 4 replaces most of those systems, so stage 3 is not tuned further.
- The reproduction labels in [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md#reproduction-status) rest on the research notes; L11's example is to be checked against the book at M2.

## Deviations from plan
Any change to a milestone's scope, order or acceptance criteria, with the evidence ([ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md) §1.4 rule 5a: criteria are amended only transparently, never weakened to pass).

| Date | Milestone | Deviation | Evidence | Approved by |
|---|---|---|---|---|
| 27 Nov 2026 | roadmap | "Minimum viable Stage 4" replaced by the functional releases (first playable new game = M0–M7 and M9; analytical recording = M0 JSONL, M13a, M13b) | M13 needed M12, which needs M10, which needs M7 and M9 | Owner (27 Nov) |

## Outstanding work
- M3 (in progress), then M4 and M5 (both need M3). The M13a warehouse core is in progress beside M3.
- **Owner finding from M2:** with our default drift, the book's corrected Hawk/Bully result (L2) does not appear; Retaliator stays dominant. The system is bistable, and drift decides the basin. The finding is recorded in STRATEGY_BENCHMARKS and GAME_THEORY.
- **Unity test runs:** background editor throttling makes each run take 45–100 minutes. Run them at checkpoint boundaries (as at 4A), not after every commit.
- M0 leftovers: the nightly 200-year recording-neutrality run (M0.2 N), CI wiring for the runner and player smokes, and the owner's manual check (play one stage 3 game from the menu).
- At M2: check L11's mushroom-call numbers against the book before its status label is final.
- At M12: decide whether autonomous communal founding fits M12 or whether `FoundCommunal` stays an interface until Stage 5 (record the choice above; I-COM-2).
- At M14: fill in the balance thresholds of M14.3 before the first balance run.

## Instructions for the next session
1. Read [STAGE_4_HANDOVER.md](../STAGE_4_HANDOVER.md), then this log, then [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md) (§2, §10, §13, §14).
2. Check the tree: `git status`, `git log --oneline -5` in `wt-lrs`; check which milestone branches exist and what is merged into `unity/poc`.
3. Run the baseline: `dotnet test DotNet -c Release` and compare with "Test results" above.
4. Continue the current milestone; respect milestone dependencies (conventions §10), not just file boundaries.
5. Before stopping: update "Current status", "Commits and branches", "Test results", "Performance benchmarks", "Temporary mechanisms" and "Outstanding work" here, in the same commit as the work.
6. Do not ask the owner questions that have reasonable defaults; take the default and record it under "Design decisions taken during implementation".

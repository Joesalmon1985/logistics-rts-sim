# Stage 4 development log

The persistent log of the autonomous Stage 4 implementation run. Every session reads it first and updates it before it stops. It records what is true now; the design itself lives in the documents listed in [DESIGN_CONVENTIONS.md §11](DESIGN_CONVENTIONS.md#11-document-map-one-owner-topic-each-link-dont-repeat), and the gates in [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

Rules for this file:
- Newest entries go at the top of each section. Dates are real dates; commits are short hashes.
- Say plainly what ran and what did not ([STAGE_4_HANDOVER.md §8](../STAGE_4_HANDOVER.md#8-working-rules-from-the-owner-and-earlier-stages)).
- A design change made during implementation is recorded here **and** in its owning document in the same commit.

## Current status

| Item | Value |
|---|---|
| Current milestone | **M1** (policy seams) and **M2** (maths lab), in parallel in separate worktrees. M0 is done and merged. |
| Current checkpoint | **4A Simulation laboratory** (M0–M2) |
| Next functional release | **First playable new game**: M0–M7 and M9 ([conventions §10](DESIGN_CONVENTIONS.md#10-roadmap-and-stage-4-milestones-the-official-list)) |
| Owner questions | None open: all 23 answered on 27 Nov 2026 ([conventions §13](DESIGN_CONVENTIONS.md#13-resolved-owner-decisions-27-nov-2026)) |
| Last updated | 27 Nov 2026 (docs update for the owner's answers) |

## Completed milestones

| Milestone | Checkpoint | Completed | Commit | Notes |
|---|---|---|---|---|
| M0 | 4A | 27 Nov 2026 | merge into `unity/poc` (branch `unity/m0`: `46d9fad`, `d44aa05`, `1dfb80f`, `acae4c1`) | Delivered: `SimTuning` (no mutable statics; audit test), keyed `Rng` streams, `SimCalendar`/`LifeScale`, `VictorySpec` list (VP10 + LastStanding default; Sandbox), `StateHashParts` including pending commands, domain events and `EventCatalog`, the `God.Recording` JSONL recorder (Off/Metrics/Summary/Full), `God.Runner` (run, batch, verify, replay, bench), save v3, `SimBuilder` shared with Unity's SmokeRun, and the HUD calendar. The Unity player and the runner give identical hashes. |

## Commits and branches

| Date | Branch | Commit | What |
|---|---|---|---|
| 27 Nov 2026 | `unity/poc` | merge of `unity/m0` | M0 merged (4 commits on `unity/m0`, listed above) |
| 27 Nov 2026 | `unity/poc` | `c584818` | Owner's 23 answers recorded, roadmap 4A–4F, DEV_LOG created |
| 27 Nov 2026 | `unity/poc` | `e4bc98d` | Stage 4 design: 24 design docs, conventions, acceptance tests, handover |
| 27 Nov 2026 | tag | `stage3-final` → `d7eeb95` | Stable stage 3 checkpoint (replaces Stage 3 save compatibility) |
| 27 Nov 2026 | `unity/poc` | `d7eeb95` | Last stage 3 code commit |

Branch rules for this run: commit and push tested changes to `unity/poc` and to feature branches (`unity/<name>`); **never push to `main`** ([handover §8](../STAGE_4_HANDOVER.md#8-working-rules-from-the-owner-and-earlier-stages)).

## Test results

| Date | Commit | Suite | Result | Notes |
|---|---|---|---|---|
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
| `AiBrain` behind the `IFactionPolicy` adapter, unchanged | Behaviour-preserving seam (M1.4) | M14 (rewritten for incentives) |
| `AiHost.StepHeld` (a paused game steps a held tick per command; replays log `hold` lines) | Existing pause mechanism | M1 |
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
| Scenario `policies` accepts only `faction.default` (AiBrain) or none | No `PolicyRegistry` yet | M1 |
| Runner exit code 9, "not implemented until Mx", for lab (M2), learn-dump (M3), export (M13a), experiment (M14), serve, dataset and verify-model (M15) | Explicit stubs; nothing is faked | The milestone named |
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
- M1 and M2 (in parallel), then M3.
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

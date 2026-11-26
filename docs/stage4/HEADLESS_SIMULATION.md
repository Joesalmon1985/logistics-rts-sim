# Headless simulation design (stage 4)

## 1. Purpose and scope
This document owns **`DotNet/God.Runner`**, the standalone console app that runs the God of the Island simulation without Unity. It covers:
- the command line (`run`, `batch`, `verify`, `replay`, `bench`, `lab`, `experiment`, `serve`, `dataset`, `verify-model`, plus utilities) and exit codes;
- scenario files: setup, tuning overrides, policies per level, recording, metrics and stop conditions;
- seeded, accelerated runs; save and load; parallel independent instances;
- replay files, state hashing and determinism checks;
- performance targets, profiling, level of detail and a bounded cost per tick;
- the migration from today's `SmokeRun`.

It links to, and does not repeat:
- observation and action encodings, `IntNet`, the bridge protocol and the `dataset` and `verify-model` commands: [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md);
- lab experiments and their expected results: [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md) and [GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md);
- the experiment file, arms, seeds and analysis: [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md);
- domain events, snapshots, recording levels, tables and the SQLite, Parquet and DuckDB layout: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- KPI definitions: [KPI_CATALOGUE.md](KPI_CATALOGUE.md);
- the acceptance list per milestone: [ACCEPTANCE_TESTS.md](ACCEPTANCE_TESTS.md).

## 2. Principles
1. **One sim, two hosts.** The Unity player and the runner build the sim from the same sources (`Assets/God/Sim`) and the same scenario code. Given the same scenario, seed and external commands, they produce the same hash sequence. The runner is not a second implementation.
2. **The runner is a host, not a rule.** It creates the sim, submits commands between ticks, drains events and hands them to recorders. It never reads the wall clock *into* the sim, never changes state directly, and never makes a decision the sim would make.
3. **Exact determinism** (rules from [DESIGN_CONVENTIONS.md §3](DESIGN_CONVENTIONS.md#3-layers-and-determinism-unchanged-from-stages-13)):
   - integers only in rules;
   - every random draw comes from a forked `Rng` stream;
   - iteration is in ascending id order;
   - no mutable statics;
   - commands apply at the start of the next tick, in submission order.
4. **Recording changes nothing.** Recorders, metrics, profilers and the hash checker only consume. A run at recording level Off and a run at Full have identical hashes at every checkpoint.
5. **Runs share nothing.** Each sim owns its state, its `SimTuning`, its random streams and its policies. Two runs in one process are as independent as two runs in two processes.
6. **Speed comes from doing less work, never from different rules.** Level of detail (section 4.9) is part of the rules: it is the same in Unity and in the runner, it is saved and hashed, and it never depends on a camera, a frame rate or the wall clock.
7. **Every run is reproducible from its files.** A run directory holds the resolved scenario, the seed, the git commit and the replay file, and that is enough to reproduce it bit for bit.

## 3. Data structures

### 3.1 Project layout
```text
DotNet/
  God.Sim/            netstandard2.1, C# 9: compiles Assets/God/Sim/** (unchanged; Unity's language level)
  God.Recording/      netstandard2.1: compiles Assets/God/Recording/** (recorder interfaces, JSONL, metrics, smoke verdict)
                      no UnityEngine, so the Unity player can use it too
  God.Runner/         net10.0 console app, latest C#: CLI, scheduler, SQLite writer (Microsoft.Data.Sqlite), serve mode
  God.Sim.Tests/      net10.0 NUnit: existing EditMode tests plus new determinism tests
  God.Runner.Tests/   net10.0 NUnit: CLI, scenario parsing, replay, parallel and recorder tests
```
- `God.Runner` references `God.Sim` and `God.Recording` with project references. It never compiles sim sources itself, so there is one build of the rules.
- The SQLite writer lives only in the runner. Unity can write JSONL through `God.Recording`, and the runner can convert it ([DATA_WAREHOUSE_DESIGN.md §4.6](DATA_WAREHOUSE_DESIGN.md#46-etl)).

### 3.2 Scenario file (`*.scenario.json`, version 1)
A scenario is everything except the seed that decides a run. Unknown keys are refused, which catches typos. The runner writes the **resolved** scenario (defaults filled in, overrides applied) into every run directory.

```json
{
  "scenarioVersion": 1,
  "name": "band_village_500",
  "description": "Six factions, 500 NPCs, sandbox, 20 sim years",
  "setup": {
    "factionCount": 6,
    "human": -1,
    "population": true,
    "startNpcsPerFaction": 80,
    "lifeScale": 100,
    "ticksPerDay": 1200,
    "daysPerYear": 10,
    "victory": [ { "kind": "sandbox" } ],
    "fullInformation": true
  },
  "tuning": { "Military.CritPct": 10, "Population.ThinkEvery": 10, "Lod.Enabled": true },
  "policies": {
    "faction":     { "default": "AiBrain", "0": "external" },
    "institution": { "default": "scripted.v1" },
    "military":    { "default": "scripted.v1" },
    "civilian":    { "default": "utility.v1", "sample": { "policy": "benchmark.TitForTat", "permille": 0 } }
  },
  "commands": [ { "tick": 6000, "type": "SetTaxRate", "args": { "faction": 1, "ware": "Grain", "permille": 100 } } ],
  "record": { "level": "summary", "domains": { "social": "full", "military": "off" }, "sampleEvery": 16 },
  "metrics": [ "population_alive", "births_per_day", "gini_wealth", "starvation_deaths" ],
  "checkpointEvery": 1200,
  "saveEveryDays": 0,
  "stop": { "maxYears": 20, "onGameOver": true, "populationBelow": 10, "wallSeconds": 0,
            "fail": [ { "metric": "invariant_failures", "above": 0 } ] },
  "checks": [ "invariants", "no-exceptions" ]
}
```

| Block | Meaning | Lands in |
|---|---|---|
| `setup` | `GameSetup` fields: faction count, human or watch, population on, LifeScale, TicksPerDay, DaysPerYear, the `IVictoryCondition` list (VP with a target, sandbox, later prosperity and others), `FullInformation` | M0 (faction count, human, victory, calendar); M3+ (population) |
| `tuning` | dotted paths into `SimTuning`. Unknown paths and anything touching seeds are refused. Values are integers. | M0 |
| `policies` | a `PolicyRegistry` name per level (`faction`, `institution`, `military`, `civilian`) with a default and per-faction or per-scope entries. `external` means served (section 4.8). `sample` swaps a deterministic subset of NPCs (chosen by id hash, not by `Rng`) to a benchmark policy. | M1 |
| `commands` | a scripted command schedule, applied as if a player sent it before the given tick. Used for policy shocks. | M0 |
| `record` | recording level and per-domain switches ([DATA_WAREHOUSE_DESIGN.md §4.1](DATA_WAREHOUSE_DESIGN.md#41-recording-levels)) | M0 (Off, Metrics, JSONL Full); M13 (SQLite, Summary, Experiment) |
| `metrics` | in-memory metrics to compute: names from [KPI_CATALOGUE.md](KPI_CATALOGUE.md) or runner counters | M0 |
| `checkpointEvery` | `K`, the ticks between hash checkpoints (default: one sim day) | M0 |
| `stop` | stop conditions, checked after each tick in this order: game over, `maxTicks`/`maxYears`, metric thresholds, `wallSeconds`. `fail` conditions end the run with a failure. | M0 |
| `checks` | assertions after the run: `invariants` (`SimInvariants.Check` every 500 ticks), `no-exceptions`, `smoke` (the stage 3 health checks, section 9) | M0 |

Command-line overrides use the same paths: `--set tuning.Military.CritPct=12 --set setup.lifeScale=50`.

### 3.3 Run directory
```text
out/<experiment or "adhoc">/<scenario name>/<seed>/<run id>/
  run.json            manifest: run id, scenario hash, seed, git commit, sim version, save version,
                      host (runner or unity), wall start and end, ticks, ticks per second, stop reason,
                      final hash, exit code, recorder level
  scenario.resolved.json
  replay.godr         replay file (3.4), always written unless --no-replay
  metrics.jsonl       one line per sim day with the requested metrics
  summary.json        final metrics and counters
  events.jsonl.gz     domain events (when recording is Full or a domain asks for JSONL)
  warehouse.sqlite    when recording uses SQLite (M13)
  saves/day_<n>.json  periodic saves when saveEveryDays > 0
  profile.json        per-system timings (bench, or --profile)
```
- **Run id:** 16 hex digits of an FNV-1a hash over the resolved scenario hash, the seed, the replicate number and the git commit. The same run gives the same id, so `batch --resume` skips finished runs. Wall time is never part of the id.

### 3.4 Replay file (`.godr`)
Gzipped JSON lines. Line 1 is the header. Then come commands, checkpoints and the end record, in tick order.

```text
{"format":"god-replay","version":1,"simVersion":"4.0.0","saveVersion":3,"gitCommit":"<sha>",
 "scenario":{...resolved...},"seed":7,"start":{"kind":"new"}             // or {"kind":"save","sha256":"...","file":"..."}
 ,"checkpointEvery":1200,"hashParts":["territory","economy","military","factions","trade",...]}
{"t":341,"c":{"type":"God.Sim.BuildRoad","json":"{...}"},"o":"human"}    // external command, applies at tick 341
{"t":342,"hold":"ai"}                                                    // this tick ran with brains held (Unity pause step)
{"t":1200,"h":"9f3c...","p":["a1..","07..",...]}                         // checkpoint: full hash and per-module hashes
{"end":24000,"h":"5b2e...","reason":"maxYears"}
```
- **Only external commands are logged.** An external command is one submitted while `GodSim.Step` is not running: the HUD, a scripted schedule, the server bridge. Commands that AI brains and other sim systems submit inside `Step` are re-created by the replay, so logging them would double them. `GodSim` gets an `InStep` flag so the recorder can tell the difference.
- **Only accepted submissions are logged.** A refused `Submit` never queued anything.
- The **origin** (`human`, `schedule`, `external:<agent>`) is for analysis only. Replay ignores it.
- **Checkpoints** are taken after the step that ends on a multiple of `K`. Each holds `StateHash` and the per-module parts, so a divergence names its module.
- A replay can **start from a save**. The header carries the save's SHA-256, and the loader refuses a different file.

### 3.5 Run result (in memory, written to `run.json`)
```text
RunResult { RunId; Seed; long Ticks; ulong FinalHash; List<(long tick, ulong hash)> Checkpoints;
            StopReason; ExitCode; Dictionary<string,long> Counters; double WallSeconds; double TicksPerSecond }
```
Wall-clock fields exist only here, outside the sim.

## 4. Mechanics

### 4.1 The run loop (one sim, one thread)
```text
sim = SimBuilder.Build(resolvedScenario, seed)        // shared with Unity's GodBootstrap (section 9)
recorders.Begin(runInfo, sim)
while not stopped:
    for each scheduled or served command with t == sim.State.Tick + 1, in file order: sim.Submit(c); replay.Log(c)
    sim.Step()
    events = sim.DrainEvents()                       // always drained, even at level Off
    recorders.OnTick(events)                         // read-only
    if calendar.IsDayEnd(tick): recorders.OnDayEnd(SnapshotBuilder.Day(sim.State))   // read-only builders
    if tick % 500 == 0 and checks.invariants: SimInvariants.Check(state)
    if tick % K == 0: replay.Checkpoint(sim.StateHashParts())
    if saveEveryDays and day boundary: write save
    evaluate stop conditions
recorders.End(); write run.json; return exit code
```
- The **snapshot builders** live in `Sim/Telemetry`. They are pure functions over `GodState` that allocate fresh row structs and write nothing, so calling them or not cannot change state. A test proves this (section 10).
- **Events** are drained every tick, so the event buffer never grows without limit. M0 adds `EventBuffer.DrainInto(List<SimEvent>)` so a drain does not allocate.

### 4.2 Accelerated simulation
- The runner steps as fast as the CPU allows. There is no frame loop and no sleep.
- `--speed X` paces to X times real time (10 ticks per second at 1x). It is only for watching a served game, and pacing changes when ticks happen, never what they compute.
- **Time scale:**
  - with the defaults (1200 ticks per day, 10 days per year) a sim year is 12,000 ticks;
  - 200 sim years are 2,400,000 ticks;
  - `setup.lifeScale` below 100 gives more generations in the same number of ticks; `ticksPerDay` changes the length of a day.

  Both are scenario parameters and change outcomes, so they are recorded in `dim_run`.

### 4.3 Save and load
- **Format:** `SaveFile` (version 3 from M3) holds the whole `GodState`, the pending commands and the extras.
- **What it must also hold:**
  - `SimTuning` and the `SimCalendar` settings (both inside `GodState` from M0);
  - the policy assignment (names from the `PolicyRegistry`, M1);
  - the LOD tiers (section 4.9).
- **AI brains** stay rebuilt from state, as today (`AiDirector.SaveTag`). The trade judge is re-bound by `SimBuilder`, not by the caller.
- **Runner use:**
  - `run --save-every-days N` writes periodic saves;
  - `run --save-at TICK` writes one save;
  - `run --from-save FILE` continues a run, and its replay header records the save's hash;
  - a save plus the remaining replay log reproduces the straight run exactly.
- `SavedAt` stays in the header only and is never hashed.

### 4.4 Hashing
- `GodSim.StateHash()` stays the single number. M0 adds `StateHashParts()`, which returns one `ulong` per module, in a fixed order: territory, economy, military, factions, trade, then each stage 4 module as it lands (population, cognition, learning, social, comms, culture, estate, state, institutions, information, policy).
- **The hash must cover:**
  - every module;
  - `GodState.Rng` and every forked stream that is stored;
  - `SimTuning`;
  - the calendar;
  - the LOD tiers;
  - the **pending command queue** (commands that systems submitted during the last step).

  Today the pending queue is not hashed, and M0 adds it. This changes every stage 3 hash once; that is accepted (no stage 3 hash is stored as a golden value). See [conventions §3](DESIGN_CONVENTIONS.md#3-layers-and-determinism-unchanged-from-stages-13).
- **Cost:** hashing is linear in state size. At 500 NPCs a hash should take under 2 ms, so a checkpoint every sim day costs under 0.1% of run time. `verify --every 1` hashes every tick, to find the exact divergent tick.

### 4.5 Replay and verification
- **`replay FILE`**:
  - rebuilds the sim from the header;
  - submits each logged command before its tick;
  - re-runs the `hold` ticks with brains held;
  - compares every checkpoint.

  It can also re-record: `replay FILE --record full` turns a cheap Off run into a full warehouse afterwards. This is the standard way to analyse a training or batch run in depth.
- **`verify`** checks determinism in one of these ways:

  | Form | What it checks |
  |---|---|
  | `verify FILE` | the replay file against itself (re-run, compare) |
  | `verify --scenario S --seed N --ticks T` | two fresh runs in sequence, then compare |
  | `--parallel` | the second run on another thread while the first runs |
  | `--save-mid` | a third run that saves at T/2, loads in a fresh sim and continues |
  | `--record-matrix` | runs at Off, Metrics, Summary and Full, plus each domain switched alone |
  | `--against-unity FILE` | a replay written by the Unity player (section 4.11) |
- **On divergence:**
  1. The runner reports the first checkpoint that differs and the modules whose part-hashes differ.
  2. It re-runs the last good interval with per-tick hashing to name the first bad tick.
  3. It writes both states at that tick as JSON saves (`diverge_a.json`, `diverge_b.json`) for a text diff.
  4. It exits with code 4.

### 4.6 Parallel independent instances
- **`batch`** takes a scenario and a seed list (`--seeds 1-64` or `--seeds-file`) and runs them with `--workers N`:
  - one sim per worker thread (the default is the number of physical cores);
  - `--processes` runs each sim in a child process instead, for isolation or to cap memory.
- **The scheduler** hands out runs in seed order. Results are written to each run's own directory. The batch index (`batch.json`, plus `dim_run` and `fact_run_metric` in `batch.sqlite`) is merged at the end in seed order, so finishing order never shows in any output.
- **No shared mutable state.** The M0 static audit removes or freezes everything shared:

  | Item today | Problem | M0 fix |
  |---|---|---|
  | `MilitaryTuning`: **22 mutable static fields** (12 `int`s such as `CritPct` and `MinDamage`, plus 10 `WeaponStats`/`DefenceStats` fields such as `Sword` and `BigShield`). The plan counted 12. | Two sims with different tuning would clash. A test already mutates `MinDamage` in `DuelTests`. | They move to `SimTuning.Military`, an instance stored in `GodState`. `Duel`, `GroupBattle`, `MilitaryRules`, `MilitarySystem`, `Pathing`, `TradeSystem`, `AiBrain` and the HUD read `s.Tuning.Military`. Constants that are never tuned stay `const`. |
  | `MilitaryTuning.SpawnSpreadX/Z`, `Wares.Raws`, `Gear.Attack`, `NodeGrid.DI` and other `static readonly` arrays | The reference is read-only but the contents are not | Expose them as `ReadOnlySpan`/`IReadOnlyList`, or keep them private |
  | `EconomyDefs` (`BuildingDef` with public mutable `int[]` fields) | Shared recipe data that anyone can edit | Freeze after the static constructor. Tunable recipe numbers move to `SimTuning.Economy`. |
  | `TradeTuning._values` (filled lazily) | Benign race, but a mutable static | Fill it in a static constructor |
  | `BoardTopology.Cache`, `NodeGrid.Cache` and the topology distance caches | Shared but locked, and pure functions of radius and shape | Allowed: they are listed in the audit allow-list. If profiling shows lock contention with many workers, `bench` measures it and the caches are pre-filled at start-up. |
  | `AiHost.Director` (Game layer) | A static in the view | Unity only, one sim per player. Allowed. |
- **The reflection audit test** (M0) enumerates every static field in the `God.Sim` assembly. It fails on anything that is not one of:
  - `const`;
  - `readonly` of an immutable type (primitive, string, enum, readonly struct, frozen collection);
  - on the allow-list, where each entry has a written reason.

### 4.7 Scenarios and parameters
- **Library:** scenarios live in `DotNet/God.Runner/Scenarios/`:

  | Scenario | What it is |
  |---|---|
  | `watch6` | the stage 3 smoke game |
  | `vp_duel` | 2 factions, VP 10 |
  | `band_village_100`, `band_village_500` | sandbox Band age |
  | `famine_shock` | a scripted harvest failure |
  | `bench_*` | fixed benchmark games (section 4.10) |
  | `lab_*` | presets for `lab` |
- **Parameter sweeps** (`experiment` with a `sweep` block, or `batch --grid "tuning.X=1,2,3"`) expand into one run per point times seed. Points come from a grid, one-at-a-time, or a Latin hypercube drawn from a seeded runner `Rng`, so the design itself is reproducible.

### 4.8 Observation and action interfaces per policy level
- **Four slots:** the runner exposes each `external` policy slot as an **agent port**, one per policy level: `faction`, `institution`, `military`, `civilian`.
- **What a port sees and does:** a port receives the observation the sim builds for that level and returns the intents or commands that level may send:
  - `FactionKnowledgeView`;
  - `InstitutionView`;
  - `SoldierObservation`;
  - `NpcObservation` plus the NPC's own `MindView` ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)).

  Encodings, masks and versions belong to [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md#34-encodings-by-level).
- **Two-phase step.** For served agents, `GodSim` gets `StepUntilDecision()` and `Resume(intents)`:
  1. `StepUntilDecision()` increments the tick, applies the queued commands, runs the systems up to the decision phase, builds the observations of the agents due to decide, and returns them.
  2. The caller supplies intents.
  3. `Resume(intents)` validates the intents through the normal intent path and runs the rest of the tick.

  Scripted policies run in the same phase and see the same observation. So an external client that returns exactly what the scripted policy would gives the same hash (test `Loopback_External_EqualsScripted`).
- **Served actions** are logged in the replay as external intents (`{"t":..,"i":{agent, intent}}`), so a served episode can be replayed without Python.

### 4.9 Level of detail and bounded cost per tick
Level of detail (LOD) is a **sim rule** in `Sim/Population`. It is decided from sim state only.

- **Salience.**
  - Every `LodPeriod` ticks (default 50), staggered by NPC id, each NPC's tier is recomputed:
    - **Near:** within 2 hexes of a hot place; awake with a drive above its pain threshold; in combat; in an active exchange or conversation; on duty or employed at a working site.
    - **Mid:** within 5 hexes of a hot place.
    - **Far:** everything else.
  - **Hot places:** a combat, an open construction or production site, an HQ, a crowd of 6 or more NPCs, a crime or a fire, an institution that is open.
  - **Hysteresis:** an NPC moves out by one tier at most per period and moves in at once.
  - **Dormant** is a fourth state for NPCs asleep in a hut with no stimulus. It wakes on reflex triggers (noise, attack, hunger above its threshold).
- **Think interval.** NPC *i* thinks when `(tick + i) % (ThinkEvery × mult[tier]) == 0`, where `mult` is 1, 2, 4 for Near, Mid, Far. Dormant NPCs do not think. Cheap reflexes (finishing a step, fleeing a hit) still run every tick for everyone.
- **Hard per-tick budgets** (in `SimTuning.Budget`):

  | Budget | Default | Overflow |
  |---|---|---|
  | NPC thinks per tick | `ceil(alive / ThinkEvery) × 2` | due NPCs are deferred to the next tick in ascending (due tick, id) order |
  | Path searches per tick | 32 searches, or 20,000 expanded nodes in total | path requests wait in a queue ordered by (request tick, NPC id) |
  | Perception queries | at most 16 nearest entities per NPC, from a spatial grid rebuilt incrementally | farther entities are ignored |
  | Gossip fan-out | each message reaches at most 4 listeners | |
  | Institution reviews per tick | 2 | staggered like the AI brains (`AiTuning.MaxThinksPerTick` today) |

  Deferrals are counted (`lod.deferred_thinks`, `path.queue_len`) and reported as metrics, so a budget that bites shows up in results.
- **Consequences.** LOD changes behaviour: a Far NPC reacts later. So:
  - LOD is the same everywhere: the Unity camera never affects it;
  - it is part of the hash;
  - `tuning.Lod.Enabled=false` is a scenario parameter (a different game, not a debugging switch);
  - an M3 acceptance test measures the **bias of LOD**: over 20 seeds and 5 sim years, mean population, births and food stock with LOD on stay within 5% of LOD off.
- **Bounded cost per tick.**
  - The goal is that no single tick costs more than 4 times the mean (p99) or more than 50 ms (max) at 500 NPCs.
  - Work that does not have to happen every tick is spread over slots: daily snapshots, ageing, household reviews, institution reviews.
  - Day-boundary work uses id-staggered slots across the first 100 ticks of the day, not one tick.

### 4.10 Performance targets and profiling
**Targets** (one core of a mid-range 2024 desktop, recording Off unless stated; `bench` measures them, and they are targets to meet by tuning the code, not promises):

| Case | Minimum | Goal |
|---|---|---|
| `bench_stage3_watch6` (today's game, no NPCs) | 2,000 ticks/s (200x) | 10,000 ticks/s |
| `bench_npc_500` (M3+) | 200 ticks/s (**20x**), mean tick at most 5 ms | 1,000 ticks/s (100x) |
| `bench_npc_1000` | 100 ticks/s (10x) | 500 ticks/s |
| Tick-time spread at 500 NPCs | p99 at most 4 × mean; max at most 50 ms | p99 at most 2 × mean |
| 200 sim years, 500 NPCs, Summary recording | at most 3.5 hours (20x) | at most 45 minutes |
| Recording overhead (vs Off) | Metrics 3%, Summary 15%, Full 60% | half of these |
| Memory per sim at 500 NPCs | at most 200 MB | at most 100 MB |
| Parallel scaling, 8 workers | at least 0.8 × 8 runs' throughput | at least 0.9 × 8 |
| Save or load at 500 NPCs | at most 1 s each | at most 0.3 s |
| Allocation in the steady state | at most 64 KB per tick | at most 8 KB per tick |

**Profiling plan:**
1. **M0 baseline:** `bench` runs `bench_stage3_watch6` for 40,000 ticks and records ticks per second, the tick-time distribution and the allocation rate (`GC.GetAllocatedBytesForCurrentThread`). The result is stored as `DotNet/God.Runner/Bench/baseline.json`.
2. **Per-system timers:**
   - `GodSim` gets an optional `ISimProfiler` callback around each system and around command application;
   - the callback is a host object and never touches state;
   - `--profile` writes `profile.json` with mean and p99 per system, so the costliest system is known before optimising.
3. **Deep traces** use `dotnet-trace` and `dotnet-counters` on the runner (no Unity overhead). Hot paths (perception, path search, decision scoring, hashing) get BenchmarkDotNet micro-benchmarks in `God.Runner.Tests/Bench`.
4. **Regression gate:**
   - `bench --compare baseline.json` fails (exit 7) when ticks per second fall more than 15% below the baseline;
   - this gate runs only on the reference machine;
   - elsewhere it prints a warning.
5. **Scaling checks** at M3, M6, M9 and M12: `bench_npc_500` and `bench_npc_1000`. A miss below the minimum blocks the milestone, and the fix is LOD or budget tuning before any rule change.

### 4.11 The CLI
`god-runner <command> [options]` (the executable `God.Runner`). Common options:

| Option | Meaning |
|---|---|
| `--scenario FILE` | the scenario to run |
| `--seed N` | the seed |
| `--set path=value` | override a scenario value |
| `--out DIR` | the output directory |
| `--record off\|metrics\|experiment\|summary\|full` | the recording level ([DATA_WAREHOUSE_DESIGN.md §4.1](DATA_WAREHOUSE_DESIGN.md#41-recording-levels)). `experiment` is the level `experiment` runs use by default. `--record-decisions` adds the optional decision trace (off by default; [AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)). |
| `--workers N` | parallel workers |
| `--quiet` | less console output |
| `--json` | machine-readable console output |

| Command | What it does | Milestone |
|---|---|---|
| `run` | One run: `--ticks`, `--years`, `--from-save`, `--save-at`, `--save-every-days`, `--profile`, `--check smoke`. Prints a standings line every sim day (like `SMOKE`) unless `--quiet`. | M0 |
| `batch` | Many seeds or grid points in parallel: `--seeds 1-64`, `--grid`, `--resume`, `--processes`. Writes `batch.json` and `batch.sqlite`. | M0 |
| `verify` | Determinism checks (section 4.5): `FILE`, `--parallel`, `--save-mid`, `--record-matrix`, `--against-unity`, `--every N` | M0 |
| `replay` | Re-run a replay: `--to TICK`, `--record LEVEL` (re-record), `--save-at`, `--print-events` | M0 |
| `bench` | The benchmark scenarios with timing: `--compare FILE`, `--update-baseline`, `--cases` | M0 (stage 3 case), M3 (NPC cases) |
| `lab` | Maths-only game-theory experiments from `Sim/Lab`, with no island. Subcommands `list`, `run`, `all --check`, `report`, `hash` (section 4.12) | M2 |
| `experiment` | `experiment <file> [--out DIR] [--workers N] [--warehouse FILE] [--dry-run] [--resume]`: a registered experiment plan, arms × seeds × environments, then metrics and results, recorded at the `experiment` level. The contract is in [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md). | M14 (balance sweeps); stage 6 (the full policy suite) |
| `serve` | Host sims for an external client: `--stdio`, or `--tcp 127.0.0.1:PORT` / `--pipe NAME`, `--envs K`, `--inference intnet`, `--write-actions FILE` (section 4.14) | M15 |
| `dataset` | `dataset --scenario FILE --seeds A..B --level civilian\|military\|institution\|faction --policy ID --days N --out DIR [--chunk ROWS]`: export observation and action records ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) | M15 |
| `verify-model` | `verify-model --model FILE.intnet --obs FILE [--python-out FILE] [--replay SCENARIO --seeds A..B]`: checks that integer inference matches Python step by step and writes a `.verified` stamp ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) | M15 |
| `learn-dump` (utility) | `learn-dump --npc N` prints one NPC's association store ([NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md)) | M3 |
| `export` (utility) | Converts per-run SQLite or JSONL to Parquet partitions, or JSONL to SQLite ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#47-parquet-and-duckdb)) | M13 |

**Exit codes** (a batch returns the highest-priority code among its runs; priority is the order of this table from the bottom up, so 6 beats 4 and so on):

| Code | Meaning |
|---|---|
| 0 | Success: the run ended by a normal stop condition and every check passed |
| 1 | A check or `fail` condition failed (the smoke verdict, a lab result outside tolerance, a metric threshold) |
| 2 | Usage error: bad arguments or an unknown command |
| 3 | Invalid input file: scenario, replay, save or experiment plan (unknown key, wrong version, hash mismatch) |
| 4 | **Determinism divergence**: a checkpoint differs |
| 5 | An invariant failed (`SimInvariants`) |
| 6 | An unhandled exception in the sim |
| 7 | Performance budget missed (`bench --compare`) |
| 8 | I/O or recorder error (disk full, SQLite failure). The sim result is still valid and the replay was written. |
| 130 | Interrupted (Ctrl+C). Partial outputs are flushed and marked `"stopReason":"interrupted"`. |

### 4.12 Game-theory lab experiments (`lab`)
- **What runs:** `lab` runs `Sim/Lab`, which has no board, no NPCs and no `GodSim`. The experiments (tournaments, replicator dynamics, agent-based populations, lattices) and their ids (`L1`…) are owned by [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md):
  - `lab list`;
  - `lab run L1 [--seed N] [--seeds A..B] [--agents N] [--gens G] [--dynamics replicator|agents] [--param key=value ...] [--out DIR] [--format jsonl|csv]`;
  - `lab all --check [--seeds 1..5]` (exit 1 if any check fails);
  - `lab report DIR`;
  - `lab hash L1 --seed N`.
- **Checking results:** every experiment in STRATEGY_BENCHMARKS has expected results and tolerances, and `lab all --check` compares with them.
- **Outputs:** CSV and JSON results, and from M13 the lab tables in the warehouse (`fact_lab_match`, `fact_lab_population_step`).
- **Lab vs island:** lab payoffs are the book's numbers. Island runs never use them ([GAME_THEORY_DESIGN.md](GAME_THEORY_DESIGN.md)). Island strategies are measured after the fact from the warehouse (`dim_strategy`).

### 4.13 Economic balance, stability and policy experiments
- **Balance (M14):**
  - `experiment` with a sweep over `SimTuning` paths, run on the sandbox scenarios first;
  - standard outcome metrics: survival of the population (share of seeds with population above 10 at 20 years), starvation deaths per 1000 person-years, food stock per head, boom-bust amplitude (peak to trough of the 1-year moving population), Gini drift, and, in VP mode, the win rate by start slot and the game-length distribution;
  - definitions are in [KPI_CATALOGUE.md](KPI_CATALOGUE.md).
- **Stability stop conditions:**
  - `stop.populationBelow` ends collapsed runs early;
  - collapsed runs are reported as outcomes and never dropped.
- **Policy experiments:**
  - arms, paired seeds (common random numbers), pre-registration, horizons in generations, and causal vs descriptive analysis are owned by [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md);
  - the runner guarantees what that document needs:
    - runs are independent;
    - the same seed in two arms gives the same initial state, because overrides apply after dealing only where the plan says;
    - every run is replayable;
    - metrics are computed by the same SQL as the KPI definitions;
    - no run is ever filtered out by its outcome.

### 4.14 Server mode (`serve`)
- **Hosting:** one runner process hosts `K` sims, one per thread.
- **Transports:**
  - `--stdio`: JSON lines on stdin and stdout, for debugging and simple clients;
  - `--tcp` / `--pipe`: length-prefixed binary frames, for the Python bridge.
- **Messages and tensor layout:** defined in [AI_TRAINING_DESIGN.md §5](AI_TRAINING_DESIGN.md#5-the-python-bridge) (`Reset`, `Step`, `Close`, `Save`, `Load`, `SetPolicy`, `GetSpec`).
- **This document fixes the host rules:**
  - **Binding:** loopback only; `--allow-remote` is required for any other address, and it prints a warning.
  - **Decision stepping:** stepping uses the two-phase step (section 4.8). Sims that need no decision keep running until one does or a `Step` tick limit is reached.
  - **Determinism:** every served episode writes a replay with its external intents, and `verify` accepts it. Hash checkpoints are returned in `info`, so a client can check two runs itself.
  - **Recording:** training sessions default to `--record off`. Turning recording on never changes the hashes.
  - **Isolation:** a bad message gets an error reply for that env only, and the other envs continue. A sim exception ends that env with code 6 in its `info`.

## 5. Interactions with other systems
- **`Sim/Core`:**
  - `GodSim` gains `InStep`, `StateHashParts()`, `ISimProfiler`, the two-phase step and `DrainInto`;
  - `GameSetup` gains `LifeScale`, `TicksPerDay`, `DaysPerYear`, the victory list and `FullInformation`;
  - `SimTuning` and `SimCalendar` are new.
- **`Sim/Policy`:** the `PolicyRegistry` names are what scenarios refer to ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)).
- **`Sim/Telemetry`:** domain events and snapshot builders, which recorders consume ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)).
- **`Sim/Population`:** owns LOD tiers and think slots ([POPULATION_DESIGN.md](POPULATION_DESIGN.md)).
- **`Sim/Lab`:** run by `lab` ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)).
- **`Sim/State`:** victory conditions and scripted state commands ([STATE_DESIGN.md](STATE_DESIGN.md)).
- **The Unity player:** `GodBootstrap` builds through `SimBuilder`. `SmokeRun` and replay writing are described in section 9.

## 6. Player and view
- **Headless has no view.** Everything a view would animate is in the event stream, so a headless run and a watched run are the same game.
- **Unity writes replays** (`-godrecord FILE` in the player; a menu option in the editor). A replay of a bug report can be checked in the runner with `verify --against-unity`, and opened back in Unity with `-godreplay FILE` to watch.
- **The HUD never reads runner outputs.** Reports in play come through `FactionKnowledgeView` ([INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md)).

## 7. Telemetry
- **The runner emits no domain events.** It consumes them. It adds **runner counters** to `summary.json` and `fact_run_metric`:
  - `ticks`, `ticks_per_second`, `wall_seconds`;
  - `checkpoints`, `invariant_failures`, `exceptions`;
  - `lod.deferred_thinks`, `path.queue_len_max`;
  - `events_total` per event type;
  - `bytes_written` per recorder.
- **Wall-clock values** appear only in `run.json`, `dim_run` and `meta_load`, and never in a hash or a sim decision.

## 8. Stage 4 minimum vs later stages

| Milestone | Runner work |
|---|---|
| **M0 (core runner)** | `God.Runner`, `God.Recording`, `God.Runner.Tests`; `SimBuilder` shared with Unity; scenario v1 (setup, victory list with VP and sandbox, calendar fields, tuning overrides, command schedule, stop conditions, checks); `SimTuning` with the `MilitaryTuning` statics removed; the static audit test; `run`, `batch`, `verify`, `replay`, `bench` (stage 3 case); replay files with checkpoints and per-module hashes; save and load in the runner; recording Off, Metrics and JSONL Full; exit codes; SmokeRun parity |
| M1 | `policies` block, registry names in saves and run manifests |
| **M2 (lab)** | `lab` with presets and `--check` against [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md) |
| M3 | LOD tiers and per-tick budgets; `bench_npc_500` and `bench_npc_1000`; the LOD bias test |
| M3–M12 | each new module adds its part-hash, its snapshot fields and its events; save version 3 at M3 |
| **M13 (recorder)** | SQLite recorder, Summary and Experiment levels, per-domain switches, `export` to Parquet, the warehouse in `batch.sqlite`, the size-budget test |
| M14 | `experiment` with balance sweeps and the standard stability metrics |
| **M15 (serve)** | `serve` with stdio and TCP or pipe, agent ports for all four levels, the two-phase step, served replays; `dataset` and `verify-model` ([AI_TRAINING_DESIGN.md](AI_TRAINING_DESIGN.md)) |
| Stage 6 | The full policy-experiment suite through `experiment` |
| Stage 7 | Shared-memory transport, cluster scheduling across machines |

## 9. Migration from the existing code (`SmokeRun`)
1. **Extract the verdict.**
   - Today's `SmokeRun` counts wars, captures, eliminations and trades from events, tracks the maximum number of HQs per faction, checks invariants every 500 ticks and judges the game: no exceptions, no invariant failures, and either a winner or (at least 3 factions expanded, at least 1 war, at least 1 capture).
   - This logic moves to a pure `SmokeVerdict` class in `God.Recording`, fed with events and the state.
   - `SmokeRun` keeps its Unity shell (frame loop, `Application.Quit`, the exception log hook) and calls `SmokeVerdict`.
   - The runner uses `SmokeVerdict` for `run --check smoke`.
2. **Share construction.**
   - `GodBootstrap` and `AiHost.StartForGame` today attach the AI with `AiTuning.Default` and a new `AiTrader`.
   - `SimBuilder.Build(scenario, seed)` does exactly that, for all non-human factions, from M0.
   - Both hosts call it, so the Unity game and the runner cannot drift apart in setup.
3. **Scenario parity.**
   - `Scenarios/watch6.scenario.json` reproduces `-godsmoke 40000 -godseed S`: 6 factions, watch mode, VP 10, 40,000 ticks, check `smoke`.
   - The standings line format stays (`tick … min … | Amber 3 VP 2 HQ …`), so old logs stay comparable.
4. **Hash parity.**
   - The player's smoke prints `SMOKE HASH <tick> <hex>` at every checkpoint and writes a replay with `-godrecord`.
   - A CI step runs the player smoke and `god-runner verify --against-unity` on its replay. They must match at every checkpoint.
5. **Held steps.**
   - `AiHost.StepHeld` (a paused game applying one command) steps the sim with brains held.
   - Until M1 replaces it with a cleaner mechanism, these ticks are logged as `hold` lines, so Unity replays stay exact.
6. **CI split.**
   - The runner smoke runs on every push. It is fast and needs no Unity licence.
   - The player smoke stays nightly, to catch IL2CPP, Mono or player-only differences.
7. **The stage 3 tests stay green** throughout, and the existing save tests (`SaveLoadContinue_GivesTheSameHash_AsAnUninterruptedRun` and the others) also run under the runner.

## 10. Tests and acceptance criteria
Tests live in `God.Runner.Tests` unless noted. "Seeds" means a fixed list in the test.

| Test | What must hold |
|---|---|
| `SameSeedSameCommands_SameHashes` | Two runs of `watch6`, seeds 1–8, 40,000 ticks: every checkpoint hash is equal |
| `DifferentSeeds_DifferentHashes` | Seeds 1–8 give 8 distinct final hashes |
| `RecordingOnOff_SameHashes` | Seeds 1–4: Off, Metrics, Experiment, Summary, Full, and each domain switched alone give identical checkpoint sequences (CI: 2 sim years; nightly: 200 sim years for seed 1) |
| `SnapshotBuilders_AreReadOnly` | Calling every snapshot builder every tick vs never: identical hashes over 5,000 ticks |
| `Parallel_EqualsSerial` | 16 seeds with `--workers 8`, and with `--processes`, give the same per-seed hashes and byte-identical `summary.json` (wall fields excluded) as a serial run |
| `TwoTunings_InParallel_DoNotLeak` | Two sims on two threads, one with `Military.CritPct=0`, one default, each equal to its own serial run |
| `StaticAudit_NoMutableStatics` | The reflection audit passes; every allow-listed field has a reason |
| `SaveLoadMid_EqualsStraight` | For seeds 1–4, save at T/3 and 2T/3, load in a fresh sim, continue: the checkpoint hashes equal the straight run. A second save of a loaded game is byte-identical (as the existing test). |
| `Replay_RoundTrip` | Record a run with a command schedule and 200 random human commands; `replay` matches every checkpoint; `verify` exits 0 |
| `Replay_DetectsTampering` | Changing one command's tick in the file makes `verify` exit 4 and name the first bad checkpoint and module |
| `Divergence_Bisect` | A deliberately injected nondeterminism (test-only hook) is found at the exact tick |
| `Unity_Agrees_With_Runner` | (Nightly, player build) the player smoke replay for seeds 1–2 verifies in the runner at every checkpoint |
| `Loopback_External_EqualsScripted` | An in-process external client that calls the scripted policy through encode and decode gives the same hashes as the scripted policy (M15) |
| `Smoke_Parity` | `run --check smoke` on `watch6` gives the same verdict, counters and final standings line as the old `SmokeRun` logic for seeds 1–4 |
| `ExitCodes` | Each code in the table is produced by a constructed case (bad flag → 2, bad scenario key → 3, tampered replay → 4, broken invariant fixture → 5, throwing test system → 6, impossible bench baseline → 7, read-only output dir → 8) |
| `Scenario_StrictParsing` | Unknown keys, unknown tuning paths and seed overrides are refused with code 3 |
| `Bench_Stage3_Baseline` | (M0, reference machine) `bench_stage3_watch6` at 2,000 ticks/s or more |
| `Bench_Npc500` | (M3+, reference machine) 200 ticks/s or more, p99 at most 4 × mean, at most 200 MB |
| `Lod_Bias_Within5pct` | (M3, nightly) 20 seeds × 5 sim years, LOD on vs off: mean population, births and food per head within 5% |
| `Lab_Presets_WithinTolerance` | (M2) every STRATEGY_BENCHMARKS experiment passes `lab all --check` |
| `LongRun_200Years` | (Nightly from M4) `band_village_500`, seed 1, 200 sim years at Summary: finishes with exit 0, at most 3.5 hours, warehouse within budget ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#10-tests-and-acceptance-criteria)) |

## 11. Open questions
None for the owner. Settled with defaults:
- **Floating point in the sim** (`BoardTopology`, `Lattice`, `FactionRules`, an `EconomySystem` tie-break, `TerritoryState.HqCircle`): they use only `+ − × ÷ √` and rounding, so they are expected to match across CoreCLR, Mono and IL2CPP on x64. Default: keep them and rely on the cross-host hash test (`verify --against-unity`); convert a function to integers only if that test fails. New code is integer-only (conventions section 3).
- **The held-step mechanism** (`AiHost.StepHeld`): default, M1 replaces it by queuing commands sent while paused until unpause, so replays need no `hold` lines.
- **The `God.Recording` assembly in Unity:** default, a third asmdef next to `Assets/God/Sim` (no UnityEngine), so the optional Unity recorder shares the runner's JSONL writer.

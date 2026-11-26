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
  warehouse.sqlite    when recording uses SQLite (M13; `--sqlite`, as built in M13a)
  saves/day_<n>.json  periodic saves when saveEveryDays > 0
  profile.json        per-system timings (bench, or --profile)
```
- **As built (M13a, 27 Nov 2026):** `run` and `batch` take `--sqlite [--no-jsonl] [--no-index] [--fk]`, and each run then writes `warehouse.sqlite` in its run directory. `--no-jsonl` skips `events.jsonl.gz`, `--no-index` skips the end-of-run indexes, and `--fk` turns on foreign-key enforcement while writing. SQLite is opt-in: without `--sqlite` nothing changes, and the scenario schema is unchanged (no new keys). A batch also writes `batch.sqlite` beside `batch.json`, holding `dim_run` and `fact_run_metric` for all runs, merged in seed order.
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

### 4.4a Owner determinism requirements (27 Nov)
Source: [conventions §14](DESIGN_CONVENTIONS.md#14-further-owner-requirements-27-nov-2026). Same version, configuration, seed, initial state and commands give identical results. Where each requirement is met: versioned saves (4.3); serialisable random streams (every stored `Rng` fork is saved and hashed, 4.4); stable processing order and deterministic tie-breaking (conventions §3, 4.1, 4.9); pending commands in the hash (4.4); persistent learning state (saved with the NPC, [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md) L9); save/load continuation parity and replay verification (4.3, 4.5); independent parallel instances and no mutable global tuning (4.6, M0 static audit); identical results at any telemetry level (G2.2); headless and Unity parity (`Unity_Agrees_With_Runner`). **Stage 3 save compatibility is not needed**: Stage 4 saves start at version 3 and need not read stage 3 saves; the git tag `stage3-final` (at `d7eeb95`) keeps the stage 3 game.

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

**Benchmark report (owner, 27 Nov: benchmark early, from M0).** Every `bench` run writes, per case, these columns (the same columns as the benchmark table in [DEV_LOG.md](DEV_LOG.md#performance-benchmarks)): ticks/s; sim years per real second; NPC decisions per second; memory per NPC (bytes, measured and against the cap in [POPULATION_DESIGN.md](POPULATION_DESIGN.md#memory-budget-per-npc-combined)); pathfinding cost (searches and expanded nodes per tick, µs per search); learning cost (µs per think); communication cost (messages and decodes per tick, µs per message); telemetry and database overhead (% against recording Off, per level); save size (bytes) and load time; parallel throughput (runs per hour at 1, 4 and 8 workers). Columns that do not exist yet at a milestone are blank, not zero.

**Rules for cost (owner, 27 Nov):**
- Per-NPC memory is bounded, with deterministic eviction (every store has a cap and an eviction order by score, ties by id).
- No all-to-all comparisons: perception, partner choice, gossip, job matching and migration use spatial indexing, staggered decisions and bounded candidate sets (section 4.9 budgets).
- Level of detail is decided by sim state and scenario, never by camera or rendering (section 4.9).
- **An optimisation that changes behaviour is documented**: in the owning document and in [DEV_LOG.md](DEV_LOG.md#design-decisions-taken-during-implementation), with the bias measured (as the LOD bias test does).
- **Capability comes first:** learning, social or economic capability is never cut for premature optimisation. A budget miss is fixed by LOD, data layout or budget tuning first; a capability change needs the owner.

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

**As built (M13a, 27 Nov 2026):**
- `run` and `batch` accept `--sqlite [--no-jsonl] [--no-index] [--fk]` (section 3.3). `batch.sqlite` holds `dim_run` and `fact_run_metric`, merged in seed order.
- `export --from FILE|DIR --to csv|parquet --out DIR [--tables]` converts a per-run `warehouse.sqlite` (or a directory of runs). Parquet is written as a hive-partitioned lake with a `_global` folder for global dimensions ([DATA_WAREHOUSE_DESIGN.md §4.7](DATA_WAREHOUSE_DESIGN.md#47-parquet-and-duckdb)).
- `export --to sqlite` (loading JSONL into SQLite) is **not built** and returns exit code 9 (not implemented). The route to a warehouse for a run recorded without SQLite is `replay --record full --sqlite`.
- `bench --cases recording` runs the recording-overhead cases (modes off, metrics, summary and full, in memory, JSONL and SQLite) and checks that all modes give one final hash.
- SQLite lives in `God.Runner` (net10) behind `IRunRecorder`; `God.Sim` and `God.Game` do not reference it, and Unity stays on JSONL.

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
| 9 | Not implemented (as built in M13a: `export --to sqlite`) |
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
| **M13a (recorder)** | SQLite recorder, Summary and Experiment levels, per-domain switches, `export` to Parquet, the warehouse in `batch.sqlite`, the size-budget test |
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
- **Floating point in the sim** (`BoardTopology`, `Lattice`, `FactionRules`, an `EconomySystem` tie-break, `TerritoryState.HqCircle`). **Owner decision (27 Nov):** M0 **audits** every use and **verifies parity on more than one host** (the runner on CoreCLR, the Unity player on Mono and IL2CPP where available, and at least a second machine when one is available). Anything that **fails parity, or whose parity cannot be guaranteed** (for example a transcendental function, or a result whose rounding depends on evaluation order), is converted to integers or fixed point. Code using only `+ − × ÷ √` with fixed evaluation order that passes parity is kept: harmless code is not rewritten without reason. New code is integer or fixed point only (conventions section 3). The audit's list, each item's verdict and the hosts checked go into [DEV_LOG.md](DEV_LOG.md#design-decisions-taken-during-implementation).
- **The held-step mechanism** (`AiHost.StepHeld`): default, M1 replaces it by queuing commands sent while paused until unpause, so replays need no `hold` lines.
- **The `God.Recording` assembly in Unity:** default, a third asmdef next to `Assets/God/Sim` (no UnityEngine), so the optional Unity recorder shares the runner's JSONL writer.


---

## 12. Owner D5: Civilisation and Divinity (26 Nov 2026)
**Status: plan only; nothing here is built.** Source: [OWNER_BRIEF_2026-11-26.md](../civdiv/OWNER_BRIEF_2026-11-26.md) §46 (determinism), §48 (performance), §33 (the warehouse), decisions D5.5–D5.12, D5.30–D5.32 (D5.32: no neural network training during this run). Sections 1–11 stand. This section (1) restates §46 as checks, (2) says how divine, possession and avatar inputs enter the replayable command stream, (3) pins neutrality for `Divinity.Enabled` and `Orgs.Enabled` in the M4/M5 way, and (4) plans the §48 benchmarks. Event codes, system orders and the save version are fixed by `docs/civdiv/SHARED_ARCHITECTURAL_CONTRACTS.md`, which a contracts agent is writing; names here are provisional.

### 12.1 Determinism requirements (brief §46) as checks
Each row is a check that can fail. "Exists" means a test or command is in the repository today; "extend" means the existing check is widened to the new content; "new" means it must be written. Owners: Q3 integrates, each package supplies its fixture.

| Requirement | Check | State |
|---|---|---|
| Pure C# authoritative sim | `God.Sim` assembly has `noEngineReferences` and no reference to `God.Game`, `God.Runner`, SQLite or Unity; `SimAndRecordingAssemblies_DoNotReferenceSqlite` | exists; extend to new namespaces (`Sim/Divinity`, `Sim/Organisations`) |
| No dependency on rendering | the same scenario and the same replay give the same hash in the runner and in the player build at any resolution, any camera, any frame rate (`Unity_Agrees_With_Runner`); a Unity EditMode test runs a replay with the camera at three places and with rendering off | exists (smoke replay); extend to a divine replay; camera test new |
| No wall-clock behaviour | static scan of `God.Sim` for `DateTime`, `Stopwatch`, `Environment.TickCount`, `Thread`, `Task`, `Random` (system), `Guid`; the M0 static audit | exists; extend to new assemblies |
| Stable entity processing order | every new system has a `SystemOrder` slot; iteration is by ascending id; no `Dictionary` or `HashSet` enumeration in saved or hashed state; reflection test over new state types (public fields, sorted lists) | exists for M3–M5; extend (F1) |
| Explicit random streams | new draws use stateless keyed forks `s.Rng.Fork(label, a, b)`, never an NPC's saved streams (SHARED_CONTRACTS 9.1); labels in 12.3 are unique per call site; test enumerates labels and fails on a duplicate | exists (rule); label test new |
| Deterministic command processing | all divine and player actions are `Command`s submitted through `GodSim.Submit`, applied in submission order at the head of the tick, rejected with a reason code, and logged; pending commands are in the hash | exists (`PendingCommands_AreInTheHash`); extend to divine commands |
| Save/load continuation parity | save at T/3 and 2T/3 of the integrated divine scenario with an open compulsion, a possession, a manifested avatar, a Mythic balance, a deferred resurrection and an archived dead person; the continued run hashes equal the straight run; a second save is byte-identical | exists (`SaveLoadContinue_GivesTheSameHash...`); scenario new |
| Replay parity | `verify FILE` and `replay FILE` of a recorded divine scenario match every checkpoint at every module part; `Replay_DetectsTampering` extended to a divine command | exists; extend |
| Headless and Unity parity | a replay written by the player after a human session (movement, possession, miracle, commandment, resurrection) verifies in the runner (`verify --against-unity`) at every checkpoint | exists for the smoke replay; divine session new (nightly) |
| Independent parallel simulations | `Parallel_EqualsSerial` and `TwoTunings_InParallel_DoNotLeak` with divine scenarios; no static mutable state in new code (static audit) | exists; extend |
| Logging independence | Off, Metrics, Experiment, Summary, Full, each domain alone, `sampleEvery` 1, 16, 64 give identical hashes, now including the divine and organisation domains; building a divine view, an inspector or a KPI engine result changes no hash | exists (`SqliteRecording_NeverChangesTheHashes...`, `RecordingOnOff_SameHashes`); extend (W-D5-3) |
| Versioned save formats | `CurrentVersion` is bumped once by the lead at the merge that adds divine state (as M4/M5 did); older versions are refused with a clear message; a fixture save of each version loads or is refused as documented | exists (rule); the bump is a lead task |
| Resource conservation | invariant C1 of SHARED_CONTRACTS 2.4 with miracle creation and destruction counted through the resource ledger (`Produced` or `Consumed` with a divine reason): per ware `Σ Produced − Σ Consumed = Σ holdings` every day boundary and after save and load | exists; extend (D3, C1) |
| Population conservation | alive at end = alive at start + births + immigrants + resurrections − deaths − emigrants, every day; an archived dead person is not alive; a resurrection restores the same `npc_id` | partly exists (`PopulationInvariants`); extend (D7) |
| Ownership integrity | every asset has owners summing to 1000‰ or none; no asset owned by a dead person after 3 days (C4) unless a resurrection reopens that person; a resurrection never takes back what an estate settled | exists for lots; extend to assets and shares (C1) |
| Bounded learning and social memory | every per-NPC store keeps its cap and eviction order (existing store tests); the new per-NPC state added by D5 (known commandments, belief about gods, perceived command memory, possession memory) has a cap and a deterministic eviction rule, and `bench` reports bytes per NPC | exists for M3 and M5 stores; new for D5 state |
| Divine inputs replayable | every divine, possession and avatar input is a command in the stream (12.2); a scenario with them replays exactly (acceptance scenario 18) | new |
| Camera independence | no sim rule reads a camera, selection, view distance or UI state; the existence and behaviour of an individual is the same whether or not the player is looking (12.5) | new test |

### 12.2 Divine, possession and avatar inputs in the command stream
**One path.** Every player or AI-god input becomes a `Command` (the existing type, with `CommandApplied` and `CommandRejected` events) and goes through the validation the sim already performs for people and bots ("bots and people use the same commands", CLAUDE.md). There is no divine back door and no second implementation of ordinary actions (brief §9): a compelled, possessed or avatar action is an ordinary action primitive started on behalf of the body, with a `control_source` tag that the warehouse records.

**Command set (names provisional).**
| Group | Commands | Notes |
|---|---|---|
| Instruction | `IssueSuggestion`, `IssueCommand`, `IssueCompulsion`, `CancelCompulsion` | action code from the brief §9 vocabulary, target (person, group, household, institution, location), scope, expiry tick; Mythic charged at acceptance |
| Possession | `BeginPossession`, `EndPossession`, `PossessedMoveIntent`, `PossessedAction` | one person at a time in the first release |
| Avatar | `ManifestAvatar`, `DismissAvatar`, `AvatarMoveIntent`, `AvatarAction` | same action set and movement as an NPC; Mythic upkeep per tick |
| Miracle | `CastMiracle` (kind, target, area, magnitude), `Resurrect` (person, variant) | validated against Mythic, location, inventory conservation, death handling |
| Doctrine | `RevealCommandment`, `ReviseCommandment`, `RevokeCommandment` | structured template with typed fields, never free text interpreted in the sim |
| Setup | `SetDivineProfile`, `SetMythicMode` (sandbox only), `SwitchGod` (sandbox scenarios) | changing configuration mid-run is a logged command |

**What is logged (extends 3.4).** Only external commands are logged, as today: a command submitted while `GodSim.Step` is not running. A human's commands are external. An AI god's policy runs inside `Step` through the policy phase (like `AiBrain`), so its commands are recreated by the replay and not logged, exactly as the faction AI's are; the policy name, registry version and model hash are in the replay header (`dim_agent_policy`). The `o` origin tag gains `human`, `aigod:<policy>` and `schedule`; replay ignores it. A scripted god in a test is a `schedule` of external commands.

**Continuous movement becomes per-tick intents.** The camera, the stick or the keys are frame-based and variable; the sim is tick-based and integer. The conversion is defined here so that a human session replays exactly:
1. **The Game layer, not the sim, converts input to an intent.** It reads the stick or keys, rotates them by the third-person camera yaw (the camera is read here and nowhere else), and quantises the result to an integer **heading** (0–63 in 1/64 turns, or the stop value 64) and an integer **gait** (0 stop, 1 walk, 2 run). The intent carries world heading, never a camera angle.
2. **Sampled at tick boundaries.** The host samples the quantised intent once per sim tick boundary. It submits a `PossessedMoveIntent` or `AvatarMoveIntent` only when the quantised value differs from the last submitted one (and at most once per body per tick), so the log holds changes, not frames. The command applies at the next tick (`t` of the replay line is that tick).
3. **The sim holds the intent until it is replaced.** The sim turns heading and gait into the next fine movement node on the body's own movement system, using the existing path and affordance rules (fine movement nodes are developer-visible only). The same intent in the same state gives the same step. Walls, other bodies and terrain stop it as for an NPC. An intent holds until replaced, until `EndPossession` or `DismissAvatar`, until the body dies, or until Mythic runs out (which ends possession or the avatar deterministically on the day or tick of exhaustion).
4. **Click-to-move and interaction are discrete.** `MoveTo(node)`, pick up, drop, give, use, talk and gesture carry ids and are validated against reach (`Reach.Of(reason)`) as for an NPC.
5. **Time scaling and pause.** The speed setting (pause, 1x, 2x, 5x, 10x, accelerated) changes how many ticks run per real second, never what a tick does. Commands issued while paused are stamped with the next tick that will run. Replays carry only tick numbers; no real-time stamp is read.
6. **Possession of an NPC** replaces the NPC's decision step for that NPC with the incoming intents; its body, needs, health, ageing, injuries, memory and learning go on as normal. The learning update runs as for an own decision with the intent as the chosen action and the exploration draw skipped by rule; the possessed NPC's saved streams are not advanced by anything the sim does on the god's behalf (draws use keyed forks, 12.3). Actions are tagged `control_source = possessed`.
7. **Compulsion** is an instruction with an expiry, checked each tick: the compelled NPC's candidate set is replaced by the compelled action while it is physically possible. If a physical constraint blocks it (no path, no object, no material) the compulsion records "blocked" and runs to expiry or cancellation; it never teleports or creates goods. It ends on completion, timeout, cancellation, death or exhaustion of Mythic (when it is charged continuously).
8. **Mythic** is integer state updated at fixed times: regeneration and upkeep at day start and per tick for continuous upkeep, costs at acceptance, all in the sim's own systems, in the `divine` hash part. Unlimited sandbox sets the charge to 0 and records the nominal cost. Disabled mode rejects every divine command with a reason code and leaves state untouched.

**Systems and order (provisional, to be fixed by the contracts document).** A `DivinitySystem` slot before `LifeCycleSystem` (240) in the day-start phase: Mythic regeneration and upkeep, compulsion and instruction expiry, possession and avatar exhaustion, resurrection completion; and, per tick, intent resolution for possessed and avatar bodies before `PopulationSystem` (250). `OrganisationSystem` after `EstateSystem` (255). Existing orders do not change.

### 12.3 Random streams for the new code (labels provisional; unique per call site)
| Owner | Label (a, b) | Purpose |
|---|---|---|
| D1/D2 | `divine-perceive` (npcId, tick), `divine-respond` (npcId, commandId) | whether and how clearly a suggestion or command is perceived; the NPC's own acceptance draw when the decision model needs one |
| D3 | `miracle-effect` (miracleId, step) | scatter and magnitude of an area miracle, if any |
| D6 | `commandment-learn` (npcId, commandmentId), `commandment-interpret` (npcId, commandmentId), `belief-drift` (npcId, day), `prayer` (npcId, day) | learning, interpretation, drift and prayer draws |
| D7 | `resurrect-state` (npcId, tick) | the variance of restored health, if any |
| C6 | `org-emerge` (hex or place id, day), `service-match` (requestId, tick) | emergent founding and provider matching |

No D5 code draws from an NPC's saved streams (`RngPolicy`, `RngWork`, `RngLearn`).

### 12.4 Neutrality pins for `Divinity.Enabled` and `Orgs.Enabled` (the M4/M5 pattern)
M4 and M5 are neutral switches: `Households.Enabled = 0` reproduces the post-F0 behaviour exactly and `Social.Enabled = 0` likewise, each pinned by hashes for `band_6x20`, seeds 1–3, 10 days (12,000 ticks) (`M4LifeCycleTests`, `M5NeutralityTests`; SHARED_CONTRACTS 7.3). The new switches follow the same pattern. State of the new module lives in its own block (`GodState.Divinity`, `GodState.Organisations`), is null when the switch is 0, and its hash part is present only when non-null, so every older hash is unchanged.

| Switch (tuning path) | Default | Effect when 0 |
|---|---|---|
| `Divinity.Enabled` | scenario-dependent: 0 in every scenario that exists today, 1 in new divine scenarios | no gods, no Mythic, no divine commands (every one is rejected with a reason), no possession, avatar, miracle, commandment, belief in gods or resurrection; D5 hooks return defaults |
| `Divinity.Mode` | `standard` | `standard` (configured maximum, regeneration, costs), `unlimited` (nominal costs recorded, nothing charged), `disabled` (state exists, every divine command rejected) |
| `Orgs.Enabled` | scenario-dependent: 0 today | no organisation state, no emergent founding, no services, no institutional expenditure; D5 hooks return defaults; the older M12 institution code is unaffected |
| `Orgs.Emergence` | 1 | with `Orgs.Enabled = 1` and this 0, organisations can be seeded by the scenario but none arises on its own |

**Pins** (each is a test; the pinned hash values are copied from the base commit before the package branch starts, as M4 and M5 did):
| Pin | Statement |
|---|---|
| P1 `Divinity_Off_Reproduces_Base` | with `Divinity.Enabled = 0`: for `band_6x20`, `m4_families`, `ibs_families`, `watch6` and, after the demography branch merges, the `demog_*` scenarios, seeds 1–3, every checkpoint hash and every part hash equals the base commit's |
| P2 `Divinity_Idle_Is_Neutral` | with `Divinity.Enabled = 1`, a god per faction, regeneration running and **no command ever issued**: every part hash except the new `divine` part equals P1. The god is not in the sim's causal chain until a command is accepted |
| P3 `Divinity_Disabled_Mode_Neutral` | `Divinity.Mode = disabled` with a scripted god issuing commands every day: all rejected, `CommandRejected` rows with reasons, every part hash except `divine` equals P1 |
| P4 `Orgs_Off_Reproduces_Base` | `Orgs.Enabled = 0`: as P1 |
| P5 `Orgs_NoEmergence_Neutral` | `Orgs.Enabled = 1`, `Orgs.Emergence = 0`, no seeded organisations: every part hash except `organisations` equals P4 |
| P6 `Switch_Matrix` | the suites of M4, M5, D-packages and C-packages each run with all other switches at 0 and equal the hashes of their own branch tips; then pairwise; then everything on. The matrix runs after every merge by the integration lead (Q3) |
| P7 `Recording_Neutral_On_New_Domains` | recording levels, each new domain alone at Full, and sampling rates give identical hashes in a divine and organisation scenario (W-D5-3) |

Pins are not an excuse to skip later review: when a pinned scenario's hash legitimately changes because a package changed behaviour, the change is recorded in `DEV_LOG.md` with the reason, as the M0 hash change was, and the pin is updated in that same commit.

### 12.5 Camera and level-of-detail independence
- **Rule (brief §48).** Rendering level of detail may vary with the camera. The existence and behaviour of individuals may not. The sim never receives the camera position, view frustum, selected object, UI mode, or the speed of rendering.
- **What may affect sim LOD (4.9) is sim state only:** hot places, combat, work and institution sites, and **bodies the god is controlling** (a possessed NPC, a manifested avatar, a compelled NPC): these are Near tier by rule because they are sim state, the same in the headless runner and the player.
- **Checks.** (a) The M0 reflection test extends to forbid `God.Sim` from referencing `Game` types. (b) `Camera_Independence`: the same replay played in the player with the camera fixed at the whole-island view, at one NPC, and with rendering off gives identical checkpoint hashes. (c) `Lod_Is_SimState_Only`: the LOD bias test of 4.9 reruns with a scripted camera in the player and gives the same tiers and hash.

### 12.6 Benchmark plan (brief §48)
`bench` stays the one tool and its report columns stay (4.10). The brief's measures map to existing columns, and new cases and columns are listed. All numbers are measured on the reference machine and recorded in `DotNet/God.Runner/Bench/baseline.json` and `DEV_LOG.md`; **no new target is promised before a measurement**, and an optimisation that changes behaviour is documented with its bias, as 4.10 requires.

| Brief §48 measure | Existing `bench` output (as built) | To add |
|---|---|---|
| Simulation ticks per second | `ticksPerSecond`, `msPerTickMean`, `msPerTickP50`, `msPerTickP99`, `msPerTickMax`, `realTimeMultiple` | none; regression gate `--compare` (15%) stays |
| NPC decisions per second | `npcDecisionsPerSecond`, `npcThinks`, `npcUpdateMicrosPerThink` | split by control source (own, compelled, possessed) in `bench_divine` |
| Simulated years per real minute | `simYearsPerRealSecond` (multiply by 60) | add `simYearsPerRealMinute` to the JSON and DEV_LOG table |
| Memory per individual | `memoryPerNpcBytes`, `learningStoreBytesPerNpc`, `memoryEndBytes`, `memoryEndUncollectedBytes` | `memoryPerArchivedDeadBytes` (resurrection eligibility), `bytesPerOrg`, D5 per-NPC state bytes |
| Population scalability | cases `bench_npc_120`, `bench_npc_300`, `bench_npc_500`, `bench_npc_1000` | `bench_npc_100`, `bench_npc_2000` and `bench_npc_5000` (feasible only if 2000 passes; stop where the minimum targets of 4.10 are missed and record that) |
| Learning overhead | `learningMicrosPerThink`, `learningShareOfThink`, `usPerThinkOff`, `usPerThinkOn`, `consolidationMsPerSimDay` | per-population rows |
| Social interaction overhead | none as a pair | `Social.Enabled=0` against 1 on the same case: `socialMicrosPerThink`, messages and decodes per tick (4.10 asks for these columns) |
| Institutional processing overhead | none | `bench_org_500`: `Orgs.Enabled=0` against 1: microseconds per tick and per delivered service, service requests per second, `orgReviewsPerTick` against its budget |
| Divine overhead | none | `bench_divine_500`: a scripted god issuing a fixed schedule (commands, one compulsion, one possession, one avatar, area miracles): commands per second, cost of a tick with a possessed or avatar body, Mythic and divine hash cost |
| Pathfinding | `pathSearchesPerTick`, `pathNodesExpandedPerSearch` | `pathMicrosPerSearch`; deferred and queued searches (`path.queue_len`) |
| Save size | `saveBytes`, `saveMs`, `loadMs`, `loadSameHash` | bytes per NPC, per archived dead person, per org; a 2000-person save |
| Database recording overhead | `bench --cases recording` (stage 3 scenario: off, in-memory metrics, SQLite metrics, JSONL summary, SQLite summary, JSONL full, SQLite full) | run the same on `bench_npc_500` and `bench_org_500` and `bench_divine_500`; SQLite bytes per sim day; rows per second; writer queue stalls |
| Parallel headless throughput | `parallel` block (`workers`, `aggregateTicksPerSecond`, `perSimTicksPerSecond`, `scaling`), default workers 1 and 4 for NPC cases | workers 1, 4, 8, 16 for 500 and 1000; `batch` throughput in runs per hour with SQLite recording on |

Populations: 100, 500, 1000, 2000 are the required set; 5000 is attempted. For each, run Off, Summary and Full, and with the divine and organisation switches 0 and 1. Report the cost of **being able to** (switch on, idle) separately from the cost of **using** (scripted activity), because P2 and P5 make the idle cost testable. The order of work is Q2: baselines first (existing cases at the merge commit), then new cases as each package merges.

### 12.7 `demog report` and the runner commands in the stream
`unity/demog` adds `demog report --scenario S --seeds 1-20 --years 15 [--set ..] [--arm "name: k=v ..."] [--plan FILE] [--out DIR]` and `unity/ibs` fixes the warehouse mappers for NPC 0; both merge before D5 work. `demog report` is the headless demographic viability experiment (F2); it and `lab`, `learn-dump`, `export` and `bench` read the sim and the recorder, never the other way round. The new commands planned for Q1 and Q2 are `sql` (read-only, [INFORMATION_AND_MEDIA_DESIGN.md §12](INFORMATION_AND_MEDIA_DESIGN.md#12-owner-d5-civilisation-and-divinity-26-nov-2026)), `bench` cases (12.6), and the divine fixture scenarios for Q3. `experiment`, `serve`, `dataset` and `verify-model` stay "not implemented" (exit 9): no training and no served agents in this run (D5.32).

### 12.8 Tier and package mapping
| Item | Package (brief §39) | Tier |
|---|---|---|
| Command types, validation, Mythic, `Divinity.Enabled`, P1–P3, divine replay | F1, D1, D2, Q3 | 1 |
| Resource ledger and conservation C1 with miracles | D3, C1, Q3 | 1 |
| Save version bump, `divine` hash part, save/load parity with divine state | F1, Q3 | 1 |
| Exact replay of a human session (movement intents, possession, avatar) | D4, D5, U1, Q3 | 2 |
| Resurrection and archive, population identity | D7, Q3 | 2 |
| `Orgs.Enabled`, P4–P5, switch matrix P6 | C6, Q3 | 2 |
| Benchmarks for populations 100–2000, learning, social, divine, recording, parallel | Q2 | 1 (baseline, 2000-person run), 2 (org and divine cases) |
| `bench_npc_5000`, nightly long runs with divine and organisations | Q2 | 3 |
| Camera independence test in the player | U1, Q3, Q4 | 2 |
| Checks of 12.1 for justice and military additions | C9, C4, Q3 | 3 |

### 12.9 Acceptance hooks
| # | Test | Pass criterion |
|---|---|---|
| H-D5-1 | `Scenario18_Replay_Exact` | a complete scenario with ordinary NPC actions, a suggestion, a command, a compulsion, a possession with movement intents, an avatar, a miracle, a commandment reveal and a resurrection: `verify FILE` exits 0; `replay FILE --record full` gives a warehouse equal to the live Full recording ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md#128-acceptance-hooks) W-D5-1, W-D5-2) |
| H-D5-2 | `Human_Session_Replays_In_Runner` | a recorded Unity session replays in the runner at every checkpoint; the command log has at most one movement command per body per tick and none from a paused frame |
| H-D5-3 | `Movement_Quantisation_Stable` | the same sequence of raw stick vectors at two different frame rates gives the same quantised intents at the same ticks and the same final hash |
| H-D5-4 | `Divinity_Pins_P1_to_P7` | the seven pins of 12.4 |
| H-D5-5 | `Save_Load_With_Divine_State` | as the save row of 12.1: continued hashes equal the straight run; second save byte-identical |
| H-D5-6 | `Camera_Independence`, `Lod_Is_SimState_Only` | 12.5 |
| H-D5-7 | `Conservation_With_Miracles_And_Resurrection` | C1, the population identity and ownership integrity hold every day over 20 sim years of the integrated scenario, and after save and load |
| H-D5-8 | `Bench_D5_Recorded` | `bench` runs the cases of 12.6 for 100, 500, 1000 and 2000 persons and writes the new columns; the idle divine and organisation switches change the cost by a measured amount that is written in `DEV_LOG.md` (no pass threshold until measured; a regression of the existing cases beyond 15% fails the gate) |

# AI training design (stage 4 readiness, training later)

## 1. Purpose and scope

**Owns:**
- the observation and action encodings for the four policy levels;
- `IntNet`, the integer-only inference engine inside the sim;
- the model file format, quantisation-aware training and the verification chain;
- the training pipeline (imitation, then reinforcement learning, self-play, curricula);
- the Python bridge;
- reward neutrality rules.

**Does not own (link only):**
- the policy interfaces and the rule that policies never read `GodState`: [DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#7-policy-levels), [COMMUNICATION_AND_COGNITION_DESIGN.md](COMMUNICATION_AND_COGNITION_DESIGN.md);
- the learned association store, its decay and its inputs to the decider: [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md);
- drives and body: [POPULATION_DESIGN.md](POPULATION_DESIGN.md);
- institution views and powers: [INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md); the faction view: [INFORMATION_AND_MEDIA_DESIGN.md](INFORMATION_AND_MEDIA_DESIGN.md);
- the runner, replays and hashes: [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md); event and snapshot storage: [DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md);
- the benchmark scripted policies: [STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md); experiments that use trained policies: [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md).

**No training happens in stage 4.** Stage 4 builds the seams, the encodings, the integer engine, the dataset export, the bridge and the verification tool, and proves them with a tiny imitation network (section 12, `BC_SanityCheck`). Training at scale is stage 7.

## 2. Principles

1. **Four independent levels.** Civilian, military, institution and faction policies are replaced one at a time. Any mix of scripted and trained policies must run. A trained faction policy can play against scripted NPCs, and the reverse.
2. **Scripted first, always.** Every level has a working scripted policy before any network exists. The scripted policy is the teacher for behaviour cloning, the baseline for evaluation and the fallback if a model fails to load.
3. **Individuals keep lifetime learning.** A trained NPC policy does not replace the NPC's associative learning, memories or beliefs. Those stay in the NPC's mind and keep changing during play. The network reads them as inputs. Two NPCs with the same weights behave differently because their minds differ. Offline training is one of the five learning mechanisms and never replaces the others ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#2-owner-decisions-complete-list-2526-nov-2026)).
4. **Same information as a human.** An institution policy sees what a human running that institution sees (`InstitutionView`). A faction policy sees what the human player sees (`FactionKnowledgeView`). Same budgets, same powers, same commands. No policy gets a private channel.
5. **No true state.** An encoder takes only its policy's own view types. The reflection test of M1 extends to encoder types. Nothing in an observation may come from `GodState` except through the information layer the policy is allowed to read ([layers](DESIGN_CONVENTIONS.md#6-information-layers-one-vocabulary-for-every-doc)).
6. **What is tested is what runs.** The deployed forward pass is the C# `IntNet`. Training uses a differentiable twin, but the verification chain (section 4.4) proves the exported integer model gives the same outputs in C# and in a bit-exact Python mirror before it is allowed in a game.
7. **Neutral rewards.** Rewards are the agent's own objective, never a political or economic doctrine (section 11).
8. **Training is outside the sim.** Training code, datasets and the bridge are consumers. A sim running a trained policy needs only a model file and `IntNet`. Recording or serving never changes hashes ([DESIGN_CONVENTIONS.md](DESIGN_CONVENTIONS.md#3-layers-and-determinism-unchanged-from-stages-13)).

## 3. Data structures

### 3.1 Policy seam recap (M1)
```csharp
// Individual levels: one intent per think.
interface INpcPolicy         { NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng); }
interface IMilitaryPolicy    { NpcIntent Decide(in SoldierObservation obs, MindView mind, ref Rng rng); }
// Institution and faction levels: zero or more commands per think, the same commands a human sends.
interface IInstitutionPolicy { void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands); }
interface IFactionPolicy     { void Decide(in FactionKnowledgeView view, ref Rng rng, List<Command> outCommands); }
```
The pattern is fixed in the [conventions, section 7](DESIGN_CONVENTIONS.md#7-policy-levels). `rng` is the agent's own forked stream (`Rng.Fork("npc", id)`, `Rng.Fork("inst", id)`, `Rng.Fork("faction", id)`).
Each is looked up in `PolicyRegistry` by `(level, scope, policyId)`. A scope is a faction, an institution kind or a single NPC id, so an experiment can give one faction a trained policy and another a scripted one. A trained policy is `IntNetPolicy<TEncoder>`: a thin class that encodes, runs `IntNet`, decodes and validates.

### 3.2 `MindView`: what the network needs from the NPC's own mind
`MindView` is defined and versioned in [NPC_LEARNING_DESIGN.md](NPC_LEARNING_DESIGN.md#36-mindview-the-read-only-mind-interface), which owns the association store. The encoders use it as is; this document adds nothing to it. The parts and sizes the encoders rely on:

| `MindView` call | Size | Used for |
|---|---|---|
| `Drives` | `DriveCount` values, 0..1000 | urgency, state |
| `CandidateTerms` | `CAND_TERMS = 16` per candidate: the score terms, familiarity and the top 4 keys' weights | per-candidate learned value |
| `TopAssociations` | `A = 8` `AssocSlot` rows of 5 values | learned habits as inputs |
| `AssocSummary` | `LEARN_DIM = 16` (one per verb group), -127..127 | compact alternative to the slots |
| `RelationshipSummary` | `REL_DIM = 8` per visible NPC | one row per visible NPC |
| `BeliefSummary` | `BELIEF_DIM = 12` | planning |
| `Memory` | `MEM_DIM = 8` | recent experience |

The requirements this document places on it (deterministic order and ties, the same context as the scripted decider, no raw item ids or true positions, bounded cost, `LayoutVersion` in the model header) are rules of that definition. If the layout changes, the encoder version changes and old models stop loading.

### 3.3 Observation tensors
A tensor is a fixed-size `short[]` (int16, scaled integers, no floats anywhere). Entity lists have a fixed capacity, zero padding and a mask.

```csharp
struct EncodedObs {
    short[] Self;        // fixed width
    short[] Entities;    // [Cap, EntityDim] row-major
    sbyte[] EntityMask;  // [Cap] 1 = real row
    short[] Candidates;  // [CandCap, CandDim] features of each legal target
    sbyte[] CandMask;    // [CandCap]
    sbyte[] VerbMask;    // [VerbCount] from the sim's own validation
}
```
Scaling rule: every raw value is an integer in a documented range, mapped to fixed point with `FRAC = 8` (256 = 1.0). Counts are clamped to a stated maximum and divided by a stated constant, never normalised by a dataset statistic. Constants are part of the encoder version.

### 3.4 Encodings by level

**Civilian (`INpcPolicy`), `NpcEncoderV1`**

| Block | Contents | Width |
|---|---|---|
| Self | drives (5-8), body (health, load, age stage), top 4 skills, personality traits, hut and household flags, time of day (table lookup), faction-rank flag | 48 |
| Mind | `AssocSummary` (16), `BeliefSummary` (12), `Memory` (8) | 36 |
| Candidate terms | up to 16 candidates x `CandidateTerms` (16) | 16 x 16 |
| Associations | 8 `AssocSlot` rows, flattened | 40 |
| Visible things | up to 12 rows: class id, relative position (clamped), visible state, owner flag, reachable flag | 12 x 8 |
| Visible NPCs | up to 8 rows: faction, visible gear class, visible action, visible gesture, `RelationshipSummary` (8), known flag | 8 x 16 |
| Messages | up to 4 rows: gesture tokens (up to 3), sender slot, decoded confidence | 4 x 8 |
| Postings | up to 4 known jobs or offers: kind, pay class, distance | 4 x 4 |

Class ids and verbs are integer indices into **embedding tables** (an `Embed` layer in the model), instead of one-hot inputs.

**Military (`IMilitaryPolicy`), `SoldierEncoderV1`:** self (health, morale, kit state, rations); orders from the assignment; up to 8 allies and 8 enemies (relative position, strength class, health class); a 5x5 patch of terrain and cover classes; the same `MindView` blocks, because a soldier is the same NPC. Verbs: move, hold, attack, retreat, surrender, assist.

**Institution (`IInstitutionPolicy`), `InstitutionEncoderV1`, reads `InstitutionView`:**
- own budget (allocation, income this period, arrears);
- staff (up to 16 rows: role, skill class, wage, tenure, reported workload);
- records held (counts and ages, plus up to 16 case rows);
- the faction's directives: targets, budget caps, legal powers;
- its own **filed returns** (`ReturnFiled`, which the manager may game) and the KPIs the faction reports to it, never the actual values, because a human manager sees only records and returns. Incentives are paid on returns only, which keeps Goodhart's law possible ([INSTITUTIONAL_STATE_DESIGN.md](INSTITUTIONAL_STATE_DESIGN.md)).

`InstitutionView` already excludes what a manager could not know, and the encoder adds nothing to it.

**Faction (`IFactionPolicy`), `FactionEncoderV1`, reads `FactionKnowledgeView` (layer 7):**
- treasury and stock by goods class;
- tax and wage settings;
- population by age band and occupation as reported (nearly complete in stage 4);
- institutions (up to 16 rows: kind, budget, reported KPI (`KpiReported`; equal to the actual value under `FullInformation`, from records and returns in later ages), staffing);
- open sites and contracts (up to 16 rows);
- stance to each other faction (up to 5 rows) and visible strength reports;
- a coarse map grid (for example 16 x 16 cells x 4 classes) of owned and neighbouring areas.

### 3.5 Factorised actions
Every level outputs a **verb**, then (if the verb needs one) a **target from the candidate list**, then an optional small **parameter bucket**. No network outputs raw coordinates or ids.

```csharp
struct EncodedAction { byte Verb; byte Target; byte Param; }   // Target indexes Candidates
```
| Level | Verbs (sketch) | Candidate targets | Param |
|---|---|---|---|
| Civilian | eat, forage, sleep, go home, take job, offer, answer offer, give, ask, gesture, back off, flee, build, migrate | attended things and NPCs, postings, places | quantity bucket, gesture token |
| Military | move, hold, attack, retreat, surrender, assist | allies, enemies, short list of positions | none |
| Institution | hire, fire, set wage, set priority, open case, close case, request budget, file return (`FileReturn`), do nothing | staff rows, case rows | amount bucket (5 steps) |
| Faction | set tax, set wage, post contract, designate site, commit treasury, fund institution, change policy, enlist order, declare war or truce, stop | sites, institutions, factions, goods classes | amount bucket |

**Action masks come from the sim's own validation, not from a Python copy.** `LegalActions(view)` runs the same checks the command handlers run (affordability, range, powers, cooldowns) and returns `VerbMask` and `CandMask`, plus a `VerbTargetMask[Verb, Cand]` table. Decoding order: masked argmax over verbs, then masked argmax over the chosen verb's row, then the parameter. An illegal command is never sent.

A faction acts with a short sequence per think: up to `MaxActionsPerThink = 4` steps, each seeing the updated view (budgets already reduced), ending on `Stop`. This matches how a human acts.

### 3.6 `IntNet` and the model file
```csharp
sealed class IntNet {                         // Assets/God/Sim/Policy
    ModelHeader Header;                       // magic, version, level, encoder id and version, dims, hash
    IntLayer[] Layers;                        // Dense, Embed, TableAct, MaskedArgmax
    void Forward(ReadOnlySpan<short> input, Span<short> output);
}
struct DenseLayer { sbyte[] W; int[] Bias; int InDim, OutDim; int[] MulQ; byte Shift; }
```
No floats, no `Math`, no hardware-dependent vector types.

## 4. Mechanics and algorithms

### 4.1 Integer inference: `IntNet`
- **Weights:** int8 per layer, with a per-output-channel integer multiplier.
- **Activations:** int16 fixed point (`FRAC = 8`), saturated to -32768..32767. Between dense layers, activations are requantised to -127..127 so each multiply is int8 by int8.
- **Accumulators:** int32. The exporter checks `InDim x 127 x 127 + max|bias|` fits in int32. For `InDim <= 1024` that is about 16.5 million, a large margin.
- **Dense layer, exactly:**
```text
acc = bias[o]
for i in 0 .. InDim-1 ascending: acc += (int)x[i] * (int)W[o,i]
y = (int)(((long)acc * MulQ[o] + (1L << (Shift-1))) >> Shift)   // round half up, arithmetic shift
y = clamp(y, -127, 127)           // final logits layer: int16 range
```
The summation order is fixed ascending. Integer addition has no rounding, but the reference fixes the order so that any SIMD port must prove equality. No wrapping: the exporter checks bounds and tests run in `checked` mode.
- **Nonlinearities:** lookup tables only. ReLU is a clamp. Tanh and sigmoid are 256-entry int8 tables indexed by `clamp(y, -128, 127) + 128`. The tables are in the model file, built by the exporter from integer formulas, so no platform `exp` is used at run time.
- **Embeddings:** `Embed` copies an int8 row by id. An out-of-range id maps to row 0 (unknown).
- **Outputs:** int16 logits. **Masked argmax:** the highest logit among unmasked entries, ties to the lowest index. No softmax and no sampling in the sim.
- **Exploration in the sim:** stochastic play matters for mixed strategies. The model header holds an integer temperature `T`. The decision is `argmax(logit + (noise * T >> 8))` with `noise` drawn from the agent's forked stream (`Rng.Fork("npc", id)`). `T = 0` is greedy. RL may train with softmax sampling, but evaluation reports the deployed scheme, and the verification tool tests both the greedy and the noisy path.
- **Cost:** a civilian net of about 150x256 + 256x256 + 256x80 weights is about 125,000 multiply-adds. With 200 NPCs thinking once per sim second (`ThinkEvery` 10 ticks, [POPULATION_DESIGN.md](POPULATION_DESIGN.md)), that is about 25 million MACs per sim second, one core at real time and the main cost at maximum speed. The runner reports forward passes per second ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)).

### 4.2 Quantisation-aware training (PyTorch)
- The Python model mirrors the `IntNet` architecture layer for layer, with **fake-quantised** weights and activations (straight-through estimator for rounding). Scale conventions equal `IntNet`'s: power-of-two activation scales and per-channel integer multipliers, so the exporter converts the trained model to integers **exactly**, with no recalibration that could change behaviour.
- **Export** (`export_intnet.py`): int8 weights, `(MulQ, Shift)` pairs, integer-built tables, bounds checked. It refuses a model that could overflow int32 or needs `Shift > 30`.
- **Why QAT, not post-training quantisation:** with PTQ the deployed network differs from the trained one, and small differences flip marginal decisions. QAT makes the trained model the integer model.
- Float evaluation is only a diagnostic. Reported results always come from games running the exported integer model.

### 4.3 Model file format (`.intnet`)
```text
magic            "GINT"
formatVersion    u16
level            u8     0 civilian, 1 military, 2 institution, 3 faction
encoderId        u16    e.g. NpcEncoderV1
encoderVersion   u16
mindLayoutVer    u16    MindView layout (civilian, military)
inputDim, output spec   verb count, candidate cap, param buckets
temperature      u16
layerCount       u16
layers...        type, dims, MulQ[], Shift, int8 W[], int32 bias[]
tables...        256-entry int8 nonlinearity tables
metadata         training run id, dataset hash, git commit, reward definition (text, not hashed)
modelHash        32 bytes, SHA-256 over everything above except metadata and this field
```
The loader recomputes the hash and refuses a mismatch. It refuses a model whose encoder id, encoder version or `mindLayoutVer` differ from the build. **The model hash goes into the save file and the replay file**, so a replay names the models it used, and `StateHash` covers every loaded model hash (a different model gives a different hash, as it should).

### 4.4 Verification chain (before any model enters a game)
```text
export  -> model.intnet
step 1  record N >= 10,000 observations from real runs (encoded tensors + masks), spread over seeds and phases
step 2  C#     IntNet.Forward on them -> out_cs.bin      (God.Runner verify-model)
step 3  Python integer mirror (Python ints or NumPy int64, no floats) -> out_py.bin
step 4  require out_cs.bin byte-identical to out_py.bin (all logits, not only the argmax)
step 5  replay check: headless game with the model, twice, equal StateHash at every checkpoint; also from a save
step 6  encoder check: re-encode the recorded sources and require identical tensors (catches encoder drift)
```
A model that passes gets a sidecar `model.intnet.verified` (model hash, tool versions, counts). The registry loads models for experiments only with the stamp; a development flag allows unverified models and every report then says so. The Python mirror is a separate, small, readable implementation of section 4.1 and never calls PyTorch. The PyTorch QAT model is compared with it only in a looser diagnostic (argmax agreement).

### 4.5 Training pipeline

**A. Behaviour cloning from scripted policies**
- The runner `dataset` command runs scripted policies and writes `(encoded obs, masks, chosen action)` rows per level, with run id, seed, tick and policy id, in chunked binary files plus an index (Parquet via DuckDB for large sets). It is a recorder outside the sim, so turning it on never changes hashes.
- Targets are the scripted policy's chosen verb, target index and param bucket. Loss: masked cross-entropy per head, with masks applied in the loss.
- Scripted policies use forked-stream noise. The recorded action is the actual choice, so the data contains the noise, which sets an accuracy ceiling. Report it.
- Deduplicate near-identical rows and weight rare verbs (inverse frequency, capped).
- **Purpose:** a pipeline test, and a warm start for RL.

**B. Reinforcement learning**
- PPO or similar actor-critic, masked categorical heads (log-probability is the sum over heads) and a value head.
- Warm start from the cloned network with an early KL penalty to it, to avoid collapse.
- Many headless environments in parallel (section 5). QAT throughout, so every checkpoint exports without a gap. If QAT RL is unstable: float with a fake-quant noise schedule, then QAT fine-tune, then verify.
- **Evaluation:** fixed seeds and scenarios, exported integer model in the real runner, compared with the scripted policy at the same seeds (common random numbers, see [POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).

**C. Faction self-play and leagues**
- Opponents come from a **league**: current self, past checkpoints, the scripted incentive AI and the benchmark strategies. Checkpoints are added every N updates, with sampling weights favouring recent and strong ones.
- A win-rate table across the league monitors cycling and forgetting. League members are `.intnet` files, so any match replays exactly.

**D. Curricula from saved mid-game states**
- A full sandbox game gives sparse feedback. The runner can **start from a saved state**: early, middle and late states from many seeds, tagged by scenario. Short-horizon tasks ("recover from a famine", "hold the border for 5 days") ordered easy to hard build the curriculum. Saves are full serialisable sim state ([HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)).
- States from scripted runs are biased toward what scripted play reaches, so mix in states from earlier checkpoints' own play.

**Multi-agent issues**
- **Shared parameters for NPCs.** One civilian network serves all NPCs, each an agent, all experience in one batch. NPCs differ in traits and learned associations, which are inputs, so shared weights do not mean identical behaviour.
- **Credit assignment.** Consequences are delayed and social (a gift now, repayment days later). Use per-agent rewards from the agent's own drives, a discount near 1 over the planning horizon, generalised advantage estimation and only neutral shaping (section 11). Give no group reward the individual cannot influence, unless the experiment is about group objectives and says so.
- **Non-stationarity.** Others adapt during training. Use populations of past policies as opponents and periodically freeze the others.
- **Births and deaths.** An agent's episode is its life, or a window of it. A newborn starts with instincts and an empty association store, so the network must act well from the start and use what it learns later. Memory inputs make this possible, so the network stays **feed-forward and stateless**, which keeps `IntNet` simple and saves trivial.
- **Lifetime learning and offline training together.** The store keeps updating at play time, so the input distribution shifts through life. Train in the real environment with the real learning rule running. Never freeze the store during training.

## 5. The Python bridge

### 5.0 Runner commands used here
[HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md) owns the CLI. This document needs three commands (M15):

| Command | Flags | Does |
|---|---|---|
| `dataset` | `--scenario <file> --seeds <a..b> --level <civilian\|military\|institution\|faction> --policy <id> --days <n> --out <dir> [--chunk <rows>]` | runs scripted policies and writes encoded observations, masks and chosen actions (4.5 A) |
| `serve` | `--port <n>` or `--pipe <name>`, `--sims <K> [--inference intnet] [--write-actions <file>]` | hosts `K` sims for the Python bridge (5.1) |
| `verify-model` | `--model <file.intnet> --obs <recorded obs> [--python-out <file>] [--replay <scenario> --seeds <a..b>]` | runs the C# side of the verification chain (4.4) and writes the `.verified` stamp when every step passes |

### 5.1 Runner serve mode
`God.Runner serve` hosts sims for Python.
- **Transport:** local TCP socket or named pipe, length-prefixed little-endian binary tensors. Shared-memory rings are a later optimisation.
- **Messages:** `Reset(scenario, seed, options)`, `Step(actions)`, `Close`, `Save`, `Load(state)`, `SetPolicy(level, scope, modelFile)`, `GetSpec`.
- **`GetSpec`** returns tensor shapes, verb lists, mask shapes and encoder versions, so Python never hard-codes layout.
- **One process, many sims.** A runner process hosts `K` independent sims, one per thread, each deterministic (no shared mutable statics, a rule from M0). One batched message carries `K` observations.
- **Decision stepping.** The runner advances until some controlled agent needs a decision (NPC think tick, institution review, faction think), returns all pending observations, and waits for actions. Uncontrolled agents run their configured policy inside the sim.
- **Determinism:** the same seed and action sequence give the same states. The runner can write the action log, which with the seed is a replay.

### 5.2 PettingZoo-style parallel API
```python
env = GodEnv(scenario="band_village", level="civilian", num_envs=64, seeds=range(64))
obs, infos = env.reset()
while env.agents:
    actions = {a: policy(obs[a]) for a in env.agents}
    obs, rewards, terminations, truncations, infos = env.step(actions)
```
- `agents` changes as NPCs are born and die. Ids are `(env index, npc id)`.
- **Vectorisation:** `GodVecEnv` stacks agents from all envs into padded batches with a mask for live rows and rows needing a decision, so one GPU forward pass serves all of them.
- Observation spaces are `Dict` of `Box(int16)`. Actions are `MultiDiscrete` (verb, target, param). Action masks are in the observation and in `info`.

### 5.3 Throughput targets (to be measured, not promises)
| Item | Target |
|---|---|
| Headless sim, no recorder | budget set in HEADLESS_SIMULATION.md |
| Civilian decisions per second per core, with encoding | at least 20,000 |
| Bridge overhead per batched step of 64 envs | under 5 ms on localhost |
| Whole pipeline, 16 cores plus one GPU | at least 50,000 agent-steps per second for civilian RL |
| One faction game of 2 sim years, headless | under 10 minutes |

If measured numbers fall short, the levers are fewer decision points (longer think intervals), smaller entity caps, shared observation computation and levels of detail.

### 5.4 GPU use
Python holds the networks, and training and batched inference use the GPU. The sim runs on CPU and never needs a GPU. Games run `IntNet` on CPU only. For fast rollouts Python may choose actions with the float QAT twin on the GPU, but the bridge also supports `inference=intnet` (the runner picks actions from a model file, no round trip) for evaluation, so reported numbers come from the deployed engine. The verification tool measures argmax disagreement between the QAT twin and `IntNet` on recorded rollouts, and a threshold (for example 0.5% of decisions) triggers an exporter check.

## 6. Interactions with other systems
- **Learning:** provides `MindView`. If its layout changes, the encoder version changes and old models stop loading, by design.
- **Cognition:** provides `NpcObservation`, candidate intents and validation, from which masks come. The scripted `AssociativeNpcPolicy` is the civilian teacher.
- **Institutions and state:** `InstitutionView` and `FactionKnowledgeView` define what policies see. If the HUD cannot show a field, no policy may read it.
- **Warehouse:** training runs usually record nothing. Evaluation runs record normally ([DATA_WAREHOUSE_DESIGN.md](DATA_WAREHOUSE_DESIGN.md)). Policy ids and model hashes are run attributes.
- **Experiments:** a trained policy is one more policy arm ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)).
- **Benchmarks:** the named strategies are benchmark civilian policies, usable as league opponents and distillation targets ([STRATEGY_BENCHMARKS.md](STRATEGY_BENCHMARKS.md)).
- **Existing AI:** `AiBrain` becomes the scripted `IFactionPolicy` and is the faction teacher.

## 7. Player and view
- **Player:** nothing in stage 4. Later, game setup picks a policy for each level and faction from a list of verified models (name, level, hash, date).
- **Developer panel:** policy id, model hash, chosen verb and the top 3 logits for a selected NPC or institution. Read-only.
- **Animations:** unchanged. The view animates events, not policy internals.
- **Training dashboards:** outside the game (TensorBoard or similar).

## 8. Telemetry
No new domain events. Additions to existing records:
- **Run record:** `policy_id` per level and scope, `model_hash`, `verified`, `encoder_version`.
- **`PolicyChanged`** when a policy is replaced mid-run.
- **Decision trace** (optional recorder level `decisions`, off by default; the level is listed in [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md)): per decision `tick`, `agent`, `level`, `verb`, `target`, `param`, top-2 logit gap and masked-choice count. Never feeds back into the sim.
- **Dataset files** carry the encoder and `MindView` layout versions.

## 9. Stage 4 minimum vs later stages

**Stage 4:**
| Milestone | Delivers |
|---|---|
| M1 | the four interfaces, `PolicyRegistry`, `FactionKnowledgeView`, the reflection test extended to encoders |
| M3 | `MindView` skeleton and first `AssocSummary` (with NPC_LEARNING_DESIGN.md) |
| M12, M14 | `InstitutionView`; the scripted incentive faction policy, the teacher |
| M15 | encoders V1 for all four levels; masks from sim validation; `IntNet`, loader and model hash; network wrappers for all four levels; dataset export (civilian and faction first); runner serve mode and a minimal Python package with the parallel env; `verify-model`, the Python integer mirror and the replay hash check; a small cloned civilian net and a small faction net as pipeline proof |

**Later:**
- Stage 5-6: behaviour cloning for institution policies as institutions appear; small RL runs on narrow tasks (foraging, one institution) to find pipeline problems.
- Stage 7: training at scale, faction league play, curricula, trained institution managers whose incentives invite Goodhart behaviour, and large-scale evaluation in the experiment suite.
- Recurrent or attention policies would need integer recurrent state saved with the NPC. Not planned: the mind's memory carries state first.

## 10. Migration from the existing code
- `AiBrain.Think` reads `GodState` directly and sends commands through `GodSim.Submit`. In M1 it is wrapped as an `IFactionPolicy` that takes a `FactionKnowledgeView` and outputs the same `Command` types.
- `AiBrain` keeps no memory between thinks and draws randomness only from `ThinkRng` (a fork by faction and tick), so it already meets the determinism rules and is a clean teacher.
- `Commands.cs` commands are the faction action space. Validate and apply steps are split where needed, so masks reuse the handlers' validation.
- `MilitaryTuning` statics move into `SimTuning` in M0, a precondition for many sims in one process.

## 11. Safety and neutrality of rewards
1. **Faction reward is the chosen victory condition.** In VP mode, the VP margin and the win. In sandbox mode, the objective stated in the run's setup file (for example population at year 10). No default objective names a political or economic system as good. Shaping terms are allowed only if they are in the setup file and reported with the results.
2. **NPC reward is the NPC's own wellbeing:** the fall in its weighted drive sum, plus injury and loss terms, using the same weights the scripted decider uses. It never rewards cooperation, obedience, productivity or ownership as such. If NPCs learn to cooperate, it is because cooperating paid in their environment.
3. **Institution reward is the institution's own incentive structure** (incentives paid on its filed returns, budget retention, staff income), which can diverge from the real outcome. Measuring the divergence is a result ([POLICY_EXPERIMENTS_DESIGN.md](POLICY_EXPERIMENTS_DESIGN.md)), not a bug to train away.
4. **No hidden objectives.** The reward definition and its code hash go into the model metadata and the experiment report.
5. **Fair comparisons.** When arms differ in policy source, both run the same environments and seeds.
6. **No reward on an experiment's outcome metric** unless the experiment says the policy optimises it. Train on the policy's own objective, then measure other outcomes.
7. **Exploits are findings.** A policy may find a loophole in the rules (a gift loop that fills a drive). Report it, fix the rule, retrain. Do not silently clip the reward.
8. **Audit trail:** reward code hash, seeds, git commit, dataset hashes and model hashes for every training run.

## 12. Tests and acceptance criteria

| Test | Criterion |
|---|---|
| `IntNet_Dense_KnownVectors` | hand-computed layer outputs match exactly, including rounding and saturation edge cases |
| `IntNet_NoFloat` | a source scan finds no `float`, `double` or `Math.` in `Sim/Policy/IntNet*` |
| `IntNet_Overflow_Bounds` | the exporter rejects a model whose worst-case accumulator exceeds int32; tests run in `checked` mode |
| `IntNet_CsMatchesPython_10k` | C# `IntNet` and the Python integer mirror give **byte-identical** logits on 10,000 recorded observations per level |
| `IntNet_Platform` | the same test passes under `dotnet test DotNet` and in Unity EditMode (Mono, and IL2CPP when available) |
| `ModelFile_RoundTrip_Hash` | write, read, hash equal; one flipped bit makes loading fail |
| `ModelFile_EncoderMismatch` | a model with a different encoder or `MindView` layout version is refused |
| `Policy_Replay_Identical` | a game with `IntNet` civilian and faction policies, 2 sim years, run twice, has the same `StateHash` at every checkpoint; a save and load in the middle gives the same final hash |
| `Policy_Mixed_Levels` | all 16 scripted/trained combinations of the four levels run and replay identically |
| `Encoding_Deterministic` | encoding the same state twice gives equal tensors; changing the insertion order of internal collections (test build) does not change them |
| `Encoding_NoTrueState` | encoder types reference no `GodState` (reflection); and a **leak test**: changing true-state fields the layer does not expose (an unseen far stockpile, another faction's hidden treasury) leaves the tensor unchanged |
| `Encoding_Bounds` | every value is in its documented range, padded rows are zero, masks match real row counts |
| `Masks_MatchValidation` | on 10,000 sampled decision points, every action marked legal passes handler validation and every one marked illegal fails it |
| `Masked_Argmax_Ties` | ties go to the lowest index; masking the best action returns the best of the rest; all-masked returns `Stop` or do nothing |
| `Dataset_Recording_Off_Same_Hash` | the final hash is identical with the dataset recorder on or off |
| `BC_SanityCheck` | a small `IntNet` (two hidden layers of 128) cloned from the scripted civilian policy reproduces its verb on **more than 99%** of held-out decisions when the policy is deterministic (and at least 95% of the noise ceiling otherwise); the scripted faction policy: over 99% on verb and 97% on the full action |
| `BC_Behaviour_Parity` | a game played by the cloned civilian net matches the scripted game on survivors at day 30 and food stock within 10% over 20 seeds |
| `Bridge_Determinism` | the Python env with a fixed action rule and seed gives the same observation hash sequence on two runs |
| `Bridge_Throughput_Report` | prints the numbers of section 5.3; logs a warning at more than 30% regression from the stored baseline (does not fail the build) |
| `Verify_Tool_EndToEnd` | `verify-model` passes a good model and exits non-zero when one weight differs in the Python mirror |

## 13. Open questions
None for the owner. Settled here, with defaults:
1. **Association inputs.** Both the 8 slots and the 16-value summary are encoded in `NpcEncoderV1`. An M15 ablation may drop one; that is an encoder version change.
2. **Entity caps** (12 things, 8 NPCs, 16 institutions) stay for stage 4. Raising them later changes the encoder version and invalidates models, which is allowed.
3. **Faction action form.** A sequence of up to 4 commands per think (3.5): simpler to mask and closer to human play.
4. **NPC think rate.** `ThinkEvery` 10 ticks (about once a second), staggered by id, with level of detail in [HEADLESS_SIMULATION.md](HEADLESS_SIMULATION.md). Decision counts and training cost follow from it.
5. **Cross-platform equality.** If training moves to Linux, `IntNet_CsMatchesPython_10k` also runs there. Nothing in the design needs one platform.

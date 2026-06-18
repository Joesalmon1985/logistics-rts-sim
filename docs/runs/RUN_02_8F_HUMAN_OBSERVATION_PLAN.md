# RUN_02.8F — Human Observation Plan (post workshop-blocker run)

Date: 2026-06-18  
Related checkpoint: [`docs/audit/RUN_02_8F_CHECKPOINT_WORKSHOP_BLOCKER.md`](../audit/RUN_02_8F_CHECKPOINT_WORKSHOP_BLOCKER.md)  
Gate status: **ECON_BOT_002 still red** (p1 `b=4`, p2 `b=5` at tick 800 on latest chain-debug)

This document is **planning only**. It tells you how to watch the best current bot behaviour in Godot without starting a new viewer implementation in this run.

---

## 1. Best available bot-vs-bot mode

### Branch / commit

| Item | Value |
|------|--------|
| **Branch** | `feature/run-02-8f-granular-bot-proof-diagnostics` |
| **Base** | `02aac40` (from `main`) |
| **State** | Uncommitted WIP at end of RUN_02.8F workshop-blocker work |

Use this branch locally. There is no separate “green” commit yet — the checkpoint doc records **still-red** verification.

### Which profile matches the gate?

| Profile constant | CLI name | Role |
|------------------|----------|------|
| `EconomyBvbSetup.PROFILE_PROOF` | `proof` | **ECON_BOT_002 gate profile** — real costs, real starting kit, `max_ticks=800` |
| `EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_EXACT` | `diag_real_costs_exact` | Diagnostic — exact chain kit, `diag_mode=true` |
| `EconomyBvbSetup.PROFILE_DIAG_REAL_COSTS_ABUNDANT` | `diag_real_costs_abundant` | Diagnostic — abundant starting wares, `diag_mode=true` |
| `EconomyBvbSetup.PROFILE_VISUAL` | `visual` | Used by the **spectator dual-bot scene** — same real economy as proof, but scenario `max_ticks=3000` |

**Authoritative sim behaviour (headless, post-fix):**

- **`PROFILE_PROOF`** — best match for the gate and the most complete watchable chain today.
  - p2 reliably completes **workshop (b=5)** by tick 800.
  - p1 stalls at **b=4** (`road_cap_reached:workshop` / workshop never finishes).
- **`PROFILE_DIAG_REAL_COSTS_ABUNDANT`** — *not* the best watch target (workshop-debug saw b≈2 at tick 800; chain stalls earlier).
- **`PROFILE_DIAG_REAL_COSTS_EXACT`** — not cleanly exercised in the workshop-debug batch (720s timeout before recorder perf fix); use only after re-running headless.

**If ECON_BOT_002 is still red (current state):** watch **`PROFILE_PROOF` behaviour** via headless chain-debug numbers, and use the **spectator dual-bot scene** for visual stepping. The scene currently loads **`PROFILE_VISUAL`** (see gap below) — economy rules match proof, but the scenario tick budget in data is 3000, not 800.

### Gap: spectator profile is hardcoded

The live viewer loads visual profile only:

```85:88:godot_project/client/core/economy_spectator_controller.gd
	scenario = EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_VISUAL)
	dual = DualBotRunner.new(runner)
	state = runner.create_game(scenario)
	EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
```

**Smallest follow-on (not done in this run):** change `PROFILE_VISUAL` → `PROFILE_PROOF` in `_reset_dual_bot()`, or add a profile dropdown / `--profile=` CLI arg. One-line constant swap is enough for a personal playtest.

---

## 2. Which Godot scene / view to run

### Scene inventory (economy / bots)

| Scene | Path | Purpose |
|-------|------|---------|
| **Economy spectator (recommended)** | `godot_project/client/scenes/economy_spectator_bot_demo.tscn` | Sprites, logistics routes, ownership, dual-bot stepping |
| Human proof harness | `godot_project/client/scenes/economy_human_proof_demo.tscn` | Dual-turn **human** play on proof start (not bot-vs-bot) |
| Economy trial viewer | `godot_project/client/scenes/economy_viewer_demo.tscn` | Single-player production-chain / build tutorial |
| Rollout observer | `godot_project/client/scenes/rollout_observer_demo.tscn` | Replay of saved rollout fixtures (not expansion duel) |
| Military viewer | `godot_project/client/scenes/military_viewer_demo.tscn` | Out of scope for economy bot proof |

No dedicated `economy_bvb_*.tscn` exists; BvB expansion is wired through **`EconomySpectatorController.DEMO_DUAL_BOT`**.

### Godot editor steps (recommended scene)

1. Install / use **Godot 4.5.2** (matches Linux verification).
2. **Project → Open** → select `godot_project/project.godot`.
3. In **FileSystem**, open `client/scenes/economy_spectator_bot_demo.tscn`.
4. **Scene → Run Current Scene** (or **F6**).  
   Do **not** rely on main scene — `project.godot` has no `run/main_scene` set.
5. In the running window, click **Dual-Bot View** (loads expansion duel + both bots).
6. Use **Step Once** (one p1 command + one p2 command + one sim tick), **Step 50**, or **Play/Pause** with **Speed** (1× → 2× → 4×).

Optional layers (right panel): **Toggle Logistics**, **Toggle Roads**, **Toggle Ownership**, **Toggle Sprites**, **Toggle Routes**.

### Run this command (CLI, non-headless)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$PWD/godot_project/.godot_user"   # or .godot_user_parallel/worker_01
mkdir -p "$GODOT_USER_DATA_DIR"

"$GODOT" --path "$PWD/godot_project" \
  res://client/scenes/economy_spectator_bot_demo.tscn
```

Then click **Dual-Bot View** in the UI (no CLI flag for profile or dual-bot mode today).

### Headless “what happened?” companion (same sim, no graphics)

Useful to confirm tick/building counts before or after watching:

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$PWD/godot_project/.godot_user_parallel/worker_01"

# End-state summary (PROFILE_PROOF, 800 ticks) — ~11 min wall
timeout 720s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_bvb_chain_debug.gd

# Workshop milestone JSON sidecar
timeout 720s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_bvb_workshop_debug.gd -- \
  --profile=proof --max-ticks=800
```

Output JSON: `training_runs/diagnostics/run_02_8f_economy_bvb/workshop_proof.json`

---

## 3. Bot-vs-bot observation instructions

### Launch dual-bot mode

1. Open **`economy_spectator_bot_demo.tscn`** (editor or CLI above).
2. Click **Dual-Bot View** — calls `controller.reset_demo(EconomySpectatorController.DEMO_DUAL_BOT)`.
3. Step the match:
   - **Step Once** = `EconomyExpansionBot` p1 → p2 → `runner.tick()` (same order as `DualBotRunner`).
   - **Step 50** × 16 ≈ 800 ticks (gate horizon).
   - **Play/Pause** + **Speed** for hands-off watching (still manual transport, not wall-clock realtime).

### Choosing PROFILE_PROOF / EXACT / ABUNDANT

| Goal | How today |
|------|-----------|
| **PROFILE_PROOF** (gate) | Headless tools accept `--profile=proof`. Spectator: temporary one-line change in `economy_spectator_controller.gd` `_reset_dual_bot()` (see §1). |
| **PROFILE_DIAG_REAL_COSTS_EXACT** | `run_economy_bvb_workshop_debug.gd -- --profile=diag_real_costs_exact --max-ticks=800` |
| **PROFILE_DIAG_REAL_COSTS_ABUNDANT** | `run_economy_bvb_workshop_debug.gd -- --profile=diag_real_costs_abundant --max-ticks=800` |

There is **no in-scene profile picker** yet. Diagnostic profiles also require `EconomyBvbSetup.attach_scenario_config(state, scenario)` in the viewer (spectator does not call this today) — another tiny follow-on if you want abundant/exact visuals in-editor.

### What you should expect to see (PROFILE_PROOF-equivalent, post-fix)

Use the checklist in §6. At a high level:

| Phase | Visual cues |
|-------|-------------|
| Early (~0–200) | HQ spines, bootstrap roads from each HQ, first **woodcutter** / **storehouse** sites |
| Mid (~200–500) | **Forester**, **sawmill** sites; carriers on routes; road network growing per player (ownership overlay) |
| Workshop attempt (~500–800) | **Workshop** construction site; connect roads; delivery pulses to sites; blocked/active site sprites |
| End state (tick ~800, current build) | **p2**: workshop **building completed** (5th building), bot status `chain_complete`. **p1**: often **no workshop building** — stall at 4 completed; may show `road_cap_reached:workshop` in headless noop reason |
| Logistics | Carriers moving; ware routes; jobs in summary panel (`jobs`, `routes` counts) |
| Failure signature | p1 workshop site missing or blocked; deliveries absent on p1 side; p2 may exceed road-cap visually if stepping continues past gate horizon |

Right-panel **Bot status** shows last command per player after each **Step Once**.

---

## 4. Human-vs-bot possibility

### Can I control one side?

**Answer: partial — dual-human only, not human-vs-bot.**

| Mode | Scene | p1 | p2 |
|------|-------|----|----|
| Bot vs bot (spectator) | `economy_spectator_bot_demo.tscn` | bot | bot |
| Dual-turn human proof | `economy_human_proof_demo.tscn` | **human** (active turn) | **human** (active turn) |
| Human vs bot | — | **not implemented** | — |

The human proof harness (`HumanProofHarnessController`) alternates **active_player_id** p1 → p2 after each submitted command; both sides are human-driven. There is no hook to run `EconomyExpansionBot` for one side while the other uses the command list.

### If human-vs-bot were supported (documented for reference)

Would use **`economy_human_proof_demo.tscn`** layout:

- **Human:** p1 — select legal command from list → **Submit Command**
- **Bot:** p2 — auto `DualBotRunner._apply_bot_turn` each tick
- Same map: seed **28001**, symmetric spawns, `EconomyHumanProofContract.proof_scenario()` (= `PROFILE_PROOF`)

Controls today in human proof scene:

1. **Reset Proof Scenario**
2. Command list (legal commands for **active** player only)
3. **Submit Command**
4. **Step Dual Tick** / dual noop buttons (advance without building)
5. Gate panel: b/r/d vs thresholds

Map interaction is **command-list based**, not map click-to-place (see `ViewerRoot` pattern in human proof demo).

### Smallest follow-on run to enable human p1 vs bot p2

**Suggested run ID:** `RUN_02_8G` (or next free slot) — **Human-vs-bot proof spectator (minimal)**

Scope (single PR, no new art):

1. Fork `HumanProofHarnessController` or extend `EconomySpectatorController`:
   - Load `EconomyBvbSetup.expansion_duel_scenario(PROFILE_PROOF)` + `attach_scenario_config` + `prepare_expansion_duel_map`.
   - After human p1 submits (or noops), call `EconomyExpansionBot` for p2 only, then `runner.tick()`.
2. Reuse **`economy_human_proof_demo.tscn`** UI (command list + gate panel) with label “p1 human / p2 bot”.
3. Add **`--profile=proof`** to spectator or harness CLI (optional).
4. Headless smoke: one tick p1 human noop + p2 bot step does not crash.
5. No new authoritative rules; no ECON_BOT_002 threshold changes.

Estimated size: **1 controller + scene wiring + short doc** (not a new renderer).

---

## 5. Recommended next playtest path

1. **Watch best bot-vs-bot game**  
   Open `economy_spectator_bot_demo.tscn` → **Dual-Bot View** → step to ~800 ticks with **Step 50** / autoplay. Optionally patch spectator to `PROFILE_PROOF` first. Cross-check with `run_economy_bvb_chain_debug.gd`.

2. **Same setup, human both sides**  
   Open `economy_human_proof_demo.tscn` → **Reset Proof Scenario** → play both p1 and p2 via command list. Compare ease of road connect + workshop delivery vs what bots do.

3. **Can a human finish the five-building chain faster?**  
   Use gate panel (b≥5 by tick 800). Oracle proves **one** p1 building is legal from this start; full five-building human path is **not** oracle-proven yet (see RUN_02.8E outcome classification).

4. **Decide failure class**  
   - If **you** reach b≥5 on p1 reliably → likely **bot intelligence / road discipline** (outcome C).  
   - If **you** also stall on p1 workshop with legal commands → **scenario / map / tick budget** (outcome A/D).  
   - If JSON oracle works but UI command list lacks entries → **UI gap** (outcome B).

---

## 6. Output format (quick reference)

### Run this scene

**Primary (bot vs bot watch):**  
`godot_project/client/scenes/economy_spectator_bot_demo.tscn`  
→ **Dual-Bot View** → step/autoplay.

**Secondary (human dual-turn on proof start):**  
`godot_project/client/scenes/economy_human_proof_demo.tscn`  
→ **Reset Proof Scenario**.

### Run this command

```bash
# Visual bot-vs-bot spectator
"$GODOT" --path godot_project res://client/scenes/economy_spectator_bot_demo.tscn

# Headless PROFILE_PROOF end-state (numbers only, ~11 min)
timeout 720s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_bvb_chain_debug.gd

# Workshop milestone report
timeout 720s "$GODOT" --headless --path godot_project \
  --script res://sim/tools/run_economy_bvb_workshop_debug.gd -- \
  --profile=proof --max-ticks=800
```

### What to look for (checklist)

- [ ] Both HQs and mirrored map; **ownership** distinguishes p1 vs p2
- [ ] Roads extend from HQ; **connect-site** roads appear at construction anchors
- [ ] Carriers and **ware routes** animate on logistics layer
- [ ] Chain order: woodcutter → storehouse → forester → sawmill → **workshop attempt**
- [ ] Construction sites show progress / blocked states
- [ ] **p2** completes a 5th building (workshop) near gate horizon
- [ ] **p1** stops at 4 buildings or stalls on workshop (`road_cap_reached:workshop` in headless)
- [ ] Upstream pauses visible in inspector/summary (output space, missing worker) on completed producers
- [ ] Compare headless `b=` / `noop=` lines with what you saw visually

### Can I control one side?

**Partial.**  
- **Yes** for dual-human proof (both sides human, alternating).  
- **No** for human p1 vs bot p2 in the same match (not built).

### If not yet, smallest next run to enable it

**RUN_02_8G — minimal human p1 vs bot p2 harness** (reuse human proof scene + `EconomyExpansionBot` for p2 only, `PROFILE_PROOF`, no gate changes). See §4.

---

## Appendix — related docs

- [`docs/audit/RUN_02_8E_HUMAN_PROOF_PLAYTEST_GUIDE.md`](../audit/RUN_02_8E_HUMAN_PROOF_PLAYTEST_GUIDE.md) — human proof UI workflow  
- [`docs/runs/RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md`](RUN_02_8_BOT_VS_BOT_ECONOMY_VISUALISATION.md) — original BvB viewer requirements  
- [`docs/verification/local_verification.md`](../verification/local_verification.md) — spectator smoke / capture commands  

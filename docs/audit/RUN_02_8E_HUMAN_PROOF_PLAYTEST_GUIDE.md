# RUN_02.8e — Human Proof Playtest Guide

## Launch

Open in Godot 4.5.2:

`godot_project/client/scenes/economy_human_proof_demo.tscn`

Or run the project with that scene as main.

## Headless oracle (recommended first)

```bash
export GODOT=/home/joe/Documents/Godot/Godot_v4.5.2-stable_linux.x86_64
export GODOT_USER_DATA_DIR="$(pwd)/godot_project/.godot_user"
timeout 120s $GODOT --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_oracle.gd -- \
  --plan=res://sim/tests/fixtures/human_proof/oracle_one_building_p1.json
```

Edit the JSON plan to experiment with legal commands before using the UI.

## Interactive dual-turn workflow

1. **Reset Proof Scenario** — loads seed `28001`, HQ spine, symmetric kit (exact `ECON_BOT_002` contract).
2. Note **Active player** (p1 or p2) in the status line.
3. Select a legal command for the active player → **Submit Command**.
4. After p2 submits, the sim advances one tick automatically.
5. Use **Dual Noop 10/50** to wait for deliveries/construction.
6. **Load Oracle Plan** replays the committed one-building reference path.
7. **Export Attempt** writes replay JSON to `training_runs/human_proof/`.

## Gates (same as ECON_BOT_002)

Per player (delta from baseline): **b≥5**, **r≥10**, **d≥10**, stall ≤300, score >0, by tick **800**.

## Checklist

- [ ] Objective clear from instructions panel?
- [ ] Legal build sites identifiable (forest density layer, inspector)?
- [ ] Road network needs visible (roads layer, connect-site labels)?
- [ ] Blocked construction reasons visible in inspector?
- [ ] One building completable (oracle fixture proves yes for p1)?
- [ ] Two buildings by tick ~400?
- [ ] Five buildings by tick 800?

## Outcome classification (required in report)

| Class | Meaning |
|-------|---------|
| **A** | Scenario impossible/too tight under legal commands (oracle fails) |
| **B** | Oracle passable, UI/client command list insufficient |
| **C** | Human/full commands passable; bot shortlist insufficient |
| **D** | Passable only with threshold/tick changes (needs user approval) |
| **E** | Test measurement/baseline wrong |

## Command availability note

Interactive harness uses **ClientCommandAvailability**. Oracle uses **CommandValidator** directly. A command that works in JSON but not in the UI is **outcome B**, not scenario failure.

## Replay verify

```bash
timeout 60s $GODOT --headless --path godot_project \
  --script res://sim/tools/run_economy_human_proof_replay.gd -- \
  --replay=training_runs/human_proof/<your_export>.json
```

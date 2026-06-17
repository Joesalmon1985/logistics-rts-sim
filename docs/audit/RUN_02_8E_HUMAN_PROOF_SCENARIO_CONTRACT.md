# RUN_02.8e — ECON_BOT_002 Human Proof Scenario Contract

Authoritative reference for the human/oracle proof harness. Any deviation must be labelled **diagnostic-only**.

## Scenario loader

```gdscript
EconomyBvbSetup.expansion_duel_scenario(EconomyBvbSetup.PROFILE_PROOF)
# then
EconomyBvbSetup.prepare_expansion_duel_map(state, scenario)
```

Shared helper: `EconomyHumanProofContract.create_proof_state()`.

## Identity

| Field | Value |
|-------|-------|
| Seed | `28001` |
| Map | `24×24` |
| Variant | `economy_trial` |
| Profile | `proof` |
| Max ticks | `800` |
| Stall threshold (match abort in bot test) | `600` |
| Build radius | `11` |
| Starting buildings | none (HQ only) |
| `apply_starting_buildings` | `false` |
| `connect_spawn_network` | `false` |

## Spawns (symmetric)

| Player | HQ | Starting wares | Carriers | Workers |
|--------|-----|----------------|----------|---------|
| p1 | (4, 4) | tools 12, planks 15, stone 12, rations 20, meals 10 | 4 | carrier×2, builder×2, woodcutter×1, carpenter×1 |
| p2 | (19, 19) | same | 4 | same |

## Map prep (proof profile)

1. Flatten grass, symmetric forest/iron pockets (radius 8 around each HQ).
2. Clear neutral corridor at map centre.
3. **HQ road spine:** 3 road segments per player via `_connect_hq_spine` **before** baselines.
4. `record_match_baselines` stores `bvb_baselines` meta (buildings, roads, deliveries at tick 0 after spine).

## Gate thresholds (delta from baseline, per player)

Same as [`test_econ_bot_002_dual_bot_expansion_metrics.gd`](../../godot_project/sim/tests/bot/test_econ_bot_002_dual_bot_expansion_metrics.gd):

| Metric | Minimum | Measurement |
|--------|---------|-------------|
| Buildings | 5 | `DualBotRunner.count_player_buildings_built` |
| Roads | 10 | `DualBotRunner.count_player_roads_built` |
| Deliveries | 10 | `DualBotRunner.count_player_deliveries_built` |
| Stall | ≤300 | `state.diagnostics.player_stalls[player].stall_ticks` |
| Score | >0 | `state.diagnostics.economy_scores[player]` |

Bot test also requires all recorded commands accepted.

## Turn model

**Dual-turn (human harness and oracle):** p1 command → p2 command → sim tick. Mirrors [`DualBotRunner`](../../godot_project/sim/bot/dual_bot_runner.gd).

## Command availability layers

| Layer | API | Used by |
|-------|-----|---------|
| Oracle | `CommandValidator` + JSON payloads | B0 oracle, replay |
| Full legal (sim) | `EconomyCommandAvailability.list_candidates` | Gap analysis |
| Client UI | `ClientCommandAvailability.list_from_snapshot` | Interactive harness |
| Bot shortlist | `EconomyCommandAvailability.list_bot_candidate_commands` | `ECON_BOT_002` bots |

If oracle accepts a command the client UI omits → **outcome B** (harness gap), not scenario impossibility.

## Known bot results (02.8d)

| Ticks | p1 b/r/d | Notes |
|-------|----------|-------|
| 800 | ~2 / ≥41 / ≥380 | Buildings gate fails |
| 3000 | ~1 / ~39 / ~1475 | Chain stall; not tick budget |

## Oracle reference path (committed fixture)

[`oracle_one_building_p1.json`](../../godot_project/sim/tests/fixtures/human_proof/oracle_one_building_p1.json): p1 completes one woodcutter from proof start (headless verified).

# Bot Recovery and Tutorial Economy AI — Current State

Date: 2026-06-26  
Branch: `feature/mrp-full-stack-autonomous`

## Existing bots

| Bot | Path | Purpose | Status |
|-----|------|---------|--------|
| `HeuristicEconomyBot` | `sim/bot/heuristic_economy_bot.gd` | Completes `tiny_success_map` workshop chain | Passes BOT-SMOKE-001 |
| `HeuristicEconomyBuilderBot` | `sim/economy/bot/heuristic_economy_builder_bot.gd` | Legacy scripted planner | Used by ECON-SMOKE-005 |
| `EconomyExpansionBot` | `sim/bot/economy_expansion_bot.gd` | Dual-bot expansion duel | **Fails building gate** |
| `HeuristicMilitaryBot` | `sim/bot/heuristic_military_bot.gd` | Skirmish 1v1 win | Passes BOT-SMOKE-002 |

## Bot tests

- 14 bot-related tests in `run_tests.gd`
- `ECON_BOT_002` / `BOT-SMOKE-004`: dual-bot expansion — **RED** (p1/p2 buildings &lt; 5 at 800 ticks, ~204s runtime)
- Individual regression tests B001–H010: pass
- Human proof oracle tests: 4/4 pass

## Known hangs

- Full `run_tests.gd` suite exceeds 900s timeout due to `ECON_BOT_002` (~204s) plus 257 other tests
- Dual-bot tests must not run in fast suite

## ECON_BOT_002 status

**Quarantined** to `run_bot_long.gd`. Root cause: `EconomyExpansionBot` stalls on workshop chain under road cap; not blocking tutorial bot work.

## Fast vs long suites

| Suite | Tool | Contents |
|-------|------|----------|
| Fast | `run_bot_fast.gd` | BOT-SMOKE-001, tutorial bot smoke, BOT-SMOKE-002 |
| Long | `run_bot_long.gd` | ECON_BOT_002, BOT-SMOKE-004, dual-bot debug |

## Command API

Bots must use: `PlaceRoadCommand`, `PlaceBuildingPlanCommand`, `SetWorkerPolicyCommand`, military commands via `CommandValidator`. No direct `GameState` mutation.

## Planned deliverable

`TutorialEconomyBot` — completes Settler MVP objective via same command API as human UI.

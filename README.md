# God of the Island

A Unity 6000.3.28f1 proof of concept (branch `unity/poc`). You are a god looking down on an island. You never control a person directly. You place buildings, and the people build them. You set which buildings get wares first. Raw materials appear on the land when the hidden Catan dice roll a tile's number. Harvesters collect them, and carriers walk them along roads. Workshops turn them into finer wares, or split them into two. Two factories turn wares into soldiers and light vehicles. These leave on their own to attack the enemy buildings you have marked, or to guard the places you have flagged.

The full plan, rules and package order are in [`docs/UNITY_POC_PLAN.md`](docs/UNITY_POC_PLAN.md). That file is the current plan. Everything else in `docs/` mostly describes the older Godot version of this project (settlement logistics and RTS rules, kept as design history and as a source of names and decisions). The Godot code itself is gone from this branch.

## State

The proof of concept is playable (24 Nov 2026). Packages P0 to P5 are merged on `unity/poc`:
- the island deal and hidden dice;
- territory, buildings and roads;
- the economy with carriers and priorities;
- the military with targets, defend flags and enemy waves;
- the natural island look;
- models;
- the god HUD.

Stage 2 (25 Nov 2026) is merged as packages P7 to P11:
- P7: six factions on a 30-hex island, war and peace, victory points, Citadels, capture and elimination;
- P8: trade envoys, offers and answers, the trade panel and offer card;
- P9: a faction AI for every non-human faction;
- P10: the start card, Esc menu, three save slots, watch mode and standings;
- P11: AI trade (`AiTrader`: the AIs offer spare goods for wares they lack the land for, and answer offers by their own needs), balance across seeds, and a watch smoke that ends with a winner on the build.

How to play: [`docs/PLAY_GUIDE.md`](docs/PLAY_GUIDE.md).

## Layout

| Path | What |
|---|---|
| `Assets/God/Sim` (`God.Sim`) | Pure C#, no UnityEngine. The whole game state and rules. Ticks in integers, receives commands. |
| `Assets/God/Game` (`God.Game`) | Views, camera, HUD and input. Reads the sim, sends commands, never changes sim state. |
| `Assets/God/Tests/EditMode`, `PlayMode` | NUnit tests. EditMode covers the sim; PlayMode covers the scene, views, models and HUD. |
| `Assets/God/Editor` | Editor code: the Windows build and the scene maker. |
| `Assets/God/Scenes/Main.unity` | The boot scene. |
| `DotNet/` | `God.Sim` and its tests compiled outside Unity (the `.cs` files are linked by glob). |
| `AgentScripts/` | Scripts for the Unity CLI (`unity command script run`). |

## Open

Install Unity 6000.3.28f1, then open this folder as a project (Unity Hub, or `unity open .` with the Unity CLI). Open `Assets/God/Scenes/Main.unity`.

## Test

- Outside Unity (fast): `dotnet test DotNet`
- EditMode, with the Unity CLI: `unity command test run --mode editor`
- PlayMode: `unity command test run --mode playmode --async-tests`, then poll `unity command test status`
- Or use Window > General > Test Runner in the Editor.

## Build

`unity command script run --file AgentScripts/BuildWindows.cs --entry BuildWindows.Run --timeout 900` writes `Builds/Windows/GodOfTheIsland.exe`. In batch mode (not tried yet): `unity build . --target StandaloneWindows64 --execute-method God.Editor.BuildWindows.RunBatch`.

## Play

Run `Builds/Windows/GodOfTheIsland.exe`, or open `Main.unity` and press Play. See [`docs/PLAY_GUIDE.md`](docs/PLAY_GUIDE.md).

## Smoke

`GodOfTheIsland.exe -batchmode -nographics -godsmoke 60000 -godseed 1 -logFile smoke.log` plays a watch game of six AI factions headless for up to 60000 ticks (100 sim-minutes). Every 1000 ticks it logs a `SMOKE` line with the standings (VP, HQs, Citadels, units, wars per faction), the wars, captures and eliminations so far, and the trades (offered, accepted, refused, envoys killed). It ends with the game-over line and a `SMOKE END` verdict, and quits with exit code 0 on success: no exception, invariants hold, and someone won (or at least 3 factions expanded with a war and a capture). The AI lives in `Assets/God/Sim/Ai`; its weights, trade ones included, are in `AiTuning`.

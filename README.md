# God of the Island

A Unity 6000.3.28f1 proof of concept (branch `unity/poc`). You are a god looking down on an island. You never control a person directly. You place buildings, and the people build them. You set which buildings get wares first. Raw materials appear on the land when the hidden Catan dice roll a tile's number. Harvesters collect them, and carriers walk them along roads. Workshops turn them into finer wares, or split them into two. Two factories turn wares into soldiers and light vehicles. These leave on their own to attack the enemy buildings you have marked, or to guard the places you have flagged.

The full plan, rules and package order are in [`docs/UNITY_POC_PLAN.md`](docs/UNITY_POC_PLAN.md). That file is the current plan. Everything else in `docs/` mostly describes the older Godot version of this project (settlement logistics and RTS rules, kept as design history and as a source of names and decisions). The Godot code itself is gone from this branch.

## State

Package P0 (project skeleton) is done: the Unity project, the pure C# sim core (seeded `Rng`, command/event tick loop at 10 Hz, hex topology, the 19-hex island deal with hidden numbers), the copied island terrain view, tests, a Windows build script and a boot scene that deals an island from a seed and shows it from the tilted god camera. There are no game rules yet. P1 (land, dice, territory) onwards adds them.

## Layout

| Path | What |
|---|---|
| `Assets/God/Sim` (`God.Sim`) | Pure C#, no UnityEngine. The whole game state and rules. Ticks in integers, receives commands. |
| `Assets/God/Game` (`God.Game`) | Views, camera, HUD and input. Reads the sim, sends commands, never changes sim state. |
| `Assets/God/Tests/EditMode`, `PlayMode` | NUnit tests. EditMode covers the sim, PlayMode the boot scene. |
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

Open `Main.unity` and press Play, or run `GodOfTheIsland.exe`. Today it shows a freshly dealt island from the god camera (add `-godseed N` to the command line for another island). The build menu, dice and everything else arrive with later packages.

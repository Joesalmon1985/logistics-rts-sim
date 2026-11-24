# God of the Island (Unity 6000.3.28f1)

Read `docs/UNITY_POC_PLAN.md` first. It is the current plan. The rest of `docs/` describes the older Godot version.

Rules that always apply:

- `Assets/God/Sim` is pure C# (`noEngineReferences`). Never reference UnityEngine there. It is deterministic: integers, the seeded `Rng`, no wall clock, no unordered enumeration. Bots and people use the same commands.
- `Assets/God/Game` only reads sim state, sends commands and animates events. It never mutates sim state.
- Sim code and its EditMode tests come before views. Tests must also pass under `dotnet test DotNet`.
- Private repo, but still: never commit `Library/`, `Temp/`, `Builds/`, `Logs/`, `UserSettings/`, `*.csproj`/`*.sln` outside `DotNet/`, `LocalData/`, or anything derived from Sims 2. No third-party art or audio. Original names and data. Run `git status` before each commit.
- Do not copy from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage`, `Assets/ThirdParty` or rigged actors of the WorldMVP project.
- One Unity editor per folder. Use the Unity CLI (`unity open`, `unity command test run`, `unity command script run --file AgentScripts/X.cs --entry Class.Method`). Close the editor when done. Never run the built player windowed in automation; use `-batchmode -nographics`.
- Commit small, logical commits on `unity/poc`. Do not push without asking. `main` changes only when the owner says so.
- Report which tests and runs actually happened, and say plainly what did not.

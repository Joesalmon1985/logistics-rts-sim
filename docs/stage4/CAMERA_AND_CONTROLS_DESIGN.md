# Camera and controls design (stage 4, divine player)

Status: design, 26 Nov 2026. Owner decisions: D5.23 (desktop first, mobile-compatible command architecture), D5.8 and D5.9 (possession and avatar), D5.7 (compulsion), D5.6 (NPC autonomy). Brief sections 6, 9, 26 and 46 ("continuous player movement must be translated into appropriate deterministic simulation inputs"). Package: U1 (camera, selection, player interaction), with D4 and D5 for the possession and avatar sim sides. Sister documents: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md), [VISUAL_WORLD_DESIGN.md](VISUAL_WORLD_DESIGN.md), [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md), [GAMEPLAY_SCENARIOS.md](GAMEPLAY_SCENARIOS.md).

## 1. Purpose and scope

How the player looks at the world and points at things (Mode A), how the camera follows a person, an avatar or a possessed body (Modes B and C), how clicks become selections and contextual actions, and how continuous movement from a keyboard, mouse, touch or stick becomes deterministic sim commands. Unity never decides an outcome: it picks a target, shapes an intent, sends a command and draws what the sim reports.

## 2. Current state (read from the code, 26 Nov 2026)

| Area | What exists | Where |
|---|---|---|
| Overhead camera | `GodCameraRig`: wheel zoom along the view ray under the cursor (each tick moves 14 percent towards the point, `ZoomStep`), right or middle drag grabs the ground and pans (6 px click slop, `Dragged`), WASD and arrows pan at `1.1 x height` per second, glide `FocusOn(ground, height)`, clamp of the looked-at point to the island (a slack of 30 m plus 85 percent of the radius as the player zooms in), `Reframe(radius, centre)`. `MinHeight` 28 m, `MaxHeight` set by framing the whole island | `Hud/GodCameraRig.cs` |
| Fixed angle | `IslandCamera.Frame`: pitch 50 degrees, FOV 30, from the south; the tilt never changes; no rotation | `Boot/IslandCamera.cs` |
| Picking helpers | `GroundPoint(screen)` iterates a ray against the ground height (`HeightAt`); `OverUi` supplied by the HUD, driven by the rectangles of drawn panels | `GodCameraRig.cs`, `GodHud.Gui.cs` |
| Selection | left click: `PickEnvoy`, else `PickNpc` (nearest figure within 2.5 percent of screen height, using the figure's drawn position), else `PickBuilding`, else `WorldClick(node)`; single selection only (`SelectedNpc`, `SelectedBuilding`, `SelectedNode`); right click without drag cancels the tool | `Hud/GodHud.cs`, `GodHud.People.cs` |
| Watch camera | keys `1` to `6` fly to a faction HQ, `Tab` cycles hot spots (captures and fights) | `Hud/WatchCamera.cs` |
| Input API | legacy `UnityEngine.Input` read directly inside `GodCameraRig.Update`, `GodHud.Update`, `WatchCamera.Update`; test hooks: `InputEnabled = false`, `DebugCursor`, public methods | those files |
| Figure position | `NpcLayer.TryPosition(id)` returns the smoothed drawn position (a frame-rate exponential follow of the node, `f.Pos`), used by pickers and cameras | `Models/NpcLayer.cs` |
| Time | the bootstrap steps the sim at `10 Hz x Speed` from `Time.deltaTime`; paused when `!Running`; `GodSim.TicksPerSecond = 10` | `Boot/GodBootstrap.cs`, `Sim/Core/GodSim.cs` |
| Replay of commands | `Command` objects submitted through `ICommandGate`, applied at the next tick boundary; the replay writer records commands at ticks (`Recording/Replay.cs`, the `.godr` file) | `Sim/Core/Commands.cs`, `Recording/` |

**Missing (needed):** camera rotation and variable pitch, close zoom to a person, follow tracking, third-person cameras, smooth mode transitions, multi-select, box and area selection, household and institution selection, a contextual action menu, a device-independent input layer, and everything on the sim side of control (a control input command, possession, avatar). The current NPC command path is `Intent`-driven by the NPC's own policy only; there is no external control.

## 3. Decisions and the input architecture

| Id | Decision |
|---|---|
| CC-1 | **Three layers, one direction:** `IInputSource` (device) produces `PlayerInputFrame`; a `PlayerController` turns frames into `PlayerIntent` objects (select, focus, orbit, arm power, confirm, move axis, invoke action); the intents drive the camera (view only) and the `PlayerCommandBuilder` (commands). Nothing but the input sources reads `UnityEngine.Input`. |
| CC-2 | **Only commands cross to the sim.** The camera, selection and UI state are not in the sim, the hash or the save's sim section. |
| CC-3 | **Camera independence.** The sim never reads the camera. Visibility, range, picking radius and LOD affect what is drawn and what can be clicked, never what happens (brief section 48). |
| CC-4 | **Selection resolves to ids; scope is resolved by the sim.** Unity sends a person id, household id, institution id, location node or an area (centre node and radius), never a list it built from what it can see (except a hand-picked list, which is explicit user intent). |
| CC-5 | **Unity does not predict outcomes.** Figures animate from sim positions; a control input that the sim rejects visibly does nothing. |
| CC-6 | **One camera component with modes,** `CameraDirector`, owning `GodCameraRig` (kept as its overhead implementation so existing tests remain valid), plus `FollowCam` and `ThirdPersonCam` behaviours, all view-only. |
| CC-7 | **Default numbers are configurable** (`ControlTuning`): transition 0.8 s, lease 3 ticks, follow smoothing 0.25 s, long-press 350 ms, drag slop 6 px (existing), orbit speed 0.25 degrees per pixel. |
| CC-8 | **Desktop first:** mouse and keyboard fully supported at Tier 1; touch and gamepad adapters at Tier 3; the intent layer is the same. |

```text
IInputSource (Mouse+Keyboard | Touch | Gamepad | Script/Test)
      -> PlayerInputFrame (pointer, buttons, axes, gestures, key chords)
      -> PlayerController -> PlayerIntent
              |-> CameraDirector (view only)
              |-> SelectionModel (view only)
              |-> PlayerCommandBuilder -> Gate.Check / Gate.Submit -> sim command queue -> (replay file)
```

`PlayerInputFrame` is plain data so tests and the AI-god harness (`ScriptedInput`) can feed it. This replaces the three scattered `Update` methods that read `Input` today.

## 4. Mode A: the free overhead camera

Parameters (replacing the fixed pitch): focus point on the ground, yaw, pitch, distance, plus FOV. The present `GodCameraRig` keeps the camera at a fixed 50 degree tilt from the south, positioned by a point and a height; the new orbit model reduces to it when yaw is 0 and pitch 50.

| Control | Desktop | Touch (later) | Notes |
|---|---|---|---|
| Pan | right or middle drag grabs the ground; WASD and arrows relative to the camera yaw; edge pan optional | one-finger drag | existing, adapted to yaw |
| Rotate | `Q` and `E`, or `Alt` plus drag, or middle-drag when the pan is on right | two-finger twist | new; snaps to north on `Home` |
| Pitch | `Alt` plus wheel, or `R` and `F` | two-finger vertical drag | range 25 to 80 degrees, default 50 |
| Zoom | wheel towards the cursor point (existing, `ZoomStep` 14 percent) | pinch | extend `MinHeight` from 28 m down to a close observation of about 4 m so one person fills the view; far limit is the whole island (`MaxHeight`, `IslandRadius`) |
| Whole island | `Home` or `H`, glides to the framed island (`Reframe`) | double-tap on the sea | existing framing |
| Focus on | double-click a person, building or alert, `F` on the selection | double-tap | `FocusOn` with a height by target size |
| Clamp | the looked-at point stays over the island with slack, as now; at close zoom the camera collision keeps the eye above the ground by `GroundY` plus 1.5 m | | |

**Zoom steps and level of detail.** Four named bands (whole island, region, settlement, individual) with default heights `MaxHeight`, 220 m, 90 m, 14 m drive the LOD of VISUAL_WORLD_DESIGN section 14 and the "zoom to person" key `Z`.

## 5. Camera modes and smooth switching

| Mode | Camera | Entered by | Left by |
|---|---|---|---|
| A Overhead | orbit overhead (section 4) | default; `Esc` from the others | selecting another mode |
| A+ Follow | overhead orbit whose focus point tracks the selected person's drawn position (`NpcLayer.TryPosition`) with 0.25 s smoothing; the player can still zoom, rotate and pitch; panning detaches (a "detached" chip offers re-follow) | `F` on a selected person, Follow button, roster chip | detach, `Esc`, death (the camera stays at the place, a memorial alert appears) |
| B Avatar third person | orbit behind the avatar at 6 to 10 m, pitch 15 to 35 degrees, shoulder offset, camera-relative movement; wheel changes distance; right-drag orbits | the `AvatarManifested` event after the player armed Manifest | `Dismiss`, the `AvatarDismissed` or `AvatarDestroyed` event, `Esc` after a confirm |
| C Possession third person | same orbit around the possessed NPC; the NPC keeps its own proportions; the camera is slightly lower for a child | the `PossessionStarted` event | release, `PossessionEnded` (Mythic exhausted, death, the NPC is dismantled) |
| Cinematic | a scripted glide and push used only for transitions and the tutorial | transitions | ends by itself |

**Smoothness.** A mode change blends focus point, yaw, pitch, distance and FOV with a critically damped spring (0.8 s default, always on `Time.unscaledDeltaTime`, as `FocusOn` does now so it works while paused). Entering a body: the camera pulls up and over to the person's shoulder line; leaving: it lifts back to the pre-possession overhead pose, which is remembered (`ReturnPose`). A transition begins when the event arrives, not when the player clicks, so the camera never gets ahead of the sim. The overhead camera keeps working during B and C through a "peek" key (hold `Space` or middle mouse) that lifts the camera while the body keeps its control lease, so the player can look at the island without leaving the body; releasing returns.

**Obstruction.** The third-person camera shortens its distance when the ground height (`GodCameraRig.GroundY`) or a building bounds block the line to the character, with no sim query.

## 6. Selection, areas, households and institutions

### 6.1 Selection model
`SelectionModel` (view only) holds `Kind` (None, Person, Group, Household, Institution, Object, Area, Location), the ids, a primary item and an anchor point, and raises `Changed`. It generalises `SelectedNpc`, `SelectedBuilding` and `SelectedNode` of the HUD, which become views of it so the existing cards and tests continue.

| Gesture | Result |
|---|---|
| Left click on a person | select (replaces); `PickNpc` radius and priority as now, with figures preferred over buildings in the same screen area |
| Shift or Ctrl click | add or toggle in a group |
| Left drag on empty ground | **box selection**: persons whose drawn position is inside the screen rectangle become a hand-picked group of ids |
| Alt plus left drag, or the Area tool | **area brush**: a circle on the ground, radius set by the wheel (default 20 m, range 6 to 120 m); produces `AreaSpec { CentreNode, RadiusSteps }` in lattice units; the sim resolves who and what is inside at the application tick |
| Double click a person | select their household (`HouseholdId` from the card) |
| `H` | select the household of the primary person |
| Click a building with an institution | select the institution (`OrgId` from the building's owner); a second click selects the building alone |
| Click a resource source or lot | Object selection with its affordances |
| Click ground | Location selection (a node), with a context radial available |
| `Esc` or click empty sky | clear |

### 6.2 Who resolves the scope
- **Person** and **hand-picked group:** explicit id lists. The sim ignores ids that are dead or unknown at the application tick and reports `NotReached` in the result events.
- **Household:** the household id; the sim resolves members at the application tick (a child who moved out is not included, a newborn is).
- **Institution:** the organisation id; the sim delivers the instruction to the relevant officials according to the organisation's roles (D5.2: governments may refuse).
- **Area:** centre node and radius; the sim resolves the persons and objects inside, by sim position, at the application tick. The view shows a ghost of the same circle but no more.
- **Location:** a node id (for "move to", "construct here", "consecrate").
Unity never enumerates the contents of an area, household or institution to build a command. This keeps results independent of the camera and of rendering.

### 6.3 Hit testing without the camera mattering
Picking uses the drawn positions and screen radius only to choose which id the click names. The id is then the only thing sent. Two clicks that name the same id at different zoom levels send identical commands, so picking is not part of the sim's inputs.

## 7. Contextual actions and the command architecture

**Hit result.** `ContextTarget { Kind, Id, Node, Point, Owner, Faction }` built by a `WorldPicker`.

**Catalogue.** The U2 catalogue (there is no sim-side `IDivineCatalog`; PLAYER_UI_DESIGN section 5.2, LEAD_RULINGS R13) returns each action with `Cost`, `Valid` and `Reason` computed with `Gate.Check`. The radial menu, the toolbar tray, the keyboard shortcut layer and the touch bottom sheet are four renderings of this one list; none contains rules.

**Command construction.** `PlayerCommandBuilder` creates `Command` objects (suggest, command, compel, miracle, reveal, resurrect, manifest, possess, release, control input) from intents and ids. It is a pure function of the intent and the current selection ids, so a test can drive it with no Unity. The HUD today does the same thing in `WorldClick`, `ClickPlace` and `Send`; those stay for the stage 3 faction mode.

**Paused play.** Commands issued while paused are checked at once, queued, shown as pending chips on the targets, and applied when time resumes or on Step (PLAYER_UI_DESIGN UI-6).

**Mobile compatibility.** Nothing in the architecture needs a hover, a right button or a keyboard: tooltips become long-press, the radial menu opens on long-press, a floating stick feeds the same `MoveAxis` intent, two-finger gestures feed orbit and zoom. Rendering sizes use `HudScale`. The simulation assumes none of this; it only receives commands.

## 8. Continuous movement becomes deterministic commands (Modes B and C)

This is the part that must be exactly right: a person holding `W` for 2.34 seconds must produce the same replay as the same inputs played back, without the sim ever seeing frame rates or mouse pixels.

### 8.1 Principle
The sim moves a body one lattice edge at a time under its existing movement and action primitives (the same used by NPC intents). The player supplies an intent at tick boundaries; the sim validates and executes it with the entity's own speed, needs and constraints. A possessed NPC keeps the NPC's physical capabilities; an avatar uses the avatar's movement parameters (brief section 6); neither can bypass physical constraints (D5.7 for compulsion applies to physical limits in general).

### 8.2 The control input command
`DivineControlInput` (the name is kept by SAC §19.2; handler in F1's `Control.cs`, registered by D4, which absorbs D5), applied at the tick boundary like every command. **Canonical fields (SAC §10.2, §11.3):** `{ God, Body, Kind (Heading, MoveTo, Stop, Action), Heading, Gait, Node, Act, Target }`, a configurable sector count (default 16), a 3-tick lease, at most one input per body per tick and the last one wins; the table below uses the UI-side spellings (`Entity` is `Body`, `Heading16` is `Heading`, `TargetNode` is `Node`, `Act` is the `Action` kind), and `Seq` is not a contract field (replay order is the command order):

| Field | Meaning |
|---|---|
| `Entity` | the controlled entity (a possessed NPC id or the avatar id); the sim verifies that this god controls it |
| `Seq` | a monotonically increasing sequence per controlled entity; the sim drops out-of-order or duplicate inputs (idempotent replay) |
| `Kind` | `Heading`, `MoveTo`, `Stop`, `Act` |
| `Heading16` | for `Heading`: one of 16 compass sectors relative to the world (north is the board's +Z), a pure integer; with a flag `Walk` or `Run` where the entity can |
| `TargetNode` | for `MoveTo`: a lattice node (click-to-move, tap-to-move) resolved by the sim's existing pathing (`FindRoadPath` and `Pathing`) |
| `Act` | for `Act`: an action id from the existing action primitives plus a target reference (pick up, drop, give, use tool, eat, drink, sleep, gather, construct, gesture, teach) |
| `LeaseTicks` | how long a `Heading` stays valid without renewal (default 3) |

### 8.3 Sampling
A `ControlSampler` (view side) runs once per sim tick, not per frame: it is called from the bootstrap's step loop just before `Sim.Step`, with the latest `PlayerInputFrame` as of that moment.

1. The camera yaw plus the move axis (`W`, `A`, `S`, `D`, stick, tap direction) give a world direction; quantised to the nearest of 16 sectors (22.5 degrees). The quantisation is the only place continuous input becomes discrete, and the result is an integer.
2. If the sector or kind changed since the last submitted input, or the lease would expire next tick, submit one `DivineControlInput` through `Gate.Submit`. Otherwise submit nothing (so a held key sends about 3 commands per second, not 10).
3. When the axis returns to zero, submit `Stop`.
4. Discrete action keys (`E` interact, `G` give, `X` drop, gesture wheel) submit `Act` commands once per press, with the target resolved by `WorldPicker` as an id.
5. While the sim is paused, no ticks run, so no sampling occurs; held keys are ignored on resume until the next tick sample.

### 8.4 What the sim does (contract for D4a and D4b)
- A heading input becomes a short-lived movement intent for the entity: choose the lattice neighbour best matching the sector among passable neighbours, move there at the entity's speed, repeat while the lease is valid. With a `Stop` or an expired lease, finish the current edge (bodies never stop mid-edge in the sim) and halt.
- `MoveTo` paths with the existing pathing and walks until arrival, the next control input, or an interrupt.
- Each input is recorded in the command stream with its tick, so a replay reproduces the exact path.
- The possessed NPC's own policy is suspended for the control slots only while the lease holds; its drives, injuries and perception continue (D5.8). If the NPC's survival constraints force an action the sim defines (for example collapse from exhaustion), the sim's rule applies and the view shows it.
- Inputs that are not physically possible (blocked edge, no reach to the object, no tool) are rejected with a reason; the view shows a small "cannot" cue near the character and a tooltip from `Reasons`, never a fake success.

### 8.5 Latency and feel
Worst-case response is one tick (100 ms) plus the edge time. Figures already interpolate toward their node (`NpcLayer`: exponential follow, rate 10), so motion is smooth. Cosmetic anticipation is allowed and has no sim effect: the controlled figure may turn its body towards the pressed heading immediately, lean, and start its walk cycle, because that does not change the position the sim reports. Camera follow lags by the follow smoothing and never extrapolates.

### 8.6 Why not send positions or velocities
Sending a continuous position or velocity would make the sim depend on frame time and float arithmetic, break determinism across machines, and let the client dictate where bodies are. A heading sector is an integer from the player's choice and a lease is counted in ticks, so the stream is small, exact, replayable, and identical between Unity and `God.Runner`.

### 8.7 Speed interaction and time dilation
Embodiment caps the speed (default 2x, PLAYER_UI_DESIGN section 15), but at any speed the sampler runs once per sim tick; at 10x ten ticks are stepped per 100 ms and the same last-input-wins rule applies for ticks stepped inside one frame (a frame that steps several ticks resubmits the same input only when the lease demands). A deterministic replay never depends on how many ticks were stepped per frame because commands carry the tick they were submitted for.

### 8.8 Avatar and possession differences
| | Possession | Avatar |
|---|---|---|
| Body | the NPC's own, with its needs and limits | a divine body: no hunger or ageing, can be attacked, has its own health |
| Policy | suspended while the lease holds; resumes on release | none; the avatar acts only on inputs |
| Mythic | continuous drain per tick (shown per sim day and as time left) | manifest cost plus upkeep |
| Camera | possession third person | avatar third person |
| Gestures | the NPC's body language, recorded as divinely controlled | the avatar gesture set |
| Commands accepted | `DivineControlInput` for the possessed entity | `DivineControlInput` for the avatar |

## 9. Input maps

| Input | Today | Divine game |
|---|---|---|
| Space | pause | pause (kept); hold in B and C = peek camera |
| `+` `-` | step speed | step speed through `{1, 2, 5, 10}` |
| WASD, arrows | pan | pan (A); move (B and C) |
| Q, E | none | rotate |
| Wheel | zoom | zoom (A); camera distance (B and C) |
| Right drag | pan | pan (A); orbit (B and C) |
| Right click (no drag) | cancel tool | radial menu on a target; cancel if a power is armed |
| Left click | select, place | select; confirm armed power |
| Left drag | none | box select |
| Tab, 1 to 5 | build tabs (watch: faction jumps) | build tabs hidden; `1` to `7` toolbar slots (PLAYER_UI_DESIGN section 6); `Tab` cycles roster |
| R | road tool | free for rotate or pitch |
| F | none | follow selected |
| H | none | household of selection (and `Home`: whole island) |
| Z | none | zoom to individual |
| E | none | interact (B and C) |
| Esc | cancel or menu | cancel, then return to A, then menu |
| F8 | true state | true state (kept) |
| F2, F3, F4, F5, F10 | bootstrap debug | unchanged, dev only |
Conflicts are resolved by `PlayerModel`: the stage 3 faction mode keeps the old bindings.

## 10. Sim-interface rules

| View | Reads | Sends |
|---|---|---|
| CameraDirector | drawn figure positions (`NpcLayer.TryPosition`), avatar and possession state, events for transitions | nothing |
| SelectionModel, WorldPicker | drawn positions, ids from `Sightings`, building and organisation ids | nothing |
| PlayerCommandBuilder | selection ids, the armed power, the U2 catalogue (`Gate.Check`) | commands through `Gate` |
| ControlSampler | `PlayerInputFrame`, camera yaw, the control lease from `PossessionView` or `AvatarView` | `DivineControlInput` once per tick when needed |
| Cursor ghosts | `Gate.Check` results | none |

Rules: no sim state mutates in Unity; commands only through `ICommandGate`; no `Time.deltaTime` in anything that becomes a command; selection and camera never enter the hash; the `ControlSampler` is deterministic given the frame (tests feed frames).

## 11. What exists now versus what is needed

| Capability | Exists | Needed |
|---|---|---|
| Overhead pan, zoom, framing, glide | yes (`GodCameraRig`) | rotation, pitch, close zoom, follow, bands |
| Click selection of one person or building | yes | multi, box, area, household, institution, object |
| Context action menu | no | radial menu from the catalogue |
| Time controls | partial (1x, 2x, 4x) | 1x, 2x, 5x, 10x, accelerated, step (PLAYER_UI_DESIGN) |
| Input abstraction | no | `IInputSource`, `PlayerInputFrame`, `PlayerController` |
| Third-person cameras | no | avatar and possession cameras, transitions |
| Control command and sim side | no | `DivineControlInput`, possession and avatar sim (D4a, D4b), `AvailableActions` |
| Command pending visuals | no | pending chips, response chips |
| Mobile | no | touch adapter (Tier 3) |

## 12. Tier and package mapping

Package ids follow the final [package index](../civdiv/DEVELOPMENT_PACKAGE_INDEX.md) (review 27 Nov 2026): D5 is absorbed into D4 (D4a possession, D4b avatar); U4 into U3 (U3a world, U3b characters and effects); Q2 into Q1; C8's Tier 2 scope into C6 and C5's minimal citizenship into C10; C4, C9, C8 banking and C5 beyond citizenship are deferred to Tier 3; U6 is deferred apart from the scenario files, which belong to Q3.

| Capability | Package | Tier |
|---|---|---|
| Rotation, pitch, close zoom, follow, `CameraDirector` with the overhead mode | U1 | 1 |
| `SelectionModel` with single, group, household, area, institution, object; click selection of anyone | U1 | 1 |
| Input layer (`PlayerInputFrame`, intents) and `PlayerCommandBuilder` | U1 | 1 |
| Contextual radial menu and the pending and response chips | U1/U2 | 1 |
| `ControlSampler` and the control input command contract | U1 with D4 (D4a, D4b) | 2 |
| Avatar and possession third-person cameras, smooth switching, peek | U1 | 2 |
| Mobile touch adapter, bottom sheets, gamepad | U1 | 3 |
| Cinematic transitions and tutorial camera scripts | U6 (deferred, index §0) | 3 |

## 13. PlayMode-testable acceptance hooks

| Hook | Test |
|---|---|
| H-CC-1 | `CameraDirector.Orbit(yawDeg, pitchDeg)`, `Zoom`, `Pan` keep the focus point over the island (`Clamp`) at every yaw; zooming to the minimum distance puts one person's figure height above 25 percent of the screen |
| H-CC-2 | `Follow(npcId)`: after 200 frames of a walking person the focus point is within 1.5 m of `TryPosition`; `Pan` detaches |
| H-CC-3 | Transition: A to C and back finishes within `TransitionSeconds` plus 0.1 s of unscaled time with the game paused; the camera never moves before the matching event |
| H-CC-4 | Selection: box select returns exactly the ids whose drawn position is in the rectangle; area brush sends `AreaSpec` with the radius in lattice steps; the command carries no id list |
| H-CC-5 | Scope resolution: the same area command sent at two camera zoom levels produces identical commands and an identical resulting state hash |
| H-CC-6 | `ControlSampler` fed a scripted held `W` for 2.34 s at frame rates 30, 60 and 144 produces the same command stream (same ticks, same fields) |
| H-CC-7 | A possession run with a recorded control stream replays in the headless runner to the same final hash (the Unity and headless parity requirement) |
| H-CC-8 | A blocked heading, an out-of-reach `Act` and a missing tool each return a reason through `Gate.Check` and show the "cannot" cue; no state changes |
| H-CC-9 | The camera mode, selection and annotations do not change `StateHash` over a 20-day run with random UI actions |
| H-CC-10 | With touch input frames only (scripted), a person can be selected, followed, given a suggestion and moved as an avatar, with no use of right click or hover |

## 14. Risks

| Risk | Mitigation |
|---|---|
| The control stream's latency feels bad at 1x | the one-tick queue, cosmetic anticipation, interpolation; tune `LeaseTicks`; measure `ControlLatencyMs` |
| Heading sectors feel coarse or jittery on a lattice | 16 sectors plus hysteresis (change sector only if the angle moves more than 14 degrees); sim chooses among passable neighbours |
| Orbit camera clips through relief or buildings | ground height clamp and shortening; no sim query |
| Rotation breaks existing pickers that assume a south view | pickers use `WorldToScreenPoint` already (`PickNpc`), so they work at any yaw; verified by H-CC-1 |
| Many control commands flood the replay | submit only on change or lease renewal (about 3 per second held) |
| Rewriting input scatters bugs across three files | do it in one step (`PlayerController`), keep the old `Update` code behind the stage 3 model until tests pass |
| Mobile left late | architecture is device independent from the first commit; only adapters are deferred |

# God of the Island: Unity proof of concept plan (24 Nov 2026)

Branch `unity/poc` (from `main` 42cadf0). It replaces the Godot project with a Unity 6000.3.28f1 project. `main` changes only when the owner says so. Nothing is pushed without asking.

## The game in one paragraph

You are a god looking down on an island. You never control a person directly. You place buildings, and the people build them. You set which buildings get wares first. Raw materials appear on the land when the hidden Catan dice roll a tile's number. Harvesters collect them, and carriers walk them along roads. Workshops turn them into finer wares, or split them into two. Two factories turn wares into soldiers and light vehicles. These leave on their own to attack the enemy buildings you have marked, or to guard the places you have flagged.

## Owner decisions (24 Nov)

| Question | Answer |
|---|---|
| Repo | New branch `unity/poc`. Unity replaces the Godot project. Docs and history stay. |
| Enemy | A static enemy base: pre-placed buildings plus a factory that sends waves. No enemy economy. |
| Logistics | Carriers walk on roads that you lay, Settlers-style. Priorities decide who is served first. |
| Land and dice | The god-board hex island with blended tiles. The dice numbers stay hidden, and richness shows as a larger centrepiece. |
| Placement | **The HQ (warehouse) goes only on a Catan corner.** It opens a round build area whose radius is half the distance between corners, so it never overlaps a neighbouring corner's area. Inside that area, building is Settlers-style on a fine grid. |
| Units | Two types: infantry and a light vehicle. |
| Control | God orders only. You mark enemy buildings as targets and set defend flags, and units choose for themselves. No unit selection. |
| Look | The god-board look is reused: tilted god camera, blended island, buildings in the people's style, small carriers, day and night, and a stripped-down cloud HUD. |

## What the proof of concept must let you do

1. Open the build and see the island from the god view, with your HQ on one corner and the enemy base on a far corner.
2. Place a woodcutter, a quarry and a clay pit inside your build area, and roads to them. Watch builders and carriers build them.
3. See the dice produce goods on the tiles, and harvesters bring them in.
4. Build a sawmill, a kiln, a smelter, a barracks and a vehicle workshop. Set an input priority on them and see deliveries change.
5. Build a second HQ on another corner, to grow the territory.
6. Tap an enemy building and mark it as a target. Units go and attack it.
7. Put a defend flag on one of your places. Some units stay there and fight the enemy waves.
8. Win by destroying the enemy HQ, or lose when your HQ falls.

## Rules for the first version (my defaults; say if any is wrong)

### Land and dice

- The island is 19 hexes, Catan-shaped, dealt fresh each game (the existing `BoardGenerator`).
- Tile kinds:
  - forest gives logs;
  - hills give clay;
  - mountains give stone or ore;
  - fields give grain;
  - desert gives nothing.
- Each tile has a hidden number from 2 to 12.
- The dice roll every 6 seconds of game time.
  - Each tile whose number comes up puts one **pile** of its raw goods at a harvest spot on the tile, inside your build area. Harvesters on that tile collect it.
  - A tile with no harvester holds up to 3 piles. After that, its piles go to waste.
- A 7 does nothing for now.
- Richness shows only through how dense and big the dressing is.
- **Owner, 24 Nov:** the hexes are abstract. The sim stays a random set of hexes, but the island must *look* natural and uneven, with no visible hexagons, circles or rings:
  - terrain borders wander;
  - the coastline is irregular;
  - dressing is scattered naturally.
  
  This is package P4a (`unity/p4-organic-look`).

### Territory and placement

- **HQ:** goes on a corner where three land hexes meet. Catan's distance rule applies: no HQ on a corner next to another HQ.
  - A new HQ must sit within your territory's reach: a corner on the edge of an existing build area.
  - Cost: 4 planks and 4 stone, carried there.
- **Build area:** a circle around the HQ corner, with a radius of half a hex edge (the "no-build zone" between corners), cut into a fine grid of small and medium plots.
  - Plots sit on the parts of the three neighbouring hexes inside the circle. A harvester belongs to the hex its plot is on.
- **Roads:** run between road nodes (flags) on the grid, inside your build areas. Carriers walk them, so a building with no road gets nothing.

### Wares and buildings

| Building | Size | Takes | Gives | Note |
|---|---|---|---|---|
| HQ / warehouse | corner | - | - | Stores everything and holds the carriers and builders. Starts with 8 planks, 6 stone and 4 brick. |
| Woodcutter | small | dice piles on forest | log | harvester |
| Clay pit | small | dice piles on hills | clay | harvester |
| Quarry | small | dice piles on mountains | stone | harvester |
| Mine | small | dice piles on mountains | ore | harvester |
| Farm | medium | dice piles on fields | grain | harvester |
| Sawmill | medium | log | plank + kindling | **split** ware |
| Kiln | medium | clay + kindling | brick | **composite** |
| Smelter | medium | ore + kindling | iron | composite |
| Barracks | medium | grain + iron | 1 infantry | unit factory |
| Vehicle workshop | medium | iron + iron + plank | 1 light vehicle | unit factory |

- **Construction:** each building costs planks, brick or stone, carried to its site. One builder walks out from the HQ.
- **Priority:** tap any building that takes wares and set each input to Stop, Low, Normal or High.
  - Carriers serve requests by priority first, then by distance.
  - A factory can also be paused.

### Military (OpenRA-style feel, god orders only)

| Unit | Speed | Health | Damage | Good against |
|---|---|---|---|---|
| Infantry | slow | low | low, fast fire | infantry |
| Light vehicle | fast | medium | high, slow fire | buildings and vehicles |

- Units are small 3D figures with a small health bar, as in OpenRA.
- **Your orders:**
  - Tap an enemy building and choose **Target**. You can mark several. They are attacked in the order you marked them.
  - Tap your own building, or a spot of land, and choose **Defend**. Give it a number of guards, 0 to 5.
- **How units choose:** a new unit first fills any defend flag short of guards, then joins the attack on the first target.
  - With no target, units wait at the HQ.
  - Any unit fights an enemy that comes within its sight. A guard returns to its flag afterwards.
- **Damage:** a destroyed building leaves rubble. Its plot can be built on again.
- **Enemy base:**
  - an HQ, a barracks, a vehicle workshop and 3 other buildings, pre-placed on a far corner;
  - the first wave after 8 minutes, then one every 2 minutes, starting at 2 infantry and growing by 1 each wave, with a vehicle from wave 3. (The plan first said every 90 seconds. On 24 Nov the smoke run showed that a passive town fell by 6½ minutes.);
  - each wave attacks your nearest building.
- **Win and lose:** destroy the enemy HQ to win. You lose if your HQ falls while you have no other HQ.

### Out of scope for this proof of concept

Enemy AI economy, trading, ports, multiplayer, save and load, sound, tutorials, phones, neural bots, the 7 / robber, specialist workers walking (they stay abstract, DDR-LOG-004), unit selection.

## What we reuse

| From | What | How |
|---|---|---|
| WorldMVP `World.Sim/Core` | seeded `Rng`, commands, events, snapshots, the `WorldSim` tick loop | copy and trim |
| WorldMVP `World.Sim/Board` | `HexCoord`, `BoardTopology`, `BoardGenerator`, `Lattice`, the `PlotLayout` idea | copy; plots rebuilt as a fine grid inside the build circle |
| WorldMVP `World.Sim/Economy/Logistics.cs` | routing and the carrier job model | adapted from corner roads to the plot-grid road nodes |
| WorldMVP `World.Sim/War` | integer seeded damage rules | adapted to ranged OpenRA-style fire |
| WorldMVP `World.Game` | island terrain and blended tiles (`TerrainBuilder`, `HexDressing`), centrepieces by richness, `PeopleBuildings`, `CollectWalkers` (small carriers), `SoldierView` meshes, `BoardSky` and night light, the tilted god camera, `MeshBuilder`/`MaterialFactory`, the shaders, the ledger cards (shrink to fit) | copy; strip god-board-only parts |
| WorldMVP tooling | asmdefs (`Sim` pure C#, `Game` view only), Unity CLI test runs, `dotnet test` mirror of the sim, AutoPlay headless smokes, the Windows build script | copy |
| logistics-rts-sim | design rules (deterministic integer sim; bots use the same commands as people), DDR-LOG decisions (road nodes = flags, per-job carriers, abstract specialists), the units and buildings catalogue as a source of names | rules kept; GDScript code is not ported |

Not reused: anything from Sims 2, rigged characters that need local art, third-party art or audio, and the council, rhythm and cave games.

## Layers (same rule as WorldMVP)

- `Assets/God/Sim` (`God.Sim`): pure C# with no Unity calls. It holds the whole game state, ticks at 10 Hz in integers, and receives commands. It is tested in EditMode and with `dotnet test`.
- `Assets/God/Game` (`God.Game`): views, camera, HUD and input. It reads snapshots and sends commands, and **never changes sim state**.

## Packages and order

The work runs as parallel sub-agents, Sonnet where possible and Opus for the hard parts, each in its own worktree with one Unity editor per folder.

| # | Package | Contents | Depends on |
|---|---|---|---|
| P0 | Project skeleton | Remove `godot_project/` and the Godot scripts. Add the Unity project, asmdefs, copied core, board and render helpers, the test setup and the build script. Update the README. | - |
| P1 | Land, dice, territory | Island deal, hidden numbers, the dice clock, piles, HQ corners and the distance rule, build circles, plot grid, road nodes. EditMode tests. | P0 |
| P2 | Economy | Building defs, construction, harvesters, workshops (split and composite), carriers on roads, priorities and pause. Tests, including a 10-minute headless economy run. | P1 |
| P3 | Military | Units, sight and fire, targets, defend flags with guard counts, the enemy base and waves, rubble, win and lose. Tests. | P1 (P2 only for the factory inputs) |
| P4 | View | God camera, island look, buildings, roads, carriers and piles, units and fire, health bars, day and night. | P0; then it follows P1 to P3 |
| P5 | God HUD | Cloud strip with the build menu, the building card with priority and pause, Target and Defend on tap, a dice tick, and win and lose cards. | P4 |
| P6 | Proof | AutoPlay smoke (build the economy, make 3 units, kill the enemy HQ), the Windows build, a play guide, screenshots for the owner. | all |

P2 and P3 can run at the same time, and so can P4 alongside P1 to P3.

## Done means

- `dotnet test` and the EditMode and PlayMode tests pass, the headless smoke wins against the enemy base, and a Windows build runs.
- The report says plainly what was tested by hand and what was not.

---

# Stage 2: six factions, AI, trade and conquest (25 Nov 2026)

The owner's request after playing the first proof of concept:

> "expand this to a bigger island (big enough for 6 player catan). Lets get the AI working, along with trade between factions. Factions should be able to send trade envoys with offers of goods for trade, if these are accepted the actual goods should be sent between factions. Also the AI should be able to play the game between 6 factions, each starting with one HQ, they should expand to get 10 VPs eventually, with conflict and conquest and units created."

They also couldn't find how to save or quit.

## Owner decisions (25 Nov)

| Question | Answer |
|---|---|
| Victory points | An HQ is 1 VP. An HQ upgraded to a **Citadel** is 2 VP. The first faction to 10 VP wins. |
| Conquest | An HQ brought to zero is **captured**. It and the buildings in its area switch to the attacker, along with their VP. A faction with no HQ left is out. |
| Players | You and 5 AIs. **Watch** mode lets all 6 AIs play each other. |
| Trade | Envoy caravans walk. The envoy walks overland from the offering HQ to the other faction's HQ, carrying the goods on offer. If the offer is accepted, it drops them and walks home with the goods paid in return. If it is refused, it brings the goods back. Units can kill the envoys of factions they are at war with, and the goods are then lost. |

## Rules (my defaults; say if any is wrong)

### Island
- 30 land hexes, as in six-player Catan: rows of 3, 4, 5, 6, 5, 4 and 3.
  - Forest 8, Hills 6, Fields 7, Mountain 7, Desert 2.
  - The 28 number tokens are dealt so that no 6 or 8 touches another.
- The organic look is kept, and the coastline wraps the new shape.
- Each of the 6 factions starts with one HQ and its start stock. The start corners are spread round the island, as far apart as possible, all on legal corners.

### Factions and war
- There are 6 factions, each with its own colour and people's style.
- Each pair of factions is either **at peace** or **at war**. Everyone starts at peace.
- Units fight only factions they are at war with, and walk past everyone else.
- Commands:
  - `DeclareWar(a, b)` makes a pair go to war at once.
  - `OfferPeace(a, b)` makes them peaceful once both sides have offered.
- You can only target buildings of a faction you are at war with.

### VP, Citadel, capture
- A finished HQ is worth 1 VP, and a Citadel is worth 2 VP.
- **Citadel:** upgrade a finished HQ for 3 plank, 4 brick, 4 stone and 3 iron, carried to it. It takes 30 s. It has 3000 health (an HQ has 1500).
- **Capture:**
  - When an HQ or Citadel reaches 0 health from a faction's units, it passes to that faction at 50% health.
  - Everything in its build area passes with it: buildings, sites, flags and the roads wholly inside it.
  - So do the stock and idle carriers in its store.
  - The old owner's walkers inside the area are removed.
- A faction with no HQ is **eliminated**: its units disband and its roads stay as ruins. Ordinary buildings still burn to rubble.
- The game ends when a faction reaches 10 VP. With one faction left standing, it wins anyway.

### Trade
- `OfferTrade(from, to, give[], want[])` sends at most 8 wares each way.
  - The offered goods leave the offering HQ's stock at once.
  - An envoy walker carries them overland to the target's nearest HQ.
- The target answers with `AnswerTrade(accept)`. AIs answer straight away; you get 60 s, with an offer card.
  - **Accepted:** the goods go into the target's store. The wanted goods are taken from the target's stock and carried home by the envoy.
  - **Refused**, or the target lacks the wanted goods: the envoy brings the offer home.
- An envoy killed on the way loses its goods.

### AI (owner rules from the 18 Nov design, adapted)
- Each AI faction runs a bot that uses only commands, like a person.
- **Economy and expansion:** build the chain, expand to new HQ corners towards missing terrain and rich land, and upgrade to a Citadel once the economy runs.
- **Army:** build units, keep guards on its HQs, and mark the targets of factions it is at war with.
- **War:**
  - Declare war on a faction leading by 2 or more VP. If two factions are tied for the lead, the leaders fight each other.
  - Declare war on a weak neighbour when its own army is clearly stronger.
  - Make peace once the reason is gone.
- **Trade:**
  - Trade with anyone who isn't winning, preferring the worst-placed faction.
  - Trade for target builds or missing wares, or to shed surplus.
  - Accept fair offers that help.

### Menu, save, quit
- **Esc** opens a menu: Resume, Save, Load, New island, Watch AIs, Quit.
- Saving writes the whole sim state, plus the bots' state, as JSON to the player's data folder. Loading the same save gives the same state hash.
- A small start card offers **Play** (you are faction 1) or **Watch**. `-godwatch` starts watch mode.

### HUD and views
- All 6 colours and styles appear in the views.
- A small standings list shows VP per faction.
- The enemy card gains Declare war / Offer peace.
- A trade panel lets you send offers.
- An offer card lets you answer incoming envoys.
- Envoys are drawn as a walker with a pack animal.

## Packages

| # | Package | Depends on |
|---|---|---|
| P7 | Six factions: `Faction` becomes 6 ids. The human faction becomes a game setting. 30-hex island, 6 starts, war and peace, VP, Citadel, capture, elimination, 10 VP win. The static enemy base and waves are removed. Minimal view and HUD updates so it compiles and shows 6 colours. | - |
| P8 | Trade: envoys in the sim, offers and answers, killing envoys, HUD trade panel and offer card, envoy model. | P7 |
| P9 | AI: bot for the economy, expansion, Citadels, army and war; a 6-AI headless game reaching 10 VP. | P7 (trade hooks after P8) |
| P10 | Menu and save/load/quit, start card and watch mode, standings, war/peace on the enemy card, 6-faction look. | P7 |
| P11 | Integration: AI trading, balance across seeds, a watch smoke that ends with a winner on the build, play guide. | P8, P9, P10 |

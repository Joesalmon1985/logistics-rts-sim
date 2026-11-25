# How to play the proof of concept

Run `Builds\Windows\GodOfTheIsland.exe`. To get a different island, add `-godseed 1234`. Add `-godwatch` to watch with no human faction (the build tools are hidden).

Stage 2 (P7, 25 Nov): the island has 30 hexes and **six factions** (Amber, Azure, Crimson, Jade, Violet and Ivory), each starting with one HQ, spread round the coast. You play Amber. Everyone starts at peace. There are no enemy waves any more. Since P9 the other five factions are played by an **AI** from the start: they build their economy with roads, expand to new HQ corners, upgrade Citadels, raise armies, guard their HQs, go to war and capture HQs. They declare war on a faction that leads by 2 VP or more (tied leaders fight each other), and on a weak neighbour when their army is clearly stronger, and make peace once the reason is gone. **F2** still starts a chain bot for your own economy. `-godwatch` gives all six factions an AI; `-godnoai` starts a game with none.

**Victory points:** a finished HQ is worth 1 VP and a Citadel 2 VP. The first faction to **10 VP** wins; so does the last faction left. A faction with no HQ is out.

## Controls

| Do | How |
|---|---|
| Move the view | Right-drag or middle-drag, or WASD or the arrow keys |
| Zoom | Mouse wheel |
| Pause | Space, or the Play/Pause button on the cloud |
| Speed | `+` and `-`, or the 1x / 2x / 4x buttons |
| Cancel a tool | Right-click or Esc |

## Build

1. Pick a building on the cloud strip at the top. Hover a button to see its cost and what it takes and gives.
2. The spots where it can go glow. Click one. Hold Shift to place several.
3. **Join it to a road.** Pick **Road**, then click a flag (your HQ has one). Then click where the road should go; it finds its own path. Finish by clicking another flag or a building's flag, or click the free end again to plant a new flag there.
4. Builders and carriers walk the materials out. Nothing gets built without a road.

**Where things can go:**
- Woodcutter, clay pit, quarry and mine must stand on forest, hills or mountain land. Each one collects the goods that pile up at the middle of that land when its hidden number is rolled.
- The farm needs field land.
- A new **HQ** can only go on the glowing corners, two steps from an HQ you already have. It gives you a new building area.

## The chain

| Building | Takes | Gives |
|---|---|---|
| Sawmill | log | plank and kindling |
| Kiln | clay and kindling | brick |
| Smelter | ore and kindling | iron |
| Barracks | grain and iron | a soldier |
| Vehicle workshop | 2 iron and a plank | a war wagon |

## Priorities

Click one of your buildings to open its card:
- For each ware it takes, set **Stop / Low / Norm / High**. Carriers serve High first.
- **Pause** stops it.
- **Defend here** keeps 0 to 5 guards at it.
- **Demolish** removes it.

## War and peace

- Click a building of another faction to open its card. At peace, it offers **Declare war**; war starts at once.
- At war, press **Target**. Units attack targets in the order you marked them. **Offer peace** ends the war once they offer too.
- Units fight only factions you are at war with and walk past everyone else.
- New units first fill any defend spots, then go after the first target.
- An HQ brought to zero by units is **captured**, at half health, with every building, site, flag and road in its area and its stock. Other buildings burn.
- Click your HQ for **Upgrade to Citadel** (3 plank, 4 brick, 4 stone, 3 iron; 30 s of work).
- You can't move units yourself; you only give god orders.
- The line under the cloud shows your soldiers, wagons and guards and who you are at war with; below it, the VP of every faction.

## A good start

1. Build a woodcutter, a quarry and a sawmill near the HQ, and give each one a road.
2. Add a clay pit and a kiln for bricks, which the medium buildings need.
3. Add a mine, a smelter and a farm, then a barracks.
4. Click your HQ and set **Defend here** to 2 or 3. Soldiers from the barracks will then guard it.

## Not in yet

- No trade yet (P8); the AI's trade hooks are empty until P11.
- No saving (P10).
- No sound.
- No tutorial.
- Unit selection isn't planned.

Balance is untested by hand. On 10 of the 16 test islands, the start area lacks a terrain the chain needs, so you'll have to build a second HQ quickly.

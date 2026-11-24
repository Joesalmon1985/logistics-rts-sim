# How to play the proof of concept

Run `Builds\Windows\GodOfTheIsland.exe`. To get a different island, add `-godseed 1234`.

You and the enemy each start with an HQ on opposite sides of the island. The enemy's first wave comes after **8 minutes**. After that a wave comes every 2 minutes, and each one is bigger. **You win by destroying the enemy HQ, and you lose if your last HQ falls.**

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

## War

- Click an enemy building and press **Target**. Units attack targets in the order you marked them.
- New units first fill any defend spots, then go after the first target.
- You can't move units yourself; you only give god orders.
- The line under the cloud shows your soldiers, wagons and guards, and when the next wave comes.

## A good first 8 minutes

1. Build a woodcutter, a quarry and a sawmill near the HQ, and give each one a road.
2. Add a clay pit and a kiln for bricks, which the medium buildings need.
3. Add a mine, a smelter and a farm, then a barracks.
4. Before the first wave, click your HQ and set **Defend here** to 2 or 3. Soldiers from the barracks will then guard it.

## Not in yet

- No enemy economy.
- No saving.
- No sound.
- No tutorial.
- Unit selection isn't planned.

Balance is untested by hand. On 10 of the 16 test islands, the start area lacks a terrain the chain needs, so you'll have to build a second HQ quickly.

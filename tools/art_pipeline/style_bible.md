# Settler MVP Art Style Bible

## Core identity

- Cosy logistics colony game on a hex map
- Mobile-readable at small sizes (48–96 px)
- Slightly top-down / three-quarter view (readable depth without heavy perspective)
- Hand-painted, toy-like strategy-board aesthetic
- Clear silhouette and strong value separation
- Consistent warm daylight lighting from upper-left
- Consistent scale across buildings, workers, terrain, and wares

## Hard constraints (every sprite)

- No text, letters, numbers, or UI labels in the image
- No watermark, signature, or border frame
- No extra objects outside the sprite subject (no ground plate, shadow card, or scene backdrop)
- Single subject centered; transparent-friendly composition after postprocess
- Square canvas; subject fills most of the frame with modest padding

## Category notes

### Terrain tiles (grass, water, forest, road)

- Designed as tileable hex-friendly squares
- Pattern can repeat at edges; avoid strong directional features that break tiling
- Readable terrain type at a glance on a phone screen
- Slightly stylised, not photorealistic

### Buildings (headquarters, woodcutter hut, sawmill, quarry)

- 96 px target; chunky readable shapes
- Distinct roof silhouettes and accent colours per building type
- Show function through props (logs, saw, stone blocks) without clutter
- Foundation implied but no full ground slab

### Logistics units (carrier)

- 48 px target; largest readable head-and-body silhouette
- Friendly worker figure; no weapons
- Idle pose only for first pass (move loops are out of scope)

### Wares (timber log)

- 64 px target; single resource object
- Simple iconic shape; high contrast against map backgrounds

## Colour and rendering

- Warm, inviting palette; avoid neon or muddy grey-brown everything
- Soft painted edges; limited fine noise that disappears at game size
- Avoid extreme perspective foreshortening

## What to avoid

- Photorealism, gritty realism, or cinematic lighting
- Dark silhouettes that vanish on forest/grass tiles
- Copying protected game or franchise visual identity
- Naming or imitating specific commercial titles or living artists in prompts

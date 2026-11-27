# Visual world design (stage 4, divine player)

Status: design, 26 Nov 2026. Owner decisions: D5.18 (Catan grid hidden in normal play), D5.19 (fine nodes developer-visible only), D5.20 (seamless, natural island, consistent with the sim), D5.21 (stylised low-poly characters), D5.9 and D5.11 (avatar and miracles shown truthfully). Brief sections 25, 27 and 29. Packages: U3 (terrain), U4 (characters, animation, carried objects), U2 (divine effect hooks), with D1 to D7 supplying the events. Sister documents: [PLAYER_UI_DESIGN.md](PLAYER_UI_DESIGN.md), [CAMERA_AND_CONTROLS_DESIGN.md](CAMERA_AND_CONTROLS_DESIGN.md), [NPC_ANIMATION_DESIGN.md](NPC_ANIMATION_DESIGN.md), [GAMEPLAY_SCENARIOS.md](GAMEPLAY_SCENARIOS.md).

## 1. Purpose and scope

How the island, its settlements, its people and the divine interventions look, so that the player reads one continuous natural world, can tell what every person is doing and what has happened to them, and can tell a suggestion from a command, compulsion, possession or physical miracle without text.

All art is procedural and original. No third-party art or audio, and nothing is copied from `Assets/Bedsit`, `Assets/Characters`, `Assets/RomanVillage` or `Assets/ThirdParty` (CLAUDE.md). The view reads the sim and never decides anything.

## 2. Current state (what the organic look already provides)

The "P4a organic island" look already exists and is the default; this design extends it. Paths are under `Assets/God/Game`.

| Feature (brief section 25) | Present now | Where |
|---|---|---|
| Hexes hidden | Default boot builds `IslandView`; the old hex look (`HexDressingView`) is behind `legacyHexLook` or `-godhexlook`. Terrain "never shows a hard outline or a flat colour" | `Boot/GodBootstrap.cs`, `Island/IslandView.cs` |
| Seamless ground | One continuous ground and sea mesh sampled from `IslandField` on a 3 m grid in 48-cell chunks, with smooth normals and vertex colours; far sea fades to haze | `Island/IslandGroundMesh.cs` |
| Terrain transitions | Every point belongs to the hex nearest a domain-warped copy of the point (`WarpAmp` 26 m), blends with the runner-up over 7 m (`BlendWidth`), so borders wander. `IslandGround` has a Gaussian multi-hex colour blend (`BlendSigma`, `Warp`) and noise-patched colour variation | `Island/IslandField.cs`, `View/IslandGround.cs` |
| Coastline | Land is the hex union pushed in or out by low noise: bays and headlands; sea level `SeaY` -0.35 m; sand and wet-sand colours | `IslandField`, `IslandGround` |
| Forest, hills, mountains, fields | `IslandDressing.Scatter(field)` deterministically scatters 13 prop kinds (conifer, broadleaf, bush, crop, hedge, haystack, mound, clay pit, peak, rock, scrub, dry grass, dead tree) over each region; richer hexes get denser and bigger dressing; clearing left at hex centres; items near buildings, flags and roads are culled (`Refresh`), light inside build circles | `Island/IslandDressing.cs`, `Island/IslandView.cs` |
| Relief | gentle heights (`TerrainHeight`, `Height`), flat where build circles can be (`CornerFlat` 32 m); walkable board is flat at `BoardSpace.GroundY` 0 and relief outside it is for shading and rim | `IslandField`, `Board/BoardSpace.cs` |
| Curving roads | Road polylines are smoothed twice with Chaikin, resampled at 1.4 m, rendered as a ribbon with a gentle meander and worn patches at flags | `Models/GroundLayer.cs` (`RebuildRoads`, `Track`) |
| Buildings | Procedural models per kind, faction accents, stage 3 additions, Citadel | `Models/BuildingModels*.cs`, `BuildingLayer.cs` |
| Dice and grid cues | the dice tick and economy debug text are switched off by the HUD; the P1 node and circle overlay is a debug flag (`TerritoryView.ShowDebugOverlay`) | `GodHud.cs`, `View/TerritoryView.cs` |
| People | Procedural low-poly figure: box torso, head, cap in the faction colour, legs and arms on pivots, about 2 m tall scaled by 1.1; load carried on the head; axe in hand; poses for eating, working, resting, sleeping; scale by life stage (child 0.5 to 1 by growth, elder 0.93 and stooped, expecting mother 1.08 wider); begging child bobs | `Models/PeopleModels.cs`, `NpcLayer.cs`, `LifeStageLayer.cs` |
| Gestures | an arm swing, a coloured line to the addressee, and the composed message as text above the head | `Models/GestureLayer.cs` |
| Effects | `FxLayer`: smoke and dust puffs, tracers, impact flashes, health bars (two dynamic meshes, one draw each) | `Models/FxLayer.cs` |

**Gaps against the brief:**
1. Still visible or hex-derived in normal play: the producing-hex glow (`IslandView.Pulse`), the dice-tied richness cue (denser dressing by pips, which is itself consistent with resources and is kept), and the stage 3 build glow. The producing-hex glow must be dev-only (section 4).
2. Terrain transitions blend colour but the dressing of a region ends at its own warped border; forest edges need a soft density ramp (section 6).
3. No distinction of settlements from scattered buildings: no village paving, fences, fields tied to households, wells, or memorial ground.
4. No visible mountains beyond `Peak` props; relief is mostly normals. Real height needs the ground heights to be real above the board's flat walkable layer (section 6).
5. Characters: only faction colour, life stage and a carried ware; no injury, illness, fatigue, mood, pregnancy beyond width, equipment beyond the axe, occupation looks, names or divine effects.
6. No divine effects exist.

## 3. Decisions

| Id | Decision |
|---|---|
| VW-1 | **Presentation is derived from sim data, never the other way.** Every rendered feature that implies accessibility or a resource (forest, field, quarry, water, road, clearing) is computed from `GodState` (terrain, resource stocks, source kinds, roads, ownership), and a test compares the render with the sim. |
| VW-2 | **The Catan hexes and the fine node lattice are dev overlays** (`DevOverlays.HexRegions`, `DevOverlays.Nodes`), off by default, available only where the truth channel is (PLAYER_UI_DESIGN section 12). |
| VW-3 | **Authoritative pathfinding, terrain rules and node spacing are not changed to disguise the grid.** Smoothness is only in how things are drawn and how figures interpolate. |
| VW-4 | **Keep the P4a pipeline** (`IslandField` as pure functions of board and seed, `IslandGroundMesh`, `IslandDressing`) and extend it with a `ResourceSkin` (section 5) that reads sim state. |
| VW-5 | **Characters stay low-poly and procedural, extended not replaced.** The existing rig (`Body`, `LegL`, `LegR`, `ArmL`, `ArmR`) gains `Head` and `Neck` pivots and optional accessory slots; no skinned meshes. |
| VW-6 | **Divine effects are readable by silhouette, motion and colour together, never colour alone** (section 9), and every effect plays from a sim event. |
| VW-7 | **Level of detail changes rendering only.** An NPC exists and acts the same whether seen or not (brief section 48). Figures far away become dots with aggregate markers; their cues are still computed, cheaply, from events. |
| VW-8 | **Procedural names are a sim fact;** the view only displays them (section 13). |
| VW-9 | **Original colour scheme and shape language** (rings, rays, spirals, chevrons) for divine effects; no imitation of any existing game's iconography. |

## 4. Hiding the grid and the settings that control it

- **Normal play:** `IslandView` only. `HexDressingView`, `TerritoryView.ShowDebugOverlay`, the producing-hex `Pulse`, the dice tick and the hex-number text are all off. `IslandView.Pulse` is wired to a `DevOverlays.ProductionPulse` flag.
- **Placement and legality cues (stage 3 faction mode only):** the soft glow on legal spots remains but is a soft organic blot, not a hex or circle.
- **Developer overlays** (PLAYER_UI_DESIGN section 19): `HexRegions` (outlines, ids, terrain, dice number), `Nodes` (the 20 m flags and the fine lattice, passable or blocked, with cost), `Accessibility` (heat of path cost), `Ownership` (tint by owner). The lattice overlay already exists as `TerritoryView.ShowDebugOverlay`; the hex overlay is a thin new view of `Board`.
- **Seam checks:** an automated screenshot test in the Unity editor renders four fixed views (whole island, mid zoom, settlement, coast) and fails if a long straight edge or a region-boundary colour step above a threshold is found along the hex borders (section 16).

## 5. Terrain, transitions and resource areas derived from the sim

**Principle.** `IslandField` stays a pure function of `(board, seed, x, z)`. A new `ResourceSkin` layer is a pure function of `(IslandField, GodState)` sampled on the same grid and re-sampled in the dirty chunks when sim state changes (`Signature` as `IslandView.RefreshNow` already does for dressing).

| Sim data | Visual consequence | Consistency rule |
|---|---|---|
| Terrain per region (`Board.HexTerrain`) | base biome colour, relief amplitude, dressing family | unchanged from P4a |
| Richness (dice pips) | density and size of dressing | kept; remains abstract |
| Resource source stocks (berries, roots, fish, nightshade, wood stands, ore, clay) | the source's centrepiece shrinks with stock (already done for bushes in `NpcLayer`), forest cover thins where wood is cut, a pit widens where clay is dug, a quarry face grows | rendered quantity is monotone in the sim stock and equals zero when depleted |
| Depleted or regrowing land | bare patch, then scrub, then regrowth over the sim's regeneration time | the sim's regeneration timer drives the stage |
| Cultivated land (fields owned or worked by a household or enterprise) | furrowed field patches whose outline follows the owned or worked area, with a distinct colour and fence segments for private or communal land; harvest stage from crop state | outlines come from sim land records, not from hexes |
| Roads and paths | curved ribbons (existing), widening with traffic counts and with maintenance | width from the sim's per-edge traffic and road class |
| Water and coast | shoreline from the field, shallows, river or fishing spots from source kinds | fishing spots sit where the sim says water resources are |
| Blocked or impassable areas (cliff, deep water, dense forest if the sim has a movement cost) | rendered as such: steep rock, water, dense trunks | the movement cost the sim uses is the one shown, so a visible clear meadow is never impassable and a visible cliff is never a path |

**Seamless transitions.** The existing blend width (7 m) is kept for ground colour. Added: a second, wider noise-modulated **ecotone** band (default 18 m, `EcotoneWidth` configurable) for dressing, where two neighbouring families mix by probability so that forest gives way to meadow through scattered trees and bushes. Mountain to hill to meadow uses a height ramp, not a seam.

## 6. Forests, hills, mountains, coasts

- **Irregular forest boundaries.** The probability of a tree at a point is `density(biome) * smoothstep(edge distance)`, where the edge distance is measured to the noise-warped border (the same warp as `IslandField`). Cluster noise (two octaves, 20 m and 6 m) makes groves and clearings. Result: fringes of single trees, bushes, then forest, with glades.
- **Hills and mountains.** Real heights above the walkable board: today the board is flat and relief is shading plus the rim (`IslandGround`, `IslandField.Height`). The design raises the *rendered* ground in hill and mountain regions, outside the nodes where people and buildings stand, and blends back to flat where `CornerFlat` and node spacing require. Where nodes are on slopes the sim has no height, so figures and buildings are placed at the rendered height by sampling `IslandField.Height` (`Rig.GroundY` is already the hook), a visual lift only. Peaks keep the current `Peak` props on top of the heightfield, plus scree and snow-line tinting by height.
- **Coast.** Wave foam band that follows the coast contour, a wet-sand strip, rocks, reeds at river mouths, a gentle animated shoreline in a shader (no textures) and a darker deep sea.
- **Rivers and lakes** are a later extension if the sim gets water bodies; until then fishing spots are in the sea.

## 7. Roads, fields, decoration

- **Roads:** keep the Chaikin plus meander ribbon, add junction blending (worn disc at junctions), edge grass, and ruts that darken with traffic. The sim's road graph is authoritative; only the drawn curve differs from the straight node line by at most half a node spacing (10 m), so a person walking the road always stays within the ribbon.
- **Procedural decoration:** wild flowers, fallen logs, boulders, reeds, mushrooms, birds as slow dots over forest at high zoom (view-only, deterministic from seed). Decoration density scales with the LOD of section 14.
- **Fields:** see section 5; crop heights change with the sim's growth stage and show harvested stubble.

## 8. Settlements and buildings

- **Recognisable settlements.** A settlement is a connected cluster of houses (huts and later houses), roads, a store, a well or fire pit, and fields. The view derives clusters from sim data (`HutBuilt`, households and their homes, `Territory` buildings, organisation buildings) by a pure function `SettlementClusters(state)` with a stable naming (a procedural settlement name drawn from the same syllable tables as people, seeded by the cluster's first building id). A cluster gets: trampled earth, a low fence or hedge ring where land is owned, lanterns or smoke from chimneys at dusk, laundry lines and children's play spots as small props that appear only when the sim shows children and households.
- **Visually distinct buildings.** Each building kind keeps a unique silhouette and a signature prop: hut (rounded thatch), store (tall gable and sacks), workshop (open side and anvil or bench), clinic (cross-free, a hanging herb bundle and a bed outline), school (circle of seats or a bench semicircle), shrine or temple (raised dais, standing stones, a light-gathering rune ring), court (a raised bench and a gavel post), barracks (rack and banner), bank (strong box with a pillar). Owner or institution identity shows as an accent colour and a small banner shape that is not the Catan faction colour alone.
- **State of the building** shows: under construction (scaffold, growing), damaged (cracks, missing roof pieces, from the sim building health), burning or smitten (scorch, collapse), abandoned (overgrown), active (smoke, lit windows when staffed). Staffing is read from the sim (workers present), never faked.
- **Interiors** are not modelled; sleeping persons are hidden inside (existing), with a soft light in the window.

## 9. Divine visual identity (brief section 29)

One family, distinct per intervention type. Every effect is a pooled procedural mesh and particle set built in `FxLayer` style (dynamic meshes, no textures needed).

### 9.1 Identity matrix

| Type | Silhouette (shape) | Colour | Motion | Duration | Origin and target |
|---|---|---|---|---|---|
| **Suggestion** | a ring of small dotted motes (a halo of 6 to 8 points) around the head | pale blue-white, low opacity | motes drift down from above and orbit slowly, no beam | 2 s | from the sky straight above the person; the person looks up |
| **Command** | a single narrow beam of light with a chevron ring on the ground pointing at the commanded target (a place, person or object) | gold | beam pulses once, the chevron points and stays for the instruction's first moments | 2.5 s then a small ground chevron stays while the instruction is open | beam on the person; chevron line towards the target |
| **Compulsion** | a thread or chain of light from the sky to the head, and a yoke ring round the shoulders that stays while active, with a shrinking arc showing time left | violet-red | the thread is taut and vibrates; the figure's gait gets a stiff, driven look (animation modifier in NPC_ANIMATION_DESIGN) | persistent until the compulsion ends; releases with a snap and fading ring | the yoke stays on the person; a tether line to the required target while moving |
| **Possession** | a full-body rim light and a rotating glyph circle at the feet; eyes glow; camera transition (CAMERA_AND_CONTROLS_DESIGN) | the god's own colour (a god has a persistent identity colour) | the rim pulses with the Mythic upkeep rhythm; a faint second shadow | persistent while possessed; end with a burst of motes leaving upwards | on the possessed body only |
| **Avatar manifestation** | a rising column of light that condenses into a distinct tall luminous figure with robes and a crown ring; the avatar has a unique model, never the NPC figure | the god's colour, white core | a ground ripple and rising motes; footsteps leave glowing prints that fade; destruction: collapse into light that rises | manifest 3 s; persistent presence; unmanifest 2 s | at the sim-chosen manifestation node |
| **Heal** | an upward spiral of green-white motes and a soft ring; wounds fade on the body (injury tint and bandage meshes dissolve) | green-white | rising, slowing | 3 s | on the target, matching the genuine health change |
| **Bless** | golden sparkles and a brief halo; a persistent small star-ring while the blessing is active | warm gold | slow twinkle | 3 s, then persistent marker | person or place |
| **Protect** | a translucent dome or ring ward that stays while the sim protection lasts | cool white-blue | gentle shimmer | persistent | person |
| **Smite** | a jagged lightning bolt from above, flash, shock ring, scorch decal, smoke; trees and buildings burn, break, or collapse according to the sim effect | white-violet bolt, orange fire | instant strike, then fire spreads only as far as the sim says | 0.6 s strike, then the aftermath is real state | person, building or tree |
| **Create resources** | motes converge from the sky and condense into the item lot, with a circular ring; the lot appears as a physical object | green-gold | converging spiral then a soft flash when the lot exists | 2.5 s | the sim lot position |
| **Destroy resources** | the object dissolves into drifting motes that rise | grey-violet | dissolve, upward drift | 2 s | at the lot |
| **Resurrection** | a light column from the grave marker, the figure forming from motes lying down then standing; variants differ by brightness and pose (lesser: weak, kneeling, dim) | white-gold | assemble then breath | 4 s | at the death site or the sim's placement node |
| **Consecration (sacred place)** | a persistent low glow ring, standing-light posts, a pale ground sigil; people kneel or lower voices (animations) near it | white-gold | slow breathing glow | persistent | the consecrated area from sim state |
| **Revelation of a commandment or knowledge** | a floating glyph tablet whose icon shows the commandment's category (open hand, crossed fist, tree, child, bowl); it enters the receiver's head; when a person tells another, a small glyph travels between them | gold-white | descend, rotate once, absorb | 3 s | receiver; later glyphs follow `CommandmentCommunicated` |
| **Prayer** | a thin ascending thread of motes from a kneeling person, many threads merge into a column over a temple | soft white | steady upward | while praying | at the praying NPC; aggregated at distance |

### 9.2 Readability rules
1. **Five core types are separable by three channels at once:** suggestion = dotted ring + pale blue + drifting; command = beam + chevron + gold; compulsion = chain and yoke + violet-red + taut and stiff; possession = body rim and ground glyph + god colour + pulsing; miracle = a sudden physical event (bolt, spiral, condense) + a changed world. Test: a viewer can identify each from a still frame (the screenshot test of section 16 compares silhouettes).
2. **Colour is never the only cue,** so the identity also works for colour-vision deficiency: shape and motion differ, and the HUD tags the same glyphs.
3. **Duration tells state:** brief for one-shot (suggestion, command, bolt), persistent for state (compulsion yoke, possession rim, protection dome, consecration), so the world shows who is currently affected at a glance.
4. **Aggregation at distance:** from the whole-island zoom, persistent states collapse into small coloured markers by type with counts; prayer threads merge into columns.
5. **Mortal perception is separate from the player's view.** The effect is drawn for the god. Whether NPCs perceive it is a sim result: the effect carries `PerceivedByMortals` from the event, and only perceivable effects (a bolt, the avatar, a miracle in sight) trigger the NPC reaction animations (fear, praying, gratitude, submission, flee). A suggestion or compulsion is not seen by witnesses unless the sim says so; the player may still see its markers.
6. **Truth in effects.** An effect never plays when the sim refused. A suggestion that is refused plays the delivery and then the NPC's own refusal animation (a head shake, a turn away) from the response event; the effect never claims the result.
7. **No text** in any effect; numbers appear only in the Mythic meter and cost chips.
8. **Budget:** at most 24 simultaneous detailed effects; the rest become markers; effect meshes are pooled (`FxLayer.Ensure`).

### 9.3 God identity colour
Each god has a persistent identity colour and a sigil shape (ring, sun, wave, tree, flame, eye, star), derived deterministically from the god id by a pure function, shown in effects, the Mythic meter, the avatar, the possession rim, sacred places and sigils on temples. Rival gods use different colours; the human god is not special (D5.26).

## 10. Characters (brief section 27)

The existing figure stays the base. Everything below is a rendering of data read from the sim.

| Look | Sim source | Rendering |
|---|---|---|
| **Age and development** | `Stage`, `Growth`, `AgeMilli`, `Life` | child scale 0.5 to 1.0 with larger head ratio; adolescent proportions between; adult 1.0; elder 0.93, stooped, grey hair, walking stick when walking. Existing `LifeStageLayer` extended with head ratio and hair colour by age |
| **Pregnancy** | `PregFather`, pregnancy events | belly bulge growing with the sim pregnancy progress (not the 1.08 uniform widening); hand to back when idle |
| **Injury** | `Injury`, `InjuryTaken` | limp (a leg lag in gait), arm sling, bandage boxes on the injured side with a red tint, blood-free stylised marks |
| **Illness** | `IllnessStarted/Ended` | green-grey skin tint, slumped posture, a small cough animation, sparse steam-like sweat motes |
| **Fatigue** | tiredness drive | slower, heavier gait, drooped head, yawn pose at high values |
| **Emotion** | drives (fear, pain), the learned reward trend, recent gestures | posture and face-less cues: shoulders up (fear), head down (sad), bounce (pleased), clenched arms (angry); a very small mood colour on the cap rim |
| **Hunger and cold** | drives | hands to belly, arms folded, shiver |
| **Equipment** | carried tools and gear lots | axe, pick, shovel, sickle, hammer, spear (existing `ToolKind`), knife, basket, water pot; belt pouch; armour pieces and shield for soldiers (existing `UnitModels` kit reused by reference of ideas, not assets) |
| **Occupation through activity and possessions** | current intent, tool in hand, last job, workplace | a woodcutter carries an axe and bark dust; a farmer a sickle and straw hat; a healer a herb pouch and apron; a teacher a staff with a mark; a priest a robe sash. Looks follow what the person carries and does, not a role label, because roles are held by individuals, not unit types (brief section 3) |
| **Carried goods** | `Carry`, lots | head load (labour, existing), in hands (display or giving), on back (bundles), in arms (infant, fragile), slung pouch; the shown ware mesh is the real ware from `LoadMesh`; count shown as stack size up to 4 |
| **Divine effects** | divine events | section 9 |
| **Faction identity** | faction | cap and sleeve accent colour as now; clothing variation by household, so households read as families: shared accent stripe |
| **Name and selection** | `NpcName` | name plate only on hover, selection ring and a followed star above the head; no permanent floating text |

**Rig changes** (`Models/PeopleModels.cs`, `NpcLayer.Fig`): split the head from the torso mesh into a `Head` pivot at neck height 1.5 so nods, bows and head shakes are possible; add `Back` and `Hip` accessory slots; add a pooled `Bandage`/`Sling` child set toggled by injury; keep the part names stable so `GestureLayer` tests and the pose code survive.

## 11. Sim-interface rules and the divine event feed

**What the view reads.** `GodState` (population, economy, territory, board, goods) through the existing views; the divine observation view (PLAYER_UI_DESIGN section 5); and the event stream from `GodBootstrap.SimEventRaised`.

**Provisional divine events the effects bind to** (final names from the divine packages and SHARED_ARCHITECTURAL_CONTRACTS):

| Event (provisional) | Effect and animation |
|---|---|
| `DivineSuggestionDelivered { God, Npc, Action }` | suggestion halo on `Npc`; the person looks up (then its own response event shows the reaction) |
| `DivineCommandIssued { God, Target, Action, Scope }` | beam and chevron on each recipient the sim reports; group: one beam each up to a cap, then a district ring |
| `CompulsionStarted` / `CompulsionEnded { Reason }` | yoke and tether; end effect by reason (completed: release; timeout: fade; cancelled; death) |
| `PossessionStarted` / `PossessionEnded { Reason }` | rim, glyph and camera transition; end burst |
| `AvatarManifested` / `AvatarDismissed` / `AvatarDestroyed` | column of light, unmanifest, collapse |
| `MiracleWrought { Kind, Target, Amount }` plus the authoritative state events (`InjuryHealed`, `ItemMoved` creation lots, `BuildingDestroyed`) | the matching miracle effect; the changed state arrives in its own events so the view never fakes the result |
| `NpcResurrected { Npc, Variant }` | resurrection column and the person's reborn pose |
| `CommandmentRevealed` / `Learned` / `Communicated` / `Revised` / `Revoked` | glyph tablet; travelling glyph between speaker and listener |
| `PrayerMade { Npc, God }` | prayer thread |
| `PlaceConsecrated` / `Deconsecrated` | sacred ground ring on or off |
| `MythicChanged { God, Delta, Reason }` | meter animation and cost chips (UI only) |
| `DivineResponse { Npc, Accepted, Refused, Ignored }` | the NPC's own animation of the answer (NPC_ANIMATION_DESIGN) |

**Rules.**
1. An effect is a deterministic function of an event and the sim state at that event; the same events produce the same effects (a pure `DivineFxPlanner` builds effect records, headless-testable).
2. An effect that has no matching event is a bug; the test of section 16 counts effects per event.
3. The view never reads `Honest`, `Mismatch` or other truth-only flags to choose an effect.
4. Effects use wall-clock animation time; none of it enters the sim or the hash.
5. Pooling: effect records are structs in a ring buffer; replaying a recorded game reproduces the same records.

## 12. Memorials and sacred places in the world

- **Memorial markers.** On `NpcDied`, a small cairn or standing stone appears at the death place or, for a settled person, at the settlement's memorial ground, with a count bar for multiple people. Markers persist (bounded by LOD: old ones merge into a field of stones). Click opens the memorial view (PLAYER_UI_DESIGN section 21).
- **Sacred places** (consecrated, shrines, temples) have standing posts, a faint ring and a soft light at night; they are part of the settlement derivation.
- **Resurrected people** step out of the marker's light; the marker remains with a "returned" glyph.

## 13. Procedural names

A sim-owned deterministic function `NpcNames.Generate(seed, lineage, rngStream)` (F1/population) makes a given name from a table of original syllables (consonant-vowel pairs, per-faction phonetic flavour) and a family name inherited through a configurable rule (matrilineal, patrilineal or compound); the same rule gives settlements, places and gods their names with different flavours. Names have a collision budget (unique within a world where practical, disambiguated by generation numerals in the UI only if a collision happens). The Game layer has a pure fallback `NameOf(id, seed)` used until the sim exists, so figures, cards and alerts already read the same string interface. The player alias is a UI annotation (PLAYER_UI_DESIGN section 13).

## 14. Level of detail and performance (brief section 48)

| Distance band | Terrain | Figures | Cues and effects |
|---|---|---|---|
| Whole island | merged chunks, dressing as density blobs | dots by faction or household colour, aggregate crowd markers, followed people as stars | persistent states as coloured markers; one-shots suppressed except critical ones |
| Mid (settlement) | full dressing, roads | simplified figures (torso, head, arms as boxes), pose subset | effects at reduced particle count; gestures as an arm raise and a silhouette symbol |
| Close | full detail | full rig, accessories, carried goods, gaze | all effects and the full vocabulary |

LOD is a function of camera distance only for what is drawn. Cues are produced from events whether or not anything is in view, in a ring buffer of the last 10 seconds of view time so that zooming in shows the current phase. The sim never reads the camera. Budgets: 1000 figures drawn as one instanced mesh per LOD tier at far range, up to 150 full rigs at close range, dressing culling in 10 m buckets (existing), a settable `DetailScale`.

## 15. Tier and package mapping

| Capability | Package | Tier |
|---|---|---|
| Grid hidden in normal play: producing-hex pulse and hex text off, overlays dev-only, `DevOverlays` flags | U3 | 1 (it supports "functional player controls": the player must not see the grid while playing) |
| Forest edges, ecotones, coast detail, curved road polish, settlement clusters and distinct buildings | U3 | 2 |
| Resource-derived skin (depletion, fields tied to land records) | U3 with C1 | 2 |
| Real hill and mountain relief with figure and building lift | U3 | 2 |
| Character look: injury, illness, fatigue, pregnancy, carried goods in hands, equipment, hair by age | U4 | 2 |
| Divine effect set (suggestion, command, compulsion, possession, heal, smite, create, avatar) | U2/U4 with D2 to D5 | 1 for suggestion, command, compulsion, heal, smite, create; 2 for possession and avatar effects, resurrection |
| Sacred places, memorial markers, prayer threads | U4/U5 with D6, D7 | 2 |
| Aggregated far-range markers, instancing, ecological decoration, shader coast | U3 | 3 |
| Emotion posture detail and cap mood rim, per-household clothing | U4 | 3 |

## 16. PlayMode-testable acceptance hooks

| Hook | Test |
|---|---|
| H-VW-1 | Normal play: no object of `HexDressingView`, `TerritoryView` debug overlay or hex number text is active; with `DevOverlays.HexRegions` on, outlines exist for 19 hexes |
| H-VW-2 | Seam test: render four fixed cameras, compute the maximum colour gradient along every hex boundary and the longest straight run; both stay under thresholds |
| H-VW-3 | Resource consistency: for each wood stand, the tree count within its radius is non-increasing as the sim stock falls, and zero at depletion (headless sim run plus the planner) |
| H-VW-4 | Accessibility consistency: sample 1000 random land points; a point drawn as "meadow" has finite sim movement cost; a point drawn as water, cliff or dense forest has the sim's blocked or high-cost value |
| H-VW-5 | Roads: every drawn road ribbon lies within 10 m of its node polyline, and every person walking a road stays inside it |
| H-VW-6 | Divine identity: a planner test produces five effect records (suggestion, command, compulsion, possession, miracle) and asserts their `Shape`, `Colour` family and `Persistent` flag differ pairwise in at least 2 of the 3 channels |
| H-VW-7 | Truth: for every divine event type, exactly one effect is planned; with no event, none; with a refused suggestion, the suggestion effect plays and the response animation is the refusal |
| H-VW-8 | Character looks: a person with `Injury > 0` has bandage parts active; with illness, tinted; with pregnancy progress the belly scale equals the function of progress; with a ware carried, the ware mesh matches `Carry` |
| H-VW-9 | LOD invariance: running the same seed with the camera at three distances gives identical `StateHash` and identical event logs |
| H-VW-10 | Names: for 1000 generated people, names are non-empty, deterministic from seed, and the collision rate is under a configured bound |

## 17. Risks

| Risk | Mitigation |
|---|---|
| Real relief breaks node placement (figures and buildings float or sink) | rendered height only; lift sampled from the same function the ground mesh uses; the sim stays flat |
| Seams reappear after resource-driven changes | re-sample only whole chunks with a feathered border; the seam test runs after each change |
| Divine effects become noise at speed | persistent-state markers, aggregation, caps, pooling; effects suppressed above 10x except critical ones |
| Resource skin cost on large islands | dirty chunks only, signature-based refresh (as `IslandView`), a time budget per frame |
| Character rig changes break existing tests | keep part names; add parts, do not rename |
| Procedural names collide or look repeated | syllable tables with several hundred combinations per flavour, a lineage rule, a collision budget |
| Look drifts from the sim | rendering is derived by planners that are tested headless against `GodState` |

## 18. Owner D6 (27 Nov 2026): industrial presentation

Owner decision D6.19 ([OWNER_DECISION_REGISTER.md](../civdiv/OWNER_DECISION_REGISTER.md)). The sim side is owned by [INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md](INDUSTRIAL_RESOURCES_AND_PRODUCTION_DESIGN.md), [VEHICLES_AND_HARVESTING_DESIGN.md](VEHICLES_AND_HARVESTING_DESIGN.md), [ELECTRICITY_DESIGN.md](ELECTRICITY_DESIGN.md), [INDUSTRIAL_MILITARY_DESIGN.md](INDUSTRIAL_MILITARY_DESIGN.md) and [WASTE_AND_CIRCULAR_DESIGN.md](WASTE_AND_CIRCULAR_DESIGN.md); this section says how it looks. Every rule of §11 holds: the view reads state and events, animates only what events report, and never decides. **The island stays seamless** (D5.18–D5.20): deposits, facilities, lines and pollution are drawn around their nodes and polylines with feathered edges, never as hex shapes; hex outlines appear only with `DevOverlays.HexRegions`.

| Thing (D6.19) | How it looks | Driven by |
|---|---|---|
| **Geological deposits** | outcrops and coloured seams on rock (iron rust-red, copper green, coal black bands, uranium a faint pale glint), dark seeps on flat land for crude oil; drawn around the deposit node within `RadiusNodes`; shrink and pale as `Remaining` falls; a spent deposit is a scarred pit | `Deposit` (as the god observes it: hidden deposits are not drawn unless the observation profile shows them), `DepositDepleted` |
| **Mines and wells** | headframe or adit with spoil heap (mine), nodding pump (well); spoil heap grows with slag in the waste bin; the wheel or pump moves only while a cycle runs | facility record, `ProductionCycleStarted/Completed`, `FacilityStatusChanged` |
| **Moving harvesters with visible cargo** | original low-poly vehicle models, interpolated between nodes from `VehicleMoved` at their step rate; the cargo bed shows a heap scaled by the cargo bin's weight share and coloured by ware (ore, coal, scrap; tanker drum level for liquids); the operator visible in the cab; exhaust only while moving; a stopped vehicle shows its reason glyph (fuel, spanner, empty seat) | `Vehicle`, cargo bin contents, `VehicleMoved`, `VehicleStopped` |
| **Industrial buildings and production animations** | each facility kind a distinct silhouette: smelter (chimney, glow while working), refinery (columns and tanks), factory (saw-tooth roof, open bay showing the product being assembled: a vehicle frame grows with cycle progress), power station (stacks, cooling towers for the fictional nuclear design), recycling plant (sorting belt), strategic site (fenced, warning colours). Smoke, sparks and lit windows only while `Working`; workers visible at the entrance from the facility's present workers. Output stockpiles visible as heaps or crates from the output bin | facility record, cycle events, workers present |
| **Power lines** | poles and cables along the link's node polyline; in normal play subtle; a cut line hangs slack; a switched-open line shows an open switch at its end | `PowerLink`, `PowerLinkSwitched/Damaged` |
| **Shortage indicators** (diegetic) | facilities in `NoPower` go dark with a flickering lamp; stations short of fuel show an empty bunker; facilities missing inputs show empty input bays; outages darken a whole component's lights at dusk | `ProductionStalled`, `OutageStarted/Ended`, `PowerShortage*`, fuel bin levels |
| **Military vehicles and defences** | distinct original models; crew in hatches; walls, bunkers and gun posts as built assets | INDUSTRIAL_MILITARY records and events |
| **Waste and pollution** | litter heaps where waste lots lie; dumps and landfills as mounds that grow; haze and discoloured vegetation by pollution band (subtle in normal play); a long-lasting pale tint for contamination | ground lots, site bins, `PollutionDay`, `ContaminationDay` |
| **Strategic strike** | a single large effect played only from `StrategicDeviceUsed`, then the real destruction events and the contamination tint; nothing is shown that the events do not report | INDUSTRIAL_MILITARY §4.7 events |

**Planners.** As for divine effects, pure headless-testable planners build the draw records: `DepositSkinPlanner`, `FacilityLookPlanner`, `VehiclePosePlanner` (node interpolation and cargo heap from state), `GridOverlayPlanner`, `PollutionSkinPlanner`. They read sim state through the existing views and the event stream only.

**LOD (§14).** Far range: facilities as silhouettes with a status colour, vehicles as dots moving along roads, lines omitted; mid: models without cargo detail; close: full detail with cargo heaps, crew and animations.

**Tier and package mapping:** package **I6** (industrial presentation and analytics, with U3 conventions): deposits, mines, moving harvesters with visible cargo, factory and station looks, shortage indicators: Tier 2 (with the D6.18 slice); pollution skin, military models, strike effect: Tier 3.

**PlayMode-testable hooks:** **H-VW-11** every vehicle's drawn position lies on the segment between its `PrevNode` and `Node` and its cargo heap scale equals the cargo bin's weight share (±1 step); **H-VW-12** a facility shows working animation iff its last status event is `Working`; **H-VW-13** with the overlay off and `DevOverlays.HexRegions` off, no hex outline or hex-shaped deposit decal exists (seam test H-VW-2 still passes with industry on); **H-VW-14** a deposit's drawn extent is non-increasing as `Remaining` falls; **H-VW-15** LOD invariance (H-VW-9) holds in the industrial scenario.

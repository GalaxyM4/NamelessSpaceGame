# Mars rock fields

## What is included

`Assets/Scenes/MarsScene.unity` contains a baked, seeded field using the five
`SM_ROCK_*` models, matching `COL_ROCK_*_Convex` hulls, and packed 1024px color/normal
textures from `rocks_lowpoly_v03_compartir.blend`. The original Blender file is
left unchanged outside `Assets`; teammates do not need Blender to play the scene.
Temporary ground/scale-reference objects and procedural source materials are not imported.

Initial population:

| Role | Count | Diameter range | Collision |
| --- | ---: | --- | --- |
| Decoration | 10,000 | 0.08–0.24 m | None |
| Obstacles | 1,600 | 0.5–2.4 m | Fixed convex hull |
| Passive study candidates | 60 | 0.16–0.30 m | Fixed convex hull |

Diameter means the mesh's longest dimension before rotation, not an arbitrary
Blender scale multiplier. Small sizes are more common than large sizes.

Terrain heights, terrain material/layers, and the remote rover's saved spawn
position are not changed. No scatter or source-raster work runs in the player.
The generated hierarchy is stored in a separate `BakedMarsRocks.prefab` so normal
scene changes remain small and the entire layout can be versioned independently.

## Scope and teammate integration

This feature handles assets and placement only. It does **not** implement collection,
study, inventory, scientific properties, input bindings, or HUD changes.
The rover's collection/study mechanic is owned by the teammate's implementation.

`RockDefinition` holds only shared visual assets and field preference.
The 60 small candidate rocks have a passive `RockSamplePlacement` component with
`definition`, `placementId`, `diameterMeters` and a screened `approachPosition`.
These are placement hints, not collectible behaviour or an interaction contract.
The teammate can attach their own mechanic and scientific data independently.

## Tuning placement

1. Select **Mars Rock Placement** in the Hierarchy.
2. Edit its normal Inspector: counts, per-role diameter limits, seed, patch scale,
   clustering, slope limits, spacing, burial, ground alignment and cull distances.
3. Run **Tools > Mars Rocks > Bake Selected Field**.
4. Review the result, then save the scene. Scene hierarchy edits are grouped for
   Undo; generated prefab file changes require rebaking or version-control rollback.

The algorithm combines broad coherent density patches, slope/terrain-break
preference, denser exploration near spawn, patch-related rock families, and
irregular spacing. It creates both open areas and rocky patches; it is not uniform
random scattering and it is not a scientific reconstruction of rock locations.

Obstacles are planned first. A four-connected terrain/obstacle driving grid,
including sampled links and rover-width clearance, screens routes from spawn.
Candidates are placed beside reachable approach positions; five starter candidates
cover all five models within 45 m. Routes are checked again with the samples present.
Decorations are added last and cannot fill sample approach envelopes.
Ground contact uses transformed mesh vertices with slight burial.

**Protection and objectives**

- Spawn clearance defaults to 10 m.
- Add `RockExclusionZone` to a scene object and assign it to `exclusions`.
  Its radius protects a circle; assigning `corridorEnd` protects an XZ capsule
  along a driving corridor. Bounds are padded by each rock's footprint.
- Assign objective transforms to `studySites` to require a reachable nearby sample
  for each. Required site samples count toward the configured total.
- Set `roverClearanceRadius` to cover the rover's footprint, and
  `settledRoverHeight` to its settled body-center height (default 1.4 m).
  Its saved airborne Editor spawn height is deliberately not used for reach checks.
- `candidateApproachRange` (default 4.5 m) is a placement-only proximity screen.
  It does not define gameplay reach. The teammate's mechanic owns its own tool
  position, size limits, collection rules and arm-pose validation.

The bake first plans the entire layout. Invalid controls, missing assets or
insufficient space cause a clear failure rather than a partial field or silently
missing samples. Only the hierarchy linked by `bakedRoot` with the matching ownership
marker is replaced. Put handcrafted rocks outside this generated hierarchy.
No global Unity random state is used. Same seed, definitions, settings, terrain and
existing scene blockers produce the same layout hash.

Driving-grid and spherical-envelope screening are conservative approximations,
not proof of a physics-drivable route for every suspension state. Playtest required
routes; assign explicit exclusion corridors for critical objectives.

## Assets and performance

### Inspector density and burial controls

Select **Mars Rock Placement** in `MarsScene`. Enable **Use Density** to set
decorations and obstacles in rocks per hectare (10,000 m² of horizontal terrain
area). The Inspector shows the resulting rounded target counts. Disable it to use
the explicit population counts instead. Passive sample candidates always retain
their own explicit count; these controls add no interaction mechanics.

**Minimum Burial Fraction** and **Maximum Burial Fraction** control sinking as a
fraction of each scaled mesh's height. The scene uses 0.08–0.35 for varied ground
contact. Equal endpoints give uniform burial; both zero remove intentional
sinking. Depths are deterministic and do not consume the placement RNG. Render
meshes and collision hulls move together; terrain heights remain untouched.

Click **Rebuild Rocks**, then save the scene. This replaces the owned generated
prefab, so it also affects other instances of that prefab. Changes do not apply
live as you drag controls. Targets that cannot fit the spacing, slope, exclusion
or access constraints fail without replacing the previous field. Each population
is limited to 100,000 rocks; high densities can make baking and rendering costly.

`Tools/Rocks/EnableRockTuning.cs` is a one-time migration helper: preserves current
target counts as densities, enables the 0.08–0.35 burial range, rebuilds, saves,
and verifies that TerrainData, terrain material and rover position are unchanged.

### Martian dust coating

The five shared materials use `Mars/Rock Dust`, keeping their original color and
normal maps. World-up deposition, a mesh-relative lower band, subtle side dust,
spatial patches and pivot-seeded variation integrate them with the ground. Dust
uses the terrain's Rock029 color source, reduces smoothness and softens normal
detail without displacing geometry or changing collision. The lower band is an
art-directed approximation for the existing buried rocks, not a terrain-contact
simulation. No scripts update individual renderers and no material copies are made.

Select a material in `Assets/Models/Rocks/Prepared` to tune **Dust coverage**,
**Dust tint**, top/base strengths, patch size, and normal retention. Coverage zero
removes the coating. `Tools > Mars Rocks > Apply Dust To Shared Materials` applies
the shader; repeated setup preserves already-tuned dust values.

`Tools/Rocks/ApplyRockDust.cs` applies the coating and renders a matching-camera
before/after comparison in Edit Mode under the approved local `Temp/opencode`
directory. It verifies that scene, generated prefab, TerrainData, terrain material
and original rock texture references remain unchanged. It does not rebake rocks.

Five shared URP dust materials reuse the Blender textures and allow GPU instancing.
Meshes are low-poly (396–700 source render triangles); dedicated convex hulls
have at most 240 triangles. Decorative objects cast no shadows and have no physics.
Objects are organized in 64 m chunks and use `LODGroup` distance culling
(140 m decorative, 450 m obstacle, 180 m sample nominal distances at 60° FOV).
Culling varies with FOV/LOD bias and never disables collision.
No extra high/medium/low mesh tiers are needed for these already-low-poly assets.
There has been no standalone build or target-device GPU benchmark.

## Reproduction

From the project root, with Blender installed:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' `
  --background rocks_lowpoly_v03_compartir.blend --python Tools/Rocks/export_rocks.py
```

Exports render/collision geometry in a common normalized frame, preserves UVs,
and extracts packed texture bytes without recoloring. It never saves the `.blend`.

Open the saved MarsScene in a connected Unity Editor, in Edit Mode:

```powershell
unity command script run --file Tools/Rocks/SetupRockField.cs --entry SetupRockField.Run --timeout 180
unity command script run --file Tools/Rocks/BakeRockField.cs --entry BakeRockField.Run --timeout 240
```

Setup imports visual assets without adding gameplay components to the rover.
The bake script also hashes TerrainData and checks that the surface and saved rover
position are unchanged. It saves the scene and writes `bake-report.json`.

## Regression checks

In Edit Mode:

```powershell
unity command script run --file Tools/Rocks/TestRockPlacement.cs --entry TestRockPlacement.Run --timeout 240
unity command script run --file Tools/Rocks/ValidateRockField.cs --entry ValidateRockField.Run --timeout 180
```

Tests check normalized assets/hulls, reproducible seeds, different-seed variation,
counts, thresholds, spacing, spawn clearance and rejection of impossible settings.
Validation regenerates the complete plan, checks its hash against the baked field,
and verifies all renderer/collider/sample counts and references. See `validation.json`.

Collection and study tests belong to the teammate's gameplay system; this tool
validates only asset preparation, placement and approximate route accessibility.

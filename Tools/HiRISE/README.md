# HiRISE terrain workflow

The procedural generator has been replaced in `MarsScene` using the existing
`MarsTerrain.asset`. No raster downloads or GIS libraries are needed in the game.

## Current map

- Jezero western delta front: **4096 × 4096 m**, **4097²** height samples (1 m grid).
- Elevation: `DTEEC_045994_1985_046060_1985_U01` (stereo DTM, 1.00966 m source posts).
- Archived imagery: `ESP_045994_1985_RED_C_01_ORTHO` (monochrome, **not displayed**).
- [Source and product downloads](https://www.uahirise.org/dtm/ESP_045994_1985).
- Credit: **NASA/JPL-Caltech/University of Arizona**; DTM produced by USGS.
- +X is east; +Z is north. One Unity unit is one projected metre.
- **191.108 m** of natural relief, from -2613.930 to -2422.822 m absolute elevation,
  without vertical exaggeration. Grid sampling uses bilinear interpolation.
- Projected crop center: `(0, 1094500)`; bounds:
  `[-2048, 1092452, 2048, 1096548]` in the manifest's Mars equirectangular CRS.
  This spans the western delta-front sector around the Hawksbill Gap / Three Forks
  exploration area, adjacent crater floor, scarps, gullies and crater rims.
  Landmark names are regional context, not surveyed in-game waypoint coordinates.
- Rover starts at world XZ `(0, 0)` on approximately 0.68° ground below the delta,
  facing west toward it. Camera far clipping is 6000 m so distant terrain is visible.
  Scarp slopes can be impassable; this is not a promise of access to every plateau.
- Scene origin uses a recorded elevation offset, not absolute Martian elevation.
- Source elevations are used everywhere, including spawn: no flattening, smoothing,
  localized gameplay offsets, or vertical exaggeration. Rover placement moves the
  rover above the natural ground; it does not reshape the ground.
- HiRISE supplies **elevation only**. The displayed surface is the existing warm
  `Rock029_8K-JPG_Color.jpg` at a nominal 3 m scale, with its matching GL normal map.
  Three samples with deterministic random offsets and rotations blend on a smooth
  triangular lattice, breaking up recognizable repeated formations. Color and
  normals share sample coordinates; normal directions are inverse-rotated back
  into the terrain tangent frame. Explicit texture gradients avoid mip seams.
  The spatial randomization is stable as the camera moves, not animated noise.
  There is no satellite-image layer or blend, so dark markings baked into that
  imagery are not displayed. Real lighting and terrain shadows still remain.
- Color, material detail and lighting are artistic, not geographically registered
  or calibrated Mars reflectance. The terrain remains at the real horizontal and
  vertical scale. No displacement or collision changes are added by the textures.
- The terrain layer uses constant zero smoothness for a matte, dusty surface.
  The matching normal map shares the color texture's UVs, at 0.3 strength, fading
  between 12 and 45 m from the camera to limit distant shimmer.
  Smooth world-space color variation adds restrained rusty/pale patches at 28 m
  and approximately 90 m scales (strength 0.18), not measured surface composition.
  The existing skybox is unchanged and
  its distant mountains are **not** part of the georeferenced terrain.

## Reproduce

Requirements: Python, [uv](https://docs.astral.sh/uv/), and the project's Unity
Editor. Python GIS packages are pinned in `prepare.py` and installed in uv's
isolated cache, not into Unity.

From the project root:

```powershell
python Tools/HiRISE/download.py --region Jezero
uv run --python 3.13 Tools/HiRISE/prepare.py --region Jezero --center 0 1094500 --extent 4096 --resolution 4097
```

Original downloads (~415 MB) live in ignored `TerrainSources/Original/Jezero`, outside
`Assets`. Preparation writes:

- `TerrainSources/Prepared/Jezero/heights.f32`: little-endian absolute float32
  elevations, south-to-north rows, west-to-east columns. Not in player builds.
- `TerrainSources/Prepared/Jezero/manifest.json`: hashes, product IDs, projection,
  crop bounds, source elevations, orientation and image contrast parameters.
- `TerrainSources/Prepared/Jezero/ortho-monochrome.png`: untinted, contrast-stretched
  imagery retained for comparison and future palette changes.
- `Assets/Textures/HiRISE/JezeroOrtho.png`: registered, north-up, artistically tinted
  imagery, archived but not assigned to the terrain. Palette endpoints and both
  image hashes are recorded in the manifest. Archived Gale products and
  `GaleOrtho.terrainlayer` remain available; the active layer is `MarsRock.terrainlayer`.

An explicit `--center` is required (except with `--inspect`); automatic low-slope
crop selection has been removed. `--region Gale` and the default 2049 resolution
retain compatibility with the archived 2048 m map.
Use `--inspect` to inspect source georeferencing without writing outputs.
The orthoimage is reprojected to the DTM crop; their pixel origins are not assumed
identical. Missing elevation or imagery coverage aborts preparation rather than
silently filling holes or inventing heights.
The Jezero JP2's embedded projection misencodes its standard parallel as a latitude
of origin. Its companion PDS label names the DTM as its source and supplies the
matching projection parameters. Preparation checks that label, corrects the image
CRS to the DTM's and retains the image's own affine transform. This correction
affects archived imagery only, never the elevations or displayed surface.

In Unity, select `DebugTerrain` and click **Bake HiRISE Mars Terrain**. This replaces
its existing TerrainData in place, snaps the assigned rover, and writes
`bake-report.json`. Save the scene afterward. Every bake uses the original prepared
elevations without gameplay height edits. `heightMode`, `verticalScale` and
`maximumGameplayHeightChangeMeters` record source fidelity. Undo covers scene
objects, terrain and existing layer changes;
new asset creation and the external report are not undone.

Rebaking reads the prepared elevations, not the orthoimage: missing archived
imagery does not block a height bake. Image fields in `bake-report.json` identify
archived provenance only; `surfaceMode`, `surfaceTextureAssetPath` and
`surfaceTileMeters` identify the displayed artistic surface.

For the initial known scene setup, with `MarsScene` open and saved:

```powershell
unity command script run --file Tools/HiRISE/BakeCurrentScene.cs --entry BakeCurrentScene.Run
```

This explicitly configures source-faithful elevations, nominal 3 m stochastic rock surface and
rover reference, then bakes and saves the scene. It refuses a
dirty scene. Do not use it to overwrite subsequent custom bake settings.

## Validation

```powershell
uv run --python 3.13 --with rasterio==1.5.2 --with numpy==2.4.6 --with pillow==12.2.0 python -m unittest discover -s Tools/HiRISE
unity command script run --file Tools/HiRISE/ValidateBake.cs --entry ValidateBake.Run
unity command script run --file Tools/HiRISE/ValidateSurfaceQuality.cs --entry ValidateSurfaceQuality.Run
```

The Python tests cover vertex sampling, orientation, float byte order and missing
data rejection and refusal to automatically choose a low-slope crop. Unity validation
compares **every** elevation sample (including spawn) with the prepared source and
checks resolution, scale, collider and artistic surface assignment/tiling. Connectivity
at a 15° slope limit on a 4 m grid is reported as information only: steep spawn or
disconnected driving areas do not reject faithful terrain. Results are in `validation.json`.

### Restore source heights without touching the surface

```powershell
unity command script run --file Tools/HiRISE/RestoreSourceHeights.cs --entry RestoreSourceHeights.Run
```

Requires a saved MarsScene in Edit Mode. Rebuilds heights and updates the existing
collider in place without invoking surface setup or texture reimports. Verifies that
the material, layer settings/references, paint weights and basemap distance are unchanged.
Use this rather than the initial-setup script to retain all current texture tuning.
After any elevation or footprint change, **Rebuild Rocks** and save again: old
rock world positions do not automatically conform to new heights.

### One-time Gale → Jezero migration

`SwitchToJezero.cs` was used in two stages after backing up the open scene,
TerrainData and generated prefab. It preserves live burial/density settings,
TerrainData identity, collider references, material, layers and paint weights.
Only heights, footprint, rover placement/orientation, camera far clip and terrain
basemap distance change. It never alters collection or study mechanics.

```powershell
unity command script run --file Tools/HiRISE/SwitchToJezero.cs --entry SwitchToJezero.Terrain --timeout 120
unity command script run --file Tools/HiRISE/SwitchToJezero.cs --entry SwitchToJezero.Rocks --timeout 1200
```

The first stage leaves the scene dirty; the second rebuilds the shared generated
prefab with a Jezero-specific seed and saves. Do not replay this migration over
later custom spawn or seed settings. For normal tuning use the Inspectors.
The 4× larger area increases rock population and bake cost; cull distances and
shared materials are retained. No target-device performance benchmark is implied.

`CaptureTerrain.cs` renders three temporary-camera views to the approved local
`Temp/opencode` folder; it does not save camera objects or modify the scene.

### Surface quality only

```powershell
unity command script run --file Tools/HiRISE/ApplySurfaceQuality.cs --entry ApplySurfaceQuality.Run
```

With a saved MarsScene open in Edit Mode, this applies the surface setup without
rebaking or changing heights. It verifies identical height hashes before/after.
Set **Surface Tile Meters** on `DebugTerrain` and reapply/rebake to change the
shared color/normal tiling. Normal strength and fade distances can be tuned in
`Assets/Textures/HiRISE/MarsTerrainDetail.mat`; rebakes preserve those material
values. Set strength to zero for an A/B comparison without close-up normal detail.
**Broad Color Patch Size** and **Broad Color Variation** in the same material
control the artistic macro variation; set variation to zero to disable it.
The same setup runs during normal terrain rebakes.

The repeating rock color uses trilinear filtering, mipmaps, 16× anisotropy and desktop BC7 at 4096²; the
existing normal map uses linear normal-map import, Repeat, trilinear filtering,
16× anisotropy and desktop BC5 at 2048². The current desktop quality profile
already forces anisotropic filtering on. No global quality setting is changed.
`surface-quality.json` records the verified configuration. Shader forward lighting
and depth normals include the same detail; stock URP terrain passes handle
shadows and picking. The stock distant basemap is kept beyond the map's viewing
range because its generator does not reproduce the stochastic shading. This
costs three color and three normal samples per forward fragment rather than one
of each; no extra geometry or texture assets are generated. Standalone GPU cost
has not been profiled. The stock Meta pass is retained, but does not reproduce
the stochastic color for lightmap baking; the current scene uses realtime surface lighting.
This custom material
targets the matte artistic rock surface, not arbitrary metallic/masked terrain layers.

`DriveSmokeTest.cs` runs only in Play Mode. It adds a temporary virtual keyboard,
holds W through the existing rover controller for up to 12 game seconds or 100 m,
then removes the keyboard. A temporary input-update hook keeps that keyboard
current in an unfocused Editor; the input background behavior is restored in
`finally`. No controller code or saved input settings are changed.
Run only while nobody is controlling the rover:

```powershell
unity command script run --file Tools/HiRISE/DriveSmokeTest.cs --entry DriveSmokeTest.Run --timeout 45
```

It writes `drive-test.json`; exit Play Mode afterward to restore the baked spawn.
This straight-line smoke test is not a whole-map driving certification. Source
sampling does not resolve wheel-scale rocks, and stereo DTMs can contain
reconstruction artifacts even where pixels are valid.

### Verified Jezero migration

- All **16,785,409** saved elevation samples checked; maximum error **0.0029296875 m**.
- Existing artistic material, layers and paint weights preserved.
- Ten rock-placement regression checks and six Python preparation checks passed.
- Full rock validation reproduced all **46,460** transforms, meshes and materials;
  **6,460** colliders, **60** passive candidates, **961,645** reachable screening cells.
- Ten nested rock prefabs; largest **17,605,542 bytes**.
- Advancing Play Mode verified; rover drove **100.81 m** through the existing controller,
  with maximum observed tilt **5.67°** and grounded wheels in every sampled observation.
- Returned to Edit Mode; temporary background-running and input overrides restored.
- This is a spawn-area smoke test and approximate route screen, not whole-map
  suspension-level certification or a standalone/target-device performance benchmark.

The prepared sources and reports are versionable reproducibility inputs; the
player packages only referenced Unity assets. No procedural height noise is used.

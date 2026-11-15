# HiRISE terrain workflow

The procedural generator has been replaced in `MarsScene` using the existing
`MarsTerrain.asset`. No raster downloads or GIS libraries are needed in the game.

## Current map

- Gale MSL landing-site region: **2048 × 2048 m**, 2049² height samples.
- Elevation: `DTEEC_024234_1755_024300_1755_U01` (stereo DTM, approximately 1 m sampling).
- Imagery: `ESP_024234_1755_RED_C_01_ORTHO` (monochrome, **not true color**).
- [Source and product downloads](https://www.uahirise.org/dtm/ESP_024234_1755).
- Credit: **NASA/JPL-Caltech/University of Arizona**; DTM produced by USGS.
- +X is east; +Z is north. One Unity unit is one projected metre.
- Approximately 14 m of natural relief, without vertical exaggeration.
- Scene origin uses a recorded elevation offset, not absolute Martian elevation.
- A 6 m spawn radius is flattened to the source elevation at spawn, blended over
  another 12 m. Maximum change in this bake: approximately 0.42 m.
- A 4096² registered orthoimage is colorized with an artistic warm Mars palette,
  inspired by the original rock texture. There is no repeating rock-layer blend.
  Brightness maps monotonically from sRGB (54,35,23) to (218,166,119), preserving
  spatial patterns without adding invented texture. This does not add resolution
  or wheel-scale detail. Color and lighting are artistic, not calibrated reflectance.
  The terrain layer uses constant zero smoothness for a matte, dusty surface.
  A terrain-compatible URP material adds only the existing rock **normal map** at
  3 m tiling and 0.3 strength, fading between 12 and 45 m from the camera.
  This is artistic close-up lighting detail: no repeating color overlay,
  displacement, or changes to terrain/collision heights. It cannot recover detail
  absent from the approximately 1 m source imagery.
  The existing skybox is unchanged and
  its distant mountains are **not** part of the georeferenced terrain.

## Reproduce

Requirements: Python, [uv](https://docs.astral.sh/uv/), and the project's Unity
Editor. Python GIS packages are pinned in `prepare.py` and installed in uv's
isolated cache, not into Unity.

From the project root:

```powershell
python Tools/HiRISE/download.py
uv run --python 3.13 Tools/HiRISE/prepare.py --center 1397.968102405869 -275285.4658527607 --extent 2048
```

Original downloads (~471 MB) live in ignored `TerrainSources/Original/Gale`, outside
`Assets`. Preparation writes:

- `TerrainSources/Prepared/Gale/heights.f32`: little-endian absolute float32
  elevations, south-to-north rows, west-to-east columns. Not in player builds.
- `TerrainSources/Prepared/Gale/manifest.json`: hashes, product IDs, projection,
  crop bounds, source elevations, orientation and image contrast parameters.
- `TerrainSources/Prepared/Gale/ortho-monochrome.png`: untinted, contrast-stretched
  imagery retained for comparison and future palette changes.
- `Assets/Textures/HiRISE/GaleOrtho.png`: registered, north-up, artistically tinted
  imagery. Palette endpoints and both image hashes are recorded in the manifest.

Omit `--center` to screen candidate crops for valid coverage and moderate slopes.
Use `--inspect` to inspect source georeferencing without writing outputs.
The orthoimage is reprojected to the DTM crop; their pixel origins are not assumed
identical. Missing elevation or imagery coverage aborts preparation rather than
silently filling holes or inventing heights.

In Unity, select `DebugTerrain` and click **Bake HiRISE Mars Terrain**. This replaces
its existing TerrainData in place, snaps the assigned rover, and writes
`bake-report.json`. Save the scene afterward. Localized offset edits and spawn
flattening are reapplied from original prepared heights on each bake, never
accumulated. Undo covers scene objects, terrain and existing layer changes;
new asset creation and the external report are not undone.

For the initial known scene setup, with `MarsScene` open and saved:

```powershell
unity command script run --file Tools/HiRISE/BakeCurrentScene.cs --entry BakeCurrentScene.Run
```

This explicitly configures the small spawn edit, texture import settings, disables
the repeating detail layer and assigns the rover, then bakes and saves the scene. It refuses a
dirty scene. Do not use it to overwrite subsequent custom bake settings.

## Validation

```powershell
uv run --python 3.13 --with rasterio==1.5.2 --with numpy==2.4.6 --with pillow==12.2.0 python -m unittest discover -s Tools/HiRISE
unity command script run --file Tools/HiRISE/ValidateBake.cs --entry ValidateBake.Run
unity command script run --file Tools/HiRISE/ValidateSurfaceQuality.cs --entry ValidateSurfaceQuality.Run
```

The Python tests cover vertex sampling, orientation, float byte order and missing
data rejection. Unity validation compares untouched elevations with the prepared
source, checks the collider and imagery tile scale, and screens connectivity on
a 4 m grid with a 15° slope limit. Results are in `validation.json`.

### Surface quality only

```powershell
unity command script run --file Tools/HiRISE/ApplySurfaceQuality.cs --entry ApplySurfaceQuality.Run
```

With a saved MarsScene open in Edit Mode, this applies the surface setup without
rebaking or changing heights. It verifies identical height hashes before/after.
Normal strength, tiling and fade distances can be tuned in
`Assets/Textures/HiRISE/MarsTerrainDetail.mat`; rebakes preserve those material
values. Set strength to zero for an A/B comparison with the untampered HiRISE color.
The same setup runs during normal terrain rebakes.

Imagery uses trilinear filtering, mipmaps, 16× anisotropy and desktop BC7; the
existing normal map uses linear normal-map import, Repeat, trilinear filtering,
16× anisotropy and desktop BC5 at 2048². The current desktop quality profile
already forces anisotropic filtering on. No global quality setting is changed.
`surface-quality.json` records the verified configuration. Shader forward lighting
and depth normals include the same detail; stock URP terrain passes handle
shadows, picking, lightmap baking and the distant basemap. This custom material
targets the matte HiRISE surface, not arbitrary metallic/masked terrain layers.

`DriveSmokeTest.cs` runs only in Play Mode. It adds a temporary virtual keyboard,
holds W through the existing rover controller for up to 12 game seconds or 100 m,
then removes the keyboard. Run only while nobody is controlling the rover:

```powershell
unity command script run --file Tools/HiRISE/DriveSmokeTest.cs --entry DriveSmokeTest.Run --timeout 45
```

It writes `drive-test.json`; exit Play Mode afterward to restore the baked spawn.
This straight-line smoke test is not a whole-map driving certification. Source
sampling does not resolve wheel-scale rocks, and stereo DTMs can contain
reconstruction artifacts even where pixels are valid.

The prepared sources and reports are versionable reproducibility inputs; the
player packages only referenced Unity assets. No procedural height noise is used.

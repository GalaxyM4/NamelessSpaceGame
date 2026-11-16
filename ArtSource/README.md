# Editable art sources

Keep authoring files outside `Assets/` so Unity imports only the exported game
assets and teammates do not need Blender installed to open the project.

- `Rocks/rocks_lowpoly_v03_compartir.blend`: original rock models, collision hulls,
  and packed textures. Export instructions are in `Tools/Rocks/README.md`.
- `Materials/Rock029_8K-JPG.blend`: original Rock029 Blender source, formerly in
  `Assets/Textures/`. Runtime texture images remain in `Assets/Textures/`.
  If Blender requests unpacked external images, relink them to that directory;
  the source file's bytes have not been rewritten during this move.

The former Unity `.blend.meta` is retained alongside the Rock029 source for
provenance. It is outside Unity's import tree and is not needed for the game.
Both `.blend` files remain covered by the repository's Git LFS configuration.
Do not place exported runtime meshes, materials, or textures here: those belong
in `Assets/`.

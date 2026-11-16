# Rock analysis window (RocasMarte_UI)

Sets itself up on Play, without touching the scene (`RocasMarteBootstrap`):
- **Every** rock of the field (`Decoration/Obstacle/Sample <Type>`) can be inspected.
  - `Sample <Type>` (60): panel of the real rock (Cheyava Falls, Séítah, Rochette, Broom Point, Singita).
  - Decoration and obstacles: the name of their rock type (BASALT, OLIVINE CUMULATE, MUDSTONE,
    IMPACT BRECCIA, GABBRO), tagged "UNCATALOGUED", with their real size.
- Press **F** near a rock (≤ 4 m from the drill tip, rocks in front first). **F** or **Esc**: close.
- Settings: `Resources/RocasMarte_Config`. Data: `Resources/rocks_info.json`. 3D scan: `Resources/RockWire/`.
- Fonts: Overmars title and FutureNow headings (`Assets/fonts`), small text Titillium Web (OFL).
- To stop the rover while the window is open: `RockInfoUI.IsOpen` / `OpenChanged`.

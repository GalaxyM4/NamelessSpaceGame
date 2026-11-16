# Ventana de análisis de rocas (RocasMarte_UI)

Se monta sola al darle a Play, sin tocar la escena (`RocasMarteBootstrap`):
- **Todas** las rocas del campo (`Decoration/Obstacle/Sample <Tipo>`) se pueden inspeccionar.
  - `Sample <Tipo>` (60): ficha de la roca real (Cheyava Falls, Séítah, Rochette, Broom Point, Singita).
  - Decoración y obstáculos: el nombre de su tipo de roca (BASALTO, CUMULADO DE OLIVINO, LUTITA,
    BRECHA DE IMPACTO, GABRO), marcadas como "SIN CATALOGAR", con su tamaño real.
- Tecla **F** cerca de una roca (≤ 3 m de su superficie, prioridad a la de delante). **F** o **Esc**: cerrar.
- Ajustes: `Resources/RocasMarte_Config`. Datos: `Resources/rocks_info.json`. Escaneo 3D: `Resources/RockWire/`.
- Fuentes: título Overmars y encabezados FutureNow (`Assets/fonts`), texto pequeño Titillium Web (OFL).
- Para parar el rover con la ventana abierta: `RockInfoUI.IsOpen` / `OpenChanged`.

# Nameless Space Game

Nameless Space Game is a 3D PC game submitted to the **2026 NASA Space Apps Challenge** by Kiwi Brotherhood. Developed as a two-day hackathon prototype, it brings space exploration closer to players through a hands-on Mars mission.

Learn to move in zero gravity inside a spacecraft capsule, then operate a rover inspired by NASA's Perseverance on the Martian surface. Configure the rover within a budget, analyze rocks, collect samples, and manage limited mission resources. The project aims to make the science and practical constraints of space exploration accessible to a broad audience.

## Mission experience

- **Zero gravity:** learn movement and interaction inside the spacecraft.
- **Mars exploration:** drive a rover across 3D terrain from a first-person perspective.
- **Rock science:** inspect rock characteristics and collect the samples required by each mission.
- **Resource management:** make equipment choices within a budget and monitor battery, time, and sample tubes.
- **Audio and interfaces:** use custom dashboards accompanied by music and rover sounds.

The experience draws on NASA resources, including Mars terrain references, information about Martian rocks, and a Perseverance 3D model used as a design reference. Some rock properties are estimates for gameplay and should not be treated as scientific measurements.

## Play

Download the Windows 64-bit build from the repository's [Releases](https://github.com/GalaxyM4/NamelessSpaceGame/releases) page. Extract the archive and run `NamelessSpaceGame.exe`. Platform availability and requirements are listed with each release.

Select **Play** from the main menu and follow the capsule tutorial. Use the mission screen to choose a mission and configure the rover, then follow the rover tutorial and briefing on Mars. Inspect nearby rocks, collect the requested samples, and monitor your remaining resources.

### Controls

Keyboard and mouse controls depend on where you are:

| Action | Space station | Rover |
| --- | --- | --- |
| Move | W, A, S, D | W, A, S, D |
| Look or rotate camera | Mouse | Q, E; left Shift, left Ctrl |
| Additional movement | Space / left Shift: up; C / left Ctrl: down | — |
| Roll | Q, E | — |
| Brake | X | Space |
| Interact or analyze a rock | F | F |
| Drill a sample | — | Hold R |
| Other | V: switch rotation and look modes | Tab: skip mission tutorial |

## Development

### Requirements and setup

- Unity **6000.6.5f1**, installed through Unity Hub, with the build support module for your target platform.
- Git and Git LFS for the repository's large art assets.

1. Clone the repository and ensure Git LFS files are downloaded.
2. Add the repository folder to Unity Hub and open it with Unity 6000.6.5f1.
3. Allow Unity to import the assets and resolve the packages in `Packages/manifest.json`.
4. Open `Assets/Scenes/MainMenuScene.unity` and enter Play mode to start from the main menu.

The project uses C#, the Universal Render Pipeline, the Unity Input System, and UI Toolkit. Build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`: main menu, loading screen, lobby, and Mars.

Blender was used for 3D modeling and textures, and Inkscape for interface design. Claude, connected to Blender through an MCP extension, assisted with models and textures; it also helped generate some rover sounds and draft the project documentation.

### Build and test

- Use **File > Build Profiles** in Unity to select a target platform and build the configured scenes. Install the appropriate platform module if Unity requests it.
- Run the mission Edit Mode tests through **Window > General > Test Runner**. Tests are located in `Assets/_Project/Missions/Tests/Editor/`.

### Project layout

- `Assets/Scenes/` — game scenes.
- `Assets/Scripts/` — player, rover, terrain, rock, and interface code.
- `Assets/_Project/Missions/` — mission definitions, runtime logic, UI, editor tools, and tests.
- `Assets/RocasMarte_UI/` — rock inspection and scanning.
- `ArtSource/` — editable art sources kept outside Unity's import directory; see its [README](ArtSource/README.md).
- `Tools/Rocks/` — rock production and validation tools; see its [README](Tools/Rocks/README.md).

## Team and credits

**Kiwi Brotherhood**

- **David Campos Mines** — 3D modeling, audio, and documentation.
- **Víctor Marin Alcalde Arturo** — user interfaces.
- **Luis Gerardo Mejía Irigoin** — programming.
- **Carlo Franco Díaz Rodrigues** — research and environments.

The project uses NASA information and visual references for educational inspiration, including the [Mars 2020 Perseverance mission](https://science.nasa.gov/mission/mars-2020-perseverance/). It was developed for the NASA Space Apps Challenge; it is not an official NASA product.

## Future directions

Potential improvements include expanding the rock catalog and scientific information, adding environmental events such as dust storms, introducing more mission stages, and testing the experience with students.

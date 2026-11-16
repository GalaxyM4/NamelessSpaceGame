# Nameless Space Game

Nameless Space Game is a Mars exploration game built in Unity. Move through a space station, operate a rover on the Martian surface, and complete missions by analyzing rocks and collecting samples. Manage your time, battery, and limited sample tubes as you work toward each objective.

## Play

Download the Windows 64-bit build from the repository's [Releases](https://github.com/GalaxyM4/NamelessSpaceGame/releases) page. Extract the archive and run `NamelessSpaceGame.exe`. Platform availability and requirements are listed with each release.

Select **Play** from the main menu and follow the on-screen tutorials. In rover missions, inspect nearby rocks, collect the samples specified in the briefing, and monitor your remaining resources.

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

The project uses the Universal Render Pipeline, the Unity Input System, and UI Toolkit. Build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`: main menu, loading screen, lobby, and Mars.

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

## Credits

Developed by Kiwi Brotherhood with AI assistance. The in-game credits also acknowledge Unity, Blender, and NASA Space Apps.

# Ragna

A Ragnarok-style 3D action RPG built with **Godot 4.7 (.NET)** and C#.

## Requirements

- Godot 4.7.x, .NET edition
- .NET 8 SDK

## Build

```sh
godot --headless --build-solutions --quit
```

Or, without the editor:

```sh
dotnet build
dotnet test
```

## Projects

| Project | Path | Notes |
| --- | --- | --- |
| `Ragna` | `Ragna.csproj` | Godot game assembly (scenes, nodes, UI) |
| `Ragna.Core` | `scripts/core/Ragna.Core.csproj` | Engine-agnostic game logic. Plain .NET SDK with no GodotSharp reference, so `using Godot;` does not compile here |
| `Ragna.Core.Tests` | `tests/Ragna.Core.Tests/` | xUnit tests for `Ragna.Core` |

## Layout

| Path | Purpose |
| --- | --- |
| `scenes/` | Godot scenes (`Main.tscn` is the entry scene) |
| `scripts/core/` | Engine-agnostic core logic and shared types (no Godot dependency) |
| `scripts/gameplay/` | Player, monsters, combat, items |
| `scripts/ui/` | HUD, inventory and menu scripts |
| `scripts/dialogue/` | Dialogue runner and scene UI |
| `data/` | Data files (JSON dialogue, item and monster tables). `data/stats/stat_table.json` tunes stats, levels and EXP; `data/monsters/monsters.json` defines monster types |
| `assets/` | Art, audio, fonts |
| `scenes/maps/` | Playable maps (`TestMap.tscn` is generated, see [docs/terrain.md](docs/terrain.md)) |
| `assets/terrain/` | Terrain MeshLibrary, materials and textures |
| `tools/` | Asset build scripts (`tools/build_terrain.sh`, `tools/characters/make_placeholder_sprites.py`) |
| `scenes/actors/` | Characters (`Player.tscn`, `Monster.tscn`) and the character sprite with its blob shadow (`CharacterVisual.tscn`) |
| `assets/characters/` | Character sprite sheets and `SpriteFrames` |
| `docs/` | Guides, for example [terrain and GridMap painting](docs/terrain.md), the [game camera](docs/camera.md), [click-to-move and navmesh baking](docs/navigation.md) and [character sprites](docs/characters.md), [stats, levels and EXP](docs/stats.md), [combat](docs/combat.md) and [monsters, AI and spawn points](docs/monsters.md) |
| `tests/` | xUnit test projects (excluded from the game assembly, ignored by Godot) |

## Controls

| Input | Action |
| --- | --- |
| Left mouse button | Walk to the clicked ground; hold it to keep walking towards the cursor |
| Left-click a monster | Walk into range and attack it until it dies (click the ground to stop) |
| **Q** / **E** | Turn the camera 45° around the player |
| Mouse wheel | Zoom in and out |

See [docs/navigation.md](docs/navigation.md), [docs/camera.md](docs/camera.md), [docs/combat.md](docs/combat.md) and [docs/monsters.md](docs/monsters.md).

## C# conventions

File-scoped namespaces, records for immutable data, and nullable reference types enabled. Warnings are treated as errors.

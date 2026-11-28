# Monsters

Monsters wander around their spawn point, chase and attack the player, walk home when the player gets away, and come back a while after they die.

The rules live in `Ragna.Core.Monsters` (`scripts/core/Monsters/`), with no Godot dependency, and are tested in `tests/Ragna.Core.Tests/Monsters/`.

| Piece | Where | What it does |
| --- | --- | --- |
| `MonsterBrain` | core | The AI. Each frame says whether to stand, move (and when to ask for a new path) or attack now |
| `MonsterDefinition`, `MonsterBehavior` | core | One monster type: stats, speeds, attack delay, respawn time, AI tuning |
| `MonsterDatabase` | core | Reads and checks `data/monsters/monsters.json` |
| `SpawnTimer` | core | Respawn bookkeeping for one spawn point |
| `Monster` | `scripts/gameplay/Combat/`, `scenes/actors/Monster.tscn` | Walks the navmesh, plays the sprite, deals and takes damage |
| `MonsterSpawner` | `scripts/gameplay/Monsters/` | A spawn point. Keeps a number of one monster type alive |
| `MonsterData` | `scripts/gameplay/Monsters/` | Loads `monsters.json` once for the game |

## AI states

| State | What the monster does | Leaves when |
| --- | --- | --- |
| Idle | Stands still for a random `idleTimeMin`–`idleTimeMax` seconds | The timer runs out (→ Wander), or it aggroes (→ Chase) |
| Wander | Walks to a random point within `wanderRadius` of home | It arrives, the path ends, or `wanderTimeout` passes (→ Idle), or it aggroes (→ Chase) |
| Chase | Runs at `chaseSpeed` towards the player | In `attackRange` (→ Attack), or leash (→ Return) |
| Attack | Stands, faces the player and attacks every `attackDelay` | Player steps out of range (→ Chase), or leash (→ Return) |
| Return | Walks home at `moveSpeed`, ignoring the player | It gets home (→ Idle, HP refilled if `healOnReturn`) |
| Dead | Nothing. Its drops fall on the ground ([items](items.md)) | The body fades out; the spawner brings a new one |

**Aggro.** An aggressive monster (`aggressive: true`) attacks a living player within `aggroRadius`. Every monster, aggressive or not, fights back when the player hits it.

**Leash.** The monster gives up and walks home when the player dies or is gone, when the monster is more than `leashRadius` from home, or when the player is more than `leashRadius` from the monster's home. A player outside the leash radius never draws aggro.

Distances are measured on the ground, so height doesn't count. The attack timer keeps running between fights, like the player's.

## Monster data

`data/monsters/monsters.json` defines the monster types. Distances in metres, times in seconds, speeds in m/s. Comments and trailing commas are allowed. Values you leave out keep their defaults (see `MonsterDefinition` and `MonsterBehavior`).

| Key | Meaning |
| --- | --- |
| `id` | Key used by spawners, for example `slime`. Must be unique |
| `name` | Name shown to the player |
| `level`, `maxHp`, `atk`, `def`, `hit`, `flee` | Combat stats (see [combat](combat.md)) |
| `expReward` | EXP for the killer |
| `moveSpeed`, `chaseSpeed` | Walking (wander, return) and running (chase) speed |
| `attackDelay` | Seconds between attacks |
| `respawnTime` | Seconds after death before a new one spawns |
| `tint` | Sprite colour, `#rrggbb` |
| `behavior.aggressive`, `behavior.aggroRadius` | Whether and from how far it attacks on sight |
| `behavior.attackRange` | Ground distance at which it stops and attacks |
| `behavior.wanderRadius`, `behavior.leashRadius` | How far it wanders, and how far it follows, from home. `leashRadius` must be at least `wanderRadius` |
| `behavior.idleTimeMin`, `behavior.idleTimeMax`, `behavior.wanderTimeout` | Wander timing |
| `behavior.arriveDistance` | How close counts as arrived |
| `behavior.healOnReturn` | Refill HP on getting home after a leash (default `true`) |
| `drops` | What it can drop on death: a list of `{ "item", "chance", "min", "max" }`. See [items](items.md) |

The game ships two types:

| | Slime | Boar |
| --- | --- | --- |
| Temper | Passive: only fights back | Aggressive within 5 m |
| HP / ATK | 60 / 6 | 140 / 14 |
| Chase speed | 2.5 m/s | 3.6 m/s |
| Respawn | 8 s | 15 s |

The data file is checked by `MonsterDatabaseTests`, so a broken value fails `dotnet test`.

## Spawn points

Add a `Node3D` with `scripts/gameplay/Monsters/MonsterSpawner.cs` to a map, then set in the Inspector:

| Property | Meaning |
| --- | --- |
| `MonsterScene` | `scenes/actors/Monster.tscn` |
| `MonsterId` | A type in `monsters.json` |
| `Count` | How many it keeps alive |
| `Radius` | Monsters spawn at random navmesh points within this distance |
| `RespawnTimeOverride` | Seconds before a dead one comes back. Below 0 uses the type's `respawnTime` |

All of a spawner's monsters appear once the navmesh is ready. Each death starts its own respawn timer. A monster's home is the spot it spawned on.

`Main.tscn` has two spawners near the player start: three Slimes east of the player and two Boars to the south-east, far enough away that the Boars don't aggro at once.

## The player

Monsters attack `PlayerCombat`, which now has `Health` (max HP from the player's stats) and the events `Attacked(result, monster)`, `Died(monster)` and `Revived`. A dead player can't move or attack. Until there is a death screen, the player gets back up with full HP after `ReviveTime` (5 s) where they fell; monsters leash home in the meantime.

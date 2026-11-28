# Combat

Ragnarok-style auto-attack. Left-click a monster: the player walks into range along the navmesh, then attacks it once every attack delay until it dies. Click the ground to stop attacking and walk away. Clicking another monster switches target without resetting the attack timer, so clicking around never attacks faster than the attack speed allows.

The rules live in `Ragna.Core.Combat` (`scripts/core/Combat/`), with no Godot dependency, and are tested in `tests/Ragna.Core.Tests/Combat/`.

| Piece | Where | What it does |
| --- | --- | --- |
| `AutoAttack` | core | Each frame says whether to chase (and when to ask for a new path), wait, or attack now |
| `DamageCalculator` | core | Rolls crit, hit and damage for one attack |
| `CombatStats` | core | ATK, DEF, HIT, FLEE, CRIT. `CombatStats.FromCharacter` builds them from `CharacterStats` |
| `Health` | core | HP with `Damaged` and `Died` events |
| `PlayerCombat` | `scripts/gameplay/Combat/` | Child of the player. Picks monsters under the cursor, drives the mover and the sprite |
| `Monster` | `scripts/gameplay/Combat/`, `scenes/actors/Monster.tscn` | A target with stats and HP from `data/monsters/monsters.json`. Flashes when hit, fades out and frees itself on death. AI and respawn: see [monsters](monsters.md) |

## Attack speed

The time between attacks is the player's `Derived.AttackDelay` (see [stats](stats.md)), so AGI and DEX make the player attack faster. The first attack lands as soon as the player reaches range, if the timer is ready.

## Damage formula

Combat stats from base stats (players):

| Value | Formula |
| --- | --- |
| ATK | `STR + floor(STR / 10)² + floor(DEX / 5) + floor(LUK / 5) + weapon ATK` |
| DEF | `floor(VIT / 2) + armour DEF` |
| HIT | `level + DEX` |
| FLEE | `level + AGI` |
| CRIT | `1 + LUK × 0.3` % |

Monsters get ATK, DEF, HIT and FLEE from `data/monsters/monsters.json` (see [monsters](monsters.md)). Monster attacks on the player use the same formula, with the player's DEF and FLEE.

One attack:

1. **Critical**: roll against CRIT. A critical always lands.
2. **Hit**: otherwise roll against `clamp(80 + HIT − FLEE, 5, 100)` %. A miss deals 0.
3. **ATK roll**: `(ATK + skill flat bonus) × (1 ± 10%)`.
4. **DEF**: subtracted, except on a critical.
5. **Multipliers**: × the skill multiplier, × 1.4 on a critical.
6. **Damage modifiers**, in order.
7. Rounded down, at least 1.

All the numbers in steps 1–7 are in `CombatRules`.

## Skills

A skill is an `AttackContext` passed to `DamageCalculator.Attack`:

```csharp
var bash = new AttackContext { Name = "Bash", SkillMultiplier = 3, HitBonus = 20, CanCrit = false };
var result = calculator.Attack(player.CombatStats, monster.CombatStats, bash);
```

For effects the formula doesn't cover (elements, cards, size penalties), implement `IDamageModifier` and add it to `AttackContext.Modifiers`.

Tests pass an `IRandomSource` with fixed rolls. Each attack uses one number for the crit roll (if the attack can crit), one for the hit roll (if it didn't crit) and one for the ATK spread.

## Physics layers

Monsters are on layer 3 (`monsters`). The click ray in `PlayerCombat` hits layers 1 (terrain) and 3, so a monster behind a cliff can't be clicked. A click that hits a monster is marked as handled and never reaches `ClickToMoveController`.

## Events for UI

- `PlayerCombat.AttackMade(result, monster)`, `Killed(monster, exp)`, `TargetChanged(monster?)`
- `Monster.Attacked(result, attacker)`, `Died(monster, killer)`, `Health.Damaged`
- `PlayerCombat.Attacked(result, monster)`, `Died(monster)`, `Revived`, `PlayerCombat.Health.Damaged`

Damage numbers and HP bars (GAM-12) and the HUD log (GAM-15) can hook these.

## Test map

`Main.tscn` has two spawn points near the player start: three passive Slimes (60 HP) and two aggressive Boars (140 HP, more FLEE). Killing one gives EXP to `PlayerCombat.Stats`, and it comes back after its respawn time. The player's starting stats and weapon ATK are exports on the `Combat` node in `Player.tscn`. See [monsters](monsters.md).

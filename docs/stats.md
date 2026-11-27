# Stats, levels and EXP

Characters have six base stats, a base level and EXP. The rules live in `Ragna.Core.Stats` (`scripts/core/Stats/`), with no Godot dependency, and are tested in `tests/Ragna.Core.Tests/Stats/`. The tuning numbers live in `data/stats/stat_table.json`.

## Base stats

| Stat | Feeds |
| --- | --- |
| STR | Melee damage (combat, GAM-10) |
| AGI | Attack speed |
| VIT | Max HP |
| INT | Max SP |
| DEX | Attack speed (a little), hit (GAM-10) |
| LUK | Critical hits (GAM-10) |

Every stat starts at `startingStatValue` (1) and can be raised to `maxStatValue` (99). Raising a stat by one costs `2 + floor((value - 1) / 10)` stat points, so 1→2 costs 2 and 91→92 costs 11.

## Levels and stat points

- A new character is level 1 with `startingStatPoints` (48) to spend.
- EXP to go from level `L` to `L + 1` is `round(exp.base + exp.factor * L ^ exp.exponent)`. Set `exp.table` to a list of `maxBaseLevel - 1` numbers to use explicit values instead.
- Reaching level `L` grants `statPoints.base + floor(L / statPoints.bonusEvery)` points (3 at first, 4 from level 5, and so on).
- One big EXP gain can give several levels. Leftover EXP carries over. EXP past `maxBaseLevel` is dropped.

## Derived stats

| Value | Formula |
| --- | --- |
| Max HP | `floor((hp.base + hp.perLevel * (level - 1)) * (1 + VIT * hp.percentPerStat / 100))` |
| Max SP | Same shape with the `sp` numbers and INT |
| Attack delay | `attackSpeed.baseDelay * (1 - min(maxReduction, AGI * agiReduction + DEX * dexReduction))` seconds, never below `minDelay` |
| Attack speed | `1 / delay` attacks per second |

## Using it

```csharp
var table = StatTableLoader.FromJson(Godot.FileAccess.GetFileAsString("res://data/stats/stat_table.json"));
var stats = new CharacterStats(table);

stats.TryRaise(StatType.Agi);          // spend points
stats.AddExp(120);                     // returns levels gained, raises LevelledUp
var hp = stats.Derived.MaxHp;          // recomputed when anything changes (Changed event)
```

`new CharacterStats(table, level, exp, baseStats, statPoints)` restores a saved character.

## Job classes and skills

Anything that changes stats implements `IStatModifierSource` and returns `StatModifier`s:

- `StatModifier.AddStat(StatType.Agi, 5)` adds to a base stat before the formulas run.
- `StatModifier.Flat(DerivedStatType.MaxHp, 100)` adds to a derived stat.
- `StatModifier.Percent(DerivedStatType.AttackSpeed, 10)` adds a percentage. Percent modifiers on the same value add up and apply after flat ones.

Add a source with `stats.AddModifierSource(...)`. When its modifiers change, for example a passive skill levels up, call `stats.Invalidate()`. A job class can also bring its own `StatTable` (different HP growth, a different EXP curve).

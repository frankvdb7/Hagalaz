# thieving — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Historical candidate pickpocketing examples

| Target | Required level | XP | Typical result |
|---|---:|---:|---|
| Man / Woman | 1 | 8 | 3 coins (base) |
| Master Farmer | 38 | 43 | Assorted seeds |
| Guard | 40 | 46.5 | 30 coins |

Man/Woman failure was described as a **5-second stun** and **10 life points** of damage, while high-level targets use stronger stuns and damage. Later quest rewards can alter successful coin yield. Sources: [Man](https://wiki.darkan.org/Man), [Thieving](https://wiki.darkan.org/Theiving).

**To implement:** isolate each pickpocket/NPC variant's level, XP, loot distribution, fail chance, stun/damage, quest flags and cooldown. Harvest supported NPC IDs and pickpocket options from cache. The NPC's despawn/attack state must invalidate an in-progress theft.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../thieving.md) · [Research policy](../research-evidence.md)

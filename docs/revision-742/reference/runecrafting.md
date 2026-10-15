# runecrafting — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Historical altar outputs

| Rune | Level | XP per essence | Requires pure essence? |
|---|---:|---:|---|
| Air | 1 | 5 | No |
| Mind | 2* | 5.5 | No |
| Water | 5 | 6 | No |
| Earth | 9 | 6.5 | No |
| Fire | 14 | 7 | No |
| Body | 20 | 7.5 | No |
| Cosmic | 27 | 8 | Yes |
| Chaos | 35 | 8.5 | Yes |
| Astral | 40 | 8.75 | Yes |
| Nature | 44 | 9 | Yes |
| Law | 54 | 9.5 | Yes |
| Death | 65 | 10 | Yes |
| Blood | 77 | verify | Yes |

*Preserved table lists Mind at **1** in one excerpt; this conflicts with commonly recorded level 2. Mark pending target verification. Multiple runes per essence do **not** multiply XP. Source: [Runecrafting](https://wiki.darkan.org/Runecrafting), [altar locations](https://wiki.darkan.org/Runecrafting_altars).

**To implement:** resolve altar object/region + matching essence type, pouch handling, level-dependent rune multiplier, talisman/tiara requirements and quest unlock; map rune IDs from 742 cache. Don't permit soul altar crafting simply because a rune definition exists.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../runecrafting.md) · [Research policy](../research-evidence.md)

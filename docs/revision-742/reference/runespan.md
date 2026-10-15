# runespan — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Activity levels and early siphoning candidates

| Node | Requirement | XP per successful siphon |
|---|---:|---:|
| Cyclone | 1 | 19 |
| Mind storm | 1 | 20 |
| Water pool | 5 | 25.3 |
| Rock fragment | 9 | 28.6 |
| Fireball | 14 | 34.8 |
| Vine | 17 | 30.3 or 34.3 by rune |
| Fleshy growth | 20 | 46.2 |
| Fire storm | 27 | 22.8 or 41.7 by rune |
| Chaotic cloud | 35 | 61.6 |
| Nebula | 40 | 63.8 or 85.6 by rune |
| Shifter | 44 | 86.8 |

Three floors: lower **1+**, middle **33+**, upper **66+** (member-only for upper). Nodes can change type while being siphoned; if the new type is too advanced the action ends. Sources: [Runespan](https://wiki.darkan.org/Runespan).

**To implement:** capture the 742 realm region layout and node/crossing IDs, runic resource economy, rune costs for travel, point conversion on exit and world-node shared state. Keep activity-only runes from being stored in general persistent inventories.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../runespan.md) · [Research policy](../research-evidence.md)

# mining — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical ore candidate table

| Ore | Mining level | XP per ore |
|---|---:|---:|
| Copper / Tin | 1 | 17.5 |
| Iron | 15 | 35 |
| Silver | 20 | 40 |
| Coal | 30 | 50 |
| Gold | 40 | 65 |
| Mithril | 55 | 80 |
| Adamantite | 70 | 95 |
| Runite | 85 | 125 |

Ore respawn depends on vein type/world population in the historical system. Rune essence has no depletion timer. Pickaxes can operate from inventory; only wielding requires Attack level. Sources: [Mining](https://wiki.darkan.org/Mining), [pickaxe and XP cross-check](https://wiki.darkan.org/Volatile_clay_pickaxe).

**To implement:** source rock-object IDs and exhausted variants from cache, match each to database rock/ore definition and XP, extract historical pickaxe tiers, respawn ranges and chance functions. Confirm mithril level and all rows against local reference definitions. Do not hard-code generic 10-second respawns.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../mining.md) · [Research policy](../research-evidence.md)

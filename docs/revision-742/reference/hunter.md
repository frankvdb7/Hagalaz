# hunter — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Bird snare candidates

| Bird | Hunter level | XP | Location family |
|---|---:|---:|---|
| Crimson swift | 1 | 34 | Jungle |
| Golden warbler | 5 | 48 | Desert |
| Copper longtail | 9 | 61 | Woodland |
| Cerulean twitch | 11 | 64.67 | Snow |
| Tropical wagtail | 19 | 95.2 | Jungle |

On successful collection, a snare supplies bird-related materials; failed snares can fall over and must be reset. Only one trap may occupy a tile. Source: [Hunter](https://wiki.darkan.org/Hunter), [training overview](https://wiki.darkan.org/Hunter_training).

**To implement:** define trap-placement restrictions and owner, catchable NPC species, bait/smoke modifiers, success odds, trap breakdown, reward item IDs and reclaim policy. No dedicated Hunter files were identified in the first code audit; reuse existing world object and NPC lifecycle instead of adding a free-standing scheduler.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../hunter.md) · [Research policy](../research-evidence.md)

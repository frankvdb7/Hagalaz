# summoning — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Familiars, scrolls and pouch candidates

| Familiar | Level | Duration | Points cost | Special |
|---|---:|---:|---:|---|
| Spirit wolf | 1 | 6 min | 1 | Howl |
| Dreadfowl | 4 | 4 min | 1 | Dreadfowl Strike |
| Spirit spider | 10 | 15 min | 2 | Egg Spawn |
| Thorny snail | 13 | 16 min | 2 | Slime Spray; 3 burden slots |
| Granite crab | 16 | 18 min | 2 | Stony Shell |

**Spirit wolf recipe:** pouch, gold charm, **7 spirit shards**, wolf bones; pouch crafted at an obelisk. Source pages conflict on creation XP (**4.8/4.85** in table vs **45.8** in prose), so do not choose one without testing. Scroll-making converts one pouch into **10 scrolls** in the historical source. Sources: [familiars](https://wiki.darkan.org/Summoning_familiars), [wolf recipe](https://wiki.darkan.org/Spirit_wolf), [gold charms](https://wiki.darkan.org/Gold_charm), [scrolls](https://wiki.darkan.org/Summoning_scrolls).

**To implement:** populate complete pouch, ingredient, scroll, NPC ID, point charge, life, ability, carry slots and timer data. Familiar spawn/combat/BOB/teleport constraints must be verified separately.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../summoning.md) · [Research policy](../research-evidence.md)

## Expanded research materials

- [61 pouches](../data/researched/summoning-pouches.csv)
- [Spawn and pouch integrity](deep-summoning.md)

These additions include period-source candidate facts or checked Hagalaz code paths. They are **not a completed 742 client/cache parity capture**.

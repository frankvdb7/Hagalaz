# crafting — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Representative historical recipes

| Result | Required level | Inputs | XP |
|---|---:|---|---:|
| Cut sapphire | 20 | Uncut sapphire + chisel | 50 |
| Sapphire ring | 20 | Sapphire + gold bar + ring mould | 40 |
| Holy symbol | 16 | Silver bar + holy mould | 50 |
| Bowstring | 10* | Flax + spinning wheel | 15 |

*Source pages conflict: the Bowstring article says level 1, while a broader Crafting list states 10. Treat the level as **unresolved** pending 742 data verification. The tool belt must be considered for mould and chisel availability. Sources: [Crafting table](https://wiki.darkan.org/Crafting), [Sapphire](https://wiki.darkan.org/Sapphire), [Bowstring](https://wiki.darkan.org/Bow_String).

**To implement:** model the distinct gem, silver, leather, pottery, jewelry, spinning, tanning and glassmaking recipe sets. Source NPC/object IDs, tool requirement, exact input-output IDs, XP, quest conditions and quantity UI per family; do not infer them from gem tier alone.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../crafting.md) · [Research policy](../research-evidence.md)

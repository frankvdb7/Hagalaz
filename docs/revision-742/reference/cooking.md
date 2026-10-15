# cooking — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical food examples

| Food | Cooking level | XP | Life points healed |
|---|---:|---:|---:|
| Crayfish | 1 | 30 | 20 |
| Shrimps | 1 | 30 | 30 |
| Sardine | 1 | 40 | 30 |
| Lobster | 40 | 120 | verify per target cache |

The first three rows come from the historical fish cooking table; lobster XP is independently listed in a preserved experience explanation. Fire, range and bonfire cooking may have different burn rates. Sources: [fish cooking](https://wiki.darkan.org/Food/Fish), [experience](https://wiki.darkan.org/Experience_points).

**To implement:** for each raw food, gather cooked/burnt item IDs, required level, XP, healing, stop-burning thresholds by source and relevant equipment bonuses. Avoid a universal burn-rate formula until the historical thresholds and trial results are checked.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../cooking.md) · [Research policy](../research-evidence.md)

## Expanded period data and code analysis

[Meat cooking level/XP/healing table](../data/researched/cooking-meat-products.csv) · [Source-aware burn and task implementation study](deep-cooking.md)

Verify all exact client IDs/rates before treating secondary research as final revision-742 behavior.

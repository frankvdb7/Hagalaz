# fletching — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical bow recipes (XP shown per stage)

| Product | Level | Cut XP | String XP |
|---|---:|---:|---:|
| Ordinary shortbow | 1 | 5 | 5 |
| Ordinary longbow | 10 | 10 | 10 |
| Oak shortbow | 20 | 16.5 | 16.5 |
| Willow longbow | 40 | 41.5 | 41.5 |
| Maple longbow | 55 | 58.3 | 58.2 |
| Yew longbow | 70 | 75 | 75 |
| Magic longbow | 85 | 91.5 | 91.5 |

One ordinary log yields 15 arrow shafts; converting 15 shafts into headless arrows requires 15 feathers. Some source pages disagree on ordinary shortbow level; verify in actual 742 data. Source: [Fletching tables](https://wiki.darkan.org/Fletching_boost).

**To implement:** keep cutting and stringing as different recipes. Extract shortbow/longbow, crossbow, stock, arrow, bolt and dart item IDs, batch sizes, feather/tip resources and unlocks from the 742 cache plus local Hagalaz data.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../fletching.md) · [Research policy](../research-evidence.md)

## Extended implementation-grade evidence

See the [68-record recipe inventory and batch safety contract](deep-fletching.md), with separate bows, crossbows, projectiles and material quantity CSVs. The earlier basic bow candidate remains unresolved wherever sources conflict.

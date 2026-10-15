# woodcutting — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical tree candidate table

| Tree | Level | XP per log |
|---|---:|---:|
| Ordinary | 1 | 25 |
| Oak | 15 | 37.5 |
| Willow | 30 | 67.5 |
| Maple | 45 | 100 |
| Yew | 60 | 175 |
| Magic | 75 | 250 |

The higher hatchet tier changes chopping speed, not the XP of one log; required Attack level to *equip* differs from required Woodcutting level to *use*. Trees other than basic trees can yield multiple logs before falling. Source: [Woodcutting](https://wiki.darkan.org/Wooductting), [Yew](https://wiki.darkan.org/Yew).

**To implement:** enumerate normal/equivalent tree object IDs, stump transformations, skill/hatchet tiers, per-tree depletion and respawn probabilities; verify with 742 cache/object scripts. Confirm higher-tier table rows directly before applying migration.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../woodcutting.md) · [Research policy](../research-evidence.md)

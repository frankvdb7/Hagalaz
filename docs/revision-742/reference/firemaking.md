# firemaking — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical single-log fire table

| Log | Level | XP |
|---|---:|---:|
| Ordinary | 1 | 40 |
| Oak | 15 | 60 |
| Willow | 30 | 90 |
| Teak | 35 | 105 |
| Maple | 45 | 135 |
| Yew | 60 | 202.5 |
| Magic | 75 | 303.8 |

Bonfires (introduced before target revision) award different XP from lighting a ground fire. Example: adding willow logs to a bonfire yields **105 XP**, compared with **90 XP** for lighting one. A player's Firemaking level does not deterministically set the lifetime of an individual ground fire. Sources: [Firemaking](https://wiki.darkan.org/Firemaking), [Willow logs](https://wiki.darkan.org/Willow_logs).

**To implement:** define recipe method (ground fire vs bonfire), tool source (belt/inventory), tile placement, burn lifetime, multiple concurrent participants, per-log XP and effect animation. Verify bonfire XP table and fire object IDs against 742.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../firemaking.md) · [Research policy](../research-evidence.md)

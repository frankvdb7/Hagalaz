# fishing — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical basic catch table

| Catch | Level | Equipment | XP per catch |
|---|---:|---|---:|
| Crayfish | 1 | Crayfish cage | 10 |
| Shrimps | 1 | Small net | 10 |
| Tuna | 35 | Harpoon | 80 |
| Lobster | 40 | Lobster pot | 90 |

These are examples, not a complete table: trout/salmon, swordfish, shark, monkfish, cave fish, rocktail, bait use, and spot relocation must be separately extracted. Sources: [free fishing progression](https://wiki.darkan.org/Free-to-play_Fishing_training), [raw lobster](https://wiki.darkan.org/Raw_lobster).

**To implement:** map every target fishing NPC definition to click option -> fishing method -> required tool/bait -> weighted catch entries -> XP -> move/respawn behavior. A successful catch consumes bait only for methods requiring bait. Verify catch rates and NPC IDs with 742 captures.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../fishing.md) · [Research policy](../research-evidence.md)

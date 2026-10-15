# smithing — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical bar smelting recipes

| Bar | Smithing level | Materials per bar | XP |
|---|---:|---|---:|
| Bronze | 1 | 1 copper + 1 tin | 6.25 |
| Iron | 15 | 1 iron | 12.5 |
| Steel | 30 | 1 iron + 2 coal | 17.5 |
| Gold | 40 | 1 gold | 22.5 |
| Mithril | 50 | 1 mithril + 4 coal | 30 |
| Adamant | 70 | 1 adamantite + 6 coal | 37.5 |
| Rune | 85 | 1 runite + 8 coal | 50 |

Iron succeeds about **50%** at level 15 and approaches **80%** at level 45; ring of forging forces success and uses charges. Goldsmith gauntlets modify gold XP to 56.25. Sources: [historical smelting](https://wiki.darkan.org/Smithing_training_pay_to_play), [Smithing](https://wiki.darkan.org/Smithing).

**To implement:** keep smelting ingredients, fail chances, XP modifiers and forging products distinct. Each forged item has a bar quantity and XP that need a separate per-product table; extract actual forge recipes from Hagalaz's existing definitions. Use transactional material/output changes per produced unit.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../smithing.md) · [Research policy](../research-evidence.md)

# agility — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Gnome course candidate XP

| Basic obstacle order | XP |
|---|---:|
| Log balance | 7.5 |
| First obstacle net | 7.5 |
| Tree branch up | 5 |
| Balancing rope | 7–7.5 |
| Tree branch down | 5 |
| Second obstacle net | 7.5–8 |
| Pipe | 7–7.5 as an isolated interaction; some sources show 47 with completion |
| **Full lap** | **86.5** |

Advanced course requires **85 Agility** and yields **725 XP** per completed lap. The preserved specialist page and overview differ in how they attribute the basic lap bonus to the final pipe and a completion reward; use total and capture per-widget XP events rather than hardcoding both conflicting subdivisions. Sources: [Gnome course](https://wiki.darkan.org/Gnome_Agility_course), [Agility](https://wiki.darkan.org/Agility).

**To implement:** use each course's actual obstacle order, collision tiles, movement path and one-time lap state; track skipped nodes and prevent bonus farming. Verify 742 clipping/camera orientation for course movement.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../agility.md) · [Research policy](../research-evidence.md)

## Expanded 2012 datasets and behavioral contract

See [course access levels and lap rewards](../data/researched/agility-course-candidates.csv), [ordered obstacle XP sequences](../data/researched/agility-obstacle-sequences.csv), [shortcuts](../data/researched/agility-shortcut-candidates.csv), and [full routing/failure evidence dossier](deep-agility.md).

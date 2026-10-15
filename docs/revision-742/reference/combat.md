# combat — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Earlier-combat calculation

Source reports base levels by style:

```text
base = Defence + Constitution + floor(Prayer/2)
melee = floor((base + 1.3 * (Attack + Strength)) / 4)
magic = floor((base + 1.3 * floor(1.5 * Magic)) / 4)
ranged = floor((base + 1.3 * floor(1.5 * Ranged)) / 4)
combat = max(melee, magic, ranged)
```

Add `floor(Summoning / 2)` to the numerator where membership and area rules permit; Wilderness may show a base + Summoning extension and only apply it when familiar-related items are present. Cap is **138** in member contexts and **126** without Summoning. Source: [combat level explanation](https://wiki.darkan.org/Calculators/Combat_level).

**To implement:** pin 742 integer rounding and branch behavior with multiple fixture characters (base 3, range/magic pures, 99-all, Wilderness with pouch/familiar). Additionally extract the complete hit-chance/max-hit/attack-speed formulas, attack animations, weapon classes, special energy and NPC weaknesses; combat-level math alone does not make combat complete.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../combat.md) · [Research policy](../research-evidence.md)

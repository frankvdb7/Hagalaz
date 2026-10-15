# prayer — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Prayer candidate values

| Prayer | Level | Effect | Base drain |
|---|---:|---|---|
| Thick Skin | 1 | +5% Defence | 1 point / 1.2 s |
| Burst of Strength | 4 | +5% Strength | 1 point / 1.2 s |
| Rock Skin | 10 | +10% Defence | 1 point / 0.6 s |
| Protect from Missiles | 40 | 100% NPC ranged / 40% player reduction in historical description | 1 point / 0.3 s |
| Protect from Melee | 43 | 100% NPC melee / 40% player reduction in historical description | 1 point / 0.3 s |
| Piety | 70 | Attack +20%, Strength +23%, Defence +25% | 1 point / 0.15 s |

Piety requires **King's Ransom**, **Knight Waves** and **70 Defence** in addition to Prayer. These drain rates assume no prayer bonus. Sources: [prayers](https://wiki.darkan.org/Prayer), [Piety](https://wiki.darkan.org/Piety_prayer).

**To implement:** enumerate all earlier prayer/curses with exclusivity groups, drain factors, effects, unlocks and quick-prayer settings; verify PVP reductions, piercing boss attacks, curses and drain tick scaling from a 742 capture.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../prayer.md) · [Research policy](../research-evidence.md)

## Expanded period data and code analysis

[Curse levels, base drain interval and effect family](../data/researched/ancient-curses.csv) · [Activation and combat-effect investigation](deep-prayer.md)

Verify all exact client IDs/rates before treating secondary research as final revision-742 behavior.

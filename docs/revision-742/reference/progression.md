# progression — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Core skill and hitpoint rules

- Ordinary skills start at level **1**; Constitution starts at **10**, yielding **100 life points** because each base Constitution level represents ten life points.
- Most skill levels cap at **99**; Dungeoneering level cap is **120**. Skills may continue accumulating XP to **200,000,000**.
- Earlier combat training awards Constitution XP during damage dealt; historical description gives approximately **1.33 Constitution XP per ten damage**, subject to activity exceptions.
- Starting combat level is **3**; maximum with Summoning is **138**.

Source: [skills](https://wiki.darkan.org/Skills), [experience](https://wiki.darkan.org/Experience_points).

**To implement:** extract exact per-level cumulative XP thresholds (including 120), revision skill index and UI mappings, XP drop scaling, bonus-XP effects, direct XP grants, and boost restoration from 742 data. The standard exponentially scaled XP table may be calculated, but cross-check canonical thresholds and integer rounding. Track current boosted vs base levels separately.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../progression.md) · [Research policy](../research-evidence.md)

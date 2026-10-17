# pvp — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Wilderness constraints to verify

Combat eligibility is linked to Wilderness depth and the **displayed combat level**, which may differ from the base level when summoning is available. Wilderness can have separate player-vs-player level reporting and item-loss rules. Source: [combat level conditions](https://wiki.darkan.org/Calculators/Combat_level), [items kept on death](https://wiki.darkan.org/Items_Kept_on_Death).

**Required 742 extract:** each region polygon / safe boundary / single-vs-multi tile, level depth, teleblock effect, teleport depth restriction, skull and Protect Item transitions, damage mitigation, target logout rules and loot visibility timers. No exact area-coordinate list or collision test was located in the historical sources here; **do not invent one**.

**To implement:** create area/collision fixtures for border tiles and character levels; reuse existing combat reach checks and character-region state. Distinguish duels, dangerous Wilderness and safe minigames in the drop handler.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../pvp.md) · [Research policy](../research-evidence.md)

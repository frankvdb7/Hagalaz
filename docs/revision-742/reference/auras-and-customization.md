# auras-and-customization — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Historical system constraints

The loyalty aura system and appearance-customization options existed before 742; exact aura inventory, effect tiers, cooldown intervals, token prices, and client appearance bit fields must be read from the target cache. Source: [contemporary loyalty and lodestone announcement](https://wiki.darkan.org/Update%3ALoyalty_Additions_and_Lodestone_Network).

**Extraction checklist:** available aura item IDs and equipped overlays; activation status, duration and cooldown; visibility while hidden/unequipped; cosmetic chathead/appearance transformations; male/female appearance part IDs, skin/clothing palettes, name-change screen variables; persistence schema for cooldown starts (absolute timestamps, not only remaining ticks). Existing Hagalaz aura and design scripts are first-entry reference paths.

**To implement:** use one character aura state, one equipment-effects bridge and existing appearance snapshots. Add tests against dropped connection/relog, duplicate clicks and expiry while offline. Do not assume a new effect is represented by a stat bonus.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../auras-and-customization.md) · [Research policy](../research-evidence.md)

# Dungeon session crafting: 2012 resource inventory and isolation requirements

## Candidate recipe inventory

- [dungeon-fletching-bows.csv](../data/researched/dungeon-fletching-bows.csv): 20 records.
- [dungeon-fletching-shafts.csv](../data/researched/dungeon-fletching-shafts.csv): 10 records.
- [dungeon-fletching-arrows.csv](../data/researched/dungeon-fletching-arrows.csv): 10 records.
- [dungeon-fletching-staves.csv](../data/researched/dungeon-fletching-staves.csv): 10 records.
- [dungeon-fletching-traps.csv](../data/researched/dungeon-fletching-traps.csv): 10 records.

The preserved source at https://wiki.darkan.org/Dungeoneering/Fletching was last edited on 6 May 2012, making it a useful period snapshot. Every listed value is still historical candidate evidence, NOT a verified build-742 item or interface record. Promethium arrow XP is absent in the source and intentionally blank in the CSV. Both ten-step bow tiers and variable arrow shafts are explicit rather than derived from modern tables.

## Session-local authority

The existing Hagalaz.Services.GameWorld/Services/MapRegionService.cs and CharacterService.cs do not establish a full instanced dungeon owner. Start with one fixed captured floor, party identity and a defined leave/finish contract; don't build a generic floor generator first. Normal inventory, bank and persistence must never accept transient dungeon resources as durable character assets. Reuse authoritative item mutation semantics within the session scope, not a second transaction implementation.

## Production behavior

- Branch tiers produce 15 through 65 shafts per branch, not a global 15-shaft rule. Separate woodcutting, knife-based shaft production, bow cutting and bow stringing actions.
- Ten staff constructions have independent Fletching gates. Their later rune imbue requires the historical candidate Runecrafting level two above the relevant Fletching requirement. A catalytic staff is not a source of free elemental runes.
- Dungeon traps require a knife, a suitable branch, and complexity 5 or 6 in the cited source; trap use and bovimastyx ownership are separate world activities.
- Validate actual party/session membership, skill, tool and available source on each step. Commit input/output in one checked mutation, then award stage XP. Reject client attempts to use dungeon recipes outside an authorized live floor.
- On exit, death, logout or restart, no transient resources may migrate into saved normal inventory, bank, equipment or unowned ground items. If verified client behavior supports reconnecting into a floor, restore using ONE documented session owner rather than a second persistent normal inventory.

## Missing content before implementation-ready

Map/landscape assets for the selected floor, 742 widget choices, CS2, item/NPC/object IDs, start/destination tiles, party key-sharing behavior, binds, mobs, bosses, rewards, XP/token formula, PvP boundaries, death/leave persistence and runtime fixtures. Item names in these CSVs are candidate human lookup keys only.

## Regression suite

Two parties cannot use one another's maps, equipment or keys. N branch items create N sets of shaft counts and stage XP; insufficient tool, level or complexity creates none. Duplicate button input produces one accepted task. Exactly once final floor reward, never leak temporary items into character saves or cross-session trading. Crash/logout halfway through crafting does not grant a delayed output or XP. Trace live target-client action and packet updates before parity claims.

[Feature guide](../dungeoneering.md) · [cache extraction](client-cache-evidence.md).

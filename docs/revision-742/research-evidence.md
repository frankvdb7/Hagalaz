# Research evidence and provenance

**Target:** revision 742, early/mid-November 2012. **Audit date:** 15 October 2026.

## Confidence model

1. **Direct revision evidence (strongest):** a recorded 742 client session, opcode/widget trace, cache asset hash and definition dump, or verified behavior reproduced against that build. The companion client repository identifies a pinned 742 cache fixture, but the LFS archive was not downloaded or exhaustively decoded in this research. Exact IDs/ticks and exhaustive rewards therefore remain open.
2. **Repository evidence:** paths and owner candidates from `frankvdb7/Hagalaz` `main`, audited at commit `9c9f8622305d18ca05fa7099848a0cdb01cf09b6`. Source presence only proves some code exists; a documented TODO or empty handler is not complete functionality.
3. **Dated historical material:** period sources and preserved 2012 update notes establish that a feature existed before the target build. These sources may have later edits; they do not by themselves prove every numerical rule.
4. **Later retrospective articles:** useful leads, not an authority for target-version details.

## Boundary and dated features

- [Community client revision chronology](https://rune-server.org/threads/rs2-update-revision-list-23-jul-2002-12-mar-2014-updated-as-of-2014.257103/): reports 742 starting 7 November 2012 and 743 on 21 November 2012. This is a **secondary** date source.
- [Period combat transition description](https://wiki.darkan.org/Evolution_of_Combat): redesign scheduled 20 November 2012. Use to exclude redesigned mechanics, **not** to derive the 742 combat formula.
- [Money pouch](https://wiki.darkan.org/Money_pouch) and [tool belt](https://wiki.darkan.org/Toolbelt): both introduced in December 2011.
- [Lodestone network](https://wiki.darkan.org/Lodestone_Network): introduced March 2012.
- [Runespan](https://wiki.darkan.org/Runespan): present from April 2012.
- [Contemporary banking information](https://wiki.darkan.org/Bank): tabs, deposit controls and 2012 capacity changes; exact entitlement must be fixture/data backed.
- [Daily challenges and Noticeboard announcement](https://wiki.darkan.org/Update%3ADaily_Challenges_%26_Noticeboard): September 2012.
- [Clan citadels launch](https://wiki.darkan.org/Update%3AClan_Citadels_are_Here%21): July 2011.
- [Dungeoneering tool-belt update](https://wiki.darkan.org/Update%3ADungeoneering_and_Shop_Improvements): April 2012.

## Evidence gaps before parity can be claimed

- Download and hash the existing pinned version-742 cache archive, then export feature-relevant definitions and asset indices. See [client/cache evidence path](reference/client-cache-evidence.md).
- Real login/interaction captures for combat, world, item container, widgets, money pouch, teleport and quests.
- Per-skill recipe database audit versus target version and manual tests of formulas/animations.
- Feature-specific persistence/error handling tests across disconnect, world handoff, full inventory and concurrent players.
- Server-run results for the files changed in a future implementation PR.

The feature guides intentionally do **not** invent missing identifiers or copy contemporary behavior into the 742 baseline. Preserve this uncertainty when implementing and replace it with reproducible fixture evidence.

## Verified companion-client source locations

A separately managed revision-742 Java client repository documents a pinned Git LFS cache in `tests/fixtures/cache742/cache.zip` with a SHA-256 sidecar. Its repository docs provide canonical cache, interface, protocol, CS2, map and region evidence ledgers. These are **verified repository-source references**, but not proof that a complete historical gameplay dataset has already been extracted. See [client/cache extraction handoff](reference/client-cache-evidence.md).

## Additional dated 2012 sources and repository findings

- [Archived ancient spell snapshot](https://wiki.darkan.org/index.php?title=Ancient_Magicks&oldid=31700), whose visible last-edit date is **24 October 2012**: period spell requirements, base hit/XP and effects. Rune reagent images remain unresolved in plain-text output.
- [Archived Slayer points snapshot](https://wiki.darkan.org/index.php?title=Slayer_points&oldid=50331), visibly last edited **2 November 2012**: reward store and master points including a bonus-multiplier discrepancy worth investigating.
- The audited Hagalaz entities already provide substantial structured columns for mining, trees, fishing, cooking, firemaking, crafting, fletching, farming, prayer, magic, runecrafting, summoning, Slayer, quests, NPCs and shops. See [entity-level crosswalk](reference/repository-data-crosswalk.md).
- Direct method inspection confirmed Farming uses per-patch tasks, Mining/Fishing re-resolve live entity handles each tick, and Summoning spawn precedes item/point debit in the examined call flow. These are source observations, **not assertions that targeted regression tests have run**.
- [Source conflict and completeness ledger](data/researched/README.md) documents unresolved cases and prevents promoting contested numbers into production.

# Machine-readable historical candidate data — revision 742

The eight per-skill CSV files are **secondary-source candidate data**; they are not an authoritative 742 cache dump. They deliberately exclude item/object/NPC IDs, success chances, respawn timers and UI IDs that were not independently established. Never copy all rows into production without reconciling them with actual revision 742 cache assets and the Hagalaz database.

| Dataset | Source | Notes |
|---|---|---|
| [Mining ores](mining-ores.csv) | [Mining](https://wiki.darkan.org/Mining) | Basic requirement/XP, no rock IDs or respawn |
| [Trees](woodcutting-trees.csv) | [Woodcutting](https://wiki.darkan.org/Wooductting) | Tier/XP, no per-tree probability |
| [Logs](firemaking-logs.csv) | [Firemaking](https://wiki.darkan.org/Firemaking) | Lighting XP, **not** bonfire XP |
| [Smelting bars](smithing-bars.csv) | [Smelting](https://wiki.darkan.org/Smithing_training_pay_to_play) | Standard ingredients and XP, not iron success roll |
| [Crafting runes](runecrafting-runes.csv) | [Runecrafting](https://wiki.darkan.org/Runecrafting) | Regular altar output; rune multipliers omitted |
| [Fletching bows](fletching-bows.csv) | [Fletching](https://wiki.darkan.org/Fletching_boost) | Cutting/stringing separately |
| [Hunter birds](hunter-birds.csv) | [Hunter](https://wiki.darkan.org/Hunter) | First 5 bird trap species |
| [Runespan nodes](runespan-nodes.csv) | [Runespan](https://wiki.darkan.org/Runespan) | XP varies by produced rune for dual-variant nodes |
| [XP thresholds](experience-levels-1-120.csv) | Generated from classic cumulative XP formula | Verify against [experience references](https://wiki.darkan.org/Experience_points) and actual server model |

## Intentional blank fields

- `runecrafting-runes.csv`: Mind rune requirement is blank because period sources conflict (level 1 vs level 2). Verify with matching cache/UI reference.
- `fletching-bows.csv`: basic shortbow requirement is blank due to conflicting historical level descriptions.
- A blank second XP variant for nodes means the cited table reported one XP output, **not** that no additional outcome exists.

## Skill level calculation

With `xpForLevel(1) = 0`, accumulate for each integer level index `i` from 1 through `level-1`:

```text
points += floor(i + 300 * 2^(i / 7))
xpForLevel(level) = floor(points / 4)
```

Examples checked in generation: level **2 = 83**, level **10 = 1,154**, level **99 = 13,034,431**, level **120 = 104,273,167**. The threshold CSV is a computed reference, not an exported client asset; compare it to Hagalaz's existing XP curve before considering replacement.

## Recommended future data format

For production ingestion, reconcile each CSV row to actual item/NPC/object definition IDs and a version-specific cache checksum, plus an evidence classification (`capture`, `cache`, `repository`, or `historical_secondary`). These input tables are deliberately documentation assets, not migrations or configuration automatically loaded by the game server.

## Larger historical dataset pack

See [expanded researched tables](researched/README.md) for **21 additional files with 354 curated rows** (total **48 gameplay CSV datasets** including the nine earlier datasets). These provide many more concrete potion, pouch, ancient spell, construction, Slayer and fishing facts, each with a source URL and evidence tier. The new files are not runtime-ready migrations or comprehensive content dumps; missing IDs and contradictions remain explicit.

The later expansion adds 30 standard prayers, 23 offering items, 23 cooked fish and 13 growable allotment/flower entries. The 742 cache identifiers remain unverified.


## Numeric item and NPC source catalog

The [script registration catalogs](catalogs/README.md) provide source-backed, numeric IDs for a documented subset of items and NPCs; their presence in the target cache has **not** been established. They are not counted among the historical skill-candidate recipe rows, and do not alter the 48 CSV gameplay dataset count.

## Direct source-extracted P1 catalogs

The [numeric content catalogs](catalogs/README.md) now cover item script registrations, potion-dose chains, equipment degradation stages, NPC script and pickpocket registrations, and world-object script IDs/opcode constants. All numeric records carry **repository-source evidence only**; they must not be counted as verified index-16/18/19 fixture definitions or as part of the 705 historical candidates.

## Source-anchored combat data

The [combat source catalogs](catalogs/README.md) contain 49 item-ID special costs, 50 prayer level/drain constants, 20 ancient spell variants and 14 review observations. These are not part of the **705 historical candidate rows** and have not been verified against the pinned 742 binary or a live database.

## Protocol and interface source crosswalks (P1 #10)

[The server/client protocol and UI catalog](catalogs/README.md) now includes 108 reconciled incoming-server opcode definitions, 71 server encoder branch entries, 75 literal widget-state calls (17 contributing files from 25 inspected) and eight implementation risk records. Client 164-opcode incoming and 108-opcode outgoing canonical ledgers remain in the GameClient repository; these are only comparison/ownership data.

The [29-varbit-send source call catalog](catalogs/client-varbit-source-calls.csv) distinguishes 18 literal-ID call sites (14 distinct values) from 11 computed-ID expressions. No index-22 cache definition join has been performed.

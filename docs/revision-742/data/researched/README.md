# Expanded period-data tables — revision 742

These are **curated candidate records**, not live game server configuration. Each row carries a source URL and evidence tag. Source page descriptions can be dated 2012 yet still be wrong, contradictory or affected by updates within the same client revision. **No target cache IDs have been invented.**

## Table inventory

| Dataset | Rows | Main role |
|---|---:|---|
| [Construction rooms](construction-rooms.csv) | 21 | Individual room skill levels and coin costs |
| [House portals](construction-house-portals.csv) | 6 | Portal movement level and fee |
| [Construction plank fees](construction-plank-fees.csv) | 4 | Sawmill price per conversion |
| [Ancient combat spells](ancient-combat-spells.csv) | 20 | Every named combat spell, spell tier, level, base hit/XP and effect |
| [Ancient teleport spells](ancient-teleports.csv) | 8 | Teleport spell level, XP and destination |
| [Herblore recipes](herblore-recipes.csv) | 53 | Ingredients, required level and creation XP |
| [Summoning pouches](summoning-pouches.csv) | 61 | Familiar, charm, spirit shards, tertiary and creation XP |
| [Slayer masters](slayer-master-points.csv) | 7 | Normal, tenth, fiftieth and challenge bonus points |
| [Slayer rewards](slayer-point-rewards.csv) | 15 | Reward shop items, unlocks and point prices |
| [Ground fire vs bonfire](firemaking-method-xp.csv) | 9 | Method-dependent experience |
| [Barbarian fishing](barbarian-fishing.csv) | 6 | Fishing/Strength/Agility level requirements and XP |
| [Fishing familiar bonuses](fishing-familiar-bonuses.csv) | 3 | Fishing boost and foraged catch examples |
| [Ancient curses](ancient-curses.csv) | 20 | Every named curse's level, effect group and historical drain interval |
| [Hunter methods](hunter-methods.csv) | 11 | Methods, levels and required equipment |
| [Hunter trap capacity](hunter-trap-capacity.csv) | 5 | Traps allowed by skill level |
| [Combination rune recipes](combination-runes.csv) | 6 | Pairs, levels, two altar XP values and chance |
| [Cooking meat products](cooking-meat-products.csv) | 10 | Levels, cooking XP and life points healed |
| [Standard prayers](standard-prayers.csv) | 30 | Level, base drain interval and effect family |
| [Prayer offerings](prayer-offering-xp.csv) | 23 | Burial/ash scattering base XP before altar bonus |
| [Cooked fish](cooking-fish.csv) | 23 | Cooking level, historical burn threshold, XP and life points restored |
| [Allotments and flowers](farming-allotments-and-flowers.csv) | 13 | Seed level, growth cycle/time, planting and harvesting XP, protection |

**Total: 354 candidate rows across 21 new files.** Earlier [datasets](../README.md) add representative skill rows and the computed 1–120 level XP curve. The [41-feature requirements matrix](../../reference/feature-completeness-matrix.csv) identifies what is still missing before full content coverage.

## Evidence classification

- `historical_2012_pending_cache`: period page independently dated before the major November combat transition; still needs comparison with the **exact** matching client/cache and Hagalaz definitions.
- `secondary_pending_cache` / `historical_secondary_pending_cache`: secondary-source statement; a date-appropriate article alone is insufficient to pin full revision-specific values.
- A blank numeric cell means **unknown/contested in the cited source**. Never interpret it as zero.
- Prices in these CSVs are **fixed historic fees or point costs** where explicitly stated, not dynamic market prices.
- The 53 Herblore entries list a base ingredient and other components **as prose**, not normalized item IDs; some are multi-step/quest potions and cannot be created by naive two-item combine.
- The Summoning dataset includes special/faction charm variants, but **does not list all later-level familiars** or any item/NPC/scroll IDs. The single missing creation-XP cell is intentional.
- `ancient-combat-spells.csv` stores base XP/hit values. Image-only rune costs are not transcribed without identifiable rune types.
- `barbarian-fishing.csv` reports secondary Strength/Agility XP with the preserved text; verify which stat gains it before applying.
- `slayer-master-points.csv`: tenth/fiftieth points are listed explicitly to avoid overwriting the Mazchna/Achtryn source exception with a generic formula.

## Primary period source snapshots

- [Ancient spell list, archived 24 October 2012](https://wiki.darkan.org/index.php?title=Ancient_Magicks&oldid=31700). The page's "last edited" stamp was visible in the source. It is a dated period reference, not a cache trace.
- [Slayer points, archived 2 November 2012](https://wiki.darkan.org/index.php?title=Slayer_points&oldid=50331). Includes the master and reward shop tables, and pre-target co-op Slayer.
- [Construction reference](https://wiki.darkan.org/Construction).
- [Herblore potion calculator](https://wiki.darkan.org/Herblore_calculator).
- [Summoning pouch reference](https://wiki.darkan.org/Summoning_pouches).
- [Fishing](https://wiki.darkan.org/Fishing).
- [Firemaking methods](https://wiki.darkan.org/Pay-to-play_Firemaking_training).

The source licenses vary; these CSVs contain short **structured factual assertions**, not copied original prose. Preserve source attribution and review the compatibility of any future full bulk content import.

## Known source inconsistencies requiring fixture evidence

| Area | Conflict | How to resolve |
|---|---|---|
| Construction | Text says 23 room types, accessible table lists only 21; starter location described inconsistently | Compare full 742 house UI, room definitions and portal map |
| Firemaking | Ground-fire magic log XP appears as 303.8 in one place and 308.8 in another | Validate actual target skill XP; no value in the method-comparison row |
| Herblore | Attack potion level 1 on a descriptive page but level 3 in the calculator | Use 742 level check and skill guide |
| Slayer | 5x/15x general streak explanation conflicts with Mazchna's printed tenth/fiftieth figures | Capture task-10 and task-50 rewards or find a dated correction |
| Fletching | Basic unstrung shortbow requirement differs between summaries | Preserve earlier blank entry pending 742 source |
| Summoning | One introductory prose claim about wolf pouch XP differs from the tabular 4.8 value | Evaluate exact `CreatePouchExperience` in corresponding 742 definition |
| House access | Room styles, portal relocation and first house placement are not necessarily the same stored concept | Separate template/default location from move/appearance fees |
| Script values | Wiki casting reagent icons are not unambiguously identified in text-only render | Decode verified 742 spell definition/item ID mapping before filling reagent counts |

## How an implementation agent should consume these tables

1. Take the desired feature row; inspect the **actual** existing EF/DTO/script definitions in [source-to-runtime crosswalk](../../reference/repository-data-crosswalk.md).
2. Decode the pinned client cache, recording checksum, top-level index, group/file ID, item/NPC/object ID and local semantic name.
3. Join on exact *validated* item/object/name and reconcile conflicting rows; mark each field as accepted/rejected/uncertain with evidence.
4. Treat each row as a potential domain definition, not an in-memory runtime event. Introduce no new repository until the existing entity and repository are checked.
5. Implement one action path with tests: eligibility, tool/input items, cost, repeat/interrupt, output, XP, UI packet, persistence and concurrency.
6. Only promote data to authoritative definitions when an executable fixture and repeatable tests demonstrate exact revision behavior.

## Test-oriented data integrity checks

- Each CSV has an explicit source and evidence per row; numeric levels are positive except intentionally missing unknowns.
- Input item names are not exact numeric item IDs; `Name` is never used as a persistence key.
- Required item counts cannot silently be coerced from missing cells to 0; creation must fail closed if an ingredient mapping is unresolved.
- All CSV record counts, unique keys and blank values should be checked after importing to a provisional dataset.
- For inherited fields `ItemId`/`NpcId`/`ObjectId`, validate the right cache index (19/18/16) and bit-shard calculation.

See [client/cache extraction guide](../../reference/client-cache-evidence.md) and [implementation constraints](../../implementation-conventions.md).

## Additional historical research links

- [Ancient curses period table](https://wiki.darkan.org/Ancient_Curses) — 20 curse effects; **seconds per point** at zero prayer bonus, not a per-second drain amount. See [drain and activation dossier](../../reference/deep-prayer.md).
- [Combination rune explanation](https://wiki.darkan.org/Runecrafting) — 50% success except charge-limited Binding necklace, pure essence and opposing talisman. See [crafting and rune-consumption dossier](../../reference/deep-runecrafting.md).
- [Hunter methods and trap rules](https://wiki.darkan.org/Hunter) — general five-trap scale with one-deadfall exception; [placement state and safety](../../reference/deep-hunter.md).
- [Historical cooking values](https://wiki.darkan.org/Cooking) — meat dish subset, spit vs fire/oven restrictions and burn rules; [actual Hagalaz task behavior](../../reference/deep-cooking.md).

All five new CSV files use historical secondary evidence only. The term `seconds_per_prayer_point` describes a *base drain interval*, not a point rate; do not transpose the numerator/denominator in production.

## Further mechanical constraints

- The Prayer source describes **ten prayer points per Prayer level** for the period after September 2011. The listed `seconds_per_prayer_point` values are base intervals, before modifiers. A period secondary formula is `actual interval = baseInterval * (1 + prayerBonus / 30)`. Do not implement it without verifying current Hagalaz tick scaling and the target 742 prayer-bar units. [Prayer](https://wiki.darkan.org/Prayer).
- The [fish Cooking source](https://wiki.darkan.org/Cooking) prints data headed "Experience (Bonfire)". Check whether XP is actually independent of cooking source for the selected build; treat these figures as candidates, not separate source effects. Some fish have **never** or unknown stop-burning levels and the CSV preserves these as literal values rather than coercing to numeric zero.
- [Farming seed sources](https://wiki.darkan.org/Seeds) express cycles in *wall-clock minutes*, with possible disease/failed cycle repeats; total time is a minimum, not always an exact harvest timestamp. Harvest XP may be **per item** or per check/harvest depending on crop type: verify the proper award unit.

## Further content extraction: forging, crafting, theft, agility (15 October 2026)

The following **223 additional candidate records in 8 CSVs** were extracted from 2012-compatible preserved source tables. These extend, rather than replace, the earlier **354 records**. Direct build/cache identity and numeric IDs remain unverified. Sources and evidence grade are present on every row.

| File | New rows | Use |
|---|---:|---|
| [smithing-forging-products.csv](smithing-forging-products.csv) | 72 | Source-linked historical candidates; IDs and runtime traces unverified |
| [crafting-gold-jewellery.csv](crafting-gold-jewellery.csv) | 28 | Source-linked historical candidates; IDs and runtime traces unverified |
| [crafting-gem-cutting.csv](crafting-gem-cutting.csv) | 9 | Source-linked historical candidates; IDs and runtime traces unverified |
| [thieving-pickpocket-targets.csv](thieving-pickpocket-targets.csv) | 22 | Source-linked historical candidates; IDs and runtime traces unverified |
| [thieving-locked-doors.csv](thieving-locked-doors.csv) | 10 | Source-linked historical candidates; IDs and runtime traces unverified |
| [agility-course-candidates.csv](agility-course-candidates.csv) | 13 | Source-linked historical candidates; IDs and runtime traces unverified |
| [agility-obstacle-sequences.csv](agility-obstacle-sequences.csv) | 38 | Source-linked historical candidates; IDs and runtime traces unverified |
| [agility-shortcut-candidates.csv](agility-shortcut-candidates.csv) | 31 | Source-linked historical candidates; IDs and runtime traces unverified |

**Important:** these are source-derived *candidate facts*, not an import script or a complete historical implementation. An exact NPC's loot probabilities, a forge product's item ID, a course obstacle's map coordinates and a widget's click component cannot be derived safely from its display name. Empty fields deliberately mean **no measured 742 value**. The [four new in-depth reference dossiers](../../reference/README.md) map the next evidence and regression work.

### Internal consistency and conflict ledger

- Forge XP is XP per **forging action**; quantities 10 or 15 indicate the number of output items created, not a multiplication of the action XP. Dart tips are gated by *The Tourist Trap*.
- Jewellery and gem cutting are separate XP actions; mould/chisel are reusable tools where applicable. Base jewellery requires gold bars; bracelets, neckwear and amulets are distinct product/item definitions.
- Thieving's multi-loot requires higher Agility and Thieving levels but provides only base XP per theft; success chance and loot weights remain blank, not zero.
- Agility sources disagree about some last-obstacle/bonus allocations and basic Barbarian obstacle subtotals. The 742 fixture must decide packet-event granularity. The recorded full-lap values are candidates, not arbitrarily summed runtime rewards.
- The source snapshots were last checked **15 October 2026**; `historical_2012_pending_cache` means preserved period text, **not** a direct 742 capture.

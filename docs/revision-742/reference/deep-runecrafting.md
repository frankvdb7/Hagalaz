# Runecrafting — altars, essence, multi-runes and combinations

## Standard runes and per-essence experience

Existing [ordinary altar reference table](../data/runecrafting-runes.csv) includes level, essence requirement and XP per essence. Rune multiplication at high skill **increases rune output, not XP per essence**. Hagalaz already has `Hagalaz.Game.Scripts/Skills/Runecrafting/Runecrafting.cs`, whose `GetMultiplier` walks `LevelCountMultipliers` backwards, skipping `-1` markers, returning `2+i` when current effective Runecrafting level meets that entry, otherwise 1. This is an **observed repository formula**, so any new feature must use/verify this existing implementation, not add a second multiplier function.

`SkillsRunecraftingDefinition` currently contains `AltarId`, `RuinId`, `PortalId`, `RiftId`, `RuneId`, `TalismanId`, `TiaraId`, `RequiredLevel`, `Experience`, `LevelCountMultipliers`, `AltarLocation`, `RuinLocation`, `RiftLocation`.

### Six combination products

[Combination reference CSV](../data/researched/combination-runes.csv) gives required levels, constituent elemental rune pairs and separate XP values for the lower-/higher-level altar. Historical [Runecrafting explanation](https://wiki.darkan.org/Runecrafting) specifies a **50% base success rate**, increased to **100%** by a worn Binding necklace; a Binding necklace lasts **15 uses**. Crafting consumes one opposing talisman unless the Lunar **Magic Imbue** effect is active; exact quest/rune requirements for Magic Imbue must be sourced separately. Failure consumes qualifying materials without awarding successful output, but whether XP accrues on failure must be verified with a target fixture. Treat the necklace's charge ownership as uncertain: historical prose describes a character-based counter, whereas ordinary equipment might store charges on the item.

### Crafting transition contract

1. Identify the current altar by its actual revision-742 object ID and live location, including instance/plane restrictions.
2. Verify level and ordinary/pure essence eligibility, selected combination ingredients, access item (talisman/tiara/staff), and number of essence actually in inventory; noted and pouch-held essence cannot be crafted before being moved into inventory.
3. For normal rune crafting, compute quantity using the *existing* level-threshold multiplier; debit essence and add output in a checked inventory operation, awarding XP per essence.
4. For combination crafting, evaluate each attempt and necklace state with exact source probabilities and resource costs. Avoid consuming an extra talisman for each essence if the actual action consumes it once per interaction.
5. Award XP and refresh outputs/configs only after the inventory mutation succeeds. If the item stack cannot fit, apply the same item-conservation rule as all other item activities.
6. Map outgoing client widget/CS2 and entering/leaving ruins/portal transport to `Altar.rsobj.cs`, `Ruins.rsobj.cs`, `Rift.rsobj.cs`, `Portal.rsobj.cs`.

### Rune-use side effect

Combination runes can satisfy two elemental rune requirements for the same spell. Some historical references state priority rules that can consume combination stacks before simple runes. Check `Magic.HasRune`, `Magic.RemoveRunes` and `GetEffectiveRuneAmounts` for one consistent reservation/consumption algorithm; tests need mixed simple, elemental staff and combination rune supplies.

## Missing reference material

Exact rune, essence, talisman, tiara, pouch and altar object IDs; all multiplier thresholds, pouch capacities, Abyss travel/penalties, ZMI/Ourania randomized table, Magic Imbue visual/config state, teleport back coordinates and unlock requirements. These must come from the pinned cache and Hagalaz definitions. Do not claim to implement an altar simply because a runecrafting record exists.

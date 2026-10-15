# Crafting: item-transform families and jewellery reference — revision 742

## Candidate data corpus

- [28 jewellery candidates](../data/researched/crafting-gold-jewellery.csv) = seven gem tiers including plain gold × ring/necklace/bracelet/amulet. Each contains the required Crafting level, XP, mould, bar and gem counts; level/XP table comes from the [historical Crafting article](https://wiki.darkan.org/Crafting).
- [Nine gem-cutting candidates](../data/researched/crafting-gem-cutting.csv), including opal, jade and red topaz. The last three have failure/crushing behavior; the probability is not established.
- Amulets produced at a furnace are *unstrung*. Stringing requires wool and awards a separate **4 XP**; spinning wool into ball of wool separately awards **2.5 XP** in the cited reference. Do not give those rewards while forging.
- Jewellery *enchanting* belongs to Magic with its own input, spell/runes, level check and item output. It does not happen as part of the furnace recipe.

## Existing Hagalaz implementation seams

`Hagalaz.Game.Scripts/Skills/Crafting/CraftingSkillService.cs`, `CraftingItemScreenOperations.cs`, `JewelryTask.cs` and `Hagalaz.Services.GameWorld/Services/CraftingService.cs` already host feature logic. EF separates gems, jewellery, leather, silver, pottery, spinning and tanning. This is intentionally **not** a proposal to unify all production types under a new DSL or polymorphic recipe engine.

## Jewellery/cutting action contract

1. Validate the exact selected furnace/crafting interaction and 742 choice component. Join that key to one verified `SkillsCraftingJewelryDefinition` row and output item definition.
2. Distinguish **presence of the mould** (inventory or permitted tool belt) from **consumption of gold bar and gem**. Never subtract a reusable mould or chisel on a normal success.
3. Re-check recipe, level, item instances and inventory capacity per batch iteration; commit each source-to-result replacement without an intermediate state that can be duplicated by re-entry.
4. Award jewellery XP only on actual successful furnace output; award gem-cutting XP only on successful transformation; preserve crush outcomes separately.
5. Stringing, spinning, silver casting and pottery have different interfaces, sources and material semantics; preserve the existing specialized task ownership and keep their tables separate.
6. On invalid choice, changed component, missing gem, player movement, input moved out of its slot, full destination, source disappearance or logout: no free output or XP.

## Source disagreements and missing fixture artifacts

The period overview provides levels and XP but not matching numeric item IDs, noted item variants, charge/attribute preservation, cut failure probabilities, animation cadence or the version-specific component click options. The existing [feature reference](crafting.md) flags a conflicting bowstring level—do not silently resolve it from the generic table.

## Required deterministic tests

Parameterized test all seven gem tiers and four mould types with amount 1; simulate exactly N inputs and an interrupted N+1 batch; test no output with a nonmatching mould, two click handlers racing, changed selected item during animation, inventory full and tool-belt-only chisel/mould where available; verify item conservation and exactly-once XP. Add live client traces for every distinct recipe screen and menu action.

[Implementation guide](../crafting.md) · [Cache evidence](client-cache-evidence.md).

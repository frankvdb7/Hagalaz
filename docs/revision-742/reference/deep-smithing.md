# Smithing: forgery, smelting and resource conservation — revision 742

## Source-derived data

- [72 forging product candidates](../data/researched/smithing-forging-products.csv): 12 canonical products across six base metals; each row provides a level, bars consumed, output count and XP. The forge table is a [24 October 2012 snapshot](https://wiki.darkan.org/index.php?title=Calculator:Standard_smithing&oldid=37329). **These are not cache item IDs or server row IDs.**
- [Smelting bars](../data/smithing-bars.csv): distinct level, ore recipe and smelting XP. [Period training reference](https://wiki.darkan.org/Pay_to_play_smithing) describes the iron success roll and the ring-of-forging exception.
- Special recipes must stay separate: steel cannonballs require an ammunition mould and yield four per steel bar with their own XP; high-value quest gold objects are *not* generic metal forging recipes. Crossbow bolt sets, arrows, nails and darts yield multiple products per action.
- Goldsmith gauntlets change gold-smelting XP; do not apply a generic multiplier to all forging or gold jewellery.

## Repository ownership (check live code before editing)

`Hagalaz.Services.GameWorld/Services/SmithingService.cs` serves recipes, `Hagalaz.Game.Scripts/Skills/Smithing/SmeltTask.cs` and `ForgeTask.cs` perform repeated actions, and `SmithingInterface.rsi.cs` handles forging choices. Existing EF definitions and their provider must be read before adding data or a new repository.

## Per-iteration contract

1. Resolve selected recipe by the exact 742 interface component and product mapping; reject a forged selection that does not belong to the displayed recipe set.
2. Check active source object/furnace/anvil identity and reachability; real skill level, quest gate, hammer or mould and the correct count of each ore/bar.
3. Each repeated tick *re-checks* all preconditions. Resolve an iron-smelting chance, ring-of-forging charges, bonus XP and the selected product count **for that one iteration**.
4. In one authoritative item-container mutation consume raw materials or bars, replace the output items and update charges as relevant. Apply XP **only after** the iteration's successful outcome; iron failure consumes ore but creates no bar according to the verified failure rule.
5. Advance completion counter only for completed iterations, stop on requested count, item depletion, source invalidation, movement, death, interface closure or logout. The task must not batch-consume on start and must not produce after cancellation.
6. Publish the existing inventory, XP and widget updates once per accepted iteration; avoid adding an independent skill production coordinator.

## Conflict and parity gates

- The source table's bronze/iron/steel/mithril/adamant/rune product levels are candidates. The 742 forge widget may expose products differently; the `product` string is never sufficient to resolve a persistent item.
- The main smelting reference describes iron success rising with level and ring charges but does not pin an exact probability function for every intermediate level. Leave that formula open.
- Confirm whether a tool-belt hammer counts as the tool in this build; never require a physical hammer solely because an older recipe article lists one.
- Exact item IDs, slot-level crafting UI IDs, sequence IDs, action cadence, source object IDs, full equipment requirements and material availability need cache plus live trace.

## Acceptance fixtures

Cover 1/2/3/5 bar costs with exactly enough/one fewer materials; full inventory with a product stack that can reuse an input slot; bolt/dart/nail/arrow multiples; recipe switch mid-batch; ring depleting on a smelt; free XP from a failed or canceled action; two simultaneous forge requests; repeated click with stale selected recipe; server save/reload after a produced unit. Compare resource and XP ledgers before and after **every** iteration.

[Implementation guide](../smithing.md) · [Evidence and cache handoff](client-cache-evidence.md).

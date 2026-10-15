# Gathering: repository-specific behavioral model

## Mining

`MiningTask` resolves a live `EntityHandle<IGameObject>` each tick via `IEntityService.TryResolve`; if gone/disabled, it cancels. Its chance roll uses `RandomStatic.Generator.NextDouble()` and invokes a completion callback; every fourth tick it replays the pickaxe animation. Therefore, adding historic ore tables belongs in existing `MiningService` definitions and this completion callback, **not** another harvesting scheduler.

Historical [ore table](../data/mining-ores.csv) supplies some levels/XP; the server EF data already has distinct ore XP, respawn time, exhausted object ID, harvest chance and tool animation. Extract **all 742 rock object variants**, ore item identities, max/depletion behavior, tool requirements/attack-to-equip rules and exact chance interpretation. Verify whether `RespawnTime` is ticks or seconds by finding its consumer.

Critical tests: changing rock object identity mid-task cancels; two miners race at depleted rock; output capacity full; wrong tool tier; animation cancellation; depleted rock restoration and region reload.

## Woodcutting

`SkillsWoodcuttingTreeDefinition` ties tree ID + stump ID + log ID; `SkillsWoodcuttingLogDefinition` stores fall chance, harvest chance and respawn. These probabilities are **different reasons to roll**. Compare to existing `WoodcuttingSkillService` and `WoodcuttingTask` before changing one. Different tree varieties can share a log or vice versa; key tree object identity explicitly.

[Tree data](../data/woodcutting-trees.csv) covers representative species; still missing individual object IDs, alternative tree appearances, logging area exceptions, animation/tool combinations and high-tier trees.

## Fishing

`FishingTask` resolves a live spot handle each tick; it cancels if NPC is missing, outside the performer's viewport or moving, then rolls catch chance. Its animation repeats on every fourth tick. Do not replace this with generic stationary-object resource code.

`SkillsFishingSpotDefinition` distinguishes `ClickType`, `ToolId`, `BaitId`, catch/exhaust chances and minimum level; `SkillsFishingFishDefinition` connects an item and weighted probability to a spot. The target menu **option determines method**; a net/bait spot cannot randomly use both methods simultaneously.

Historical rod fishing consumes **one bait per successful catch**; fly fishing uses one feather; special colored feathers may change catch. Barbarian hand-fishing has independent skill requirements and secondary Strength experience, while Agility double-catch does not double Fishing XP. [Fishing source](https://wiki.darkan.org/Fishing).

Build fixture combinations:
- Spot + net, spot + rod/bait, lure/feather, cage/harpoon, quest-gated spot.
- Empty supplies before start vs supplies depleted mid-task.
- Weighted fish roll at different required levels (ineligible fish must never land).
- NPC moves during tick, region swap, line-of-sight invalidation and disconnect.
- Familiar foraged fish and Agility double-catch are separate award causes, not implicit ordinary-catch multipliers.

## Cooking and Firemaking

Cooking uses **raw->cooked/burnt** definitions, and eating uses a separate `SkillsCookingFoodDefinition` with heal and leftover item. A cook roll must not use `HealAmount` to compute XP. Source-specific burn thresholds need explicit 742 evidence; `StopBurningLevel` is one available storage field.

Firemaking's existing single-log definition has `FireObjectId` and `Ticks` but no documented bonfire mode. Ground-fire lighting and adding logs to a **shared bonfire** have different XP; see [candidate comparison](../data/researched/firemaking-method-xp.csv). The archived source conflicts for **magic logs** (303.8 vs 308.8 ground XP), so the new CSV deliberately leaves that value blank until a 742 fixture settles it.

## Acceptance invariant for every gathering action

```text
resolve live target -> validate identity/location/skills/equipment/inventory ->
schedule existing tick task -> revalidate live target ->
commit exactly one item/currency mutation -> award configured XP ->
publish animation/object/UI update
```

A roll succeeding is not itself a guarantee the output fits. If destination capacity or a required item check fails, no reward is generated. Never change object/resource state before confirming the corresponding item mutation.

# Revision 742: gathering skills and persistent progression — implementation audit

> Scope: Woodcutting, Mining and Farming. This is a **source-backed implementation inventory**, not a claim that all rates or IDs have been verified against revision 742. Keep client/cache IDs separate from semantic gameplay rules. Do not substitute current live-game values for historical revision data.

## Confirmed server behavior and ownership

| Feature | Existing owner | Confirmed behavior | Verification still needed |
| --- | --- | --- | --- |
| Woodcutting action | `Hagalaz.Game.Scripts/Skills/Woodcutting/WoodcuttingSkillService.cs` | Resolves a live tree handle; retrieves log/tree/hatchet definitions and object loot; validates required level, hatchet in inventory/equipment and free inventory space; computes a chance; awards configured XP when loot is awarded; rolls depletion and can transform tree to stump | Complete tree/object ID matrix, per-tree XP, chop probability and depletion, stump/respawn timings, tool/animation IDs and special trees |
| Woodcutting cycle | `Hagalaz.Game.Scripts/Skills/Woodcutting/WoodcuttingTask.cs` | Tick-based attempt using a passed chance; cancels for missing/disabled tree or creature interruption; queues tool animation every fourth tick | Verify exact cycle timing, interruption semantics, animation updates and potential double-award edge cases |
| Mining tool selection | `Hagalaz.Game.Scripts/Skills/Mining/Mining.cs` | Picks highest-required-level accessible pickaxe, provided it is equipped or in inventory | Tool ID/requirement table and all conditional tool variants |
| Mining cycle | `Hagalaz.Game.Scripts/Skills/Mining/MiningTask.cs` | Tick-based attempt against live object handle; cancels when object disappears/is disabled or player is interrupted; queues configured pickaxe animation every fourth tick | Ore/rock list, ore XP, failure/respawn probability, resource depletion, special rocks, mining guild effects |
| Farming character state | `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Farming.cs` | Owns per-character patch task dictionary keyed by patch object ID; creates patch tick tasks, queues them on the character, hydrates/dehydrates persisted patch states | Full crop growth stage data, growth cycles, disease/cure, yield, compost, protection, weather/season assumptions, offline progression |
| Farming script routing | `Hagalaz.Game.Scripts/Skills/Farming/Patch.rsobj.cs` | Forwards object option 1–4 clicks and item-on-patch interactions to the farming skill service; refreshes a character-specific patch upon rendering | Every patch/type ID, contextual option order, client varbit and object transformation rules |

## Minimum reproducible data contract for each gathering node

Record a separate row per **revision-specific node variant**, not merely per skill name:

- `revision`, `skill`, `objectId`, `objectState`, `locationOrSpawnSource`.
- `requiredLevel`, `acceptedToolIds`, `rewardItemId`, `rewardQuantity`, `experiencePerSuccess`.
- `actionCycleTicks`, `successChanceParameters`, `depletionRule`, `respawnRule`.
- `animationId`, `graphicsId`, `soundId`, `startAndSuccessMessages`, `interruptions`.
- `source`, `sourceRevision`, `evidenceType`, `confidence` and `notes`.

Do not fill unknown numeric fields with current-game assumptions. Use `unverified` until cross-checked against cache data, local seed tables, scripts, a contemporaneous capture or another revision-matched source. Probabilities also need tested formula inputs and boundary checks, not just an anecdotal average.

## Farm-patch state contract

Capture by patch archetype: patch object IDs (all visual states), seed/item requirements, minimum level, planting/harvesting XP, growth stage count, stage interval, watering support, compost effects, disease/death rules, protection payment, harvest yield formula, replanting/clearing rules, associated varp/varbit and per-player visibility. Separately validate offline elapsed time after reconnect: the server's persisted patch state is authoritative, and rendered objects are its view.

## Character progression prerequisites

Before declaring any skill complete, inspect the shared statistics owner for XP thresholds, virtual/current/boosted level distinction, boosts and drains, cap behavior, client update packet mapping, level-up effects, persistence, quest rewards and any experience modifiers. Skill scripts should call existing statistics/inventory/loot services rather than creating parallel XP or item state.

## Targeted checks to add before expanding content

1. Cancel an active gathering task when its world object disappears or is replaced; ensure no subsequent loot or XP.
2. Interrupt gathering mid-cycle; ensure no next-cycle action or duplicate event subscriptions.
3. Validate inventory-full behavior both at initial interaction and on every successful harvest; XP must correspond to actual awarded items.
4. Verify tool choice when the strongest eligible tool is in inventory, equipment, neither, or level-ineligible.
5. Verify two players harvesting one depleting world node: exactly one authoritative world-state transformation, with valid individual rewards.
6. Hydrate multiple farming patches and resume growth without duplicating queued tasks or advancing already-completed stages.
7. Validate per-player farming patch visual state so one player's growth state is not incorrectly exposed to another.

## Investigation order

1. Inspect seed/DTO tables behind `IWoodcuttingService`, Mining service definitions, farming services and `FarmingPatchTickTask`; export existing rows with provenance.
2. Extract the revision-742 object, item, animation and varbit identifiers from the actual cache and reconcile identifiers against script handlers.
3. Measure and document action cycle, depletion and respawn rules at source; write deterministic regression tests for boundaries.
4. Complete all gathering skill data tables, then move to production skills and remaining progression systems in separate feature documents.

## Scope / non-goals

This pass does **not** alter runtime game logic, invent historical drop or XP tables, or refactor skill systems. Existing behavior is evidence of Hagalaz implementation, **not** independent proof of revision accuracy. Subsequent docs should link exact cache extraction scripts or dataset snapshots and record measured values before marking a feature revision-verified.

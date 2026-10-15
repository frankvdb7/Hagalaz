# Summoning: data joins, familiar lifecycle and failure boundaries

## Period pouch data

[61 reference pouch recipes](../data/researched/summoning-pouches.csv) provide level, familiar, shard count, charm color, tertiary input and (where confirmed in source) pouch creation XP. These **do not** include validated item/NPC/scroll IDs or familiar combat scripts. The source has a conditional formula for pouch high-alchemy value and a 70% shard-recovery conversion at a specific NPC. [Historical pouch source](https://wiki.darkan.org/Summoning_pouches).

Ordinary pouches use a blank pouch + spirit shards + one colored charm + tertiary component. One pouch may be converted into special-ability scrolls. The desired familiar's combat level, storage slots, point drain, lifetime and scroll effects are **additional independent datasets**.

## Actual Hagalaz implementation crosswalk

- `SkillsSummoningDefinition`: `NpcId`, `PouchId`, `ScrollId`, `ConfigId`, `SummonSpawnCost`, `SummonLevel`, `SummonExperience`, `CreatePouchExperience`, `ScrollExperience`, `Ticks`.
- `Hagalaz.Game.Scripts/Skills/Summoning/SummoningSkillService.cs` resolves a pouch to definition, verifies `Area.FamiliarAllowed` and `SummoningAllowEvent`, confirms the inventory **instance slot**, and tests level/points. It then spawns NPC with an attached familiar script, **then** removes the pouch and damages summoning points.
- Pouch ingredient validation calls `ItemTypeLogic.GetCreateItemRequirements(itemDef)`; ingredient definitions therefore may exist in **item metadata**, not in the `SkillsSummoningDefinition` entity. Trace the actual metadata/creation operation before proposing a new ingredients table.
- `FamiliarCharacterScript` and `FamiliarHydrator`/`FamiliarDehydrator` implement save/restore paths for familiar and carried items.

## Critical ordering question (investigate, not asserted defect)

The summon code shown above spawns before it consumes the pouch. If spawn succeeds but item removal fails, or callbacks are invoked twice concurrently, this may create a familiar without paying its cost. Check `AttachFamiliar`, NPC spawning rollback, available inventory mutation guards and execution serialization. Use **one owner and one checked spawn/consumption lifecycle**, not a speculative generic compensator.

## Required familiar state

NPC source ID and live entity handle; original character owner; summon tick/expiry; attack/defence policy; summoned point debit/drain; active special move/scroll state; follow distance; carried item container; zone permissions; drop/teleport behaviors; persisted summon and inventory state.

## Interaction acceptance tests

1. Summon valid pouch -> NPC and points/item change exactly once, visible to both server and 742 client.
2. Invalid zone/level/points, stale item slot or duplicate request -> no spawn/no debit.
3. Client disconnect while NPC is spawning; logout and reconnect with filled beast-of-burden bag -> same items and one familiar.
4. Dead familiar versus manual dismissal versus timer expiry; beast-of-burden items cannot vanish when both bank/rewards are full.
5. Scroll invalid target/insufficient points/empty scroll -> no effect/cost; successful scroll charge exactly once.
6. Swapped familiar during active combat or minigame boundary -> correct restriction and atomic transfer/retirement.
7. Pouch creation batch in full inventory; exact shard and charm removal, item metadata required ingredients, XP, and scroll output quantities.

**Still missing** full combat/skill bonuses and familiar restrictions, pouch + scroll IDs from cache, 742 cast graphics, creation/spawn animation, exception/rollback semantics and familiar-specific inventory capacity.

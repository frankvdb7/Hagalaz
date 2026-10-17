# Revision 742 Thieving: stall, chest and runtime reachability audit

**Status:** literal Hagalaz source definitions and runtime owner analysis. This supplements [the original Thieving reference](reference/deep-thieving.md) and the already-available [numeric pickpocket catalog](data/catalogs/npc-pickpocket-source-rules.csv). No database export or cache decoding was performed.

## Source configuration inventory

[Stalls/chests source CSV](data/catalogs/thieving-stall-and-chest-source-definitions.csv) extracts **23 configured entries** from `Hagalaz.Game.Scripts/Skills/Thieving/Thieving.cs`, including exact declaration ordinal, level, success XP, respawn ticks, NPC owner sentinel, emptied-object transformation and source line. Of these **12 entries carry at least one literal object ID** and **11 have an empty ID array**, which is faithfully represented by an *empty* `object_ids` field rather than an invented numeric object ID. Each row is marked `repository_source_unverified_742`.

The entries comprise **18 stall types and 5 chests**. `Stall.rsobj.cs` explicitly registers 13 numeric IDs across the 12 source entries with IDs. Its `Initialize()` method searches the source `StealDefinition.GameObjectIDs` arrays and **cannot match** a definition with no IDs. These 11 entries are therefore **not reachable through the current `Stall` registration/lookup path**; their presence in a static table does not mean those stalls are functional in production.

| Source entry | Declared IDs | Required level | XP | Configured respawn ticks | Notes |
| --- | --- | ---: | ---: | ---: | --- |
| Vegetable stall | 4706, 4708 | 2 | 10 | 2 | Special empty object 634 |
| Baker's stall | 34384 | 5 | 16 | 4 | Normal empty object 34381 |
| Crafting stall | **unknown** | 5 | 16 | 17 | Source array is empty |
| Silk stall | 34383 | 20 | 24 | 8 | Declared registered |
| Wine stall | 14011 | 22 | 27 | 25 | Declared registered |
| Spice stall | 34386 | 65 | 81.3 | 133 | Declared registered |
| Gem stall | **unknown** | 75 | 160 | 300 | Source array is empty |
| Gold chest | 59731 | 80 | 130 | 5 | Explicit -1 empty-object sentinel |

The configured tick counts are **server units**, not independently observed wall-clock delays. A [period bakery stall](https://wiki.darkan.org/Bakery_Stall) description was last edited on 13 October 2012; [period market reference](https://wiki.darkan.org/Ardougne_market) and [spice stall reference](https://wiki.darkan.org/Ardougne_Spice_Stall) help contextualize rewards, NPC reactions and stall restoration. **Do not substitute durations from modern pages for verified 742 tick values.**

## Execution path already implemented

1. `Stall.OnCharacterClickPerform` receives option 1/2, interrupts current character activity, and queues `Thieving.Steal`.
2. `Thieving.Steal` checks a local `ThievingStallState`, a standard region object with matching ID, an async loot-table lookup, required level, and `FreeSlots >= lootTable.MaxResultCount`. It sets `ThievingStallState` and schedules `StandardStealTask` with a two-tick execution delay.
3. `StandardStealTask.OnPerform` clears the stall state, validates the exact **same object instance** remains at the tile, then checks visible guards whose names contain `Guard` and are within range 3. Each qualifying guard has a `Random.Next(0,5) == 2` opportunity to detect theft.
4. On a guard detection, it queues a reach task; successful reach stuns the character for 6 ticks, applies animation, graphic and 20 life-point damage.
5. Otherwise it resolves the object's loot table, grants loot via `TryAddLoot` (which can spawn owned ground overflow), sends success, and awards the configured Thieving XP.
6. Unless `EmptyGameObjectID == -1` or the object already has the emptied ID, it mutates that game object to the empty variant and schedules restoration after `Definition.RespawnTicks`.

**Source caveats:** a successful theft when the second lookup returns no loot table can still show success and grant XP. The first lookup already required a table, so reproducing this needs actual mid-action table availability change; do not automatically file a defect. The owner-identity guard protects against an object instance replacement. A missing `GameObjectIDs` list is a reachability/documentation gap, not proof that other custom theft scripts are absent.

## Pickpocket source kept authoritative rather than duplicated

[The existing pickpocket source catalog](data/catalogs/npc-pickpocket-source-rules.csv) already lists NPC-specific levels, XP, fail damage and multi-loot eligibility thresholds. No duplicate CSV is needed.

`StandardPickPocketTask` calculates its extra-loot tier on initialization, draws a level/Agility-based failure roll on the scheduled action, applies a six-tick stun and configured damage on failure, and queues asynchronous loot/XP on success. XP is given once for the successful attempt; multi-loot changes result count, not base XP.

The exact failure formula is *implementation evidence*, not a historical success probability. Capture controlled cases and do not replace it with the unverified linear success approximation criticized in `reference/deep-thieving.md`. Source reads `PickPocketingLootTableId` from the NPC definition; do not invent static loot weights here.

## Regression and verification cases

| ID | Action | Invariant |
| --- | --- | --- |
| THIEF-01 | For each of the 12 entries with IDs, activate a registered object of the specified identity | Source definition resolves to the intended entry, expected level and XP |
| THIEF-02 | Select one of 11 source entries with no object IDs | Do **not** report it as implemented or fabricate a registration; verify against other scripts only if adding content later |
| THIEF-03 | Replace the target object instance during the two-tick delay | No loot, XP or depletion |
| THIEF-04 | Another character empties the shared stall before this task resolves | Only the authoritative current object can be looted |
| THIEF-05 | Guard moves into range just before completion | Detection evaluated at execution time, not at click time |
| THIEF-06 | Guard fails to reach offender | Do not apply success-of-reach damage/stun without successful reach |
| THIEF-07 | Successful stall transform and server restore | Single authoritative empty/full object cycle, no stale restore overwriting unrelated later object |
| THIEF-08 | Successful pickpocket with multi-loot perk | Extra item rolls but one base XP event |
| THIEF-09 | NPC dead at pickpocket completion | No delayed loot or XP |
| THIEF-10 | Limited inventory slots | Follow existing loot fallback and precheck semantics; don't infer lost items from capacity alone |

No tests have been executed as part of this documentation-only work.

## Deferred tasks

- Join actual index-16 object definitions, locations, quest restrictions and transformed variants when the approved cache snapshot is available.
- Obtain authenticated stall/NPC loot weights and world spawns in the later data work, without inventing missing values.
- Compare all 11 unresolved source stalls with other object script registrations before proposing real implementation; do not treat absence from `Stall` as proof of absence everywhere.

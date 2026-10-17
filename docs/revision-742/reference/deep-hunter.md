# Hunter — traps, catch ownership and implementation baseline

## Reference mechanics

[Historical methods](../data/researched/hunter-methods.csv) enumerate tracking, bird snaring, netting, deadfall, box traps, rabbit snares, net/pitfall, falconry and magical imp boxes, with minimum level and required equipment. [Trap capacity](../data/researched/hunter-trap-capacity.csv) is **1 at level 1, 2 at 20, 3 at 40, 4 at 60, 5 at 80** for shared trap styles, while **deadfall allows one** irrespective of Hunter level. The existing [bird species table](../data/hunter-birds.csv) lists five species and XP; [Hunter source](https://wiki.darkan.org/Hunter) also notes level **39 Wimpy bird, 167 XP**.

**Single-tile ownership:** only one placed snare occupies a tile. Bird snares may collapse and become public dropped items after a delay; player logout must not duplicate trap items through both carried inventory and ground state. Catch XP is rewarded when collecting a captured bird, not necessarily when the trap triggers. Smoking a trap (Hunter 39 plus lit torch) improves chance and must not grant loot by itself.

## Existing Hagalaz seams

No dedicated Hunter skill service or script family was found in the audited main tree. Prefer existing `GameObjectService`, `NpcService`, `MapRegionService`, `EntityService`, `GroundItemService`, inventory mutation boundaries and tick scheduling. Begin with **one bird snare** and its NPC catch target; use measured collision and NPC behavior before adding a new generic state machine.

### Owner model required

A placed trap has a **single authoritative world owner** and an associated character ID, original physical item identity, tile+plane and region, method/trap tier, live stage, timers and eventual reward owner. Stages are conceptually `Placed -> Armed -> Caught|Failed|Collapsed -> Collected|Reclaimed|Expired`; use these as analysis, not a mandate for a new universal trap framework.

### Per-action data to extract

```text
method,trap_item_id,placement_object_id,replacement_object_ids,
target_npc_ids,skill_level,maximum_traps,required_area,tile_collision,
bait_items/smoke_modifier,success_probability,inspection_action,
loot_item_ids/counts,xp,trap_timeout,ground_item_ownership_timeout
```

## Test matrix

- Two players attempt to place a trap on same tile; only one succeeds and one item is consumed.
- Player cap at levels 1, 20, 40, 60, 80, with special deadfall limit.
- NPC crosses trap at precise tick and spawns exactly one reward; duplicate collection does not double reward/XP.
- Trap falls/collapses with full inventory, logout, teleport, despawned NPC and region reload.
- Other player's trap cannot be collected early or privately inspected without permissions.
- Smoking with/without lit torch, lure/bait with unavailable item, skill boost expiry and trap capacity drop.
- Reward inventory full does not destroy caught resources or original trap.
- No hidden extra trap worker; existing world tick and cleanup own stage progression.

The exact object/NPC/item identities, trap timing, catch chances, region placement and animations remain unverified in version-742 data.

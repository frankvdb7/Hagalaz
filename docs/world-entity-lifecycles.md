# World entity lifecycles

This guide describes the current GameWorld lifecycle for characters, NPCs, game objects, and ground items. It follows the production ownership paths; it does not define a new lifecycle contract.

## Ownership at a glance

| Entity | Runtime owner | Activation | End of life |
| --- | --- | --- | --- |
| Character | Character store, entity store, and its current map region | World sign-in calls `OnRegistered()` after hydration and region readiness | Logout captures a final character snapshot, removes store ownership, then destroys the character |
| NPC | NPC store, entity store, and its current map region | `NpcService` registers it and calls `OnRegistered()` | NPC service unregisters it, then `Destroy()` runs creature and NPC cleanup |
| Game object | A region part and the entity store while registered | A region part adds it, enables static objects, and calls `OnSpawn()` | Dynamic objects are destroyed on removal; static objects are disabled and retained until replaced or region teardown |
| Ground item | A region part and the entity store while registered | A region part adds it, unless it merges into an existing stack | Pickup, expiry replacement, or region teardown removes/replaces it |

Characters and NPCs share the [`Creature`](../Hagalaz.Services.GameWorld/Model/Creatures/Creature.cs) runtime and region-tick path. Game objects and ground items are region-owned map entities; they do not use `Creature` or its combat and movement lifecycle.

## Shared creature lifecycle

`Creature.OnRegistered()` initializes its viewport, attaches the creature to the region for its location, and runs the initial spawn path. Character and NPC override registration to perform their own setup around that shared work.

The [game worker](../Hagalaz.Services.GameWorld/Services/GameWorkerService.cs) runs creature logic and map updates on the serialized game loop. Each major tick runs creature content/combat/state/movement, prepares client updates, sends updates, and resets per-tick rendering state. Region preparation also advances ground-item timers.

`Creature.Destroy()` is the common terminal path for characters and NPCs. It cancels creature tasks, removes the creature from its region, notifies the area that the creature exited, calls the concrete creature's `OnDestroy()`, calls combat cleanup, and disposes the creature's service scope. See [`Creature.Destroy()`](../Hagalaz.Services.GameWorld/Model/Creatures/Creature.cs) and the type-specific sections below.

## Character

### Typical sequence

```text
create → hydrate → add to character/entity stores → load visible regions
  → ICharacter.OnRegistered()
  → Character.OnRegistered(): script.OnRegistered() → OnInit() → OnSpawn()/script.OnSpawn()
  → active creature ticks
  → successful terminal logout only: ICharacter.OnDetach() → character-script OnDestroy() callbacks
  → final snapshot → remove from stores → Character.Destroy()
  → Creature.Destroy(): leave region/area → Character.OnDestroy() entity hook → combat cleanup → dispose scope
```

Here `ICharacter.OnDetach()` and `Character.OnDestroy()` are different stages. `OnDetach()` runs character-script destruction callbacks before the logout snapshot; `Character.OnDestroy()` is the entity hook called from `Destroy()` after region removal. The script callbacks are skipped there if they completed successfully during detachment. Other destruction paths, such as failed sign-in cleanup, can call `Destroy()` without `OnDetach()`.

### Sign-in and activation

1. [`WorldSessionAdmissionService`](../Hagalaz.Services.GameWorld/Services/WorldSessionAdmissionService.cs) acquires the world-session claim, requests the persisted character model, creates the character, hydrates its components, and adds it to the character and entity stores.
2. [`WorldSignInCommandConsumer`](../Hagalaz.Services.GameWorld/Mediator/Consumers/WorldSignInCommandConsumer.cs) ensures the character's visible regions are loaded, then calls `ICharacter.OnRegistered()`.
3. [`Character.OnRegistered()`](../Hagalaz.Services.GameWorld/Model/Creatures/Characters/Character.cs) initializes client rendering, attaches the character to its region, registers event handlers, refreshes appearance, calls each character script's `OnRegistered()`, initializes the default state, and starts the spawn path. The spawn path calls script `OnSpawn()` callbacks.

### Active and persisted state

The character participates in the normal creature tick. Its content tick advances prayers and statistics and calls attached character scripts' `Tick()` methods.

Character data is hydrated by [`CharacterHydrationService`](../Hagalaz.Services.GameWorld/Services/CharacterHydrationService.cs) and the registered hydrators. [`CharacterDehydrationWorkerService`](../Hagalaz.Services.GameWorld/Services/CharacterDehydrationWorkerService.cs) captures periodic snapshots every five minutes and during shutdown. Character snapshots contain character-owned state; NPC, game-object, and ground-item instances are managed separately by GameWorld's stores and regions.

### Logout and destruction

For terminal logout, [`CharacterLogoutService.DetachAsync()`](../Hagalaz.Services.GameWorld/Services/CharacterLogoutService.cs) calls `ICharacter.OnDetach()` before taking the final snapshot. `Character.OnDetach()` invokes the attached scripts' `OnDestroy()` callbacks before dehydration. If all callbacks complete successfully, `Character.OnDestroy()` skips invoking them again; if any callback fails, the call reports the failures and the completion flag is not set. The service then removes the character from its stores, records the snapshot for persistence, and calls `Character.Destroy()`.

`Character.OnDestroy()` publishes the creature-destroyed event, invokes script destruction callbacks if they have not already run, and unregisters event handlers. When `Creature.Destroy()` later disposes the service scope, character-owned services and tasks end as well. Failed sign-in cleanup can call `Destroy()` directly without going through the logout snapshot path.

The distinction matters: `OnDetach()` is the pre-snapshot callback in the terminal logout path; `Destroy()` is the terminal in-memory cleanup path.

## NPC

### Typical sequence

```text
create → NpcService.Register() → add to NPC/entity stores → Npc.OnRegistered()
  → Creature.OnRegistered(): attach to region → OnInit() → OnSpawn()/script.OnSpawn()
  → script.OnCreate() → active creature ticks
  → death: NPC script.OnDeath() → either respawn same instance and call Npc.OnSpawn() again,
    or NpcService.Unregister() → remove from stores → Npc.Destroy()
  → Creature.Destroy(): leave region/area → Npc.OnDestroy() entity hook/script callback → dispose scope
```

### Registration and activation

[`NpcService.Register()` / `RegisterAsync()`](../Hagalaz.Services.GameWorld/Services/NpcService.cs) add the NPC to the NPC store and entity store, then call `INpc.OnRegistered()`. [`Npc.OnRegistered()`](../Hagalaz.Services.GameWorld/Model/Creatures/Npcs/Npc.cs) initializes rendering and delegates to `Creature.OnRegistered()`, which attaches it to its region and runs the spawn path. NPC spawn sends `CreatureSpawnedEvent`, calls the NPC script's `OnSpawn()`, and resets combat spawn state. Registration then calls the script's `OnCreate()` and may queue random walking.

### Active, death, and respawn

NPCs use the shared creature tick. Their content tick advances NPC statistics and calls the NPC script's `Tick()`; the creature tick also advances combat, states, and movement.

On death, the NPC sends `CreatureDiedEvent`, locks movement, and invokes script and combat death handling. Combat schedules the death presentation and then either schedules the script's respawn behavior or unregisters a non-respawning NPC. Respawn reuses the NPC instance: it normalizes statistics, teleports to its default location, unlocks movement, makes it visible, and runs `OnSpawn()` again.

### Unregister and destruction

Unregister removes the NPC from the NPC store and entity store before calling `Destroy()`. NPC destruction sends `CreatureDestroyedEvent`, invokes script `OnDestroy()` if `OnCreate()` started, and unregisters event handlers; common creature cleanup handles tasks, region membership, area exit, combat, and scope disposal. Region teardown also unregisters its NPCs through `NpcService`.

## Game objects

### Typical sequence

```text
construct → script.Initialize() → MapRegionPart.Add()
  → own/register active instance → [static: OnEnable()] → OnSpawn()/script.OnSpawn()
  → active in region
  → dynamic removal: GameObject.Destroy() → game-object script.OnDestroy()
  → static removal: OnDisable() → retained as disabled static object
      → same instance re-added: OnEnable() → OnSpawn()
      → replaced or region destroyed: GameObject.Destroy() → game-object script.OnDestroy()
```

[`GameObject`](../Hagalaz.Services.GameWorld/Model/Maps/GameObjects/GameObject.cs) initializes its script in the constructor. Map data is loaded through [`MapRegionLoader`](../Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs); runtime changes use [`MapRegionService`](../Hagalaz.Services.GameWorld/Services/MapRegionService.cs). A [`MapRegionPart`](../Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegionPart.cs) owns objects by map position and registers active instances with the entity store.

When a region part adds an object, it stores the object, enables it if static, calls script `OnSpawn()`, and queues a client update when needed. Static removal calls `OnDisable()` and moves the object into the part's disabled-static collection; it remains available for re-enabling. Re-adding that same static object calls `OnEnable()` and `OnSpawn()`. Replacing it with another instance or tearing down the region destroys the retained object. Removing a dynamic object destroys it and removes its entity identity immediately.

`GameObject.Destroy()` calls script `OnDestroy()`. Static `OnEnable()` / `OnDisable()` represent temporary activation changes and are distinct from destruction. The script's `CanSuspend()` and `CanDestroy()` values participate in region lifecycle decisions. The game-object script contract has no general per-tick callback; scripts respond to interactions, lifecycle callbacks, and work they schedule themselves.

## Ground items

### Typical sequence

```text
MapRegionService.AddGroundItem() → MapRegionPart.Add()
  → [matching stack: merge and update existing item]
  → [otherwise: register and own new item → OnSpawn() (currently a no-op)]
  → region ticks decrement timer → expiry replaces or removes the instance

pickup: ItemScript.TakeItem() → check permission/capacity → IGroundItem.Despawn()
  → remove exact region instance (or create respawn instance) → add item clone to inventory
```

[`MapRegionService.AddGroundItem()`](../Hagalaz.Services.GameWorld/Services/MapRegionService.cs) finds or creates the item's region; the region part then owns the item by location and registers its entity identity. Before adding a new ordinary ground item, the part may merge it into an existing stack when owner and stacking rules match. In that case the incoming instance is not separately registered. Ground items currently have no-op `OnSpawn()` and `Destroy()` methods and no per-item script lifecycle.

`Owner != null` means the item is owner-restricted; `Owner == null` means it is public. Each region's major client-preparation tick decrements `TicksLeft` and processes expired items:

- A normal item configured to respawn is replaced by a respawning instance. `ItemScript.CanDrawFor()` keeps that instance hidden while its respawn timer runs. When the timer expires, it is replaced by a normal visible instance.
- An expired private item without a respawn delay is removed. If its item script permits trading, it is replaced by a public copy; otherwise it disappears.
- An expired public item without a respawn delay is removed.

Pickup is handled by the item script. [`ItemScript.TakeItem()`](../Hagalaz.Game.Scripts/Model/Items/ItemScript.cs) checks permission and inventory capacity, despawns the exact ground-item instance through `IGroundItem.Despawn()`, then adds a clone to inventory. A successful despawn may create a respawning instance when the ground item is configured to respawn.

Ground-item timers affect whether a region can suspend or be destroyed. Region teardown calls `Destroy()` and removes each remaining item from the entity store. Ground items are runtime region entities; they are not part of a character snapshot.

## Region suspension and teardown

The [region background service](../Hagalaz.Services.GameWorld/Services/MapRegionBackgroundService.cs) periodically suspends regions that are no longer visible to characters. `MapRegion.CanSuspend()` rejects suspension while a character or any non-suspendable creature, object, or ground item remains. `MapRegion.CanDestroy()` additionally rejects dynamic regions, recently idle regions, and regions containing entities whose `CanDestroy()` is false.

When an idle region is removed, [`MapRegion.Destroy()`](../Hagalaz.Services.GameWorld/Model/Maps/Regions/MapRegion.cs) unregisters NPCs and destroys/unregisters its ground items and active or disabled static game objects. This region cleanup is separate from character logout and character persistence.

# Construction: full runtime slice and research expansion

## Historical period facts and source anomalies

- `Construction` on the archived wiki specifies room footprints of **8x8 tiles**, 3 reachable floors (ground/upper/basement), and a room-count maximum increasing from **20 at level 1** to **32 at level 99**. House dimensions start at 3x3 rooms and grow to 7x7 by level 60. [Source](https://wiki.darkan.org/Construction)
- Room construction requires build mode, suitable skill, coins, neighboring door support, a clear room slot and an owner-only action. Build mode cannot start with guests present; familiars and dropped items are restricted during building. An existing room cannot be overwritten without removal; removal cannot leave unsupported rooms above.
- The source says there are **23** room types, but the visible list used in [CSV](../data/researched/construction-rooms.csv) contains **21** named entries. Do **not** claim the CSV is complete until the remaining room identities are proven or the source count corrected.
- The archived article contradicts itself on the first house being in Taverley versus Rimmington. This is a source conflict, **not** license to assume one.
- Sawmill plank conversion is one log plus 100 coins ordinary / 250 oak / 500 teak / 1,500 mahogany; these are *fee candidates*, not a universal server-shop rule. [Source](https://wiki.darkan.org/Construction)

## Data structure to reconstruct from the 742 cache

**House locations:** portal/object ID, map tile and plane, level, relocation cost, destination instance template. [Six locations](../data/researched/construction-house-portals.csv).

**Room definition:** room type ID, source map chunks, width/depth/doors, floor constraint, level, coin cost, max copies, rotation transform, upper/lower support rule, player cap. [Known prices and levels](../data/researched/construction-rooms.csv).

**Furniture:** hotspot ID/slot, room type, object ID before/after construction, dismantle result, required level, resource item/count list, tool/saw/hammer requirement, nails-break probability, build and flatpack XP, optional animations/config update.

**Servants:** service type and level, two-bed requirement, material transit capacity/timing, fee, bank/owner permissions, fetch/deliver semantics, interruption and dismissal.

**Runtime state:** owning character, house portal location, layout grid/room rotations and levels, placed hotspot objects, pets/costume collection, servant, guest list/access rights, editing session, permanent state revision and safe exit coordinate.

## Actual Hagalaz reuse

`Hagalaz.Services.GameWorld/Services/MapRegionService.cs` and game object/area services own world placement. `Hagalaz.Game.Abstractions/Collections/ItemContainerTransaction.cs` owns material and coins cost; the character snapshot pipeline owns character-associated state. No existing generic house engine was identified. Avoid an independent second map/animation engine or an imaginary cache-side server coordinator.

### Mutations and failure semantics

1. Enter through validated portal; verify owner/visitor and load exact persistent layout.
2. In edit mode, refuse guest presence and ineligible room changes; validate rotation/door/support and coin/cost capacity.
3. Reserve cost and change one room/hotspot under a **single authoritative persistence/transaction boundary** appropriate for the storage owner. A partially deducted cost must not leave the layout unchanged after crash.
4. Publish scene object changes only after commit. Existing objects are immutable to unauthorized guests.
5. On logout or unexpected instance closure, persist the layout, remove temporary guests and restore a safe outside location. Instance cleanup cannot erase paid furniture.

### Regression fixtures

- Empty house -> build room -> save -> reload -> same tiles/objects and consumed coins.
- Rotate prospective room into blocked connection -> cost unchanged.
- Build with guests in instance or no upper support -> rejected.
- Remove room containing a staircase supporting upper floor -> rejected.
- Guest attempts object removal, bank-servant action or build mode -> rejected server-side.
- Two simultaneous build requests -> exactly one mutation and one cost.
- Failed persistence or logout during construction -> durable deterministic result, not a half-built room.

**Missing:** complete furniture hotspot/cache index and usable house instance coordinates, all-room canonical list, servant datasets, UI/CS2 and wall geometry fixtures. These require the pinned 742 cache before declaring completion.

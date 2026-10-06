## Why

Item movement is currently split across separate removal and insertion calls. A later capacity, stack-limit, or source-count failure can leave one container changed and the other unchanged, so this change gives exact two-container movement one checked storage boundary.

## What Changes

- Add a synchronous exact transfer operation for two item containers. Success commits the requested quantity on both sides; failure leaves both storage states and revisions unchanged.
- Make the public `IItemContainer.TryTransferTo(...)` self-contained for a standalone transfer, while allowing it to participate in a caller-owned transaction without committing that larger scope. Keep the internal mutation boundary transaction-required.
- Share a deterministic lock order across normal container mutations and transfers, based on the existing trade-container ordering.
- Preserve source item data and instances for ordinary movement, honor preferred source and destination slots, reject zero-count transfers, and retain container sentinel behavior when a source is drained.
- Separate storage mutation from change publication for the named composed operations: trade settlement/refund/conservation, offer coin movement, paired Money Pouch and Inventory operations, and equipment movement. Complete storage first, then required equipment domain effects, then publish changes.
- Migrate bank, familiar inventory, reward, equipment/inventory, generic familiar bulk movement, and trade checked mutations to the common storage boundary where their existing behavior is an exact item movement. Price Checker keeps cloned non-owning selections and renders a projected inventory view.
- Use the same boundary for the item movement in shop sales, while leaving payment and full shop transaction behavior in the owning shop flow.
- Keep intentional partial gameplay behavior explicit by choosing its exact quantity before calling the transfer operation.

### Non-goals

- No inheritance-to-composition migration from #438.
- No atomic payment, stock, and inventory transaction for shop purchases from #449; exact shop payment remains covered by #441.
- No generic transaction/Unit of Work, mutation-planning framework, async lock, event queue, or reusable multi-container transaction framework. The named existing operations may use explicit storage-only mutations under their existing locks and snapshots.
- No redesign of duel escrow, persistence, shop transaction scope, or unrelated container APIs.

### Acceptance Criteria

- Failed exact transfers, including capacity and stack overflow failures, leave source/destination slots, counts, and revisions unchanged and publish no updates.
- Successful transfers preserve the exact requested quantity, stacking rules, preferred slots, item data, and source sentinel state; standalone transfers publish only after both containers commit.
- A standalone public transfer owns its short transaction; transfers inside a common caller-owned transaction remain deferred and roll back with that transaction. Partial/conflicting transaction participation fails before mutation.
- Concurrent opposite-direction transfers use one deterministic lock order and complete without deadlock.
- Listed gameplay flows use the primitive without changing their intentional partial-count or equipment-callback behavior.
- Price Checker selections are non-owning clones. Inventory remains authoritative, and closing or disconnecting cannot lose items because no authoritative item leaves Inventory.
- Composed trade, Money Pouch, and equipment operations finish all related storage mutations and required equipment effects before publishing changed container state. Checked failures restore all affected storage before any restored-state publication.
- Widget close remains unconditional, with a safe snapshot during batch closure. No close guard or terminal conservation lifecycle is introduced.
- Focused MSTest regressions and strict OpenSpec validation pass.

### Stop Conditions

- If safe transfer requires a general multi-container transaction or a second state owner, leave that flow in its owning domain and document a follow-up.
- If an affected derived container cannot preserve its documented domain behavior through the common storage boundary, do not bypass its callbacks or sentinel rules to force migration.

## Capabilities

### New Capabilities

- `atomic-item-container-transfer`: Exact synchronous transfers commit both container storage states together or leave both unchanged.

### Modified Capabilities

None.

## Impact

`BaseItemContainer` and `TradeItemContainer` in `Hagalaz.Game.Abstractions`, the named GameWorld character containers, shop stock and widget close lifecycle, Price Checker and bank/familiar UI movement callers, and focused Abstractions/GameWorld/Scripts MSTest suites. No package, persistence, or protocol changes.

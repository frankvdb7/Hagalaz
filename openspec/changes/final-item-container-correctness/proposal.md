# Proposal

## Why

Weapon and shield replacement and unequipping can act on stale item references because eligibility callbacks run before mutation ownership. After storage commits, an exception from one publisher can also prevent unrelated committed publishers or completion owners from running.

## What Changes

- Revalidate captured inventory and equipment instances inside the existing Equipment transaction before storage mutation.
- Revalidate the equipped instance and choose the preferred inventory destination while the existing unequip transaction owns both boundaries.
- Attempt each independent standalone and transactional publication/completion stage once after storage commits, preserving failure propagation and cleanup.
- Update the canonical and active OpenSpec requirements and add focused regression coverage.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: require stale Equipment operations to reject before mutation and require committed independent publishers/completion owners to be attempted despite earlier failures.

## Impact

Changes are limited to item-container mutation and transaction code, EquipmentContainer, the related Abstractions/GameWorld test projects, and item-container OpenSpec files. No public API or transaction architecture changes are planned.

Acceptance requires exact-instance rejection and successful placement regressions, attempt-all publication with committed storage and preserved exceptions, targeted tests, full solution build/tests, strict OpenSpec validation, the existing duplication gate, and a clean diff check.

Non-goals: changing generic transfer semantics, adding synchronization or transaction abstractions, changing gameplay eligibility callbacks, or redesigning trade processing. Stop if the existing boundaries cannot provide the required ownership without a new abstraction.

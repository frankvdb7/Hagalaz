## 1. Common storage transfer

- [x] 1.1 Move the stable mutation lock and order to the common base-container boundary, and verify base mutators and trade checked operations serialize through it with Abstractions tests
- [x] 1.2 Implement exact two-container preflight and commit with slot, sentinel, identity, revision, and post-commit notification behavior, and verify the transfer regression tests
- [x] 1.3 Reuse exact removal/addition from `AddAndRemoveFrom` and trade checked operations, and verify the affected Abstractions and trade tests

## 2. Character item movement

- [x] 2.1 Migrate bank, familiar inventory, and reward flows while preserving their explicit quantity and note policies, and verify the GameWorld container tests
- [x] 2.2 Migrate applicable one-item equipment/inventory storage paths while keeping validation and callbacks in `EquipmentContainer`, and verify equipment callback tests
- [x] 2.3 Keep Inventory authoritative and implement Price Checker selections as clones with a projected inventory view
- [x] 2.4 Migrate the exact item movement in shop sales and verify stock, inventory, and item data; leave payout/purchase transaction semantics to the owning shop flow
- [x] 2.5 Preserve bank-tab insertion behavior after transfer stops guaranteeing destination object identity, and verify the affected Scripts build/test boundary

## 3. Validation and review

- [x] 3.1 Run focused and affected project test suites, build the appropriate solution boundary, and record successful commands and any pre-existing warnings
- [x] 3.2 Run strict OpenSpec validation and review the complete diff for atomicity, locking, item identity, notification timing, callback behavior, trade duplication, and scope
- [x] 3.3 Correct post-commit publication semantics and impossible non-stackable preflight with focused regressions
- [x] 3.4 Remove close refusal concepts and simplify widget-close APIs while retaining safe batch snapshots
- [x] 3.5 Remove obsolete refusal tests, retain behavior tests, and rerun the requested project/build/OpenSpec/jscpd/diff validation

## 4. Post-commit publication exception behavior

- [x] 4.1 Let transfer, trade, and Money Pouch change publication run after storage commits, with unexpected exceptions propagating
- [x] 4.2 Verify publication exceptions propagate while committed transfer storage remains committed
- [x] 4.3 Run focused and full project tests, solution build, strict OpenSpec, jscpd against `origin/main`, and `git diff --check`

## 5. Separate storage mutation from publication in composed operations

- [x] 5.1 Factor one storage-only transfer path for equipment while keeping standalone `TryTransfer` behavior unchanged
- [x] 5.2 Add checked trade storage-only operations with changed slots, and defer settlement/refund/conservation/offer coin publication until final storage is committed or restored
- [x] 5.3 Separate paired Money Pouch and Inventory storage mutation from change publication and pouch messages
- [x] 5.4 Run equipment domain effects after storage commit and before publication for equip and unequip operations
- [x] 5.5 Add focused ordering and rollback regressions for trade coin movement, settlement, restoration, and equipment; validate affected project suites

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
- [x] 3.3 Correct post-commit observer semantics and impossible non-stackable preflight with focused regressions; the refused-close redraw was removed with the superseded close-guard design
- [x] 3.4 Remove close refusal concepts and simplify widget-close APIs while retaining safe batch snapshots
- [x] 3.5 Remove obsolete refusal tests, retain behavior tests, and rerun the requested project/build/OpenSpec/jscpd/diff validation

## 4. Post-commit callback failure behavior

- [x] 4.1 Remove observer exception suppression and invoke transfer, trade, and Money Pouch callbacks directly after storage commits
- [x] 4.2 Replace observer suppression tests with one regression proving callback exceptions propagate while committed transfer storage remains committed; remove fatal-exception policy tests
- [x] 4.3 Run focused and full project tests, solution build, strict OpenSpec, jscpd against `origin/main`, and `git diff --check`

# Tasks

## 1. Storage core and transfer boundary

- [x] 1.1 Implement `ItemContainerStorage` as the sole owner of slots, mutation algorithms, revision/enumeration, reset-count handling, exact restoration, and stable mutation locking; port core mutation cases into `ItemContainerStorageTests` and run the Abstractions test project.
- [x] 1.2 Move storage-to-storage plan/commit logic into `ItemContainerStorage.TryTransfer`, add `IItemContainerStorageProvider` and `ItemContainerTransfer`, and verify exact transfer, publication, exceptions, sentinel movement, and lock ordering in `ItemContainerTransferTests`.
- [x] 1.3 Move `AddAndRemoveFrom` coordination to `ItemContainerTransfer`, preserve its current per-slot/partial behavior, and verify existing and new boundary cases in Abstractions tests.

## 2. Simple and temporary containers

- [x] 2.1 Migrate `GenericContainer` and its callback behavior to direct storage composition, with focused tests for callback timing and mutation forwarding.
- [x] 2.2 Migrate Price Checker selection/projected containers and `DuelContainer`, preserving projected exact removal and UI slot updates; run the Scripts tests.

## 3. Character inventory containers

- [x] 3.1 Migrate `FamiliarInventoryContainer` and `RewardContainer` to direct storage composition and the shared transfer coordinator; verify transfer, update publication, and exact-slot hydration behavior.
- [x] 3.2 Migrate `BankContainer`, remove its redundant free-slot override, and use `ItemContainerTransfer` for bank deposit/withdraw paths; verify note transforms and existing transfer behavior.
- [x] 3.3 Migrate `InventoryContainer` and preserve its trade checked APIs and inventory event semantics; verify storage, publication, transfer, and persistence tests.

## 4. Special storage semantics

- [x] 4.1 Migrate MoneyPouch to reset-to-zero storage, keep sentinel validation and pouch/inventory trade semantics in the domain class, and verify hydration, overflow, combined balance, trade, and restoration tests.
- [x] 4.2 Migrate ShopStock to reset-to-zero storage and preserve normalization, original stock and publication behavior; add focused depleted-stock/zero-count normalization tests and run GameWorld shop tests.

## 5. Equipment behavior

- [x] 5.1 Migrate EquipmentContainer to direct storage composition and the storage-only transfer coordinator; characterize callback ordering for equip, unequip, replace, remove, clear and failed transfer, then run the GameWorld and Scripts equipment tests.

## 6. Trade settlement

- [x] 6.1 Migrate the trade offer container and inheritance-based trade fixtures to direct storage composition while preserving its UI and acceptance revision semantics; verify offer revision tests.
- [x] 6.2 Migrate `TradeExchange` lock ordering, snapshots, restoration and storage-only trade mutations to `ItemContainerStorage`; verify settlement, refund, escrow, pouch movement, rollback, publication and lock-release regressions.
- [x] 6.3 Remove `BaseItemContainer.cs` and `TradeItemContainer.cs`; verify all production/test containers compose storage and no replacement implementation base exists.

## 7. Integration and review

- [x] 7.1 Review the cumulative diff for one slot owner/one mutation algorithm, callback and publication order, exact persistence slots and absence of #439 cleanup; run focused Abstractions, GameWorld and Scripts test projects.
- [x] 7.2 Run the affected solution build, strict OpenSpec validation, local repository-configured jscpd base comparison, and `git diff --check`; record exact commands and results.
- [ ] 7.3 Resolve the 27 new clone pairs reported against the pull-request base under an approved policy, then obtain a passing `fail-on-new-clones` result.

# Tasks

## 1. Storage core and transfer boundary

- [x] 1.1 Implement `ItemContainerStorage` as the sole owner of slots, mutation algorithms, revision/enumeration, reset-count handling, exact restoration, and stable mutation locking; port core mutation cases into `ItemContainerStorageTests` and run the Abstractions test project.
- [x] 1.2 (Temporary migration implementation; `IItemContainerStorageOwner` and `ItemContainerTransfer` were removed by task 10.2.) Move storage-to-storage plan/commit logic into `ItemContainerStorage.TryTransfer`, add `IItemContainerStorageOwner` and `ItemContainerTransfer`, and verify exact transfer, publication, exceptions, sentinel movement, and lock ordering in `ItemContainerTransferTests`.
- [x] 1.3 (Temporary generic partial-bulk implementation; removed by task 13.2 and replaced with familiar-owned policy.) Move `AddAndRemoveFrom` coordination to `ItemContainerTransfer`, preserve its current per-slot/partial behavior, and verify existing and new boundary cases in Abstractions tests.

## 2. Simple and temporary containers

- [x] 2.1 Migrate `GenericContainer` and its callback behavior to direct storage composition, with focused tests for callback timing and mutation forwarding.
- [x] 2.2 Migrate Price Checker selection/projected containers and `DuelContainer`, preserving projected exact removal and UI slot updates; run the Scripts tests.

## 3. Character inventory containers

- [x] 3.1 Keep `FamiliarInventoryContainer` and `RewardContainer` as domain objects that own `ItemContainer`; remove `IContainer` forwarding and migrate consumers to `.Items`; verify transfer, publication, and hydration.
- [x] 3.2 Keep `BankContainer` as a domain object that owns `ItemContainer`; remove `IContainer` forwarding and migrate consumers to `.Items`; verify note transforms and transfer behavior.
- [x] 3.3 Keep `InventoryContainer` as a domain object that owns `ItemContainer`; remove `IContainer` forwarding and migrate consumers to `.Items`; verify event, transfer, and persistence behavior.

## 4. Special storage semantics

- [x] 4.1 Migrate MoneyPouch to reset-to-zero storage, keep sentinel validation and pouch/inventory trade semantics in the domain class, and verify hydration, overflow, combined balance, trade, and restoration tests.
- [x] 4.2 Keep ShopStock domain behavior over composed `ItemContainer`; remove generic forwarding and migrate consumers to `.Items`, preserving normalization, original stock, publication, and depleted-stock behavior.

## 5. Equipment behavior

- [x] 5.1 Migrate EquipmentContainer to direct storage composition and the storage-only transfer coordinator; characterize callback ordering for equip, unequip, replace, remove, clear and failed transfer, then run the GameWorld and Scripts equipment tests.

## 6. Trade settlement

- [x] 6.1 Make TradeOffer own `ItemContainer` and retain only trade state/operations; migrate callers to `Items` and preserve UI and acceptance revision semantics.
- [x] 6.2 Keep `TradeExchange` as a consumer of generic storage operations under its existing ordered locks; preserve snapshots, restoration, pouch movement, publication and lock-release behavior without trade-specific item-container APIs.
- [x] 6.3 Remove `BaseItemContainer.cs` and `TradeItemContainer.cs`; verify all production/test containers compose storage and no replacement implementation base exists.

## 7. Integration and review

- [x] 7.1 Review the cumulative diff for one slot owner/one mutation algorithm, callback and publication order, exact persistence slots, and no copied generic container implementations; run focused Abstractions, GameWorld, Scripts and Extensions test projects.
- [ ] 7.2 Run the affected solution build, strict OpenSpec validation, local repository-configured jscpd base comparison, and `git diff --check`; record exact commands and results.
- [ ] 7.3 Eliminate every new clone pair relative to the pull-request base and obtain a passing repository `fail-on-new-clones` result with zero new pairs. Local jscpd 5.3.2 comparison against base `75e18b9975c58b209018662a2617fb9b8b285883` began at 38 new pairs; after generic-wrapper composition the current report is 43 new pairs, so the zero-new-clones gate remains open.
- [x] 7.4 Preserve interface dispatch for domain-specialized operations, name the storage owner accurately, and audit GameWorld global imports.
- [x] 7.5 Keep concrete `ItemContainer` as the sole `IItemContainer` implementation over `ItemContainerStorage`, including generic exact removal and committed-change publication callback; remove `ITradeItemContainer`.
- [x] 7.6 Ensure production domain containers and script-local objects compose `ItemContainer` without implementing or forwarding `IContainer`; retain only domain-specific operations and callbacks.
- [x] 7.7 Migrate trade settlement and tests to consume generic exact storage operations; preserve MoneyPouch semantics, Equipment callback order, restoration, and shop zero-count behavior.
- [ ] 7.8 Run focused projects, complete solution tests, build, strict OpenSpec validation, spec validation, diff check, and jscpd zero-new-clones gate. Docker-backed suites require an available engine; jscpd must report zero new pairs.

## 8. Complete composition through domain and script objects

- [x] 8.1 Remove `IContainer<IItem?>` inheritance and forwarding members from Inventory, Bank, Reward, FamiliarInventory, and ShopStock; migrate callers to each domain object's `Items` component.
- [x] 8.2 Replace `GenericContainer`, Price Checker forwarding containers, TradeOffer, and Duel forwarding containers with direct `ItemContainer` composition; migrate callers and preserve domain callbacks/state.
- [x] 8.3 Replace test fixtures that reproduce generic container behavior with real `ItemContainer` instances and callback assertions; keep only genuine domain-specific fakes.
- [x] 8.4 Ensure ordinary wrappers do not implement `IItemContainerStorageOwner`; keep storage ownership on the composed `ItemContainer` and preserve specialized MoneyPouch/Equipment boundaries.
- [ ] 8.5 Reclassify every remaining clone pair after composition cleanup and remove only duplications with a clear shared owner; retain domain-specific code where abstraction would worsen the design.

## 9. Domain contract consistency

- [x] 9.1 Expose ordinary domain components as `IItemContainer Items` while keeping concrete `ItemContainer` ownership private to implementations.
- [x] 9.2 Move Equipment and MoneyPouch to direct `ItemContainerStorage` ownership and remove their generic `Items` view; keep Equipment's read-only container projection and domain callbacks.
- [x] 9.3 Remove `AddAndRemoveFrom(ItemContainer)` from `IItemContainer`, route bulk movement through `ItemContainerTransfer`, and remove equipment publication from `IEquipmentContainer`.
- [x] 9.4 Preserve MoneyPouch presentation by sending a fixed one-slot item list from `Count`; add interface-shape assertions and run focused project validation.

## 10. Instance mutation boundaries and transactions

- [x] 10.1 Add concrete `ItemContainerMutationBoundary` and make `ItemContainer` own/expose it alongside private storage; move normal two-container locking, transfer, staged transfer, and publication onto the boundary without duplicating the storage algorithm.
- [x] 10.2 Delete `IItemContainerStorageOwner` and `ItemContainerTransfer`; change ordinary domain interfaces and properties to `IItemContainer Items`, and migrate transfer callers to interface-typed instance boundaries.
- [x] 10.3 Give Equipment a private mutation boundary and domain-owned transfer operation; preserve equip/unequip callbacks, changed-slot publication, failure atomicity, and callback order.
- [x] 10.4 Give MoneyPouch a private mutation boundary; route exact pouch/inventory mutations through boundaries and retain a narrow safe participation path for TradeExchange.
- [x] 10.5 Add concrete short-lived `ItemContainerTransaction` for deterministic multi-boundary locking, snapshots, rollback, and post-commit publication; refactor TradeExchange and escrow recovery to use it without direct storage access.
- [x] 10.6 Keep raw storage access within storage-owning types and transaction infrastructure; migrate hydration, persistence tests, and all callers without infrastructure-recovery casts.
- [x] 10.7 Add/update boundary, transaction, Equipment, MoneyPouch, and Trade regression tests for exact mutation, publication, lock ordering, rollback, and final state.
- [x] 10.8 Audit repository-wide references and update current/delta OpenSpec wording; run focused tests, solution build, strict and current spec validation, and `git diff --check`. Do not run jscpd in this pass.

## 11. Interface-typed composition follow-up

- [x] 11.1 Add declaration-only `IItemContainerMutationBoundary` and `IItemContainerTransaction`; expose only the boundary abstraction from `IItemContainer`, with concrete enlistment confined to the internal infrastructure bridge.
- [x] 11.2 Change ordinary domain item properties and peer operations to `IItemContainer`; audit Equipment and MoneyPouch so their storage and concrete boundaries remain private.
- [x] 11.3 Route direct and multi-container transfers through the same transaction lock/snapshot/rollback path, with interface-typed participants and no implementation recovery casts.
- [x] 11.4 Replace MoneyPouch infrastructure leaks with semantic staging over `IItemContainerTransaction` and success-only post-commit callbacks; migrate TradeExchange generic signatures.
- [x] 11.5 Keep publication-only methods off `IEquipmentContainer`; preserve death update ordering by deferring the final equipment clear until character death processing is ready to publish.
- [x] 11.6 Audit all domain-facing interfaces and infrastructure casts; run the four focused test projects, solution build, strict change/spec validation, and `git diff --check`. Do not run jscpd.

## 12. Compose trade orchestration

- [x] 12.1 Convert `TradeExchange` from static orchestration to a concrete instance collaborator that owns `IItemBuilder`, then compose it from `TradingCharacterScript` and migrate production and test call sites without adding a service or interface.
- [x] 12.2 Replace the death-cleanup publication escape hatch with a natural Equipment mutation at the end of death cleanup; preserve the final update order and cover script-level trade composition with a behavior test.
- [x] 12.3 Specify composed trade orchestration and its abstraction/ownership boundaries, then run focused suites, solution build, strict OpenSpec validation, and `git diff --check`; leave jscpd work open.

## 13. Final transaction and familiar ownership tightening

- [x] 13.1 Keep `TryExecute` on concrete `ItemContainerTransaction`; narrow `IItemContainerTransaction` to active staging, make changed-slot bookkeeping private, and remove unused changed-slot outputs and generic bulk-transfer APIs.
- [x] 13.2 Move partial familiar withdrawal into `IFamiliarInventoryContainer.WithdrawAvailableToInventory`, migrate all callers, and verify all-fit, partial-fit, no-fit, and once-per-container publication behavior.
- [x] 13.3 Stage MoneyPouch and inventory storage through the same transaction, delete its redundant snapshot/restore path, and verify rollback suppresses domain effects.
- [x] 13.4 Narrow storage and mutation-boundary concrete visibility; reconcile proposal, design, delta, current spec, historical task notes, and final ownership requirements. Run focused and full validation except deferred jscpd work.
- [x] 13.5 Make post-commit processing irreversible, exhaustive, deterministically ordered, and exception-preserving; add regressions for publisher, callback, aggregate, and pre-publication failures.
- [x] 13.6 Route MoneyPouch Add/Remove/AddFromInventory/MoveToInventory and BankContainer.DepositFromMoneyPouch through a single transaction; remove unused direct-storage pouch helpers and cover rollback/publication behavior.
- [x] 13.7 Make ShopStockContainer buy/sell economic mutations atomic, defer ShopItemBoughtEvent until commit, preserve shop policy/sorting, and cover pouch and inventory currency failures.
- [x] 13.8 Add concrete DuelStakeExchange, migrate every duel inventory/pouch stake and return path, make both-player cancellation refunds atomic, and preserve escrow/session state on refund failure.
- [x] 13.9 Preflight all conflicting equipment unequip permissions before mutation; retain custom UnEquipItem command behavior for non-weapon/shield replacements and add rejection regressions.
- [x] 13.10 Reconcile proposal, design, tasks, delta and current specs for accepted requirements; run focused suites, solution build, strict OpenSpec validation, full serial tests, and diff checks without jscpd.
- [x] 13.11 Make Weapon/Shield conflict replacement one atomic transaction with internal exact-slot insertion and ordered post-commit callbacks; retain the non-weapon command path, add rollback/success/publisher-failure regressions, reconcile proposal/design/delta/current spec, and run the requested validation without jscpd.
- [x] 13.12 Capture and restore each storage mutation revision with its transaction snapshot; verify false-result and exception rollback preserve pre-existing enumerators while successful mutations still invalidate them.
- [x] 13.13 Treat empty or all-null `TryAddRange` as a successful no-op without revision changes, publication, or transaction change tracking; preserve callbacks and committed range mutation behavior, with focused tests and validation.
- [x] 13.14 Harden the Equipment mutation API, split MoneyPouch staging behind `IMoneyPouchMutationBoundary`, and remove raw `ItemContainer.Storage` access through narrow internal operations; add boundary regressions and validate without jscpd.
- [x] 13.15 Commit Equipment replacement, full-removal, and clear storage before lifecycle callbacks; exhaustively attempt callbacks and publication after commit, preserving single errors and aggregating multiple failures. Add focused ordering/failure regressions and validate the updated requirement.

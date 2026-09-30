# Tasks

## 1. Storage core and transfer boundary

- [x] 1.1 Implement `ItemContainerStorage` as the sole owner of slots, mutation algorithms, revision/enumeration, reset-count handling, exact restoration, and stable mutation locking; port core mutation cases into `ItemContainerStorageTests` and run the Abstractions test project.
- [x] 1.2 Move storage-to-storage plan/commit logic into `ItemContainerStorage.TryTransfer`, add `IItemContainerStorageOwner` and `ItemContainerTransfer`, and verify exact transfer, publication, exceptions, sentinel movement, and lock ordering in `ItemContainerTransferTests`.
- [x] 1.3 Move `AddAndRemoveFrom` coordination to `ItemContainerTransfer`, preserve its current per-slot/partial behavior, and verify existing and new boundary cases in Abstractions tests.

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

- [ ] 7.1 Review the cumulative diff for one slot owner/one mutation algorithm, callback and publication order, exact persistence slots, and no copied generic container implementations; run focused Abstractions, GameWorld, Scripts and Extensions test projects.
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

# Tasks

## 1. Ordered synchronization and ownership

- [x] 1.1 Add `ItemContainerLockSet` and route transaction, standalone transfer, and owner-finalization acquisition through it; verify opposite lock order with `TryTransferTo_OppositeStandaloneTransfersUseOneLockOrder` and independent lock/publication ordering with `Begin_LocksAllParticipantsWhenInputOrderDiffersFromBoundaryOrder` plus publication-order coverage.
- [x] 1.2 Count physical standalone mutation ownership and transfer it to publication ownership without a gap; verify with `NormalMutation_UsesStorageMutationLock`, `ItemContainerAdd_FailedStandaloneMutationReleasesLockWithoutPublishing`, and `TryTransferTo_StandalonePublicationFailureReleasesClaimsAndUnblocksTransaction`.
- [x] 1.3 Preserve conflict behavior and snapshot timing; verify `TryTransferTo_ForeignTransactionPublicationReleasesAcquiredLockPrefixBeforeWaiting`, `Begin_SnapshotFailureReleasesAllLocksAndNeverExposesBindings`, `Begin_ResolvesEveryContributionBeforeAcquiringFirstLock`, and `TransactionBegin_WhenOwningStorageRejectsContendedAdditionalLocksAndReleasesPrefix`.
- [x] 1.4 Test foreign transaction, transaction publication, and standalone publication waits; verify `Mutation_WaitsForForeignTransactionPublicationOwnership`, `Begin_IndependentContenderWaitsForSameOrOverlappingStorage`, and `TryTransferTo_WaitsForForeignStandalonePublication`.
- [x] 1.5 Test nested disjoint success, same-boundary rejection, and nested contended rejection without deadlock; verify `Begin_NestedDisjointScopeSucceeds`, `Begin_NestedScopeForEnlistedBoundaryRejectsWithoutChangingOuterScope`, and `TransactionBegin_WhenOwningStorageRejectsContendedAdditionalLocksAndReleasesPrefix`.

## 2. Storage callbacks and gameplay failure rules

- [x] 2.1 Document and audit `CanStackItem` as a deterministic storage-level decision; verify the XML contract and current implementations in `ItemScript` and `PvPToken`.
- [x] 2.2 Add a deliberate reentrant/contended stacking-script test; verify `Add_MisbehavingStackCheckRejectsContendedStorageWithoutDeadlock` and unchanged source/destination state.
- [x] 2.3 Preserve ordinary rejection and misuse distinctions; verify `TryTransferTo_InterfaceTypedContainersLeaveBothStoragesUnchangedOnFailure` and `Begin_NestedContendedScopeRejectsWithoutWaiting`.

## 3. Atomic Equipment replacement

- [x] 3.1 Remove the replacement-approval API and keep the existing Equipment transaction path as the atomic swap; verify all `IEquipmentScript` implementers compile and `EquipItem_NonWeaponReplacementSwapsIntoFreedInventorySlot`.
- [x] 3.2 Preserve exact-instance transaction validation and rollback; verify `EquipItem_NonWeaponReplacementRollsBackOnDestinationPlanningFailure`, `EquipItem_RequiresTheExactIncomingInstanceToRemainInInventory`, `EquipItem_RepeatedRequestForSameInstanceDoesNotDuplicateItems`, and `EquipItem_ConcurrentRequestsForSameInstanceDoNotDuplicateItems`.
- [x] 3.3 Route active aura confirmation through `AuraEquipmentScript.EquipItem` while preserving direct unequip; verify `InactiveEquippedAura_DelegatesEquipImmediately`, `ActiveAuraReplacement_CancellationClosesDialogueWithoutEquipping`, `ActiveAuraReplacement_YesUsesEquipmentScriptEntryPointOnce`, `ActiveAuraReplacement_StaleConfirmationDoesNotEquipIncomingItem`, and `ActivatedAuraOption4_UsesAuraUnequipCallback`.
- [x] 3.4 Replace compensation assertions with atomic-conservation, ordering, and failure regressions; verify `EquipItem_NonWeaponReplacementRunsOnEquippedBeforePublication`, `EquipItem_NonWeaponReplacementPublicationFailureFollowsOnEquipped`, and `EquipItem_StandaloneOwnershipSpansLifecycleAndPublication`.

## 4. Removal and inventory drop correctness

- [x] 4.1 Make stack removal consume no more than the requested total while preserving exact removal; verify `Remove_MultipleMatchingStacks_RemovesRequestedTotalOnly`, `Remove_InsufficientMatchingStacksRemovesAvailableTotal`, `TryRemoveExact_InsufficientQuantityLeavesContainerUnchanged`, `Remove_NonStackableItemsFromMultipleSlots_Success`, and `Remove_WithCountToResetTo_ResetsItemCount`.
- [x] 4.2 Audit production partial/exact removals and change only complete-item consumers; verify reviewed callers and `InventoryDrop_FailedExactRemovalDoesNotCreateGroundItem`.
- [x] 4.3 Preserve exact drop quantity and failure behavior; verify `DropItem_CreatesGroundItemWithExactRemovedQuantity` and `DropItem_DoesNotCreateGroundItemWhenExactRemovalLosesRace`.

## 5. Shop stock normalization

- [x] 5.1 Capture immutable item-ID baselines and validate duplicate aggregation; verify `ShopStockConstructor_DuplicateBaselineCountsAggregate` and `ShopStockConstructor_BaselineOverflowThrows`.
- [x] 5.2 Plan normalization with copy-on-write and one state replacement; verify `NormalizeStock_UsesOriginalBaselineAfterItemMoves`, `NormalizeStock_HandlesPlayerStockBeyondOriginalEntries`, and `NormalizeStock_ChangesOnlyPlannedItemIdentity`.
- [x] 5.3 Preserve sentinel, no-op, and failure semantics; verify `NormalizeStock_RetainsDepletedPlayerStockAtZeroCount`, `NormalizeStock_NoChangeDoesNotAdvanceRevisionOrPublish`, and `NormalizeStock_PlanningFailureLeavesStateAndRevisionUnchanged`.

## 6. No-op mutations and decision-critical reads

- [x] 6.1 Make sort report actual movement and avoid no-op revision/publication; verify `Sort_AlreadyCompactContainerDoesNotReviseOrPublish` and `Sort_WithEmptySlotsRemovesGaps`.
- [x] 6.2 Preserve clear behavior for empty storage and zero-count sentinels; verify `Clear_EmptyAndZeroCapacityContainersPreserveRevisionAndDoNotPublish` and `Clear_NonnullZeroCountSentinelIsTreatedAsOccupied`.
- [x] 6.3 Audit requested Equipment, Bank, Rewards, Shop, Familiar, MoneyPouch, Trade/Duel, and Inventory preflight reads; verify the concrete stale-source/slot regression tests added for any unsafe path and record the review in `.git/testagent/pr516-itemcontainer-architecture-20261011/research.md`.

## 7. Specification, integration, and reviews

- [x] 7.1 Reconcile the delta and canonical item-container-storage spec; verify obsolete recovery wording is absent with `rg -n "Rewards storage, then Bank|Interactive Equipment replacement preserves" openspec/specs openspec/changes/archive`.
- [x] 7.2 Run focused Abstractions, GameWorld, and Scripts tests; record exact commands, test names, and results in `.git/testagent/pr516-itemcontainer-architecture-20261011/status.md`.
- [x] 7.3 Run `dotnet build Hagalaz.sln --no-restore --no-incremental`, `dotnet test Hagalaz.sln --no-build --no-restore`, `openspec validate --all --strict`, the existing jscpd gate, and `git diff --check`; record results in the test-agent status file.
- [x] 7.4 Complete three read-only reviews (concurrency/transaction, economy conservation, API/KISS), fix confirmed in-scope blockers, and rerun affected focused tests; record findings and disposition in the test-agent status file.
- [x] 7.5 Verify cumulative diff, public API changes, OpenSpec archive state, and clean/uncommitted working-tree status; do not commit or push.

## 8. Single-use dialogue confirmation

- [x] 8.1 Consume Yes/No callbacks before invoking and clear them on close; verify repeated clicks, cancellation, and reuse in `YesNoDialogueScriptTests`.
- [x] 8.2 Remove the approval API and verify stale dialogue confirmation plus repeated/concurrent equip requests cannot duplicate items in the focused Scripts and GameWorld tests.

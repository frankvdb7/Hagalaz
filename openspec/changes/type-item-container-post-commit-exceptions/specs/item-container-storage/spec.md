# Spec Delta

## MODIFIED Requirements

### Requirement: Transaction construction and commit preserve storage state
`ItemContainerTransaction` MUST resolve and validate all participants before locking, deduplicate repeated storage synchronizations without changing participant encounter order, acquire synchronizations in deterministic lock order, capture snapshots, and establish bindings only after construction succeeds. Construction failure MUST release acquired locks without changing storage or publishing. Mutations affect live storage while the scope is active. Dispose without Commit MUST restore snapshots and discard provisional tracker changes. Commit MUST make storage irreversible, promote all participant changes, release physical locks, then invoke each reference-distinct completion owner in resolved-synchronization encounter order: all owners' completion-effects phase runs before any owner's publication phase. Transaction bindings remain until Dispose so owners can complete without an intervening mutation. An exception reported during completion effects MUST make `Commit()` throw `ItemContainerPostCommitCompletionException`; an exception reported during publication MUST make it throw `ItemContainerPostCommitPublicationException`. The exception type identifies the phase in which the owner reports the failure, not necessarily its underlying cause: Equipment attempts its required state publication before reporting retained lifecycle-hook failures, so those failures surface as publication-phase failures. Both types MUST derive directly from `Exception`, retain the exact exception reported by the owner as `InnerException`, and make clear that storage is already irreversible. Callbacks MUST remain fail-fast and MUST NOT be retried. Failures before these callback phases, including transaction validation, snapshot, synchronization, tracker, or lock-release failures, MUST remain unwrapped. Snapshots restore slot topology, item references, counts, and storage revision, but do not deep-copy mutable item metadata such as `ExtraData`.

#### Scenario: Construction fails during snapshot capture
- **WHEN** snapshot capture throws after locks have been acquired
- **THEN** all acquired locks are released, no binding survives, and storage and publication remain unchanged

#### Scenario: Scope exits without commit
- **WHEN** an early return, exception, or cancellation exits an active scope
- **THEN** every participant is restored while its lock and binding are held, provisional changes are discarded, and no domain effect is published

#### Scenario: Commit automatically completes registered owners
- **WHEN** all mutations succeed and `Commit()` returns
- **THEN** tracker changes are promoted, physical locks are released, and each registered owner has completed before the transaction binding is released by `Dispose`

#### Scenario: Commit completes all storage before invoking owners
- **WHEN** a transaction commits changes for multiple registered owners
- **THEN** every participant's tracker changes are promoted and physical locks released before any owner callback runs

#### Scenario: Commit invokes each owner once in separate phases
- **WHEN** a transaction commits with one or more registered owners
- **THEN** each reference-distinct owner receives one effects-phase invocation followed by one publication-phase invocation in resolved-synchronization encounter order, before lock-order sorting

#### Scenario: Completion callback failure identifies committed storage
- **WHEN** a completion callback throws after storage becomes irreversible
- **THEN** `Commit()` throws `ItemContainerPostCommitCompletionException` with the exact callback exception as `InnerException`
- **AND** publication does not begin, storage remains changed, and disposal does not roll it back or retry the callback

#### Scenario: Publication-phase failure identifies committed storage
- **WHEN** an exception escapes a publication callback after completion succeeds
- **THEN** `Commit()` throws `ItemContainerPostCommitPublicationException` with the exact exception reported by the owner as `InnerException`
- **AND** earlier completion effects remain complete, later publishers are skipped, and disposal does not roll back or retry the committed operation

#### Scenario: Equipment reports retained hook failure from publication phase
- **WHEN** an Equipment lifecycle hook fails but Equipment state publication succeeds
- **THEN** Equipment reports the retained hook failure after publication and `Commit()` throws `ItemContainerPostCommitPublicationException` with that hook failure as `InnerException`

#### Scenario: Callback aggregate remains the inner exception
- **WHEN** a completion or publication callback throws an `AggregateException`
- **THEN** the corresponding post-commit exception retains the same aggregate instance as `InnerException` without flattening or replacing its contents

#### Scenario: Rejected mutation rolls back without a post-commit exception
- **WHEN** a staged operation returns false and the active scope is disposed without `Commit()`
- **THEN** all enlisted storage is restored and neither post-commit exception is thrown or publication occurs

#### Scenario: Pre-commit failure remains unwrapped
- **WHEN** a domain mutation throws before `Commit()` is called
- **THEN** the original exception propagates unchanged, disposal restores storage, and no completion or publication occurs

#### Scenario: Foreign operations wait through committed owner completion
- **WHEN** an owner completes changes after Commit but before transaction disposal
- **THEN** foreign mutations and tracker reads wait until the scope releases its bindings

# Game-world session specification

## Purpose

Describe the local and distributed ownership behavior for game-world sessions.

## Requirements

### Requirement: One active world session per account

The system MUST allow at most one active world session for an account in the distributed deployment.

#### Scenario: A second world sign-in races with the first

- GIVEN two game-world instances attempt to sign in the same account
- WHEN both attempt to acquire ownership
- THEN exactly one session owns the distributed claim
- AND the losing attempt does not hydrate or promote a world session

### Requirement: Session promotion is claim-protected

The system MUST promote a pending world session only while its exact distributed claim is still owned.

#### Scenario: Ownership expires before promotion

- GIVEN a pending world session whose claim is no longer owned
- WHEN promotion is attempted
- THEN the pending session is not promoted
- AND its exact-owner claim cleanup is reconciled

### Requirement: Lost connections are reconciled

The game-session lease cycle MUST own retries for deferred aborts and exact-session cleanup. A single processor may claim each pending abort with a store-owned marker. Failures release the marker for retry; claim, completion, and release verify the exact session instance so stale work cannot affect a replacement. Connection aborts MUST be idempotent.

#### Scenario: Aborting a lost connection fails temporarily

- GIVEN a session has lost its distributed claim
- WHEN the connection abort fails
- THEN the session remains reserved for abort reconciliation
- AND the existing lease cycle retries the abort

#### Scenario: Abort cleanup completes

- GIVEN an abort reservation is being processed
- WHEN the connection abort succeeds
- THEN the reservation is cleared
- AND the connection identifier can be reused

#### Scenario: Deferred abort is retried by the lease cycle

- GIVEN an abort reservation whose previous abort attempt failed
- WHEN the next lease cycle runs
- THEN the reservation is processed again
- AND a successful abort clears the reservation

#### Scenario: Abort failure does not create a second retry subsystem

- GIVEN an abort attempt fails
- WHEN the failure is recorded
- THEN the local reservation remains available for lease reconciliation
- AND no separate retry queue or retry worker is created

#### Scenario: A stale session cannot complete a newer reservation

- GIVEN a pending abort processor releases its processing marker after failure
- WHEN the lease cycle claims the reservation for a different session instance
- AND the previous processor later reports completion
- THEN the stale completion is rejected
- AND the newer reservation remains owned by the new processing marker

### Requirement: Cleanup is exact-owner and recoverable

The system MUST remove only the local session or distributed claim belonging to the expected session and MUST retain enough local state for reconciliation after a transient infrastructure failure.

#### Scenario: Cleanup does not remove a replacement session

- GIVEN the expected session has been replaced at the same connection
- WHEN exact-owner cleanup runs
- THEN the replacement session and its distributed claim remain intact

#### Scenario: Cleanup failure remains recoverable

- GIVEN exact-owner cleanup cannot complete because infrastructure fails temporarily
- WHEN cleanup returns
- THEN the local state needed to retry the cleanup is retained
- AND the existing lease cycle can retry the cleanup

#### Scenario: Failed admission completes after GameWorker termination

- GIVEN world admission registered a character, failed before session promotion, and scheduled rollback
- AND GameWorker execution has definitively completed before the rollback task receives a tick
- WHEN admission compensation runs
- THEN the registered character's exact local ownership is removed and its owned resources are disposed
- AND the exact persistence lifecycle and session ownership are released or retained for the existing recovery path
- AND compensation completes without requiring another GameWorker tick
- AND compensation never mutates GameWorld state while GameWorker execution may still run

### Requirement: Final logout snapshots include script detach cleanup

Terminal character detachment MUST invoke character-script detach callbacks before capturing its final snapshot so required synchronous domain cleanup is persisted. General character-script destruction callbacks MUST remain at actual character destruction.

#### Scenario: Pre-combat duel cancellation is included in the snapshot

- GIVEN a character has items in a pre-combat duel stake container
- WHEN terminal logout captures the character's final snapshot
- THEN the duel script's detach callback cancels the duel and returns the stakes before dehydration
- AND the final snapshot contains the returned items

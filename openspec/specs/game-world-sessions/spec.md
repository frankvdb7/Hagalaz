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

The system MUST retain a local abort reservation until the old connection has been given an abort opportunity and the reservation has been completed.

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

### Requirement: Cleanup is exact-owner and recoverable

The system MUST remove only the local session or distributed claim belonging to the expected session and MUST retain enough local state for reconciliation after a transient infrastructure failure.

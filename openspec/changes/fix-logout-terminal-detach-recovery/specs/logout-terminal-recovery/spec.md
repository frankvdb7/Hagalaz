## Purpose

This capability keeps terminal character logout cleanup recoverable when request cancellation outlives the caller's ability to complete the normal persistence and session-release handoff, while ensuring exactly one continuation owns the final snapshot.

## ADDED Requirements

### Requirement: Terminal detach preserves recoverability after canceled waiting

The system SHALL keep a pending logout recoverable when its terminal detach has been scheduled or started, the caller stops waiting because its cancellation token is canceled, and the terminal transition later establishes a valid final snapshot.

#### Scenario: Cancellation precedes worker execution

- **WHEN** terminal detach is scheduled, the caller's wait is canceled, and the worker later captures the final snapshot
- **THEN** the pending logout is recovery-eligible and can complete persistence and session cleanup through the existing recovery path

#### Scenario: Cancellation occurs during terminal execution

- **WHEN** the caller's wait is canceled while terminal detach is executing and the terminal transition later establishes the final snapshot
- **THEN** the pending logout is recovery-eligible without requiring the canceled caller continuation

#### Scenario: Terminal failure precedes a valid snapshot

- **WHEN** terminal detach fails before character ownership removal and final snapshot storage succeed
- **THEN** the logout is not marked recoverable solely because the caller was canceled

#### Scenario: Terminal failure follows snapshot establishment

- **WHEN** terminal detach establishes the final snapshot and then fails during remaining terminal cleanup
- **THEN** the pending logout is recovery-eligible and retains the original terminal exception for the caller or scheduler to report

### Requirement: A stored snapshot has one continuation owner

The system SHALL atomically assign one continuation owner to a pending final snapshot. Normal detachment and recovery SHALL NOT both issue persistence, session removal, or completion for that snapshot.

#### Scenario: Recovery owns a snapshot before another sign-out

- **WHEN** recovery claims a pending final snapshot and another sign-out is admitted for the same character
- **THEN** the second sign-out is rejected before it can issue forced persistence, and the recovery owner performs the persistence, session removal, and pending-state completion once

#### Scenario: Competing recovery scans

- **WHEN** two recovery operations inspect the same recovery-available pending logout
- **THEN** exactly one atomically claims it and the other performs no recovery work

#### Scenario: Failed pre-snapshot attempt is retried

- **WHEN** a canceled terminal attempt fails before storing a snapshot and a later attempt succeeds
- **THEN** the later attempt owns the normal continuation and is not treated as stale recovery work

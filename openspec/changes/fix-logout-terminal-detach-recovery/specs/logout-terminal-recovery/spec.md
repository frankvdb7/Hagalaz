## Purpose

This capability keeps terminal character logout cleanup recoverable when request cancellation outlives the caller's ability to complete the normal persistence and session-release handoff.

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

## Why

The logout request can time out while the GameWorker terminal-detach task continues. The task can then remove and destroy the character and store its final snapshot without making the pending logout recoverable, leaving persistence and session ownership stranded.

## What Changes

- Record cancellation of the terminal-detach wait without canceling the already-scheduled terminal transition.
- Atomically make a stored terminal snapshot recovery-eligible when the canceled wait has been observed.
- Preserve normal logout handoff ordering and retain recovery eligibility when terminal cleanup fails after a valid snapshot exists.
- Add deterministic regression coverage for cancellation before worker execution and recovery completion.

## Capabilities

### New Capabilities

- `logout-terminal-recovery`: Terminal character detachment remains recoverable when its request-side wait is canceled.

### Modified Capabilities

## Impact

Affected code is limited to `CharacterLogoutService` state/terminal-detach handling and its GameWorld logout tests. No map-region behavior, CodeQL cleanup, dependencies, or public transport contracts change.

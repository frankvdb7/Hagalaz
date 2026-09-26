## Why

The logout request can time out while the GameWorker terminal-detach task continues. The task can then remove and destroy the character and store its final snapshot without making the pending logout recoverable, leaving persistence and session ownership stranded. A first recovery fix also left normal and recovery continuations insufficiently exclusive: a later sign-out could issue another forced persistence command while recovery was already completing the same snapshot.

## What Changes

- Record cancellation against the specific terminal transition without canceling the already-scheduled terminal transition.
- Represent normal continuation, recovery availability, and claimed continuation ownership in the existing logout state gate.
- Atomically claim exactly one normal or recovery continuation for a stored snapshot.
- Preserve normal logout handoff ordering and retain recovery eligibility when terminal cleanup fails after a valid snapshot exists.
- Add deterministic regression coverage for competing sign-outs/recovery scans, stale cancellation, cancellation after snapshot publication, and blocked destruction.

## Capabilities

### New Capabilities

- `logout-terminal-recovery`: Terminal character detachment remains recoverable when its request-side wait is canceled.

### Modified Capabilities

## Impact

Affected code is limited to `CharacterLogoutService` state/terminal-detach handling and its GameWorld logout tests. No map-region behavior, CodeQL cleanup, dependencies, or public transport contracts change.

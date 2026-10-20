## Why

Two GameWorld processes compete for the CharacterHydrationState queue while storing saga instances in separate in-memory repositories. A character response can reach a process without the corresponding saga, leaving world login waiting until cancellation.

## What Changes

Route hydration requests to a temporary endpoint unique to the requesting WorldInstanceIdentity. Keep the existing state machine and in-memory repository. Replies must return to the same endpoint.

## Capabilities

### New Capabilities
- `world-hydration-ownership`: Process-local request and reply routing for character hydration.

### Modified Capabilities

## Impact

GameWorld MassTransit registration and regression tests. No map loading, persistence schema, inbox cleanup, other consumer routing, or protocol changes.

## Acceptance Criteria

- Two worlds with independent saga repositories both complete hydration requests.
- Each world's request and reply use its own temporary endpoint.
- A restarted world uses a new endpoint and does not depend on stale process state.

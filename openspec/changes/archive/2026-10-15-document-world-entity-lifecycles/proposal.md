# Proposal

## Why

Characters, NPCs, game objects, and ground items have different owners and lifecycle callbacks, but their paths are spread across services, regions, and entity classes. A source-backed guide will make creation, activation, ticking, persistence, and teardown easier to follow without changing runtime behavior.

## What Changes

- Add `docs/world-entity-lifecycles.md` describing the current lifecycle of the four entity types and their shared region/creature behavior.
- Link each lifecycle stage to the production service or model that owns it.
- Call out differences that are easy to confuse, including character persistence, NPC script startup, static game-object disable/enable, and ground-item expiry/respawn.

## Scope and Acceptance Criteria

- Document character construction, hydration, registration, runtime updates, final logout snapshot, and destruction.
- Document NPC registration, spawn callbacks, updates, death/respawn, unregister, and destruction.
- Document game-object region ownership, static enable/disable, dynamic removal, and destruction.
- Document ground-item ownership, timer expiry, pickup/removal, public conversion, respawn, and region teardown.
- Keep the guide factual and link to source files for implementation details.
- Make no runtime/API changes and preserve all existing working-tree changes.

## Non-Goals

- Do not redesign lifecycle callbacks or move runtime operations.
- Do not promise persistence for NPCs, game objects, or ground items beyond what the current code implements.
- Do not add a generic entity lifecycle abstraction or duplicate all OpenSpec requirements in prose.

## Capabilities

### New Capabilities

None. This change documents existing behavior.

### Modified Capabilities

None. No runtime behavior requirements change. The change opts out of delta specs with `skip_specs: true`.

## Impact

- Documentation: `docs/world-entity-lifecycles.md`.
- Runtime behavior: none.
- Existing OpenSpec capability requirements: unchanged.

## Stop Conditions

Stop and record a follow-up if tracing a lifecycle reveals that the guide would require a runtime behavior change to be accurate or if the four entity paths cannot be described from production code.

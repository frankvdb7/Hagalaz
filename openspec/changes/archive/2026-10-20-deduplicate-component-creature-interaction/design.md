# Design

## Context

Both public handlers first use their existing character or NPC service to locate a target. They then capture its generational creature handle and enqueue the same widget check, live-handle resolution, and component callback.

## Goals / Non-Goals

**Goals:**

- Keep the type-specific lookup in each public handler.
- Share only the identical queued operation and preserve deferred handle validation.
- Directly verify successful character and NPC calls through the public handlers.

**Non-Goals:**

- Change other component-use paths, scheduling, or message semantics.
- Introduce a separate service, interface, callback model, or static utility.

## Decisions

- Add one private instance method to `ComponentHub` that receives the character, captured `EntityHandle<ICreature>`, and existing component-use values. It owns only the repeated queued operation; the existing hub and `IEntityService` remain the owners of scheduling and live entity resolution.
  - A new class or interface would create a boundary with one caller and no independent responsibility. A callback would obscure identical arguments and add indirection.
- Keep target lookup in each handler, then call the helper only when that lookup succeeds. This preserves the lookup services and early-return behavior.
- Extend `CreatureInteractionHubTests` with an NPC success case and a closed-widget case. These assert the exact component id, target instance, force-run flag, extra data, and the shared operation's widget guard. Existing character success and stale-handle cases continue to cover the identity guard.

## Risks / Trade-offs

- Passing component values separately makes the helper signature longer, but keeps the operation explicit without adding a message abstraction shared by two distinct protocol types.

## Migration Plan

No persisted data or public API signatures change. Rollback consists of moving the helper body back into each handler.

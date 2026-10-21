# Design

## Context

See `proposal.md` for the motivation and acceptance criteria. Each sign-in consumer already composes the same concrete `ContactSnapshotLoader`, but currently owns the same connection lookup, feature lookup, and observation-window startup. The loader already owns snapshot request handling, response application, failure logging, snapshot-gate completion, and observation-window cleanup.

## Goals / Non-Goals

**Goals:**

- Make the existing loader the single owner of locating the per-connection contact feature and beginning the snapshot observation window.
- Keep lobby and world sign-in side effects in their existing consumers and preserve their current parallel execution with snapshot loading.

**Non-Goals:**

- Change session ownership, presence rules, request validation, or sign-in publication semantics.
- Add a new public API, interface, DI registration, state store, or retry mechanism.

## Decisions

- Compose `IGameConnectionService` into the existing concrete `ContactSnapshotLoader`. Its `OpenObservationWindowAsync` resolves the connection and `IContactsFeature`, returning a nullable feature/boundary tuple. `LoadAsync` accepts that tuple, returns immediately when it is absent, and otherwise issues the existing snapshot request.
  - This gives the existing snapshot lifecycle one owner and removes the exact duplicated block without introducing another component.
  - Keeping lookup and boundary creation in both consumers would retain the clone. A static helper or an interface-backed service would add indirection without a second implementation or runtime variant.
- Keep each consumer responsible for its distinct sign-in work. Lobby continues to send world information and publish lobby sign-in; world continues map initialization and world sign-in publication. Each awaits `OpenObservationWindowAsync` at the current point, starts `LoadAsync`, and then awaits it alongside the existing `Task.WhenAll` operations.
  - The two calls preserve the current ordering: connection lookup and window startup finish before consumer-specific sign-in side effects are started, while the contact request still runs alongside those effects.
- Keep snapshot completion and observation-window closure in the loader's existing `finally` block. Timeout and fault remain handled there; cancellation remains unhandled by the loader and propagates after cleanup.
  - The loader already owns these transitions, so moving contact discovery into it does not create a second cleanup owner.

## Risks / Trade-offs

- The loader separates window startup from request execution to preserve sequencing. Both consumers must await window startup at the current lookup position, then start snapshot loading before their existing `Task.WhenAll`.
- The contacts feature may be absent. The loader must complete without opening or completing a feature gate, matching the current consumer behavior.

## Migration Plan

No persisted data or external API changes. Update the loader construction and both call sites, then validate the consumer tests and the focused jscpd scan. Rollback consists of restoring the duplicated lookup/window startup at both call sites.

# Design

## Context

See [proposal.md](proposal.md). The implementation is distributed across GameWorld services, region ownership, entity classes, and entity scripts. The guide will describe those existing owners and call paths.

## Goals / Non-Goals

**Goals:**

- Give developers one entry point for following each entity from creation/registration through active updates and removal.
- Explain which lifecycle callbacks belong to the entity, its script, its region, or persistence.
- Link statements to the production files that implement them.

**Non-Goals:**

- Change lifecycle behavior or add callbacks.
- Specify every script-specific behavior or task scheduled by scripts.
- Replace the behavior requirements already held in OpenSpec.

## Decisions

- Put the guide in `docs/world-entity-lifecycles.md`, beside the existing character persistence guide. A standalone guide is appropriate because the request spans characters, NPCs, map objects, and ground items.
- Organize the guide around the lifecycle stages and then call out per-type differences. This makes the shared `Creature` path visible without implying that map objects and ground items are creatures.
- Describe behavior from the concrete GameWorld implementation. Link to creation/registration services, entity classes, region code, and the common game-worker tick path.
- Treat the guide as descriptive documentation. Set `skip_specs: true`; no runtime contract changes, so a delta requirement would duplicate existing behavior in a second form.
- Keep diagrams limited to the major ownership and callback sequences. Do not add a new lifecycle framework or duplicate detailed per-script logic.

## Risks / Trade-offs

- [Lifecycle code changes after documentation] → Link each stage to its owner file and keep the guide explicit that it describes current behavior.
- [A lifecycle detail may be missed in a broad codebase] → Trace the registration, region mutation, update, and destruction entry points for each entity type before finalizing.

## Migration Plan

No runtime migration is required. Review source links and OpenSpec validation after writing the guide.

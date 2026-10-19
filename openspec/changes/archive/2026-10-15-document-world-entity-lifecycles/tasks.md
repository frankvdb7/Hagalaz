# Tasks

## 1. Trace the current lifecycle paths

- [x] 1.1 Verify the production creation, registration, update, and destruction entry points for each entity type against the owning services and model classes.

## 2. Write and review the lifecycle guide

- [x] 2.1 Add `docs/world-entity-lifecycles.md` with source-linked lifecycle sections for characters, NPCs, game objects, and ground items; verify each statement against the corresponding implementation.
- [x] 2.2 Review the guide for scope, stale claims, and working-tree preservation; verify with `git diff --check` and `openspec validate document-world-entity-lifecycles --type change --strict`.

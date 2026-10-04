# Tasks

- [x] Split lock release from committed scope-binding release while preserving irreversible commit and failure policy.
- [x] Make foreign overlapping `Begin(...)` wait with the existing monitor and retry deterministic acquisition without holding a partial prefix.
- [x] Add deterministic tests for publication visibility/waiting, same-thread reentrancy, publication failure cleanup, multi-storage overlap, and updated callback lock/binding expectations.
- [x] Update canonical and active OpenSpec lifecycle requirements without changing unrelated architecture.
- [x] Run requested build, focused and serial test suites, strict OpenSpec validation, duplication gate, and diff/status checks.

# Tasks

## 1. Capability and Equipment API

- [x] 1.1 Add `IReadOnlyItemContainer`, make `IItemContainer` inherit it, add a minimal storage-backed read-only projection, and verify the abstraction interface tests pass.
- [x] 1.2 Change `IEquipmentContainer` and Equipment to expose the read-only view plus the equipment-slot indexer; verify interface shape and Equipment read behavior tests.
- [x] 1.3 Migrate generic Equipment reads to `.Items`, retain equipment-slot reads and domain mutations, then verify GameWorld and Scripts suites compile and pass.

## 2. Integration Validation

- [x] 2.1 Run the Abstractions, GameWorld, and Scripts test suites, solution build, strict OpenSpec validation, jscpd, and `git diff --check`; review all affected Equipment callers for intentional access paths.

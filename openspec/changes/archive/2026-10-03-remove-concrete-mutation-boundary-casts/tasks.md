# Tasks

## 1. Interface and caller cleanup

- [x] 1.1 Expose `EnsureOutsideTransaction` on `IItemContainerMutationBoundary`, rename the implementation method, and migrate Equipment checks to the interface.
- [x] 1.2 Add focused tests for outside and active transaction behavior, including unchanged storage and transaction state on rejection.

## 2. Validation

- [x] 2.1 Search all production code for concrete boundary casts and run the requested test suites, solution build, strict OpenSpec validation, jscpd, and diff check.

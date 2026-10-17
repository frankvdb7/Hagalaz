# Tasks

## 1. Boundary-owned tracker

- [x] 1.1 Add the sealed tracker with committed/provisional slot merging and consumption; verify standalone, full-change, empty-change, and result-copy tests pass.
- [x] 1.2 Route boundary mutations and automatic publication through the tracker; verify standalone mutation, suppression, publisher-failure, and transfer behavior tests pass.
- [x] 1.3 Remove transaction changed-slot accumulation and use tracker promotion/discard; verify commit, rollback-preservation, publication-order, and transaction ownership tests pass.

## 2. PriceChecker and regression coverage

- [x] 2.1 Expose the same tracker from concrete `ItemContainer` and migrate PriceChecker to incremental tracked updates; verify focused PriceChecker tests pass.
- [x] 2.2 Add tracker and boundary concurrency regressions, and update existing PriceChecker assertions for incremental slots; verify the full Abstractions and PriceChecker test suites pass.

## 3. Specification and integration validation

- [x] 3.1 Update canonical item-container requirements and verify strict OpenSpec validation passes.
- [x] 3.2 Run relevant Game Scripts, GameWorld, and domain tests, solution build, duplication checks, and `git diff --check`; review the cumulative diff for unchanged transaction and publication semantics.

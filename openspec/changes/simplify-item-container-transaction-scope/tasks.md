## 1. Transaction scope and regression guarantees

- [x] 1.1 Implement Begin/Commit/Dispose, opaque participation, deterministic locks, snapshot rollback, thread ownership and exception-safe construction (AC1, AC2, AC3).
- [x] 1.2 Aggregate mutation notifications, enforce complete same-scope participation, and perform ordered post-unlock completion with irreversible failure semantics (AC3, AC4, AC5, AC6).
- [x] 1.3 Add regression coverage for construction failure, rollback, idempotent disposal, ownership, aliases, joining, ordering and completion failures (AC1–6).

## 2. Domain migration

- [x] 2.1 Migrate exact transfers and pouch operations; remove staging/receipt APIs and preserve overflow behavior (AC4, AC6, AC7).
- [x] 2.2 Migrate equipment, shop, bank, familiar and duel operations while preserving domain hook and notification ordering (AC4, AC5, AC7).
- [x] 2.3 Migrate session-owned trade completion/refunds/recovery and preserve issue #347 terminal cleanup and rejection behavior (AC7).
- [x] 2.4 Migrate existing tests and add focused domain regressions for the new common path (AC5, AC7).

## 3. Validation and specification

- [x] 3.1 Synchronize conflicting current behavior requirements with the approved lifecycle and validate OpenSpec strictly (AC1–7).
- [x] 3.2 Run abstraction, script and GameWorld regression suites and solution build; review the cumulative change for obsolete APIs and scope drift (AC1–7).

## 4. PR #516 review

- [x] 4.1 Resolve pouch inventory contributions through the existing internal participant bridge; test unsupported/malformed inventory rejection before locks, composite contributions, aliases and publication order (AC1, AC4, AC6, AC7).
- [x] 4.2 Remove the reported terminal trade test clone with private setup/assertion helpers and guarantee test disposal on exceptional paths (AC2, AC7).
- [x] 4.3 Review the complete PR and validate focused suites, solution build, the full CI test command, strict OpenSpec, the pinned duplication gate against the actual PR base, diff cleanliness and final-head hosted CI/CodeQL (AC1–7).
## 5. Contention and completion review

- [x] 5.1 Distinguish current-thread joining/nesting from independent cross-thread lock contention; add same/overlapping storage and helper regressions without sleeps (AC1, AC2, AC6).
- [x] 5.2 Review joining helper failure side effects, preserve standalone messages outside owned locks, and document validation boundaries that cannot become pure without domain redesign (AC3, AC7).
- [x] 5.3 Flatten completion failures, preserve primary construction/rollback failures during cleanup, and document eager-reader isolation and thread limits (AC2, AC3, AC5).
- [x] 5.4 Run focused tests first, all three regression suites, build, full CI tests, strict OpenSpec, duplication and diff gates; inspect hosted CI and CodeQL for the updated PR head (AC1–7).
## 6. Domain-neutral after-publication actions

- [x] 6.1 Rename the internal after-publication list/gateway, migrate pouch and test callers, and describe completion ordering in domain-neutral terms without changing control flow or public APIs (AC3–5, AC7).
- [x] 6.2 Verify hook/container/action ordering, container-failure skipping and middle-action failure; retain real pouch publication-order regressions (AC3–5, AC7).
- [x] 6.3 Run focused and full regression suites, build, strict OpenSpec, duplication and diff checks; record local validation evidence (AC1–7).

After pushing this refactor, verify hosted CI and CodeQL against that exact head. Record their results in the PR description and final handoff, without a documentation-only commit that would invalidate the referenced head.

## 7. Fixed owner completion

- [x] 7.1 Remove transaction/boundary callback queues and registration, delegate fixed completion to owner state, preserve ordering and discard on every exit (AC2�5).
- [x] 7.2 Migrate equipment and pouch to owned pending data and test fixtures to fixed completion; add reflection, multiple mutation, alias, rollback/stale-effect and failure regressions (AC2�5, AC7).
- [x] 7.3 Run focused tests, all three suites, solution build, exact CI test command, strict OpenSpec, duplication and diff checks; record local evidence and review cumulative scope (AC1�7).

Verify final pushed-head hosted CI and CodeQL and report their results in the PR description and final handoff.

## 8. Direct typed equipment execution

- [x] 8.1 Replace delegate conversion with direct typed equipment execution and explicit standalone publication; mechanically rename discard without changing transaction mechanics (AC2-5, AC7).
- [x] 8.2 Add focused regressions for delegate-free pending facts, standalone and committed attempt-all/failure ordering, and no stale committed effects (AC3-5, AC7).
- [x] 8.3 Run the three suites, solution build, exact CI command, strict OpenSpec, duplication and diff checks; record local evidence (AC1-7).

Per the user's current instruction, do not commit, push or monitor hosted checks automatically. Hosted validation of these local changes remains pending until a push is requested.

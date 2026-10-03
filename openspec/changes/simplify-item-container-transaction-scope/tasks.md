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

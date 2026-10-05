# Tasks

- [x] Split lock release from committed scope-binding release while preserving irreversible commit and failure policy.
- [x] Make foreign overlapping `Begin(...)` wait with the existing monitor and retry deterministic acquisition without holding a partial prefix.
- [x] Add deterministic tests for publication visibility/waiting, same-thread reentrancy, publication failure cleanup, multi-storage overlap, and updated callback lock/binding expectations.
- [x] Update canonical and active OpenSpec lifecycle requirements without changing unrelated architecture.
- [x] Run requested build, focused and serial test suites, strict OpenSpec validation, duplication gate, and diff/status checks.
- [x] Keep storage mutation validation under its lock; capture publication ownership in aggregates under the same lock and pass it explicitly to notification and Equipment completion.
- [x] Make committed binding teardown reacquire the full ordered lock set, retry interrupted lock acquisition, and propagate interruption only after cleanup.
- [x] Require caller-owned shared scope for MoneyPouch coin transfer and migrate terminal TradeExchange movement to exact transfers.
- [x] Add deterministic attribution, stale authorization, interrupted teardown, alias, pouch, and Trade staging regression coverage.
- [x] Update canonical lifecycle/trade/rollback requirements, correct active deltas, and archive the obsolete trade-completion change.
- [x] Run full local build and serial test workflow, strict OpenSpec and archive validation, C# duplication gate, and final diff/status review.

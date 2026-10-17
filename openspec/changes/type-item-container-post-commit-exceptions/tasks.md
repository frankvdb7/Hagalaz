# Tasks

## 1. Typed transaction completion failures

- [x] 1.1 Add `ItemContainerPostCommitException` and its completion/publication phase enum; verify the public API preserves the exact inner exception and documents that storage is irreversible.
- [x] 1.2 Wrap only transaction-owned completion and publication callbacks in `Commit()`; verify infrastructure and pre-commit exceptions remain unwrapped and callback order/fail-fast behavior is unchanged.
- [x] 1.3 Add transaction regressions for completion failure, publication failure, aggregate inner exceptions, rejected operations, and pre-commit rollback; verify committed state, disposal cleanup, and no callback retry.
- [x] 1.4 Update transaction-owned publisher/equipment exception assertions in GameWorld, Scripts, and trade tests; verify each expects the typed phase and exact original exception.

## 2. Duel and trade integration

- [x] 2.1 Handle only `ItemContainerPostCommitException` at the duel victory boundary and log it while continuing the existing victory flow; verify real death completion finishes without duplicate payout and capacity rejection remains retryable.
- [x] 2.2 Verify trade completion/refund stays terminal when transaction publication throws and the original exception is retained by the typed wrapper.

## 3. Specifications and validation

- [x] 3.1 Update canonical item-container and trading requirements plus the active duel settlement change to describe phase classification and duel handling; verify deltas match canonical behavior.
- [x] 3.2 Run focused Abstractions, GameWorld, and Scripts tests, then solution build and full solution tests; record any environment-blocked checks.
- [x] 3.3 Run `openspec validate --all --strict`, the repository duplication gate, and `git diff --check`; inspect the final cumulative diff for scope and warning regressions.

## Validation notes

- Focused tests passed: Game Abstractions (355), GameWorld (1,336), and Game Scripts (378).
- Solution build passed with no errors and 253 existing warnings (package vulnerability, nullable, and MSTest analyzer warnings); no warnings were reported from changed files.
- The full solution test run reached all projects; 23 tests in `Hagalaz.Services.Characters.Tests`, `Hagalaz.Data.IntegrationTests`, and `Hagalaz.Services.GameWorld.IntegrationTests` failed because Testcontainers could not connect to Docker at `npipe://./pipe/docker_engine`. Other projects, including the focused suites, passed.
- `openspec validate --all --strict` passed all 71 items; jscpd 5.3.2 found the same 502 baseline clones and no new clone; `git diff --check` passed.
